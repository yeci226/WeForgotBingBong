using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Zorro.Core;

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
    private ConfigEntry<string> curseType = null!;
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
      curseType = Config.Bind("General", "CurseType", "Poison", "負面效果類型，可選 Poison/Injury/Exhaustion/Paranoia");
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
          curseLogic.Setup(bingBongItemID, curseInterval.Value, curseType.Value, showUI.Value);
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
            curseLogic.Setup(bingBongItemID, curseInterval.Value, curseType.Value, showUI.Value);
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

  public class BingBongCurseLogic : MonoBehaviour
  {
    private ushort bingBongItemID;
    private float curseInterval = 2f;
    private string curseType = "Poison";
    private bool showUI = true;
    private float curseIntensity = 1.0f;
    private float timer = 0f;
    private Dictionary<Player, float> playerPickupTime = new Dictionary<Player, float>();
    private const float PICKUP_DELAY = 0.5f;

    private Dictionary<Player, List<MonoBehaviour>> activeCurses = new Dictionary<Player, List<MonoBehaviour>>();

    public void Setup(ushort itemID, float interval, string type, bool displayUI)
    {
      bingBongItemID = itemID;
      curseInterval = interval;
      curseType = type;
      showUI = displayUI;
      curseIntensity = Plugin.Instance.curseIntensity.Value;
    }

    public void UpdateCurseIntensity()
    {
      curseIntensity = Plugin.Instance.curseIntensity.Value;
    }

    private Player[] cachedPlayers = new Player[0];
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
      Player localPlayer = null;
      List<string> carryingPlayers = new List<string>();

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
        UIManager.instance?.SetCurseInfo(curseType, timer, curseInterval);
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
          Plugin.Logger.LogInfo($"开始施加诅咒，当前诅咒类型: {curseType}");

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
      Plugin.Logger.LogInfo($"对玩家 {playerName} 施加 {curseType} 诅咒，强度: {curseIntensity}");

      if (!activeCurses.ContainsKey(player))
      {
        activeCurses[player] = new List<MonoBehaviour>();
      }

      ApplyCurseAlternative(player, playerName);

      try
      {
        switch (curseType)
        {
          case "Poison":
            ApplyPoisonCurse(player, playerName);
            break;

          case "Injury":
            ApplyInjuryCurse(player, playerName);
            break;

          default:
            Plugin.Logger.LogWarning($"未知的诅咒类型: {curseType}，使用默认中毒效果");
            ApplyPoisonCurse(player, playerName);
            break;
        }
      }
      catch (System.Exception ex)
      {
        Plugin.Logger.LogError($"组件方法失败，已使用备用方法: {ex.Message}");
      }
    }

    private void ApplyPoisonCurse(Player player, string playerName)
    {
      try
      {
        var poison = player.gameObject.GetComponent<Action_InflictPoison>();
        if (poison == null)
        {
          poison = player.gameObject.AddComponent<Action_InflictPoison>();
          Plugin.Logger.LogInfo($"为玩家 {playerName} 创建新的中毒效果组件");
        }

        if (poison == null)
        {
          Plugin.Logger.LogError($"无法创建中毒组件");
          return;
        }

        poison.inflictionTime = 5f * curseIntensity;
        poison.poisonPerSecond = 0.05f * curseIntensity;
        poison.enabled = true;

        try
        {
          poison.RunAction();
          Plugin.Logger.LogInfo($"玩家 {playerName} 中毒效果已激活，持续时间: {poison.inflictionTime}秒，每秒伤害: {poison.poisonPerSecond}");
        }
        catch (System.Exception ex)
        {
          Plugin.Logger.LogError($"执行中毒效果时出错: {ex.Message}");
          Plugin.Logger.LogError($"中毒组件类型: {poison.GetType().Name}");
          Plugin.Logger.LogError($"中毒组件状态: enabled={poison.enabled}");
        }

        activeCurses[player].Add(poison);
      }
      catch (System.Exception ex)
      {
        Plugin.Logger.LogError($"创建中毒效果时出错: {ex.Message}");
      }
    }

    private void ApplyInjuryCurse(Player player, string playerName)
    {
      try
      {
        var injury = player.gameObject.GetComponent<Action_ModifyStatus>();
        if (injury == null)
        {
          injury = player.gameObject.AddComponent<Action_ModifyStatus>();
          Plugin.Logger.LogInfo($"为玩家 {playerName} 创建新的受伤效果组件");
        }

        if (injury == null)
        {
          Plugin.Logger.LogError($"无法创建受伤组件");
          return;
        }

        injury.statusType = CharacterAfflictions.STATUSTYPE.Injury;
        injury.changeAmount = 0.2f * curseIntensity;
        injury.enabled = true;

        try
        {
          injury.RunAction();
          Plugin.Logger.LogInfo($"玩家 {playerName} 受伤效果已激活，伤害值: {injury.changeAmount}");
        }
        catch (System.Exception ex)
        {
          Plugin.Logger.LogError($"执行受伤效果时出错: {ex.Message}");
          Plugin.Logger.LogError($"受伤组件类型: {injury.GetType().Name}");
          Plugin.Logger.LogError($"受伤组件状态: enabled={injury.enabled}");
        }

        activeCurses[player].Add(injury);
      }
      catch (System.Exception ex)
      {
        Plugin.Logger.LogError($"创建受伤效果时出错: {ex.Message}");
      }
    }

    private void ApplyExhaustionCurse(Player player, string playerName)
    {
      var exhaustion = player.gameObject.GetComponent<Action_ModifyStatus>();
      if (exhaustion == null)
      {
        exhaustion = player.gameObject.AddComponent<Action_ModifyStatus>();
        Plugin.Logger.LogInfo($"为玩家 {playerName} 创建新的疲惫效果组件");
      }

      exhaustion.statusType = CharacterAfflictions.STATUSTYPE.Hunger;
      exhaustion.changeAmount = 0.15f * curseIntensity;
      exhaustion.enabled = true;

      activeCurses[player].Add(exhaustion);
      Plugin.Logger.LogInfo($"玩家 {playerName} 疲惫效果已激活，饥饿值: {exhaustion.changeAmount}");
    }

    private void ApplyParanoiaCurse(Player player, string playerName)
    {
      var paranoia = player.gameObject.GetComponent<Action_ModifyStatus>();
      if (paranoia == null)
      {
        paranoia = player.gameObject.AddComponent<Action_ModifyStatus>();
        Plugin.Logger.LogInfo($"为玩家 {playerName} 创建新的偏执效果组件");
      }

      paranoia.statusType = CharacterAfflictions.STATUSTYPE.Hunger;
      paranoia.changeAmount = 0.25f * curseIntensity;
      paranoia.enabled = true;

      activeCurses[player].Add(paranoia);
      Plugin.Logger.LogInfo($"玩家 {playerName} 偏执效果已激活，饥饿值: {paranoia.changeAmount}");
    }

    private void ApplyRandomCurse(Player player, string playerName)
    {
      string[] curseTypes = { "Poison", "Injury", "Exhaustion", "Paranoia" };
      string randomCurse = curseTypes[Random.Range(0, curseTypes.Length)];

      Plugin.Logger.LogInfo($"为玩家 {playerName} 随机选择诅咒类型: {randomCurse}");

      switch (randomCurse)
      {
        case "Poison":
          ApplyPoisonCurse(player, playerName);
          break;
        case "Injury":
          ApplyInjuryCurse(player, playerName);
          break;
      }
    }

    private void ApplyCurseAlternative(Player player, string playerName)
    {
      Plugin.Logger.LogInfo($"使用備用方法對玩家 {playerName} 施加詛咒");

      try
      {
        if (player.character != null && player.character.refs != null && player.character.refs.afflictions != null)
        {
          Plugin.Logger.LogInfo($"通過player.character.refs找到玩家 {playerName} 的CharacterAfflictions組件");

          switch (curseType.ToLower())
          {
            case "poison":
              player.character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Poison, 0.1f * curseIntensity, false);
              Plugin.Logger.LogInfo($"對玩家 {playerName} 施加中毒詛咒，強度: {0.1f * curseIntensity}");
              break;

            case "injury":
              player.character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Injury, 0.05f * curseIntensity, false);
              Plugin.Logger.LogInfo($"對玩家 {playerName} 施加受傷詛咒，強度: {0.05f * curseIntensity}");
              break;

            case "hunger":
              player.character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Hunger, 0.1f * curseIntensity, false);
              Plugin.Logger.LogInfo($"對玩家 {playerName} 施加飢餓詛咒，強度: {0.1f * curseIntensity}");
              break;

            case "drowsy":
              player.character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Drowsy, 0.1f * curseIntensity, false);
              Plugin.Logger.LogInfo($"對玩家 {playerName} 施加困倦詛咒，強度: {0.1f * curseIntensity}");
              break;

            case "curse":
              player.character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Curse, 0.1f * curseIntensity, false);
              Plugin.Logger.LogInfo($"對玩家 {playerName} 施加詛咒狀態，強度: {0.1f * curseIntensity}");
              break;

            case "cold":
              player.character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Cold, 0.1f * curseIntensity, false);
              Plugin.Logger.LogInfo($"對玩家 {playerName} 施加寒冷詛咒，強度: {0.1f * curseIntensity}");
              break;

            case "hot":
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

    private System.Collections.IEnumerator ContinuousCurseEffect(Player player, string playerName)
    {
      float duration = 10f;
      float interval = 1f;
      float elapsed = 0f;

      Plugin.Logger.LogInfo($"开始持续诅咒效果，持续时间: {duration}秒");

      while (elapsed < duration && player != null)
      {
        try
        {
          var allComponents = player.GetComponents<MonoBehaviour>();

          foreach (var component in allComponents)
          {
            if (component != null)
            {
              var takeDamageMethod = component.GetType().GetMethod("TakeDamage");
              if (takeDamageMethod != null)
              {
                float damage = 2f * curseIntensity;
                takeDamageMethod.Invoke(component, new object[] { damage });
                Plugin.Logger.LogInfo($"持续诅咒通过反射对玩家 {playerName} 造成 {damage} 点伤害");
                break;
              }
            }
          }
        }
        catch (System.Exception ex)
        {
          Plugin.Logger.LogError($"持续诅咒效果出错: {ex.Message}");
        }

        elapsed += interval;
        yield return new WaitForSeconds(interval);
      }

      Plugin.Logger.LogInfo($"玩家 {playerName} 的持续诅咒效果结束");
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

  public class UIManager : MonoBehaviour
  {
    public static UIManager instance = null!;
    private bool isHeld;
    private float distance;
    private bool hasValidData = false;
    private string currentCurseType = "None";
    private float curseTimer = 0f;
    private float curseInterval = 2f;

    void Awake()
    {
      if (instance == null)
      {
        instance = this;
        DontDestroyOnLoad(this.gameObject);
      }
      else
      {
        Destroy(this.gameObject);
      }
    }

    public void SetBingBongStatus(bool held, float dist)
    {
      isHeld = held;
      distance = dist;
      hasValidData = true;
    }

    public void SetCurseInfo(string curseType, float timer, float interval)
    {
      currentCurseType = curseType;
      curseTimer = timer;
      curseInterval = interval;
    }

    void OnGUI()
    {
      if (instance == null || !hasValidData) return;

      GUI.skin.label.fontSize = 14;
      GUI.skin.label.fontStyle = FontStyle.Bold;

      GUI.backgroundColor = new Color(0, 0, 0, 0.8f);
      GUI.Box(new Rect(15, 15, 350, 80), "");

      string statusText;
      Color statusColor;
      if (isHeld)
      {
        statusText = "✅ BingBong 正在被玩家攜帶";
        statusColor = Color.green;
      }
      else
      {
        statusText = "❌ BingBong 未被攜帶";
        statusColor = Color.red;
      }

      GUI.color = statusColor;
      GUI.Label(new Rect(20, 20, 340, 25), statusText);

      GUI.color = Color.white;
      GUI.Label(new Rect(20, 45, 340, 20), $"詛咒類型: {currentCurseType}");

      if (!isHeld)
      {
        float progress = curseTimer / curseInterval;
        GUI.color = Color.yellow;
        GUI.Label(new Rect(20, 65, 340, 20), $"下次詛咒: {curseTimer:F1}s / {curseInterval:F1}s");

        GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
        GUI.Box(new Rect(20, 85, 340 * progress, 8), "");
        GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        GUI.Box(new Rect(20 + 340 * progress, 85, 340 * (1 - progress), 8), "");
      }

      GUI.color = Color.white;
      GUI.backgroundColor = Color.white;
    }
  }
}

