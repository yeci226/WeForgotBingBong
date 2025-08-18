# WeForgotBingBong - Curse System

This is a mod that adds curse effects to the game. When players are not carrying the BingBong item, negative effects will be periodically applied to them.

## Features

### 🎯 Curse Types
- **Poison**: Continuous damage over time
- **Injury**: Increases injury status
- **Hunger**: Increases hunger status
- **Drowsy**: Increases drowsy status
- **Curse**: Curse status effect
- **Cold**: Cold status effect
- **Hot**: Hot status effect

### ⚙️ Configuration Options

#### Basic Configuration
- `CurseInterval`: Interval in seconds between curse applications (0.5-60.0)
- `CurseIntensity`: Amount of curse effect applied per application (0.1-5.0)
- `PlayerJoinBufferTime`: Buffer time in seconds after player joins before curses can start (0-300)
- `ShowBingBongUI`: Whether to display status UI

#### Curse Type Configuration
- `SelectionMode`: Curse selection mode
  - `Single`: Single curse type
  - `Random`: Random selection
  - `Multiple`: Multiple curses
- `SingleCurseType`: Single curse type selection
- Individual curse type switches

#### Carrying Detection Configuration
- `CountBackpackAsCarrying`: Whether BingBong in backpack counts as carrying
- `CountNearbyAsCarrying`: Whether nearby BingBong counts as carrying
- `NearbyDetectionRadius`: Nearby detection radius
- `CountTempSlotAsCarrying`: Whether BingBong in temporary item slot counts as carrying

#### Final Curse Intensity Configuration
- Direct final intensity control for each curse type
- Range: 0.1-10.0

### 🖥️ UI Interface
- Real-time BingBong carrying status display
- Current curse type display
- Curse countdown progress bar
- Color-coded status indicators

## Technical Implementation

### Method Without Using HarmonyLib
This mod implements curse effects through the following methods without requiring HarmonyLib:

1. **Direct Addition of Built-in Game Components**:
   ```csharp
   // Using built-in status system
   player.character.refs.afflictions.AddStatus(
       CharacterAfflictions.STATUSTYPE.Poison, 
       0.1f * curseIntensity, 
       false
   );
   ```

2. **Using Built-in Status System**:
   - `CharacterAfflictions.STATUSTYPE.Poison` - Poison status
   - `CharacterAfflictions.STATUSTYPE.Injury` - Injury status
   - `CharacterAfflictions.STATUSTYPE.Hunger` - Hunger status
   - `CharacterAfflictions.STATUSTYPE.Drowsy` - Drowsy status
   - `CharacterAfflictions.STATUSTYPE.Curse` - Curse status
   - `CharacterAfflictions.STATUSTYPE.Cold` - Cold status
   - `CharacterAfflictions.STATUSTYPE.Hot` - Hot status

3. **Component Lifecycle Management**:
   - Automatic cleanup of destroyed players
   - Curse effect component caching
   - Status change monitoring

### Core Classes

- **Plugin**: Main plugin class, responsible for initialization and configuration management
- **BingBongCurseLogic**: Core curse logic, manages effect application and removal
- **UIManager**: UI manager, displays status information and progress bars

## Installation

1. Ensure BepInEx is installed
2. Place mod files in the `BepInEx/plugins` folder
3. Start the game, the mod will load automatically

## Configuration

A configuration file `config.cfg` will be generated in the `BepInEx/config` folder where you can adjust the following parameters:

### Fast Curse Mode
```ini
[General]
CurseInterval = 1.0
CurseIntensity = 2.0

[CurseType]
SelectionMode = Multiple
EnablePoison = true
EnableInjury = true
EnableHunger = false
EnableDrowsy = false
EnableCurse = true
EnableCold = false
EnableHot = false
```

### Gentle Curse Mode
```ini
[General]
CurseInterval = 5.0
CurseIntensity = 0.5

[CurseType]
SelectionMode = Random
EnablePoison = true
EnableHunger = true
EnableDrowsy = true
EnableInjury = false
EnableCurse = false
EnableCold = false
EnableHot = false
```

### Single Curse Mode
```ini
[General]
CurseInterval = 3.0
CurseIntensity = 1.5

[CurseType]
SelectionMode = Single
SingleCurseType = Poison
EnablePoison = true
EnableInjury = false
EnableHunger = false
EnableDrowsy = false
EnableCurse = false
EnableCold = false
EnableHot = false
```

For detailed configuration instructions, please refer to the `config_template.md` file.

## Compatibility

- Based on built-in game status system, no additional dependencies required
- Supports multiplayer games
- Automatically handles player join/leave
- Maintains state during scene transitions

## Troubleshooting

If you encounter issues, please check:
1. Whether BepInEx is properly installed
2. Whether there are error messages in the game logs
3. Whether the configuration file is correctly set
4. Whether debug mode is enabled for detailed logging

## Development Notes

This mod demonstrates how to implement the following without using HarmonyLib:
- Add built-in game effect components
- Manage player status
- Implement real-time UI updates
- Handle multiplayer game synchronization
- Implement flexible configuration system

By directly using the APIs and components provided by the game, we can avoid the complexity of HarmonyLib while achieving the same functional effects.

## Changelog

### v1.0.0
- Basic curse system
- Multiple curse type support
- Flexible configuration options
- Carrying detection system
- Real-time UI display
