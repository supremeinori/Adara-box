use std::cell::RefCell;

use serde::Serialize;
use tauri::{AppHandle, Emitter, Manager, WebviewWindow};
use webview2_com::Microsoft::Web::WebView2::Win32::{
    ICoreWebView2, ICoreWebView2Controller, ICoreWebView2Settings2, ICoreWebView2_19,
    ICoreWebView2_2, COREWEBVIEW2_MEMORY_USAGE_TARGET_LEVEL_LOW,
    COREWEBVIEW2_MEMORY_USAGE_TARGET_LEVEL_NORMAL, COREWEBVIEW2_WEB_ERROR_STATUS,
    COREWEBVIEW2_WEB_ERROR_STATUS_CANNOT_CONNECT,
    COREWEBVIEW2_WEB_ERROR_STATUS_CERTIFICATE_COMMON_NAME_IS_INCORRECT,
    COREWEBVIEW2_WEB_ERROR_STATUS_CERTIFICATE_EXPIRED,
    COREWEBVIEW2_WEB_ERROR_STATUS_CERTIFICATE_IS_INVALID,
    COREWEBVIEW2_WEB_ERROR_STATUS_CERTIFICATE_REVOKED,
    COREWEBVIEW2_WEB_ERROR_STATUS_CLIENT_CERTIFICATE_CONTAINS_ERRORS,
    COREWEBVIEW2_WEB_ERROR_STATUS_CONNECTION_ABORTED,
    COREWEBVIEW2_WEB_ERROR_STATUS_CONNECTION_RESET, COREWEBVIEW2_WEB_ERROR_STATUS_DISCONNECTED,
    COREWEBVIEW2_WEB_ERROR_STATUS_ERROR_HTTP_INVALID_SERVER_RESPONSE,
    COREWEBVIEW2_WEB_ERROR_STATUS_HOST_NAME_NOT_RESOLVED,
    COREWEBVIEW2_WEB_ERROR_STATUS_OPERATION_CANCELED,
    COREWEBVIEW2_WEB_ERROR_STATUS_REDIRECT_FAILED,
    COREWEBVIEW2_WEB_ERROR_STATUS_SERVER_UNREACHABLE, COREWEBVIEW2_WEB_ERROR_STATUS_TIMEOUT,
    COREWEBVIEW2_WEB_ERROR_STATUS_VALID_AUTHENTICATION_CREDENTIALS_REQUIRED,
    COREWEBVIEW2_WEB_ERROR_STATUS_VALID_PROXY_AUTHENTICATION_REQUIRED,
};
use windows::Win32::Foundation::{HWND, RECT};
use windows::Win32::System::Com::CoTaskMemFree;
use windows_core::{Interface, BOOL, HSTRING, PCWSTR, PWSTR};

#[derive(Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct AiPaneStatusEvent {
    pub state: String,
    pub url: String,
    pub error: Option<String>,
}

#[derive(Default)]
struct AiPaneSlot {
    controller: Option<ICoreWebView2Controller>,
    core: Option<ICoreWebView2>,
    creating: bool,
    visible: bool,
    bounds: RECT,
    desired_url: String,
    navigated_url: String,
    nav_failed: bool,
}

thread_local! {
    static AI_PANE: RefCell<AiPaneSlot> = RefCell::new(AiPaneSlot::default());
}

pub fn validate_ai_url(url: &str) -> Result<String, String> {
    let trimmed = url.trim();
    if !(trimmed.starts_with("https://") || trimmed.starts_with("http://")) {
        return Err("AI companion URL must use HTTPS.".into());
    }
    Ok(trimmed.to_string())
}

unsafe fn pwstr_to_string(ptr: PWSTR) -> String {
    if ptr.is_null() {
        return String::new();
    }
    let s = ptr.to_string().unwrap_or_default();
    CoTaskMemFree(Some(ptr.0 as *const _));
    s
}

