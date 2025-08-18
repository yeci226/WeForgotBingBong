using UnityEngine;
using System.Collections.Generic;

using WeForgotBingBong;
using Gui;
using ConfigSpace;

namespace Curse
{
    public class BingBongCurseLogic : MonoBehaviour
    {
        private ushort bingBongItemID;
        private float timer = 0f;
        private readonly Dictionary<Player, float> playerPickupTime = [];

        private Player[] cachedPlayers = [];
        private float lastPlayerUpdateTime = 0f;
        private const float PLAYER_UPDATE_INTERVAL = 1f;
        private bool lastCurseState = false;

        private readonly Dictionary<Player, List<MonoBehaviour>> activeCurses = [];

        public void Setup(ushort itemID, float interval, bool displayUI)
        {
            bingBongItemID = itemID;
        }

        void Update()
        {
            if (LoadingScreenHandler.loading)
            {
                return;
            }

            if (RunManager.Instance == null || RunManager.Instance.timeSinceRunStarted < ConfigClass.invincibilityPeriod.Value)
            {
                float remaining = 0f;
                if (RunManager.Instance != null)
                {
                    remaining = ConfigClass.invincibilityPeriod.Value - RunManager.Instance.timeSinceRunStarted;
                }

                if (ConfigClass.showUI.Value)
                {
                    UIManager.instance?.SetBingBongStatus(false, 0f);
                    UIManager.instance?.SetInvincibilityInfo(true, remaining);
                }

                if (ConfigClass.debugMode.Value)
                {
                    Plugin.Logger.LogInfo($"[Curse] Run not started or invincibility active ({remaining:F1}s left)");
                }

                return; 
            }

            if (ConfigClass.showUI.Value)
            {
                UIManager.instance?.SetInvincibilityInfo(false, 0f);
            }

            if (Time.time - lastPlayerUpdateTime >= PLAYER_UPDATE_INTERVAL)
            {
                cachedPlayers = UnityEngine.Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
                lastPlayerUpdateTime = Time.time;
            }

            if (cachedPlayers.Length == 0) return;

            bool isHeldByAny = false;
            Player? localPlayer = null;
            List<string> carryingPlayers = [];

            foreach (var player in cachedPlayers)
            {
                if (player == null) continue;

                bool isHeld = Plugin.CheckIfPlayerHasBingBong(player, bingBongItemID);
                if (isHeld)
                {
                    isHeldByAny = true;
                    string playerName = player.name ?? "Unknown Player";
                    carryingPlayers.Add(playerName);
                }

                if (player.photonView.IsMine)
                {
                    localPlayer = player;
                }
            }

            if (ConfigClass.showUI.Value && localPlayer != null)
            {
                float localPlayerDistance = Vector3.Distance(localPlayer.transform.position, transform.position);
                UIManager.instance?.SetBingBongStatus(isHeldByAny, localPlayerDistance);
            }

            if (!isHeldByAny)
            {
                timer += Time.deltaTime;

                if (timer >= ConfigClass.curseInterval.Value)
                {
                    int cursedPlayerCount = 0;
                    foreach (var player in cachedPlayers)
                    {
                        if (player != null)
                        {
                            ApplyCurse(player);
                            cursedPlayerCount++;
                        }
                    }

                    timer = 0f;
                }
            }

            if (lastCurseState != isHeldByAny)
            {
                lastCurseState = isHeldByAny;
            }
        }

        void ApplyCurse(Player player)
        {
            string playerName = player.name ?? "Unknown Player";

            if (!activeCurses.ContainsKey(player))
            {
                activeCurses[player] = [];
            }

            ApplyCurseAlternative(player, playerName);
        }

