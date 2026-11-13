pub mod db;
pub mod launcher;
pub mod models;
pub mod validator;

use std::sync::atomic::{AtomicBool, AtomicU64, Ordering};
use std::sync::Arc;
use std::time::Duration;

use tauri::{AppHandle, Emitter, Manager, State};
use tauri_plugin_dialog::DialogExt;
use uuid::Uuid;

use self::db::WorkspaceStore;
use self::models::{
    AvailabilityResult, BatchLaunchSummary, LaunchItemResult, LaunchProgressEvent,
    NewResourceInput, ResourceDto, ResourceType, WorkspaceDto,
};

const BATCH_PACE_MS: u64 = 150;

pub struct WorkspaceState {
    pub store: WorkspaceStore,
    batch_gen: AtomicU64,
    cancel_flag: std::sync::Mutex<Option<Arc<AtomicBool>>>,
}

impl WorkspaceState {
    pub fn new() -> Self {
        let store = WorkspaceStore::default_store();
        if let Err(err) = store.ensure_initialized() {
            eprintln!("[AdaraBox] Workspace DB init error: {err}");
        }
        Self {
            store,
            batch_gen: AtomicU64::new(0),
            cancel_flag: std::sync::Mutex::new(None),
        }
    }

    fn begin_batch(&self) -> (u64, Arc<AtomicBool>) {
        if let Ok(mut guard) = self.cancel_flag.lock() {
            if let Some(prev) = guard.take() {
                prev.store(true, Ordering::SeqCst);
            }
            let flag = Arc::new(AtomicBool::new(false));
            *guard = Some(Arc::clone(&flag));
            let gen = self.batch_gen.fetch_add(1, Ordering::SeqCst) + 1;
            (gen, flag)
        } else {
            let gen = self.batch_gen.fetch_add(1, Ordering::SeqCst) + 1;
            (gen, Arc::new(AtomicBool::new(false)))
        }
    }

    fn cancel_current_batch(&self) {
        if let Ok(guard) = self.cancel_flag.lock() {
            if let Some(flag) = guard.as_ref() {
                flag.store(true, Ordering::SeqCst);
            }
        }
    }
}

#[tauri::command]
pub fn ws_list(state: State<'_, WorkspaceState>) -> Result<Vec<WorkspaceDto>, String> {
    state.store.list_workspaces()
}

#[tauri::command]
pub fn ws_create(
    state: State<'_, WorkspaceState>,
    name: String,
    icon: Option<String>,
) -> Result<WorkspaceDto, String> {
    state.store.create_workspace(&name, icon.as_deref())
}

#[tauri::command]
pub fn ws_update(
    state: State<'_, WorkspaceState>,
    id: String,
    name: String,
    icon: Option<String>,
) -> Result<WorkspaceDto, String> {
    state.store.update_workspace(&id, &name, icon.as_deref())
}

#[tauri::command]
pub fn ws_reorder(state: State<'_, WorkspaceState>, ordered_ids: Vec<String>) -> Result<(), String> {
    state.store.reorder_workspaces(&ordered_ids)
}

#[tauri::command]
pub fn ws_delete(state: State<'_, WorkspaceState>, id: String) -> Result<(), String> {
    state.store.delete_workspace(&id)
}

#[tauri::command]
pub fn ws_create_backup(state: State<'_, WorkspaceState>) -> Result<String, String> {
    state
        .store
        .create_backup()
        .map(|p| p.to_string_lossy().to_string())
}

#[tauri::command]
pub async fn ws_pick_icon_image(
    app: AppHandle,
    state: State<'_, WorkspaceState>,
) -> Result<Option<String>, String> {
    let icons_dir = state.store.icons_dir().to_path_buf();
    tauri::async_runtime::spawn_blocking(move || {
        let mut builder = app
            .dialog()
            .file()
            .set_title("Choose Workspace Icon")
            .add_filter("Images", &["png", "jpg", "jpeg", "webp", "ico", "bmp"]);
        if let Some(w) = app.get_webview_window("workspace").or_else(|| app.get_webview_window("main")) {
            builder = builder.set_parent(&w);
        }
        let picked = builder.blocking_pick_file();
        let Some(file_path) = picked else {
            return Ok(None);
        };
        let src = file_path.into_path().map_err(|e| e.to_string())?;

        std::fs::create_dir_all(&icons_dir)
            .map_err(|e| format!("Failed creating icons directory: {e}"))?;

        let bytes = std::fs::read(&src).map_err(|e| format!("Failed reading icon image: {e}"))?;
        if bytes.len() > 8 * 1024 * 1024 {
            return Err("Icon image must be smaller than 8 MB.".into());
        }

        let img = image::load_from_memory(&bytes)
            .map_err(|e| format!("Unsupported or corrupt image file: {e}"))?;
        let resized = img.thumbnail(96, 96);
        let filename = format!("ws-{}.png", Uuid::new_v4());
        let dest = icons_dir.join(&filename);
        resized
            .save_with_format(&dest, image::ImageFormat::Png)
            .map_err(|e| format!("Failed saving workspace icon: {e}"))?;

        Ok(Some(format!("custom:{filename}")))
    })
    .await
    .map_err(|e| format!("Icon picker task failed: {e}"))?
}