fn describe_web_error(status: COREWEBVIEW2_WEB_ERROR_STATUS) -> String {
    if status == COREWEBVIEW2_WEB_ERROR_STATUS_HOST_NAME_NOT_RESOLVED {
        "Cannot resolve server address (check DNS or internet connection).".into()
    } else if status == COREWEBVIEW2_WEB_ERROR_STATUS_DISCONNECTED {
        "Device appears offline.".into()
    } else if status == COREWEBVIEW2_WEB_ERROR_STATUS_TIMEOUT {
        "Connection timed out waiting for AI provider.".into()
    } else if status == COREWEBVIEW2_WEB_ERROR_STATUS_CANNOT_CONNECT
        || status == COREWEBVIEW2_WEB_ERROR_STATUS_SERVER_UNREACHABLE
    {
        "Unable to connect to AI provider server.".into()
    } else if status == COREWEBVIEW2_WEB_ERROR_STATUS_CONNECTION_ABORTED
        || status == COREWEBVIEW2_WEB_ERROR_STATUS_CONNECTION_RESET
    {
        "Connection was interrupted or reset.".into()
    } else if status == COREWEBVIEW2_WEB_ERROR_STATUS_CERTIFICATE_EXPIRED
        || status == COREWEBVIEW2_WEB_ERROR_STATUS_CERTIFICATE_IS_INVALID
        || status == COREWEBVIEW2_WEB_ERROR_STATUS_CERTIFICATE_COMMON_NAME_IS_INCORRECT
        || status == COREWEBVIEW2_WEB_ERROR_STATUS_CERTIFICATE_REVOKED
        || status == COREWEBVIEW2_WEB_ERROR_STATUS_CLIENT_CERTIFICATE_CONTAINS_ERRORS
    {
        "TLS / SSL certificate verification failed.".into()
    } else if status == COREWEBVIEW2_WEB_ERROR_STATUS_ERROR_HTTP_INVALID_SERVER_RESPONSE {
        "Received an invalid response from server.".into()
    } else if status == COREWEBVIEW2_WEB_ERROR_STATUS_REDIRECT_FAILED {
        "Too many redirects or login redirect failed.".into()
    } else if status == COREWEBVIEW2_WEB_ERROR_STATUS_VALID_PROXY_AUTHENTICATION_REQUIRED
        || status == COREWEBVIEW2_WEB_ERROR_STATUS_VALID_AUTHENTICATION_CREDENTIALS_REQUIRED
    {
        "Proxy or authentication credentials required.".into()
    } else {
        format!("Navigation failed (code {}).", status.0)
    }
}

fn emit_status(app: &AppHandle, state: &str, url: String, error: Option<String>) {
    let _ = app.emit_to(
        crate::on_demand_windows::LIBRARY,
        "lib-ai-status",
        AiPaneStatusEvent {
            state: state.to_string(),
            url,
            error,
        },
    );
}

unsafe fn set_ai_memory_level(core: &ICoreWebView2, low: bool) {
    if let Ok(core19) = core.cast::<ICoreWebView2_19>() {
        let level = if low {
            COREWEBVIEW2_MEMORY_USAGE_TARGET_LEVEL_LOW
        } else {
            COREWEBVIEW2_MEMORY_USAGE_TARGET_LEVEL_NORMAL
        };
        let _ = core19.SetMemoryUsageTargetLevel(level);
    }
}

unsafe fn apply_controller_state(slot: &mut AiPaneSlot) {
    let Some(ctrl) = slot.controller.as_ref() else {
        return;
    };
    let has_area = (slot.bounds.right - slot.bounds.left) > 4
        && (slot.bounds.bottom - slot.bounds.top) > 4;
    let show = slot.visible && has_area && !slot.nav_failed;
    let _ = ctrl.SetBounds(slot.bounds);
    let _ = ctrl.SetIsVisible(show);

    if let Some(core) = slot.core.as_ref() {
        set_ai_memory_level(core, !show);
        if slot.visible
            && has_area
            && !slot.desired_url.is_empty()
            && (slot.desired_url != slot.navigated_url || slot.nav_failed)
        {
            slot.navigated_url = slot.desired_url.clone();
            slot.nav_failed = false;
            let _ = ctrl.SetIsVisible(true);
            let _ = core.Navigate(&HSTRING::from(slot.desired_url.as_str()));
        }
    }
}

