import { useCallback, useEffect, useRef, useState } from "react";
import { ChevronLeft, ChevronRight, Minus, Plus, RotateCcw } from "lucide-react";
import {
  formatBookTitle,
  libGetBookBytes,
  libSaveProgress,
  type LibraryBookDto,
} from "../libraryApi";

interface PdfViewportLike {
  width: number;
  height: number;
}

interface PdfPageLike {
  getViewport: (opts: { scale: number }) => PdfViewportLike;
  render: (opts: { canvasContext: CanvasRenderingContext2D; viewport: PdfViewportLike }) => {
    promise: Promise<void>;
  };
}

interface PdfDocumentLike {
  numPages: number;
  getPage: (num: number) => Promise<PdfPageLike>;
  destroy?: () => Promise<void> | void;
}

interface PageFlipUiLike {
  distElement: HTMLElement;
  getMousePos: (clientX: number, clientY: number) => { x: number; y: number };
}

interface PageFlipLike {
  loadFromHTML: (pages: NodeListOf<Element> | HTMLElement[]) => void;
  on: (event: string, cb: (e: { data: number }) => void) => void;
  flipPrev: () => void;
  flipNext: () => void;
  destroy: () => void;
  getUI?: () => PageFlipUiLike;
}

interface EpubLocationEvent {
  start?: {
    cfi?: string;
    percentage?: number;
  };
}

interface EpubRenditionLike {
  display: (target?: string) => Promise<void>;
  prev: () => void;
  next: () => void;
  on: (event: string, cb: (loc: EpubLocationEvent) => void) => void;
  currentLocation: () => EpubLocationEvent | null;
  destroy: () => void;
}

interface EpubBookLike {
  renderTo: (el: HTMLElement | string, opts: Record<string, unknown>) => EpubRenditionLike;
  ready: Promise<void>;
  locations: {
    generate: (chars: number) => Promise<void>;
    length: () => number;
    percentageFromCfi: (cfi: string) => number;
  };
  destroy: () => void;
}

declare global {
  interface Window {
    pdfjsLib?: {
      GlobalWorkerOptions: { workerSrc: string };
      getDocument: (src: { data: Uint8Array | ArrayBuffer }) => { promise: Promise<PdfDocumentLike> };
    };
    St?: {
      PageFlip: new (container: HTMLElement, opts: Record<string, unknown>) => PageFlipLike;
    };
    JSZip?: unknown;
    ePub?: (data: ArrayBuffer) => EpubBookLike;
  }
}

export interface LibraryReaderProps {
  book: LibraryBookDto;
  coverDataUrl: string | null;
  pdfMode: "flip" | "scroll";
  onBookUpdated: (updated: LibraryBookDto) => void;
}

const MIN_ZOOM = 0.5;
const MAX_ZOOM = 2.5;
const ZOOM_STEP = 0.15;

