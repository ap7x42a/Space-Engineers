//////////////////////////////////////////////////////////////
//  Manned Turret Operating System                         //
//  - Boot sequence with parallel multi-display animation  //
//  - Real-time combat and crane information display       //
//  - Dynamic resolution detection and font scaling        //
//  - Stateful operation with power on/off control         //
//////////////////////////////////////////////////////////////

// ----------------- TUNABLE BOOT CONFIG -----------------
float BootTextSize = 0.4f;  // Increased from 0.25f
float BootLineSpacing = 10f;  // Reduced from 30f

float CockpitBootLineStartMargin = 0.01f;  // Reduced from 0.10f
const int BootLinesPerDisplay = 16;  // Reduced from 25
const int BootStepDelay = 10; // Increased from 1 (10 ticks = ~0.17 seconds per line)
bool AllowTextScale = true;

// ----------------- DISPLAY CONFIG -----------------
string CockpitName = "Turret Control Seat";

// ----------------- BLOCK NAMES -----------------
string BatteryName = "Turret Battery";
string GantryCraneRotorName = "Gantry Crane Main Mount Rotor";
string[] GatlingGunNames = {
    "Warfare Gatling Gun",
    "Warfare Gatling Gun 10", 
    "Warfare Gatling Gun 6",
    "Warfare Gatling Gun 9"
};

string[] TurretLightNames = {
    "Turret Light Panel",
    "Turret Light Panel",
    "Turret Light Panel"
};

string[] TurretHingeNames = {
    "Turret Guns Hinge R1",
    "Turret Guns Hinge R2",
    "Turret Guns Hinge L1",
    "Turret Guns Hinge L2"
};

string[] TurretGyroNames = {
    "Turret Gyroscope 1",
    "Turret Gyroscope 2", 
    "Turret Gyroscope 3"
};

string GunDoorsGroupName = "Gun Doors";

// ----------------- OPERATIONAL CONFIG -----------------
float StatusFontSize = 0.5f;
float StatusLineSpacing = 15f;
Vector2 StatusStartPosition = new Vector2(10, 45);

// Colors
Color BootTextColor = Color.LimeGreen;
Color SystemReadyColor = Color.Cyan;
Color AlertColor = Color.Orange;
Color DangerColor = Color.Red;
Color SuccessColor = Color.Green;
Color WarningColor = Color.Yellow;
Color StandbyColor = Color.LightBlue;

// ----------------- GLOBALS -----------------
Dictionary<string, IMyTextSurface> displays = new Dictionary<string, IMyTextSurface>();
List<List<string>> bootLines = new List<List<string>>();

// System state
bool isBooted = false;
bool isBooting = false;
int bootTimer = 0;
int revealCount = 0;
DateTime bootStartTime;

// Operational state
bool emergencyMode = false;

// Target lock state tracking
bool hasTargetLock = false;
DateTime lastFireTime = DateTime.MinValue;
int lastTotalAmmo = 0;

// Block references
IMyCockpit cockpit = null;
IMyBatteryBlock turretBattery = null;
IMyMotorStator gantryCraneRotor = null;
List<IMyUserControllableGun> gatlingGuns = new List<IMyUserControllableGun>();
List<IMyLightingBlock> turretLights = new List<IMyLightingBlock>();
List<IMyMotorStator> turretHinges = new List<IMyMotorStator>();
List<IMyGyro> turretGyros = new List<IMyGyro>();
IMyBlockGroup gunDoorsGroup = null;

// ----------------- INITIALIZATION -----------------
// This runs when the script is first loaded
bool scriptInitialized = false;

void InitializeScript()
{
    if (scriptInitialized) return;
    
    Runtime.UpdateFrequency = UpdateFrequency.Update1; // Update every tick for responsiveness
    
    InitializeDisplays();
    InitializeBlocks();
    GenerateBootLines();
    
    Echo("Manned Turret OS Initialized");
    Echo("Status: Powered Off");
    Echo("Use argument 'boot' to initialize system");
    
    scriptInitialized = true;
}

// ----------------- MAIN FUNCTION -----------------
void Main(string argument, UpdateType updateSource)
{
    // Initialize script on first run
    InitializeScript();
    
    // Handle commands
    if ((updateSource & (UpdateType.Terminal | UpdateType.Trigger)) != 0)
    {
        if (argument.ToLower() == "boot" && !isBooted && !isBooting)
        {
            StartBootSequence();
            return;
        }
        
        if (argument.ToLower() == "shutdown" && isBooted)
        {
            Shutdown();
            return;
        }
        
        if (argument.ToLower() == "emergency")
        {
            emergencyMode = !emergencyMode;
            Echo($"Emergency mode: {(emergencyMode ? "ACTIVE" : "INACTIVE")}");
            return;
        }
        
        if (argument.ToLower() == "activate" && isBooted && !IsTacticalMode())
        {
            ActivateWeapons();
            return;
        }
        
        if (argument.ToLower() == "deactivate" && isBooted)
        {
            DeactivateWeapons();
            return;
        }
    }
    
    // Update boot sequence
    if (isBooting)
    {
        UpdateBootSequence();
    }
    
    // Update operational displays
    if (isBooted && !isBooting)
    {
        UpdateOperationalDisplays();
        UpdateLighting();
        UpdateGyroControl();
        UpdateTargetLock();
    }
    else if (!isBooting)
    {
        // Keep displays black when waiting for boot command
        foreach (var display in displays.Values)
        {
            ClearDisplay(display);
        }
        SetLightsOff();
        SetGyroControl(false);
        SetWeaponsEnabled(false);
    }
    
    UpdateStatus();
}

