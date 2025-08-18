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
        private readonly Dictionary<Player, float> playerJoinTime = [];
        private const float PICKUP_DELAY = 0.5f;

        private Player[] cachedPlayers = [];
        private float lastPlayerUpdateTime = 0f;
        private const float PLAYER_UPDATE_INTERVAL = 1f;
        private bool lastCurseState = false;

        private readonly Dictionary<Player, List<MonoBehaviour>> activeCurses = [];

        public void Setup(ushort itemID, float interval, bool displayUI)
        {
            bingBongItemID = itemID;
            // interval 和 displayUI 參數現在直接從 ConfigClass 讀取，所以不需要存儲
        }

        void Start()
        {
            StartCoroutine(InitializeBufferForExistingPlayers());
        }

        private System.Collections.IEnumerator InitializeBufferForExistingPlayers()
        {
            yield return new WaitForSeconds(0.5f);

            var players = UnityEngine.Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player == null) continue;
                playerJoinTime[player] = Time.time;
                if (ConfigClass.debugMode.Value)
                {
                    Plugin.Logger.LogInfo($"[InitBuffer] Player {player.name} buffered at {Time.time:F1}s, duration {ConfigClass.playerJoinBufferTime.Value:F1}s");
                }
            }

            // 再次尝试一遍，处理本地玩家延迟出现的情况
            yield return new WaitForSeconds(1.0f);
            players = UnityEngine.Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player == null) continue;
                if (!playerJoinTime.ContainsKey(player))
                {
                    playerJoinTime[player] = Time.time;
                    if (ConfigClass.debugMode.Value)
                    {
                        Plugin.Logger.LogInfo($"[RetryBuffer] Late player {player.name} buffered at {Time.time:F1}s");
                    }
                }
            }
        }

        void Update()
        {
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

                bool isHeld = CheckIfPlayerHasBingBong(player);
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
                bool localPlayerHolding = CheckIfPlayerHasBingBong(localPlayer);
                float localPlayerDistance = Vector3.Distance(localPlayer.transform.position, transform.position);
                UIManager.instance?.SetBingBongStatus(localPlayerHolding, localPlayerDistance);

                string currentCurseType = GetCurrentCurseTypeDisplay();
                UIManager.instance?.SetCurseInfo(currentCurseType, timer);

                // 更新缓冲时间信息到UI
                bool isInBuffer = IsPlayerInBufferTime(localPlayer);
                float remainingBufferTime = GetPlayerRemainingBufferTime(localPlayer);
                UIManager.instance?.SetBufferTimeInfo(isInBuffer, remainingBufferTime);

                // 调试信息
                if (ConfigClass.debugMode.Value)
                {
                    Plugin.Logger.LogInfo($"[UI Update] Player: {localPlayer.name}, InBuffer: {isInBuffer}, Remaining: {remainingBufferTime:F1}s, Timer: {timer:F1}s");
                }
            }

            if (!isHeldByAny)
            {
                // Check if any players are still in buffer time
                bool anyPlayerInBuffer = false;
                foreach (var player in cachedPlayers)
                {
                    if (player != null && !IsPlayerReadyForCurse(player))
                    {
                        anyPlayerInBuffer = true;
                        if (ConfigClass.debugMode.Value)
                        {
                            Plugin.Logger.LogInfo($"[BufferCheck] Player {player.name} still in buffer");
                        }
                        break;
                    }
                }

                if (ConfigClass.debugMode.Value)
                {
                    Plugin.Logger.LogInfo($"[BufferCheck] AnyPlayerInBuffer: {anyPlayerInBuffer}, Timer: {timer:F1}s");
                }

                if (!anyPlayerInBuffer)
                {
                    timer += Time.deltaTime;
                    if (ConfigClass.debugMode.Value)
                    {
                        Plugin.Logger.LogInfo($"[Timer] Current timer: {timer:F1}s / {ConfigClass.curseInterval.Value:F1}s");
                    }

                    if (timer >= ConfigClass.curseInterval.Value)
                    {
                        if (ConfigClass.debugMode.Value)
                        {
                            Plugin.Logger.LogInfo($"[Curse] Timer reached {ConfigClass.curseInterval.Value}s, applying curses to ready players");
                        }

                        int cursedPlayerCount = 0;
                        foreach (var player in cachedPlayers)
                        {
                            if (player != null && IsPlayerReadyForCurse(player))
                            {
                                if (ConfigClass.debugMode.Value)
                                {
                                    Plugin.Logger.LogInfo($"[Curse] Applying curse to player: {player.name}");
                                }
                                ApplyCurse(player);
                                cursedPlayerCount++;
                            }
                            else if (player != null)
                            {
                                if (ConfigClass.debugMode.Value)
                                {
                                    Plugin.Logger.LogInfo($"[Curse] Player {player.name} not ready for curse");
                                }
                            }
                        }

                        if (ConfigClass.debugMode.Value)
                        {
                            Plugin.Logger.LogInfo($"[Curse] Applied curses to {cursedPlayerCount} players");
                        }

                        timer = 0f;
                    }
                }

            }
            else
            {
                if (timer > 0)
                {
                    timer = 0f;

                    foreach (var player in cachedPlayers)
                    {
                        if (player != null) ClearCurse(player);
                    }
                }
            }

            if (lastCurseState != isHeldByAny)
            {
                lastCurseState = isHeldByAny;
            }
        }

        public void OnItemStateChanged(Player player)
        {
            if (player != null)
            {
                playerPickupTime[player] = Time.time;
            }
        }

        public void OnPlayerJoined(Player player)
        {
            if (player != null)
            {
                playerJoinTime[player] = Time.time;
                if (ConfigClass.debugMode.Value)
                {
                    Plugin.Logger.LogInfo($"Player {player.name} joined, buffer time started at {Time.time:F1}s, buffer duration: {ConfigClass.playerJoinBufferTime.Value:F1}s");
                }
            }
        }

        public float GetPlayerRemainingBufferTime(Player player)
        {
            if (player == null || !playerJoinTime.ContainsKey(player)) return 0f;

            float timeSinceJoin = Time.time - playerJoinTime[player];
            float bufferTime = ConfigClass.playerJoinBufferTime.Value;
            float remainingTime = bufferTime - timeSinceJoin;

            return Mathf.Max(0f, remainingTime);
        }

        public bool IsPlayerInBufferTime(Player player)
        {
            if (player == null) return false;
            return playerJoinTime.ContainsKey(player) && GetPlayerRemainingBufferTime(player) > 0f;
        }

        private bool IsPlayerReadyForCurse(Player player)
        {
            if (player == null) return false;

            // Check if player has joined recently and is still in buffer time
            if (playerJoinTime.ContainsKey(player))
            {
                float timeSinceJoin = Time.time - playerJoinTime[player];
                float bufferTime = ConfigClass.playerJoinBufferTime.Value;

                if (ConfigClass.debugMode.Value)
                {
                    Plugin.Logger.LogInfo($"Player {player.name} buffer check: timeSinceJoin={timeSinceJoin:F1}s, bufferTime={bufferTime:F1}s, remaining={bufferTime - timeSinceJoin:F1}s");
                }

                if (timeSinceJoin < bufferTime)
                {
                    return false;
                }
                else
                {
                    // Remove from join time tracking after buffer expires
                    playerJoinTime.Remove(player);
                    if (ConfigClass.debugMode.Value)
                    {
                        Plugin.Logger.LogInfo($"Player {player.name} buffer time expired, removed from tracking");
                    }
                }
            }

            return true;
        }

        private bool CheckIfPlayerHasBingBong(Player player)
        {
            if (player == null || player.itemSlots == null) return false;

            if (playerPickupTime.ContainsKey(player))
            {
                float timeSincePickup = Time.time - playerPickupTime[player];
                if (timeSincePickup < PICKUP_DELAY)
                {
                    return false;
                }
                else
                {
                    playerPickupTime.Remove(player);
                }
            }

            bool hasInSlot = false;
            try
            {
                hasInSlot = player.HasInAnySlot(bingBongItemID);
            }
            catch (System.Exception)
            {
            }

            bool hasInAnySlotManual = false;
            for (int i = 0; i < player.itemSlots.Length; i++)
            {
                var slot = player.itemSlots[i];
                if (!slot.IsEmpty() && slot.prefab != null)
                {
                    if (slot.prefab.itemID == bingBongItemID)
                    {
                        hasInAnySlotManual = true;
                        break;
                    }
                    else if (slot.prefab.name.Contains("BingBong"))
                    {
                        hasInAnySlotManual = true;
                        break;
                    }
                }
            }

            // Check temporary item slot
            if (!hasInAnySlotManual && ConfigClass.countTempSlotAsCarrying.Value &&
                !player.tempFullSlot.IsEmpty() && player.tempFullSlot.prefab != null)
            {
                if (player.tempFullSlot.prefab.itemID == bingBongItemID)
                {
                    hasInAnySlotManual = true;
                }
                else if (player.tempFullSlot.prefab.name.Contains("BingBong"))
                {
                    hasInAnySlotManual = true;
                }
            }

            // Check backpack
            if (!hasInAnySlotManual && ConfigClass.countBackpackAsCarrying.Value &&
                player.backpackSlot.hasBackpack && !player.backpackSlot.IsEmpty() &&
                player.backpackSlot.prefab != null)
            {
                if (player.backpackSlot.prefab.itemID == bingBongItemID)
                {
                    hasInAnySlotManual = true;
                }
                else if (player.backpackSlot.prefab.name.Contains("BingBong"))
                {
                    hasInAnySlotManual = true;
                }
            }

            // Check nearby items
            bool hasNearby = false;
            if (!hasInSlot && !hasInAnySlotManual && ConfigClass.countNearbyAsCarrying.Value)
            {
                float radius = ConfigClass.nearbyDetectionRadius.Value;
                var nearbyItems = Physics.OverlapSphere(player.transform.position, radius);
                foreach (var collider in nearbyItems)
                {
                    var item = collider.GetComponent<Item>();
                    if (item != null && (item.itemID == bingBongItemID || item.name.Contains("BingBong")))
                    {
                        hasNearby = true;
                        break;
                    }
                }
            }

            bool finalResult = hasInSlot || hasInAnySlotManual || hasNearby;

            return finalResult;
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
                    // Select curse types based on configuration
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

                var allComponents = player.GetComponents<Component>();
                foreach (var comp in allComponents)
                {
                    if (comp != null)
                    {
                        // 可以在这里添加组件检查逻辑
                    }
                }
                if (player.character != null)
                {
                    if (player.character.refs != null)
                    {
                        // 可以在这里添加角色引用检查逻辑
                    }
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
                CharacterAfflictions.STATUSTYPE.Poison => "Poison ",
                CharacterAfflictions.STATUSTYPE.Injury => "Injury",
                CharacterAfflictions.STATUSTYPE.Hunger => "Hunger",
                CharacterAfflictions.STATUSTYPE.Drowsy => "Drowsy",
                CharacterAfflictions.STATUSTYPE.Curse => "Curse",
                CharacterAfflictions.STATUSTYPE.Cold => "Cold",
                CharacterAfflictions.STATUSTYPE.Hot => "Hot",
                _ => "Unknown"
            };
        }

        void ClearCurse(Player player)
        {
            if (player == null) return;

            if (!activeCurses.ContainsKey(player))
            {
                return;
            }

            foreach (var curse in activeCurses[player])
            {
                if (curse != null)
                {
                    curse.enabled = false;
                }
            }
            activeCurses[player].Clear();
        }
    }
}