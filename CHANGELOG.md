# Changelog

All notable changes to the Adara Box project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.2.0] - 2026-10-31

### Added
- **Apple Design & Dark Neomorphism Overhaul**
  - Ultra-compact Top Bar height (30 DIP, matching macOS Menu Bar format) and expanded 640 DIP width.
  - Dark Neomorphism surface styling: matte obsidian `#16161B`, hairline top highlights `#25FFFFFF`, dual-layer ambient drop shadows, and tactile micro-press feedback (`scale 0.97`) on all interactive buttons and cards.
  - Fluid Pop-Down & Pop-Up sheet animations (`TranslateTransform.Y` + `Opacity` with `CubicEase`) for opening and closing module panels.
  - Pure high-contrast monochrome color palette removing all AI-slop gradients and harsh bright blues.
- **Windows GSMTC Native Media Controller**
  - Zero-Chromium native media playback control via Windows WinRT `GlobalSystemMediaTransportControlsSessionManager`.
  - Supports Spotify Desktop, Chrome/Edge browser tabs (YouTube, SoundCloud, Spotify Web), Apple Music, and VLC.
  - Controls Play/Pause, Next Track, Previous Track with live Now Playing metadata display (`Track — Artist`) both in the compact Top Bar and Media Panel.
- **1-Click Instant Shortcut Launch**
  - Single-click on any workspace file, folder, application, or link immediately launches it.
  - Added "🚀 Buka Semua" (Launch All) button in workspace header for simultaneous one-click batch launching with background pacing.
- **Workspace Grid View & Custom Logo Image / Emoji Picker**
  - Replaced single-column vertical workspace list with a responsive, modern Grid View (`WrapPanel`).
  - Added support for custom logo image files (`.png`, `.jpg`, `.ico`, `.webp`) with automatic, secure caching in `%LOCALAPPDATA%\AdaraBox\workspace-icons\`.
  - Preset emoji picker (📁, 💼, 🚀, 💻, 🎨, 📚, 🎮, 🎧, ⚡, 🌐, ☕, 🛠️, 📊, 🎯, 🎬, 💡) and custom emoji input for workspaces without image uploads.
  - SQLite database schema migration #003 adding `icon` column to `workspace` table.
- **Automated Inno Setup Installer & GitHub Release Workflow**
  - Single-file installer executable `AdaraBox-Setup-1.0.0.exe` (~52 MB) with per-user installation (no admin privileges required).
  - GitHub Actions automated release pipeline (`.github/workflows/release.yml`) for automated building, testing, installer packaging, and release artifact generation.
  - Increased automated test suite to 229 tests (100% passing).

---

## [1.1.0] - 2026-10-31

### Added
- **M07: System Stats (Hardware Metrics)**
  - Real-time hardware metric panel displaying CPU utilization, RAM usage (used/total + percentage), storage volumes, and GPU 3D utilization.
  - Zero-polling background optimization (NFR-PF-04): hardware metrics are only polled while the Stats panel is active and visible; zero overhead when closed or hidden.
  - Accurate kernel/user times calculations via Win32 `GetSystemTimes` (avoiding fragile performance counter localization issues).
  - Physical memory measurement via `GlobalMemoryStatusEx`.
  - Automatic fixed drive enumeration via `DriveInfo` with safe fault isolation per volume.
  - Transparent error handling (FR-ST-06): unsupported or inaccessible metrics display a distinct "Tidak tersedia" status without fabricated or estimated dummy data.
- **M08: Media Module (Media Quick Links & Exploration)**
  - **Tahap A (Production):** Media Quick Links allowing users to curate shortcuts to favorite streaming services, web radio, or playlists (Spotify Web, YouTube Music, SoundCloud, podcasts).
  - Launch quick links directly in the user's default browser via `IResourceLauncher`.
  - Schema migration #002 adding indexed `media_link` local table.
  - Complete CRUD support for media links with strict URL scheme validation (rejects dangerous schemes `javascript:`, `file:`, etc.).
  - Clear UI notice stating links are opened in the default browser and playback controls are handled by the browser.
  - **Tahap B (Technical Evaluation Reports):**
    - `docs/webview-evaluation.md`: Detailed evaluation on Microsoft WebView2 embedding covering authentication boundaries, cookie storage, ToS risks, memory/CPU overhead (200-500MB RAM), and official recommendation to postpone/reject in favor of lightweight browser links.
    - `docs/media-controls-evaluation.md`: Technical feasibility study on Windows `GlobalSystemMediaTransportControlsSessionManager` (GSMTC) WinRT APIs.

---

## [1.0.0] - 2026-10-31

### Added
- **M01: Application Shell & Top Bar Overlay**
  - Minimalist floating Top Bar (36 DIP) centered at the top of the active monitor.
  - Interactive hover Trigger Strip with dwell timer and smooth expand/collapse animations.
  - Modular panel structure with horizontal width expansion based on active module.
  - Transparent implementation placeholders for Media and Stats modules ("Masih dalam pengembangan").
  - Single instance mutex enforcement (`Global\AdaraBox.SingleInstance`).
  - Windows System Tray icon with quick toggle and exit actions.
- **M02: Workspace Management & Local Persistence**
  - Create, rename, and delete workspaces with real-time inline validation.
  - SQLite database storage with Write-Ahead Logging (WAL) and automatic migration runner.
  - Workspace list with item counts, last modified timestamps, and custom reordering.
  - Safe deletion dialogs with non-destructive guarantees (Golden Rule #1).
- **M03: Resource Management**
  - Add resources across 4 distinct categories: Files, Folders, Applications (Desktop `.exe`, shortcuts `.lnk`, and Windows UWP store apps), and Web Links (`http://`, `https://`).
  - Real-time disk availability checker with visual warning badges (⚠️) for moved or missing files.
  - Windows shell icon extraction and default web favicons.
  - Target URL scheme validation and path normalization.
