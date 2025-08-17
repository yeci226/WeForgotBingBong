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

    // Configs
    private ConfigEntry<float> curseInterval = null!;
    // private ConfigEntry<string> curseType = null!;
    private ConfigEntry<bool> showUI = null!;
    public ConfigEntry<float> curseIntensity = null!;
    public ConfigEntry<bool> debugMode = null!;

    private void Awake()
    {
      Instance = this;
      Logger = base.Logger;
      Object.DontDestroyOnLoad(base.gameObject);
      SceneManager.sceneLoaded += OnSceneLoaded;

      curseInterval = Config.Bind("General", "CurseInterval", 2f, "施加負面效果的間隔秒數");
      // curseType = Config.Bind("General", "CurseType", "Poison", "負面效果類型，可選 Poison/Injury/Exhaustion/Paranoia");
      showUI = Config.Bind("UI", "ShowBingBongUI", true, "是否在螢幕上顯示 BingBong 狀態");
      curseIntensity = Config.Bind("General", "CurseIntensity", 1.0f, "詛咒效果強度倍數 (0.5-2.0)");
      debugMode = Config.Bind("Debug", "EnableDebugMode", true, "啟用調試模式，顯示詳細日誌");
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
          uiManagerGO.AddComponent<UIManager>();
        }

        StartCoroutine(MonitorPlayerInventoryChanges());

        StartCoroutine(MonitorItemStateChanges());
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

