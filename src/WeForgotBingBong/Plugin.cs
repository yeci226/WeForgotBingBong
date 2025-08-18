using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Zorro.Core;

using Gui;
using Curse;
using ConfigSpace;

namespace WeForgotBingBong
{
    [BepInPlugin("com.yeci.weforgotbingbong", "WeForgotBingBong!", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger = null!;
        public static Plugin Instance { get; private set; } = null!;

        private bool alreadyLoaded = false;
        private GameObject bingBong = null!;
        private ushort bingBongItemID;


        // Curse Selection Mode Enum
        public enum CurseSelectionMode
        {
            Single,      // Single curse type
            Random,      // Random selection
            Multiple     // Multiple curses
        }

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;
            Object.DontDestroyOnLoad(base.gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

            ConfigClass.InitConfig(Config);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode _)
        {
            if (!alreadyLoaded)
            {
                alreadyLoaded = true;

                StartCoroutine(InitializeBingBongLogic());
            }
        }

        private System.Collections.IEnumerator InitializeBingBongLogic()
        {
            yield return new WaitForSeconds(1f);

            ItemDatabase db = SingletonAsset<ItemDatabase>.Instance;
            if (db == null)
            {
                yield break;
            }

            bool foundBingBong = false;
            foreach (KeyValuePair<ushort, Item> kv in db.itemLookup)
            {
                if (kv.Value.name == "BingBong")
                {
                    bingBong = kv.Value.gameObject;
                    bingBongItemID = kv.Value.itemID;
                    foundBingBong = true;

                    var curseLogic = bingBong.AddComponent<BingBongCurseLogic>();
                    curseLogic.Setup(bingBongItemID, ConfigClass.curseInterval.Value, ConfigClass.showUI.Value);
                    break;
                }
            }

            if (!foundBingBong)
            {
                var allItems = UnityEngine.Object.FindObjectsByType<Item>(FindObjectsSortMode.None);
                foreach (var item in allItems)
                {
                    if (item.name.Contains("BingBong"))
                    {
                        bingBong = item.gameObject;
                        bingBongItemID = item.itemID;
                        foundBingBong = true;

                        var curseLogic = bingBong.AddComponent<BingBongCurseLogic>();
                        curseLogic.Setup(bingBongItemID, ConfigClass.curseInterval.Value, ConfigClass.showUI.Value);
                        break;
                    }
                }
            }

            if (foundBingBong)
            {
                if (ConfigClass.showUI.Value)
                {
                    var uiManagerGO = new GameObject("BingBongUIManager");
                    var uiManager = uiManagerGO.AddComponent<UIManager>();

                    // Start buffer time for local player immediately when UI is shown
                    StartCoroutine(StartLocalPlayerBuffer(uiManager));
                }

                // 立即尝试为本地玩家设置缓冲时间
                StartCoroutine(SetLocalPlayerBufferImmediately());

                StartCoroutine(MonitorPlayerInventoryChanges());

                StartCoroutine(MonitorItemStateChanges());

                StartCoroutine(MonitorPlayerJoins());
            }
        }

        private System.Collections.IEnumerator MonitorItemStateChanges()
        {
            while (true)
            {
                yield return new WaitForSeconds(3f); // 每3秒检查一次，减少频率

                var allItems = UnityEngine.Object.FindObjectsByType<Item>(FindObjectsSortMode.None);
                foreach (var item in allItems)
                {
                    if (item.itemID == bingBongItemID || item.name.Contains("BingBong"))
                    {
                        if (item.itemState == ItemState.Held)
                        {
                            var player = item.GetComponentInParent<Player>();
                            if (player != null)
                            {
                            }
                        }
                    }
                }
            }
        }

        private System.Collections.IEnumerator MonitorPlayerInventoryChanges()
        {
            while (true)
            {
                yield return new WaitForSeconds(2f);

                var players = UnityEngine.Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
                foreach (var player in players)
                {
                    if (player.photonView.IsMine)
                    {
                        bool currentlyHolding = CheckIfPlayerHasBingBong(player);
                        if (currentlyHolding)
                        {
                        }
                    }
                }
            }
        }

        private System.Collections.IEnumerator MonitorPlayerJoins()
        {
            var trackedPlayers = new HashSet<Player>();

            while (true)
            {
                yield return new WaitForSeconds(1f);

                var players = UnityEngine.Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
                
                // Check for genuinely new players only
                foreach (var player in players)
                {
                    if (player != null && player.photonView != null && !trackedPlayers.Contains(player))
                    {
                        // This is a genuinely new player
                        trackedPlayers.Add(player);
                        
                        var curseLogic = bingBong.GetComponent<BingBongCurseLogic>();
                        if (curseLogic != null)
                        {
                            curseLogic.OnPlayerJoined(player);
                            if (ConfigClass.debugMode.Value)
                            {
                                Logger.LogInfo($"New player detected: {player.name}, starting buffer time");
                            }
                        }
                    }
                }
                trackedPlayers.RemoveWhere(p => p == null);
            }
        }

        private System.Collections.IEnumerator SetLocalPlayerBufferImmediately()
        {
            // 立即尝试找到本地玩家并设置缓冲时间
            var players = UnityEngine.Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player != null && player.photonView != null && player.photonView.IsMine)
                {
                    var curseLogic = bingBong.GetComponent<BingBongCurseLogic>();
                    if (curseLogic != null)
                    {
                        curseLogic.OnPlayerJoined(player);
                        if (ConfigClass.debugMode.Value)
                        {
                            Logger.LogInfo($"Immediately set buffer time for local player: {player.name}");
                        }
                    }
                    break;
                }
            }

            yield break;
        }

