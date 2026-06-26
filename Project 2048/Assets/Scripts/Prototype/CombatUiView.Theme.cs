using UnityEngine;

namespace Project2048.Prototype
{
    public partial class CombatUiView
    {
        public static readonly Color ThemePrimaryColor = new(73f / 255f, 175f / 255f, 181f / 255f, 1f);
        public static readonly Color ThemeHpFillColor = ThemePrimaryColor;
        public static readonly Color ThemeHpDarkColor = new(12f / 255f, 13f / 255f, 14f / 255f, 1f);
        public static readonly Color ThemeHpBarBackgroundColor = ThemeHpDarkColor;
        public static readonly Color ThemeHpDamageTrailColor = new(20f / 255f, 79f / 255f, 84f / 255f, 0.90f);
        public static readonly Color ThemeHpTextOutlineColor = new(42f / 255f, 127f / 255f, 133f / 255f, 1f);
        public static readonly Color ThemeHpBorderColor = new(0f, 0f, 0f, 1f);
        public static readonly Color ThemeSkillAttackColor = new(79f / 255f, 106f / 255f, 90f / 255f, 1f);
        public static readonly Color ThemeSkillDefenseColor = new(45f / 255f, 103f / 255f, 107f / 255f, 1f);
        public static readonly Color ThemeSkillChangeColor = new(68f / 255f, 88f / 255f, 105f / 255f, 1f);
        public static readonly Color ThemeBoardCellColor = new(0.20f, 0.20f, 0.22f, 1f);
        public static readonly Color ThemeBoardHelpIconColor = new(0.42f, 0.46f, 0.50f, 0.95f);
        public static readonly Color ThemeBoardHelpOutlineColor = ThemeBoardCellColor;
        public static readonly Color ThemeBottomPanelSideFillColor = new(15f / 255f, 14f / 255f, 13f / 255f, 1f);
        private static readonly Color ThemeSkillEmptyColor = new(22f / 255f, 25f / 255f, 28f / 255f, 1f);

        public const float HpStatusEffectXOffset = 18f;
        public const float HpTextMinFontSize = 22f;
        public const float HpTextOutlineWidth = 0.26f;
        public const float HpTextOutlineDistance = 1.65f;
        public const float IntentBubbleSquareSize = 60f;

        private const float HpDamageFlashDurationSeconds = 0.12f;
        private const float HpHitShakeMagnitude = 12f;
        private const float HpBarBorderThickness = 2.75f;
        private const float IntentBubbleTextPadding = 3f;
        private const float IntentBubbleTextMinFontSize = 8f;
        private const float IntentBubbleTextMaxFontSize = 14f;
        private const float TooltipFontSize = 18f;
        private const float TooltipSingleLineWidth = 640f;
        private const float TooltipMultiLineWidth = 960f;
        private const float TooltipBaseHeight = 72f;
        private const float TooltipLineHeight = 44f;
        private const float TooltipMinHeight = 128f;
        private const float TooltipMaxHeight = 440f;
        private const string HpBarInteriorName = "HpBarInterior";
        private const string HpBarOutlineName = "HpBarOutline";
        private const string StatusEffectTemplateName = "StatusEffectIconSample";
        private const float UiSfxDistance = 10000f;
    }
}
