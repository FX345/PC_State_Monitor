# Tactical Anime UI Design

**Goal:** Upgrade the current cyber HUD skin into an original tactical anime terminal style inspired by modular mobile-game UI language, while keeping every PcGuardianLite feature unchanged.

**Visual Direction:** Use a larger dark tactical panel, disciplined spacing, cold graphite surfaces, thin technical borders, diagonal accent marks, cyan status light, and restrained orange warning highlights. Avoid copying logos, characters, or exact franchise assets.

**Animation Scope:** Add native WPF animation only: floating-ball breathing glow, panel fade/slide-in on open, card hover glow, tab selected highlight slide, button press feedback, cleanup indeterminate activity bar, smooth health-score color transition, and custom scrollbar hover glow.

**Layout Scope:** Increase the app window and expanded panel size. Add wider spacing between cards and tool sections. Keep the existing tab structure and all named controls/event handlers compatible.

**Validation:** Tests assert that XAML and code contain the tactical UI resources and animation hooks. Full tests, WPF build, and installer rebuild verify behavior.