void InitializeDisplays()
{
    displays.Clear();
    
    cockpit = GridTerminalSystem.GetBlockWithName(CockpitName) as IMyCockpit;
    if (cockpit == null)
    {
        Echo($"ERROR: Cockpit '{CockpitName}' not found!");
        return;
    }
    
    // Initialize the three top displays of the Industrial Cockpit
    displays["TopLeft"] = cockpit.GetSurface(0);
    displays["TopCenter"] = cockpit.GetSurface(1);
    displays["TopRight"] = cockpit.GetSurface(2);
    
    // Debug display sizes - show both SurfaceSize and TextureSize
    foreach (var kvp in displays)
    {
        var surface = kvp.Value;
        Echo($"{kvp.Key} - Surface: {surface.SurfaceSize.X} x {surface.SurfaceSize.Y}, Texture: {surface.TextureSize.X} x {surface.TextureSize.Y}");
    }
    
    // Set all displays to script mode and make them black
    foreach (var display in displays.Values)
    {
        display.ContentType = ContentType.SCRIPT;
        display.Script = "";
        using (var frame = display.DrawFrame())
        {
            // Draw a black rectangle covering the entire display using TextureSize
            var rect = MySprite.CreateSprite("SquareSimple", display.TextureSize * 0.5f, display.TextureSize);
            rect.Color = Color.Black;
            frame.Add(rect);
        }
    }
    Echo($"Initialized {displays.Count} cockpit displays - all black");
}

void InitializeBlocks()
{
    // Initialize battery
    turretBattery = GridTerminalSystem.GetBlockWithName(BatteryName) as IMyBatteryBlock;
    if (turretBattery == null)
        Echo($"WARNING: Battery '{BatteryName}' not found!");
    
    // Initialize gantry crane rotor
    gantryCraneRotor = GridTerminalSystem.GetBlockWithName(GantryCraneRotorName) as IMyMotorStator;
    if (gantryCraneRotor == null)
        Echo($"WARNING: Gantry crane rotor '{GantryCraneRotorName}' not found!");
    
    // Initialize gatling guns
    gatlingGuns.Clear();
    foreach (var gunName in GatlingGunNames)
    {
        var gun = GridTerminalSystem.GetBlockWithName(gunName) as IMyUserControllableGun;
        if (gun != null)
        {
            gatlingGuns.Add(gun);
        }
        else
        {
            Echo($"WARNING: Gatling gun '{gunName}' not found!");
        }
    }
    
    // Initialize turret lights
    turretLights.Clear();
    foreach (var lightName in TurretLightNames)
    {
        var light = GridTerminalSystem.GetBlockWithName(lightName) as IMyLightingBlock;
        if (light != null)
        {
            turretLights.Add(light);
        }
        else
        {
            Echo($"WARNING: Turret light '{lightName}' not found!");
        }
    }
    
    // Initialize turret hinges
    turretHinges.Clear();
    foreach (var hingeName in TurretHingeNames)
    {
        var hinge = GridTerminalSystem.GetBlockWithName(hingeName) as IMyMotorStator;
        if (hinge != null)
        {
            turretHinges.Add(hinge);
        }
        else
        {
            Echo($"WARNING: Turret hinge '{hingeName}' not found!");
        }
    }
    
    // Initialize turret gyros
    turretGyros.Clear();
    foreach (var gyroName in TurretGyroNames)
    {
        var gyro = GridTerminalSystem.GetBlockWithName(gyroName) as IMyGyro;
        if (gyro != null)
        {
            turretGyros.Add(gyro);
        }
        else
        {
            Echo($"WARNING: Turret gyro '{gyroName}' not found!");
        }
    }
    
    // Initialize gun doors group
    gunDoorsGroup = GridTerminalSystem.GetBlockGroupWithName(GunDoorsGroupName);
    if (gunDoorsGroup == null)
        Echo($"WARNING: Gun doors group '{GunDoorsGroupName}' not found!");
    
    Echo($"Initialized {gatlingGuns.Count}/{GatlingGunNames.Length} gatling guns");
    Echo($"Initialized {turretLights.Count}/{TurretLightNames.Length} turret lights");
    Echo($"Initialized {turretHinges.Count}/{TurretHingeNames.Length} turret hinges");
    Echo($"Initialized {turretGyros.Count}/{TurretGyroNames.Length} turret gyros");
}

