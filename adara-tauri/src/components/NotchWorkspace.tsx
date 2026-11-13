import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { listen } from "@tauri-apps/api/event";
import { getCurrentWebviewWindow } from "@tauri-apps/api/webviewWindow";
import {
  AlertTriangle,
  AppWindow,
  ArrowDown,
  ArrowLeft,
  ArrowUp,
  BookOpen,
  Check,
  Clipboard,
  ExternalLink,
  FileText,
  Folder,
  Globe,
  HardDriveDownload,
  ImagePlus,
  LayoutGrid,
  List,
  Pencil,
  Play,
  Plus,
  RefreshCw,
  Search,
  Square,
  Trash2,
  X,
} from "lucide-react";
import { useT } from "../i18n";
import {
  isImageWorkspaceIcon,
  type AvailabilityResult,
  type BatchLaunchSummary,
  type LaunchProgressEvent,
  type ResourceDto,
  type ResourceType,
  type WorkspaceDto,
  extractErrorMessage,
  formatDomainOrTarget,
  listenWorkspaceChanged,
  openLibraryWindow,
  openWorkspaceWindow,
  resAdd,
  resAddMany,
  resCheckAvailability,
  resGetIcon,
  resLaunchBatch,
  resLaunchSingle,
  resList,
  resPickFile,
  resPickFolder,
  resRemove,
  resReorder,
  resUpdate,
  wsCancelLaunch,
  wsCreate,
  wsCreateBackup,
  wsDelete,
  wsGetIconDataUrl,
  wsList,
  wsPickIconImage,
  wsReorder,
  wsUpdate,
} from "../workspaceApi";
import "./NotchWorkspace.css";

const EMOJI_PRESETS = ["📁", "💼", "💻", "📚", "🎨", "🎵", "🎮", "🧪", "🚀", "📌"];
const ACTIVE_WS_STORAGE = "adarabox-active-workspace-id";
const VIEW_MODE_STORAGE = "adarabox-workspace-view";

type SheetState =
  | { kind: "none" }
  | { kind: "create-ws" }
  | { kind: "edit-ws"; workspace: WorkspaceDto; confirmDelete?: boolean }
  | { kind: "add-res" }
  | { kind: "edit-res"; resource: ResourceDto }
  | { kind: "summary"; summary: BatchLaunchSummary };

type WorkspaceViewProps = {
  mode: "notch" | "window";
  active?: boolean;
  windowLabel?: string;
  onBusyChange?: (busy: boolean) => void;
  onDropHoverChange?: (hovering: boolean) => void;
};

function WorkspaceAvatar({
  icon,
  size = "sm",
}: {
  icon: string;
  size?: "sm" | "lg";
}) {
  const [dataUrl, setDataUrl] = useState<string | null>(null);
  const isImage = isImageWorkspaceIcon(icon);

  useEffect(() => {
    let cancelled = false;
    if (!isImage) {
      setDataUrl(null);
      return;
    }
    wsGetIconDataUrl(icon).then((url) => {
      if (!cancelled) setDataUrl(url);
    });
    return () => {
      cancelled = true;
    };
  }, [icon, isImage]);

  if (isImage && dataUrl) {
    return (
      <img
        className={size === "lg" ? "ws-card-cover-img" : "ws-avatar-img"}
        src={dataUrl}
        alt=""
        draggable={false}
      />
    );
  }
  const fallback = isImage ? "📁" : icon || "📁";
  return (
    <span className={size === "lg" ? "ws-card-cover-emoji" : "ws-avatar-emoji"}>
      {fallback}
    </span>
  );
}

function ResourceGlyph({ resource }: { resource: ResourceDto }) {
  const [iconUrl, setIconUrl] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    if (resource.type === "link") {
      setIconUrl(null);
      return;
    }
    resGetIcon(resource.type, resource.target).then((url) => {
      if (!cancelled) setIconUrl(url);
    });
    return () => {
      cancelled = true;
    };
  }, [resource.type, resource.target]);

  if (iconUrl) {
    return <img src={iconUrl} alt="" draggable={false} />;
  }
  switch (resource.type) {
    case "application":
      return <AppWindow size={15} />;
    case "folder":
      return <Folder size={15} />;
    case "link":
      return <Globe size={15} />;
    default:
      return <FileText size={15} />;
  }
}

