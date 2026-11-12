import { emit, listen } from "@tauri-apps/api/event";
import { invoke } from "@tauri-apps/api/core";

export type ClickSoundPreset = "crisp" | "soft" | "mechanical";

export const CLICK_SOUND_ENABLED_KEY = "nectar-click-sound-enabled";
export const CLICK_SOUND_PRESET_KEY = "nectar-click-sound-preset";

const PRESET_PATHS: Record<ClickSoundPreset, string> = {
  crisp: "/sounds/ui-click-crisp.wav",
  soft: "/sounds/ui-click-soft.wav",
  mechanical: "/sounds/ui-click-mechanical.wav",
};

const INTERACTIVE_SELECTOR = [
  "button:not(:disabled)",
  "a[href]",
  "summary",
  "[role='button']",
  "[role='tab']",
  "[role='switch']",
  "[role='menuitem']",
  "label.toggle-switch",
  "input[type='checkbox']:not(:disabled)",
  "input[type='radio']:not(:disabled)",
  "select:not(:disabled)",
  ".ws-card",
  ".ws-overview-card",
  ".lib-nav-item",
  ".lib-book-stage",
  ".dock-item",
  ".wc-btn:not(:disabled)",
].join(",");

let enabledCache: boolean | null = null;
let presetCache: ClickSoundPreset | null = null;
let initialized = false;
let audioCtx: AudioContext | null = null;
const bufferCache = new Map<ClickSoundPreset, AudioBuffer>();
const loadingCache = new Map<ClickSoundPreset, Promise<AudioBuffer | null>>();
let lastPlayMs = 0;

function normalizePreset(raw: string | null | undefined): ClickSoundPreset {
  if (raw === "soft" || raw === "mechanical" || raw === "crisp") return raw;
  return "crisp";
}

export function isClickSoundEnabled(): boolean {
  if (enabledCache !== null) return enabledCache;
  try {
    const val = localStorage.getItem(CLICK_SOUND_ENABLED_KEY);
    enabledCache = val !== "false";
  } catch {
    enabledCache = true;
  }
  return enabledCache;
}

export function getClickSoundPreset(): ClickSoundPreset {
  if (presetCache !== null) return presetCache;
  try {
    presetCache = normalizePreset(localStorage.getItem(CLICK_SOUND_PRESET_KEY));
  } catch {
    presetCache = "crisp";
  }
  return presetCache;
}

export function setClickSoundState(enabled: boolean, preset?: ClickSoundPreset): void {
  enabledCache = enabled;
  try {
    localStorage.setItem(CLICK_SOUND_ENABLED_KEY, String(enabled));
  } catch {
    // ignore storage failures
  }
  if (preset) {
    presetCache = preset;
    try {
      localStorage.setItem(CLICK_SOUND_PRESET_KEY, preset);
    } catch {
      // ignore
    }
    void preloadPreset(preset);
  }
}

export async function broadcastClickSoundChanged(): Promise<void> {
  try {
    await emit("click-sound-changed", {
      enabled: isClickSoundEnabled(),
      preset: getClickSoundPreset(),
    });
  } catch {
    // ignore if emitted from a single-window context
  }
}

function getContext(): AudioContext | null {
  if (typeof window === "undefined") return null;
  if (!audioCtx) {
    const Ctor = window.AudioContext || (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!Ctor) return null;
    audioCtx = new Ctor();
  }
  if (audioCtx.state === "suspended") {
    void audioCtx.resume().catch(() => {});
  }
  return audioCtx;
}

async function preloadPreset(preset: ClickSoundPreset): Promise<AudioBuffer | null> {
  const existing = bufferCache.get(preset);
  if (existing) return existing;
  const inflight = loadingCache.get(preset);
  if (inflight) return inflight;

  const ctx = getContext();
  if (!ctx) return null;

  const task = fetch(PRESET_PATHS[preset])
    .then((res) => (res.ok ? res.arrayBuffer() : Promise.reject(new Error("Missing click wav"))))
    .then((buf) => ctx.decodeAudioData(buf))
    .then((decoded) => {
      bufferCache.set(preset, decoded);
      return decoded;
    })
    .catch(() => null)
    .finally(() => {
      loadingCache.delete(preset);
    });

  loadingCache.set(preset, task);
  return task;
}

export function playClickSound(force = false, overridePreset?: ClickSoundPreset): void {
  if (!force && !isClickSoundEnabled()) return;
  const now = performance.now();
  if (!force && now - lastPlayMs < 20) return;
  lastPlayMs = now;

  const preset = overridePreset ?? getClickSoundPreset();
  const ctx = getContext();
  const cached = bufferCache.get(preset);
  if (ctx && cached) {
    try {
      const src = ctx.createBufferSource();
      const gain = ctx.createGain();
      gain.gain.value = 0.42;
      src.buffer = cached;
      src.connect(gain);
      gain.connect(ctx.destination);
      src.start(0);
      return;
    } catch {
      // fall through to HTMLAudio fallback
    }
  }

  void preloadPreset(preset).then((buf) => {
    if (!buf || !ctx) {
      try {
        const audio = new Audio(PRESET_PATHS[preset]);
        audio.volume = 0.4;
        void audio.play().catch(() => {});
      } catch {
        // ignore
      }
      return;
    }
    try {
      const src = ctx.createBufferSource();
      const gain = ctx.createGain();
      gain.gain.value = 0.42;
      src.buffer = buf;
      src.connect(gain);
      gain.connect(ctx.destination);
      src.start(0);
    } catch {
      // ignore
    }
  });
}

export function initClickSounds(): void {
  if (initialized || typeof document === "undefined") return;
  initialized = true;

  void preloadPreset(getClickSoundPreset());

  // Sync from SQLite persisted settings in case localStorage in a newly-created window is behind
  void invoke<Record<string, string>>("load_settings")
    .then((settings) => {
      const rawEnabled = settings[CLICK_SOUND_ENABLED_KEY];
      const rawPreset = settings[CLICK_SOUND_PRESET_KEY];
      const nextEnabled =
        rawEnabled !== undefined ? String(rawEnabled) !== "false" : isClickSoundEnabled();
      const nextPreset = rawPreset ? normalizePreset(String(rawPreset)) : getClickSoundPreset();
      setClickSoundState(nextEnabled, nextPreset);
    })
    .catch(() => {});

  void listen<{ enabled?: boolean; preset?: ClickSoundPreset }>("click-sound-changed", (event) => {
    const enabled = event.payload.enabled ?? isClickSoundEnabled();
    const preset = event.payload.preset ? normalizePreset(event.payload.preset) : getClickSoundPreset();
    setClickSoundState(enabled, preset);
  }).catch(() => {});

  window.addEventListener("storage", (e) => {
    if (e.key === CLICK_SOUND_ENABLED_KEY && e.newValue !== null) {
      enabledCache = e.newValue !== "false";
    } else if (e.key === CLICK_SOUND_PRESET_KEY && e.newValue !== null) {
      presetCache = normalizePreset(e.newValue);
      void preloadPreset(presetCache);
    }
  });

  document.addEventListener(
    "pointerdown",
    (e) => {
      if (e.button !== 0 || !isClickSoundEnabled()) return;
      const target = e.target as HTMLElement | null;
      if (!target) return;
      if (!target.closest(INTERACTIVE_SELECTOR)) return;
      playClickSound(false);
    },
    true
  );
}