void GenerateBootLines()
{
    bootLines.Clear();
    
    // Generate boot lines for each display
    for (int displayIndex = 0; displayIndex < 3; displayIndex++)
    {
        var lines = new List<string>();
        
        if (displayIndex == 0) // Left display - System initialization
        {
            lines.Add("Turret OS v3.1 - Initializing...");
            lines.Add("Memory allocation");
            lines.Add("Core systems check");
            lines.Add("Display driver load");
            lines.Add("Weapon system interface");
            lines.Add("Targeting computer");
            lines.Add("Crane control module");
            lines.Add("Hydraulic systems");
            lines.Add("Safety protocols");
            lines.Add("Emergency systems");
            lines.Add("Power management");
            lines.Add("Communication array");
            lines.Add("Sensor network");
            lines.Add("Navigation systems");
            lines.Add("Combat algorithms");
            lines.Add("All systems operational");
        }
        else if (displayIndex == 1) // Center display - Hardware checks
        {
            lines.Add("Hardware Diagnostic");
            lines.Add("CPU: Operational");
            lines.Add("RAM: 16GB Available");
            lines.Add("Storage: Online");
            lines.Add("Network: Connected");
            lines.Add("Gyroscopes: Calibrated");
            lines.Add("Thrusters: Ready");
            lines.Add("Weapons: Armed");
            lines.Add("Sensors: Active");
            lines.Add("Crane Motors: Online");
            lines.Add("Drill Systems: Ready");
            lines.Add("Piston Array: Functional");
            lines.Add("Power Cells: Charged");
            lines.Add("Cooling: Optimal");
            lines.Add("Diagnostics Complete");
            lines.Add("System Ready");
        }
        else // Right display - Security and status
        {
            lines.Add("Security Protocols");
            lines.Add("Biometric scan");
            lines.Add("Access verified");
            lines.Add("Threat assessment");
            lines.Add("Perimeter scan");
            lines.Add("IFF systems");
            lines.Add("Target acquisition");
            lines.Add("Fire control");
            lines.Add("Ammunition check");
            lines.Add("Barrel temperature");
            lines.Add("Tracking systems");
            lines.Add("Range finding");
            lines.Add("Ballistics computer");
            lines.Add("Crane positioning");
            lines.Add("Mining protocols");
            lines.Add("Defense systems active");
        }
        
        bootLines.Add(lines);
    }
}

void StartBootSequence()
{
    isBooting = true;
    bootTimer = 0;
    revealCount = 0;
    bootStartTime = DateTime.Now;
    
    Echo("Boot sequence initiated...");
    
    // Clear all displays and set to script mode
    foreach (var display in displays.Values)
    {
        display.ContentType = ContentType.SCRIPT;
        ClearDisplay(display);
    }
}

void UpdateBootSequence()
{
    bootTimer++;
    
    if (bootTimer >= BootStepDelay)
    {
        bootTimer = 0;
        revealCount++;
        
        Echo($"Boot step: {revealCount}/{BootLinesPerDisplay}");
        
        if (revealCount <= BootLinesPerDisplay)
        {
            // Draw boot lines on all displays simultaneously
            int displayIndex = 0;
            foreach (var display in displays.Values)
            {
                if (displayIndex < bootLines.Count)
                {
                    DrawBootDisplay(display, bootLines[displayIndex], revealCount);
                }
                displayIndex++;
            }
        }
        else
        {
            // Boot sequence complete
            CompleteBootSequence();
        }
    }
}

void DrawBootDisplay(IMyTextSurface surface, List<string> lines, int revealedCount)
{
    using (var frame = surface.DrawFrame())
    {
        // Use TextureSize for proper pixel-based calculations
        var viewport = surface.TextureSize;
        
        // Start position with minimal margin
        var startPos = new Vector2(viewport.X * CockpitBootLineStartMargin + 5, 10);
        
        // Calculate text scale based on available space
        float textScale = BootTextSize;
        if (AllowTextScale)
        {
            // Calculate how many lines can fit in the texture area
            float availableHeight = viewport.Y - 20; // Leave small margins
            float maxLines = availableHeight / BootLineSpacing;
            if (lines.Count > maxLines)
                textScale *= maxLines / lines.Count;
            
            // Ensure minimum readable size
            if (textScale < 0.3f) textScale = 0.3f;
            if (textScale > 1.0f) textScale = 1.0f;
        }
        
        // Draw revealed lines
        for (int i = 0; i < Math.Min(revealedCount, lines.Count); i++)
        {
            var lineText = lines[i];
            var pos = new Vector2(startPos.X, startPos.Y + (i * BootLineSpacing));
            
            // Skip if line would be off screen
            if (pos.Y > viewport.Y - 10) 
            {
                break;
            }
            
            // Draw text
            var text = MySprite.CreateText(lineText, "Monospace", BootTextColor, textScale);
            text.Position = pos;
            text.Alignment = TextAlignment.LEFT;
            frame.Add(text);
            
            // Draw status box - positioned from right edge (all successful for now)
            var boxPos = new Vector2(viewport.X - 30, pos.Y);
            var box = MySprite.CreateText("[OK]", "Monospace", Color.Green, textScale * 0.8f);
            box.Position = boxPos;
            box.Alignment = TextAlignment.RIGHT;
            frame.Add(box);
        }
    }
}

void CompleteBootSequence()
{
    isBooting = false;
    isBooted = true;
    
    var bootTime = (DateTime.Now - bootStartTime).TotalSeconds;
    Echo($"Boot sequence complete - {bootTime:F1}s");
    Echo("System operational");
}

void UpdateOperationalDisplays()
{
    var currentTime = DateTime.Now.ToString("HH:mm:ss");
    var uptime = (DateTime.Now - bootStartTime).ToString(@"hh\:mm\:ss");
    
    // Left display - System status
    if (displays.ContainsKey("TopLeft"))
    {
        DrawSystemStatus(displays["TopLeft"], currentTime, uptime);
    }
    
    // Center display - Main interface
    if (displays.ContainsKey("TopCenter"))
    {
        DrawMainInterface(displays["TopCenter"], currentTime);
    }
    
    // Right display - Turret and crane info
    if (displays.ContainsKey("TopRight"))
    {
        DrawTurretInfo(displays["TopRight"], currentTime);
    }
}