export function WorkspaceView({
  mode,
  active = true,
  windowLabel = "",
  onBusyChange,
  onDropHoverChange,
}: WorkspaceViewProps) {
  const t = useT();
  const [workspaces, setWorkspaces] = useState<WorkspaceDto[]>([]);
  const [activeWsId, setActiveWsId] = useState<string>(
    () => localStorage.getItem(ACTIVE_WS_STORAGE) || ""
  );
  const [resources, setResources] = useState<ResourceDto[]>([]);
  const [availability, setAvailability] = useState<Record<string, AvailabilityResult>>({});
  const [viewMode, setViewMode] = useState<"grid" | "list">(() =>
    localStorage.getItem(VIEW_MODE_STORAGE) === "list" ? "list" : "grid"
  );
  const [showOverview, setShowOverview] = useState<boolean>(() => mode === "notch");
  const [search, setSearch] = useState("");
  const [sheet, setSheet] = useState<SheetState>({ kind: "none" });
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [statusNotice, setStatusNotice] = useState<string | null>(null);
  const [dropActive, setDropActive] = useState(false);
  const [dialogBusy, setDialogBusy] = useState(false);
  const [justLaunchedId, setJustLaunchedId] = useState<string | null>(null);
  const [launchProgress, setLaunchProgress] = useState<LaunchProgressEvent | null>(null);
  const [batchRunning, setBatchRunning] = useState(false);

  // Sheet form state
  const [wsName, setWsName] = useState("");
  const [wsIcon, setWsIcon] = useState("📁");
  const [resName, setResName] = useState("");
  const [resType, setResType] = useState<ResourceType>("application");
  const [resTarget, setResTarget] = useState("");

  const activeWsIdRef = useRef(activeWsId);
  activeWsIdRef.current = activeWsId;

  const activeWorkspace = useMemo(
    () => workspaces.find((w) => w.id === activeWsId) ?? workspaces[0] ?? null,
    [workspaces, activeWsId]
  );

  // Keep Notch pinned while user interacts with forms, native pickers, drag-drop, or batch launch.
  useEffect(() => {
    const busy = sheet.kind !== "none" || dialogBusy || batchRunning || dropActive;
    onBusyChange?.(busy);
  }, [sheet.kind, dialogBusy, batchRunning, dropActive, onBusyChange]);

  const loadWorkspaces = useCallback(async () => {
    try {
      const list = await wsList();
      setWorkspaces(list);
      setActiveWsId((prev) => {
        if (prev && list.some((w) => w.id === prev)) return prev;
        const first = list[0]?.id ?? "";
        if (first) localStorage.setItem(ACTIVE_WS_STORAGE, first);
        return first;
      });
    } catch (err) {
      setErrorMsg(extractErrorMessage(err));
    }
  }, []);

  const loadResources = useCallback(async (workspaceId: string) => {
    if (!workspaceId) {
      setResources([]);
      setAvailability({});
      return;
    }
    try {
      const list = await resList(workspaceId);
      setResources(list);
      const checks = await resCheckAvailability(workspaceId);
      const map: Record<string, AvailabilityResult> = {};
      for (const c of checks) map[c.resourceId] = c;
      setAvailability(map);
    } catch (err) {
      setErrorMsg(extractErrorMessage(err));
    }
  }, []);

  useEffect(() => {
    loadWorkspaces();
    const unlisten = listenWorkspaceChanged(() => {
      loadWorkspaces();
      if (activeWsIdRef.current) loadResources(activeWsIdRef.current);
    });
    return () => {
      unlisten.then((fn) => fn());
    };
  }, [loadWorkspaces, loadResources]);

  useEffect(() => {
    if (activeWorkspace?.id) {
      localStorage.setItem(ACTIVE_WS_STORAGE, activeWorkspace.id);
      loadResources(activeWorkspace.id);
    } else {
      setResources([]);
    }
  }, [activeWorkspace?.id, loadResources]);

  useEffect(() => {
    const unlisten = listen<LaunchProgressEvent>("workspace-launch-progress", (e) => {
      setLaunchProgress(e.payload);
      if (e.payload.cancelled || e.payload.completed >= e.payload.total) {
        setTimeout(() => setLaunchProgress(null), 900);
      }
    });
    return () => {
      unlisten.then((fn) => fn());
    };
  }, []);

  // Drag & drop: in Notch mode cooperate with OLE shelf-drag-* events; in Pop-out Window use Tauri drag-drop.
  useEffect(() => {
    if (mode === "notch") {
      if (!active || !windowLabel) return;
      const unlistens = [
        listen<{ label: string }>("shelf-drag-enter", (e) => {
          if (e.payload.label === windowLabel) {
            setDropActive(true);
            onDropHoverChange?.(true);
          }
        }),
        listen<{ label: string }>("shelf-drag-leave", (e) => {
          if (e.payload.label === windowLabel) {
            setDropActive(false);
            onDropHoverChange?.(false);
          }
        }),
        listen<{ label: string; paths?: string[]; text?: string }>("shelf-drag-drop", async (e) => {
          if (e.payload.label !== windowLabel) return;
          setDropActive(false);
          onDropHoverChange?.(false);
          const wsId = activeWsIdRef.current;
          if (!wsId) return;
          const inputs = [
            ...(e.payload.paths ?? []).map((p) => ({ target: p })),
            ...(e.payload.text && e.payload.text.trim() ? [{ target: e.payload.text.trim() }] : []),
          ];
          if (inputs.length === 0) return;
          try {
            await resAddMany(wsId, inputs);
            await loadWorkspaces();
            await loadResources(wsId);
          } catch (err) {
            setErrorMsg(extractErrorMessage(err));
          }
        }),
      ];
      return () => {
        setDropActive(false);
        unlistens.forEach((p) => p.then((fn) => fn()));
      };
    }

    const win = getCurrentWebviewWindow();
    const unlisten = win.onDragDropEvent(async (event) => {
      const payload = event.payload;
      if (payload.type === "enter" || payload.type === "over") {
        setDropActive(true);
      } else if (payload.type === "leave") {
        setDropActive(false);
      } else if (payload.type === "drop") {
        setDropActive(false);
        const wsId = activeWsIdRef.current;
        if (!wsId || payload.paths.length === 0) return;
        try {
          await resAddMany(
            wsId,
            payload.paths.map((p) => ({ target: p }))
          );
          await loadWorkspaces();
          await loadResources(wsId);
        } catch (err) {
          setErrorMsg(extractErrorMessage(err));
        }
      }
    });
    return () => {
      unlisten.then((fn) => fn());
    };
  }, [mode, active, windowLabel, loadWorkspaces, loadResources, onDropHoverChange]);

  const enterWorkspace = (ws: WorkspaceDto) => {
    setActiveWsId(ws.id);
    setSearch("");
    setShowOverview(false);
  };

  const filteredWorkspaces = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q || !showOverview) return workspaces;
    return workspaces.filter((w) => w.name.toLowerCase().includes(q));
  }, [workspaces, search, showOverview]);

  const filteredResources = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return resources;
    return resources.filter(
      (r) =>
        r.name.toLowerCase().includes(q) ||
        r.target.toLowerCase().includes(q) ||
        r.type.toLowerCase().includes(q)
    );
  }, [resources, search]);

  const toggleViewMode = () => {
    const next = viewMode === "grid" ? "list" : "grid";
    setViewMode(next);
    localStorage.setItem(VIEW_MODE_STORAGE, next);
  };

  const openCreateWsSheet = () => {
    setWsName("");
    setWsIcon("📁");
    setErrorMsg(null);
    setSheet({ kind: "create-ws" });
  };

  const openEditWsSheet = (ws: WorkspaceDto) => {
    setWsName(ws.name);
    setWsIcon(ws.icon || "📁");
    setErrorMsg(null);
    setSheet({ kind: "edit-ws", workspace: ws, confirmDelete: false });
  };

  const sanitizeTarget = (raw: string): string => {
    let clean = raw.trim();
    if (clean.startsWith('"') && clean.endsWith('"') && clean.length >= 2) {
      clean = clean.slice(1, -1).trim();
    }
    return clean;
  };

  const inferMetaFromTarget = (target: string): { type?: ResourceType; name?: string } => {
    const clean = sanitizeTarget(target);
    if (!clean) return {};

    const lower = clean.toLowerCase();
    if (lower.startsWith("http://") || lower.startsWith("https://") || lower.startsWith("www.")) {
      try {
        const urlStr = lower.startsWith("www.") ? `https://${clean}` : clean;
        const u = new URL(urlStr);
        const host = u.hostname.replace(/^www\./i, "");
        const pathPart = u.pathname.replace(/^\/|\/$/g, "").split("/").pop();
        const autoName = pathPart ? `${host}/${pathPart}` : host;
        return { type: "link", name: autoName };
      } catch {
        return { type: "link", name: clean.replace(/^https?:\/\//i, "").split("/")[0] || "Link" };
      }
    }

    if (/\.(exe|lnk|bat|cmd|msi)$/i.test(clean)) {
      const base = clean.split(/[\\/]/).filter(Boolean).pop() ?? clean;
      const cleanName = base.replace(/\.(exe|lnk|bat|cmd|msi)$/i, "");
      return { type: "application", name: cleanName };
    }

    if (/[\\/]$/.test(clean) || (!/\.[a-zA-Z0-9]{1,6}$/.test(clean) && /^[a-zA-Z]:[\\/]/.test(clean))) {
      const base = clean.split(/[\\/]/).filter(Boolean).pop() ?? clean;
      return { type: "folder", name: base };
    }

    const base = clean.split(/[\\/]/).filter(Boolean).pop() ?? clean;
    return { type: "file", name: base };
  };

  const handlePasteTarget = async () => {
    try {
      const text = await navigator.clipboard.readText();
      if (!text || !text.trim()) {
        setStatusNotice("Clipboard is empty");
        setTimeout(() => setStatusNotice(null), 1500);
        return;
      }
      const clean = sanitizeTarget(text);
      setResTarget(clean);
      const inferred = inferMetaFromTarget(clean);
      if (inferred.type && (sheet.kind === "add-res" || resType === "application")) {
        setResType(inferred.type);
      }
      if (!resName.trim() && inferred.name) {
        setResName(inferred.name);
      }
      setStatusNotice(t("ws.pasteSuccess") || "Pasted from clipboard");
      setTimeout(() => setStatusNotice(null), 1500);
    } catch (err) {
      setErrorMsg("Failed to read clipboard: " + extractErrorMessage(err));
    }
  };

  const openAddResSheet = () => {
    if (!activeWorkspace) {
      openCreateWsSheet();
      return;
    }
    setResName("");
    setResType("application");
    setResTarget("");
    setErrorMsg(null);
    setSheet({ kind: "add-res" });
  };

  const openEditResSheet = (res: ResourceDto) => {
    setResName(res.name);
    setResType(res.type);
    setResTarget(res.target);
    setErrorMsg(null);
    setSheet({ kind: "edit-res", resource: res });
  };

  const handlePickCustomWsIcon = async () => {
    setDialogBusy(true);
    try {
      const custom = await wsPickIconImage();
      if (custom) setWsIcon(custom);
    } catch (err) {
      setErrorMsg(extractErrorMessage(err));
    } finally {
      setDialogBusy(false);
    }
  };

  const handleSaveWorkspace = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (sheet.kind === "create-ws") {
        const created = await wsCreate(wsName, wsIcon);
        setActiveWsId(created.id);
        setShowOverview(false);
        setSheet({ kind: "none" });
        await loadWorkspaces();
      } else if (sheet.kind === "edit-ws") {
        await wsUpdate(sheet.workspace.id, wsName, wsIcon);
        setSheet({ kind: "none" });
        await loadWorkspaces();
      }
    } catch (err) {
      setErrorMsg(extractErrorMessage(err));
    }
  };

  const handleDeleteWorkspace = async (ws: WorkspaceDto) => {
    try {
      await wsDelete(ws.id);
      setSheet({ kind: "none" });
      await loadWorkspaces();
    } catch (err) {
      setErrorMsg(extractErrorMessage(err));
    }
  };

  const handleMoveWorkspace = async (idx: number, delta: -1 | 1) => {
    const targetIdx = idx + delta;
    if (targetIdx < 0 || targetIdx >= workspaces.length) return;
    const next = [...workspaces];
    const [moved] = next.splice(idx, 1);
    next.splice(targetIdx, 0, moved);
    setWorkspaces(next);
    await wsReorder(next.map((w) => w.id)).catch((err) => setErrorMsg(extractErrorMessage(err)));
  };

  const handleBrowseResource = async () => {
    setDialogBusy(true);
    try {
      const picked = resType === "folder" ? await resPickFolder() : await resPickFile();
      if (!picked) return;
      setResTarget(picked);
      if (!resName.trim()) {
        const base = picked.split(/[\\/]/).filter(Boolean).pop() ?? picked;
        const clean =
          resType === "application" ? base.replace(/\.(exe|lnk|bat|cmd|msi)$/i, "") : base;
        setResName(clean);
      }
    } catch (err) {
      setErrorMsg(extractErrorMessage(err));
    } finally {
      setDialogBusy(false);
    }
  };

  const handleSaveResource = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!activeWorkspace) return;
    try {
      if (sheet.kind === "add-res") {
        const autoName =
          resName.trim() ||
          (resType === "link"
            ? resTarget.replace(/^https?:\/\//i, "").split("/")[0] || "Link"
            : resTarget.split(/[\\/]/).filter(Boolean).pop() || resTarget);
        await resAdd(activeWorkspace.id, autoName, resType, resTarget);
        setSheet({ kind: "none" });
        await Promise.all([loadWorkspaces(), loadResources(activeWorkspace.id)]);
      } else if (sheet.kind === "edit-res") {
        await resUpdate(
          sheet.resource.id,
          activeWorkspace.id,
          resName,
          resType,
          resTarget
        );
        setSheet({ kind: "none" });
        await loadResources(activeWorkspace.id);
      }
    } catch (err) {
      setErrorMsg(extractErrorMessage(err));
    }
  };

  const handleRemoveResource = async (res: ResourceDto) => {
    try {
      await resRemove(res.id, res.workspaceId);
      await Promise.all([loadWorkspaces(), loadResources(res.workspaceId)]);
    } catch (err) {
      setErrorMsg(extractErrorMessage(err));
    }
  };

  const handleMoveResource = async (idx: number, delta: -1 | 1) => {
    if (!activeWorkspace || search.trim()) return;
    const targetIdx = idx + delta;
    if (targetIdx < 0 || targetIdx >= resources.length) return;
    const next = [...resources];
    const [moved] = next.splice(idx, 1);
    next.splice(targetIdx, 0, moved);
    setResources(next);
    await resReorder(activeWorkspace.id, next.map((r) => r.id)).catch((err) =>
      setErrorMsg(extractErrorMessage(err))
    );
  };

  const handleLaunchSingle = async (res: ResourceDto) => {
    setErrorMsg(null);
    try {
      const result = await resLaunchSingle(res.id);
      if (result.success) {
        setJustLaunchedId(res.id);
        setTimeout(() => setJustLaunchedId((cur) => (cur === res.id ? null : cur)), 900);
      } else {
        setErrorMsg(result.errorMessage || `Failed to launch ${res.name}`);
      }
    } catch (err) {
      setErrorMsg(extractErrorMessage(err));
    }
  };

  const handleLaunchBatch = async (onlyIds?: string[]) => {
    if (!activeWorkspace || batchRunning) return;
    setErrorMsg(null);
    setBatchRunning(true);
    try {
      const summary = await resLaunchBatch(activeWorkspace.id, onlyIds);
      if (summary.failed > 0) {
        setSheet({ kind: "summary", summary });
      } else if (summary.launched > 0 && !summary.cancelled) {
        setStatusNotice(`Opened ${summary.launched} item${summary.launched === 1 ? "" : "s"}`);
        setTimeout(() => setStatusNotice(null), 1800);
      }
    } catch (err) {
      setErrorMsg(extractErrorMessage(err));
    } finally {
      setBatchRunning(false);
    }
  };

  const handleBackup = async () => {
    try {
      await wsCreateBackup();
      setStatusNotice("Database snapshot saved");
      setTimeout(() => setStatusNotice(null), 2000);
    } catch (err) {
      setErrorMsg(extractErrorMessage(err));
    }
  };

  return (
    <div
      tabIndex={0}
      className={`ws-root ${mode === "notch" ? "is-notch" : "is-window"}`}
      onClick={(e) => e.stopPropagation()}
      onKeyDown={async (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "v" && sheet.kind === "none" && activeWorkspace) {
          try {
            const text = await navigator.clipboard.readText();
            if (text && text.trim()) {
              const clean = sanitizeTarget(text);
              const inferred = inferMetaFromTarget(clean);
              setResTarget(clean);
              setResType(inferred.type || (clean.startsWith("http") ? "link" : "file"));
              setResName(inferred.name || "");
              setErrorMsg(null);
              setSheet({ kind: "add-res" });
            }
          } catch {}
        }
      }}
    >
      {mode === "window" && (
        <aside className="ws-sidebar">
          <div className="ws-sidebar-header">
            <span>{t("panel.workspace")}</span>
            <button
              className="ws-action-btn primary"
              style={{ height: 24, padding: "0 8px", fontSize: 11 }}
              onClick={openCreateWsSheet}
              title={t("ws.newWorkspace")}
              aria-label={t("ws.newWorkspace")}
            >
              <Plus size={12} />
              <span>{t("ws.add")}</span>
            </button>
          </div>

          <button
            className={`ws-sidebar-item ${showOverview ? "active" : ""}`}
            onClick={() => {
              setSearch("");
              setShowOverview(true);
            }}
          >
            <LayoutGrid size={14} />
            <span className="ws-sidebar-item-name">{t("ws.allWorkspaces")}</span>
            <span className="ws-pill-count">{workspaces.length}</span>
          </button>

          <div className="ws-sidebar-list">
            {workspaces.map((ws, idx) => (
              <div
                key={ws.id}
                role="button"
                tabIndex={0}
                className={`ws-sidebar-item ${!showOverview && ws.id === activeWorkspace?.id ? "active" : ""}`}
                onClick={() => enterWorkspace(ws)}
                onKeyDown={(e) => {
                  if (e.key === "Enter" || e.key === " ") enterWorkspace(ws);
                }}
              >
                <WorkspaceAvatar icon={ws.icon} />
                <span className="ws-sidebar-item-name">{ws.name}</span>
                <span className="ws-pill-count">{ws.resourceCount}</span>
                <div className="ws-sidebar-actions" onClick={(e) => e.stopPropagation()}>
                  {idx > 0 && (
                    <button
                      className="ws-mini-btn"
                      onClick={() => handleMoveWorkspace(idx, -1)}
                      title="Move up"
                    >
                      <ArrowUp size={11} />
                    </button>
                  )}
                  {idx < workspaces.length - 1 && (
                    <button
                      className="ws-mini-btn"
                      onClick={() => handleMoveWorkspace(idx, 1)}
                      title="Move down"
                    >
                      <ArrowDown size={11} />
                    </button>
                  )}
                  <button
                    className="ws-mini-btn"
                    onClick={() => openEditWsSheet(ws)}
                    title={t("ws.editWorkspace")}
                  >
                    <Pencil size={11} />
                  </button>
                </div>
              </div>
            ))}
          </div>

          <div className="ws-sidebar-footer">
            <button
              className="ws-action-btn"
              style={{ flex: 1, justifyContent: "center" }}
              onClick={handleBackup}
              title="Save rolling SQLite backup"
            >
              <HardDriveDownload size={13} />
              <span>{t("ws.backup")}</span>
            </button>
          </div>
        </aside>
      )}

      <div className="ws-main">
        {showOverview ? (
          <>
            <div className="ws-detail-head">
              <div className="ws-detail-title">
                <LayoutGrid size={14} />
                <span className="ws-detail-title-text">
                  {t("ws.allWorkspaces")} ({workspaces.length})
                </span>
              </div>
              <button
                className="ws-action-btn primary"
                onClick={openCreateWsSheet}
                title={t("ws.newWorkspace")}
              >
                <Plus size={12} />
                <span>{t("ws.newWorkspaceShort")}</span>
              </button>
              {mode === "notch" && (
                <>
                  <button
                    className="ws-icon-btn"
                    onClick={() => openLibraryWindow().catch(() => {})}
                    title={t("ws.openLibrary")}
                    aria-label={t("ws.openLibrary")}
                  >
                    <BookOpen size={13} />
                  </button>
                  <button
                    className="ws-icon-btn"
                    onClick={() => openWorkspaceWindow().catch(() => {})}
                    title={t("ws.popOut")}
                    aria-label={t("ws.popOut")}
                  >
                    <ExternalLink size={12} />
                  </button>
                </>
              )}
            </div>

            {workspaces.length > 0 && (
              <div className="ws-toolbar">
                <div className="ws-search-wrap">
                  <Search size={12} className="ws-search-icon" />
                  <input
                    className="ws-search-input"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    onKeyDown={(e) => {
                      if (e.key === "Escape") setSearch("");
                    }}
                    placeholder={t("ws.allWorkspaces")}
                  />
                  {search && (
                    <button
                      className="ws-search-clear"
                      onClick={() => setSearch("")}
                      aria-label="Clear search"
                    >
                      <X size={10} />
                    </button>
                  )}
                </div>
              </div>
            )}
          </>
        ) : (
          <>
            <div className="ws-detail-head">
              <button
                className="ws-action-btn"
                onClick={() => {
                  setSearch("");
                  setShowOverview(true);
                }}
                title={t("ws.allWorkspaces")}
              >
                <ArrowLeft size={12} />
                <span>{t("ws.backToWorkspaces")}</span>
              </button>
              <div className="ws-detail-title">
                {activeWorkspace && <WorkspaceAvatar icon={activeWorkspace.icon} />}
                <span className="ws-detail-title-text">
                  {activeWorkspace?.name ?? t("panel.workspace")}
                </span>
              </div>
              {activeWorkspace && (
                <button
                  className="ws-icon-btn"
                  onClick={() => openEditWsSheet(activeWorkspace)}
                  title={t("ws.editWorkspace")}
                  aria-label={t("ws.editWorkspace")}
                >
                  <Pencil size={12} />
                </button>
              )}
              {mode === "notch" && (
                <>
                  <button
                    className="ws-icon-btn"
                    onClick={openCreateWsSheet}
                    title={t("ws.newWorkspace")}
                    aria-label={t("ws.newWorkspace")}
                  >
                    <Plus size={13} />
                  </button>
                  <button
                    className="ws-icon-btn"
                    onClick={() => openLibraryWindow().catch(() => {})}
                    title={t("ws.openLibrary")}
                    aria-label={t("ws.openLibrary")}
                  >
                    <BookOpen size={13} />
                  </button>
                  <button
                    className="ws-icon-btn"
                    onClick={() => openWorkspaceWindow().catch(() => {})}
                    title={t("ws.popOut")}
                    aria-label={t("ws.popOut")}
                  >
                    <ExternalLink size={12} />
                  </button>
                </>
              )}
            </div>

            <div className="ws-toolbar">
              <div className="ws-search-wrap">
                <Search size={12} className="ws-search-icon" />
                <input
                  className="ws-search-input"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === "Escape") setSearch("");
                  }}
                  placeholder={t("ws.searchPlaceholder")}
                />
                {search && (
                  <button
                    className="ws-search-clear"
                    onClick={() => setSearch("")}
                    aria-label="Clear search"
                  >
                    <X size={10} />
                  </button>
                )}
              </div>

              <button
                className="ws-icon-btn"
                onClick={toggleViewMode}
                title={viewMode === "grid" ? t("ws.listView") : t("ws.gridView")}
                aria-label={viewMode === "grid" ? t("ws.listView") : t("ws.gridView")}
              >
                {viewMode === "grid" ? <List size={13} /> : <LayoutGrid size={13} />}
              </button>

              <button
                className="ws-action-btn"
                onClick={openAddResSheet}
                disabled={!activeWorkspace}
                title={t("ws.addResource")}
              >
                <Plus size={13} />
                <span>{t("ws.add")}</span>
              </button>

              {batchRunning ? (
                <button
                  className="ws-action-btn danger"
                  onClick={() => wsCancelLaunch().catch(() => {})}
                >
                  <Square size={11} />
                  <span>{t("ws.cancel")}</span>
                </button>
              ) : (
                <button
                  className="ws-action-btn primary"
                  onClick={() => handleLaunchBatch()}
                  disabled={!activeWorkspace || resources.length === 0}
                  title={t("ws.openAll")}
                >
                  <Play size={12} />
                  <span>{t("ws.openAll")}</span>
                </button>
              )}
            </div>
          </>
        )}

        {launchProgress && (
          <div className="ws-progress-bar-wrap">
            <span>
              {launchProgress.cancelled
                ? t("ws.cancelled")
                : `${launchProgress.completed}/${launchProgress.total} ${launchProgress.currentName}`}
            </span>
            <div className="ws-progress-track">
              <div
                className="ws-progress-fill"
                style={{
                  width: `${
                    launchProgress.total > 0
                      ? Math.round((launchProgress.completed / launchProgress.total) * 100)
                      : 0
                  }%`,
                }}
              />
            </div>
          </div>
        )}

        {errorMsg && (
          <div className="ws-banner-error">
            <span style={{ overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
              {errorMsg}
            </span>
            <button className="ws-mini-btn" onClick={() => setErrorMsg(null)}>
              <X size={12} />
            </button>
          </div>
        )}

        {statusNotice && !errorMsg && (
          <div
            className="ws-progress-bar-wrap"
            style={{ borderColor: "rgba(52, 211, 153, 0.35)", color: "#a7f3d0" }}
          >
            <Check size={12} />
            <span>{statusNotice}</span>
          </div>
        )}

        <div className="ws-scroll">
          {showOverview ? (
            filteredWorkspaces.length === 0 ? (
              <div className="ws-empty">
                <div>{workspaces.length === 0 ? t("ws.emptyList") : t("ws.noMatches")}</div>
                {workspaces.length === 0 && (
                  <button className="ws-action-btn primary" onClick={openCreateWsSheet}>
                    <Plus size={12} />
                    <span>{t("ws.newWorkspace")}</span>
                  </button>
                )}
              </div>
            ) : (
              <div className="ws-overview-grid">
                {filteredWorkspaces.map((ws) => {
                  const hasImage = isImageWorkspaceIcon(ws.icon);
                  return (
                  <div
                    key={ws.id}
                    role="button"
                    tabIndex={0}
                    className={`ws-overview-card${hasImage ? " has-image" : ""}`}
                    onClick={() => enterWorkspace(ws)}
                    onKeyDown={(e) => {
                      if (e.key === "Enter" || e.key === " ") enterWorkspace(ws);
                    }}
                    title={ws.name}
                  >
                    <button
                      type="button"
                      className="ws-mini-btn ws-overview-edit"
                      onClick={(e) => {
                        e.stopPropagation();
                        openEditWsSheet(ws);
                      }}
                      title={t("ws.editWorkspace")}
                      aria-label={t("ws.editWorkspace")}
                    >
                      <Pencil size={11} />
                    </button>
                    <div className="ws-overview-cover">
                      <WorkspaceAvatar icon={ws.icon} size="lg" />
                    </div>
                    <div className="ws-overview-meta">
                      <div className="ws-overview-name">{ws.name}</div>
                      <div className="ws-overview-count">
                        {ws.resourceCount === 1
                          ? t("ws.itemCountOne")
                          : t("ws.itemCount", { count: ws.resourceCount })}
                      </div>
                    </div>
                  </div>
                  );
                })}
                <button
                  type="button"
                  className="ws-overview-card is-new"
                  onClick={openCreateWsSheet}
                >
                  <Plus size={18} />
                  <span style={{ fontSize: 11, fontWeight: 500 }}>
                    {t("ws.newWorkspace")}
                  </span>
                </button>
              </div>
            )
          ) : filteredResources.length === 0 ? (
            <div className="ws-empty">
              <div>
                {resources.length === 0 ? t("ws.emptyWorkspace") : t("ws.noMatches")}
              </div>
              {resources.length === 0 && activeWorkspace && (
                <button className="ws-action-btn" onClick={openAddResSheet}>
                  <Plus size={12} />
                  <span>{t("ws.addResource")}</span>
                </button>
              )}
            </div>
          ) : (
            <div className={viewMode === "grid" ? "ws-grid" : "ws-list"}>
              {filteredResources.map((res, idx) => {
                const avail = availability[res.id];
                const unavailable = avail && !avail.available;
                const launched = justLaunchedId === res.id;
                return (
                  <div
                    key={res.id}
                    className={`ws-card ${launched ? "launched" : ""} ${
                      unavailable ? "unavailable" : ""
                    }`}
                    onClick={() => handleLaunchSingle(res)}
                    title={
                      unavailable
                        ? `${res.target} — ${avail.reason || "Target unavailable"}`
                        : res.target
                    }
                  >
                    <div className="ws-res-icon">
                      {launched ? <Check size={15} /> : <ResourceGlyph resource={res} />}
                      {unavailable && <span className="ws-missing-dot" />}
                    </div>
                    <div className="ws-res-meta">
                      <span className="ws-res-name">{res.name}</span>
                      <span className="ws-res-sub">{formatDomainOrTarget(res)}</span>
                    </div>
                    <div className="ws-card-actions" onClick={(e) => e.stopPropagation()}>
                      {mode === "window" && !search.trim() && (
                        <>
                          {idx > 0 && (
                            <button
                              className="ws-mini-btn"
                              onClick={() => handleMoveResource(idx, -1)}
                              title="Move up"
                            >
                              <ArrowUp size={11} />
                            </button>
                          )}
                          {idx < resources.length - 1 && (
                            <button
                              className="ws-mini-btn"
                              onClick={() => handleMoveResource(idx, 1)}
                              title="Move down"
                            >
                              <ArrowDown size={11} />
                            </button>
                          )}
                        </>
                      )}
                      <button
                        className="ws-mini-btn"
                        onClick={() => openEditResSheet(res)}
                        title={t("ws.edit")}
                      >
                        <Pencil size={11} />
                      </button>
                      <button
                        className="ws-mini-btn danger"
                        onClick={() => handleRemoveResource(res)}
                        title={t("ws.remove")}
                      >
                        <Trash2 size={11} />
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>

      {dropActive && (
        <div className="ws-drop-overlay">
          <Plus size={20} />
          <span>{t("ws.dropToAdd", { name: activeWorkspace?.name || t("panel.workspace") })}</span>
        </div>
      )}

      {(sheet.kind === "create-ws" || sheet.kind === "edit-ws") && (
        <div className="ws-sheet">
          <form className="ws-sheet-card" onSubmit={handleSaveWorkspace}>
            <div className="ws-sheet-head">
              <span>
                {sheet.kind === "create-ws" ? t("ws.newWorkspace") : t("ws.editWorkspace")}
              </span>
              <button
                type="button"
                className="ws-mini-btn"
                onClick={() => setSheet({ kind: "none" })}
              >
                <X size={14} />
              </button>
            </div>

            <div>
              <label className="ws-field-label">{t("ws.name")}</label>
              <input
                className="ws-input"
                value={wsName}
                maxLength={60}
                autoFocus
                onChange={(e) => setWsName(e.target.value)}
                placeholder="Study, Design, Work..."
              />
            </div>

            <div>
              <label className="ws-field-label">{t("ws.icon")}</label>
              <div className="ws-emoji-row">
                {EMOJI_PRESETS.map((em) => (
                  <button
                    key={em}
                    type="button"
                    className={`ws-emoji-chip ${wsIcon === em ? "active" : ""}`}
                    onClick={() => setWsIcon(em)}
                  >
                    {em}
                  </button>
                ))}
                <button
                  type="button"
                  className="ws-action-btn"
                  onClick={handlePickCustomWsIcon}
                  title="Choose custom icon image"
                >
                  <ImagePlus size={12} />
                  <span>{t("ws.image")}</span>
                </button>
              </div>
            </div>

            {sheet.kind === "edit-ws" && sheet.confirmDelete && (
              <div className="ws-note-warning">{t("ws.deleteWarning")}</div>
            )}

            <div className="ws-sheet-foot">
              {sheet.kind === "edit-ws" && !sheet.confirmDelete && workspaces.length > 1 && (
                <button
                  type="button"
                  className="ws-action-btn danger"
                  style={{ marginRight: "auto" }}
                  onClick={() => setSheet({ ...sheet, confirmDelete: true })}
                >
                  <Trash2 size={12} />
                  <span>{t("ws.delete")}</span>
                </button>
              )}
              {sheet.kind === "edit-ws" && sheet.confirmDelete && (
                <button
                  type="button"
                  className="ws-action-btn danger"
                  style={{ marginRight: "auto" }}
                  onClick={() => handleDeleteWorkspace(sheet.workspace)}
                >
                  <span>{t("ws.confirmDelete")}</span>
                </button>
              )}
              <button
                type="button"
                className="ws-action-btn"
                onClick={() => setSheet({ kind: "none" })}
              >
                {t("ws.cancel")}
              </button>
              <button type="submit" className="ws-action-btn primary">
                {t("ws.save")}
              </button>
            </div>
          </form>
        </div>
      )}

      {(sheet.kind === "add-res" || sheet.kind === "edit-res") && (
        <div className="ws-sheet">
          <form className="ws-sheet-card" onSubmit={handleSaveResource}>
            <div className="ws-sheet-head">
              <span>
                {sheet.kind === "add-res" ? t("ws.addResource") : t("ws.editResource")}
              </span>
              <button
                type="button"
                className="ws-mini-btn"
                onClick={() => setSheet({ kind: "none" })}
              >
                <X size={14} />
              </button>
            </div>

            <div className="ws-type-pills">
              {(["application", "file", "folder", "link"] as ResourceType[]).map((kind) => (
                <button
                  key={kind}
                  type="button"
                  className={`ws-type-pill ${resType === kind ? "active" : ""}`}
                  onClick={() => setResType(kind)}
                >
                  {t(`ws.type.${kind}`)}
                </button>
              ))}
            </div>

            <div>
              <label className="ws-field-label">{t("ws.name")}</label>
              <input
                className="ws-input"
                value={resName}
                maxLength={100}
                onChange={(e) => setResName(e.target.value)}
                placeholder={resType === "link" ? "GitHub, Docs..." : "Display name"}
              />
            </div>

            <div>
              <label className="ws-field-label">
                {resType === "link" ? t("ws.url") : t("ws.path")}
              </label>
              <div className="ws-target-row">
                <input
                  className="ws-input"
                  value={resTarget}
                  autoFocus
                  onChange={(e) => {
                    const clean = sanitizeTarget(e.target.value);
                    setResTarget(clean);
                  }}
                  onPaste={(e) => {
                    const pasted = e.clipboardData.getData("text");
                    if (pasted) {
                      e.preventDefault();
                      const clean = sanitizeTarget(pasted);
                      setResTarget(clean);
                      const inferred = inferMetaFromTarget(clean);
                      if (inferred.type && sheet.kind === "add-res") {
                        setResType(inferred.type);
                      }
                      if (!resName.trim() && inferred.name) {
                        setResName(inferred.name);
                      }
                    }
                  }}
                  placeholder={
                    resType === "link" ? "https://example.com" : "C:\\Path\\To\\Target"
                  }
                />
                <button
                  type="button"
                  className="ws-action-btn"
                  onClick={handlePasteTarget}
                  title={t("ws.pasteTooltip")}
                >
                  <Clipboard size={12} />
                  <span>{t("ws.paste")}</span>
                </button>
                {resType !== "link" && (
                  <button
                    type="button"
                    className="ws-action-btn"
                    onClick={handleBrowseResource}
                  >
                    {t("ws.browse")}
                  </button>
                )}
              </div>
            </div>

            <div className="ws-sheet-foot">
              <button
                type="button"
                className="ws-action-btn"
                onClick={() => setSheet({ kind: "none" })}
              >
                {t("ws.cancel")}
              </button>
              <button type="submit" className="ws-action-btn primary">
                {t("ws.save")}
              </button>
            </div>
          </form>
        </div>
      )}

      {sheet.kind === "summary" && (
        <div className="ws-sheet">
          <div className="ws-sheet-card">
            <div className="ws-sheet-head">
              <span style={{ display: "inline-flex", alignItems: "center", gap: 6 }}>
                <AlertTriangle size={14} color="#fbbf24" />
                {t("ws.launchFailures", {
                  launched: sheet.summary.launched,
                  total: sheet.summary.total,
                })}
              </span>
              <button className="ws-mini-btn" onClick={() => setSheet({ kind: "none" })}>
                <X size={14} />
              </button>
            </div>
            <div className="ws-scroll" style={{ maxHeight: 190 }}>
              <div className="ws-list">
                {sheet.summary.results
                  .filter((r) => !r.success)
                  .map((r) => (
                    <div key={r.resourceId} className="ws-card unavailable" style={{ cursor: "default" }}>
                      <div className="ws-res-meta">
                        <span className="ws-res-name">{r.name}</span>
                        <span className="ws-res-sub">{r.errorMessage || r.errorCode}</span>
                      </div>
                    </div>
                  ))}
              </div>
            </div>
            <div className="ws-sheet-foot">
              <button
                className="ws-action-btn"
                onClick={() => setSheet({ kind: "none" })}
              >
                {t("ws.close")}
              </button>
              <button
                className="ws-action-btn primary"
                onClick={() => {
                  const failedIds = sheet.summary.results
                    .filter((r) => !r.success)
                    .map((r) => r.resourceId);
                  setSheet({ kind: "none" });
                  handleLaunchBatch(failedIds);
                }}
              >
                <RefreshCw size={12} />
                <span>{t("ws.retryFailed")}</span>
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
