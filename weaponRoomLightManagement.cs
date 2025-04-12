// Event-Based Blink Interval Management

// ---- Adjustable Settings ----
float bottomLightIntensityEnabled = 10.0f;
float bottomLightIntensityDisabled = 0.0f;
float bottomLightAdjustmentStep = 0.2f;
float blueLightIntensityStep = 0.05f; // Speed of intensity change
float blueLightRadiusStep = 0.05f;    // Speed of radius change

// ---- State Tracking ----
float currentBottomLightIntensityLeft = 0f;
float currentBottomLightIntensityRight = 0f;

float currentBottomLightIntensityLeft2 = 0f;
float currentBottomLightIntensityRight2 = 0f;

float currentBottomLightIntensityFloor5 = 0f;

// ---- Configurable Block Names ----
IMyLightingBlock bottomLightLeft, bottomLightRight, bottomLightLeft2, bottomLightRight2, bottomLightFloor5;
IMyBatteryBlock battery1, battery2, battery3, battery4;
IMyLightingBlock lightRed1, lightRed2, lightRed3, lightRed4;
IMyLightingBlock insetLightRed1, insetLightRed2;
IMyLightingBlock lightYellow2, lightYellow4;
IMyLightingBlock insetLightGreen2;
IMyLightingBlock lightYellow1, lightYellow3;
IMyLightingBlock insetLightGreen1;
IMyBatteryBlock weaponRoomBattery1, weaponRoomBattery2, weaponRoomBattery3, weaponRoomBattery4;
IMyTurretControlBlock turretControllerRight;
IMyTurretControlBlock turretControllerLeft;
IMyTextPanel lcdRight;
IMyTextPanel lcdLeft;
IMyHeatVent heatVent1, heatVent2, heatVent3, heatVent4;
IMyHeatVent floorVentLeft1, floorVentLeft2, floorVentRight1, floorVentRight2;
IMyLightingBlock lightBlue1, lightBlue2, lightBlue3, lightBlue4;

float currentCeilingVentIntensity1 = 0f;
float currentCeilingVentIntensity2 = 0f;
float currentCeilingVentIntensity3 = 0f;
float currentCeilingVentIntensity4 = 0f;


// ---- Script Initialization ----
public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10; // Periodic updates
    LoadBlocks();
    Echo("Event-Based Blink Interval Management Initialized");
}

