use std::path::{Path, PathBuf};

use base64::{engine::general_purpose::STANDARD as BASE64, Engine as _};
use chrono::Utc;
use rusqlite::{params, OptionalExtension, Row};
use uuid::Uuid;

use crate::workspace::db::WorkspaceStore;

use super::models::{BookFileType, BookProgressInput, LibraryBookDto, UpdateBookInput};

const VALID_CATEGORIES: &[&str] = &[
    "Any",
    "Psychology",
    "Philosophy",
    "Literature",
    "Cinema",
    "Technology",
    "Other",
];

const VALID_STATUSES: &[&str] = &["New", "Want to Read", "Reading", "Finished"];
const MAX_BOOK_BYTES: u64 = 512 * 1024 * 1024;
const MAX_COVER_BYTES: usize = 8 * 1024 * 1024;

fn books_dir(store: &WorkspaceStore) -> PathBuf {
    store.data_dir().join("library").join("books")
}

fn covers_dir(store: &WorkspaceStore) -> PathBuf {
    store.data_dir().join("library").join("covers")
}

fn validate_title(raw: &str) -> Result<String, String> {
    let trimmed = raw.trim();
    if trimmed.is_empty() {
        return Err("Book title cannot be blank.".into());
    }
    if trimmed.chars().count() > 200 {
        return Err("Book title must be 200 characters or fewer.".into());
    }
    Ok(trimmed.to_string())
}

fn normalize_author(raw: &str) -> String {
    let trimmed = raw.trim();
    if trimmed.is_empty() {
        "Unknown Author".into()
    } else {
        trimmed.chars().take(120).collect()
    }
}

fn normalize_category(raw: &str) -> String {
    let trimmed = raw.trim();
    VALID_CATEGORIES
        .iter()
        .find(|&&c| c.eq_ignore_ascii_case(trimmed))
        .map(|&c| c.to_string())
        .unwrap_or_else(|| "Any".into())
}

fn normalize_status(raw: &str) -> String {
    let trimmed = raw.trim();
    VALID_STATUSES
        .iter()
        .find(|&&s| s.eq_ignore_ascii_case(trimmed))
        .map(|&s| s.to_string())
        .unwrap_or_else(|| "New".into())
}

fn normalize_notes(raw: &str) -> String {
    raw.trim().chars().take(2000).collect()
}

fn decode_and_save_cover(cover_data_url: &str, dest_path: &Path) -> Result<(), String> {
    let bytes = if let Some(b64_part) = cover_data_url.split(";base64,").nth(1) {
        if b64_part.len() > MAX_COVER_BYTES * 2 {
            return Err("Cover image is too large (max 8 MB).".into());
        }
        BASE64
            .decode(b64_part.trim())
            .map_err(|e| format!("Invalid base64 cover image: {e}"))?
    } else {
        return Err("Cover image must be a base64 data URL.".into());
    };

    if bytes.len() > MAX_COVER_BYTES {
        return Err("Cover image must be smaller than 8 MB.".into());
    }
    save_cover_bytes(&bytes, dest_path)
}

pub fn save_cover_bytes(bytes: &[u8], dest_path: &Path) -> Result<(), String> {
    if bytes.len() > MAX_COVER_BYTES {
        return Err("Cover image must be smaller than 8 MB.".into());
    }
    if let Some(parent) = dest_path.parent() {
        std::fs::create_dir_all(parent)
            .map_err(|e| format!("Failed creating covers directory: {e}"))?;
    }
    let img = image::load_from_memory(bytes)
        .map_err(|e| format!("Unsupported or corrupt cover image: {e}"))?;
    let resized = img.thumbnail(480, 680);
    resized
        .save_with_format(dest_path, image::ImageFormat::Png)
        .map_err(|e| format!("Failed saving cover image: {e}"))
}

