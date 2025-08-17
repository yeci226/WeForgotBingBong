using UnityEngine;
using System.Collections.Generic;

using WeForgotBingBong;
using Giu;
using System;

namespace Curse
{
  public class BingBongCurseLogic : MonoBehaviour
  {
    private ushort bingBongItemID;
    private float curseInterval = 2f;
    // private string curseType = "Poison";
    private bool showUI = true;
    private float curseIntensity = 1.0f;
    private float timer = 0f;
    private readonly Dictionary<Player, float> playerPickupTime = [];
    private const float PICKUP_DELAY = 0.5f;

    private readonly Dictionary<Player, List<MonoBehaviour>> activeCurses = [];

    public void Setup(ushort itemID, float interval, bool displayUI)
    {
      bingBongItemID = itemID;
      curseInterval = interval;
      // curseType = type;
      showUI = displayUI;
      curseIntensity = Plugin.Instance.curseIntensity.Value;
    }

    public void UpdateCurseIntensity()
    {
      curseIntensity = Plugin.Instance.curseIntensity.Value;
    }

    private Player[] cachedPlayers = [];
    private float lastPlayerUpdateTime = 0f;
    private const float PLAYER_UPDATE_INTERVAL = 1f;
    private bool lastCurseState = false;

    void Update()
    {
      UpdateCurseIntensity();

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

      if (showUI && localPlayer != null)
      {
        bool localPlayerHolding = CheckIfPlayerHasBingBong(localPlayer);
        float localPlayerDistance = Vector3.Distance(localPlayer.transform.position, transform.position);
        UIManager.instance?.SetBingBongStatus(localPlayerHolding, localPlayerDistance);
        // UIManager.instance?.SetCurseInfo(curseType, timer, curseInterval);
      }

      if (!isHeldByAny)
      {
        timer += Time.deltaTime;
        if (Plugin.Instance.debugMode.Value)
        {
          Plugin.Logger.LogInfo($"诅咒计时器: {timer:F1}s / {curseInterval:F1}s");
        }

        if (timer >= curseInterval)
        {
          // Plugin.Logger.LogInfo($"开始施加诅咒，当前诅咒类型: {curseType}");

          foreach (var player in cachedPlayers)
          {
            if (player != null)
            {
              Plugin.Logger.LogInfo($"对玩家 {player.name ?? "Unknown"} 施加诅咒");
              ApplyCurse(player);
            }
          }

          timer = 0f;
          Plugin.Logger.LogInfo("诅咒施加完成，计时器重置");
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

      if (!hasInAnySlotManual && !player.tempFullSlot.IsEmpty() && player.tempFullSlot.prefab != null)
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

      if (!hasInAnySlotManual && player.backpackSlot.hasBackpack && !player.backpackSlot.IsEmpty() && player.backpackSlot.prefab != null)
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

      bool hasNearby = false;
      if (!hasInSlot && !hasInAnySlotManual)
      {
        var nearbyItems = Physics.OverlapSphere(player.transform.position, 2f);
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
      Plugin.Logger.LogInfo($"使用備用方法對玩家 {playerName} 施加詛咒");

      try
      {
        if (player.character != null && player.character.refs != null && player.character.refs.afflictions != null)
        {
          Plugin.Logger.LogInfo($"通過player.character.refs找到玩家 {playerName} 的CharacterAfflictions組件");

          CharacterAfflictions.STATUSTYPE[] availableCurses = [
            CharacterAfflictions.STATUSTYPE.Poison,
            // CharacterAfflictions.STATUSTYPE.Injury,
            CharacterAfflictions.STATUSTYPE.Hunger,
            CharacterAfflictions.STATUSTYPE.Drowsy,
            // CharacterAfflictions.STATUSTYPE.Curse,
            CharacterAfflictions.STATUSTYPE.Cold,
            CharacterAfflictions.STATUSTYPE.Hot
          ];

          int randomIndex = UnityEngine.Random.Range(0, availableCurses.Length);
          CharacterAfflictions.STATUSTYPE statusType = availableCurses[randomIndex];

          Plugin.Logger.LogInfo($"隨機選擇的詛咒類型: {statusType} (索引: {randomIndex})");

          switch (statusType)
          {
            case CharacterAfflictions.STATUSTYPE.Poison:
              player.character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Poison, 0.1f * curseIntensity, false);
              Plugin.Logger.LogInfo($"對玩家 {playerName} 施加中毒詛咒，強度: {0.1f * curseIntensity}");
              break;

            case CharacterAfflictions.STATUSTYPE.Injury:
              player.character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Injury, 0.05f * curseIntensity, false);
              Plugin.Logger.LogInfo($"對玩家 {playerName} 施加受傷詛咒，強度: {0.05f * curseIntensity}");
              break;

            case CharacterAfflictions.STATUSTYPE.Hunger:
              player.character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Hunger, 0.1f * curseIntensity, false);
              Plugin.Logger.LogInfo($"對玩家 {playerName} 施加飢餓詛咒，強度: {0.1f * curseIntensity}");
              break;

            case CharacterAfflictions.STATUSTYPE.Drowsy:
              player.character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Drowsy, 0.1f * curseIntensity, false);
              Plugin.Logger.LogInfo($"對玩家 {playerName} 施加困倦詛咒，強度: {0.1f * curseIntensity}");
              break;

            case CharacterAfflictions.STATUSTYPE.Curse:
              player.character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Curse, 0.1f * curseIntensity, false);
              Plugin.Logger.LogInfo($"對玩家 {playerName} 施加詛咒狀態，強度: {0.1f * curseIntensity}");
              break;

            case CharacterAfflictions.STATUSTYPE.Cold:
              player.character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Cold, 0.1f * curseIntensity, false);
              Plugin.Logger.LogInfo($"對玩家 {playerName} 施加寒冷詛咒，強度: {0.1f * curseIntensity}");
              break;

            case CharacterAfflictions.STATUSTYPE.Hot:
              player.character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Hot, 0.1f * curseIntensity, false);
              Plugin.Logger.LogInfo($"對玩家 {playerName} 施加炎熱詛咒，強度: {0.1f * curseIntensity}");
              break;

            default:
              player.character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Poison, 0.1f * curseIntensity, false);
              Plugin.Logger.LogInfo($"使用默認中毒詛咒對玩家 {playerName}，強度: {0.1f * curseIntensity}");
              break;
          }
          return;
        }


        if (Plugin.Instance.debugMode.Value)
        {
          var allComponents = player.GetComponents<Component>();
          Plugin.Logger.LogInfo($"玩家 {playerName} 的所有組件:");
          foreach (var comp in allComponents)
          {
            if (comp != null)
            {
              Plugin.Logger.LogInfo($"  - {comp.GetType().Name}");
            }
          }

          Plugin.Logger.LogInfo($"player.character: {player.character != null}");
          if (player.character != null)
          {
            Plugin.Logger.LogInfo($"player.character.refs: {player.character.refs != null}");
            if (player.character.refs != null)
            {
              Plugin.Logger.LogInfo($"player.character.refs.afflictions: {player.character.refs.afflictions != null}");
            }
          }
        }

        Plugin.Logger.LogError($"所有方法都無法找到玩家 {playerName} 的詛咒系統組件");
      }
      catch (System.Exception ex)
      {
        Plugin.Logger.LogError($"施加詛咒時發生錯誤: {ex.Message}");
        Plugin.Logger.LogError($"錯誤堆疊: {ex.StackTrace}");
      }
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