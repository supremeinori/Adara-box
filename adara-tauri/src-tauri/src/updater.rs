use serde::Serialize;
use tauri::{AppHandle, Emitter};

#[derive(Clone, Default, Serialize)]
pub struct UpdateCheckResult {
    pub available: bool,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub version: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub date: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub body: Option<String>,
}

pub async fn run_startup_check(app: AppHandle) {
    let result = UpdateCheckResult::default();
    let _ = app.emit("update-available", &result);
    if crate::utils::get_setting_str(&app, "nectar-auto-update").as_deref() == Some("true") {
        let _ = app.emit("auto-update-status", serde_json::json!({ "status": "done" }));
    }
}

#[tauri::command]
pub async fn check_for_updates(app: AppHandle, _force: bool) -> Result<UpdateCheckResult, String> {
    let result = UpdateCheckResult::default();
    let _ = app.emit("update-available", &result);
    Ok(result)
}

#[tauri::command]
pub async fn install_update(app: AppHandle) -> Result<(), String> {
    let _ = app.emit("auto-update-status", serde_json::json!({ "status": "done" }));
    Err("Auto-updates are not configured for this build of Adara Box.".to_string())
}

#[tauri::command]
pub fn get_update_state(_app: AppHandle) -> UpdateCheckResult {
    UpdateCheckResult::default()
}
