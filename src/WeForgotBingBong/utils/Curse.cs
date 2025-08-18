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

    private Player[] cachedPlayers = [];
    private float lastPlayerUpdateTime = 0f;
    private const float PLAYER_UPDATE_INTERVAL = 1f;
    private bool lastCurseState = false;
    private Item[] cachedBingBongs = [];
    private float lastBingBongUpdateTime = 0f;
    private const float BINGBONG_UPDATE_INTERVAL = 2f;

    private readonly Dictionary<Player, List<MonoBehaviour>> activeCurses = [];

    public void Setup(ushort itemID)
    {
      bingBongItemID = itemID;
    }

    void Update()
    {
      if (LoadingScreenHandler.loading)
      {
        return;
      }

      if (Time.time - lastPlayerUpdateTime >= PLAYER_UPDATE_INTERVAL)
      {
        cachedPlayers = FindObjectsByType<Player>(FindObjectsSortMode.None);
        lastPlayerUpdateTime = Time.time;
      }
      if (cachedPlayers.Length == 0) return;
      if (Time.time - lastBingBongUpdateTime >= BINGBONG_UPDATE_INTERVAL)
      {
        List<Item> found = [];
        foreach (var item in FindObjectsByType<Item>(FindObjectsSortMode.None))
        {
          if (item != null && (item.itemID == bingBongItemID || item.name.Contains("BingBong")))
          {
            if (item.transform.root == item.transform)
            {
              found.Add(item);
            }
          }
        }
        cachedBingBongs = [.. found];
        lastBingBongUpdateTime = Time.time;
      }

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

      bool inInvincibility = RunManager.Instance != null &&
                            RunManager.Instance.timeSinceRunStarted < ConfigClass.invincibilityPeriod.Value;
      float invRemaining = 0f;
      if (inInvincibility && RunManager.Instance != null)
      {
        invRemaining = ConfigClass.invincibilityPeriod.Value - RunManager.Instance.timeSinceRunStarted;
      }

      if (ConfigClass.showUI.Value && localPlayer != null)
      {
        Vector3 playerPos;
        if (localPlayer.character != null && localPlayer.character.refs.head != null)
        {
          playerPos = localPlayer.character.refs.head.transform.position;
        }
        else
        {
          playerPos = localPlayer.character?.refs?.head?.transform?.position
          ?? localPlayer.character?.transform?.position
          ?? localPlayer.transform.position;
        }

        float localPlayerDistance = float.MaxValue;
        foreach (var item in cachedBingBongs)
        {
          if (item == null || item.itemState == ItemState.Held || item.itemState == ItemState.InBackpack) continue;

          // 只計算地上 BingBong 的距離
          float dist = Vector3.Distance(playerPos, item.transform.position);
          if (dist < localPlayerDistance)
          {
            localPlayerDistance = dist;
          }
        }

        if (localPlayerDistance == float.MaxValue)
          localPlayerDistance = -1f;

        UIManager.instance?.SetBingBongStatus(isHeldByAny, localPlayerDistance);

        if (inInvincibility)
        {
          UIManager.instance?.SetInvincibilityInfo(true, invRemaining);
          UIManager.instance?.SetCurseInfo(0f);
        }
        else
        {
          UIManager.instance?.SetInvincibilityInfo(false, 0f);

          if (!isHeldByAny)
          {
            float remaining = Mathf.Max(0f, ConfigClass.curseInterval.Value - timer);
            UIManager.instance?.SetCurseInfo(remaining);
          }
          else
          {
            UIManager.instance?.SetCurseInfo(0f);
          }
        }
      }


      if (!inInvincibility && !isHeldByAny)
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
  }
}