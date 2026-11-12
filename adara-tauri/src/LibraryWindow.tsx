import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import ReactDOM from "react-dom/client";
import { invoke } from "@tauri-apps/api/core";
import { listen } from "@tauri-apps/api/event";
import { Effect, EffectState } from "@tauri-apps/api/window";
import { getCurrentWebviewWindow } from "@tauri-apps/api/webviewWindow";
import {
  ArrowLeft,
  BookOpen,
  Bot,
  Edit3,
  ExternalLink,
  Eye,
  EyeOff,
  FolderKanban,
  Image as ImageIcon,
  LayoutDashboard,
  Library as LibraryIcon,
  Maximize2,
  Minimize2,
  Play,
  Plus,
  RefreshCw,
  Search,
  Trash2,
  Upload,
  X,
} from "lucide-react";
import { WindowControls } from "./components/WindowControls";
import { LibraryReader } from "./components/LibraryReader";
import { initTheme } from "./theme";
import {
  BOOK_CATEGORIES,
  BOOK_STATUSES,
  formatBookTitle,
  formatBytes,
  libAddBookFromPath,
  libDeleteBook,
  libGetCoverDataUrl,
  libListBooks,
  libPickBookFile,
  libPickCoverImage,
  libUpdateBook,
  openWorkspaceWindow,
  type LibraryBookDto,
} from "./libraryApi";
import "./surfaces.css";
import "./Library.css";
import { initClickSounds } from "./clickSounds";

initClickSounds();

type LibraryTab = "dashboard" | "collection";
type PdfMode = "flip" | "scroll";
type AiProvider = "gemini" | "chatgpt" | "claude" | "perplexity";

const AI_PROVIDERS: Record<AiProvider, { label: string; url: string }> = {
  gemini: { label: "Gemini", url: "https://gemini.google.com/app" },
  chatgpt: { label: "ChatGPT", url: "https://chatgpt.com" },
  claude: { label: "Claude", url: "https://claude.ai" },
  perplexity: { label: "Perplexity", url: "https://www.perplexity.ai" },
};

function BookCoverCard({
  book,
  coverUrl,
  onOpen,
  onQuickCover,
  onEdit,
  onDelete,
}: {
  book: LibraryBookDto;
  coverUrl: string | null;
  onOpen: () => void;
  onQuickCover: () => void;
  onEdit: () => void;
  onDelete: () => void;
}) {
  const displayTitle = formatBookTitle(book.title);
  const pct = Math.min(100, Math.max(0, Math.round(book.readingProgress || 0)));
  const isResumable = pct > 0 && pct < 100;
  const statusCls =
    book.status === "Reading"
      ? "reading"
      : book.status === "Finished"
      ? "finished"
      : book.status === "Want to Read"
      ? "want"
      : "";

  return (
    <article className="lib-book-card">
      <div className="lib-book-stage" onClick={onOpen} title={`Read ${displayTitle}`}>
        <div className="lib-book-3d">
          <div className="lib-book-spine" />
          {coverUrl ? (
            <img className="lib-book-cover-img" src={coverUrl} alt={displayTitle} draggable={false} />
          ) : (
            <div className="lib-book-cover-gen">
              <div className="lib-book-cover-inlay">
                <div className="lib-cover-top">
                  <span>{book.fileType.toUpperCase()}</span>
                  <span>{book.category}</span>
                </div>
                <div>
                  <h4 className="lib-cover-title">{displayTitle}</h4>
                  <p className="lib-cover-author">{book.author || "Unknown Author"}</p>
                </div>
                <span className="lib-cover-press">MY LITTLE LIBRARY</span>
              </div>
            </div>
          )}
          <div className="lib-book-pages-edge" />
        </div>
      </div>

      <div className="lib-book-details">
        <div>
          <div className="lib-badges">
            <span className={`lib-badge ${statusCls}`}>{book.status}</span>
            <span className="lib-badge">{book.fileType.toUpperCase()}</span>
            <span className="lib-badge">{book.category}</span>
            <span className="lib-badge">{formatBytes(book.fileSize)}</span>
          </div>
          <h3 className="lib-card-title" title={book.title}>
            {displayTitle}
          </h3>
          <p className="lib-card-author">{book.author || "Unknown Author"}</p>

          {pct > 0 && (
            <div className="lib-progress-wrap">
              <div className="lib-progress-meta">
                <span>
                  {book.fileType === "pdf" && book.currentPage > 0
                    ? `Page ${book.currentPage}${book.totalPages ? ` of ${book.totalPages}` : ""}`
                    : `${pct}% read`}
                </span>
                <span>{pct}%</span>
              </div>
              <div className="lib-progress-track">
                <div className="lib-progress-fill" style={{ width: `${pct}%` }} />
              </div>
            </div>
          )}
        </div>

        <div className="lib-card-actions">
          <button
            type="button"
            className="lib-btn primary sm"
            onClick={onOpen}
          >
            {isResumable ? <Play size={11} /> : <BookOpen size={11} />}
            <span>{isResumable ? "Resume" : "Read"}</span>
          </button>
          <div className="lib-card-tools">
            <button
              type="button"
              className="lib-icon-btn"
              onClick={onQuickCover}
              title="Set / change cover artwork"
            >
              <ImageIcon size={13} />
            </button>
            <button
              type="button"
              className="lib-icon-btn"
              onClick={onEdit}
              title="Edit metadata"
            >
              <Edit3 size={13} />
            </button>
            <button
              type="button"
              className="lib-icon-btn danger"
              onClick={onDelete}
              title="Remove book"
            >
              <Trash2 size={13} />
            </button>
          </div>
        </div>
      </div>
    </article>
  );
}

