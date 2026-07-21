# Premium Instrument UI Design

**Goal:** Rebuild PcGuardianLite's WPF presentation into a high-quality, restrained, original modern Windows utility interface with modular industrial structure and clear information hierarchy.

**Approved Direction:** The user provided a detailed implementation specification and reference image, and explicitly asked to inspect and directly implement the current project rather than provide another concept-only response.

**Technical Stack:** PcGuardianLite is a WPF/.NET 8 desktop application. The main window is `MainWindow.xaml`; behavior and data refresh live in `MainWindow.xaml.cs`; business logic lives in `PcGuardianLite.Core`.

**Design System:** Move reusable color, spacing, typography, card, button, scrollbar, list, navigation, and progress styles into `Styles/PremiumTheme.xaml`. Use a dark graphite design system with cool text, restrained cyan state accents, amber warning accents, and red danger accents. Avoid broad neon glow and continuous expensive shadow animation.

**Layout:** Replace the top tab strip with a left navigation rail inside a larger expanded panel. The overview page presents live metric cards, health summary, process ranking preview, and compact cleanup entry. Tool pages retain cleanup, network, process, and report workflows.

**Motion:** Keep motion subtle and performant: floating-ball low-frequency breath, panel fade/slide open and close, card hover transform, selected navigation indicator, button press scale, cleanup activity bar, and health color transition. Avoid particles, high-frequency flashing, large blur, and animating heavy shadows.

**Compatibility:** Preserve existing named controls and event handlers for metrics, cleanup, reports, tray, and floating-ball interactions. Do not change system monitoring, cleaning, or installer logic.

**Validation:** Tests assert the premium theme resource dictionary, left navigation shell, and animation hooks exist. Full test and build runs must pass before completion.