// ---- Load Blocks ----
void LoadBlocks()
{
    battery1 = GridTerminalSystem.GetBlockWithName("Weapon Startup Battery 1") as IMyBatteryBlock;
    battery2 = GridTerminalSystem.GetBlockWithName("Weapon Startup Battery 2") as IMyBatteryBlock;
    battery3 = GridTerminalSystem.GetBlockWithName("Weapon Startup Battery 3") as IMyBatteryBlock;
    battery4 = GridTerminalSystem.GetBlockWithName("Weapon Startup Battery 4") as IMyBatteryBlock;

    lightRed1 = GridTerminalSystem.GetBlockWithName("Warfare Battery 1 Overhead Light Red") as IMyLightingBlock;
    lightRed2 = GridTerminalSystem.GetBlockWithName("Warfare Battery 2 Overhead Light Red") as IMyLightingBlock;
    lightRed3 = GridTerminalSystem.GetBlockWithName("Warfare Battery 3 Overhead Light Red") as IMyLightingBlock;
    lightRed4 = GridTerminalSystem.GetBlockWithName("Warfare Battery 4 Overhead Light Red") as IMyLightingBlock;

    insetLightRed1 = GridTerminalSystem.GetBlockWithName("Weapon Room Floor Start Inset Light Red 1") as IMyLightingBlock;
    insetLightRed2 = GridTerminalSystem.GetBlockWithName("Weapon Room Floor Start Inset Light Red 2") as IMyLightingBlock;

    lightYellow2 = GridTerminalSystem.GetBlockWithName("Warfare Battery 2 Overhead Light Yellow") as IMyLightingBlock;
    lightYellow4 = GridTerminalSystem.GetBlockWithName("Warfare Battery 4 Overhead Light Yellow") as IMyLightingBlock;
    insetLightGreen2 = GridTerminalSystem.GetBlockWithName("Weapon Room Floor Start Inset Light Green 2") as IMyLightingBlock;

    lightYellow1 = GridTerminalSystem.GetBlockWithName("Warfare Battery 1 Overhead Light Yellow") as IMyLightingBlock;
    lightYellow3 = GridTerminalSystem.GetBlockWithName("Warfare Battery 3 Overhead Light Yellow") as IMyLightingBlock;
    insetLightGreen1 = GridTerminalSystem.GetBlockWithName("Weapon Room Floor Start Inset Light Green 1") as IMyLightingBlock;

    weaponRoomBattery1 = GridTerminalSystem.GetBlockWithName("Weapon Room Battery 1") as IMyBatteryBlock;
    weaponRoomBattery2 = GridTerminalSystem.GetBlockWithName("Weapon Room Battery 2") as IMyBatteryBlock;
    weaponRoomBattery3 = GridTerminalSystem.GetBlockWithName("Weapon Room Battery 3") as IMyBatteryBlock;
    weaponRoomBattery4 = GridTerminalSystem.GetBlockWithName("Weapon Room Battery 4") as IMyBatteryBlock;

    turretControllerRight = GridTerminalSystem.GetBlockWithName("Weapon Room Right Power On Button Panel") as IMyTurretControlBlock;
    turretControllerLeft = GridTerminalSystem.GetBlockWithName("Weapon Room Left Power On Button Panel") as IMyTurretControlBlock;
    lcdRight = GridTerminalSystem.GetBlockWithName("Weapon Room Initialization LCD Right") as IMyTextPanel;
    lcdLeft = GridTerminalSystem.GetBlockWithName("Weapon Room Initialization LCD Left") as IMyTextPanel;

    heatVent1 = GridTerminalSystem.GetBlockWithName("Weapon Room Ceiling Heat Vent 1") as IMyHeatVent;
    heatVent2 = GridTerminalSystem.GetBlockWithName("Weapon Room Ceiling Heat Vent 2") as IMyHeatVent;
    heatVent3 = GridTerminalSystem.GetBlockWithName("Weapon Room Ceiling Heat Vent 3") as IMyHeatVent;
    heatVent4 = GridTerminalSystem.GetBlockWithName("Weapon Room Ceiling Heat Vent 4") as IMyHeatVent;

    floorVentLeft1 = GridTerminalSystem.GetBlockWithName("Weapon Room Left Floor Heat Vent 1") as IMyHeatVent;
    floorVentLeft2 = GridTerminalSystem.GetBlockWithName("Weapon Room Left Floor Heat Vent 2") as IMyHeatVent;
    floorVentRight1 = GridTerminalSystem.GetBlockWithName("Weapon Room Right Floor Heat Vent 1") as IMyHeatVent;
    floorVentRight2 = GridTerminalSystem.GetBlockWithName("Weapon Room Right Floor Heat Vent 2") as IMyHeatVent;

    lightBlue1 = GridTerminalSystem.GetBlockWithName("Warfare Battery 1 Overhead Light Blue") as IMyLightingBlock;
    lightBlue2 = GridTerminalSystem.GetBlockWithName("Warfare Battery 2 Overhead Light Blue") as IMyLightingBlock;
    lightBlue3 = GridTerminalSystem.GetBlockWithName("Warfare Battery 3 Overhead Light Blue") as IMyLightingBlock;
    lightBlue4 = GridTerminalSystem.GetBlockWithName("Warfare Battery 4 Overhead Light Blue") as IMyLightingBlock;

    bottomLightLeft = GridTerminalSystem.GetBlockWithName("Weapon Room Control Bottom Light Left") as IMyLightingBlock;
    bottomLightRight = GridTerminalSystem.GetBlockWithName("Weapon Room Control Bottom Light Right") as IMyLightingBlock;
    bottomLightLeft2 = GridTerminalSystem.GetBlockWithName("Weapon Room Control Bottom Light Left 2") as IMyLightingBlock;
    bottomLightRight2 = GridTerminalSystem.GetBlockWithName("Weapon Room Control Bottom Light Right 2") as IMyLightingBlock;
    bottomLightFloor5 = GridTerminalSystem.GetBlockWithName("Weapon Room Variable Floor Light 5") as IMyLightingBlock;

    // Validate existence of all critical blocks
    ValidateBlock(battery1, "Weapon Startup Battery 1");
    ValidateBlock(battery2, "Weapon Startup Battery 2");
    ValidateBlock(battery3, "Weapon Startup Battery 3");
    ValidateBlock(battery4, "Weapon Startup Battery 4");
    ValidateBlock(lightRed1, "Warfare Battery 1 Overhead Light Red");
    ValidateBlock(lightRed2, "Warfare Battery 2 Overhead Light Red");
    ValidateBlock(lightRed3, "Warfare Battery 3 Overhead Light Red");
    ValidateBlock(lightRed4, "Warfare Battery 4 Overhead Light Red");
    ValidateBlock(insetLightRed1, "Weapon Room Floor Start Inset Light Red 1");
    ValidateBlock(insetLightRed2, "Weapon Room Floor Start Inset Light Red 2");
    ValidateBlock(lightYellow2, "Warfare Battery 2 Overhead Light Yellow");
    ValidateBlock(lightYellow4, "Warfare Battery 4 Overhead Light Yellow");
    ValidateBlock(insetLightGreen2, "Weapon Room Floor Start Inset Light Green 2");
    ValidateBlock(lightYellow1, "Warfare Battery 1 Overhead Light Yellow");
    ValidateBlock(lightYellow3, "Warfare Battery 3 Overhead Light Yellow");
    ValidateBlock(insetLightGreen1, "Weapon Room Floor Start Inset Light Green 1");
    ValidateBlock(weaponRoomBattery1, "Weapon Room Battery 1");
    ValidateBlock(weaponRoomBattery2, "Weapon Room Battery 2");
    ValidateBlock(weaponRoomBattery3, "Weapon Room Battery 3");
    ValidateBlock(weaponRoomBattery4, "Weapon Room Battery 4");
    ValidateBlock(turretControllerRight, "Weapon Room Right Power On Button Panel");
    ValidateBlock(turretControllerLeft, "Weapon Room Left Power On Button Panel");
    ValidateBlock(lcdRight, "Weapon Room Initialization LCD Right");
    ValidateBlock(lcdLeft, "Weapon Room Initialization LCD Left");
    ValidateBlock(heatVent1, "Weapon Room Ceiling Heat Vent 1");
    ValidateBlock(heatVent2, "Weapon Room Ceiling Heat Vent 2");
    ValidateBlock(heatVent3, "Weapon Room Ceiling Heat Vent 3");
    ValidateBlock(heatVent4, "Weapon Room Ceiling Heat Vent 4");
    ValidateBlock(floorVentLeft1, "Weapon Room Left Floor Heat Vent 1");
    ValidateBlock(floorVentLeft2, "Weapon Room Left Floor Heat Vent 2");
    ValidateBlock(floorVentRight1, "Weapon Room Right Floor Heat Vent 1");
    ValidateBlock(floorVentRight2, "Weapon Room Right Floor Heat Vent 2");
    ValidateBlock(lightBlue1, "Warfare Battery 1 Overhead Light Blue");
    ValidateBlock(lightBlue2, "Warfare Battery 2 Overhead Light Blue");
    ValidateBlock(lightBlue3, "Warfare Battery 3 Overhead Light Blue");
    ValidateBlock(lightBlue4, "Warfare Battery 4 Overhead Light Blue");
    ValidateBlock(bottomLightLeft, "Weapon Room Control Bottom Light Left");
    ValidateBlock(bottomLightRight, "Weapon Room Control Bottom Light Right");
    ValidateBlock(bottomLightLeft2, "Weapon Room Control Bottom Light Left 2");
    ValidateBlock(bottomLightRight2, "Weapon Room Control Bottom Light Right 2");
    ValidateBlock(bottomLightFloor5, "Weapon Room Variable Floor Light 5");
}

