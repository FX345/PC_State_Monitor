# Premium Instrument UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild the WPF front end into a high-quality left-navigation instrument panel while preserving all current PcGuardianLite functionality.

**Architecture:** Add a WPF ResourceDictionary for design tokens and reusable styles. Rewrite `MainWindow.xaml` around a larger shell, left navigation, dashboard cards, and existing named controls. Keep `MainWindow.xaml.cs` business interactions intact, adding only layout/animation helpers.

**Tech Stack:** WPF XAML, .NET 8, existing console test harness.

## Global Constraints

No copied game assets, logos, character art, or direct franchise UI components.
No new runtime dependencies.
Do not change monitoring, cleanup, report, tray, installer, or single-instance logic.
Use native WPF only.
Prioritize transform/opacity animations; avoid expensive continuous blur/shadow animation.
Keep generated installer and publish outputs out of git.

---

### Task 1: Add Premium UI Regression Test

**Files:**
- Modify: `tests/PcGuardianLite.Tests/Program.cs`

**Interfaces:**
- Consumes: `src/PcGuardianLite.App/MainWindow.xaml`, `src/PcGuardianLite.App/App.xaml`, `src/PcGuardianLite.App/Styles/PremiumTheme.xaml`, and `MainWindow.xaml.cs`
- Produces: `TestMainWindowUsesPremiumInstrumentShell`

- [x] **Step 1: Add failing assertions**

Assert sources contain `Styles/PremiumTheme.xaml`, `PremiumShellGrid`, `PremiumNavigationTabControlStyle`, `PremiumMetricCardStyle`, `PremiumFloatingBallStyle`, `PremiumStatusPillStyle`, `PremiumScrollbarStyle`, `MetricGrid`, and code hooks `AnimatePanelClose` and `KeepPanelInsideWorkArea`.

- [x] **Step 2: Run failing test**

Run `dotnet run --project .\tests\PcGuardianLite.Tests\PcGuardianLite.Tests.csproj` and verify the new test fails before implementation.

### Task 2: Build Design System Resources

**Files:**
- Create: `src/PcGuardianLite.App/Styles/PremiumTheme.xaml`
- Modify: `src/PcGuardianLite.App/App.xaml`

**Interfaces:**
- Produces reusable WPF resources for colors, spacing, typography, buttons, cards, scrollbars, progress bars, list rows, and left navigation.

- [x] **Step 1: Create premium theme dictionary**

Define design tokens and styles for the premium instrument UI.

- [x] **Step 2: Merge theme in App.xaml**

Add `Styles/PremiumTheme.xaml` to application resources.

### Task 3: Rebuild MainWindow Layout

**Files:**
- Modify: `src/PcGuardianLite.App/MainWindow.xaml`

**Interfaces:**
- Preserves named controls: `FloatingBall`, `DetailPanel`, `CpuText`, `MemoryText`, `DownloadText`, `UploadText`, `NetworkDownloadText`, `NetworkUploadText`, `DiskText`, `TemperatureText`, `HealthScoreText`, `DiskWarningText`, `HealthReasonText`, `ProcessRankingText`, cleanup controls, report buttons.

- [x] **Step 1: Resize window and panel shell**

Use a larger transparent window and a large expanded panel with left navigation.

- [x] **Step 2: Rebuild overview page**

Place metric cards, health module, process preview, and cleanup summary in a balanced dashboard.

- [x] **Step 3: Rebuild tool pages**

Keep cleanup, network, process, and report pages but space and style them consistently.

- [x] **Step 4: Rebuild floating ball**

Use multi-ring precision instrument styling, quiet breath animation, and unified state colors.

### Task 4: Add Smooth Layout Hooks

**Files:**
- Modify: `src/PcGuardianLite.App/MainWindow.xaml.cs`

**Interfaces:**
- Produces `AnimatePanelClose`, `KeepPanelInsideWorkArea`, and smoother open/close behavior.

- [x] **Step 1: Animate close**

Close with opacity/translate animation before collapsing the panel.

- [x] **Step 2: Keep panel in work area**

When opening panel, adjust window position to avoid clipping at screen edges.

### Task 5: Verify, Package, Commit, Push

**Files:**
- Update local generated outputs under `publish-single` and `installer-output`

**Interfaces:**
- Consumes passing tests/build
- Produces refreshed local exe/installer and pushed source commit

- [x] **Step 1: Run tests and full build**

Run the test project and full solution build.

- [x] **Step 2: Rebuild local artifacts**

Run `publish_single_exe.bat` and `build_installer.ps1`.

- [x] **Step 3: Commit and push**

Commit source/doc/test changes and push `main`.
