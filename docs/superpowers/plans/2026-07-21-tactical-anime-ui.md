# Tactical Anime UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Improve PcGuardianLite's visual quality with a larger tactical anime terminal skin and native WPF motion effects.

**Architecture:** Keep the existing WPF app, named controls, tabs, cleanup service, monitor service, scripts, tray behavior, and installer unchanged. Add XAML resources/styles/storyboards plus small code-behind animation hooks for panel open, cleanup activity, and health-score color transitions.

**Tech Stack:** WPF XAML, .NET 8, existing console test harness.

## Global Constraints

No copied game assets, logos, character art, or franchise-specific marks.
No new runtime dependencies.
Do not change monitoring, cleanup, report, tray, installer, or single-instance logic.
Use native WPF animation only.
Keep text readable and controls spaced on the larger panel.

---

### Task 1: Lock Tactical UI Regression Test

**Files:**
- Modify: `tests/PcGuardianLite.Tests/Program.cs`

**Interfaces:**
- Consumes: `src/PcGuardianLite.App/MainWindow.xaml` and `MainWindow.xaml.cs`
- Produces: `TestMainWindowUsesTacticalAnimeMotionSkin`

- [x] **Step 1: Add failing test**

Assert the XAML contains `TacticalAnimePanelBrush`, `TacticalScrollBarStyle`, `PanelEntranceTransform`, `CleanupActivityBar`, and `TabSelectionRail`. Assert the code-behind contains `AnimatePanelOpen`, `SetCleanupActivity`, and `AnimateHealthScoreBrush`.

- [x] **Step 2: Run test**

Run `dotnet run --project .\tests\PcGuardianLite.Tests\PcGuardianLite.Tests.csproj` and verify the new test fails before implementation.

### Task 2: Upgrade XAML Visual System

**Files:**
- Modify: `src/PcGuardianLite.App/MainWindow.xaml`

**Interfaces:**
- Consumes: existing named WPF controls
- Produces: larger tactical UI layout, styled scrollbars, hover/press/tab animations, cleanup activity bar

- [x] **Step 1: Increase window and panel size**

Set window width/height and detail panel width/max height larger. Increase panel padding and section/card spacing.

- [x] **Step 2: Add tactical brush and scrollbar resources**

Add original tactical dark surface brushes and custom scrollbar template with hover glow.

- [x] **Step 3: Improve card, tab, button, and ball styles**

Use sharper technical surfaces, hover glow, selected tab rail animation, button press scale, and stronger floating-ball breath.

- [x] **Step 4: Add cleanup activity bar**

Add `CleanupActivityBar` under cleanup summary, hidden when idle and indeterminate while scanning/cleaning.

### Task 3: Add Animation Hooks

**Files:**
- Modify: `src/PcGuardianLite.App/MainWindow.xaml.cs`

**Interfaces:**
- Consumes: `DetailPanel`, `PanelEntranceTransform`, `CleanupActivityBar`, `HealthScoreText`
- Produces: `AnimatePanelOpen`, `SetCleanupActivity`, `AnimateHealthScoreBrush`

- [x] **Step 1: Animate panel open**

When the detail panel becomes visible, fade from 0 to 1 and translate up into place.

- [x] **Step 2: Animate cleanup activity**

Show the activity bar before scan/clean async work and hide it in finally blocks.

- [x] **Step 3: Animate health color**

Transition health score foreground from green/cyan to orange/red based on health score.

### Task 4: Verify, Package, Commit, Push

**Files:**
- Update local generated outputs under `publish-single` and `installer-output`

**Interfaces:**
- Consumes: passing tests and build
- Produces: refreshed local exe/installer and GitHub source commit

- [x] **Step 1: Run tests and build**

Run test project and full solution build.

- [x] **Step 2: Rebuild artifacts**

Run `publish_single_exe.bat` and `build_installer.ps1`.

- [ ] **Step 3: Commit and push**

Commit source/doc/test changes and push to `main`.