- **M04: Open the File (Multi-Launch Engine)**
  - Dual view modes: Grid View and List View with per-workspace persistence.
  - **Open the File** multi-selection mode with custom checkboxes and active emerald styling.
  - Bulk actions: Select All (`Ctrl+A`), Clear Selection, and realtime selection counter.
  - Parallel background launcher with 150ms pacing preventing Windows shell exhaustion or UI freezing.
  - Per-item fault isolation: a single failure never aborts other launches (FR-OF-06).
  - Launch Summary dialog detailing failed items with failure reason badges and retry capability.
  - Complete separation between management mode and launch mode (FR-OF-05).
- **M05: MVP Hardening & Recovery**
  - Automatic pre-migration SQLite backup using `VACUUM INTO` with retention pruning keeping top 5.
  - Startup database integrity check (`PRAGMA quick_check;`) and zero-byte file detection.
  - Database Recovery Dialog offering options to open data folder, restore from backup, start fresh with archive, or exit.
  - Multi-monitor Per-Monitor V2 DPI awareness: Top Bar automatically appears on the monitor where the cursor resides (`ScreenPositionHelper`) and repositions dynamically on display setting changes.
  - System-wide global hotkey (default: `Ctrl+Alt+Space`) via Win32 `RegisterHotKey` to focus/toggle Top Bar.
  - Windows startup integration (`HKCU\...\Run` toggleable in Settings).
  - Full keyboard accessibility (`F2` rename, `Delete` delete, `Enter` open, `Space` toggle selection, `Esc` hierarchical back-step).
- **M06: Packaging & Release**
  - Self-contained x64 deployment without requiring .NET runtime installation.
  - Inno Setup per-user installer (`AdaraBox-Setup-1.0.0.exe`) with clean uninstaller preserving user data by default.
  - Multi-resolution application icon (`app.ico`) embedded into executable and task tray.