        private void ApplyCurseAlternative(Player player, string playerName)
        {
            try
            {
                if (player.character != null && player.character.refs != null && player.character.refs.afflictions != null)
                {
                    var curseTypes = GetAvailableCurseTypes();
                    if (curseTypes.Count == 0)
                    {
                        return;
                    }

                    var selectedCurses = SelectCurses(curseTypes);

                    foreach (var curseType in selectedCurses)
                    {
                        ApplySpecificCurse(player, playerName, curseType);
                    }

                    return;
                }
            }
            catch (System.Exception)
            {
            }
        }

        private List<CharacterAfflictions.STATUSTYPE> GetAvailableCurseTypes()
        {
            var availableCurses = new List<CharacterAfflictions.STATUSTYPE>();

            if (ConfigClass.enablePoison.Value)
                availableCurses.Add(CharacterAfflictions.STATUSTYPE.Poison);
            if (ConfigClass.enableInjury.Value)
                availableCurses.Add(CharacterAfflictions.STATUSTYPE.Injury);
            if (ConfigClass.enableHunger.Value)
                availableCurses.Add(CharacterAfflictions.STATUSTYPE.Hunger);
            if (ConfigClass.enableDrowsy.Value)
                availableCurses.Add(CharacterAfflictions.STATUSTYPE.Drowsy);
            if (ConfigClass.enableCurse.Value)
                availableCurses.Add(CharacterAfflictions.STATUSTYPE.Curse);
            if (ConfigClass.enableCold.Value)
                availableCurses.Add(CharacterAfflictions.STATUSTYPE.Cold);
            if (ConfigClass.enableHot.Value)
                availableCurses.Add(CharacterAfflictions.STATUSTYPE.Hot);

            return availableCurses;
        }

        private List<CharacterAfflictions.STATUSTYPE> SelectCurses(List<CharacterAfflictions.STATUSTYPE> availableCurses)
        {
            var selectedCurses = new List<CharacterAfflictions.STATUSTYPE>();

            if (ConfigClass.debugMode.Value)
            {
                Plugin.Logger.LogInfo($"[SelectCurses] Available curses: {string.Join(", ", availableCurses)}");
                Plugin.Logger.LogInfo($"[SelectCurses] Selection mode: {ConfigClass.curseSelectionMode.Value}");
                Plugin.Logger.LogInfo($"[SelectCurses] Single curse type setting: {ConfigClass.singleCurseType.Value}");
            }

            switch (ConfigClass.curseSelectionMode.Value)
            {
                case Plugin.CurseSelectionMode.Single:
                    // Single curse type
                    var singleType = GetCurseTypeFromString(ConfigClass.singleCurseType.Value);
                    if (availableCurses.Contains(singleType))
                    {
                        selectedCurses.Add(singleType);
                        if (ConfigClass.debugMode.Value)
                        {
                            Plugin.Logger.LogInfo($"[SelectCurses] Selected single curse: {singleType}");
                        }
                    }
                    else if (availableCurses.Count > 0)
                    {
                        selectedCurses.Add(availableCurses[0]); // Fallback to first available type
                        if (ConfigClass.debugMode.Value)
                        {
                            Plugin.Logger.LogInfo($"[SelectCurses] Single curse not available, fallback to: {availableCurses[0]}");
                        }
                    }
                    break;

                case Plugin.CurseSelectionMode.Random:
                    // Random selection
                    if (availableCurses.Count > 0)
                    {
                        int randomIndex = UnityEngine.Random.Range(0, availableCurses.Count);
                        selectedCurses.Add(availableCurses[randomIndex]);
                        if (ConfigClass.debugMode.Value)
                        {
                            Plugin.Logger.LogInfo($"[SelectCurses] Random selection: {availableCurses[randomIndex]} (index {randomIndex} of {availableCurses.Count})");
                        }
                    }
                    break;

                case Plugin.CurseSelectionMode.Multiple:
                    // Multiple curses
                    selectedCurses.AddRange(availableCurses);
                    if (ConfigClass.debugMode.Value)
                    {
                        Plugin.Logger.LogInfo($"[SelectCurses] Multiple curses selected: {string.Join(", ", selectedCurses)}");
                    }
                    break;
            }

            if (ConfigClass.debugMode.Value)
            {
                Plugin.Logger.LogInfo($"[SelectCurses] Final selected curses: {string.Join(", ", selectedCurses)}");
            }

            return selectedCurses;
        }