#[tauri::command]
pub fn ws_get_icon_data_url(
    state: State<'_, WorkspaceState>,
    icon: String,
) -> Result<Option<String>, String> {
    let clean = validator::validate_workspace_icon(Some(&icon))?;
    if let Some(filename) = clean.strip_prefix("custom:") {
        let path = state.store.icons_dir().join(filename);
        if !path.is_file() {
            return Ok(None);
        }
        return Ok(crate::utils::image_file_to_base64(&path.to_string_lossy()));
    }

    // Legacy AdaraBox (C#) databases store direct filesystem paths for workspace cover art.
    let path = std::path::Path::new(&clean);
    if path.is_file() {
        let is_image = path
            .extension()
            .and_then(|e| e.to_str())
            .is_some_and(|ext| {
                matches!(
                    ext.to_ascii_lowercase().as_str(),
                    "png" | "jpg" | "jpeg" | "webp" | "ico" | "bmp" | "gif"
                )
            });
        if is_image {
            return Ok(crate::utils::image_file_to_base64(&clean));
        }
    }

    Ok(None)
}

#[tauri::command]
pub fn res_list(
    state: State<'_, WorkspaceState>,
    workspace_id: String,
) -> Result<Vec<ResourceDto>, String> {
    state.store.list_resources(&workspace_id)
}

#[tauri::command]
pub fn res_add(
    state: State<'_, WorkspaceState>,
    workspace_id: String,
    name: String,
    #[allow(non_snake_case)] resourceType: ResourceType,
    target: String,
) -> Result<ResourceDto, String> {
    state
        .store
        .add_resource(&workspace_id, &name, resourceType, &target)
}

#[tauri::command]
pub fn res_add_many(
    state: State<'_, WorkspaceState>,
    workspace_id: String,
    items: Vec<NewResourceInput>,
) -> Result<Vec<ResourceDto>, String> {
    state.store.add_resources_bulk(&workspace_id, &items)
}

#[tauri::command]
pub fn res_update(
    state: State<'_, WorkspaceState>,
    id: String,
    name: String,
    #[allow(non_snake_case)] resourceType: ResourceType,
    target: String,
) -> Result<ResourceDto, String> {
    state
        .store
        .update_resource(&id, &name, resourceType, &target)
}

#[tauri::command]
pub fn res_remove(state: State<'_, WorkspaceState>, id: String) -> Result<(), String> {
    state.store.remove_resource(&id)
}

#[tauri::command]
pub fn res_reorder(
    state: State<'_, WorkspaceState>,
    workspace_id: String,
    ordered_ids: Vec<String>,
) -> Result<(), String> {
    state.store.reorder_resources(&workspace_id, &ordered_ids)
}

#[tauri::command]
pub async fn res_check_availability(
    state: State<'_, WorkspaceState>,
    workspace_id: String,
) -> Result<Vec<AvailabilityResult>, String> {
    let resources = state.store.list_resources(&workspace_id)?;
    tauri::async_runtime::spawn_blocking(move || {
        resources
            .iter()
            .map(launcher::check_availability)
            .collect::<Vec<_>>()
    })
    .await
    .map_err(|e| format!("Availability check failed: {e}"))
}

#[tauri::command]
pub async fn res_get_icon(
    #[allow(non_snake_case)] resourceType: ResourceType,
    target: String,
) -> Result<Option<String>, String> {
    tauri::async_runtime::spawn_blocking(move || launcher::extract_resource_icon(resourceType, &target))
        .await
        .map_err(|e| format!("Icon extraction failed: {e}"))
}

#[tauri::command]
pub async fn res_launch_single(
    state: State<'_, WorkspaceState>,
    id: String,
) -> Result<LaunchItemResult, String> {
    let resource = state.store.get_resource(&id)?;
    tauri::async_runtime::spawn_blocking(move || launcher::launch_single(&resource))
        .await
        .map_err(|e| format!("Launch task failed: {e}"))
}

