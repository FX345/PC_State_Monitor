# Cyber HUD Skin Design

**Goal:** Give PcGuardianLite a cyber-anime HUD visual style while preserving all current monitoring, cleanup, report, tray, and installer behavior.

**Scope:** This first skin pass changes presentation only. It does not add character artwork, Live2D, new monitoring metrics, cleanup logic, or extra dependencies.

**Visual Direction:** The UI uses a dark translucent panel, cyan and violet neon accents, compact metric cards, soft glow shadows, and light pulse animation. Text remains readable and the panel keeps a scrollbar so lower actions stay reachable.

**Main Window:** The floating ball becomes a glowing dark orb with a neon border and subtle pulse. The detail panel becomes a HUD board with a title strip, metric cards, a health block, process ranking, cleanup controls, and report buttons.

**Compatibility:** Existing named WPF controls and event handlers remain in place so the C# behavior does not need to change.

**Validation:** Unit tests assert the XAML includes the Cyber HUD skin resources. A full test run and solution build validate that bindings and event handlers still compile.