unsafe fn install_core_handlers(app: &AppHandle, core: &ICoreWebView2) {
    if let Ok(settings) = core.Settings() {
        let _ = settings.SetIsStatusBarEnabled(false);
        let _ = settings.SetIsZoomControlEnabled(true);
        let _ = settings.SetAreDefaultContextMenusEnabled(true);
        let _ = settings.SetIsScriptEnabled(true);
        let _ = settings.SetIsWebMessageEnabled(true);
        let _ = settings.SetAreDefaultScriptDialogsEnabled(true);
        if let Ok(settings2) = settings.cast::<ICoreWebView2Settings2>() {
            let mut raw_ua = PWSTR::null();
            if settings2.UserAgent(&mut raw_ua).is_ok() {
                let current_ua = pwstr_to_string(raw_ua);
                let cleaned = current_ua.replace("; wv)", ")").replace(" wv)", ")");
                if !cleaned.is_empty() && cleaned != current_ua {
                    let ua_h = HSTRING::from(cleaned.as_str());
                    let _ = settings2.SetUserAgent(PCWSTR(ua_h.as_ptr()));
                }
            }
        }
    }

    let app_start = app.clone();
    let start_handler = webview2_com::NavigationStartingEventHandler::create(Box::new(
        move |_sender, args_opt| {
            let url = if let Some(args) = args_opt {
                let mut raw = PWSTR::null();
                if args.Uri(&mut raw).is_ok() {
                    pwstr_to_string(raw)
                } else {
                    String::new()
                }
            } else {
                String::new()
            };
            AI_PANE.with(|cell| {
                cell.borrow_mut().nav_failed = false;
            });
            emit_status(&app_start, "loading", url, None);
            Ok(())
        },
    ));
    let mut tok_start = 0i64;
    let _ = core.add_NavigationStarting(&start_handler, &mut tok_start);

    let app_done = app.clone();
    let done_handler = webview2_com::NavigationCompletedEventHandler::create(Box::new(
        move |sender_opt, args_opt| {
            let mut is_success = BOOL(0);
            let mut web_err = COREWEBVIEW2_WEB_ERROR_STATUS(0);
            if let Some(args) = args_opt {
                let _ = args.IsSuccess(&mut is_success);
                let _ = args.WebErrorStatus(&mut web_err);
            }
            let current_url = if let Some(core) = sender_opt {
                let mut raw = PWSTR::null();
                if core.Source(&mut raw).is_ok() {
                    pwstr_to_string(raw)
                } else {
                    String::new()
                }
            } else {
                String::new()
            };

            if is_success.as_bool() {
                AI_PANE.with(|cell| {
                    cell.borrow_mut().nav_failed = false;
                });
                emit_status(&app_done, "ready", current_url, None);
            } else if web_err != COREWEBVIEW2_WEB_ERROR_STATUS_OPERATION_CANCELED {
                let reason = describe_web_error(web_err);
                AI_PANE.with(|cell| {
                    let mut slot = cell.borrow_mut();
                    slot.nav_failed = true;
                    if let Some(ctrl) = slot.controller.as_ref() {
                        let _ = ctrl.SetIsVisible(false);
                    }
                });
                emit_status(&app_done, "error", current_url, Some(reason));
            }
            Ok(())
        },
    ));
    let mut tok_done = 0i64;
    let _ = core.add_NavigationCompleted(&done_handler, &mut tok_done);

    // Allow OAuth/sign-in popups sharing WebView2's environment & cookies by not cancelling NewWindowRequested.
    let new_win_handler = webview2_com::NewWindowRequestedEventHandler::create(Box::new(
        move |_sender, args_opt| {
            if let Some(args) = args_opt {
                let _ = args.SetHandled(false);
            }
            Ok(())
        },
    ));
    let mut tok_newwin = 0i64;
    let _ = core.add_NewWindowRequested(&new_win_handler, &mut tok_newwin);
}

unsafe fn create_child_controller(app: AppHandle, win: &WebviewWindow, parent_core: &ICoreWebView2) {
    let Ok(hwnd) = win.hwnd() else {
        AI_PANE.with(|cell| cell.borrow_mut().creating = false);
        return;
    };
    let Ok(core2) = parent_core.cast::<ICoreWebView2_2>() else {
        AI_PANE.with(|cell| cell.borrow_mut().creating = false);
        return;
    };
    let Ok(env) = core2.Environment() else {
        AI_PANE.with(|cell| cell.borrow_mut().creating = false);
        return;
    };

    let handler = webview2_com::CreateCoreWebView2ControllerCompletedHandler::create(Box::new(
        move |res, controller_opt| {
            if res.is_err() {
                AI_PANE.with(|cell| cell.borrow_mut().creating = false);
                emit_status(
                    &app,
                    "error",
                    String::new(),
                    Some("Could not initialize embedded WebView2 AI pane.".into()),
                );
                return Ok(());
            }
            AI_PANE.with(|cell| {
                let mut slot = cell.borrow_mut();
                slot.creating = false;
                let Some(ctrl) = controller_opt else {
                    emit_status(
                        &app,
                        "error",
                        slot.desired_url.clone(),
                        Some("WebView2 controller returned empty.".into()),
                    );
                    return;
                };

                if let Ok(core) = ctrl.CoreWebView2() {
                    install_core_handlers(&app, &core);
                    slot.core = Some(core);
                }
                slot.controller = Some(ctrl);
                apply_controller_state(&mut slot);
            });
            Ok(())
        },
    ));

    if env.CreateCoreWebView2Controller(hwnd, &handler).is_err() {
        AI_PANE.with(|cell| cell.borrow_mut().creating = false);
    }
}

