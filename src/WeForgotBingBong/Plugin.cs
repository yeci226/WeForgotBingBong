using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Zorro.Core;

using Gui;
using Curse;
using ConfigSpace;
using System.Linq;

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
      SceneManager.sceneLoaded += OnSceneLoaded;

      ConfigClass.InitConfig(Config);
    }

    private static GameObject? uiManagerGO;

    private void OnSceneLoaded(Scene scene, LoadSceneMode _)
    {
      if (!alreadyLoaded)
      {
        alreadyLoaded = true;
        StartCoroutine(InitializeBingBongLogic());
      }

      ManageUIManager();
    }

    private void ManageUIManager()
    {
      if (ConfigClass.showUI.Value)
      {
        if (uiManagerGO == null)
        {
          uiManagerGO = new GameObject("BingBongUIManager");
          uiManagerGO.AddComponent<UIManager>();
        }
      }
      else
      {
        if (uiManagerGO != null)
        {
          Destroy(uiManagerGO);
          uiManagerGO = null;
        }
      }
    }

    private void OnDestroy()
    {
      if (uiManagerGO != null)
      {
        Destroy(uiManagerGO);
        uiManagerGO = null;
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
          curseLogic.Setup(bingBongItemID);
          break;
        }
      }

      if (foundBingBong)
      {
        if (ConfigClass.showUI.Value)
        {
          var uiManagerGO = new GameObject("BingBongUIManager");
          uiManagerGO.AddComponent<UIManager>();
        }

        StartCoroutine(StartLocalPlayerSetup());
        StartCoroutine(MonitorPlayerJoins());
      }
    }


    private System.Collections.IEnumerator MonitorPlayerJoins()
    {
      var trackedPlayers = new HashSet<Player>();

      while (true)
      {
        yield return new WaitForSeconds(1f);

        var players = FindObjectsByType<Player>(FindObjectsSortMode.None);

        // Check for genuinely new players only
        foreach (var player in players)
        {
          if (player != null && player.photonView != null && !trackedPlayers.Contains(player))
          {
            // This is a genuinely new player
            trackedPlayers.Add(player);

            if (ConfigClass.debugMode.Value)
            {
              Logger.LogInfo($"New player detected: {player.name}");
            }
          }
        }
        trackedPlayers.RemoveWhere(p => p == null);
      }
    }

    private System.Collections.IEnumerator StartLocalPlayerSetup()
    {
      yield return new WaitForSeconds(1f);

      var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
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
        if (ConfigClass.debugMode.Value)
        {
          Logger.LogInfo($"Local player ready: {localPlayer.name}");
        }
      }
    }

    public static bool CheckIfPlayerHasBingBong(Player player, ushort bingBongItemID)
    {
      if (player?.itemSlots == null) return false;

      bool hasInSlots = player.itemSlots.Any(slot =>
          !slot.IsEmpty() && slot.prefab != null &&
          (slot.prefab.itemID == bingBongItemID || slot.prefab.name.Contains("BingBong")));

      if (hasInSlots) return true;

      if (ConfigClass.countTempSlotAsCarrying.Value && HasBingBongInSlot(player.tempFullSlot, bingBongItemID))
        return true;
      return ConfigClass.countBackpackAsCarrying.Value && HasBingBongInBackPack(player, bingBongItemID);
    }

    private static bool HasBingBongInSlot(ItemSlot slot, ushort bingBongItemID)
    {
      return !slot.IsEmpty() && slot.prefab != null &&
             (slot.prefab.itemID == bingBongItemID || slot.prefab.name.Contains("BingBong"));
    }

    private static bool HasBingBongInBackPack(Player player, ushort bingBongItemID)
    {
      if (!player.backpackSlot.hasBackpack || player.backpackSlot.IsEmpty())
        return false;

      try
      {
        BackpackReference backpackRef = BackpackReference.GetFromEquippedBackpack(player.character);
        BackpackData data = backpackRef.GetData();

        if (data?.itemSlots != null)
        {
          foreach (var slot in data.itemSlots)
          {
            if (HasBingBongInSlot(slot, bingBongItemID))
            {
              if (ConfigClass.debugMode.Value)
              {
                Logger.LogInfo($"[BackpackCheck] Found BingBong inside backpack of {player.name}");
              }
              return true;
            }
          }
        }
      }
      catch (System.Exception ex)
      {
        if (ConfigClass.debugMode.Value)
        {
          Logger.LogWarning($"[BackpackCheck] Error checking backpack for {player.name}: {ex.Message}");
        }
      }

      return false;
    }
  }
}

