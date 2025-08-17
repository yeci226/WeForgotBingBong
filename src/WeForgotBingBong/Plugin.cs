using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
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

            // 初始化 Config
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

                // 延迟初始化，确保ItemDatabase完全加载
                StartCoroutine(InitializeBingBongLogic());
            }
        }

        private System.Collections.IEnumerator InitializeBingBongLogic()
        {
            // 等待几帧确保ItemDatabase完全初始化
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
                // 尝试通过名称搜索
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
                // 創建本地玩家的 UI Manager
                if (showUI.Value)
                {
                    var uiManagerGO = new GameObject("BingBongUIManager");
                    uiManagerGO.AddComponent<UIManager>();
                }

                // 开始监听玩家物品变化
                StartCoroutine(MonitorPlayerInventoryChanges());
                
                // 监听场景中的物品状态变化
                StartCoroutine(MonitorItemStateChanges());
            }
        }

        private System.Collections.IEnumerator MonitorItemStateChanges()
        {
            while (true)
            {
                yield return new WaitForSeconds(3f); // 每3秒检查一次，减少频率
                
                // 查找场景中所有的BingBong物品
                var allItems = UnityEngine.Object.FindObjectsByType<Item>(FindObjectsSortMode.None);
                foreach (var item in allItems)
                {
                    if (item.itemID == bingBongItemID || item.name.Contains("BingBong"))
                    {
                        // 只在状态变化时记录日志
                        if (item.itemState == ItemState.Held)
                        {
                            var player = item.GetComponentInParent<Player>();
                            if (player != null)
                            {
                                // 移除日志记录
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
                yield return new WaitForSeconds(2f); // 每2秒检查一次，减少频率
                
                var players = UnityEngine.Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
                foreach (var player in players)
                {
                    if (player.photonView.IsMine)
                    {
                        bool currentlyHolding = CheckIfPlayerHasBingBong(player);
                        if (currentlyHolding)
                        {
                            // 移除日志记录
                        }
                    }
                }
            }
        }

        private bool CheckIfPlayerHasBingBong(Player player)
        {
            if (player == null || player.itemSlots == null) return false;
            
            // 检查所有物品槽
            for (int i = 0; i < player.itemSlots.Length; i++)
            {
                var slot = player.itemSlots[i];
                if (!slot.IsEmpty() && slot.prefab != null && slot.prefab.itemID == bingBongItemID)
                {
                    return true;
                }
            }
            
            // 检查临时槽位
            if (!player.tempFullSlot.IsEmpty() && player.tempFullSlot.prefab != null && 
                player.tempFullSlot.prefab.itemID == bingBongItemID)
            {
                return true;
            }
            
            // 检查背包槽位
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
        private const float PICKUP_DELAY = 0.5f; // 物品拾取后等待0.5秒再检测

        // 诅咒效果组件缓存
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
        private const float PLAYER_UPDATE_INTERVAL = 1f; // 每1秒更新一次玩家列表
        private bool lastCurseState = false; // 记录上次的诅咒状态

        void Update()
        {
            // 更新诅咒强度（如果配置发生变化）
            UpdateCurseIntensity();
            
            // 缓存玩家列表，避免每帧查找
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
                if (player == null) continue; // 跳过已销毁的玩家

                // 检查玩家是否携带BingBong
                bool isHeld = CheckIfPlayerHasBingBong(player);
                if (isHeld) 
                {
                    isHeldByAny = true;
                    string playerName = player.name ?? "Unknown Player";
                    carryingPlayers.Add(playerName);
                }

                // 找到本地玩家
                if (player.photonView.IsMine)
                {
                    localPlayer = player;
                }
            }

            // 更新UI显示 - 只对本地玩家显示
            if (showUI && localPlayer != null)
            {
                bool localPlayerHolding = CheckIfPlayerHasBingBong(localPlayer);
                float localPlayerDistance = Vector3.Distance(localPlayer.transform.position, transform.position);
                UIManager.instance?.SetBingBongStatus(localPlayerHolding, localPlayerDistance);
                UIManager.instance?.SetCurseInfo(curseType, timer, curseInterval);
            }

            // 诅咒逻辑：如果没有玩家携带BingBong，则施加诅咒
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
                    
                    // 对所有玩家施加诅咒
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
                // 有人携带BingBong，重置计时器并清除诅咒
                if (timer > 0)
                {
                    timer = 0f;
                    
                    // 清除所有玩家的诅咒效果
                    foreach (var player in cachedPlayers)
                    {
                        if (player != null) ClearCurse(player);
                    }
                }
            }

            // 记录诅咒状态变化
            if (lastCurseState != isHeldByAny)
            {
                lastCurseState = isHeldByAny;
            }
        }

        // 当检测到物品状态变化时调用此方法
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
            
            // 检查是否需要延迟检测
            if (playerPickupTime.ContainsKey(player))
            {
                float timeSincePickup = Time.time - playerPickupTime[player];
                if (timeSincePickup < PICKUP_DELAY)
                {
                    return false;
                }
                else
                {
                    // 延迟时间已过，移除记录
                    playerPickupTime.Remove(player);
                }
            }

            // 方法1：使用HasInAnySlot检查物品栏
            bool hasInSlot = false;
            try
            {
                hasInSlot = player.HasInAnySlot(bingBongItemID);
            }
            catch (System.Exception)
            {
                // 忽略异常，继续使用其他检测方法
            }
            
            // 方法2：手动检查所有物品槽
            bool hasInAnySlotManual = false;
            for (int i = 0; i < player.itemSlots.Length; i++)
            {
                var slot = player.itemSlots[i];
                if (!slot.IsEmpty() && slot.prefab != null)
                {
                    if (slot.prefab.itemID == bingBongItemID)
                    {
                        hasInAnySlotManual = true;
                        break; // 找到就跳出循环
                    }
                    // 备用检测：通过名称匹配
                    else if (slot.prefab.name.Contains("BingBong"))
                    {
                        hasInAnySlotManual = true;
                        break; // 找到就跳出循环
                    }
                }
            }
            
            // 检查临时槽位（如果还没找到的话）
            if (!hasInAnySlotManual && !player.tempFullSlot.IsEmpty() && player.tempFullSlot.prefab != null)
            {
                if (player.tempFullSlot.prefab.itemID == bingBongItemID)
                {
                    hasInAnySlotManual = true;
                }
                // 备用检测：通过名称匹配
                else if (player.tempFullSlot.prefab.name.Contains("BingBong"))
                {
                    hasInAnySlotManual = true;
                }
            }
            
            // 检查背包槽位（如果还没找到的话）
            if (!hasInAnySlotManual && player.backpackSlot.hasBackpack && !player.backpackSlot.IsEmpty() && player.backpackSlot.prefab != null)
            {
                if (player.backpackSlot.prefab.itemID == bingBongItemID)
                {
                    hasInAnySlotManual = true;
                }
                // 备用检测：通过名称匹配
                else if (player.backpackSlot.prefab.name.Contains("BingBong"))
                {
                    hasInAnySlotManual = true;
                }
            }

            // 方法4：检查玩家周围是否有BingBong物体（最后的备用检测）
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
            
            // 初始化玩家的诅咒列表（如果不存在）
            if (!activeCurses.ContainsKey(player))
            {
                activeCurses[player] = new List<MonoBehaviour>();
            }
            
            // 尝试使用备用方法（更可靠）
            ApplyCurseAlternative(player, playerName);
            
            // 同时尝试使用组件方法（如果备用方法失败）
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
                // 尝试获取现有的中毒组件
                var poison = player.gameObject.GetComponent<Action_InflictPoison>();
                if (poison == null) 
                {
                    // 如果不存在，创建一个新的
                    poison = player.gameObject.AddComponent<Action_InflictPoison>();
                    Plugin.Logger.LogInfo($"为玩家 {playerName} 创建新的中毒效果组件");
                }
                
                // 检查组件是否有效
                if (poison == null)
                {
                    Plugin.Logger.LogError($"无法创建中毒组件");
                    return;
                }
                
                // 设置中毒参数
                poison.inflictionTime = 5f * curseIntensity;
                poison.poisonPerSecond = 0.05f * curseIntensity;
                poison.enabled = true;
                
                // 立即执行一次中毒效果
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
                // 尝试获取现有的受伤组件
                var injury = player.gameObject.GetComponent<Action_ModifyStatus>();
                if (injury == null) 
                {
                    // 如果不存在，创建一个新的
                    injury = player.gameObject.AddComponent<Action_ModifyStatus>();
                    Plugin.Logger.LogInfo($"为玩家 {playerName} 创建新的受伤效果组件");
                }
                
                // 检查组件是否有效
                if (injury == null)
                {
                    Plugin.Logger.LogError($"无法创建受伤组件");
                    return;
                }
                
                // 设置受伤参数
                injury.statusType = CharacterAfflictions.STATUSTYPE.Injury;
                injury.changeAmount = 0.2f * curseIntensity;
                injury.enabled = true;
                
                // 立即执行一次受伤效果
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
            // 使用Action_ModifyStatus来模拟疲惫效果，使用Hunger状态
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
            // 使用Action_ModifyStatus来模拟偏执效果，使用Hunger状态
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
            // 随机选择一种诅咒效果
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
        
        // 使用游戏内置诅咒系统的备用方法
        private void ApplyCurseAlternative(Player player, string playerName)
        {
            Plugin.Logger.LogInfo($"使用备用方法对玩家 {playerName} 施加诅咒");
            
            // 方法1：尝试使用反射获取玩家的健康组件
            try
            {
                // 查找所有可能的健康相关组件
                var allComponents = player.GetComponents<MonoBehaviour>();
                Plugin.Logger.LogInfo($"玩家 {playerName} 的所有组件数量: {allComponents.Length}");
                
                foreach (var component in allComponents)
                {
                    if (component != null)
                    {
                        var componentType = component.GetType();
                        Plugin.Logger.LogInfo($"组件: {componentType.Name}");
                        
                        // 查找TakeDamage方法
                        var takeDamageMethod = componentType.GetMethod("TakeDamage");
                        if (takeDamageMethod != null)
                        {
                            Plugin.Logger.LogInfo($"找到TakeDamage方法在组件: {componentType.Name}");
                            float damage = 5f * curseIntensity;
                            takeDamageMethod.Invoke(component, new object[] { damage });
                            Plugin.Logger.LogInfo($"通过反射对玩家 {playerName} 造成 {damage} 点伤害");
                            break;
                        }
                        
                        // 查找其他可能的伤害方法
                        var damageMethods = componentType.GetMethods().Where(m => m.Name.ToLower().Contains("damage") || m.Name.ToLower().Contains("hurt"));
                        foreach (var method in damageMethods)
                        {
                            Plugin.Logger.LogInfo($"找到可能的伤害方法: {method.Name}");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogError($"反射调用时出错: {ex.Message}");
            }
            
            // 方法2：使用协程持续施加效果
            StartCoroutine(ContinuousCurseEffect(player, playerName));
        }
        
        private System.Collections.IEnumerator ContinuousCurseEffect(Player player, string playerName)
        {
            float duration = 10f; // 诅咒持续时间
            float interval = 1f; // 每秒施加一次效果
            float elapsed = 0f;
            
            Plugin.Logger.LogInfo($"开始持续诅咒效果，持续时间: {duration}秒");
            
            while (elapsed < duration && player != null)
            {
                try
                {
                    // 尝试通过反射持续造成伤害
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
            
            // 检查玩家是否在诅咒字典中
            if (!activeCurses.ContainsKey(player))
            {
                return; // 如果玩家不在字典中，直接返回
            }
            
            // 清除所有诅咒效果
            foreach (var curse in activeCurses[player])
            {
                if (curse != null)
                {
                    curse.enabled = false;
                }
            }
            activeCurses[player].Clear(); // 清空列表
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

            // 设置更好的字体样式
            GUI.skin.label.fontSize = 14;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            
            // 主状态框
            GUI.backgroundColor = new Color(0, 0, 0, 0.8f);
            GUI.Box(new Rect(15, 15, 350, 80), "");
            
            // BingBong状态
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
            
            // 诅咒信息
            GUI.color = Color.white;
            GUI.Label(new Rect(20, 45, 340, 20), $"詛咒類型: {currentCurseType}");
            
            if (!isHeld)
            {
                float progress = curseTimer / curseInterval;
                GUI.color = Color.yellow;
                GUI.Label(new Rect(20, 65, 340, 20), $"下次詛咒: {curseTimer:F1}s / {curseInterval:F1}s");
                
                // 进度条
                GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
                GUI.Box(new Rect(20, 85, 340 * progress, 8), "");
                GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                GUI.Box(new Rect(20 + 340 * progress, 85, 340 * (1 - progress), 8), "");
            }
            
            // 重置颜色
            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
        }
    }
}