pub fn encode_cover_image_bytes(bytes: &[u8]) -> Result<String, String> {
    if bytes.len() > MAX_COVER_BYTES {
        return Err("Cover image must be smaller than 8 MB.".into());
    }
    let img = image::load_from_memory(bytes)
        .map_err(|e| format!("Unsupported or corrupt cover image: {e}"))?;
    let resized = img.thumbnail(480, 680);
    let mut cursor = std::io::Cursor::new(Vec::new());
    resized
        .write_to(&mut cursor, image::ImageFormat::Png)
        .map_err(|e| format!("Failed encoding cover thumbnail: {e}"))?;
    Ok(format!("data:image/png;base64,{}", BASE64.encode(cursor.into_inner())))
}

fn resolve_managed_path(base_dir: &Path, stored: &str) -> Result<PathBuf, String> {
    let p = Path::new(stored);
    if p.is_absolute() {
        return Ok(p.to_path_buf());
    }
    let file_name = p
        .file_name()
        .ok_or_else(|| "Invalid stored file name.".to_string())?;
    Ok(base_dir.join(file_name))
}

fn row_to_book(row: &Row<'_>) -> rusqlite::Result<LibraryBookDto> {
    let type_str: String = row.get(6)?;
    let file_type = BookFileType::from_ext(&type_str).unwrap_or(BookFileType::Pdf);
    let cover_path: Option<String> = row.get(9)?;
    Ok(LibraryBookDto {
        id: row.get(0)?,
        title: row.get(1)?,
        author: row.get(2)?,
        category: row.get(3)?,
        status: row.get(4)?,
        notes: row.get(5)?,
        file_type,
        file_name: row.get(7)?,
        file_size: row.get(8)?,
        has_cover: cover_path.as_deref().is_some_and(|s| !s.trim().is_empty()),
        current_page: row.get(10)?,
        total_pages: row.get(11)?,
        current_cfi: row.get(12)?,
        reading_progress: row.get(13)?,
        last_read_at: row.get(14)?,
        created_at: row.get(15)?,
        updated_at: row.get(16)?,
    })
}

const SELECT_BOOK_COLS: &str = "
    SELECT id, title, author, category, status, notes, file_type,
           file_name, file_size, cover_path, current_page, total_pages,
           current_cfi, reading_progress, last_read_at, created_at, updated_at
    FROM library_books
";

pub fn list_books(store: &WorkspaceStore) -> Result<Vec<LibraryBookDto>, String> {
    store.with_connection(|conn| {
        let mut stmt = conn
            .prepare(&format!(
                "{SELECT_BOOK_COLS} ORDER BY COALESCE(last_read_at, updated_at, created_at) DESC"
            ))
            .map_err(|e| format!("Failed preparing library query: {e}"))?;
        let rows = stmt
            .query_map([], row_to_book)
            .map_err(|e| format!("Failed querying books: {e}"))?;
        let mut out = Vec::new();
        for item in rows {
            out.push(item.map_err(|e| format!("Failed reading book row: {e}"))?);
        }
        Ok(out)
    })
}

pub fn get_book(store: &WorkspaceStore, id: &str) -> Result<LibraryBookDto, String> {
    store.with_connection(|conn| {
        conn.query_row(
            &format!("{SELECT_BOOK_COLS} WHERE id = ?1"),
            params![id],
            row_to_book,
        )
        .optional()
        .map_err(|e| format!("Failed fetching book: {e}"))?
        .ok_or_else(|| "Book not found.".into())
    })
}

