# Adara Box

<p align="center">
  <img src="adara-tauri/public/adara.png" alt="Adara Box Logo" width="96" />
</p>

<p align="center">
  <strong>A unified Windows desktop command shell combining a top-edge Dynamic Notch, an application/file Workspace launcher, and an interactive 3D Document Library.</strong>
</p>

<p align="center">
  Built with <strong>Tauri v2</strong> · <strong>Rust</strong> · <strong>React + TypeScript</strong> · <strong>SQLite</strong> · <strong>WebView2</strong>
</p>

---

## Overview

**Adara Box** merges three projects into a single native-feeling Windows 10/11 desktop utility:

- **Dynamic Notch & Dock Shell** (based on *Nectar*): Always-reachable top notch and customizable Windows dock with glass/solid theming, remembered last-opened panel state, corner status widgets, media playback integration, CalDAV calendar/timers, and quick Control Panel toggles.
- **Adara Workspace**: Organize projects into dedicated workspaces with full-cover artwork, drag-and-drop folders/files/executables/URLs, quick reordering, and local SQLite persistence (`%LOCALAPPDATA%\AdaraBox\adarabox.db`).
- **My Little Library**: Built-in PDF (`.pdf`) and EPUB (`.epub`) manager with 3D Flipbook rendering, window-aware responsive reader framing (normal, maximized, and fullscreen modes without clipping or slide glitches), and a docked **AI Study Companion**.

---

## Key Features

### 1. Dynamic Notch (Last-Opened Memory & Panel Order)
- **Last Opened, First Opened**: Automatically remembers the last active Notch panel when your cursor leaves and reopens that exact tab next time you hover in (configurable in Settings).
- **Default Panel Flow**: `Workspace` → `Stats` → `Media` → `Calendar` → `Control Panel` (with `Shelf` optional and disabled by default).
- **Widget Placement Settings**: Customize Notch panel ordering, toggle individual panels (including `Shelf`) on or off, and arrange corner status widgets across left/right Notch wings.

### 2. Live PC Hardware Stats (`Stats` Panel)
Real-time native Win32 & DXGI system telemetry polled directly from the Rust backend:
- **CPU**: Processor model, logical thread count, and live system utilization.
- **RAM**: Live memory load percentage with used/total GB readout.
- **Disk (`C:\`)**: Used/total capacity and free GB remaining.
- **GPU**: Hardware adapter name and DXGI local VRAM usage/capacity.
- **Network, Power & Uptime**: Live `↑ Upload / ↓ Download` throughput, Wi-Fi/Ethernet status, battery state, and system uptime.

### 3. Monochrome Workspace Canvas
- Full-bleed square workspace cover cards inside both the Notch view and the full Dashboard window.
- Instant native Windows launching for `.exe` shortcuts, folders, documents, and web links.
- Atomic SQLite backups and `.adarabox` archive export/import support.

### 4. Interactive 3D Library & AI Companion
- PDF 3D flipbook + EPUB chapter reader (`pdfjs-dist` & `epubjs`).
- Integrated Maximize / Minimize / Fullscreen window controls and non-overlapping navigation chrome.
- Dockable WebView2 AI Study Companion panel with quick provider switching and external browser fallback.

### 5. Synthesized Audio Feedback (Python)
- Tactile UI click sound presets (`Crisp Tactile`, `Soft Pop`, `Mechanical Key`) and a smooth **Startup Rising Chime** generated offline using pure Python stdlib (`wave`, `math`, `struct`) via `adara-tauri/scripts/generate_click_sounds.py`.
- Independently toggleable and previewable in **Settings → General**.

---

## Repository Structure

```text
Adara-box/
├── adara-tauri/                  # Active unified Tauri v2 application
│   ├── src/                      # React + TypeScript frontend (Notch, Dock, Workspace, Library, Settings)
│   │   ├── components/           # Notch panels (SystemStatsPanel, NotchWorkspace, NotchLibrary, ...)
│   │   ├── settings/             # Tabbed Settings UI (General, Appearance, Notch, Dock, Overlays, About)
│   │   ├── locales/              # Multi-language UI localization JSON files
│   │   └── clickSounds.ts        # Click & startup chime audio player + SQLite sync
│   ├── src-tauri/                # Rust backend & Win32 / DXGI / WebView2 integrations
│   │   ├── src/commands.rs       # Tauri IPC commands, Win32 controls & hardware telemetry
│   │   ├── src/workspace/        # SQLite schema, CRUD, launcher & archive handling
│   │   └── src/library/          # Document shelf, EPUB/PDF indexing & reader helpers
│   ├── scripts/
│   │   └── generate_click_sounds.py  # Offline WAV synthesizer for click & startup sounds
│   ├── installer/                # NSIS packaging hooks (bundles WebView2Loader.dll)
│   └── package.json
└── AdaraBox/                     # Legacy .NET 8 prototype reference
```

---

## Development & Build

### Prerequisites
- **Windows 10 / 11 (x64)** with Microsoft Edge WebView2 Runtime
- **Bun** (v1.x+)
- **Rust** (`stable-x86_64-pc-windows-gnu` toolchain with `llvm-mingw` / `lld` configured in `adara-tauri/src-tauri/.cargo/config.toml`)
- **Python 3** (optional, only needed to regenerate `.wav` audio assets)

### Commands

```powershell
# 1. Install frontend dependencies
cd adara-tauri
bun install

# 2. (Optional) Regenerate click & startup WAV assets with Python
python scripts/generate_click_sounds.py

# 3. Run frontend type-check & production bundle check
bun run build

# 4. Run Rust unit tests
cd src-tauri
cargo +stable-x86_64-pc-windows-gnu test --bin adara-box

# 5. Launch development build
cd ..
bun tauri dev

# 6. Build Windows NSIS installer
bun run tauri build --bundles nsis
```

The NSIS installer outputs to:
`adara-tauri/src-tauri/target/release/bundle/nsis/Adara Box_<version>_x64-setup.exe`

---

## Credits & Acknowledgements

**Adara Box** brings together:
- **Adara Workspace** — workspace launcher & visual project canvas
- **Nectar** — dynamic Windows Notch & Dock shell foundation
- **My Little Library** — personal 3D book reader & study companion

Licensed under **GPL-3.0-only**.