void DrawSystemStatus(IMyTextSurface surface, string currentTime, string uptime)
{
    using (var frame = surface.DrawFrame())
    {
        var viewport = surface.TextureSize;
        var pos = StatusStartPosition;
        var fontSize = StatusFontSize;
        
        // Title - Red sprite text
        var title = MySprite.CreateText("SYSTEM STATUS", "Monospace", DangerColor, fontSize * 1.2f);
        title.Position = pos;
        title.Alignment = TextAlignment.LEFT;
        frame.Add(title);
        pos.Y += StatusLineSpacing * 2;
        
        // Status - Check battery state
        bool batteryOn = turretBattery != null && turretBattery.Enabled;
        string statusText = batteryOn ? "OPERATIONAL" : "EMERGENCY";
        Color statusColor = batteryOn ? SuccessColor : DangerColor;
        
        var statusLabel = MySprite.CreateText("Status:", "Monospace", Color.White, fontSize);
        statusLabel.Position = pos;
        statusLabel.Alignment = TextAlignment.LEFT;
        frame.Add(statusLabel);
        
        var statusValue = MySprite.CreateText(statusText, "Monospace", statusColor, fontSize);
        statusValue.Position = new Vector2(viewport.X - 10, pos.Y);
        statusValue.Alignment = TextAlignment.RIGHT;
        frame.Add(statusValue);
        pos.Y += StatusLineSpacing;
        
        // Power - Follow battery state
        string powerText = batteryOn ? "ONLINE" : "OFFLINE";
        Color powerColor = batteryOn ? SuccessColor : DangerColor;
        
        var powerLabel = MySprite.CreateText("Power:", "Monospace", Color.White, fontSize);
        powerLabel.Position = pos;
        powerLabel.Alignment = TextAlignment.LEFT;
        frame.Add(powerLabel);
        
        var powerValue = MySprite.CreateText(powerText, "Monospace", powerColor, fontSize);
        powerValue.Position = new Vector2(viewport.X - 10, pos.Y);
        powerValue.Alignment = TextAlignment.RIGHT;
        frame.Add(powerValue);
        pos.Y += StatusLineSpacing;
        
        // Weapons - Monitor gatling guns
        string weaponStatus = GetWeaponStatus();
        Color weaponColor = GetWeaponStatusColor(weaponStatus);
        
        var weaponLabel = MySprite.CreateText("Weapons:", "Monospace", Color.White, fontSize);
        weaponLabel.Position = pos;
        weaponLabel.Alignment = TextAlignment.LEFT;
        frame.Add(weaponLabel);
        
        var weaponValue = MySprite.CreateText(weaponStatus, "Monospace", weaponColor, fontSize);
        weaponValue.Position = new Vector2(viewport.X - 10, pos.Y);
        weaponValue.Alignment = TextAlignment.RIGHT;
        frame.Add(weaponValue);
        pos.Y += StatusLineSpacing;
        
        // Safety - Based on gatling gun state
        bool allWeaponsOff = true;
        if (gatlingGuns.Count > 0)
        {
            allWeaponsOff = gatlingGuns.All(gun => gun == null || !gun.Enabled);
        }
        string safetyText = allWeaponsOff ? "ENABLED" : "OVERRIDE";
        Color safetyColor = allWeaponsOff ? SuccessColor : DangerColor;
        
        var safetyLabel = MySprite.CreateText("Safety:", "Monospace", Color.White, fontSize);
        safetyLabel.Position = pos;
        safetyLabel.Alignment = TextAlignment.LEFT;
        frame.Add(safetyLabel);
        
        var safetyValue = MySprite.CreateText(safetyText, "Monospace", safetyColor, fontSize);
        safetyValue.Position = new Vector2(viewport.X - 10, pos.Y);
        safetyValue.Alignment = TextAlignment.RIGHT;
        frame.Add(safetyValue);
        pos.Y += StatusLineSpacing;
        
        // Target Lock - Proper target lock simulation
        bool tacticalActive = IsTacticalMode();
        string targetText;
        Color targetColor;
        
        if (!tacticalActive)
        {
            targetText = "NO TARGET";
            targetColor = Color.Gray;
        }
        else if (hasTargetLock)
        {
            targetText = "LOCKED";
            targetColor = DangerColor;
        }
        else
        {
            targetText = "SEARCHING";
            targetColor = WarningColor;
        }
        
        var targetLabel = MySprite.CreateText("Target:", "Monospace", Color.White, fontSize);
        targetLabel.Position = pos;
        targetLabel.Alignment = TextAlignment.LEFT;
        frame.Add(targetLabel);
        
        var targetValue = MySprite.CreateText(targetText, "Monospace", targetColor, fontSize);
        targetValue.Position = new Vector2(viewport.X - 10, pos.Y);
        targetValue.Alignment = TextAlignment.RIGHT;
        frame.Add(targetValue);
        pos.Y += StatusLineSpacing * 2;
        
        // Time and uptime
        var timeLabel = MySprite.CreateText("Time:", "Monospace", Color.White, fontSize);
        timeLabel.Position = pos;
        timeLabel.Alignment = TextAlignment.LEFT;
        frame.Add(timeLabel);
        
        var timeValue = MySprite.CreateText(currentTime, "Monospace", Color.White, fontSize);
        timeValue.Position = new Vector2(viewport.X - 10, pos.Y);
        timeValue.Alignment = TextAlignment.RIGHT;
        frame.Add(timeValue);
        pos.Y += StatusLineSpacing;
        
        var uptimeLabel = MySprite.CreateText("Uptime:", "Monospace", Color.White, fontSize);
        uptimeLabel.Position = pos;
        uptimeLabel.Alignment = TextAlignment.LEFT;
        frame.Add(uptimeLabel);
        
        var uptimeValue = MySprite.CreateText(uptime, "Monospace", Color.White, fontSize);
        uptimeValue.Position = new Vector2(viewport.X - 10, pos.Y);
        uptimeValue.Alignment = TextAlignment.RIGHT;
        frame.Add(uptimeValue);
    }
}

