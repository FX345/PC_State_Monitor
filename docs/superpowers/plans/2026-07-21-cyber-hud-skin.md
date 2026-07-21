# Cyber HUD Skin Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Re-skin the existing WPF floating monitor as a cyber-anime HUD without changing feature behavior.

**Architecture:** Keep all C# monitor, cleanup, report, tray, and installer logic unchanged. Apply the first skin through `MainWindow.xaml` resources, styles, layout, and WPF animations while preserving existing control names and click handlers.

**Tech Stack:** WPF XAML, .NET 8, existing console-style test harness.

## Global Constraints

All existing named controls and event handlers must remain compatible with `MainWindow.xaml.cs`.
No new runtime dependencies or heavy character-rendering modules.
The panel must retain scroll support.
Text must remain readable and not overlap in the compact floating UI.

---

### Task 1: Lock Skin Regression Test

**Files:**
- Modify: `PcGuardianLite/tests/PcGuardianLite.Tests/Program.cs`

**Interfaces:**
- Consumes: `MainWindow.xaml`
- Produces: `TestMainWindowUsesCyberHudSkinResources`

- [x] **Step 1: Add a failing test**

Assert that `MainWindow.xaml` contains `CyberPanelBrush`, `CyberMetricCardStyle`, `CyberPulseStoryboard`, `#07111F`, and `SYSTEM STATUS`.

- [x] **Step 2: Run the test**

Run: `dotnet run --project .\PcGuardianLite\tests\PcGuardianLite.Tests\PcGuardianLite.Tests.csproj`
Expected: one failure for missing Cyber HUD resources.

### Task 2: Apply XAML Skin

**Files:**
- Modify: `PcGuardianLite/src/PcGuardianLite.App/MainWindow.xaml`

**Interfaces:**
- Consumes: existing named controls such as `FloatingBall`, `DetailPanel`, `CpuText`, `CleanupListView`
- Produces: cyber HUD resources and styles used by the window

- [x] **Step 1: Add cyber brushes and styles**

Define panel, card, text, button, check box, and list styles in `Window.Resources`.

- [x] **Step 2: Rework the floating ball**

Preserve drag and context menu handlers. Add neon border, dark radial fill, pulse animation, and readable metric text.

- [x] **Step 3: Rework the detail panel**

Keep scrollbar and existing command buttons. Replace the flat white panel with HUD sections and metric cards.

- [x] **Step 4: Run tests and build**

Run the test project, then build the solution.

### Task 3: Rebuild User Artifacts

**Files:**
- Update generated outputs under `PcGuardianLite/publish-single` and `PcGuardianLite/installer-output`

**Interfaces:**
- Consumes: passing app build
- Produces: refreshed `PcGuardianLite.exe` and `PcGuardianLiteSetup.exe`

- [x] **Step 1: Publish single launcher**

Run `.\PcGuardianLite\publish_single_exe.bat`.

- [x] **Step 2: Rebuild installer**

Run `.\PcGuardianLite\build_installer.ps1`.

- [x] **Step 3: Report exact output paths**

Tell the user where the updated launcher and installer are located.
