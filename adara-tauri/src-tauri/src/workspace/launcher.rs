use std::collections::HashMap;
use std::path::Path;
use std::sync::{Mutex, OnceLock};

use windows::Win32::Foundation::{GetLastError, WIN32_ERROR};
use windows::Win32::System::Com::{CoInitializeEx, CoUninitialize, COINIT_APARTMENTTHREADED};
use windows::Win32::UI::Shell::{SHGetFileInfoW, SHFILEINFOW, SHGFI_ICON, SHGFI_LARGEICON, SHGFI_USEFILEATTRIBUTES};
use windows::Win32::UI::WindowsAndMessaging::{DestroyIcon, SW_SHOWNORMAL};
use windows::Win32::UI::Shell::ShellExecuteW;

use super::models::{AvailabilityResult, LaunchItemResult, ResourceDto, ResourceType};
use super::validator;

static RESOURCE_ICON_CACHE: OnceLock<Mutex<HashMap<String, Option<String>>>> = OnceLock::new();

fn expand_env_path(raw: &str) -> String {
    crate::utils::expand_env_vars(raw)
}

pub fn check_availability(resource: &ResourceDto) -> AvailabilityResult {
    let expanded = expand_env_path(&resource.target);
    let (available, reason) = match resource.resource_type {
        ResourceType::Link => match validator::normalize_link(&expanded) {
            Ok(_) => (true, None),
            Err(err) => (false, Some(err)),
        },
        ResourceType::Folder => {
            if Path::new(&expanded).is_dir() {
                (true, None)
            } else {
                (false, Some("Folder does not exist on disk.".into()))
            }
        }
        ResourceType::File => {
            if Path::new(&expanded).is_file() {
                (true, None)
            } else {
                (false, Some("File does not exist on disk.".into()))
            }
        }
        ResourceType::Application => {
            let p = Path::new(&expanded);
            if p.is_file() || crate::utils::resolve_executable_path(&expanded).is_some() {
                (true, None)
            } else {
                (false, Some("Application executable was not found.".into()))
            }
        }
    };

    AvailabilityResult {
        resource_id: resource.id.clone(),
        available,
        reason,
    }
}

pub fn launch_single(resource: &ResourceDto) -> LaunchItemResult {
    match execute_resource(resource) {
        Ok(()) => LaunchItemResult {
            resource_id: resource.id.clone(),
            name: resource.name.clone(),
            resource_type: resource.resource_type,
            target: resource.target.clone(),
            success: true,
            error_code: None,
            error_message: None,
        },
        Err((code, message)) => LaunchItemResult {
            resource_id: resource.id.clone(),
            name: resource.name.clone(),
            resource_type: resource.resource_type,
            target: resource.target.clone(),
            success: false,
            error_code: Some(code),
            error_message: Some(message),
        },
    }
}

fn execute_resource(resource: &ResourceDto) -> Result<(), (String, String)> {
    let expanded = expand_env_path(&resource.target);

    match resource.resource_type {
        ResourceType::Link => {
            let url = validator::normalize_link(&expanded)
                .map_err(|msg| ("INVALID_URL".to_string(), msg))?;
            shell_execute_open(&url, None, None)
        }
        ResourceType::Folder => {
            let path = Path::new(&expanded);
            if !path.is_dir() {
                return Err(("PATH_NOT_FOUND".into(), "Folder was not found.".into()));
            }
            shell_execute_open(&expanded, None, Some(&expanded))
        }
        ResourceType::File => {
            let path = Path::new(&expanded);
            if !path.is_file() {
                return Err(("FILE_NOT_FOUND".into(), "File was not found.".into()));
            }
            let dir = path.parent().and_then(|p| p.to_str());
            shell_execute_open(&expanded, None, dir)
        }
        ResourceType::Application => {
            let mut launch_target = expanded.clone();
            let mut launch_args: Option<String> = None;

            if launch_target.to_ascii_lowercase().ends_with(".lnk") {
                if let Some((resolved_exe, args)) = crate::utils::resolve_shortcut(&launch_target) {
                    launch_target = resolved_exe;
                    if !args.trim().is_empty() {
                        launch_args = Some(args);
                    }
                }
            }

            if !Path::new(&launch_target).is_file() {
                if let Some(resolved_exe) = crate::utils::resolve_executable_path(&launch_target) {
                    launch_target = resolved_exe;
                }
            }

            let working_dir = Path::new(&launch_target)
                .parent()
                .filter(|p| p.is_dir())
                .and_then(|p| p.to_str())
                .map(ToString::to_string);

            shell_execute_open(
                &launch_target,
                launch_args.as_deref(),
                working_dir.as_deref(),
            )
        }
    }
}

fn shell_execute_open(
    target: &str,
    params: Option<&str>,
    working_dir: Option<&str>,
) -> Result<(), (String, String)> {
    let verb_w = to_wide("open");
    let target_w = to_wide(target);
    let params_w = params.map(to_wide);
    let dir_w = working_dir.map(to_wide);

    unsafe {
        let com_ok = CoInitializeEx(None, COINIT_APARTMENTTHREADED).is_ok();
        let hinst = ShellExecuteW(
            None,
            windows::core::PCWSTR(verb_w.as_ptr()),
            windows::core::PCWSTR(target_w.as_ptr()),
            params_w
                .as_ref()
                .map_or(windows::core::PCWSTR::null(), |v| windows::core::PCWSTR(v.as_ptr())),
            dir_w
                .as_ref()
                .map_or(windows::core::PCWSTR::null(), |v| windows::core::PCWSTR(v.as_ptr())),
            SW_SHOWNORMAL,
        );
        let err = GetLastError();
        if com_ok {
            CoUninitialize();
        }

        let code = hinst.0 as isize;
        if code > 32 {
            Ok(())
        } else {
            Err(map_shell_execute_error(code, err))
        }
    }
}