void DrawMainInterface(IMyTextSurface surface, string currentTime)
{
    using (var frame = surface.DrawFrame())
    {
        var center = surface.SurfaceSize * 0.5f;
        bool tacticalMode = IsTacticalMode();
        
        if (tacticalMode)
        {
            // Tactical mode display - CUSTOMIZABLE POSITIONS
            var modeText = MySprite.CreateText("TACTICAL MODE", "Monospace", DangerColor, 0.8f);
            modeText.Position = center + new Vector2(0, -40);  // Move title higher: -60 instead of -40
            modeText.Alignment = TextAlignment.CENTER;
            frame.Add(modeText);
            
            // Check if any weapons are firing
            bool anyWeaponsFiring = gatlingGuns.Any(gun => gun != null && gun.IsShooting);
            string weaponStatusText = anyWeaponsFiring ? "FIRING" : "WEAPONS READY";
            Color weaponStatusColor = anyWeaponsFiring ? DangerColor : AlertColor;
            
            var statusText = MySprite.CreateText(weaponStatusText, "Monospace", weaponStatusColor, 0.6f);
            statusText.Position = center + new Vector2(0, 0);  // Move status higher: -35 instead of -20
            statusText.Alignment = TextAlignment.CENTER;
            frame.Add(statusText);
            
            // Display ammo counts for each gatling gun
            var ammoPos = new Vector2(center.X, center.Y + 20);  // Start ammo list below center instead of above
            int lineSpacing = 20;  // Increase spacing between gun lines
            
            for (int i = 0; i < gatlingGuns.Count && i < 4; i++)
            {
                var gun = gatlingGuns[i];
                int magazineCount = GetGunAmmo(gun);
                int maxMagazines = 4; // Each gun holds 4 magazines when fully loaded
                
                // Calculate percentage
                float percentage = maxMagazines > 0 ? (float)magazineCount / maxMagazines * 100f : 0f;
                
                string ammoText;
                if (magazineCount == 0)
                {
                    ammoText = "NO AMMO";
                }
                else
                {
                    ammoText = $"{percentage:F0}%";
                }
                
                Color ammoColor = magazineCount == 0 ? DangerColor : GetAmmoColor(magazineCount, maxMagazines);
                
                // Split the text - gun label in white, ammo info in color
                var gunLabel = MySprite.CreateText($"Gatling Gun {i + 1}: ", "Monospace", Color.White, 0.5f);
                gunLabel.Position = new Vector2(center.X - 100, ammoPos.Y); // Position gun labels to the left
                gunLabel.Alignment = TextAlignment.LEFT;
                frame.Add(gunLabel);
                
                var ammoLabel = MySprite.CreateText(ammoText, "Monospace", ammoColor, 0.5f);
                ammoLabel.Position = new Vector2(center.X + 100, ammoPos.Y); // Align ammo to right side
                ammoLabel.Alignment = TextAlignment.RIGHT;
                frame.Add(ammoLabel);
                
                ammoPos.Y += lineSpacing;  // Use variable spacing
            }
        }
        else
        {
            // Defensive mode display
            var modeText = MySprite.CreateText("DEFENSIVE MODE", "Monospace", SuccessColor, 0.8f);
            modeText.Position = center + new Vector2(0, -30);
            modeText.Alignment = TextAlignment.CENTER;
            frame.Add(modeText);
            
            var statusText = MySprite.CreateText("SYSTEM STANDBY", "Monospace", StandbyColor, 0.6f);
            statusText.Position = center + new Vector2(0, 0);
            statusText.Alignment = TextAlignment.CENTER;
            frame.Add(statusText);
            
            var timeText = MySprite.CreateText(currentTime, "Monospace", Color.Gray, 0.5f);
            timeText.Position = center + new Vector2(0, 30);
            timeText.Alignment = TextAlignment.CENTER;
            frame.Add(timeText);
            
            // Display facing angle
            string facingAngleText = "Facing Angle: N/A";
            if (cockpit != null)
            {
                try
                {
                    // Since the programmable block is on the turret head, use the cockpit's orientation
                    var cockpitMatrix = cockpit.WorldMatrix;
                    var forward = cockpitMatrix.Forward;
                    
                    // Calculate angle in horizontal plane (XZ plane)
                    double angleRadians = Math.Atan2(forward.X, -forward.Z);
                    
                    // Convert to degrees and normalize to 0-360
                    double angleDegrees = angleRadians * 180.0 / Math.PI;
                    if (angleDegrees < 0) angleDegrees += 360.0;
                    
                    facingAngleText = $"Facing Angle: {angleDegrees:F1}°";
                }
                catch (Exception e)
                {
                    facingAngleText = "Facing Angle: ERROR";
                }
            }
            
            var angleText = MySprite.CreateText(facingAngleText, "Monospace", WarningColor, 0.5f);
            angleText.Position = center + new Vector2(0, 60);
            angleText.Alignment = TextAlignment.CENTER;
            frame.Add(angleText);
        }
    }
}

