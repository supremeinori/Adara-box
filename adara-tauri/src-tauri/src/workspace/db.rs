use std::collections::HashSet;
use std::path::{Path, PathBuf};
use std::sync::Mutex;

use chrono::Utc;
use rusqlite::{params, Connection, OptionalExtension};
use uuid::Uuid;

use super::models::{NewResourceInput, ResourceDto, ResourceType, WorkspaceDto};
use super::validator;

const TARGET_USER_VERSION: i32 = 5;
const MAX_BACKUPS: usize = 5;

pub struct WorkspaceStore {
    db_path: PathBuf,
    icons_dir: PathBuf,
    backups_dir: PathBuf,
    lock: Mutex<()>,
}

impl WorkspaceStore {
    pub fn default_store() -> Self {
        let root = data_root_dir();
        Self::for_paths(
            root.join("adarabox.db"),
            root.join("icons"),
            root.join("backups"),
        )
    }

    pub fn for_paths(db_path: PathBuf, icons_dir: PathBuf, backups_dir: PathBuf) -> Self {
        Self {
            db_path,
            icons_dir,
            backups_dir,
            lock: Mutex::new(()),
        }
    }

    pub fn db_path(&self) -> &Path {
        &self.db_path
    }

    pub fn data_dir(&self) -> PathBuf {
        self.db_path
            .parent()
            .map(Path::to_path_buf)
            .unwrap_or_else(data_root_dir)
    }

    pub fn icons_dir(&self) -> &Path {
        &self.icons_dir
    }

    pub fn with_connection<T, F>(&self, f: F) -> Result<T, String>
    where
        F: FnOnce(&Connection) -> Result<T, String>,
    {
        let _guard = self.lock.lock().map_err(|e| e.to_string())?;
        let conn = self.open_connection()?;
        f(&conn)
    }

    pub fn ensure_initialized(&self) -> Result<(), String> {
        let _guard = self.lock.lock().map_err(|e| e.to_string())?;
        self.init_locked()
    }