pub fn map_shell_execute_error(hinst_code: isize, last_error: WIN32_ERROR) -> (String, String) {
    let win_code = if last_error.0 != 0 {
        last_error.0 as isize
    } else {
        hinst_code
    };
    match win_code {
        2 => ("FILE_NOT_FOUND".into(), "The target file was not found.".into()),
        3 => ("PATH_NOT_FOUND".into(), "The target path was not found.".into()),
        5 => ("ACCESS_DENIED".into(), "Windows denied access to open this target.".into()),
        27 | 31 | 1155 => (
            "NO_ASSOCIATION".into(),
            "No Windows application is associated with this file type.".into(),
        ),
        1223 => (
            "CANCELLED_BY_USER".into(),
            "Launch was cancelled by Windows prompt.".into(),
        ),
        other => (
            format!("WIN32_{other}"),
            format!("Windows ShellExecute failed (code {other})."),
        ),
    }
}

pub fn extract_resource_icon(resource_type: ResourceType, target: &str) -> Option<String> {
    if resource_type == ResourceType::Link {
        return None;
    }

    let expanded = expand_env_path(target);
    let cache = RESOURCE_ICON_CACHE.get_or_init(|| Mutex::new(HashMap::new()));
    let cache_key = format!("{}:{}", resource_type.as_str(), expanded.to_ascii_lowercase());
    if let Ok(guard) = cache.lock() {
        if let Some(hit) = guard.get(&cache_key) {
            return hit.clone();
        }
    }

    let resolved = if resource_type == ResourceType::Application && !Path::new(&expanded).exists() {
        crate::utils::resolve_executable_path(&expanded).unwrap_or_else(|| expanded.clone())
    } else {
        expanded.clone()
    };

    let icon = unsafe { extract_shell_icon(&resolved, resource_type) };
    if let Ok(mut guard) = cache.lock() {
        guard.insert(cache_key, icon.clone());
    }
    icon
}

unsafe fn extract_shell_icon(path: &str, resource_type: ResourceType) -> Option<String> {
    let com_ok = CoInitializeEx(None, COINIT_APARTMENTTHREADED).is_ok();
    let mut shfi: SHFILEINFOW = std::mem::zeroed();
    let wide = to_wide(path);

    let exists = Path::new(path).exists();
    let (flags, attrs) = if exists {
        (SHGFI_ICON | SHGFI_LARGEICON, Default::default())
    } else {
        let attr = match resource_type {
            ResourceType::Folder => windows::Win32::Storage::FileSystem::FILE_ATTRIBUTE_DIRECTORY,
            _ => windows::Win32::Storage::FileSystem::FILE_ATTRIBUTE_NORMAL,
        };
        (SHGFI_ICON | SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES, attr)
    };

    let res = SHGetFileInfoW(
        windows::core::PCWSTR(wide.as_ptr()),
        attrs,
        Some(&mut shfi),
        std::mem::size_of::<SHFILEINFOW>() as u32,
        flags,
    );

    let icon = if res != 0 && !shfi.hIcon.is_invalid() {
        let b64 = crate::utils::icon_to_base64(shfi.hIcon);
        let _ = DestroyIcon(shfi.hIcon);
        b64
    } else {
        None
    };

    if com_ok {
        CoUninitialize();
    }
    icon
}

fn to_wide(s: &str) -> Vec<u16> {
    s.encode_utf16().chain(std::iter::once(0)).collect()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn maps_shell_execute_win32_error_codes() {
        assert_eq!(map_shell_execute_error(2, WIN32_ERROR(2)).0, "FILE_NOT_FOUND");
        assert_eq!(map_shell_execute_error(3, WIN32_ERROR(3)).0, "PATH_NOT_FOUND");
        assert_eq!(map_shell_execute_error(5, WIN32_ERROR(5)).0, "ACCESS_DENIED");
        assert_eq!(map_shell_execute_error(31, WIN32_ERROR(31)).0, "NO_ASSOCIATION");
        assert_eq!(map_shell_execute_error(31, WIN32_ERROR(1155)).0, "NO_ASSOCIATION");
        assert_eq!(map_shell_execute_error(5, WIN32_ERROR(1223)).0, "CANCELLED_BY_USER");
    }

    #[test]
    fn checks_availability_accurately() {
        let now = "2026-01-01T00:00:00Z".to_string();
        let link = ResourceDto {
            id: "1".into(),
            workspace_id: "w".into(),
            name: "Link".into(),
            resource_type: ResourceType::Link,
            target: "https://example.com".into(),
            sort_order: 0,
            created_at: now.clone(),
            updated_at: now.clone(),
        };
        assert!(check_availability(&link).available);

        let missing = ResourceDto {
            id: "2".into(),
            workspace_id: "w".into(),
            name: "Missing".into(),
            resource_type: ResourceType::File,
            target: r"C:\__definitely_missing_adara_box_test__.txt".into(),
            sort_order: 1,
            created_at: now.clone(),
            updated_at: now,
        };
        assert!(!check_availability(&missing).available);
    }
}