void DrawTurretInfo(IMyTextSurface surface, string currentTime)
{
    using (var frame = surface.DrawFrame())
    {
        var viewport = surface.TextureSize;
        var pos = StatusStartPosition;
        var fontSize = StatusFontSize;
        
        // Title - Yellow sprite text
        var title = MySprite.CreateText("GANTRY CRANE", "Monospace", WarningColor, fontSize * 1.2f);
        title.Position = pos;
        title.Alignment = TextAlignment.LEFT;
        frame.Add(title);
        pos.Y += StatusLineSpacing * 2;
        
        // Check if gantry crane rotor is attached to something
        bool craneAttached = gantryCraneRotor != null && gantryCraneRotor.IsAttached;
        
        // DRILL SYSTEMS label
        var drillLabel = MySprite.CreateText("DRILL SYSTEMS:", "Monospace", Color.White, fontSize);
        drillLabel.Position = pos;
        drillLabel.Alignment = TextAlignment.LEFT;
        frame.Add(drillLabel);
        
        if (!craneAttached)
        {
            // Show OFFLINE status aligned to the right
            var drillStatus = MySprite.CreateText("OFFLINE", "Monospace", DangerColor, fontSize);
            drillStatus.Position = new Vector2(viewport.X - 10, pos.Y);
            drillStatus.Alignment = TextAlignment.RIGHT;
            frame.Add(drillStatus);
        }
        else
        {
            // TODO: Add drill system status when built
            var drillStatus = MySprite.CreateText("READY", "Monospace", SuccessColor, fontSize);
            drillStatus.Position = new Vector2(viewport.X - 10, pos.Y);
            drillStatus.Alignment = TextAlignment.RIGHT;
            frame.Add(drillStatus);
        }
        pos.Y += StatusLineSpacing;
        
        // CRANE SYSTEMS label
        var craneLabel = MySprite.CreateText("CRANE SYSTEMS:", "Monospace", Color.White, fontSize);
        craneLabel.Position = pos;
        craneLabel.Alignment = TextAlignment.LEFT;
        frame.Add(craneLabel);
        
        if (!craneAttached)
        {
            // Show OFFLINE status aligned to the right
            var craneStatus = MySprite.CreateText("OFFLINE", "Monospace", DangerColor, fontSize);
            craneStatus.Position = new Vector2(viewport.X - 10, pos.Y);
            craneStatus.Alignment = TextAlignment.RIGHT;
            frame.Add(craneStatus);
        }
        else
        {
            // TODO: Add crane system status when built
            var craneStatus = MySprite.CreateText("READY", "Monospace", SuccessColor, fontSize);
            craneStatus.Position = new Vector2(viewport.X - 10, pos.Y);
            craneStatus.Alignment = TextAlignment.RIGHT;
            frame.Add(craneStatus);
        }
        pos.Y += StatusLineSpacing * 2;
        
        // If systems are offline, collapse the sub-statuses and move everything up
        if (craneAttached)
        {
            // TODO: Add detailed sub-system statuses here when crane is built
            // For now, just placeholder text
            var placeholderText = MySprite.CreateText("[ Detailed systems pending construction ]", "Monospace", Color.Gray, fontSize * 0.8f);
            placeholderText.Position = pos;
            placeholderText.Alignment = TextAlignment.LEFT;
            frame.Add(placeholderText);
            pos.Y += StatusLineSpacing * 2;
        }
        
        // Last update time
        var updateLabel = MySprite.CreateText("Last Update:", "Monospace", Color.White, fontSize * 0.9f);
        updateLabel.Position = pos;
        updateLabel.Alignment = TextAlignment.LEFT;
        frame.Add(updateLabel);
        
        var updateValue = MySprite.CreateText(currentTime, "Monospace", Color.White, fontSize * 0.9f);
        updateValue.Position = new Vector2(viewport.X - 10, pos.Y);
        updateValue.Alignment = TextAlignment.RIGHT;
        frame.Add(updateValue);
    }
}

void Shutdown()
{
    isBooted = false;
    isBooting = false;
    emergencyMode = false;
    hasTargetLock = false;
    manualWeaponActivation = false; // Reset manual activation flag
    
    // Turn off lights and disable gyro control
    SetLightsOff();
    SetGyroControl(false);
    SetWeaponsEnabled(false);
    
    // Clear all displays and show shutdown message
    foreach (var display in displays.Values)
    {
        using (var frame = display.DrawFrame())
        {
            var center = display.TextureSize * 0.5f;
            var text = MySprite.CreateText("SYSTEM SHUTDOWN", "Monospace", Color.Red, 0.8f);
            text.Position = center;
            text.Alignment = TextAlignment.CENTER;
            frame.Add(text);
        }
    }
    
    Echo("System shutdown complete");
}

void ClearDisplay(IMyTextSurface surface)
{
    using (var frame = surface.DrawFrame())
    {
        // Draw a black rectangle covering the entire display using TextureSize
        var rect = MySprite.CreateSprite("SquareSimple", surface.TextureSize * 0.5f, surface.TextureSize);
        rect.Color = Color.Black;
        frame.Add(rect);
    }
}

