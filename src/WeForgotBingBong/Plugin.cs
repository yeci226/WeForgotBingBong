using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Zorro.Core;

using Giu;
using Curse;

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

    // Basic Configuration
    private ConfigEntry<float> curseInterval = null!;
    private ConfigEntry<bool> showUI = null!;
    public ConfigEntry<float> curseIntensity = null!;
    public ConfigEntry<bool> debugMode = null!; 
    public ConfigEntry<float> playerJoinBufferTime = null!;
    
    // Curse Type Configuration
    public ConfigEntry<CurseSelectionMode> curseSelectionMode = null!;
    public ConfigEntry<string> singleCurseType = null!;
    public ConfigEntry<bool> enablePoison = null!;
    public ConfigEntry<bool> enableInjury = null!;
    public ConfigEntry<bool> enableHunger = null!;
    public ConfigEntry<bool> enableDrowsy = null!;
    public ConfigEntry<bool> enableCurse = null!;
    public ConfigEntry<bool> enableCold = null!;
    public ConfigEntry<bool> enableHot = null!;
    
    // Carrying Detection Configuration
    public ConfigEntry<bool> countBackpackAsCarrying = null!;
    public ConfigEntry<bool> countNearbyAsCarrying = null!;
    public ConfigEntry<float> nearbyDetectionRadius = null!;
    public ConfigEntry<bool> countTempSlotAsCarrying = null!;
    
    // Final Curse Effect Intensity Configuration
    public ConfigEntry<float> poisonIntensity = null!;
    public ConfigEntry<float> injuryIntensity = null!;
    public ConfigEntry<float> hungerIntensity = null!;
    public ConfigEntry<float> drowsyIntensity = null!;
    public ConfigEntry<float> curseStatusIntensity = null!;
    public ConfigEntry<float> coldIntensity = null!;
    public ConfigEntry<float> hotIntensity = null!;

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

      // Basic Configuration
      curseInterval = Config.Bind("General", "CurseInterval", 5.0f, "Interval in seconds between curse applications (Min: 0.5, Max: 60.0)");
      showUI = Config.Bind("UI", "ShowBingBongUI", true, "Whether to display BingBong status on screen");
      curseIntensity = Config.Bind("General", "CurseIntensity", 1.0f, "Amount of curse effect applied per application (0.1-5.0)");
      playerJoinBufferTime = Config.Bind("General", "PlayerJoinBufferTime", 30.0f, "Buffer time in seconds after player joins before curses can start (Min: 0, Max: 300)");
      debugMode = Config.Bind("Debug", "EnableDebugMode", true, "Enable debug mode for detailed logging");
      
      // Curse Type Selection Configuration
      curseSelectionMode = Config.Bind("CurseType", "SelectionMode", CurseSelectionMode.Single, 
        "Curse selection mode: Single(one type), Random(random selection), Multiple(multiple curses)");
      singleCurseType = Config.Bind("CurseType", "SingleCurseType", "Poison", 
        "Single curse type (effective when SelectionMode is set to Single)");
      
      // Individual Curse Type Switches
      enablePoison = Config.Bind("CurseType", "EnablePoison", true, "Enable poison curse");
      enableInjury = Config.Bind("CurseType", "EnableInjury", true, "Enable injury curse");
      enableHunger = Config.Bind("CurseType", "EnableHunger", true, "Enable hunger curse");
      enableDrowsy = Config.Bind("CurseType", "EnableDrowsy", true, "Enable drowsy curse");
      enableCurse = Config.Bind("CurseType", "EnableCurse", false, "Enable curse status");
      enableCold = Config.Bind("CurseType", "EnableCold", true, "Enable cold curse");
      enableHot = Config.Bind("CurseType", "EnableHot", true, "Enable hot curse");
      
      // Carrying Detection Configuration
      countBackpackAsCarrying = Config.Bind("CarryingDetection", "CountBackpackAsCarrying", true, 
        "Whether BingBong in backpack counts as carrying (prevents curses)");
      countNearbyAsCarrying = Config.Bind("CarryingDetection", "CountNearbyAsCarrying", true, 
        "Whether nearby BingBong counts as carrying (prevents curses)");
      nearbyDetectionRadius = Config.Bind("CarryingDetection", "NearbyDetectionRadius", 10.0f, 
        "Nearby detection radius (effective when CountNearbyAsCarrying is true)");
      countTempSlotAsCarrying = Config.Bind("CarryingDetection", "CountTempSlotAsCarrying", true, 
        "Whether BingBong in temporary item slot counts as carrying");
      
      // Final Curse Effect Intensity Configuration
      poisonIntensity = Config.Bind("CurseIntensity", "PoisonIntensity", 0.1f, "Final poison curse intensity (0.1-10.0)");
      injuryIntensity = Config.Bind("CurseIntensity", "InjuryIntensity", 0.05f, "Final injury curse intensity (0.1-10.0)");
      hungerIntensity = Config.Bind("CurseIntensity", "HungerIntensity", 0.1f, "Final hunger curse intensity (0.1-10.0)");
      drowsyIntensity = Config.Bind("CurseIntensity", "DrowsyIntensity", 0.1f, "Final drowsy curse intensity (0.1-10.0)");
      curseStatusIntensity = Config.Bind("CurseIntensity", "CurseStatusIntensity", 0.1f, "Final curse status intensity (0.1-10.0)");
      coldIntensity = Config.Bind("CurseIntensity", "ColdIntensity", 0.1f, "Final cold curse intensity (0.1-10.0)");
      hotIntensity = Config.Bind("CurseIntensity", "HotIntensity", 0.1f, "Final hot curse intensity (0.1-10.0)");
      
      // Setup configuration value constraints
      SetupConfigConstraints();
    }

    private void SetupConfigConstraints()
    {
      // Curse interval constraints
      curseInterval.SettingChanged += (sender, e) => {
        if (curseInterval.Value < 0.5f) curseInterval.Value = 0.5f;
        if (curseInterval.Value > 60.0f) curseInterval.Value = 60.0f;
      };
      
      // Curse intensity constraints
      curseIntensity.SettingChanged += (sender, e) => {
        if (curseIntensity.Value < 0.1f) curseIntensity.Value = 0.1f;
        if (curseIntensity.Value > 5.0f) curseIntensity.Value = 5.0f;
      };
      
      // Nearby detection radius constraints
      nearbyDetectionRadius.SettingChanged += (sender, e) => {
        if (nearbyDetectionRadius.Value < 0.5f) nearbyDetectionRadius.Value = 0.5f;
        if (nearbyDetectionRadius.Value > 10.0f) nearbyDetectionRadius.Value = 10.0f;
      };
      
      // Player join buffer time constraints
      playerJoinBufferTime.SettingChanged += (sender, e) => {
        if (playerJoinBufferTime.Value < 0f) playerJoinBufferTime.Value = 0f;
        if (playerJoinBufferTime.Value > 300f) playerJoinBufferTime.Value = 300f;
      };
      
      // Final curse intensity constraints
      poisonIntensity.SettingChanged += (sender, e) => {
        if (poisonIntensity.Value < 0.1f) poisonIntensity.Value = 0.1f;
        if (poisonIntensity.Value > 10.0f) poisonIntensity.Value = 10.0f;
      };
      
      injuryIntensity.SettingChanged += (sender, e) => {
        if (injuryIntensity.Value < 0.1f) injuryIntensity.Value = 0.1f;
        if (injuryIntensity.Value > 10.0f) injuryIntensity.Value = 10.0f;
      };
      
      hungerIntensity.SettingChanged += (sender, e) => {
        if (hungerIntensity.Value < 0.1f) hungerIntensity.Value = 0.1f;
        if (hungerIntensity.Value > 10.0f) hungerIntensity.Value = 10.0f;
      };
      
      drowsyIntensity.SettingChanged += (sender, e) => {
        if (drowsyIntensity.Value < 0.1f) drowsyIntensity.Value = 0.1f;
        if (drowsyIntensity.Value > 10.0f) drowsyIntensity.Value = 10.0f;
      };
      
      curseStatusIntensity.SettingChanged += (sender, e) => {
        if (curseStatusIntensity.Value < 0.1f) curseStatusIntensity.Value = 0.1f;
        if (curseStatusIntensity.Value > 10.0f) curseStatusIntensity.Value = 10.0f;
      };
      
      coldIntensity.SettingChanged += (sender, e) => {
        if (coldIntensity.Value < 0.1f) coldIntensity.Value = 0.1f;
        if (coldIntensity.Value > 10.0f) coldIntensity.Value = 10.0f;
      };
      
      hotIntensity.SettingChanged += (sender, e) => {
        if (hotIntensity.Value < 0.1f) hotIntensity.Value = 0.1f;
        if (hotIntensity.Value > 10.0f) hotIntensity.Value = 10.0f;
      };
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
          curseLogic.Setup(bingBongItemID, curseInterval.Value, showUI.Value);
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
            curseLogic.Setup(bingBongItemID, curseInterval.Value, showUI.Value);
            break;
          }
        }
      }

      if (foundBingBong)
        {
          if (showUI.Value)
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
      var lastPlayerCount = 0;
      
      while (true)
      {
        yield return new WaitForSeconds(1f);
        
        var players = UnityEngine.Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
        int currentPlayerCount = players.Length;
        
        // Check for new players
        if (currentPlayerCount > lastPlayerCount)
        {
          foreach (var player in players)
          {
            if (player != null && player.photonView != null)
            {
              // Find the curse logic component and notify about new player
              var curseLogic = bingBong.GetComponent<BingBongCurseLogic>();
              if (curseLogic != null)
              {
                curseLogic.OnPlayerJoined(player);
              }
            }
          }
        }
        
        lastPlayerCount = currentPlayerCount;
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
            if (debugMode.Value)
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
      Player localPlayer = null;
      
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
          
          if (debugMode.Value)
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
              
              if (debugMode.Value)
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