        private System.Collections.IEnumerator StartLocalPlayerBuffer(UIManager uiManager)
        {
            // Wait a bit for the game to fully load
            yield return new WaitForSeconds(1f);

            // Find local player
            var players = UnityEngine.Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
            Player? localPlayer = null;

            foreach (var player in players)
            {
                if (player != null && player.photonView != null && player.photonView.IsMine)
                {
                    localPlayer = player;
                    break;
                }
            }

            if (localPlayer != null)
            {
                // Start buffer time for local player
                var curseLogic = bingBong.GetComponent<BingBongCurseLogic>();
                if (curseLogic != null)
                {
                    curseLogic.OnPlayerJoined(localPlayer);

                    if (ConfigClass.debugMode.Value)
                    {
                        Logger.LogInfo($"Started buffer time for local player: {localPlayer.name}");
                    }
                }
            }

            // Wait for local player to be found
            yield return new WaitForSeconds(2f);

            // Try again if local player wasn't found initially
            if (localPlayer == null)
            {
                players = UnityEngine.Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
                foreach (var player in players)
                {
                    if (player != null && player.photonView != null && player.photonView.IsMine)
                    {
                        localPlayer = player;
                        var curseLogic = bingBong.GetComponent<BingBongCurseLogic>();
                        if (curseLogic != null)
                        {
                            curseLogic.OnPlayerJoined(localPlayer);

                            if (ConfigClass.debugMode.Value)
                            {
                                Logger.LogInfo($"Started buffer time for local player (delayed): {localPlayer.name}");
                            }
                        }
                        break;
                    }
                }
            }
        }

        private bool CheckIfPlayerHasBingBong(Player player)
        {
            if (player == null || player.itemSlots == null) return false;

            for (int i = 0; i < player.itemSlots.Length; i++)
            {
                var slot = player.itemSlots[i];
                if (!slot.IsEmpty() && slot.prefab != null && slot.prefab.itemID == bingBongItemID)
                {
                    return true;
                }
            }

            if (!player.tempFullSlot.IsEmpty() && player.tempFullSlot.prefab != null &&
                player.tempFullSlot.prefab.itemID == bingBongItemID)
            {
                return true;
            }

            if (player.backpackSlot.hasBackpack && !player.backpackSlot.IsEmpty() &&
                player.backpackSlot.prefab != null && player.backpackSlot.prefab.itemID == bingBongItemID)
            {
                return true;
            }

            return false;
        }
    }
}