// ----------------- WEAPON STATUS HELPERS -----------------
bool IsTacticalMode()
{
    // Tactical mode is active if any weapon is enabled
    if (gatlingGuns.Count == 0) return false;
    return gatlingGuns.Any(gun => gun != null && gun.Enabled);
}

string GetWeaponStatus()
{
    if (gatlingGuns.Count == 0) return "NO GUNS";
    
    int validGuns = gatlingGuns.Count(gun => gun != null);
    if (validGuns == 0) return "NO GUNS";
    
    int enabledGuns = gatlingGuns.Count(gun => gun != null && gun.Enabled);
    if (enabledGuns == 0) return "OFFLINE";
    
    int firingGuns = 0;
    int readyGuns = 0;
    int emptyGuns = 0;
    
    foreach (var gun in gatlingGuns)
    {
        if (gun == null || !gun.Enabled) continue;
        
        if (gun.IsShooting)
        {
            firingGuns++;
        }
        else
        {
            int ammo = GetGunAmmo(gun);
            if (ammo == 0)
                emptyGuns++;
            else
                readyGuns++;
        }
    }
    
    if (emptyGuns == enabledGuns) return "NO AMMO";
    if (firingGuns > 0) return "FIRING";
    if (readyGuns > 0) return "READY";
    
    return "STANDBY";
}

Color GetWeaponStatusColor(string status)
{
    switch (status)
    {
        case "READY": return SuccessColor;
        case "FIRING": return DangerColor;
        case "STANDBY": return WarningColor;
        case "OFFLINE":
        case "NO AMMO":
        case "NO GUNS": return DangerColor;
        default: return Color.White;
    }
}

int GetGunAmmo(IMyUserControllableGun gun)
{
    if (gun == null) return 0;
    
    try
    {
        // Try terminal properties approach - look for hidden ammo properties
        var properties = new List<ITerminalProperty>();
        gun.GetProperties(properties);
        
        foreach (var prop in properties)
        {
            string propId = prop.Id.ToLower();
            // Look for ammo-related properties
            if (propId.Contains("ammo") || propId.Contains("bullet") || propId.Contains("current"))
            {
                Echo($"Found property: {prop.Id}");
                // Try to get the value if it's a numeric property
                if (prop is ITerminalProperty<float>)
                {
                    var floatProp = prop as ITerminalProperty<float>;
                    float value = floatProp.GetValue(gun);
                    Echo($"  Float value: {value}");
                    if (value > 0) return (int)value;
                }
                else if (prop is ITerminalProperty<int>)
                {
                    var intProp = prop as ITerminalProperty<int>;
                    int value = intProp.GetValue(gun);
                    Echo($"  Int value: {value}");
                    if (value > 0) return value;
                }
            }
        }
        
        // If no ammo properties found, show all available properties for debugging
        Echo($"Gun {gun.DisplayNameText} - All properties:");
        foreach (var prop in properties)
        {
            Echo($"  {prop.Id} (Type: {prop.TypeName})");
        }
        
        // Fallback: Try DetailedInfo parsing
        string detailedInfo = gun.DetailedInfo;
        if (!string.IsNullOrEmpty(detailedInfo))
        {
            Echo($"DetailedInfo for {gun.DisplayNameText}:");
            Echo(detailedInfo);
            
            // Parse DetailedInfo for ammo numbers
            string[] lines = detailedInfo.Split('\n');
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (trimmedLine.ToLower().Contains("ammo"))
                {
                    string[] parts = trimmedLine.Split(':');
                    if (parts.Length >= 2)
                    {
                        string ammoPart = parts[1].Trim();
                        if (ammoPart.Contains("/"))
                        {
                            string currentAmmo = ammoPart.Split('/')[0].Trim();
                            int ammoCount;
                            if (int.TryParse(currentAmmo, out ammoCount))
                            {
                                Echo($"Parsed ammo from DetailedInfo: {ammoCount}");
                                return ammoCount;
                            }
                        }
                    }
                }
            }
        }
        
        // Last resort: try inventory but show it's not real-time
        var inventory = gun.GetInventory();
        if (inventory != null)
        {
            var items = new List<MyInventoryItem>();
            inventory.GetItems(items);
            
            int totalItems = 0;
            foreach (var item in items)
            {
                string itemType = item.Type.ToString();
                if (itemType.ToLower().Contains("ammo") || itemType.ToLower().Contains("bullet") || itemType.ToLower().Contains("magazine"))
                {
                    totalItems += (int)item.Amount;
                    Echo($"Inventory item: {itemType} = {item.Amount}");
                }
            }
            
            if (totalItems > 0)
            {
                Echo($"Magazine count: {totalItems}");
                return totalItems; // Return actual magazine count, not estimated bullets
            }
        }
        
    }
    catch (Exception e)
    {
        Echo($"Error getting ammo for {gun.DisplayNameText}: {e.Message}");
    }
    
    return 0;
}

Color GetAmmoColor(int ammo, int maxAmmo)
{
    if (ammo == 0) return DangerColor;
    float percentage = (float)ammo / maxAmmo;
    if (percentage > 0.5f) return SuccessColor;
    if (percentage > 0.25f) return AlertColor;
    return DangerColor;
}

