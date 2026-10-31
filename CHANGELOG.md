# Changelog

All notable changes to the Adara Box project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
