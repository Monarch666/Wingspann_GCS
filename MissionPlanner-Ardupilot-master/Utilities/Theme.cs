using System.Drawing;

namespace MissionPlanner.Utilities
{
    /// <summary>
    /// Apex Control Design System — derived from Horizon GCS (Stitch).
    /// All UI tokens are centralized here. No hardcoded colors elsewhere.
    /// </summary>
    public static class Theme
    {
        // ── Surface Hierarchy (Tonal Layering) ──────────────────────────
        public static readonly Color Surface            = Color.FromArgb(10, 12, 14);    // #0A0C0E  — deepest void
        public static readonly Color SurfaceDim         = Color.FromArgb(18, 20, 22);    // #121416  — dim panels
        public static readonly Color SurfaceContainer   = Color.FromArgb(30, 32, 34);    // #1E2022  — primary modules
        public static readonly Color SurfaceContainerHigh   = Color.FromArgb(40, 42, 44);  // #282A2C  — elevated cards
        public static readonly Color SurfaceContainerHighest = Color.FromArgb(51, 53, 55); // #333537  — focused inputs
        public static readonly Color SurfaceBright      = Color.FromArgb(56, 57, 60);    // #38393C  — brightest surface

        // Keep legacy alias for backward compat
        public static readonly Color Background = Surface;
        public static readonly Color Panel      = SurfaceContainer;
        public static readonly Color PanelHover = SurfaceContainerHigh;

        // ── Primary: Matrix Green ───────────────────────────────────────
        public static readonly Color Primary            = Color.FromArgb(0, 255, 65);    // #00FF41  — active/nominal
        public static readonly Color PrimaryDim         = Color.FromArgb(0, 230, 57);    // #00E639  — dimmed active
        public static readonly Color PrimaryText        = Color.FromArgb(235, 255, 226); // #EBFFE2  — high-contrast on dark
        public static readonly Color PrimaryContainer   = Color.FromArgb(0, 113, 23);    // #007117  — on-primary-container

        // Keep legacy alias
        public static readonly Color Green = Primary;
        public static readonly Color AccentOrange = Primary; // Redirect old orange refs → green

        // ── Secondary: Aviation Amber ───────────────────────────────────
        public static readonly Color Secondary          = Color.FromArgb(255, 219, 157); // #FFDB9D  — warning text
        public static readonly Color SecondaryContainer = Color.FromArgb(254, 183, 0);   // #FEB700  — warning badges
        public static readonly Color Amber              = Color.FromArgb(255, 184, 0);   // #FFB800  — caution

        // ── Tertiary: Signal Red ────────────────────────────────────────
        public static readonly Color Tertiary           = Color.FromArgb(255, 59, 48);   // #FF3B30  — critical/emergency
        public static readonly Color TertiaryContainer  = Color.FromArgb(255, 210, 204); // #FFD2CC  — error badge bg
        public static readonly Color Error              = Color.FromArgb(255, 180, 171); // #FFB4AB  — error text

        // ── Text Colors ─────────────────────────────────────────────────
        public static readonly Color OnSurface          = Color.FromArgb(226, 226, 229); // #E2E2E5  — primary text
        public static readonly Color OnSurfaceVariant   = Color.FromArgb(185, 204, 178); // #B9CCB2  — muted text
        public static readonly Color White              = Color.FromArgb(255, 255, 255);
        public static readonly Color GreyText           = Color.FromArgb(132, 150, 126); // #84967E  — outline tone
        public static readonly Color UnselectedText     = Color.FromArgb(132, 150, 126);

        // ── Borders & Outlines ──────────────────────────────────────────
        public static readonly Color Outline            = Color.FromArgb(132, 150, 126); // #84967E
        public static readonly Color OutlineVariant     = Color.FromArgb(59, 75, 55);    // #3B4B37  — ghost borders
        public static readonly Color Border = OutlineVariant; // legacy alias

        // ── Typography ──────────────────────────────────────────────────
        // Display: large telemetry values
        public static readonly Font FontDisplayMono = new Font("Consolas", 28F, FontStyle.Bold);
        // Headline: section titles
        public static readonly Font FontHeadlineLg  = new Font("Segoe UI", 22F, FontStyle.Bold);
        public static readonly Font FontHeadlineMd  = new Font("Segoe UI", 16F, FontStyle.Bold);
        // Telemetry: live data streams
        public static readonly Font FontTelemetry   = new Font("Consolas", 14F, FontStyle.Regular);
        // Body: general text
        public static readonly Font FontBody        = new Font("Segoe UI", 10F, FontStyle.Regular);
        public static readonly Font FontBodySmall   = new Font("Segoe UI", 9F, FontStyle.Regular);
        // Labels: uppercase mono tags
        public static readonly Font FontLabel       = new Font("Consolas", 8.5F, FontStyle.Bold);
        public static readonly Font FontLabelSmall  = new Font("Consolas", 7.5F, FontStyle.Bold);
        // Title: panel titles
        public static readonly Font FontTitle       = new Font("Segoe UI", 11F, FontStyle.Bold);

        // Legacy aliases
        public static readonly Font HeaderFont    = FontHeadlineLg;
        public static readonly Font SubtitleFont  = FontBodySmall;
        public static readonly Font TitleFont     = FontTitle;
        public static readonly Font RegularFont   = FontBody;
        public static readonly Font TechnicalFont = new Font("Consolas", 10F, FontStyle.Bold);

        // ── Spacing ─────────────────────────────────────────────────────
        public const int SpacingUnit       = 4;
        public const int SpacingGutter     = 12;
        public const int SpacingMargin     = 16;
        public const int SpacingPanelPad   = 12;
        public const int SpacingStackGap   = 8;

        // ── Shape ───────────────────────────────────────────────────────
        public const int CornerRadius      = 4;   // Soft corners for cards
        public const int CornerRadiusSharp = 0;   // Sharp for progress bars
    }
}