function LibraryWindowApp() {
  const [books, setBooks] = useState<LibraryBookDto[]>([]);
  const [covers, setCovers] = useState<Record<string, string | null>>({});
  const [tab, setTab] = useState<LibraryTab>("dashboard");
  const [search, setSearch] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("All");
  const [statusFilter, setStatusFilter] = useState("All");
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [dropActive, setDropActive] = useState(false);

  // Window sizing & Reader state
  const [activeBookId, setActiveBookId] = useState<string | null>(null);
  const [pdfMode, setPdfMode] = useState<PdfMode>("flip");
  const [focusMode, setFocusMode] = useState(true);
  const [chromePeek, setChromePeek] = useState(false);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [isMaximized, setIsMaximized] = useState(false);

  // AI Split Pane state (native child WebView2)
  const [aiOpen, setAiOpen] = useState(false);
  const [aiProvider, setAiProvider] = useState<AiProvider>("gemini");
  const [aiWidth, setAiWidth] = useState(440);
  const [aiStatus, setAiStatus] = useState<{
    state: "idle" | "loading" | "ready" | "error";
    error: string | null;
  }>({ state: "idle", error: null });
  const aiSlotRef = useRef<HTMLDivElement>(null);
  const aiPaneHostRef = useRef<HTMLElement>(null);
  const isResizingRef = useRef(false);

  // Form Modal state
  const [formOpen, setFormOpen] = useState(false);
  const [editingBook, setEditingBook] = useState<LibraryBookDto | null>(null);
  const [title, setTitle] = useState("");
  const [author, setAuthor] = useState("");
  const [category, setCategory] = useState<string>("Any");
  const [status, setStatus] = useState<string>("New");
  const [notes, setNotes] = useState("");
  const [pickedFile, setPickedFile] = useState<{ path: string; name: string; size: number } | null>(null);
  const [coverDataUrl, setCoverDataUrl] = useState<string | null>(null);
  const [clearCoverFlag, setClearCoverFlag] = useState(false);
  const [saving, setSaving] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<LibraryBookDto | null>(null);

  useEffect(() => initTheme(), []);

  useEffect(() => {
    const appWindow = getCurrentWebviewWindow();
    const syncWinFlags = () => {
      appWindow.isFullscreen().then(setIsFullscreen).catch(() => {});
      appWindow.isMaximized().then(setIsMaximized).catch(() => {});
    };
    syncWinFlags();
    const unlistenResize = appWindow.onResized(syncWinFlags);
    appWindow
      .setEffects({ effects: [Effect.Acrylic], state: EffectState.Active, color: [10, 10, 15, 20] })
      .catch(() => {})
      .finally(() => {
        requestAnimationFrame(() =>
          requestAnimationFrame(() => invoke("on_demand_window_ready").catch(() => {}))
        );
      });
    return () => {
      unlistenResize.then((fn) => fn());
    };
  }, []);

  const toggleFullscreen = useCallback(() => {
    const appWindow = getCurrentWebviewWindow();
    setIsFullscreen((prev) => {
      const next = !prev;
      void appWindow.setFullscreen(next).catch(() => {});
      return next;
    });
  }, []);

  useEffect(() => {
    const onGlobalKey = (e: KeyboardEvent) => {
      if (e.key === "F11") {
        e.preventDefault();
        toggleFullscreen();
      }
    };
    window.addEventListener("keydown", onGlobalKey);
    return () => window.removeEventListener("keydown", onGlobalKey);
  }, [toggleFullscreen]);

  useEffect(() => {
    const unlisten = listen<{ state?: string; url?: string; error?: string | null }>(
      "lib-ai-status",
      (event) => {
        const s = event.payload.state;
        if (s === "ready") {
          setAiStatus({ state: "ready", error: null });
        } else if (s === "error") {
          setAiStatus({
            state: "error",
            error: event.payload.error || "Unable to connect to AI provider.",
          });
        } else if (s === "loading") {
          setAiStatus({ state: "loading", error: null });
        }
      }
    );
    return () => {
      void unlisten.then((fn) => fn());
    };
  }, []);

  const loadBooks = useCallback(async () => {
    try {
      const list = await libListBooks();
      setBooks(list);
      for (const b of list) {
        if (b.hasCover) {
          void libGetCoverDataUrl(b.id).then((url) => {
            setCovers((prev) => ({ ...prev, [b.id]: url }));
          });
        } else {
          setCovers((prev) => ({ ...prev, [b.id]: null }));
        }
      }
    } catch (err) {
      setErrorMsg(err instanceof Error ? err.message : String(err));
    }
  }, []);

  useEffect(() => {
    void loadBooks();
  }, [loadBooks]);

  const activeBook = useMemo(
    () => books.find((b) => b.id === activeBookId) ?? null,
    [books, activeBookId]
  );

  // Drag & drop PDF / EPUB files directly onto Library window
  useEffect(() => {
    const win = getCurrentWebviewWindow();
    const unlisten = win.onDragDropEvent((event) => {
      const p = event.payload;
      if (p.type === "enter" || p.type === "over") {
        if (!activeBookId) setDropActive(true);
      } else if (p.type === "leave") {
        setDropActive(false);
      } else if (p.type === "drop") {
        setDropActive(false);
        if (activeBookId) return;
        const firstBookPath = p.paths.find((item) => /\.(pdf|epub)$/i.test(item));
        if (!firstBookPath) {
          setErrorMsg("Drop a .pdf or .epub file to add it to your library.");
          return;
        }
        const name = firstBookPath.split(/[\\/]/).filter(Boolean).pop() || firstBookPath;
        setEditingBook(null);
        setPickedFile({ path: firstBookPath, name, size: 0 });
        setTitle(formatBookTitle(name));
        setAuthor("");
        setCategory("Any");
        setStatus("New");
        setNotes("");
        setCoverDataUrl(null);
        setClearCoverFlag(false);
        setFormOpen(true);
      }
    });
    return () => {
      void unlisten.then((fn) => fn());
    };
  }, [activeBookId]);

  // Focus Mode shortcuts (`F` toggles focus chrome, `Escape` reveals chrome or closes reader)
  useEffect(() => {
    if (!activeBook) return;
    const onKeyDown = (e: KeyboardEvent) => {
      const target = e.target as HTMLElement | null;
      if (target && (target.tagName === "INPUT" || target.tagName === "TEXTAREA")) return;
      if (e.key === "f" || e.key === "F") {
        setFocusMode((prev) => !prev);
      } else if (e.key === "Escape") {
        if (focusMode && !chromePeek) {
          setChromePeek(true);
        } else {
          setActiveBookId(null);
          setAiOpen(false);
        }
      }
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [activeBook, focusMode, chromePeek]);

  // Sync native Win32 child AI WebView2 bounds
  const syncAiPane = useCallback(() => {
    const visible = Boolean(activeBook && aiOpen);
    const slot = aiSlotRef.current;
    if (!visible || !slot) {
      void invoke("lib_ai_sync", {
        visible: false,
        x: 0,
        y: 0,
        width: 0,
        height: 0,
        url: null,
      }).catch(() => {});
      return;
    }
    const rect = slot.getBoundingClientRect();
    const dpr = window.devicePixelRatio || 1;
    void invoke("lib_ai_sync", {
      visible: true,
      x: Math.round(rect.left * dpr),
      y: Math.round(rect.top * dpr),
      width: Math.max(0, Math.round(rect.width * dpr)),
      height: Math.max(0, Math.round(rect.height * dpr)),
      url: AI_PROVIDERS[aiProvider].url,
    }).catch(() => {});
  }, [activeBook, aiOpen, aiProvider]);

  useEffect(() => {
    if (activeBook && aiOpen) {
      setAiStatus((prev) =>
        prev.state === "error" ? prev : { state: "loading", error: null }
      );
    }
    syncAiPane();
    const slot = aiSlotRef.current;
    if (!slot || !aiOpen || !activeBook) return;
    const ro = new ResizeObserver(() => syncAiPane());
    ro.observe(slot);
    window.addEventListener("resize", syncAiPane);
    return () => {
      ro.disconnect();
      window.removeEventListener("resize", syncAiPane);
    };
  }, [syncAiPane, aiOpen, activeBook]);

  // Splitter dragging for AI split pane
  useEffect(() => {
    const onMove = (e: MouseEvent) => {
      if (!isResizingRef.current) return;
      const next = Math.max(280, Math.min(window.innerWidth - 360, window.innerWidth - e.clientX));
      setAiWidth(next);
    };
    const onUp = () => {
      isResizingRef.current = false;
    };
    window.addEventListener("mousemove", onMove);
    window.addEventListener("mouseup", onUp);
    return () => {
      window.removeEventListener("mousemove", onMove);
      window.removeEventListener("mouseup", onUp);
    };
  }, []);

  const openAddModal = () => {
    setEditingBook(null);
    setPickedFile(null);
    setTitle("");
    setAuthor("");
    setCategory("Any");
    setStatus("New");
    setNotes("");
    setCoverDataUrl(null);
    setClearCoverFlag(false);
    setFormOpen(true);
  };

  const openEditModal = async (book: LibraryBookDto) => {
    setEditingBook(book);
    setPickedFile(null);
    setTitle(book.title);
    setAuthor(book.author);
    setCategory(book.category);
    setStatus(book.status);
    setNotes(book.notes);
    setClearCoverFlag(false);
    const existing = covers[book.id] ?? (book.hasCover ? await libGetCoverDataUrl(book.id) : null);
    setCoverDataUrl(existing);
    setFormOpen(true);
  };

  const handleChooseBookFile = async () => {
    const picked = await libPickBookFile().catch(() => null);
    if (!picked) return;
    setPickedFile(picked);
    if (!title.trim()) setTitle(formatBookTitle(picked.name));
  };

  const handleChooseCover = async () => {
    const dataUrl = await libPickCoverImage().catch(() => null);
    if (!dataUrl) return;
    setCoverDataUrl(dataUrl);
    setClearCoverFlag(false);
  };

  const handleQuickCover = async (book: LibraryBookDto) => {
    const dataUrl = await libPickCoverImage().catch(() => null);
    if (!dataUrl) return;
    const updated = await libUpdateBook({
      id: book.id,
      title: book.title,
      author: book.author,
      category: book.category,
      status: book.status,
      notes: book.notes,
      coverDataUrl: dataUrl,
    });
    setBooks((prev) => prev.map((b) => (b.id === updated.id ? updated : b)));
    setCovers((prev) => ({ ...prev, [updated.id]: dataUrl }));
  };

  const handleSaveForm = async (e: React.FormEvent) => {
    e.preventDefault();
    if (saving) return;
    setErrorMsg(null);
    setSaving(true);
    try {
      if (editingBook) {
        const updated = await libUpdateBook({
          id: editingBook.id,
          title,
          author,
          category,
          status,
          notes,
          coverDataUrl: clearCoverFlag ? null : coverDataUrl,
          clearCover: clearCoverFlag,
        });
        setBooks((prev) => prev.map((b) => (b.id === updated.id ? updated : b)));
        setCovers((prev) => ({
          ...prev,
          [updated.id]: clearCoverFlag ? null : coverDataUrl,
        }));
      } else {
        if (!pickedFile) {
          setErrorMsg("Please select a .pdf or .epub file.");
          setSaving(false);
          return;
        }
        const created = await libAddBookFromPath({
          title: title.trim() || formatBookTitle(pickedFile.name),
          author,
          category,
          status,
          notes,
          sourcePath: pickedFile.path,
          coverDataUrl,
        });
        setBooks((prev) => [created, ...prev]);
        if (coverDataUrl) {
          setCovers((prev) => ({ ...prev, [created.id]: coverDataUrl }));
        }
      }
      setFormOpen(false);
    } catch (err) {
      setErrorMsg(err instanceof Error ? err.message : String(err));
    } finally {
      setSaving(false);
    }
  };

  const handleConfirmDelete = async () => {
    if (!deleteTarget) return;
    try {
      await libDeleteBook(deleteTarget.id);
      setBooks((prev) => prev.filter((b) => b.id !== deleteTarget.id));
      if (activeBookId === deleteTarget.id) {
        setActiveBookId(null);
        setAiOpen(false);
      }
      setDeleteTarget(null);
    } catch (err) {
      setErrorMsg(err instanceof Error ? err.message : String(err));
    }
  };

  const stats = useMemo(() => {
    const reading = books.filter((b) => b.status === "Reading").length;
    const finished = books.filter((b) => b.status === "Finished").length;
    const totalBytes = books.reduce((acc, b) => acc + (b.fileSize || 0), 0);
    return { total: books.length, reading, finished, totalBytes };
  }, [books]);

  const recentBooks = useMemo(
    () => books.filter((b) => b.lastReadAt || b.status === "Reading").slice(0, 6),
    [books]
  );

  const filteredBooks = useMemo(() => {
    const q = search.trim().toLowerCase();
    return books.filter((b) => {
      if (categoryFilter !== "All" && b.category !== categoryFilter) return false;
      if (statusFilter !== "All" && b.status !== statusFilter) return false;
      if (!q) return true;
      return (
        b.title.toLowerCase().includes(q) ||
        b.author.toLowerCase().includes(q) ||
        b.notes.toLowerCase().includes(q)
      );
    });
  }, [books, search, categoryFilter, statusFilter]);

  return (
    <div
      className={`lib-shell${isFullscreen ? " is-fullscreen" : ""}${
        isMaximized ? " is-maximized" : ""
      }`}
    >
      {(!activeBook || !focusMode) && (
        <header className="lib-titlebar" data-tauri-drag-region>
          <div className="lib-titlebar-left">
            <LibraryIcon size={15} />
            <span>Adara Box — My Little Library</span>
          </div>
          <div className="lib-titlebar-actions">
            <button
              type="button"
              className="lib-btn sm"
              onClick={() => openWorkspaceWindow().catch(() => {})}
              title="Open Workspace Window"
            >
              <FolderKanban size={12} />
              <span>Workspace</span>
            </button>
            <button
              type="button"
              className={`lib-btn sm ${isFullscreen ? "primary" : ""}`}
              onClick={toggleFullscreen}
              title={isFullscreen ? "Exit Fullscreen (F11)" : "Fullscreen (F11)"}
            >
              {isFullscreen ? <Minimize2 size={12} /> : <Maximize2 size={12} />}
              <span>{isFullscreen ? "Windowed" : "Fullscreen"}</span>
            </button>
            <WindowControls allowMaximize />
          </div>
        </header>
      )}

      {activeBook ? (
        <div
          className={`lib-reader-shell ${focusMode ? "focus-mode" : ""}`}
          onMouseMove={(e) => {
            if (!focusMode) return;
            const targetNode = e.target as Node | null;
            const inAiPane = Boolean(aiPaneHostRef.current && targetNode && aiPaneHostRef.current.contains(targetNode));
            if (!inAiPane && e.clientY <= 14) setChromePeek(true);
            else if (chromePeek && e.clientY > 92) setChromePeek(false);
          }}
        >
          <div
            className={`lib-reader-bar ${
              focusMode ? (chromePeek ? "visible-chrome" : "hidden-chrome") : ""
            }`}
          >
            <div style={{ display: "flex", alignItems: "center", gap: 10, minWidth: 0 }}>
              <button
                type="button"
                className="lib-btn sm"
                onClick={() => {
                  setActiveBookId(null);
                  setAiOpen(false);
                }}
              >
                <ArrowLeft size={13} />
                <span>Library</span>
              </button>
              <span
                style={{
                  fontSize: 12.5,
                  fontWeight: 600,
                  overflow: "hidden",
                  textOverflow: "ellipsis",
                  whiteSpace: "nowrap",
                }}
              >
                {formatBookTitle(activeBook.title)}
              </span>
            </div>

            <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
              {activeBook.fileType === "pdf" && (
                <button
                  type="button"
                  className="lib-btn sm"
                  onClick={() => setPdfMode((m) => (m === "flip" ? "scroll" : "flip"))}
                  title="Switch between 3D Flipbook spread and Continuous Scroll"
                >
                  <BookOpen size={12} />
                  <span>{pdfMode === "flip" ? "3D Flipbook" : "Scroll"}</span>
                </button>
              )}

              <button
                type="button"
                className={`lib-btn sm ${focusMode ? "primary" : ""}`}
                onClick={() => {
                  setFocusMode((v) => !v);
                  setChromePeek(false);
                }}
                title="Toggle Focus Mode (F)"
              >
                {focusMode ? <EyeOff size={12} /> : <Eye size={12} />}
                <span>Focus</span>
              </button>

              <button
                type="button"
                className={`lib-btn sm ${aiOpen ? "primary" : ""}`}
                onClick={() => setAiOpen((v) => !v)}
                title="Toggle AI Reading Companion Split Pane"
              >
                <Bot size={12} />
                <span>AI Companion</span>
              </button>

              {focusMode && (
                <>
                  <button
                    type="button"
                    className={`lib-btn sm ${isFullscreen ? "primary" : ""}`}
                    onClick={toggleFullscreen}
                    title={isFullscreen ? "Exit Fullscreen (F11)" : "Fullscreen (F11)"}
                  >
                    {isFullscreen ? <Minimize2 size={12} /> : <Maximize2 size={12} />}
                  </button>
                  <WindowControls allowMaximize />
                </>
              )}
            </div>
          </div>

          {focusMode && !chromePeek && (
            <button
              type="button"
              className="lib-focus-reveal-btn"
              style={aiOpen ? { right: aiWidth + 20 } : undefined}
              onClick={() => setChromePeek(true)}
              title="Show reader controls (or press F)"
            >
              <Eye size={12} /> Controls
            </button>
          )}

          <div className="lib-reader-split">
            <div className="lib-reader-stage-wrap">
              <LibraryReader
                book={activeBook}
                coverDataUrl={covers[activeBook.id] ?? null}
                pdfMode={pdfMode}
                onBookUpdated={(updated) => {
                  setBooks((prev) => prev.map((b) => (b.id === updated.id ? updated : b)));
                }}
              />
            </div>

            {aiOpen && (
              <>
                <div
                  className="lib-splitter"
                  onMouseDown={() => {
                    isResizingRef.current = true;
                  }}
                />
                <aside ref={aiPaneHostRef} className="lib-ai-pane-host" style={{ width: aiWidth }}>
                  <div className="lib-ai-toolbar">
                    <div className="lib-ai-providers">
                      {(Object.keys(AI_PROVIDERS) as AiProvider[]).map((key) => (
                        <button
                          key={key}
                          type="button"
                          className={`lib-ai-chip ${aiProvider === key ? "active" : ""}`}
                          onClick={() => {
                            setAiStatus({ state: "loading", error: null });
                            setAiProvider(key);
                          }}
                        >
                          {AI_PROVIDERS[key].label}
                        </button>
                      ))}
                    </div>
                    <div style={{ display: "flex", alignItems: "center", gap: 4 }}>
                      <button
                        type="button"
                        className="lib-icon-btn"
                        onClick={() => {
                          setAiStatus({ state: "loading", error: null });
                          void invoke("lib_ai_reload").catch(() => {});
                        }}
                        title="Reload AI companion"
                      >
                        <RefreshCw size={12} />
                      </button>
                      <button
                        type="button"
                        className="lib-icon-btn"
                        onClick={() =>
                          void invoke("lib_ai_open_external", {
                            url: AI_PROVIDERS[aiProvider].url,
                          }).catch(() => {})
                        }
                        title="Open in default browser"
                      >
                        <ExternalLink size={12} />
                      </button>
                      <button
                        type="button"
                        className="lib-icon-btn"
                        onClick={() => setAiOpen(false)}
                        title="Close AI companion"
                      >
                        <X size={13} />
                      </button>
                    </div>
                  </div>
                  <div ref={aiSlotRef} className="lib-ai-viewport-slot">
                    {aiStatus.state === "error" ? (
                      <div className="lib-ai-fallback">
                        <p className="lib-ai-fallback-title">
                          Could not connect to {AI_PROVIDERS[aiProvider].label}
                        </p>
                        <p className="lib-ai-fallback-desc">{aiStatus.error}</p>
                        <div className="lib-ai-fallback-actions">
                          <button
                            type="button"
                            className="lib-btn primary sm"
                            onClick={() => {
                              setAiStatus({ state: "loading", error: null });
                              void invoke("lib_ai_reload").catch(() => {});
                            }}
                          >
                            <RefreshCw size={12} />
                            <span>Retry</span>
                          </button>
                          <button
                            type="button"
                            className="lib-btn sm"
                            onClick={() =>
                              void invoke("lib_ai_open_external", {
                                url: AI_PROVIDERS[aiProvider].url,
                              }).catch(() => {})
                            }
                          >
                            <ExternalLink size={12} />
                            <span>Open in Browser</span>
                          </button>
                        </div>
                      </div>
                    ) : (
                      <span>
                        Connecting to {AI_PROVIDERS[aiProvider].label}…
                      </span>
                    )}
                  </div>
                </aside>
              </>
            )}
          </div>
        </div>
      ) : (
        <div className="lib-main-layout">
          <aside className="lib-sidebar">
            <div className="lib-nav-group">
              <span className="lib-nav-label">Library</span>
              <button
                type="button"
                className={`lib-nav-item ${tab === "dashboard" ? "active" : ""}`}
                onClick={() => setTab("dashboard")}
              >
                <span className="lib-nav-item-left">
                  <LayoutDashboard size={14} />
                  <span>Overview</span>
                </span>
              </button>
              <button
                type="button"
                className={`lib-nav-item ${tab === "collection" ? "active" : ""}`}
                onClick={() => setTab("collection")}
              >
                <span className="lib-nav-item-left">
                  <BookOpen size={14} />
                  <span>Collection</span>
                </span>
                <span className="lib-nav-count">{books.length}</span>
              </button>
            </div>

            <div className="lib-nav-group">
              <span className="lib-nav-label">Categories</span>
              {["All", ...BOOK_CATEGORIES].map((cat) => (
                <button
                  key={cat}
                  type="button"
                  className={`lib-nav-item ${
                    tab === "collection" && categoryFilter === cat ? "active" : ""
                  }`}
                  onClick={() => {
                    setCategoryFilter(cat);
                    setTab("collection");
                  }}
                >
                  <span>{cat}</span>
                  <span className="lib-nav-count">
                    {cat === "All"
                      ? books.length
                      : books.filter((b) => b.category === cat).length}
                  </span>
                </button>
              ))}
            </div>

            <div className="lib-sidebar-footer">
              <button type="button" className="lib-btn primary" onClick={openAddModal}>
                <Plus size={14} />
                <span>Add Book</span>
              </button>
            </div>
          </aside>

          <main className="lib-content">
            <div className="lib-scroll">
              {errorMsg && (
                <div
                  style={{
                    marginBottom: 12,
                    padding: "8px 12px",
                    borderRadius: 8,
                    background: "rgba(239, 68, 68, 0.16)",
                    border: "1px solid rgba(239, 68, 68, 0.35)",
                    color: "#fca5a5",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                    fontSize: 12,
                  }}
                >
                  <span>{errorMsg}</span>
                  <button
                    type="button"
                    className="lib-icon-btn"
                    onClick={() => setErrorMsg(null)}
                  >
                    <X size={13} />
                  </button>
                </div>
              )}

              {tab === "dashboard" && (
                <>
                  <div className="lib-header-row">
                    <div>
                      <h1 className="lib-heading">Personal Reading Room</h1>
                      <p className="lib-subtitle">
                        Offline PDF 3D flipbook &amp; EPUB library stored safely inside Adara Box.
                      </p>
                    </div>
                    <button type="button" className="lib-btn primary" onClick={openAddModal}>
                      <Plus size={14} />
                      <span>Add PDF / EPUB</span>
                    </button>
                  </div>

                  <div className="lib-stats-grid">
                    <div className="lib-stat-card">
                      <span className="lib-stat-label">Total Books</span>
                      <span className="lib-stat-value">{stats.total}</span>
                    </div>
                    <div className="lib-stat-card">
                      <span className="lib-stat-label">Currently Reading</span>
                      <span className="lib-stat-value">{stats.reading}</span>
                    </div>
                    <div className="lib-stat-card">
                      <span className="lib-stat-label">Finished</span>
                      <span className="lib-stat-value">{stats.finished}</span>
                    </div>
                    <div className="lib-stat-card">
                      <span className="lib-stat-label">Library Size</span>
                      <span className="lib-stat-value">{formatBytes(stats.totalBytes)}</span>
                    </div>
                  </div>

                  {books.length === 0 ? (
                    <div className="lib-dropzone" onClick={openAddModal}>
                      <Upload size={22} style={{ marginBottom: 6, opacity: 0.7 }} />
                      <div>Your shelf is empty — drag &amp; drop a PDF or EPUB file here, or click to browse.</div>
                    </div>
                  ) : (
                    <>
                      <div className="lib-header-row" style={{ marginBottom: 12 }}>
                        <h2 style={{ margin: 0, fontSize: 15, fontWeight: 700 }}>
                          {recentBooks.length > 0 ? "Continue Reading" : "Recently Added"}
                        </h2>
                        <button
                          type="button"
                          className="lib-btn sm"
                          onClick={() => setTab("collection")}
                        >
                          View All ({books.length})
                        </button>
                      </div>
                      <div className="lib-books-grid">
                        {(recentBooks.length > 0 ? recentBooks : books.slice(0, 6)).map((book) => (
                          <BookCoverCard
                            key={book.id}
                            book={book}
                            coverUrl={covers[book.id] ?? null}
                            onOpen={() => setActiveBookId(book.id)}
                            onQuickCover={() => void handleQuickCover(book)}
                            onEdit={() => void openEditModal(book)}
                            onDelete={() => setDeleteTarget(book)}
                          />
                        ))}
                      </div>
                    </>
                  )}
                </>
              )}

              {tab === "collection" && (
                <>
                  <div className="lib-header-row">
                    <div>
                      <h1 className="lib-heading">Collection</h1>
                      <p className="lib-subtitle">
                        Showing {filteredBooks.length} of {books.length} books
                      </p>
                    </div>
                    <button type="button" className="lib-btn primary" onClick={openAddModal}>
                      <Plus size={14} />
                      <span>Add Book</span>
                    </button>
                  </div>

                  <div className="lib-filters">
                    <div className="lib-search-box">
                      <Search size={13} className="lib-search-icon" />
                      <input
                        className="lib-input"
                        value={search}
                        onChange={(e) => setSearch(e.target.value)}
                        placeholder="Search title, author, or notes..."
                      />
                    </div>
                    <select
                      className="lib-select"
                      value={categoryFilter}
                      onChange={(e) => setCategoryFilter(e.target.value)}
                    >
                      <option value="All">All categories</option>
                      {BOOK_CATEGORIES.map((c) => (
                        <option key={c} value={c}>
                          {c}
                        </option>
                      ))}
                    </select>
                    <select
                      className="lib-select"
                      value={statusFilter}
                      onChange={(e) => setStatusFilter(e.target.value)}
                    >
                      <option value="All">All statuses</option>
                      {BOOK_STATUSES.map((s) => (
                        <option key={s} value={s}>
                          {s}
                        </option>
                      ))}
                    </select>
                  </div>

                  {filteredBooks.length === 0 ? (
                    <div className="lib-dropzone" onClick={openAddModal}>
                      No books match your filters. Drop a PDF/EPUB or click to add a book.
                    </div>
                  ) : (
                    <div className="lib-books-grid">
                      {filteredBooks.map((book) => (
                        <BookCoverCard
                          key={book.id}
                          book={book}
                          coverUrl={covers[book.id] ?? null}
                          onOpen={() => setActiveBookId(book.id)}
                          onQuickCover={() => void handleQuickCover(book)}
                          onEdit={() => void openEditModal(book)}
                          onDelete={() => setDeleteTarget(book)}
                        />
                      ))}
                    </div>
                  )}
                </>
              )}
            </div>

            {dropActive && (
              <div className="lib-drop-overlay">
                <Upload size={26} />
                <span>Drop PDF or EPUB to add to My Little Library</span>
              </div>
            )}
          </main>
        </div>
      )}

      {formOpen && (
        <div className="lib-modal-backdrop" onClick={() => setFormOpen(false)}>
          <form
            className="lib-modal-card"
            onClick={(e) => e.stopPropagation()}
            onSubmit={handleSaveForm}
          >
            <div className="lib-modal-head">
              <h2 className="lib-modal-title">{editingBook ? "Edit Book" : "Add Book"}</h2>
              <button
                type="button"
                className="lib-icon-btn"
                onClick={() => setFormOpen(false)}
              >
                <X size={15} />
              </button>
            </div>

            {!editingBook && (
              <div>
                <label className="lib-field-label">Document File (PDF or EPUB)</label>
                <div
                  className={`lib-dropzone ${pickedFile ? "active" : ""}`}
                  onClick={() => void handleChooseBookFile()}
                >
                  {pickedFile ? (
                    <div>
                      <strong>{pickedFile.name}</strong>
                      {pickedFile.size > 0 && (
                        <div style={{ opacity: 0.7, fontSize: 11 }}>{formatBytes(pickedFile.size)}</div>
                      )}
                    </div>
                  ) : (
                    <span>Click to select a .pdf or .epub file</span>
                  )}
                </div>
              </div>
            )}

            <div>
              <label className="lib-field-label">Title</label>
              <input
                className="lib-input"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                required
                maxLength={200}
                placeholder="Enter book title"
              />
            </div>

            <div>
              <label className="lib-field-label">Author</label>
              <input
                className="lib-input"
                value={author}
                onChange={(e) => setAuthor(e.target.value)}
                maxLength={120}
                placeholder="Author name (optional)"
              />
            </div>

            <div className="lib-form-grid">
              <div>
                <label className="lib-field-label">Category</label>
                <select
                  className="lib-select"
                  style={{ width: "100%" }}
                  value={category}
                  onChange={(e) => setCategory(e.target.value)}
                >
                  {BOOK_CATEGORIES.map((c) => (
                    <option key={c} value={c}>
                      {c}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="lib-field-label">Reading Status</label>
                <select
                  className="lib-select"
                  style={{ width: "100%" }}
                  value={status}
                  onChange={(e) => setStatus(e.target.value)}
                >
                  {BOOK_STATUSES.map((s) => (
                    <option key={s} value={s}>
                      {s}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div>
              <label className="lib-field-label">Cover Artwork (optional)</label>
              <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                <button
                  type="button"
                  className="lib-btn sm"
                  onClick={() => void handleChooseCover()}
                >
                  <ImageIcon size={12} />
                  <span>{coverDataUrl ? "Change Cover" : "Choose Cover Image"}</span>
                </button>
                {coverDataUrl && (
                  <button
                    type="button"
                    className="lib-btn sm"
                    onClick={() => {
                      setCoverDataUrl(null);
                      setClearCoverFlag(true);
                    }}
                  >
                    Remove Cover
                  </button>
                )}
              </div>
            </div>

            <div>
              <label className="lib-field-label">Notes</label>
              <textarea
                className="lib-textarea"
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                maxLength={2000}
                placeholder="Personal reading notes..."
              />
            </div>

            <div style={{ display: "flex", justifyContent: "flex-end", gap: 8 }}>
              <button
                type="button"
                className="lib-btn"
                onClick={() => setFormOpen(false)}
              >
                Cancel
              </button>
              <button type="submit" className="lib-btn primary" disabled={saving}>
                {saving ? "Saving..." : editingBook ? "Save Changes" : "Add to Library"}
              </button>
            </div>
          </form>
        </div>
      )}

      {deleteTarget && (
        <div className="lib-modal-backdrop" onClick={() => setDeleteTarget(null)}>
          <div className="lib-modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="lib-modal-head">
              <h2 className="lib-modal-title">Remove Book</h2>
              <button
                type="button"
                className="lib-icon-btn"
                onClick={() => setDeleteTarget(null)}
              >
                <X size={15} />
              </button>
            </div>
            <p style={{ margin: 0, fontSize: 12.5, lineHeight: 1.5, opacity: 0.85 }}>
              Remove <strong>{formatBookTitle(deleteTarget.title)}</strong> from your Adara Box
              library? This deletes the copy inside Adara Box storage (your original file elsewhere
              on disk is untouched).
            </p>
            <div style={{ display: "flex", justifyContent: "flex-end", gap: 8 }}>
              <button
                type="button"
                className="lib-btn"
                onClick={() => setDeleteTarget(null)}
              >
                Cancel
              </button>
              <button
                type="button"
                className="lib-btn danger"
                onClick={() => void handleConfirmDelete()}
              >
                Remove Book
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

ReactDOM.createRoot(document.getElementById("root") as HTMLElement).render(
  <React.StrictMode>
    <LibraryWindowApp />
  </React.StrictMode>
);