// ---- Main Method ----
public void Main(string argument, UpdateType updateSource)
{
    Echo("=== Debugging Battery States ===");
    Echo($"Runtime: {Runtime.LastRunTimeMs} ms");
    Echo($"Turret Right IsUnderControl: {turretControllerRight?.IsUnderControl}");
    Echo($"Turret Left IsUnderControl: {turretControllerLeft?.IsUnderControl}");

    // Monitor heat vent states
    Echo("=== Debugging Heat Vents ===");
    bool allVentsEnabled = AllHeatVentsEnabled();
    Echo($"All Heat Vents Enabled: {allVentsEnabled}");

    // Adjust lights based on vent states
    AdjustLightsBasedOnVents(allVentsEnabled);
    AdjustBottomLightsBasedOnVents(allVentsEnabled);

    // Halt if any batteries are missing
    if (battery1 == null) throw new Exception("Error: Weapon Startup Battery 1 not found!");
    if (battery2 == null) throw new Exception("Error: Weapon Startup Battery 2 not found!");
    if (battery3 == null) throw new Exception("Error: Weapon Startup Battery 3 not found!");
    if (battery4 == null) throw new Exception("Error: Weapon Startup Battery 4 not found!");

    // Halt if any screens are missing
    if (turretControllerRight == null) Echo("Error: Button Panel Right not found!");
    if (turretControllerLeft == null) Echo("Error: Button Panel Left not found!");
    if (lcdRight == null) Echo("Error: LCD Right not found!");
    if (lcdLeft == null) Echo("Error: LCD Left not found!");

    // Output debug messages for each vent
    if (heatVent1 == null) Echo("Heat Vent 1 not found!");
    if (heatVent2 == null) Echo("Heat Vent 2 not found!");
    if (heatVent3 == null) Echo("Heat Vent 3 not found!");
    if (heatVent4 == null) Echo("Heat Vent 4 not found!");

    // Debug individual battery-light pairings
    DebugBatteryState(battery1, battery1.CustomName);
    DebugBatteryState(battery2, battery2.CustomName);
    DebugBatteryState(battery3, battery3.CustomName);
    DebugBatteryState(battery4, battery4.CustomName);

    // Manage blink intervals for individual lights
    ManageBlinkInterval(battery1, lightRed1);
    ManageBlinkInterval(battery2, lightRed2);
    ManageBlinkInterval(battery3, lightRed3);
    ManageBlinkInterval(battery4, lightRed4);

    // Manage combined battery states for inset lights
    ManageCombinedBlinkInterval(battery1, battery3, insetLightRed1);
    ManageCombinedBlinkInterval(battery2, battery4, insetLightRed2);

    // Manage blink interval for green inset light based on yellow overhead lights
    ManageYellowLightEvent(lightYellow2, lightYellow4, insetLightGreen2);
    ManageYellowLightEvent(lightYellow1, lightYellow3, insetLightGreen1);

    // Manage blink interval for yellow lights based on weapon room battery status
    ManageWeaponRoomBatteryEvent(weaponRoomBattery1, lightYellow1);
    ManageWeaponRoomBatteryEvent(weaponRoomBattery2, lightYellow2);
    ManageWeaponRoomBatteryEvent(weaponRoomBattery3, lightYellow3);
    ManageWeaponRoomBatteryEvent(weaponRoomBattery4, lightYellow4);

    // Manage LCD Content types for initialization LCDs
    if (turretControllerRight != null && lcdRight != null)
    {
        ManageLCDContent(turretControllerRight, lcdRight);
    }
    if (turretControllerLeft != null && lcdLeft != null)
    {
        ManageLCDContent(turretControllerLeft, lcdLeft);
    }
}