        private CharacterAfflictions.STATUSTYPE GetCurseTypeFromString(string curseTypeString)
        {
            return curseTypeString.ToLower() switch
            {
                "poison" => CharacterAfflictions.STATUSTYPE.Poison,
                "injury" => CharacterAfflictions.STATUSTYPE.Injury,
                "hunger" => CharacterAfflictions.STATUSTYPE.Hunger,
                "drowsy" => CharacterAfflictions.STATUSTYPE.Drowsy,
                "curse" => CharacterAfflictions.STATUSTYPE.Curse,
                "cold" => CharacterAfflictions.STATUSTYPE.Cold,
                "hot" => CharacterAfflictions.STATUSTYPE.Hot,
                _ => CharacterAfflictions.STATUSTYPE.Poison
            };
        }

        private void ApplySpecificCurse(Player player, string playerName, CharacterAfflictions.STATUSTYPE curseType)
        {
            float finalIntensity = GetFinalIntensityForCurseType(curseType);
            
            if (ConfigClass.debugMode.Value)
            {
                Plugin.Logger.LogInfo($"[ApplySpecificCurse] Applying {curseType} to {playerName} with intensity {finalIntensity}");
            }

            player.character.refs.afflictions.AddStatus(curseType, finalIntensity, false);
        }

        private float GetFinalIntensityForCurseType(CharacterAfflictions.STATUSTYPE curseType)
        {
            return curseType switch
            {
                CharacterAfflictions.STATUSTYPE.Poison => ConfigClass.poisonIntensity.Value,
                CharacterAfflictions.STATUSTYPE.Injury => ConfigClass.injuryIntensity.Value,
                CharacterAfflictions.STATUSTYPE.Hunger => ConfigClass.hungerIntensity.Value,
                CharacterAfflictions.STATUSTYPE.Drowsy => ConfigClass.drowsyIntensity.Value,
                CharacterAfflictions.STATUSTYPE.Curse => ConfigClass.curseStatusIntensity.Value,
                CharacterAfflictions.STATUSTYPE.Cold => ConfigClass.coldIntensity.Value,
                CharacterAfflictions.STATUSTYPE.Hot => ConfigClass.hotIntensity.Value,
                _ => 0.1f
            };
        }

        private string GetCurrentCurseTypeDisplay()
        {
            var availableCurses = GetAvailableCurseTypes();
            if (availableCurses.Count == 0) return "No Curse";

            switch (ConfigClass.curseSelectionMode.Value)
            {
                case Plugin.CurseSelectionMode.Single:
                    return GetCurseTypeDisplayName(GetCurseTypeFromString(ConfigClass.singleCurseType.Value));
                case Plugin.CurseSelectionMode.Random:
                    return "Random Curse";
                case Plugin.CurseSelectionMode.Multiple:
                    return "Multiple Curse";
                default:
                    return "Unknown Mode";
            }
        }

        private string GetCurseTypeDisplayName(CharacterAfflictions.STATUSTYPE curseType)
        {
            return curseType switch
            {
                CharacterAfflictions.STATUSTYPE.Poison => "Poison",
                CharacterAfflictions.STATUSTYPE.Injury => "Injury",
                CharacterAfflictions.STATUSTYPE.Hunger => "Hunger",
                CharacterAfflictions.STATUSTYPE.Drowsy => "Drowsy",
                CharacterAfflictions.STATUSTYPE.Curse => "Curse",
                CharacterAfflictions.STATUSTYPE.Cold => "Cold",
                CharacterAfflictions.STATUSTYPE.Hot => "Hot",
                _ => "Unknown"
            };
        }
    }
}