pub fn add_book_from_path(
    store: &WorkspaceStore,
    title: &str,
    author: &str,
    category: &str,
    status: &str,
    notes: &str,
    source_path: &str,
    cover_data_url: Option<&str>,
) -> Result<LibraryBookDto, String> {
    let src = Path::new(source_path.trim());
    if !src.is_file() {
        return Err("Selected book file does not exist.".into());
    }
    let meta = std::fs::metadata(src).map_err(|e| format!("Cannot read book file metadata: {e}"))?;
    if meta.len() == 0 {
        return Err("Selected file is empty.".into());
    }
    if meta.len() > MAX_BOOK_BYTES {
        return Err("Book file exceeds maximum supported size (512 MB).".into());
    }

    let ext = src
        .extension()
        .and_then(|s| s.to_str())
        .ok_or_else(|| "File has no extension (.pdf or .epub).".to_string())?;
    let file_type = BookFileType::from_ext(ext)?;
    let file_name = src
        .file_name()
        .and_then(|s| s.to_str())
        .unwrap_or("book")
        .to_string();

    let bytes_len = meta.len() as i64;
    let id = Uuid::new_v4().to_string();
    let stored_book_name = format!("{id}.{}", file_type.as_str());
    let dest_book_path = books_dir(store).join(&stored_book_name);
    std::fs::create_dir_all(books_dir(store))
        .map_err(|e| format!("Failed creating library books folder: {e}"))?;
    std::fs::copy(src, &dest_book_path)
        .map_err(|e| format!("Failed copying book into library storage: {e}"))?;

    insert_book_record(
        store,
        id,
        title,
        author,
        category,
        status,
        notes,
        file_type,
        file_name,
        bytes_len,
        stored_book_name,
        dest_book_path,
        cover_data_url,
    )
}

pub fn add_book_from_bytes(
    store: &WorkspaceStore,
    title: &str,
    author: &str,
    category: &str,
    status: &str,
    notes: &str,
    file_name: &str,
    bytes: &[u8],
    cover_data_url: Option<&str>,
) -> Result<LibraryBookDto, String> {
    if bytes.is_empty() {
        return Err("Uploaded book file is empty.".into());
    }
    if (bytes.len() as u64) > MAX_BOOK_BYTES {
        return Err("Book file exceeds maximum supported size (512 MB).".into());
    }
    let clean_file_name = Path::new(file_name)
        .file_name()
        .and_then(|s| s.to_str())
        .unwrap_or("book.pdf")
        .to_string();
    let ext = Path::new(&clean_file_name)
        .extension()
        .and_then(|s| s.to_str())
        .ok_or_else(|| "File has no extension (.pdf or .epub).".to_string())?;
    let file_type = BookFileType::from_ext(ext)?;

    let id = Uuid::new_v4().to_string();
    let stored_book_name = format!("{id}.{}", file_type.as_str());
    let dest_book_path = books_dir(store).join(&stored_book_name);
    std::fs::create_dir_all(books_dir(store))
        .map_err(|e| format!("Failed creating library books folder: {e}"))?;
    std::fs::write(&dest_book_path, bytes)
        .map_err(|e| format!("Failed saving book into library storage: {e}"))?;

    insert_book_record(
        store,
        id,
        title,
        author,
        category,
        status,
        notes,
        file_type,
        clean_file_name,
        bytes.len() as i64,
        stored_book_name,
        dest_book_path,
        cover_data_url,
    )
}

