# Tabbed Panel Design

**Goal:** Reduce crowding in the expanded PcGuardianLite panel by showing a clean overview first and moving secondary tools into tabs.

**Scope:** This change only reorganizes the WPF panel layout. Monitoring, cleanup, report generation, tray behavior, installer behavior, and network-speed calculation remain unchanged.

**Panel Structure:** The expanded panel uses top tabs: `总览`, `清理`, `网络`, `进程`, and `报告`. The overview tab shows the core live metrics, health score, current status, and short explanations for upload/download speed. Cleanup controls live only under `清理`; process ranking lives only under `进程`; report buttons live only under `报告`.

**Network Copy:** Download speed means current receive traffic across network adapters. Upload speed means current send traffic across network adapters. These values are live traffic, not a broadband speed-test result.

**Validation:** A regression test checks that `MainWindow.xaml` contains the tab resources and the five expected tab headers. Full test and build runs validate that existing bindings and event handlers still compile.