// ---- Debug Individual Battery State ----
void DebugBatteryState(IMyBatteryBlock battery, string label)
{
    if (battery == null)
    {
        Echo($"{label}: Block missing!");
        return;
    }

    // Debugging the toggle state
    Echo($"{label}: IsFunctional = {battery.IsFunctional}");
    Echo($"{label}: IsEnabled = {battery.Enabled}");

    // Determine if battery is "on"
    if (IsBatteryOn(battery))
    {
        Echo($"{label}: Battery is ON.");
    }
    else
    {
        Echo($"{label}: Battery is OFF.");
    }
}

// ---- Manage Blink Interval for Individual Batteries ----
void ManageBlinkInterval(IMyBatteryBlock battery, IMyLightingBlock light)
{
    if (battery == null || light == null) return;

    float targetInterval = IsBatteryOn(battery) ? 0 : 1;

    Echo($"[{light.CustomName}] Current Interval: {light.BlinkIntervalSeconds}, Target: {targetInterval}");
    if (Math.Abs(light.BlinkIntervalSeconds - targetInterval) > 0.01f) // Avoid unnecessary updates
    {
        Echo($"Updating {light.CustomName} Interval to {targetInterval}");
        light.BlinkIntervalSeconds = targetInterval;
    }
}

// ---- Manage Combined Blink Interval for Paired Batteries ----
void ManageCombinedBlinkInterval(IMyBatteryBlock batteryA, IMyBatteryBlock batteryB, IMyLightingBlock light)
{
    if (batteryA == null || batteryB == null || light == null) return;

    float targetInterval = (IsBatteryOn(batteryA) && IsBatteryOn(batteryB)) ? 0 : 1;

    Echo($"[{light.CustomName}] Current Interval: {light.BlinkIntervalSeconds}, Target: {targetInterval}");
    if (Math.Abs(light.BlinkIntervalSeconds - targetInterval) > 0.01f) // Avoid unnecessary updates
    {
        Echo($"Updating {light.CustomName} Interval to {targetInterval}");
        light.BlinkIntervalSeconds = targetInterval;
    }
}