#[allow(clippy::too_many_arguments)]
fn insert_book_record(
    store: &WorkspaceStore,
    id: String,
    title: &str,
    author: &str,
    category: &str,
    status: &str,
    notes: &str,
    file_type: BookFileType,
    file_name: String,
    file_size: i64,
    stored_book_name: String,
    dest_book_path: PathBuf,
    cover_data_url: Option<&str>,
) -> Result<LibraryBookDto, String> {
    let clean_title = match validate_title(title) {
        Ok(v) => v,
        Err(e) => {
            let _ = std::fs::remove_file(&dest_book_path);
            return Err(e);
        }
    };
    let clean_author = normalize_author(author);
    let clean_category = normalize_category(category);
    let clean_status = normalize_status(status);
    let clean_notes = normalize_notes(notes);

    let mut stored_cover_name: Option<String> = None;
    let mut dest_cover_path: Option<PathBuf> = None;
    if let Some(data_url) = cover_data_url.map(str::trim).filter(|s| !s.is_empty()) {
        let cname = format!("{id}.png");
        let cpath = covers_dir(store).join(&cname);
        if let Err(err) = decode_and_save_cover(data_url, &cpath) {
            let _ = std::fs::remove_file(&dest_book_path);
            return Err(err);
        }
        stored_cover_name = Some(cname);
        dest_cover_path = Some(cpath);
    }

    let now = Utc::now().to_rfc3339();
    let db_result = store.with_connection(|conn| {
        conn.execute(
            "
            INSERT INTO library_books (
                id, title, author, category, status, notes, file_type,
                file_name, file_size, file_path, cover_path,
                current_page, total_pages, current_cfi, reading_progress,
                last_read_at, created_at, updated_at
            ) VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, 1, 0, NULL, 0.0, NULL, ?12, ?12)
            ",
            params![
                id,
                clean_title,
                clean_author,
                clean_category,
                clean_status,
                clean_notes,
                file_type.as_str(),
                file_name,
                file_size,
                stored_book_name,
                stored_cover_name,
                now
            ],
        )
        .map_err(|e| format!("Failed inserting book record: {e}"))?;
        Ok(())
    });

    if let Err(err) = db_result {
        let _ = std::fs::remove_file(&dest_book_path);
        if let Some(cpath) = dest_cover_path {
            let _ = std::fs::remove_file(cpath);
        }
        return Err(err);
    }

    Ok(LibraryBookDto {
        id,
        title: clean_title,
        author: clean_author,
        category: clean_category,
        status: clean_status,
        notes: clean_notes,
        file_type,
        file_name,
        file_size,
        has_cover: stored_cover_name.is_some(),
        current_page: 1,
        total_pages: 0,
        current_cfi: None,
        reading_progress: 0.0,
        last_read_at: None,
        created_at: now.clone(),
        updated_at: now,
    })
}

pub fn update_book(store: &WorkspaceStore, input: &UpdateBookInput) -> Result<LibraryBookDto, String> {
    let clean_title = validate_title(&input.title)?;
    let clean_author = normalize_author(&input.author);
    let clean_category = normalize_category(&input.category);
    let clean_status = normalize_status(&input.status);
    let clean_notes = normalize_notes(&input.notes);
    let now = Utc::now().to_rfc3339();

    let existing_cover: Option<String> = store.with_connection(|conn| {
        conn.query_row(
            "SELECT cover_path FROM library_books WHERE id = ?1",
            params![input.id],
            |r| r.get(0),
        )
        .optional()
        .map_err(|e| format!("Failed querying existing book: {e}"))?
        .ok_or_else(|| "Book not found.".into())
    })?;

    let mut next_cover = existing_cover.clone();
    if input.clear_cover.unwrap_or(false) {
        if let Some(prev) = &existing_cover {
            if let Ok(path) = resolve_managed_path(&covers_dir(store), prev) {
                let _ = std::fs::remove_file(path);
            }
        }
        next_cover = None;
    } else if let Some(data_url) = input.cover_data_url.as_deref().map(str::trim).filter(|s| !s.is_empty()) {
        let cname = format!("{}.png", input.id);
        let cpath = covers_dir(store).join(&cname);
        decode_and_save_cover(data_url, &cpath)?;
        next_cover = Some(cname);
    }

    store.with_connection(|conn| {
        conn.execute(
            "
            UPDATE library_books
            SET title = ?2, author = ?3, category = ?4, status = ?5,
                notes = ?6, cover_path = ?7, updated_at = ?8
            WHERE id = ?1
            ",
            params![
                input.id,
                clean_title,
                clean_author,
                clean_category,
                clean_status,
                clean_notes,
                next_cover,
                now
            ],
        )
        .map_err(|e| format!("Failed updating book: {e}"))?;
        Ok(())
    })?;

    get_book(store, &input.id)
}

