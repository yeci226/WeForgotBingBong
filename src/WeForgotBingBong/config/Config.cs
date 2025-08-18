using BepInEx.Configuration;

using static WeForgotBingBong.Plugin;

namespace ConfigSpace
{
    public class ConfigClass
    {
        // Basic Configuration
        public static ConfigEntry<float> curseInterval = null!;
        public static ConfigEntry<float> invincibilityPeriod = null!;
        public static ConfigEntry<bool> showUI = null!;
        public static ConfigEntry<bool> debugMode = null!;

        // Curse Type Configuration
        public static ConfigEntry<CurseSelectionMode> curseSelectionMode = null!;
        public static ConfigEntry<string> singleCurseType = null!;
        public static ConfigEntry<bool> enablePoison = null!;
        public static ConfigEntry<bool> enableInjury = null!;
        public static ConfigEntry<bool> enableHunger = null!;
        public static ConfigEntry<bool> enableDrowsy = null!;
        public static ConfigEntry<bool> enableCurse = null!;
        public static ConfigEntry<bool> enableCold = null!;
        public static ConfigEntry<bool> enableHot = null!;

        // Carrying Detection Configuration
        public static ConfigEntry<bool> countBackpackAsCarrying = null!;
        public static ConfigEntry<bool> countTempSlotAsCarrying = null!;

        // Final Curse Effect Intensity Configuration
        public static ConfigEntry<float> poisonIntensity = null!;
        public static ConfigEntry<float> injuryIntensity = null!;
        public static ConfigEntry<float> hungerIntensity = null!;
        public static ConfigEntry<float> drowsyIntensity = null!;
        public static ConfigEntry<float> curseStatusIntensity = null!;
        public static ConfigEntry<float> coldIntensity = null!;
        public static ConfigEntry<float> hotIntensity = null!;

        public static void InitConfig(ConfigFile config)
        {
            // Basic Configuration
            curseInterval = config.Bind("General", "CurseInterval", 5.0f, "Interval in seconds between curse applications (Min: 0.01, Max: 60.0)");
            invincibilityPeriod = config.Bind("General", "InvincibilityPeriod", 20.0f, "Duration in seconds for invincibility after curse application (Min: 0.0, Max: 60.0)");
            showUI = config.Bind("UI", "ShowBingBongUI", true, "Whether to display BingBong status on screen");
            debugMode = config.Bind("Debug", "EnableDebugMode", false, "Enable debug mode for detailed logging");

            // Curse Type Selection Configuration
            curseSelectionMode = config.Bind("CurseType", "SelectionMode", CurseSelectionMode.Single,
              "Curse selection mode: Single(one type), Random(random selection), Multiple(multiple curses)");
            singleCurseType = config.Bind("CurseType", "SingleCurseType", "Poison",
              "Single curse type (effective when SelectionMode is set to Single)");

            // Individual Curse Type Switches
            enablePoison = config.Bind("CurseType", "EnablePoison", true, "Enable poison curse");
            enableInjury = config.Bind("CurseType", "EnableInjury", true, "Enable injury curse");
            enableHunger = config.Bind("CurseType", "EnableHunger", true, "Enable hunger curse");
            enableDrowsy = config.Bind("CurseType", "EnableDrowsy", true, "Enable drowsy curse");
            enableCurse = config.Bind("CurseType", "EnableCurse", false, "Enable curse status");
            enableCold = config.Bind("CurseType", "EnableCold", true, "Enable cold curse");
            enableHot = config.Bind("CurseType", "EnableHot", true, "Enable hot curse");

            // Carrying Detection Configuration
            countBackpackAsCarrying = config.Bind("CarryingDetection", "CountBackpackAsCarrying", true,
              "Whether BingBong in backpack counts as carrying (prevents curses)");
            countTempSlotAsCarrying = config.Bind("CarryingDetection", "CountTempSlotAsCarrying", true,
              "Whether BingBong in temporary item slot counts as carrying");

            // Final Curse Effect Intensity Configuration
            poisonIntensity = config.Bind("CurseIntensity", "PoisonIntensity", 0.1f, "Final poison curse intensity (0.01-10.0)");
            injuryIntensity = config.Bind("CurseIntensity", "InjuryIntensity", 0.1f, "Final injury curse intensity (0.01-10.0)");
            hungerIntensity = config.Bind("CurseIntensity", "HungerIntensity", 0.1f, "Final hunger curse intensity (0.01-10.0)");
            drowsyIntensity = config.Bind("CurseIntensity", "DrowsyIntensity", 0.1f, "Final drowsy curse intensity (0.01-10.0)");
            curseStatusIntensity = config.Bind("CurseIntensity", "CurseStatusIntensity", 0.1f, "Final curse status intensity (0.01-10.0)");
            coldIntensity = config.Bind("CurseIntensity", "ColdIntensity", 0.1f, "Final cold curse intensity (0.01-10.0)");
            hotIntensity = config.Bind("CurseIntensity", "HotIntensity", 0.1f, "Final hot curse intensity (0.01-10.0)");

            // Setup configuration value constraints
            SetupConfigConstraints();
        }

        private static void SetupConfigConstraints()
        {
            // Curse interval constraints
            curseInterval.SettingChanged += (sender, e) =>
            {
                if (curseInterval.Value < 0.01f) curseInterval.Value = 0.01f;
                if (curseInterval.Value > 60.0f) curseInterval.Value = 60.0f;
            };

            invincibilityPeriod.SettingChanged += (sender, e) =>
            {
                if (invincibilityPeriod.Value < 0.0f) invincibilityPeriod.Value = 0.0f;
                if (invincibilityPeriod.Value > 60.0f) invincibilityPeriod.Value = 60.0f;
            };


            // Final curse intensity constraints
            poisonIntensity.SettingChanged += (sender, e) =>
            {
                if (poisonIntensity.Value < 0.01f) poisonIntensity.Value = 0.01f;
                if (poisonIntensity.Value > 10.0f) poisonIntensity.Value = 10.0f;
            };

            injuryIntensity.SettingChanged += (sender, e) =>
            {
                if (injuryIntensity.Value < 0.01f) injuryIntensity.Value = 0.01f;
                if (injuryIntensity.Value > 10.0f) injuryIntensity.Value = 10.0f;
            };

            hungerIntensity.SettingChanged += (sender, e) =>
            {
                if (hungerIntensity.Value < 0.01f) hungerIntensity.Value = 0.01f;
                if (hungerIntensity.Value > 10.0f) hungerIntensity.Value = 10.0f;
            };

            drowsyIntensity.SettingChanged += (sender, e) =>
            {
                if (drowsyIntensity.Value < 0.01f) drowsyIntensity.Value = 0.01f;
                if (drowsyIntensity.Value > 10.0f) drowsyIntensity.Value = 10.0f;
            };

            curseStatusIntensity.SettingChanged += (sender, e) =>
            {
                if (curseStatusIntensity.Value < 0.01f) curseStatusIntensity.Value = 0.01f;
                if (curseStatusIntensity.Value > 10.0f) curseStatusIntensity.Value = 10.0f;
            };

            coldIntensity.SettingChanged += (sender, e) =>
            {
                if (coldIntensity.Value < 0.01f) coldIntensity.Value = 0.01f;
                if (coldIntensity.Value > 10.0f) coldIntensity.Value = 10.0f;
            };

            hotIntensity.SettingChanged += (sender, e) =>
            {
                if (hotIntensity.Value < 0.01f) hotIntensity.Value = 0.01f;
                if (hotIntensity.Value > 10.0f) hotIntensity.Value = 10.0f;
            };
        }
    }
}