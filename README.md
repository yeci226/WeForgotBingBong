# WeForgotBingBong - 诅咒系统

这是一个为游戏添加诅咒效果的模组，当玩家没有携带BingBong物品时，会定期对玩家施加负面效果。

## 功能特性

### 🎯 诅咒类型
- **Poison (中毒)**: 持续造成伤害
- **Injury (受伤)**: 增加受伤状态
- **Exhaustion (疲惫)**: 使用饥饿状态模拟疲惫效果
- **Paranoia (偏执)**: 使用恐惧状态模拟偏执效果
- **Random (随机)**: 随机选择一种诅咒效果

### ⚙️ 配置选项
- `CurseInterval`: 施加诅咒的间隔时间（秒）
- `CurseType`: 诅咒效果类型
- `CurseIntensity`: 诅咒效果强度倍数 (0.5-2.0)
- `ShowBingBongUI`: 是否显示状态UI

### 🖥️ UI界面
- 实时显示BingBong携带状态
- 显示当前诅咒类型
- 诅咒倒计时进度条
- 彩色状态指示器

## 技术实现

### 不使用HarmonyLib的方法
本模组通过以下方式实现诅咒效果，无需使用HarmonyLib：

1. **直接添加游戏内置组件**:
   ```csharp
   // 中毒效果
   var poison = player.gameObject.AddComponent<Action_InflictPoison>();
   poison.inflictionTime = 5f * curseIntensity;
   poison.poisonPerSecond = 0.05f * curseIntensity;
   
   // 受伤效果
   var injury = player.gameObject.AddComponent<Action_ModifyStatus>();
   injury.statusType = CharacterAfflictions.STATUSTYPE.Injury;
   injury.changeAmount = 0.2f * curseIntensity;
   ```

2. **使用游戏内置的状态系统**:
   - `CharacterAfflictions.STATUSTYPE.Injury` - 受伤状态
   - `CharacterAfflictions.STATUSTYPE.Hunger` - 饥饿状态（模拟疲惫）
   - `CharacterAfflictions.STATUSTYPE.Fear` - 恐惧状态（模拟偏执）

3. **组件生命周期管理**:
   - 自动清理已销毁的玩家
   - 诅咒效果组件缓存
   - 状态变化监听

### 核心类说明

- **Plugin**: 主插件类，负责初始化和配置
- **BingBongCurseLogic**: 诅咒逻辑核心，管理效果施加和清除
- **UIManager**: UI管理器，显示状态信息和进度条

## 安装说明

1. 确保已安装BepInEx
2. 将模组文件放入`BepInEx/plugins`文件夹
3. 启动游戏，模组会自动加载

## 配置说明

在`BepInEx/config`文件夹中会生成配置文件，可以调整以下参数：

```toml
[General]
CurseInterval = 2.0
CurseType = Poison
CurseIntensity = 1.0

[UI]
ShowBingBongUI = true
```

## 兼容性

- 基于游戏内置的状态系统，无需额外依赖
- 支持多人游戏
- 自动处理玩家加入/离开
- 场景切换时保持状态

## 故障排除

如果遇到问题，请检查：
1. BepInEx是否正确安装
2. 游戏日志中是否有错误信息
3. 配置文件是否正确设置

## 开发说明

本模组展示了如何在不使用HarmonyLib的情况下：
- 添加游戏内置效果组件
- 管理玩家状态
- 实现实时UI更新
- 处理多人游戏同步

通过直接使用游戏提供的API和组件，可以避免HarmonyLib的复杂性，同时实现相同的功能效果。