pub fn save_progress(store: &WorkspaceStore, input: &BookProgressInput) -> Result<LibraryBookDto, String> {
    let now = Utc::now().to_rfc3339();
    store.with_connection(|conn| {
        let (cur_page, cur_total, cur_cfi, cur_prog, cur_status): (i64, i64, Option<String>, f64, String) = conn
            .query_row(
                "SELECT current_page, total_pages, current_cfi, reading_progress, status FROM library_books WHERE id = ?1",
                params![input.id],
                |r| Ok((r.get(0)?, r.get(1)?, r.get(2)?, r.get(3)?, r.get(4)?)),
            )
            .optional()
            .map_err(|e| format!("Failed loading book progress: {e}"))?
            .ok_or_else(|| "Book not found.".to_string())?;

        let next_page = input.current_page.unwrap_or(cur_page).max(1);
        let next_total = input.total_pages.unwrap_or(cur_total).max(0);
        let next_cfi = input.current_cfi.clone().or(cur_cfi);
        let next_prog = input
            .reading_progress
            .unwrap_or(cur_prog)
            .clamp(0.0, 100.0);

        let next_status = if let Some(explicit) = &input.status {
            normalize_status(explicit)
        } else if next_prog >= 99.5 && cur_status != "Finished" {
            "Finished".to_string()
        } else if next_prog > 0.0 && (cur_status == "New" || cur_status == "Want to Read") {
            "Reading".to_string()
        } else {
            cur_status
        };

        conn.execute(
            "
            UPDATE library_books
            SET current_page = ?2, total_pages = ?3, current_cfi = ?4,
                reading_progress = ?5, status = ?6, last_read_at = ?7, updated_at = ?7
            WHERE id = ?1
            ",
            params![input.id, next_page, next_total, next_cfi, next_prog, next_status, now],
        )
        .map_err(|e| format!("Failed saving reading progress: {e}"))?;
        Ok(())
    })?;

    get_book(store, &input.id)
}

pub fn delete_book(store: &WorkspaceStore, id: &str) -> Result<(), String> {
    let paths: Option<(String, Option<String>)> = store.with_connection(|conn| {
        let info = conn
            .query_row(
                "SELECT file_path, cover_path FROM library_books WHERE id = ?1",
                params![id],
                |r| Ok((r.get(0)?, r.get(1)?)),
            )
            .optional()
            .map_err(|e| format!("Failed inspecting book before delete: {e}"))?;
        conn.execute("DELETE FROM library_books WHERE id = ?1", params![id])
            .map_err(|e| format!("Failed deleting book: {e}"))?;
        Ok(info)
    })?;

    if let Some((file_stored, cover_stored)) = paths {
        let bdir = books_dir(store);
        if let Ok(path) = resolve_managed_path(&bdir, &file_stored) {
            if path.starts_with(&bdir) {
                let _ = std::fs::remove_file(path);
            }
        }
        if let Some(cov) = cover_stored {
            let cdir = covers_dir(store);
            if let Ok(path) = resolve_managed_path(&cdir, &cov) {
                if path.starts_with(&cdir) {
                    let _ = std::fs::remove_file(path);
                }
            }
        }
    }
    Ok(())
}

pub fn read_book_bytes(store: &WorkspaceStore, id: &str) -> Result<Vec<u8>, String> {
    let file_stored: String = store.with_connection(|conn| {
        conn.query_row(
            "SELECT file_path FROM library_books WHERE id = ?1",
            params![id],
            |r| r.get(0),
        )
        .optional()
        .map_err(|e| format!("Failed resolving book path: {e}"))?
        .ok_or_else(|| "Book not found.".into())
    })?;

    let path = resolve_managed_path(&books_dir(store), &file_stored)?;
    std::fs::read(&path).map_err(|e| format!("Failed reading stored book file: {e}"))
}