// ---- Manage Yellow Light Event ----
void ManageYellowLightEvent(IMyLightingBlock lightA, IMyLightingBlock lightB, IMyLightingBlock insetLight)
{
    if (lightA == null || lightB == null || insetLight == null) return;

    bool bothOn = lightA.Enabled && lightB.Enabled;
    float targetInterval = bothOn ? 0 : 1;

    Echo($"[{insetLight.CustomName}] Current Interval: {insetLight.BlinkIntervalSeconds}, Target: {targetInterval}");
    if (Math.Abs(insetLight.BlinkIntervalSeconds - targetInterval) > 0.01f) // Avoid unnecessary updates
    {
        Echo($"Updating {insetLight.CustomName} Interval to {targetInterval}");
        insetLight.BlinkIntervalSeconds = targetInterval;
    }
}

// ---- Manage Weapon Room Battery Event ----
void ManageWeaponRoomBatteryEvent(IMyBatteryBlock battery, IMyLightingBlock light)
{
    if (battery == null || light == null) return;

    float targetInterval = IsBatteryOn(battery) ? 1 : 0;

    Echo($"[{light.CustomName}] Current Interval: {light.BlinkIntervalSeconds}, Target: {targetInterval}");
    if (Math.Abs(light.BlinkIntervalSeconds - targetInterval) > 0.01f) // Avoid unnecessary updates
    {
        Echo($"Updating {light.CustomName} Interval to {targetInterval}");
        light.BlinkIntervalSeconds = targetInterval;
    }
}

// ---- Helper: Check if a Battery is On ----
bool IsBatteryOn(IMyBatteryBlock battery)
{
    // A battery is "ON" if it is toggled "enabled"
    return battery.Enabled;
}

// ---- Validate Block ----
void ValidateBlock(IMyTerminalBlock block, string name)
{
    if (block == null)
    {
        Echo($"Warning: Block '{name}' not found or invalid.");
        return; // Do not throw an exception, allow the script to continue
    }

    Echo($"Loaded: {block.CustomName}");
}

// ---- LCD Content Management Logic ----
void ManageLCDContent(IMyTurretControlBlock turretController, IMyTextPanel lcd)
{
    if (turretController == null || lcd == null)
    {
        Echo("ManageLCDContent: Missing turret controller or LCD.");
        return;
    }

    Echo($"Managing LCD {lcd.CustomName} based on {turretController.CustomName}");
    Echo($"Turret Controller IsUnderControl: {turretController.IsUnderControl}");

    // Check if the turret controller is under control
    if (turretController.Enabled)
    {
        Echo($"Setting {lcd.CustomName} to SCRIPT");
        lcd.ContentType = ContentType.SCRIPT; // Change to 'Apps'
    }
    else
    {
        Echo($"Setting {lcd.CustomName} to TEXT_AND_IMAGE");
        lcd.ContentType = ContentType.TEXT_AND_IMAGE; // Change to 'Text and Images'
    }
}

