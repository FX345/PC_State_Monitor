# Tabbed Panel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split the expanded WPF panel into overview and tool tabs so cleanup, reports, process ranking, and network actions no longer crowd the first screen.

**Architecture:** Keep all existing control names and click handlers. Change `MainWindow.xaml` to use a styled WPF `TabControl` with five `TabItem` pages, and add a test that locks the expected tab structure.

**Tech Stack:** WPF XAML, .NET 8, existing console-style test harness.

## Global Constraints

Do not change monitoring, cleanup, report, tray, installer, or single-instance logic.
Keep existing named controls such as `CpuText`, `CleanupListView`, and `ProcessRankingText`.
Keep the cyber HUD visual direction.
Keep panel contents readable without stacking every feature in one scroll page.

---

### Task 1: Add Tab Regression Test

**Files:**
- Modify: `tests/PcGuardianLite.Tests/Program.cs`

**Interfaces:**
- Consumes: `src/PcGuardianLite.App/MainWindow.xaml`
- Produces: `TestMainWindowUsesTabbedToolLayout`

- [x] **Step 1: Write the failing test**

Add a test that asserts the XAML contains `CyberTabControlStyle`, `Header="总览"`, `Header="清理"`, `Header="网络"`, `Header="进程"`, and `Header="报告"`.

- [x] **Step 2: Run the test**

Run: `dotnet run --project .\tests\PcGuardianLite.Tests\PcGuardianLite.Tests.csproj`
Expected: one failure for missing tab layout.

### Task 2: Reorganize MainWindow XAML

**Files:**
- Modify: `src/PcGuardianLite.App/MainWindow.xaml`

**Interfaces:**
- Consumes: existing WPF event handlers and named controls
- Produces: tabbed panel layout

- [x] **Step 1: Add TabControl and TabItem cyber styles**

Define dark HUD styles for tab strip and tab headers.

- [x] **Step 2: Move overview content into `总览`**

Keep CPU, memory, download, upload, disk, temperature, health score, status, and network explanation.

- [x] **Step 3: Move cleanup controls into `清理`**

Keep all cleanup checkboxes, scan button, clean button, summary text, and list view.

- [x] **Step 4: Move process ranking into `进程` and report buttons into `报告`**

Keep `ProcessRankingText` and all report/open/exit buttons wired to existing handlers.

### Task 3: Verify, Package, and Push

**Files:**
- Update generated local artifacts under `publish-single` and `installer-output`

**Interfaces:**
- Consumes: passing tests and build
- Produces: refreshed local exe/installer and GitHub source commit

- [x] **Step 1: Run tests and solution build**

Run the test project, then build the solution.

- [x] **Step 2: Publish single launcher and installer**

Run `publish_single_exe.bat` and `build_installer.ps1`.

- [ ] **Step 3: Commit and push source**

Commit source/doc/test changes and push `main` to GitHub.