#[tauri::command]
pub async fn res_launch_batch(
    app: AppHandle,
    state: State<'_, WorkspaceState>,
    workspace_id: String,
    resource_ids: Option<Vec<String>>,
) -> Result<BatchLaunchSummary, String> {
    let all = state.store.list_resources(&workspace_id)?;
    let queue: Vec<ResourceDto> = match resource_ids {
        Some(filter) if !filter.is_empty() => filter
            .iter()
            .filter_map(|id| all.iter().find(|r| &r.id == id).cloned())
            .collect(),
        _ => all,
    };

    let (_gen, cancel_flag) = state.begin_batch();
    let total = queue.len();
    let mut results = Vec::with_capacity(total);
    let mut launched = 0usize;
    let mut failed = 0usize;
    let mut cancelled = false;

    for (idx, item) in queue.into_iter().enumerate() {
        if cancel_flag.load(Ordering::SeqCst) {
            cancelled = true;
            break;
        }

        let _ = app.emit(
            "workspace-launch-progress",
            LaunchProgressEvent {
                workspace_id: workspace_id.clone(),
                completed: idx,
                total,
                current_name: item.name.clone(),
                cancelled: false,
            },
        );

        let name = item.name.clone();
        let res = tauri::async_runtime::spawn_blocking(move || launcher::launch_single(&item))
            .await
            .map_err(|e| format!("Batch launch failed: {e}"))?;

        if res.success {
            launched += 1;
        } else {
            failed += 1;
        }
        results.push(res);

        let _ = app.emit(
            "workspace-launch-progress",
            LaunchProgressEvent {
                workspace_id: workspace_id.clone(),
                completed: idx + 1,
                total,
                current_name: name,
                cancelled: false,
            },
        );

        if idx + 1 < total {
            let mut waited = 0u64;
            while waited < BATCH_PACE_MS {
                if cancel_flag.load(Ordering::SeqCst) {
                    cancelled = true;
                    break;
                }
                let step = 25u64.min(BATCH_PACE_MS - waited);
                tokio::time::sleep(Duration::from_millis(step)).await;
                waited += step;
            }
            if cancelled {
                break;
            }
        }
    }

    if cancelled {
        let _ = app.emit(
            "workspace-launch-progress",
            LaunchProgressEvent {
                workspace_id: workspace_id.clone(),
                completed: results.len(),
                total,
                current_name: String::new(),
                cancelled: true,
            },
        );
    }

    Ok(BatchLaunchSummary {
        workspace_id,
        total,
        launched,
        failed,
        cancelled,
        results,
    })
}

#[tauri::command]
pub fn ws_cancel_launch(state: State<'_, WorkspaceState>) {
    state.cancel_current_batch();
}

#[tauri::command]
pub fn open_workspace_window(app: AppHandle) {
    crate::on_demand_windows::open(&app, crate::on_demand_windows::WORKSPACE);
}

#[tauri::command]
pub async fn res_pick_file(app: AppHandle) -> Result<Option<String>, String> {
    tauri::async_runtime::spawn_blocking(move || {
        let mut builder = app
            .dialog()
            .file()
            .set_title("Select File or Application");
        if let Some(w) = app.get_webview_window("workspace").or_else(|| app.get_webview_window("main")) {
            builder = builder.set_parent(&w);
        }
        let picked = builder.blocking_pick_file();
        match picked {
            Some(p) => Ok(Some(
                p.into_path()
                    .map_err(|e| e.to_string())?
                    .to_string_lossy()
                    .to_string(),
            )),
            None => Ok(None),
        }
    })
    .await
    .map_err(|e| format!("File picker failed: {e}"))?
}

#[tauri::command]
pub async fn res_pick_folder(app: AppHandle) -> Result<Option<String>, String> {
    tauri::async_runtime::spawn_blocking(move || {
        let mut builder = app
            .dialog()
            .file()
            .set_title("Select Folder");
        if let Some(w) = app.get_webview_window("workspace").or_else(|| app.get_webview_window("main")) {
            builder = builder.set_parent(&w);
        }
        let picked = builder.blocking_pick_folder();
        match picked {
            Some(p) => Ok(Some(
                p.into_path()
                    .map_err(|e| e.to_string())?
                    .to_string_lossy()
                    .to_string(),
            )),
            None => Ok(None),
        }
    })
    .await
    .map_err(|e| format!("Folder picker failed: {e}"))?
}
