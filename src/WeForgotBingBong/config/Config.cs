using BepInEx.Configuration;

using static WeForgotBingBong.Plugin;

namespace ConfigSpace
{
    public class ConfigClass
    {
        // Basic Configuration
        public static ConfigEntry<float> curseInterval = null!;
        public static ConfigEntry<bool> showUI = null!;
        public static ConfigEntry<float> curseIntensity = null!;
        public static ConfigEntry<bool> debugMode = null!;
        public static ConfigEntry<float> playerJoinBufferTime = null!;

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
        public static ConfigEntry<bool> countNearbyAsCarrying = null!;
        public static ConfigEntry<float> nearbyDetectionRadius = null!;
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
            curseInterval = config.Bind("General", "CurseInterval", 5.0f, "Interval in seconds between curse applications (Min: 0.5, Max: 60.0)");
            showUI = config.Bind("UI", "ShowBingBongUI", true, "Whether to display BingBong status on screen");
            curseIntensity = config.Bind("General", "CurseIntensity", 1.0f, "Amount of curse effect applied per application (0.1-5.0)");
            playerJoinBufferTime = config.Bind("General", "PlayerJoinBufferTime", 30.0f, "Buffer time in seconds after player joins before curses can start (Min: 0, Max: 300)");
            debugMode = config.Bind("Debug", "EnableDebugMode", true, "Enable debug mode for detailed logging");

            // Curse Type Selection Configuration
            curseSelectionMode = config.Bind("CurseType", "SelectionMode", CurseSelectionMode.Random,
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
            countNearbyAsCarrying = config.Bind("CarryingDetection", "CountNearbyAsCarrying", true,
              "Whether nearby BingBong counts as carrying (prevents curses)");
            nearbyDetectionRadius = config.Bind("CarryingDetection", "NearbyDetectionRadius", 10.0f,
              "Nearby detection radius (effective when CountNearbyAsCarrying is true)");
            countTempSlotAsCarrying = config.Bind("CarryingDetection", "CountTempSlotAsCarrying", true,
              "Whether BingBong in temporary item slot counts as carrying");

            // Final Curse Effect Intensity Configuration
            poisonIntensity = config.Bind("CurseIntensity", "PoisonIntensity", 0.1f, "Final poison curse intensity (0.1-10.0)");
            injuryIntensity = config.Bind("CurseIntensity", "InjuryIntensity", 0.05f, "Final injury curse intensity (0.1-10.0)");
            hungerIntensity = config.Bind("CurseIntensity", "HungerIntensity", 0.1f, "Final hunger curse intensity (0.1-10.0)");
            drowsyIntensity = config.Bind("CurseIntensity", "DrowsyIntensity", 0.1f, "Final drowsy curse intensity (0.1-10.0)");
            curseStatusIntensity = config.Bind("CurseIntensity", "CurseStatusIntensity", 0.1f, "Final curse status intensity (0.1-10.0)");
            coldIntensity = config.Bind("CurseIntensity", "ColdIntensity", 0.1f, "Final cold curse intensity (0.1-10.0)");
            hotIntensity = config.Bind("CurseIntensity", "HotIntensity", 0.1f, "Final hot curse intensity (0.1-10.0)");

            // Setup configuration value constraints
            SetupConfigConstraints();
        }

        private static void SetupConfigConstraints()
        {
            // Curse interval constraints
            curseInterval.SettingChanged += (sender, e) =>
            {
                if (curseInterval.Value < 0.5f) curseInterval.Value = 0.5f;
                if (curseInterval.Value > 60.0f) curseInterval.Value = 60.0f;
            };

            // Curse intensity constraints
            curseIntensity.SettingChanged += (sender, e) =>
            {
                if (curseIntensity.Value < 0.1f) curseIntensity.Value = 0.1f;
                if (curseIntensity.Value > 5.0f) curseIntensity.Value = 5.0f;
            };

            // Nearby detection radius constraints
            nearbyDetectionRadius.SettingChanged += (sender, e) =>
            {
                if (nearbyDetectionRadius.Value < 0.5f) nearbyDetectionRadius.Value = 0.5f;
                if (nearbyDetectionRadius.Value > 10.0f) nearbyDetectionRadius.Value = 10.0f;
            };

            // Player join buffer time constraints
            playerJoinBufferTime.SettingChanged += (sender, e) =>
            {
                if (playerJoinBufferTime.Value < 0f) playerJoinBufferTime.Value = 0f;
                if (playerJoinBufferTime.Value > 300f) playerJoinBufferTime.Value = 300f;
            };

            // Final curse intensity constraints
            poisonIntensity.SettingChanged += (sender, e) =>
            {
                if (poisonIntensity.Value < 0.1f) poisonIntensity.Value = 0.1f;
                if (poisonIntensity.Value > 10.0f) poisonIntensity.Value = 10.0f;
            };

            injuryIntensity.SettingChanged += (sender, e) =>
            {
                if (injuryIntensity.Value < 0.1f) injuryIntensity.Value = 0.1f;
                if (injuryIntensity.Value > 10.0f) injuryIntensity.Value = 10.0f;
            };

            hungerIntensity.SettingChanged += (sender, e) =>
            {
                if (hungerIntensity.Value < 0.1f) hungerIntensity.Value = 0.1f;
                if (hungerIntensity.Value > 10.0f) hungerIntensity.Value = 10.0f;
            };

            drowsyIntensity.SettingChanged += (sender, e) =>
            {
                if (drowsyIntensity.Value < 0.1f) drowsyIntensity.Value = 0.1f;
                if (drowsyIntensity.Value > 10.0f) drowsyIntensity.Value = 10.0f;
            };

            curseStatusIntensity.SettingChanged += (sender, e) =>
            {
                if (curseStatusIntensity.Value < 0.1f) curseStatusIntensity.Value = 0.1f;
                if (curseStatusIntensity.Value > 10.0f) curseStatusIntensity.Value = 10.0f;
            };

            coldIntensity.SettingChanged += (sender, e) =>
            {
                if (coldIntensity.Value < 0.1f) coldIntensity.Value = 0.1f;
                if (coldIntensity.Value > 10.0f) coldIntensity.Value = 10.0f;
            };

            hotIntensity.SettingChanged += (sender, e) =>
            {
                if (hotIntensity.Value < 0.1f) hotIntensity.Value = 0.1f;
                if (hotIntensity.Value > 10.0f) hotIntensity.Value = 10.0f;
            };
        }
    }
}