import { useEffect, useState } from "react";
import { getCurrentWebviewWindow } from "@tauri-apps/api/webviewWindow";
import { useT } from "../i18n";
import "./WindowControls.css";

interface WindowControlsProps {
  allowMaximize?: boolean;
}

export function WindowControls({ allowMaximize = false }: WindowControlsProps) {
  const t = useT();
  const [focused, setFocused] = useState(true);
  const [maximized, setMaximized] = useState(false);

  useEffect(() => {
    const win = getCurrentWebviewWindow();
    const unlistenFocus = win.onFocusChanged(({ payload }) => setFocused(payload));
    let unlistenResize: Promise<() => void> | null = null;
    if (allowMaximize) {
      win.isMaximized().then(setMaximized).catch(() => {});
      unlistenResize = win.onResized(() => {
        win.isMaximized().then(setMaximized).catch(() => {});
      });
    }
    return () => {
      unlistenFocus.then((fn) => fn());
      unlistenResize?.then((fn) => fn());
    };
  }, [allowMaximize]);

  const win = getCurrentWebviewWindow();

  return (
    <div className={`wc${focused ? "" : " blurred"}`}>
      <button
        className="wc-btn minimize"
        aria-label={t("window.minimize")}
        title={t("window.minimize")}
        onClick={() => win.minimize().catch(() => {})}
      />
      {allowMaximize && (
        <button
          className="wc-btn zoom"
          aria-label={maximized ? "Restore window" : "Maximize window"}
          title={maximized ? "Restore window" : "Maximize window"}
          onClick={() => win.toggleMaximize().catch(() => {})}
        />
      )}
      <button
        className="wc-btn close"
        aria-label={t("window.close")}
        title={t("window.close")}
        onClick={() => win.hide().catch(() => {})}
      />
    </div>
  );
}