pub fn sync_pane(
    app: &AppHandle,
    visible: bool,
    x: i32,
    y: i32,
    width: i32,
    height: i32,
    url: Option<String>,
) -> Result<(), String> {
    let clean_url = match url {
        Some(u) if !u.trim().is_empty() => Some(validate_ai_url(&u)?),
        _ => None,
    };

    let Some(win) = app.get_webview_window(crate::on_demand_windows::LIBRARY) else {
        return Ok(());
    };

    let win_clone = win.clone();
    let app_clone = app.clone();
    let _ = win.with_webview(move |webview| unsafe {
        let mut need_create = false;
        AI_PANE.with(|cell| {
            let mut slot = cell.borrow_mut();
            slot.visible = visible;
            slot.bounds = RECT {
                left: x,
                top: y,
                right: x + width.max(0),
                bottom: y + height.max(0),
            };
            if let Some(u) = clean_url {
                if u != slot.desired_url {
                    slot.nav_failed = false;
                }
                slot.desired_url = u;
            }
            if slot.controller.is_some() {
                apply_controller_state(&mut slot);
            } else if visible && !slot.creating {
                slot.creating = true;
                need_create = true;
            }
        });

        if need_create {
            if let Ok(parent_core) = webview.controller().CoreWebView2() {
                create_child_controller(app_clone, &win_clone, &parent_core);
            } else {
                AI_PANE.with(|cell| cell.borrow_mut().creating = false);
            }
        }
    });

    Ok(())
}

pub fn reload_pane(app: &AppHandle) {
    let Some(win) = app.get_webview_window(crate::on_demand_windows::LIBRARY) else {
        return;
    };
    let app_clone = app.clone();
    let _ = win.with_webview(move |_webview| unsafe {
        AI_PANE.with(|cell| {
            let mut slot = cell.borrow_mut();
            let desired = slot.desired_url.clone();
            let failed = slot.nav_failed;
            let Some(core) = slot.core.clone() else {
                return;
            };
            let ctrl_opt = slot.controller.clone();
            let mut raw = PWSTR::null();
            let current_src = if core.Source(&mut raw).is_ok() {
                pwstr_to_string(raw)
            } else {
                String::new()
            };
            emit_status(&app_clone, "loading", desired.clone(), None);
            if (failed || current_src.is_empty() || current_src == "about:blank")
                && !desired.is_empty()
            {
                slot.nav_failed = false;
                slot.navigated_url = desired.clone();
                if let Some(ctrl) = ctrl_opt.as_ref() {
                    let _ = ctrl.SetIsVisible(true);
                }
                let _ = core.Navigate(&HSTRING::from(desired.as_str()));
            } else {
                slot.nav_failed = false;
                if let Some(ctrl) = ctrl_opt.as_ref() {
                    let _ = ctrl.SetIsVisible(true);
                }
                let _ = core.Reload();
            }
        });
    });
}

pub fn open_external(url: &str) -> Result<(), String> {
    let clean = validate_ai_url(url)?;
    let url_h = HSTRING::from(clean.as_str());
    let op_h = HSTRING::from("open");
    let ret = unsafe {
        windows::Win32::UI::Shell::ShellExecuteW(
            Some(HWND(std::ptr::null_mut())),
            PCWSTR(op_h.as_ptr()),
            PCWSTR(url_h.as_ptr()),
            PCWSTR::null(),
            PCWSTR::null(),
            windows::Win32::UI::WindowsAndMessaging::SW_SHOWNORMAL,
        )
    };
    if (ret.0 as isize) <= 32 {
        return Err("Failed to open AI link in default browser.".into());
    }
    Ok(())
}

pub fn on_library_window_hidden(win: &WebviewWindow) {
    let _ = win.with_webview(move |_webview| unsafe {
        AI_PANE.with(|cell| {
            let slot = cell.borrow();
            if let Some(ctrl) = slot.controller.as_ref() {
                let _ = ctrl.SetIsVisible(false);
            }
            if let Some(core) = slot.core.as_ref() {
                set_ai_memory_level(core, true);
            }
        });
    });
}

pub fn on_library_window_shown(win: &WebviewWindow) {
    let _ = win.with_webview(move |_webview| unsafe {
        AI_PANE.with(|cell| {
            let mut slot = cell.borrow_mut();
            if slot.visible {
                apply_controller_state(&mut slot);
            }
        });
    });
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn validates_ai_companion_urls() {
        assert!(validate_ai_url("https://gemini.google.com/app").is_ok());
        assert!(validate_ai_url("javascript:alert(1)").is_err());
        assert!(validate_ai_url("file:///C:/Windows/System32").is_err());
    }

    #[test]
    fn maps_web_error_statuses_to_readable_messages() {
        assert!(describe_web_error(COREWEBVIEW2_WEB_ERROR_STATUS_HOST_NAME_NOT_RESOLVED)
            .to_lowercase()
            .contains("resolve"));
        assert!(describe_web_error(COREWEBVIEW2_WEB_ERROR_STATUS_TIMEOUT)
            .to_lowercase()
            .contains("timed out"));
    }
}