    fn init_locked(&self) -> Result<(), String> {
        let conn = self.open_connection()?;

        // Verify integrity on existing databases before applying migrations.
        let check: String = conn
            .query_row("PRAGMA quick_check(1)", [], |r| r.get(0))
            .map_err(|e| format!("SQLite quick_check failed: {e}"))?;
        if !check.eq_ignore_ascii_case("ok") {
            return Err(format!("Database integrity check failed: {check}"));
        }

        conn.execute_batch(
            "
            CREATE TABLE IF NOT EXISTS workspace (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                sort_order INTEGER NOT NULL DEFAULT 0,
                icon TEXT NOT NULL DEFAULT '📁'
            );

            CREATE TABLE IF NOT EXISTS resource (
                id TEXT PRIMARY KEY,
                workspace_id TEXT NOT NULL REFERENCES workspace(id) ON DELETE CASCADE,
                name TEXT NOT NULL,
                type TEXT NOT NULL CHECK (type IN ('application', 'file', 'folder', 'link')),
                target TEXT NOT NULL,
                sort_order INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS media_link (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                url TEXT NOT NULL,
                sort_order INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS library_books (
                id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                author TEXT NOT NULL DEFAULT 'Unknown Author',
                file_type TEXT NOT NULL CHECK (file_type IN ('pdf', 'epub')),
                file_path TEXT NOT NULL,
                cover_path TEXT,
                current_page INTEGER NOT NULL DEFAULT 1,
                total_pages INTEGER NOT NULL DEFAULT 0,
                current_cfi TEXT,
                reading_progress REAL NOT NULL DEFAULT 0.0,
                last_read_at TEXT,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );
            ",
        )
        .map_err(|e| format!("Failed creating schema: {e}"))?;

        Self::ensure_column(&conn, "workspace", "sort_order", "INTEGER NOT NULL DEFAULT 0")?;
        Self::ensure_column(&conn, "workspace", "icon", "TEXT NOT NULL DEFAULT '📁'")?;
        Self::ensure_column(&conn, "resource", "sort_order", "INTEGER NOT NULL DEFAULT 0")?;
        Self::ensure_column(&conn, "resource", "created_at", "TEXT NOT NULL DEFAULT ''")?;
        Self::ensure_column(&conn, "resource", "updated_at", "TEXT NOT NULL DEFAULT ''")?;
        Self::ensure_column(&conn, "library_books", "category", "TEXT NOT NULL DEFAULT 'Any'")?;
        Self::ensure_column(&conn, "library_books", "status", "TEXT NOT NULL DEFAULT 'New'")?;
        Self::ensure_column(&conn, "library_books", "notes", "TEXT NOT NULL DEFAULT ''")?;
        Self::ensure_column(&conn, "library_books", "file_name", "TEXT NOT NULL DEFAULT ''")?;
        Self::ensure_column(&conn, "library_books", "file_size", "INTEGER NOT NULL DEFAULT 0")?;

        conn.execute_batch(
            "
            CREATE INDEX IF NOT EXISTS idx_workspace_sort ON workspace(sort_order, created_at);
            CREATE INDEX IF NOT EXISTS idx_resource_ws ON resource(workspace_id, sort_order, created_at);
            CREATE INDEX IF NOT EXISTS idx_library_updated ON library_books(updated_at DESC);
            ",
        )
        .map_err(|e| format!("Failed creating indexes: {e}"))?;

        let user_version: i32 = conn
            .query_row("PRAGMA user_version", [], |r| r.get(0))
            .map_err(|e| format!("Failed reading user_version: {e}"))?;
        if user_version < TARGET_USER_VERSION {
            conn.execute_batch(&format!("PRAGMA user_version = {TARGET_USER_VERSION};"))
                .map_err(|e| format!("Failed updating user_version: {e}"))?;
        }

        // Seed initial workspace once when opening a completely fresh database.
        let seeded: Option<String> = conn
            .query_row(
                "SELECT value FROM settings WHERE key = 'adarabox_seeded'",
                [],
                |r| r.get(0),
            )
            .optional()
            .map_err(|e| format!("Failed checking seed flag: {e}"))?;
        let ws_count: i64 = conn
            .query_row("SELECT COUNT(*) FROM workspace", [], |r| r.get(0))
            .map_err(|e| format!("Failed counting workspaces: {e}"))?;

        if seeded.is_none() && ws_count == 0 {
            let now = Utc::now().to_rfc3339();
            let id = Uuid::new_v4().to_string();
            conn.execute(
                "INSERT INTO workspace (id, name, icon, sort_order, created_at, updated_at) VALUES (?1, ?2, ?3, 0, ?4, ?4)",
                params![id, "My Workspace", "📁", now],
            )
            .map_err(|e| format!("Failed seeding default workspace: {e}"))?;
            conn.execute(
                "INSERT OR REPLACE INTO settings (key, value) VALUES ('adarabox_seeded', '1')",
                [],
            )
            .map_err(|e| format!("Failed marking seed flag: {e}"))?;
        } else if seeded.is_none() && ws_count > 0 {
            conn.execute(
                "INSERT OR REPLACE INTO settings (key, value) VALUES ('adarabox_seeded', '1')",
                [],
            )
            .map_err(|e| format!("Failed marking existing DB seeded: {e}"))?;
        }

        Ok(())
    }

    fn ensure_column(
        conn: &Connection,
        table: &str,
        column: &str,
        ddl: &str,
    ) -> Result<(), String> {
        let mut stmt = conn
            .prepare(&format!("PRAGMA table_info({table})"))
            .map_err(|e| format!("Failed inspecting table {table}: {e}"))?;
        let cols: Vec<String> = stmt
            .query_map([], |r| r.get::<_, String>(1))
            .map_err(|e| format!("Failed reading columns of {table}: {e}"))?
            .collect::<Result<Vec<_>, _>>()
            .map_err(|e| format!("Failed collecting columns of {table}: {e}"))?;
        if !cols.iter().any(|c| c.eq_ignore_ascii_case(column)) {
            conn.execute(&format!("ALTER TABLE {table} ADD COLUMN {column} {ddl}"), [])
                .map_err(|e| format!("Failed migrating {table}.{column}: {e}"))?;
        }
        Ok(())
    }

    fn open_connection(&self) -> Result<Connection, String> {
        if let Some(parent) = self.db_path.parent() {
            std::fs::create_dir_all(parent)
                .map_err(|e| format!("Failed creating database directory: {e}"))?;
        }
        let conn = Connection::open(&self.db_path)
            .map_err(|e| format!("Failed opening SQLite database: {e}"))?;
        conn.execute_batch(
            "
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = NORMAL;
            PRAGMA foreign_keys = ON;
            PRAGMA busy_timeout = 5000;
            ",
        )
        .map_err(|e| format!("Failed configuring SQLite pragmas: {e}"))?;
        Ok(conn)
    }

    pub fn list_workspaces(&self) -> Result<Vec<WorkspaceDto>, String> {
        let _guard = self.lock.lock().map_err(|e| e.to_string())?;
        let conn = self.open_connection()?;
        let mut stmt = conn
            .prepare(
                "
                SELECT w.id, w.name, COALESCE(NULLIF(w.icon, ''), '📁'), w.sort_order, w.created_at, w.updated_at,
                       (SELECT COUNT(*) FROM resource r WHERE r.workspace_id = w.id) AS res_count
                FROM workspace w
                ORDER BY w.sort_order ASC, w.created_at ASC
                ",
            )
            .map_err(|e| format!("Failed preparing workspace query: {e}"))?;
        let rows = stmt
            .query_map([], |row| {
                Ok(WorkspaceDto {
                    id: row.get(0)?,
                    name: row.get(1)?,
                    icon: row.get(2)?,
                    sort_order: row.get(3)?,
                    created_at: row.get(4)?,
                    updated_at: row.get(5)?,
                    resource_count: row.get::<_, i64>(6)?.max(0) as usize,
                })
            })
            .map_err(|e| format!("Failed querying workspaces: {e}"))?;

        rows.collect::<Result<Vec<_>, _>>()
            .map_err(|e| format!("Failed reading workspace rows: {e}"))
    }

    pub fn create_workspace(&self, name: &str, icon: Option<&str>) -> Result<WorkspaceDto, String> {
        let clean_name = validator::validate_workspace_name(name)?;
        let clean_icon = validator::validate_workspace_icon(icon)?;
        let _guard = self.lock.lock().map_err(|e| e.to_string())?;
        let conn = self.open_connection()?;

        let next_order: i64 = conn
            .query_row(
                "SELECT COALESCE(MAX(sort_order) + 1, 0) FROM workspace",
                [],
                |r| r.get(0),
            )
            .map_err(|e| format!("Failed computing workspace sort order: {e}"))?;

        let now = Utc::now().to_rfc3339();
        let id = Uuid::new_v4().to_string();
        conn.execute(
            "INSERT INTO workspace (id, name, icon, sort_order, created_at, updated_at) VALUES (?1, ?2, ?3, ?4, ?5, ?5)",
            params![id, clean_name, clean_icon, next_order, now],
        )
        .map_err(|e| format!("Failed creating workspace: {e}"))?;

        Ok(WorkspaceDto {
            id,
            name: clean_name,
            icon: clean_icon,
            sort_order: next_order,
            created_at: now.clone(),
            updated_at: now,
            resource_count: 0,
        })
    }

    pub fn update_workspace(
        &self,
        id: &str,
        name: &str,
        icon: Option<&str>,
    ) -> Result<WorkspaceDto, String> {
        let clean_name = validator::validate_workspace_name(name)?;
        let _guard = self.lock.lock().map_err(|e| e.to_string())?;
        let conn = self.open_connection()?;

        let existing_icon: String = conn
            .query_row(
                "SELECT COALESCE(NULLIF(icon, ''), '📁') FROM workspace WHERE id = ?1",
                params![id],
                |r| r.get(0),
            )
            .optional()
            .map_err(|e| format!("Failed loading workspace: {e}"))?
            .ok_or_else(|| format!("Workspace not found: {id}"))?;

        let clean_icon = match icon {
            Some(raw) => validator::validate_workspace_icon(Some(raw))?,
            None => existing_icon.clone(),
        };

        let now = Utc::now().to_rfc3339();
        conn.execute(
            "UPDATE workspace SET name = ?1, icon = ?2, updated_at = ?3 WHERE id = ?4",
            params![clean_name, clean_icon, now, id],
        )
        .map_err(|e| format!("Failed updating workspace: {e}"))?;

        if existing_icon != clean_icon {
            self.cleanup_unused_custom_icon(&conn, &existing_icon);
        }

        let dto = conn
            .query_row(
                "
                SELECT w.id, w.name, w.icon, w.sort_order, w.created_at, w.updated_at,
                       (SELECT COUNT(*) FROM resource r WHERE r.workspace_id = w.id)
                FROM workspace w WHERE w.id = ?1
                ",
                params![id],
                |r| {
                    Ok(WorkspaceDto {
                        id: r.get(0)?,
                        name: r.get(1)?,
                        icon: r.get(2)?,
                        sort_order: r.get(3)?,
                        created_at: r.get(4)?,
                        updated_at: r.get(5)?,
                        resource_count: r.get::<_, i64>(6)?.max(0) as usize,
                    })
                },
            )
            .map_err(|e| format!("Failed reading updated workspace: {e}"))?;
        Ok(dto)
    }

    pub fn reorder_workspaces(&self, ordered_ids: &[String]) -> Result<(), String> {
        let _guard = self.lock.lock().map_err(|e| e.to_string())?;
        let mut conn = self.open_connection()?;
        let tx = conn
            .transaction()
            .map_err(|e| format!("Failed starting reorder transaction: {e}"))?;
        let now = Utc::now().to_rfc3339();
        for (idx, id) in ordered_ids.iter().enumerate() {
            tx.execute(
                "UPDATE workspace SET sort_order = ?1, updated_at = ?2 WHERE id = ?3",
                params![idx as i64, now, id],
            )
            .map_err(|e| format!("Failed updating workspace order: {e}"))?;
        }
        tx.commit()
            .map_err(|e| format!("Failed committing workspace order: {e}"))?;
        Ok(())
    }

    pub fn delete_workspace(&self, id: &str) -> Result<(), String> {
        let _guard = self.lock.lock().map_err(|e| e.to_string())?;
        let conn = self.open_connection()?;
        let prev_icon: Option<String> = conn
            .query_row(
                "SELECT icon FROM workspace WHERE id = ?1",
                params![id],
                |r| r.get(0),
            )
            .optional()
            .map_err(|e| format!("Failed reading workspace before delete: {e}"))?;

        let deleted = conn
            .execute("DELETE FROM workspace WHERE id = ?1", params![id])
            .map_err(|e| format!("Failed deleting workspace: {e}"))?;
        if deleted == 0 {
            return Err(format!("Workspace not found: {id}"));
        }
        if let Some(icon) = prev_icon {
            self.cleanup_unused_custom_icon(&conn, &icon);
        }
        Ok(())
    }

    pub fn list_resources(&self, workspace_id: &str) -> Result<Vec<ResourceDto>, String> {
        let _guard = self.lock.lock().map_err(|e| e.to_string())?;
        let conn = self.open_connection()?;
        let mut stmt = conn
            .prepare(
                "
                SELECT id, workspace_id, name, type, target, sort_order, created_at, updated_at
                FROM resource
                WHERE workspace_id = ?1
                ORDER BY sort_order ASC, created_at ASC
                ",
            )
            .map_err(|e| format!("Failed preparing resource query: {e}"))?;
        let rows = stmt
            .query_map(params![workspace_id], Self::map_resource_row)
            .map_err(|e| format!("Failed querying resources: {e}"))?;
        rows.collect::<Result<Vec<_>, _>>()
            .map_err(|e| format!("Failed reading resource rows: {e}"))
    }

    pub fn get_resource(&self, id: &str) -> Result<ResourceDto, String> {
        let _guard = self.lock.lock().map_err(|e| e.to_string())?;
        let conn = self.open_connection()?;
        conn.query_row(
            "SELECT id, workspace_id, name, type, target, sort_order, created_at, updated_at FROM resource WHERE id = ?1",
            params![id],
            Self::map_resource_row,
        )
        .optional()
        .map_err(|e| format!("Failed loading resource: {e}"))?
        .ok_or_else(|| format!("Resource not found: {id}"))
    }

    pub fn add_resource(
        &self,
        workspace_id: &str,
        name: &str,
        resource_type: ResourceType,
        target: &str,
    ) -> Result<ResourceDto, String> {
        let items = vec![NewResourceInput {
            name: Some(name.to_string()),
            resource_type: Some(resource_type),
            target: target.to_string(),
        }];
        let mut inserted = self.add_resources_bulk(workspace_id, &items)?;
        Ok(inserted.remove(0))
    }

    pub fn add_resources_bulk(
        &self,
        workspace_id: &str,
        inputs: &[NewResourceInput],
    ) -> Result<Vec<ResourceDto>, String> {
        if inputs.is_empty() {
            return Ok(Vec::new());
        }
        let _guard = self.lock.lock().map_err(|e| e.to_string())?;
        let mut conn = self.open_connection()?;

        let exists: i64 = conn
            .query_row(
                "SELECT COUNT(*) FROM workspace WHERE id = ?1",
                params![workspace_id],
                |r| r.get(0),
            )
            .map_err(|e| format!("Failed checking workspace existence: {e}"))?;
        if exists == 0 {
            return Err(format!("Workspace not found: {workspace_id}"));
        }

        let mut existing_targets: HashSet<(String, String)> = {
            let mut stmt = conn
                .prepare("SELECT type, target FROM resource WHERE workspace_id = ?1")
                .map_err(|e| format!("Failed checking duplicate targets: {e}"))?;
            let rows = stmt
                .query_map(params![workspace_id], |r| {
                    Ok((
                        r.get::<_, String>(0)?,
                        r.get::<_, String>(1)?.to_ascii_lowercase(),
                    ))
                })
                .map_err(|e| format!("Failed reading existing targets: {e}"))?;
            rows.collect::<Result<HashSet<_>, _>>()
                .map_err(|e| format!("Failed collecting existing targets: {e}"))?
        };

        let mut next_order: i64 = conn
            .query_row(
                "SELECT COALESCE(MAX(sort_order) + 1, 0) FROM resource WHERE workspace_id = ?1",
                params![workspace_id],
                |r| r.get(0),
            )
            .map_err(|e| format!("Failed computing resource order: {e}"))?;

        let tx = conn
            .transaction()
            .map_err(|e| format!("Failed starting resource transaction: {e}"))?;
        let now = Utc::now().to_rfc3339();
        let mut created = Vec::with_capacity(inputs.len());

        for input in inputs {
            let (rtype, clean_target, default_name) = match input.resource_type {
                Some(explicit_type) => {
                    let clean_target =
                        validator::validate_and_normalize_target(explicit_type, &input.target)?;
                    let fallback = match explicit_type {
                        ResourceType::Link => validator::friendly_link_name(&clean_target),
                        _ => Path::new(&clean_target)
                            .file_name()
                            .and_then(|s| s.to_str())
                            .unwrap_or(&clean_target)
                            .to_string(),
                    };
                    (explicit_type, clean_target, fallback)
                }
                None => validator::infer_resource_from_raw(&input.target)?,
            };

            let raw_name = input
                .name
                .as_deref()
                .map(str::trim)
                .filter(|s| !s.is_empty())
                .unwrap_or(&default_name);
            let clean_name = validator::validate_resource_name(raw_name)?;

            // Skip exact duplicates when bulk-dropping; error cleanly on explicit single-add duplicate.
            let key = (
                rtype.as_str().to_string(),
                clean_target.to_ascii_lowercase(),
            );
            if existing_targets.contains(&key) {
                if inputs.len() == 1 {
                    return Err("This resource is already in the workspace.".into());
                }
                continue;
            }
            existing_targets.insert(key);

            let id = Uuid::new_v4().to_string();
            tx.execute(
                "INSERT INTO resource (id, workspace_id, name, type, target, sort_order, created_at, updated_at) VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?7)",
                params![id, workspace_id, clean_name, rtype.as_str(), clean_target, next_order, now],
            )
            .map_err(|e| format!("Failed inserting resource: {e}"))?;

            created.push(ResourceDto {
                id,
                workspace_id: workspace_id.to_string(),
                name: clean_name,
                resource_type: rtype,
                target: clean_target,
                sort_order: next_order,
                created_at: now.clone(),
                updated_at: now.clone(),
            });
            next_order += 1;
        }

        if !created.is_empty() {
            tx.execute(
                "UPDATE workspace SET updated_at = ?1 WHERE id = ?2",
                params![now, workspace_id],
            )
            .map_err(|e| format!("Failed touching workspace updated_at: {e}"))?;
        }

        tx.commit()
            .map_err(|e| format!("Failed committing resources: {e}"))?;
        Ok(created)
    }

    pub fn update_resource(
        &self,
        id: &str,
        name: &str,
        resource_type: ResourceType,
        target: &str,
    ) -> Result<ResourceDto, String> {
        let clean_name = validator::validate_resource_name(name)?;
        let clean_target = validator::validate_and_normalize_target(resource_type, target)?;
        let _guard = self.lock.lock().map_err(|e| e.to_string())?;
        let conn = self.open_connection()?;

        let workspace_id: String = conn
            .query_row(
                "SELECT workspace_id FROM resource WHERE id = ?1",
                params![id],
                |r| r.get(0),
            )
            .optional()
            .map_err(|e| format!("Failed finding resource: {e}"))?
            .ok_or_else(|| format!("Resource not found: {id}"))?;

        let now = Utc::now().to_rfc3339();
        conn.execute(
            "UPDATE resource SET name = ?1, type = ?2, target = ?3, updated_at = ?4 WHERE id = ?5",
            params![clean_name, resource_type.as_str(), clean_target, now, id],
        )
        .map_err(|e| format!("Failed updating resource: {e}"))?;
        conn.execute(
            "UPDATE workspace SET updated_at = ?1 WHERE id = ?2",
            params![now, workspace_id],
        )
        .map_err(|e| format!("Failed updating workspace timestamp: {e}"))?;

        conn.query_row(
            "SELECT id, workspace_id, name, type, target, sort_order, created_at, updated_at FROM resource WHERE id = ?1",
            params![id],
            Self::map_resource_row,
        )
        .map_err(|e| format!("Failed reloading updated resource: {e}"))
    }

    pub fn remove_resource(&self, id: &str) -> Result<(), String> {
        let _guard = self.lock.lock().map_err(|e| e.to_string())?;
        let conn = self.open_connection()?;
        let deleted = conn
            .execute("DELETE FROM resource WHERE id = ?1", params![id])
            .map_err(|e| format!("Failed deleting resource: {e}"))?;
        if deleted == 0 {
            return Err(format!("Resource not found: {id}"));
        }
        Ok(())
    }

    pub fn reorder_resources(&self, workspace_id: &str, ordered_ids: &[String]) -> Result<(), String> {
        let _guard = self.lock.lock().map_err(|e| e.to_string())?;
        let mut conn = self.open_connection()?;
        let tx = conn
            .transaction()
            .map_err(|e| format!("Failed starting resource reorder: {e}"))?;
        let now = Utc::now().to_rfc3339();
        for (idx, id) in ordered_ids.iter().enumerate() {
            tx.execute(
                "UPDATE resource SET sort_order = ?1, updated_at = ?2 WHERE id = ?3 AND workspace_id = ?4",
                params![idx as i64, now, id, workspace_id],
            )
            .map_err(|e| format!("Failed reordering resource: {e}"))?;
        }
        tx.commit()
            .map_err(|e| format!("Failed committing resource reorder: {e}"))?;
        Ok(())
    }

    pub fn create_backup(&self) -> Result<PathBuf, String> {
        let _guard = self.lock.lock().map_err(|e| e.to_string())?;
        if !self.db_path.exists() {
            return Err("Database does not exist yet.".into());
        }
        std::fs::create_dir_all(&self.backups_dir)
            .map_err(|e| format!("Failed creating backup directory: {e}"))?;

        let stamp = Utc::now().format("%Y%m%d-%H%M%S-%3f");
        let backup_path = self.backups_dir.join(format!("adarabox-{stamp}.db"));
        let conn = self.open_connection()?;
        conn.execute("VACUUM INTO ?1", params![backup_path.to_string_lossy().as_ref()])
            .map_err(|e| format!("Failed creating SQLite backup: {e}"))?;

        // Retain at most MAX_BACKUPS newest snapshots.
        if let Ok(entries) = std::fs::read_dir(&self.backups_dir) {
            let mut files: Vec<PathBuf> = entries
                .filter_map(|e| e.ok().map(|entry| entry.path()))
                .filter(|p| {
                    p.is_file()
                        && p.file_name()
                            .and_then(|n| n.to_str())
                            .is_some_and(|n| n.starts_with("adarabox-") && n.ends_with(".db"))
                })
                .collect();
            files.sort();
            while files.len() > MAX_BACKUPS {
                let oldest = files.remove(0);
                let _ = std::fs::remove_file(oldest);
            }
        }

        Ok(backup_path)
    }

    fn cleanup_unused_custom_icon(&self, conn: &Connection, icon: &str) {
        let Some(rel) = icon.strip_prefix("custom:") else {
            return;
        };
        let in_use: i64 = conn
            .query_row(
                "SELECT COUNT(*) FROM workspace WHERE icon = ?1",
                params![icon],
                |r| r.get(0),
            )
            .unwrap_or(1);
        if in_use == 0 {
            let path = self.icons_dir.join(rel);
            if path.is_file() {
                let _ = std::fs::remove_file(path);
            }
        }
    }

    fn map_resource_row(row: &rusqlite::Row<'_>) -> rusqlite::Result<ResourceDto> {
        let type_str: String = row.get(3)?;
        let resource_type = type_str.parse::<ResourceType>().map_err(|err| {
            rusqlite::Error::FromSqlConversionFailure(
                3,
                rusqlite::types::Type::Text,
                Box::new(std::io::Error::new(std::io::ErrorKind::InvalidData, err)),
            )
        })?;
        Ok(ResourceDto {
            id: row.get(0)?,
            workspace_id: row.get(1)?,
            name: row.get(2)?,
            resource_type,
            target: row.get(4)?,
            sort_order: row.get(5)?,
            created_at: row.get(6)?,
            updated_at: row.get(7)?,
        })
    }
}

pub fn data_root_dir() -> PathBuf {
    if let Ok(local) = std::env::var("LOCALAPPDATA") {
        let trimmed = local.trim();
        if !trimmed.is_empty() {
            return PathBuf::from(trimmed).join("AdaraBox");
        }
    }
    if let Ok(roaming) = std::env::var("APPDATA") {
        let trimmed = roaming.trim();
        if !trimmed.is_empty() {
            return PathBuf::from(trimmed).join("AdaraBox");
        }
    }
    PathBuf::from("AdaraBox")
}

#[cfg(test)]
mod tests {
    use super::*;

    fn temp_store(tag: &str) -> (WorkspaceStore, PathBuf) {
        let root = std::env::temp_dir().join(format!("adara-ws-test-{tag}-{}", Uuid::new_v4()));
        let store = WorkspaceStore::for_paths(
            root.join("adarabox.db"),
            root.join("icons"),
            root.join("backups"),
        );
        (store, root)
    }

    #[test]
    fn initializes_seeds_and_upgrades_v1_schema_preserving_data() {
        let (store, root) = temp_store("v1-upgrade");
        std::fs::create_dir_all(&root).unwrap();

        // Simulate a legacy v1 adarabox.db missing workspace.sort_order/icon and resource timestamps.
        {
            let conn = Connection::open(store.db_path()).unwrap();
            conn.execute_batch(
                "
                CREATE TABLE workspace (
                    id TEXT PRIMARY KEY,
                    name TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                );
                CREATE TABLE resource (
                    id TEXT PRIMARY KEY,
                    workspace_id TEXT NOT NULL REFERENCES workspace(id) ON DELETE CASCADE,
                    name TEXT NOT NULL,
                    type TEXT NOT NULL CHECK (type IN ('application', 'file', 'folder', 'link')),
                    target TEXT NOT NULL
                );
                INSERT INTO workspace (id, name, created_at, updated_at)
                VALUES ('ws-legacy', 'Legacy', '2025-01-01T00:00:00Z', '2025-01-01T00:00:00Z');
                INSERT INTO resource (id, workspace_id, name, type, target)
                VALUES ('res-1', 'ws-legacy', 'Docs', 'link', 'https://rust-lang.org');
                PRAGMA user_version = 1;
                ",
            )
            .unwrap();
        }

        store.ensure_initialized().unwrap();
        let list = store.list_workspaces().unwrap();
        assert_eq!(list.len(), 1);
        assert_eq!(list[0].id, "ws-legacy");
        assert_eq!(list[0].icon, "📁");
        assert_eq!(list[0].resource_count, 1);

        let resources = store.list_resources("ws-legacy").unwrap();
        assert_eq!(resources.len(), 1);
        assert_eq!(resources[0].target, "https://rust-lang.org");

        let _ = std::fs::remove_dir_all(root);
    }

    #[test]
    fn crud_reorder_cascade_delete_and_rolling_backups() {
        let (store, root) = temp_store("crud");
        store.ensure_initialized().unwrap();

        let initial = store.list_workspaces().unwrap();
        assert_eq!(initial.len(), 1);
        assert_eq!(initial[0].name, "My Workspace");

        let study = store.create_workspace("Study", Some("📚")).unwrap();
        store.reorder_workspaces(&[study.id.clone(), initial[0].id.clone()]).unwrap();
        let ordered = store.list_workspaces().unwrap();
        assert_eq!(ordered[0].id, study.id);

        let r1 = store
            .add_resource(&study.id, "Docs", ResourceType::Link, "docs.rs")
            .unwrap();
        let r2 = store
            .add_resource(&study.id, "Temp", ResourceType::Folder, r"C:\Windows")
            .unwrap();
        assert_eq!(r1.target, "https://docs.rs");

        store.reorder_resources(&study.id, &[r2.id.clone(), r1.id.clone()]).unwrap();
        let res_ordered = store.list_resources(&study.id).unwrap();
        assert_eq!(res_ordered[0].id, r2.id);

        // Rolling max-5 VACUUM INTO backups.
        for _ in 0..7 {
            store.create_backup().unwrap();
        }
        let backup_count = std::fs::read_dir(root.join("backups")).unwrap().count();
        assert_eq!(backup_count, 5);

        // Deleting workspace cascades DB resource references only.
        store.delete_workspace(&study.id).unwrap();
        assert!(store.list_resources(&study.id).unwrap().is_empty());
        assert!(Path::new(r"C:\Windows").exists());

        let _ = std::fs::remove_dir_all(root);
    }
}
