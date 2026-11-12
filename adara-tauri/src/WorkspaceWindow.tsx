import React, { useEffect } from "react";
import ReactDOM from "react-dom/client";
import { invoke } from "@tauri-apps/api/core";
import { Effect, EffectState } from "@tauri-apps/api/window";
import { getCurrentWebviewWindow } from "@tauri-apps/api/webviewWindow";
import { BookOpen, FolderKanban } from "lucide-react";
import { WindowControls } from "./components/WindowControls";
import { WorkspaceView } from "./components/NotchWorkspace";
import { useT } from "./i18n";
import { initTheme } from "./theme";
import { openLibraryWindow } from "./workspaceApi";
import "./surfaces.css";
import "./components/NotchWorkspace.css";
import { initClickSounds } from "./clickSounds";

initClickSounds();

function WorkspaceWindowApp() {
  const t = useT();

  useEffect(() => initTheme(), []);

  useEffect(() => {
    const appWindow = getCurrentWebviewWindow();
    appWindow
      .setEffects({ effects: [Effect.Acrylic], state: EffectState.Active, color: [10, 10, 15, 20] })
      .catch(() => {})
      .finally(() => {
        requestAnimationFrame(() =>
          requestAnimationFrame(() => invoke("on_demand_window_ready").catch(() => {}))
        );
      });
  }, []);

  return (
    <div className="ws-window-shell">
      <header className="ws-window-titlebar" data-tauri-drag-region>
        <div className="ws-window-title">
          <FolderKanban size={15} />
          <span>Adara Box — {t("panel.workspace")}</span>
        </div>
        <div className="ws-window-title-actions">
          <button
            className="ws-action-btn"
            style={{ height: 24, padding: "0 8px", fontSize: 11 }}
            onClick={() => openLibraryWindow().catch(() => {})}
            title={t("ws.openLibrary")}
          >
            <BookOpen size={12} />
            <span>{t("ws.library")}</span>
          </button>
          <WindowControls allowMaximize />
        </div>
      </header>
      <div style={{ flex: 1, minHeight: 0, display: "flex" }}>
        <WorkspaceView mode="window" />
      </div>
    </div>
  );
}

ReactDOM.createRoot(document.getElementById("root") as HTMLElement).render(
  <React.StrictMode>
    <WorkspaceWindowApp />
  </React.StrictMode>
);