// ---- Manage Blue Light adjustments
void AdjustLightsBasedOnVents(bool ventsEnabled)
{
    // Blue lights target settings
    float targetBlueIntensity = ventsEnabled ? 1.0f : 5.0f;
    float targetBlueRadius = ventsEnabled ? 2.5f : 3.6f;

    // Floor heat vent intensity target
    float targetFloorVentIntensity = ventsEnabled ? 1.0f : 10.0f;

    // Ceiling heat vent intensity target
    float targetCeilingVentIntensity = ventsEnabled ? 10.0f : 0.0f;

    // Adjust blue lights
    AdjustLightProperties(lightBlue1, targetBlueIntensity, targetBlueRadius);
    AdjustLightProperties(lightBlue2, targetBlueIntensity, targetBlueRadius);
    AdjustLightProperties(lightBlue3, targetBlueIntensity, targetBlueRadius);
    AdjustLightProperties(lightBlue4, targetBlueIntensity, targetBlueRadius);

    // Adjust floor heat vent intensities
    AdjustHeatVentIntensity(floorVentLeft1, targetFloorVentIntensity);
    AdjustHeatVentIntensity(floorVentLeft2, targetFloorVentIntensity);
    AdjustHeatVentIntensity(floorVentRight1, targetFloorVentIntensity);
    AdjustHeatVentIntensity(floorVentRight2, targetFloorVentIntensity);

    // Gradually adjust ceiling heat vent intensities
    float ventAdjustmentStep = 0.1f; // Example step value
    GraduallyAdjustCeilingVentIntensity(heatVent1, ref currentCeilingVentIntensity1, targetCeilingVentIntensity, ventAdjustmentStep);
    GraduallyAdjustCeilingVentIntensity(heatVent2, ref currentCeilingVentIntensity2, targetCeilingVentIntensity, ventAdjustmentStep);
    GraduallyAdjustCeilingVentIntensity(heatVent3, ref currentCeilingVentIntensity3, targetCeilingVentIntensity, ventAdjustmentStep);
    GraduallyAdjustCeilingVentIntensity(heatVent4, ref currentCeilingVentIntensity4, targetCeilingVentIntensity, ventAdjustmentStep);
}

void AdjustLightProperties(IMyLightingBlock light, float targetIntensity, float targetRadius)
{
    if (light == null) return;

    // Gradually adjust intensity
    if (Math.Abs(light.Intensity - targetIntensity) > 0.01f)
    {
        light.Intensity += Math.Sign(targetIntensity - light.Intensity) * blueLightIntensityStep; // Adjust by step
    }

    // Gradually adjust radius
    if (Math.Abs(light.Radius - targetRadius) > 0.01f)
    {
        light.Radius += Math.Sign(targetRadius - light.Radius) * blueLightRadiusStep; // Adjust by step
    }

    Echo($"[{light.CustomName}] Intensity: {light.Intensity:F2}, Radius: {light.Radius:F2}");
}


void AdjustHeatVentIntensity(IMyHeatVent vent, float targetIntensity)
{
    if (vent == null) return;

    // Use terminal actions to adjust intensity
    string actionName = targetIntensity > 1.0f ? "IncreaseIntensity" : "DecreaseIntensity";
    var action = vent.GetActionWithName(actionName);
    if (action != null)
    {
        // Apply the action repeatedly to simulate gradual adjustment
        for (int i = 0; i < Math.Abs(targetIntensity); i++)
        {
            action.Apply(vent);
        }
        Echo($"[{vent.CustomName}] Applied Action: {actionName} to reach target intensity.");
    }
    else
    {
        Echo($"[{vent.CustomName}] Action '{actionName}' not found.");
    }
}