pub fn get_cover_data_url(store: &WorkspaceStore, id: &str) -> Result<Option<String>, String> {
    let cover_stored: Option<String> = store.with_connection(|conn| {
        let res = conn
            .query_row(
                "SELECT cover_path FROM library_books WHERE id = ?1",
                params![id],
                |r| r.get::<_, Option<String>>(0),
            )
            .optional()
            .map_err(|e| format!("Failed looking up cover image: {e}"))?;
        Ok(res.flatten())
    })?;

    let Some(stored) = cover_stored.filter(|s| !s.trim().is_empty()) else {
        return Ok(None);
    };
    let path = resolve_managed_path(&covers_dir(store), &stored)?;
    if !path.is_file() {
        return Ok(None);
    }
    Ok(crate::utils::image_file_to_base64(&path.to_string_lossy()))
}

#[cfg(test)]
mod tests {
    use super::*;

    fn temp_store(name: &str) -> (WorkspaceStore, PathBuf) {
        let dir = std::env::temp_dir().join(format!("adarabox_lib_test_{}_{}", name, Uuid::new_v4()));
        let store = WorkspaceStore::for_paths(
            dir.join("adarabox.db"),
            dir.join("icons"),
            dir.join("backups"),
        );
        store.ensure_initialized().expect("init schema");
        (store, dir)
    }

    #[test]
    fn add_update_progress_and_delete_book_lifecycle() {
        let (store, dir) = temp_store("lifecycle");
        let src_pdf = dir.join("sample.pdf");
        std::fs::create_dir_all(&dir).unwrap();
        std::fs::write(&src_pdf, b"%PDF-1.4\n%test-pdf-content\n").unwrap();

        let mut png_bytes = Vec::new();
        image::DynamicImage::new_rgba8(2, 2)
            .write_to(&mut std::io::Cursor::new(&mut png_bytes), image::ImageFormat::Png)
            .unwrap();
        let tiny_png = encode_cover_image_bytes(&png_bytes).unwrap();
        let created = add_book_from_path(
            &store,
            "Clean Code",
            "Robert Martin",
            "Technology",
            "New",
            "Notes here",
            &src_pdf.to_string_lossy(),
            Some(&tiny_png),
        )
        .expect("add pdf book");

        let created_epub = add_book_from_bytes(
            &store,
            "Meditations",
            "Marcus Aurelius",
            "Philosophy",
            "New",
            "",
            "meditations.epub",
            b"PK\x03\x04dummy-epub-zip-bytes",
            None,
        )
        .expect("add epub book");
        assert_eq!(created_epub.file_type, BookFileType::Epub);
        assert_eq!(
            read_book_bytes(&store, &created_epub.id).unwrap(),
            b"PK\x03\x04dummy-epub-zip-bytes"
        );
        delete_book(&store, &created_epub.id).expect("delete epub book");

        assert!(created.has_cover);
        assert_eq!(created.file_type, BookFileType::Pdf);
        assert!(get_cover_data_url(&store, &created.id).unwrap().is_some());
        assert_eq!(
            read_book_bytes(&store, &created.id).unwrap(),
            b"%PDF-1.4\n%test-pdf-content\n"
        );

        let progressed = save_progress(
            &store,
            &BookProgressInput {
                id: created.id.clone(),
                current_page: Some(12),
                total_pages: Some(100),
                current_cfi: None,
                reading_progress: Some(12.0),
                status: None,
            },
        )
        .expect("save progress");
        assert_eq!(progressed.status, "Reading");
        assert_eq!(progressed.current_page, 12);

        let updated = update_book(
            &store,
            &UpdateBookInput {
                id: created.id.clone(),
                title: "Clean Code Revised".into(),
                author: "".into(),
                category: "Philosophy".into(),
                status: "Want to Read".into(),
                notes: "Updated".into(),
                cover_data_url: None,
                clear_cover: Some(true),
            },
        )
        .expect("update book");
        assert!(!updated.has_cover);
        assert_eq!(updated.author, "Unknown Author");

        delete_book(&store, &created.id).expect("delete book");
        assert!(list_books(&store).unwrap().is_empty());
        // Original user source file must never be removed when book is deleted from library.
        assert!(src_pdf.exists());

        let _ = std::fs::remove_dir_all(&dir);
    }
}
