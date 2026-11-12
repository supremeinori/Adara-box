pub mod ai_pane;
pub mod db;
pub mod models;

use tauri::{ipc::Response, AppHandle, State};
use tauri_plugin_dialog::DialogExt;

use crate::workspace::WorkspaceState;

use self::models::{BookProgressInput, LibraryBookDto, UpdateBookInput};

#[tauri::command]
pub fn open_library_window(app: AppHandle) {
    crate::on_demand_windows::open(&app, crate::on_demand_windows::LIBRARY);
}

#[tauri::command]
pub fn lib_list_books(state: State<'_, WorkspaceState>) -> Result<Vec<LibraryBookDto>, String> {
    db::list_books(&state.store)
}

#[tauri::command]
#[allow(clippy::too_many_arguments)]
pub async fn lib_add_book_from_path(
    state: State<'_, WorkspaceState>,
    title: String,
    author: String,
    category: String,
    status: String,
    notes: String,
    source_path: String,
    cover_data_url: Option<String>,
) -> Result<LibraryBookDto, String> {
    db::add_book_from_path(
        &state.store,
        &title,
        &author,
        &category,
        &status,
        &notes,
        &source_path,
        cover_data_url.as_deref(),
    )
}

#[tauri::command]
#[allow(clippy::too_many_arguments)]
pub async fn lib_add_book_bytes(
    state: State<'_, WorkspaceState>,
    title: String,
    author: String,
    category: String,
    status: String,
    notes: String,
    file_name: String,
    bytes: Vec<u8>,
    cover_data_url: Option<String>,
) -> Result<LibraryBookDto, String> {
    db::add_book_from_bytes(
        &state.store,
        &title,
        &author,
        &category,
        &status,
        &notes,
        &file_name,
        &bytes,
        cover_data_url.as_deref(),
    )
}

#[tauri::command]
pub fn lib_update_book(
    state: State<'_, WorkspaceState>,
    input: UpdateBookInput,
) -> Result<LibraryBookDto, String> {
    db::update_book(&state.store, &input)
}

#[tauri::command]
pub fn lib_save_progress(
    state: State<'_, WorkspaceState>,
    input: BookProgressInput,
) -> Result<LibraryBookDto, String> {
    db::save_progress(&state.store, &input)
}

#[tauri::command]
pub fn lib_delete_book(state: State<'_, WorkspaceState>, id: String) -> Result<(), String> {
    db::delete_book(&state.store, &id)
}

#[tauri::command]
pub fn lib_get_cover_data_url(
    state: State<'_, WorkspaceState>,
    id: String,
) -> Result<Option<String>, String> {
    db::get_cover_data_url(&state.store, &id)
}

#[tauri::command]
pub fn lib_get_book_bytes(state: State<'_, WorkspaceState>, id: String) -> Result<Response, String> {
    let bytes = db::read_book_bytes(&state.store, &id)?;
    Ok(Response::new(bytes))
}

#[tauri::command]
pub async fn lib_pick_book_file(app: AppHandle) -> Result<Option<(String, String, u64)>, String> {
    tauri::async_runtime::spawn_blocking(move || {
        let picked = app
            .dialog()
            .file()
            .set_title("Select Book (PDF or EPUB)")
            .add_filter("Books", &["pdf", "epub"])
            .blocking_pick_file();
        let Some(p) = picked else {
            return Ok(None);
        };
        let path = p.into_path().map_err(|e| e.to_string())?;
        let name = path
            .file_name()
            .and_then(|s| s.to_str())
            .unwrap_or("book")
            .to_string();
        let size = std::fs::metadata(&path).map(|m| m.len()).unwrap_or(0);
        Ok(Some((path.to_string_lossy().to_string(), name, size)))
    })
    .await
    .map_err(|e| format!("Book picker failed: {e}"))?
}

#[tauri::command]
pub async fn lib_pick_cover_image(app: AppHandle) -> Result<Option<String>, String> {
    tauri::async_runtime::spawn_blocking(move || {
        let picked = app
            .dialog()
            .file()
            .set_title("Choose Book Cover Image")
            .add_filter("Images", &["png", "jpg", "jpeg", "webp", "bmp"])
            .blocking_pick_file();
        let Some(p) = picked else {
            return Ok(None);
        };
        let path = p.into_path().map_err(|e| e.to_string())?;
        let bytes = std::fs::read(&path).map_err(|e| format!("Failed reading cover image: {e}"))?;
        let encoded = db::encode_cover_image_bytes(&bytes)?;
        Ok(Some(encoded))
    })
    .await
    .map_err(|e| format!("Cover picker failed: {e}"))?
}

#[tauri::command]
pub fn lib_ai_sync(
    app: AppHandle,
    visible: bool,
    x: i32,
    y: i32,
    width: i32,
    height: i32,
    url: Option<String>,
) -> Result<(), String> {
    ai_pane::sync_pane(&app, visible, x, y, width, height, url)
}

#[tauri::command]
pub fn lib_ai_reload(app: AppHandle) {
    ai_pane::reload_pane(&app);
}

#[tauri::command]
pub fn lib_ai_open_external(url: String) -> Result<(), String> {
    ai_pane::open_external(&url)
}
