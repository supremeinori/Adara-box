use std::cell::RefCell;

use tauri::{AppHandle, Manager, WebviewWindow};
use webview2_com::Microsoft::Web::WebView2::Win32::{
    ICoreWebView2, ICoreWebView2Controller, ICoreWebView2Settings2, ICoreWebView2_19,
    ICoreWebView2_2, ICoreWebView2_3, COREWEBVIEW2_MEMORY_USAGE_TARGET_LEVEL_LOW,
    COREWEBVIEW2_MEMORY_USAGE_TARGET_LEVEL_NORMAL,
};
use windows::Win32::Foundation::RECT;
use windows_core::{w, Interface, HSTRING};

const DESKTOP_CHROME_UA: windows_core::PCWSTR = w!(
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0"
);

#[derive(Default)]
struct AiPaneSlot {
    controller: Option<ICoreWebView2Controller>,
    core: Option<ICoreWebView2>,
    creating: bool,
    visible: bool,
    bounds: RECT,
    desired_url: String,
    navigated_url: String,
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

unsafe fn set_ai_suspended(core: &ICoreWebView2, suspend: bool) {
    if let Ok(core19) = core.cast::<ICoreWebView2_19>() {
        let level = if suspend {
            COREWEBVIEW2_MEMORY_USAGE_TARGET_LEVEL_LOW
        } else {
            COREWEBVIEW2_MEMORY_USAGE_TARGET_LEVEL_NORMAL
        };
        let _ = core19.SetMemoryUsageTargetLevel(level);
    }
    if let Ok(core3) = core.cast::<ICoreWebView2_3>() {
        if suspend {
            let _ = core3.TrySuspend(&webview2_com::TrySuspendCompletedHandler::create(Box::new(
                |_, _| Ok(()),
            )));
        } else {
            let _ = core3.Resume();
        }
    }
}

unsafe fn apply_controller_state(slot: &mut AiPaneSlot) {
    let Some(ctrl) = slot.controller.as_ref() else {
        return;
    };
    let _ = ctrl.SetBounds(slot.bounds);
    let _ = ctrl.SetIsVisible(slot.visible);

    if let Some(core) = slot.core.as_ref() {
        set_ai_suspended(core, !slot.visible);
        if slot.visible
            && !slot.desired_url.is_empty()
            && slot.desired_url != slot.navigated_url
        {
            slot.navigated_url = slot.desired_url.clone();
            let _ = core.Navigate(&HSTRING::from(slot.desired_url.as_str()));
        }
    }
}

unsafe fn create_child_controller(win: &WebviewWindow, parent_core: &ICoreWebView2) {
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
        move |_err, controller_opt| {
            AI_PANE.with(|cell| {
                let mut slot = cell.borrow_mut();
                slot.creating = false;
                let Some(ctrl) = controller_opt else {
                    return;
                };

                if let Ok(core) = ctrl.CoreWebView2() {
                    if let Ok(settings) = core.Settings() {
                        let _ = settings.SetIsStatusBarEnabled(false);
                        let _ = settings.SetIsZoomControlEnabled(true);
                        let _ = settings.SetAreDefaultContextMenusEnabled(true);
                        if let Ok(settings2) = settings.cast::<ICoreWebView2Settings2>() {
                            let _ = settings2.SetUserAgent(DESKTOP_CHROME_UA);
                        }
                    }
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
                create_child_controller(&win_clone, &parent_core);
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
    let _ = win.with_webview(move |_webview| unsafe {
        AI_PANE.with(|cell| {
            let slot = cell.borrow();
            if let Some(core) = slot.core.as_ref() {
                let _ = core.Reload();
            }
        });
    });
}

pub fn on_library_window_hidden(win: &WebviewWindow) {
    let _ = win.with_webview(move |_webview| unsafe {
        AI_PANE.with(|cell| {
            let slot = cell.borrow();
            if let Some(ctrl) = slot.controller.as_ref() {
                let _ = ctrl.SetIsVisible(false);
            }
            if let Some(core) = slot.core.as_ref() {
                set_ai_suspended(core, true);
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
}