function escapeHtml(raw: string): string {
  return raw
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

export function LibraryReader({
  book,
  coverDataUrl,
  pdfMode,
  onBookUpdated,
}: LibraryReaderProps) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [counterLabel, setCounterLabel] = useState("...");
  const [zoom, setZoom] = useState(1.0);
  const [ctrlHeld, setCtrlHeld] = useState(false);

  const stageHostRef = useRef<HTMLDivElement>(null);
  const flipWrapperRef = useRef<HTMLDivElement>(null);
  const flipContainerRef = useRef<HTMLDivElement>(null);
  const scrollStageRef = useRef<HTMLDivElement>(null);
  const epubHostRef = useRef<HTMLDivElement>(null);

  const flipInstanceRef = useRef<PageFlipLike | null>(null);
  const epubBookRef = useRef<EpubBookLike | null>(null);
  const epubRenditionRef = useRef<EpubRenditionLike | null>(null);
  const pdfDocRef = useRef<PdfDocumentLike | null>(null);
  const zoomRef = useRef(1.0);
  const saveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const pendingProgressRef = useRef<{
    currentPage?: number;
    totalPages?: number;
    currentCfi?: string;
    readingProgress?: number;
  } | null>(null);
  const onBookUpdatedRef = useRef(onBookUpdated);
  onBookUpdatedRef.current = onBookUpdated;

  const flushProgress = useCallback(
    async (
      id: string,
      payload: {
        currentPage?: number;
        totalPages?: number;
        currentCfi?: string;
        readingProgress?: number;
      }
    ) => {
      try {
        const updated = await libSaveProgress({ id, ...payload });
        onBookUpdatedRef.current(updated);
      } catch {
        // Ignore transient save errors while paging rapidly
      }
    },
    []
  );

  const scheduleProgressSave = useCallback(
    (
      id: string,
      payload: {
        currentPage?: number;
        totalPages?: number;
        currentCfi?: string;
        readingProgress?: number;
      }
    ) => {
      pendingProgressRef.current = payload;
      if (saveTimerRef.current) clearTimeout(saveTimerRef.current);
      saveTimerRef.current = setTimeout(() => {
        const p = pendingProgressRef.current;
        pendingProgressRef.current = null;
        if (p) flushProgress(id, p);
      }, 450);
    },
    [flushProgress]
  );

  const applyZoom = useCallback((next: number) => {
    const clamped = Math.max(MIN_ZOOM, Math.min(MAX_ZOOM, Math.round(next * 100) / 100));
    zoomRef.current = clamped;
    setZoom(clamped);
    if (flipContainerRef.current) {
      flipContainerRef.current.style.transform = `scale(${clamped})`;
    }
  }, []);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    zoomRef.current = 1.0;
    setZoom(1.0);

    const cleanupInstances = () => {
      if (saveTimerRef.current) {
        clearTimeout(saveTimerRef.current);
        saveTimerRef.current = null;
      }
      if (pendingProgressRef.current) {
        const pending = pendingProgressRef.current;
        pendingProgressRef.current = null;
        void flushProgress(book.id, pending);
      }
      if (flipInstanceRef.current) {
        try {
          flipInstanceRef.current.destroy();
        } catch {
          // ignore
        }
        flipInstanceRef.current = null;
      }
      if (epubRenditionRef.current) {
        try {
          epubRenditionRef.current.destroy();
        } catch {
          // ignore
        }
        epubRenditionRef.current = null;
      }
      if (epubBookRef.current) {
        try {
          epubBookRef.current.destroy();
        } catch {
          // ignore
        }
        epubBookRef.current = null;
      }
      if (pdfDocRef.current) {
        try {
          void pdfDocRef.current.destroy?.();
        } catch {
          // ignore
        }
        pdfDocRef.current = null;
      }
      if (flipContainerRef.current) {
        flipContainerRef.current.innerHTML = "";
      }
      if (scrollStageRef.current) {
        scrollStageRef.current.innerHTML = "";
      }
      if (epubHostRef.current) {
        epubHostRef.current.innerHTML = "";
      }
    };

    const loadBook = async () => {
      try {
        const buf = await libGetBookBytes(book.id);
        if (cancelled) return;

        if (book.fileType === "pdf") {
          if (!window.pdfjsLib) {
            throw new Error("Offline PDF engine (pdf.min.js) failed to load.");
          }
          window.pdfjsLib.GlobalWorkerOptions.workerSrc = "/library/vendor/pdf.worker.min.js";
          const pdfDoc = await window.pdfjsLib.getDocument({ data: new Uint8Array(buf) }).promise;
          if (cancelled) {
            void pdfDoc.destroy?.();
            return;
          }
          pdfDocRef.current = pdfDoc;
          const totalPages = pdfDoc.numPages;
          const targetPage = Math.max(
            0,
            Math.min(totalPages, book.currentPage > 0 ? book.currentPage : 0)
          );

          if (pdfMode === "flip") {
            const container = flipContainerRef.current;
            const stage = stageHostRef.current;
            if (!container || !stage) return;

            if (!window.St?.PageFlip) {
              throw new Error("Offline 3D Flipbook engine (page-flip.browser.js) failed to load.");
            }

            const firstPage = await pdfDoc.getPage(1);
            const vp1 = firstPage.getViewport({ scale: 1.0 });
            const aspect = vp1.height / vp1.width;

            const availW = Math.max(360, (stage.clientWidth || window.innerWidth) - 72);
            const availH = Math.max(320, (stage.clientHeight || window.innerHeight) - 104);
            let singleWidth = Math.floor(Math.min(availW / 2, availH / aspect));
            singleWidth = Math.max(200, Math.min(540, singleWidth));
            const singleHeight = Math.max(260, Math.floor(singleWidth * aspect));

            container.style.width = `${singleWidth * 2}px`;
            container.style.height = `${singleHeight}px`;
            container.style.transform = "scale(1)";

            const displayTitle = formatBookTitle(book.title);
            const rendered = new Set<number>();

            const frontCover = document.createElement("div");
            frontCover.className = "flipbook-page flipbook-cover-page";
            frontCover.dataset.density = "hard";
            if (coverDataUrl) {
              const img = document.createElement("img");
              img.className = "flipbook-cover-artwork";
              img.src = coverDataUrl;
              img.alt = "Cover";
              frontCover.appendChild(img);
            } else {
              frontCover.innerHTML = `
                <div class="flipbook-cover-inlay">
                  <div class="lib-cover-top"><span>MY LITTLE LIBRARY</span><span>PDF</span></div>
                  <div>
                    <div class="flipbook-cover-title">${escapeHtml(displayTitle)}</div>
                    <div class="flipbook-cover-author">${escapeHtml(book.author || "Unknown Author")}</div>
                  </div>
                  <div class="lib-cover-press">ADARA BOX EDITION</div>
                </div>
              `;
            }
            container.appendChild(frontCover);

            for (let i = 1; i <= totalPages; i++) {
              const sideClass = i % 2 === 1 ? "flipbook-page--right" : "flipbook-page--left";
              const pageDiv = document.createElement("div");
              pageDiv.className = `flipbook-page ${sideClass}`;
              pageDiv.dataset.density = "soft";
              pageDiv.innerHTML = `
                <div class="flipbook-page-loading" id="libPageLoad-${i}">Rendering page ${i}…</div>
                <canvas class="flipbook-canvas hidden" id="libCanvas-${i}"></canvas>
                <span class="flipbook-page-footer">${i}</span>
              `;
              container.appendChild(pageDiv);
            }

            if (totalPages % 2 !== 0) {
              const endpaper = document.createElement("div");
              endpaper.className = "flipbook-page flipbook-page--left";
              endpaper.dataset.density = "soft";
              endpaper.innerHTML = `<span class="flipbook-page-footer">—</span>`;
              container.appendChild(endpaper);
            }

            const backCover = document.createElement("div");
            backCover.className = "flipbook-page flipbook-cover-page";
            backCover.dataset.density = "hard";
            backCover.innerHTML = `
              <div class="flipbook-cover-inlay" style="justify-content:center;align-items:center;">
                <div class="lib-cover-press">EX LIBRIS • ADARA BOX</div>
              </div>
            `;
            container.appendChild(backCover);

            const pageElements = container.querySelectorAll(".flipbook-page");
            const flip = new window.St.PageFlip(container, {
              width: singleWidth,
              height: singleHeight,
              size: "fixed",
              minWidth: singleWidth,
              maxWidth: singleWidth * 2,
              minHeight: singleHeight,
              maxHeight: singleHeight * 2,
              drawShadow: true,
              maxShadowOpacity: 0.55,
              showCover: true,
              usePortrait: false,
              startPage: targetPage,
              startZIndex: 1,
              autoSize: false,
              showPageCorners: false,
              flippingTime: 620,
            });
            flip.loadFromHTML(pageElements);

            const ui = flip.getUI?.();
            if (ui) {
              ui.getMousePos = function (clientX: number, clientY: number) {
                const rect = this.distElement.getBoundingClientRect();
                return {
                  x: (clientX - rect.left) / zoomRef.current,
                  y: (clientY - rect.top) / zoomRef.current,
                };
              };
            }

            flipInstanceRef.current = flip;

            const renderPageCanvas = async (pageNum: number) => {
              if (cancelled || rendered.has(pageNum) || pageNum < 1 || pageNum > totalPages) {
                return;
              }
              rendered.add(pageNum);
              try {
                const page = await pdfDoc.getPage(pageNum);
                if (cancelled) return;
                const canvas = document.getElementById(`libCanvas-${pageNum}`) as HTMLCanvasElement | null;
                const loader = document.getElementById(`libPageLoad-${pageNum}`);
                if (!canvas) return;
                const baseVp = page.getViewport({ scale: 1.0 });
                const scale = Math.max(1.5, Math.min(2.5, (singleWidth / baseVp.width) * 1.5));
                const vp = page.getViewport({ scale });
                canvas.width = vp.width;
                canvas.height = vp.height;
                const ctx = canvas.getContext("2d");
                if (!ctx) return;
                await page.render({ canvasContext: ctx, viewport: vp }).promise;
                if (loader) loader.style.display = "none";
                canvas.classList.remove("hidden");
              } catch {
                // ignore individual page render cancellation
              }
            };

            const updateCounter = (idx: number) => {
              if (idx === 0) setCounterLabel("Cover");
              else if (idx > totalPages) setCounterLabel("Back Cover");
              else setCounterLabel(`Page ${idx} / ${totalPages}`);
            };

            updateCounter(targetPage);
            const firstVisible = Math.max(1, targetPage);
            await renderPageCanvas(firstVisible);
            void renderPageCanvas(firstVisible - 1);
            void renderPageCanvas(firstVisible + 1);
            void renderPageCanvas(firstVisible + 2);

            flip.on("flip", (e) => {
              const idx = e.data;
              updateCounter(idx);
              let pageNum = idx;
              let pct = 0;
              if (idx === 0) {
                pageNum = 1;
                pct = 0;
              } else if (idx > totalPages) {
                pageNum = totalPages;
                pct = 100;
              } else {
                pageNum = idx;
                pct = Math.min(100, Math.max(1, Math.round((idx / totalPages) * 100)));
              }
              scheduleProgressSave(book.id, {
                currentPage: pageNum,
                totalPages,
                readingProgress: pct,
              });
              for (let offset = -2; offset <= 3; offset++) {
                void renderPageCanvas(idx + offset);
              }
            });

            if (!cancelled) setLoading(false);
          } else {
            // Continuous Scroll PDF canvas viewer
            const scrollHost = scrollStageRef.current;
            if (!scrollHost) return;
            const rendered = new Set<number>();
            const pageBoxes: HTMLDivElement[] = [];

            const firstPage = await pdfDoc.getPage(1);
            const vp1 = firstPage.getViewport({ scale: 1.0 });
            const aspect = vp1.height / vp1.width;
            const pageW = Math.min(820, Math.max(360, (scrollHost.clientWidth || 860) - 64));
            const estH = Math.round(pageW * aspect);

            for (let i = 1; i <= totalPages; i++) {
              const box = document.createElement("div");
              box.className = "lib-pdf-scroll-page";
              box.dataset.pageNum = String(i);
              box.style.width = `${pageW}px`;
              box.style.height = `${estH}px`;
              box.innerHTML = `<span class="flipbook-page-loading">Page ${i}</span>`;
              scrollHost.appendChild(box);
              pageBoxes.push(box);
            }

            const renderScrollPage = async (pageNum: number) => {
              if (cancelled || rendered.has(pageNum)) return;
              rendered.add(pageNum);
              const page = await pdfDoc.getPage(pageNum);
              if (cancelled) return;
              const box = pageBoxes[pageNum - 1];
              if (!box) return;
              const baseVp = page.getViewport({ scale: 1.0 });
              const dpr = Math.min(2, window.devicePixelRatio || 1.25);
              const renderScale = (pageW / baseVp.width) * dpr;
              const vp = page.getViewport({ scale: renderScale });
              const canvas = document.createElement("canvas");
              canvas.width = vp.width;
              canvas.height = vp.height;
              canvas.style.width = "100%";
              canvas.style.height = "100%";
              const ctx = canvas.getContext("2d");
              if (!ctx) return;
              await page.render({ canvasContext: ctx, viewport: vp }).promise;
              box.innerHTML = "";
              box.appendChild(canvas);
            };

            const observer = new IntersectionObserver(
              (entries) => {
                for (const entry of entries) {
                  if (!entry.isIntersecting) continue;
                  const p = Number((entry.target as HTMLElement).dataset.pageNum || 1);
                  void renderScrollPage(p);
                  if (p < totalPages) void renderScrollPage(p + 1);
                  setCounterLabel(`Page ${p} / ${totalPages}`);
                  const pct = Math.min(100, Math.max(1, Math.round((p / totalPages) * 100)));
                  scheduleProgressSave(book.id, {
                    currentPage: p,
                    totalPages,
                    readingProgress: pct,
                  });
                }
              },
              { root: scrollHost, threshold: 0.25 }
            );

            pageBoxes.forEach((b) => observer.observe(b));
            const resumeIdx = Math.max(0, Math.min(totalPages - 1, (book.currentPage || 1) - 1));
            pageBoxes[resumeIdx]?.scrollIntoView({ block: "start" });
            setCounterLabel(`Page ${resumeIdx + 1} / ${totalPages}`);
            if (!cancelled) setLoading(false);
          }
        } else {
          // EPUB Reader
          if (!window.ePub || !epubHostRef.current) {
            throw new Error("Offline EPUB engine (epub.min.js) failed to load.");
          }
          const epub = window.ePub(buf);
          epubBookRef.current = epub;
          const rendition = epub.renderTo(epubHostRef.current, {
            width: "100%",
            height: "100%",
            spread: "none",
          });
          epubRenditionRef.current = rendition;

          rendition.on("relocated", (loc) => {
            if (cancelled || !loc?.start?.cfi) return;
            let pct = 0;
            if (epub.locations && epub.locations.length() > 0) {
              pct = Math.round(epub.locations.percentageFromCfi(loc.start.cfi) * 100);
            } else if (typeof loc.start.percentage === "number") {
              pct = Math.round(loc.start.percentage * 100);
            }
            const nextPct = Math.max(1, Math.min(100, pct || Math.round(book.readingProgress) || 1));
            setCounterLabel(`${nextPct}%`);
            scheduleProgressSave(book.id, {
              currentCfi: loc.start.cfi,
              readingProgress: nextPct,
            });
          });

          if (book.currentCfi) {
            await rendition.display(book.currentCfi);
          } else {
            await rendition.display();
          }

          setCounterLabel(
            book.readingProgress > 0 ? `${Math.round(book.readingProgress)}%` : "EPUB"
          );
          if (!cancelled) setLoading(false);

          epub.ready
            .then(() => epub.locations.generate(1000))
            .then(() => {
              if (cancelled) return;
              const loc = rendition.currentLocation();
              if (loc?.start?.cfi && epub.locations.length() > 0) {
                const pct = Math.max(
                  1,
                  Math.min(100, Math.round(epub.locations.percentageFromCfi(loc.start.cfi) * 100))
                );
                setCounterLabel(`${pct}%`);
                scheduleProgressSave(book.id, {
                  currentCfi: loc.start.cfi,
                  readingProgress: pct,
                });
              }
            })
            .catch(() => {});
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : String(err));
          setLoading(false);
        }
      }
    };

    void loadBook();
    return () => {
      cancelled = true;
      cleanupInstances();
    };
  }, [book.id, pdfMode, book.fileType, book.title, book.author, coverDataUrl, flushProgress, scheduleProgressSave]);

  // Ctrl + wheel zoom and Ctrl + drag panning in flipbook stage
  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.ctrlKey) setCtrlHeld(true);
      if (e.key === "ArrowLeft") {
        if (book.fileType === "pdf" && pdfMode === "flip") flipInstanceRef.current?.flipPrev();
        else if (book.fileType === "epub") epubRenditionRef.current?.prev();
      } else if (e.key === "ArrowRight") {
        if (book.fileType === "pdf" && pdfMode === "flip") flipInstanceRef.current?.flipNext();
        else if (book.fileType === "epub") epubRenditionRef.current?.next();
      } else if (e.ctrlKey && (e.key === "=" || e.key === "+")) {
        e.preventDefault();
        applyZoom(zoomRef.current + ZOOM_STEP);
      } else if (e.ctrlKey && e.key === "-") {
        e.preventDefault();
        applyZoom(zoomRef.current - ZOOM_STEP);
      } else if (e.ctrlKey && e.key === "0") {
        e.preventDefault();
        applyZoom(1.0);
      }
    };
    const onKeyUp = (e: KeyboardEvent) => {
      if (!e.ctrlKey) setCtrlHeld(false);
    };
    window.addEventListener("keydown", onKeyDown);
    window.addEventListener("keyup", onKeyUp);
    return () => {
      window.removeEventListener("keydown", onKeyDown);
      window.removeEventListener("keyup", onKeyUp);
    };
  }, [book.fileType, pdfMode, applyZoom]);

  // Panning handlers inside flipbook wrapper
  useEffect(() => {
    const wrapper = flipWrapperRef.current;
    const stage = stageHostRef.current;
    if (!wrapper || !stage || book.fileType !== "pdf" || pdfMode !== "flip") return;

    let isPanning = false;
    let startX = 0;
    let startY = 0;
    let scrollLeft = 0;
    let scrollTop = 0;

    const onWheel = (e: WheelEvent) => {
      if (e.ctrlKey) {
        e.preventDefault();
        applyZoom(zoomRef.current + (e.deltaY < 0 ? ZOOM_STEP : -ZOOM_STEP));
      }
    };
    const onMouseDown = (e: MouseEvent) => {
      if ((e.ctrlKey && e.button === 0) || e.button === 1) {
        e.preventDefault();
        e.stopPropagation();
        isPanning = true;
        wrapper.classList.add("is-panning");
        startX = e.clientX;
        startY = e.clientY;
        scrollLeft = wrapper.scrollLeft;
        scrollTop = wrapper.scrollTop;
      }
    };
    const onMouseMove = (e: MouseEvent) => {
      if (!isPanning) return;
      e.preventDefault();
      wrapper.scrollLeft = scrollLeft - (e.clientX - startX);
      wrapper.scrollTop = scrollTop - (e.clientY - startY);
    };
    const onMouseUp = () => {
      if (isPanning) {
        isPanning = false;
        wrapper.classList.remove("is-panning");
      }
    };

    stage.addEventListener("wheel", onWheel, { passive: false });
    wrapper.addEventListener("mousedown", onMouseDown, true);
    window.addEventListener("mousemove", onMouseMove);
    window.addEventListener("mouseup", onMouseUp);

    return () => {
      stage.removeEventListener("wheel", onWheel);
      wrapper.removeEventListener("mousedown", onMouseDown, true);
      window.removeEventListener("mousemove", onMouseMove);
      window.removeEventListener("mouseup", onMouseUp);
    };
  }, [book.fileType, pdfMode, applyZoom]);

  if (error) {
    return (
      <div className="flipbook-stage">
        <p style={{ color: "#fca5a5", fontSize: 13 }}>{error}</p>
      </div>
    );
  }

  if (book.fileType === "pdf" && pdfMode === "scroll") {
    return (
      <div className="lib-reader-stage-wrap">
        {loading && (
          <div className="flipbook-stage">
            <span className="flipbook-page-loading">Loading PDF document…</span>
          </div>
        )}
        <div
          ref={scrollStageRef}
          className="lib-pdf-scroll-stage"
          style={{ display: loading ? "none" : "flex" }}
        />
        {!loading && (
          <div className="flipbook-toolbar">
            <span className="flipbook-counter">{counterLabel}</span>
          </div>
        )}
      </div>
    );
  }

  if (book.fileType === "epub") {
    return (
      <div className="lib-epub-wrap">
        {loading && (
          <div className="flipbook-stage" style={{ position: "absolute", inset: 0, zIndex: 20 }}>
            <span className="flipbook-page-loading">Opening EPUB publication…</span>
          </div>
        )}
        <div ref={epubHostRef} className="lib-epub-viewport" />
        <button
          type="button"
          className="lib-epub-nav prev"
          onClick={() => epubRenditionRef.current?.prev()}
          aria-label="Previous page"
        >
          ‹
        </button>
        <button
          type="button"
          className="lib-epub-nav next"
          onClick={() => epubRenditionRef.current?.next()}
          aria-label="Next page"
        >
          ›
        </button>
        <div className="flipbook-toolbar">
          <button
            type="button"
            className="flipbook-btn"
            onClick={() => epubRenditionRef.current?.prev()}
          >
            <ChevronLeft size={12} /> Prev
          </button>
          <span className="flipbook-counter">{counterLabel}</span>
          <button
            type="button"
            className="flipbook-btn"
            onClick={() => epubRenditionRef.current?.next()}
          >
            Next <ChevronRight size={12} />
          </button>
        </div>
      </div>
    );
  }

  return (
    <div
      ref={stageHostRef}
      className={`flipbook-stage ${ctrlHeld ? "ctrl-active" : ""}`}
    >
      {loading && (
        <div className="flipbook-page-loading" style={{ position: "absolute", zIndex: 5 }}>
          Binding book pages…
        </div>
      )}
      <div
        ref={flipWrapperRef}
        className="flipbook-wrapper"
        style={{ opacity: loading ? 0 : 1 }}
      >
        <div ref={flipContainerRef} className="flipbook-container" />
      </div>

      {!loading && (
        <div className="flipbook-toolbar">
          <button
            type="button"
            className="flipbook-btn"
            onClick={() => flipInstanceRef.current?.flipPrev()}
          >
            <ChevronLeft size={12} /> Prev
          </button>
          <span className="flipbook-counter">{counterLabel}</span>
          <button
            type="button"
            className="flipbook-btn"
            onClick={() => flipInstanceRef.current?.flipNext()}
          >
            Next <ChevronRight size={12} />
          </button>

          <button
            type="button"
            className="flipbook-btn"
            onClick={() => applyZoom(zoom - ZOOM_STEP)}
            title="Zoom out"
          >
            <Minus size={12} />
          </button>
          <button
            type="button"
            className="flipbook-btn"
            onClick={() => applyZoom(1.0)}
            title="Reset zoom"
          >
            <RotateCcw size={11} /> {Math.round(zoom * 100)}%
          </button>
          <button
            type="button"
            className="flipbook-btn"
            onClick={() => applyZoom(zoom + ZOOM_STEP)}
            title="Zoom in"
          >
            <Plus size={12} />
          </button>
        </div>
      )}
    </div>
  );
}