void UpdateStatus()
{
    Echo("=== MANNED TURRET OS ===");
    if (isBooting)
        Echo($"Status: Booting ({revealCount}/{BootLinesPerDisplay})");
    else if (isBooted)
        Echo($"Status: {(emergencyMode ? "COMBAT MODE" : "Operational")}");
    else
        Echo("Status: Powered Off");
        
    Echo($"Displays: {displays.Count} connected");
    
    // Debug weapon information
    Echo($"Weapons found: {gatlingGuns.Count}/{GatlingGunNames.Length}");
    if (gatlingGuns.Count > 0)
    {
        int enabled = gatlingGuns.Count(gun => gun != null && gun.Enabled);
        int firing = gatlingGuns.Count(gun => gun != null && gun.IsShooting);
        Echo($"Enabled: {enabled}, Firing: {firing}");
        Echo($"Tactical Mode: {IsTacticalMode()}");
        Echo($"Weapon Status: {GetWeaponStatus()}");
        
        // Debug ammo counts for each gun
        for (int i = 0; i < gatlingGuns.Count; i++)
        {
            var gun = gatlingGuns[i];
            if (gun != null)
            {
                int ammo = GetGunAmmo(gun);
                Echo($"Gun {i + 1} ({gun.DisplayNameText}): {ammo} bullets");
            }
        }
    }
    
    // Debug battery information
    if (turretBattery != null)
        Echo($"Battery: {(turretBattery.Enabled ? "ON" : "OFF")}");
    else
        Echo("Battery: NOT FOUND");
    
    // Debug gantry crane rotor information
    if (gantryCraneRotor != null)
    {
        float angle = MathHelper.ToDegrees(gantryCraneRotor.Angle);
        float velocity = gantryCraneRotor.TargetVelocityRPM;
        Echo($"Gantry Rotor: {angle:F1}° (Velocity: {velocity:F2} RPM)");
    }
    else
        Echo("Gantry Rotor: NOT FOUND");
    
    Echo("Commands: 'boot', 'shutdown', 'emergency', 'activate', 'deactivate'");
}

// ----------------- LIGHTING CONTROL -----------------
void UpdateLighting()
{
    bool tacticalMode = IsTacticalMode();
    
    foreach (var light in turretLights)
    {
        if (light == null) continue;
        
        light.Enabled = true;
        
        if (tacticalMode)
        {
            // Red lighting for tactical mode
            light.Color = new Color(120, 0, 0);
            light.Radius = 3.6f;
            light.Falloff = 1.3f;
            light.Intensity = 5f;
        }
        else
        {
            // Green lighting for defensive mode
            light.Color = new Color(38, 234, 106);
            light.Radius = 3.6f;
            light.Falloff = 1.3f;
            light.Intensity = 0.5f;
        }
    }
}

void SetLightsOff()
{
    foreach (var light in turretLights)
    {
        if (light != null)
        {
            light.Enabled = false;
        }
    }
}

// ----------------- GYRO CONTROL -----------------
bool manualWeaponActivation = false;

void UpdateGyroControl()
{
    bool allowGyros = true;
    
    // Check if any turret hinge is at 89 degrees or higher
    foreach (var hinge in turretHinges)
    {
        if (hinge != null)
        {
            float hingeAngle = MathHelper.ToDegrees(hinge.Angle);
            if (hingeAngle >= 89f)
            {
                allowGyros = false;
                break; // If any hinge is at 89+, disable gyros
            }
        }
    }
    
    SetGyroControl(allowGyros);
}

void SetGyroControl(bool enabled)
{
    foreach (var gyro in turretGyros)
    {
        if (gyro != null)
        {
            gyro.Enabled = enabled;
        }
    }
    
    // Only disable weapons automatically if not manually activated
    if (!enabled && !manualWeaponActivation)
    {
        SetWeaponsEnabled(false);
    }
}

void SetWeaponsEnabled(bool enabled)
{
    foreach (var gun in gatlingGuns)
    {
        if (gun != null)
        {
            gun.Enabled = enabled;
        }
    }
}

void ActivateWeapons()
{
    Echo("Activating weapons - enabling guns");
    
    // Set manual activation flag to prevent gyro control from disabling weapons
    manualWeaponActivation = true;
    
    // Enable all weapons
    SetWeaponsEnabled(true);
}

void DeactivateWeapons()
{
    Echo("Deactivating weapons - disabling guns");
    
    // Clear manual activation flag
    manualWeaponActivation = false;
    
    // Disable all weapons
    SetWeaponsEnabled(false);
}

// ----------------- TARGET LOCK TRACKING -----------------
void UpdateTargetLock()
{
    if (!IsTacticalMode())
    {
        hasTargetLock = false;
        return;
    }
    
    // Calculate current total ammo
    int currentTotalAmmo = 0;
    bool anyWeaponFiring = false;
    
    foreach (var gun in gatlingGuns)
    {
        if (gun != null && gun.Enabled)
        {
            currentTotalAmmo += GetGunAmmo(gun);
            if (gun.IsShooting)
            {
                anyWeaponFiring = true;
            }
        }
    }
    
    // Check if ammo decreased (weapons fired)
    if (lastTotalAmmo > 0 && currentTotalAmmo < lastTotalAmmo)
    {
        hasTargetLock = true;
        lastFireTime = DateTime.Now;
    }
    
    // If weapons are actively firing, maintain lock
    if (anyWeaponFiring)
    {
        hasTargetLock = true;
        lastFireTime = DateTime.Now;
    }
    
    // Lose target lock after 30 seconds of no firing
    if (hasTargetLock && (DateTime.Now - lastFireTime).TotalSeconds > 30)
    {
        hasTargetLock = false;
    }
    
    lastTotalAmmo = currentTotalAmmo;
}
