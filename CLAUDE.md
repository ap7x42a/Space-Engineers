# Space Engineers Scripts Codebase - Comprehensive Instructions

## Project Overview
This repository contains Space Engineers in-game programmable block scripts for controlling various ship systems and subsystems. All scripts are written in C# using the Space Engineers Modding API.

## Technology Stack
- **Language**: C# (Space Engineers scripting variant)
- **Runtime**: Space Engineers Programmable Block environment
- **API**: Space Engineers Modding API (Sandbox.ModAPI.Ingame namespace)
- **Update Frequency**: Most scripts use `UpdateFrequency.Update10` (every 10 game ticks)

## Code Structure & Conventions

### File Organization
- Each `.cs` file is a standalone script for a programmable block
- Scripts are named descriptively by their function (e.g., `airlockPincode.cs`, `turretLiftController.cs`)
- No external dependencies between scripts - each is self-contained

### Common Patterns

#### 1. Script Initialization Pattern
```csharp
public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    // Initialize blocks and state
}

void Main(string argument, UpdateType updateSource) {
    // Handle commands and regular updates
}
```

#### 2. Block Reference Management
- Blocks are typically found by exact name or partial name matching
- References cached in class fields to avoid repeated lookups
- Common pattern: `GridTerminalSystem.GetBlockWithName()` or `GetBlocksOfType()`

#### 3. Display/LCD Management
Scripts commonly use structured classes for managing display state:
- `LCDInfo` - Display configuration (font size, colors, positions)
- `MyLCD` - Individual display management
- Boot sequences with animated text reveal

#### 4. State Management
- Scripts maintain state between runs using class fields
- Common states: `isBooted`, `isBooting`, `systemActive`, `emergencyMode`
- Timers and counters for animations and sequences

#### 5. Error Handling
- Graceful handling of missing blocks
- `Echo()` for debug output
- Fallback to safe states when blocks unavailable

## Script Categories

### 1. Turret & Weapon Systems
- **adaptiveTurretSystem.cs**: Multi-mode turret controller with Hovercat/AssaultCat/Transfer modes
- **mannedTurretOS.cs**: Boot sequence and UI for manned turret stations
- **turretLiftController.cs**: Controls turret lift mechanism with safety checks

### 2. Lighting Control
- **reactorRoomLights.cs**: Multi-zone lighting with animations
- **weaponRoomLightManagement.cs**: Weapon room lighting automation
- **cryoRoomLights.cs**: Cryo chamber lighting control
- **entranceLightsController.cs**: Entrance area lighting management

### 3. Airlock Systems
- **airlockPincode.cs**: Secure pincode access for airlocks
- **airlockInfoDisplays.cs**: Airlock status displays
- **innerAirlockMessage.cs**: Interior airlock messaging

### 4. System Monitoring
- **shipSummary.cs**: Scans and logs all ship blocks
- **conveyorDiagnostic.cs**: Conveyor system diagnostics
- **blockSettingsCollector.cs**: Collects block configuration data
- **emergencyPowerLcds.cs**: Power system monitoring displays

### 5. Utility Scripts
- **gravityAlignGyro.cs**: Auto-aligns subgrids to gravity
- **binarycounterconverter.cs**: Binary counter visualization
- **cafePCprogram.cs**: Simulated computer terminal
- **rogueai.cs**: AI personality simulator

## Common API Elements Used

### Block Types
- `IMyTextSurface` / `IMyTextPanel` - Display surfaces
- `IMyCockpit` - Control seats with multiple displays
- `IMyBatteryBlock` - Battery management
- `IMyMotorStator` - Rotors and hinges
- `IMyGyro` - Gyroscopes for stabilization
- `IMyUserControllableGun` - Weapons
- `IMyLightingBlock` - Lights
- `IMyDoor` - Doors and airlocks
- `IMyShipController` - Ship controllers for gravity/orientation

### Common Methods
- `GridTerminalSystem.GetBlockWithName()` - Find single block
- `GridTerminalSystem.GetBlocksOfType()` - Find multiple blocks
- `Echo()` - Debug output
- `Runtime.UpdateFrequency` - Set script update rate
- `Me.CustomData` - Store persistent data

## Coding Style Guidelines

1. **Variable Naming**
   - camelCase for local variables and parameters
   - PascalCase for public fields and class names
   - Descriptive names (e.g., `turretBattery`, not `tb`)

2. **Configuration**
   - User-configurable values at top of file
   - Clear section headers with comments
   - Default values provided

3. **Comments**
   - Header block explaining script purpose
   - Section dividers for organization
   - Inline comments for complex logic only

4. **Display Formatting**
   - Consistent use of color schemes
   - Font size and spacing variables
   - Vector2 for positioning

## Testing & Debugging

1. **Echo Messages**: Use `Echo()` liberally for status updates
2. **Custom Data**: Store debug info in `Me.CustomData`
3. **Gradual Testing**: Test with individual blocks before full systems
4. **Safe Defaults**: Always provide fallback behavior

## Performance Considerations

1. **Update Frequency**: Use `Update10` or `Update100` unless real-time needed
2. **Block Caching**: Cache block references in constructor
3. **Conditional Updates**: Only update displays when state changes
4. **List Reuse**: Clear and reuse lists rather than creating new ones

## Safety Patterns

1. **Null Checks**: Always verify block references before use
2. **Grid Filtering**: Use `CubeGrid == Me.CubeGrid` to isolate to current grid
3. **State Validation**: Verify system state before operations
4. **Graceful Degradation**: Continue operation with reduced functionality

## Common Tasks

### Adding New Script
1. Create new `.cs` file with descriptive name
2. Add header comment block
3. Define configuration variables at top
4. Implement `Program()` constructor and `Main()` method
5. Test in single-player world first

### Modifying Existing Script
1. Preserve configuration section structure
2. Maintain existing naming conventions
3. Test changes incrementally
4. Update header comments if functionality changes

### Display/UI Work
1. Use existing display classes as templates (LCDInfo, MyLCD)
2. Maintain consistent color schemes
3. Test on different display sizes
4. Consider font scaling for readability

## Known Limitations

1. **Script Complexity**: Limited instruction count per tick
2. **No External Files**: Cannot import external libraries
3. **Sandboxed Environment**: Limited API access
4. **Performance**: Heavy operations should be spread across ticks

## Script Dependencies

All scripts are standalone - no inter-script dependencies. However, they may depend on specific block configurations:
- Named blocks must exist with exact names specified
- Block groups must be created for group operations
- Subgrids require proper rotor/piston connections

## Maintenance Notes

- Scripts assume vanilla SE + specific block names
- May need adjustment for modded blocks
- Regular testing needed after SE updates
- Performance profiling recommended for complex scripts

---

## Quick Reference

### To modify a script:
1. Check the configuration section at the top
2. Understand the Main() flow
3. Review block dependencies
4. Test changes incrementally

### To create similar functionality:
1. Find existing script with similar pattern
2. Copy structure and adapt
3. Maintain naming conventions
4. Test thoroughly

### Common issues:
- Missing blocks: Check exact names in configuration
- Performance: Reduce update frequency or spread operations
- Display issues: Verify surface indices and font sizes
- State persistence: Use class fields, not local variables