void GraduallyAdjustCeilingVentIntensity(IMyHeatVent vent, ref float currentIntensity, float targetIntensity, float step = 0.1f)
{
    if (vent == null) return;

    // Gradually approach the target intensity
    float intensityDifference = targetIntensity - currentIntensity;

    if (Math.Abs(intensityDifference) > 0.01f)
    {
        // Apply the step towards the target intensity (slower or faster depending on step value)
        currentIntensity += Math.Sign(intensityDifference) * step;

        // Ensure the intensity stays within bounds (0 to 10)
        currentIntensity = MathHelper.Clamp(currentIntensity, 0f, 10f);

        // Apply the action to the vent to adjust its intensity
        string actionName = intensityDifference > 0 ? "IncreaseIntensity" : "DecreaseIntensity";
        var action = vent.GetActionWithName(actionName);
        if (action != null)
        {
            action.Apply(vent);
            Echo($"[{vent.CustomName}] Applied {actionName}. Current Intensity: {currentIntensity:F2}");
        }
        else
        {
            Echo($"[{vent.CustomName}] Action '{actionName}' not found.");
        }
    }
    else
    {
        // Ensure the vent reaches the target value (0 or 10) when the difference is small enough
        currentIntensity = targetIntensity;
        Echo($"[{vent.CustomName}] Reached target intensity: {currentIntensity:F2}");
    }
}

void AdjustBottomLightsBasedOnVents(bool ventsEnabled)
{
    float targetBottomLightIntensity = ventsEnabled ? bottomLightIntensityEnabled : bottomLightIntensityDisabled;

    GraduallyAdjustLightIntensity(bottomLightLeft, ref currentBottomLightIntensityLeft, targetBottomLightIntensity, bottomLightAdjustmentStep);
    GraduallyAdjustLightIntensity(bottomLightRight, ref currentBottomLightIntensityRight, targetBottomLightIntensity, bottomLightAdjustmentStep);
    GraduallyAdjustLightIntensity(bottomLightLeft2, ref currentBottomLightIntensityLeft2, targetBottomLightIntensity, bottomLightAdjustmentStep);
    GraduallyAdjustLightIntensity(bottomLightRight2, ref currentBottomLightIntensityRight2, targetBottomLightIntensity, bottomLightAdjustmentStep);
    GraduallyAdjustLightIntensity(bottomLightFloor5, ref currentBottomLightIntensityFloor5, targetBottomLightIntensity, bottomLightAdjustmentStep);
}

void GraduallyAdjustLightIntensity(IMyLightingBlock light, ref float currentIntensity, float targetIntensity, float step)
{
    if (light == null) return;

    // Gradually approach the target intensity
    if (Math.Abs(currentIntensity - targetIntensity) > 0.01f)
    {
        currentIntensity += (targetIntensity > currentIntensity ? step : -step);
        currentIntensity = MathHelper.Clamp(currentIntensity, 0f, 10f); // Ensure intensity stays within bounds
        light.Intensity = currentIntensity;
        Echo($"[{light.CustomName}] Intensity: {currentIntensity:F2}");
    }
    else
    {
        Echo($"[{light.CustomName}] Intensity is already at target: {currentIntensity:F2}");
    }
}


bool AllHeatVentsEnabled()
{
    if (heatVent1 == null || heatVent2 == null || heatVent3 == null || heatVent4 == null)
    {
        Echo("One or more heat vents are missing. Cannot evaluate vent state.");
        if (heatVent1 == null) Echo("Heat Vent 1 is missing.");
        if (heatVent2 == null) Echo("Heat Vent 2 is missing.");
        if (heatVent3 == null) Echo("Heat Vent 3 is missing.");
        if (heatVent4 == null) Echo("Heat Vent 4 is missing.");
        return false;
    }

    Echo($"Heat Vent 1 Enabled: {heatVent1.Enabled}");
    Echo($"Heat Vent 2 Enabled: {heatVent2.Enabled}");
    Echo($"Heat Vent 3 Enabled: {heatVent3.Enabled}");
    Echo($"Heat Vent 4 Enabled: {heatVent4.Enabled}");

    return heatVent1.Enabled && heatVent2.Enabled && heatVent3.Enabled && heatVent4.Enabled;
}

