//////////////////////////////////////////////////////////////
//  Adaptive Turret System                                 //
//  - Multi-mode operation with dynamic grid detection     //
//  - Hovercat, AssaultCat, Transfer, Turret, Crane modes  //
//  - Real-time physics and system monitoring              //
//  - Graceful handling of grid attach/detach events       //
//////////////////////////////////////////////////////////////

// ----------------- DISPLAY CONFIG -----------------
string CockpitName = "Turret Control Seat";

// ----------------- BLOCK NAMES -----------------
string BatteryName = "Turret Battery";
string GantryCraneRotorName = "Gantry Crane Main Mount Rotor";
string NeckRotorName = "Neck Rotor";
string ArenaRotorName = "Arena Rotor";

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

// ----------------- VISUAL CONFIG -----------------
float StatusFontSize = 0.5f;
float StatusLineSpacing = 15f;
Vector2 StatusStartPosition = new Vector2(10, 45);

// Colors
Color DangerColor = Color.Red;
Color SuccessColor = Color.Green;
Color AlertColor = Color.Orange;
Color WarningColor = Color.Yellow;
Color StandbyColor = Color.LightBlue;
Color PinkColor = Color.Pink;
Color GrayColor = Color.Gray;

// ----------------- SYSTEM STATE -----------------
bool systemActive = false;
bool transferModeOverride = false;
bool scriptInitialized = false;

// Block references
Dictionary<string, IMyTextSurface> displays = new Dictionary<string, IMyTextSurface>();
IMyCockpit cockpit = null;
IMyBatteryBlock turretBattery = null;
IMyMotorStator gantryCraneRotor = null;
IMyMotorStator neckRotor = null;
IMyMotorStator arenaRotor = null;
List<IMyUserControllableGun> gatlingGuns = new List<IMyUserControllableGun>();
List<IMyLightingBlock> turretLights = new List<IMyLightingBlock>();
List<IMyMotorStator> turretHinges = new List<IMyMotorStator>();
List<IMyGyro> turretGyros = new List<IMyGyro>();
List<IMyGyro> allSmallGridGyros = new List<IMyGyro>();
List<IMyThrust> allSmallGridThrusters = new List<IMyThrust>();
IMyBlockGroup gunDoorsGroup = null;

// Physics tracking
Vector3D lastPosition = Vector3D.Zero;
Vector3D velocity = Vector3D.Zero;
bool physicsInitialized = false;

// ----------------- MAIN FUNCTION -----------------
void Main(string argument, UpdateType updateSource)
{
    // Initialize script on first run
    if (!scriptInitialized)
    {
        Runtime.UpdateFrequency = UpdateFrequency.Update10; // Run every 10 ticks
        Echo("Script initializing...");
        UpdateBlockReferences(); // Initialize all blocks immediately
        scriptInitialized = true;
    }
    
    // Handle commands
    if ((updateSource & (UpdateType.Terminal | UpdateType.Trigger)) != 0)
    {
        Echo($"Received command: '{argument}'");
        
        if (argument.ToLower() == "on")
        {
            systemActive = true;
            transferModeOverride = false;
            Echo("System activated");
            return;
        }
        
        if (argument.ToLower() == "off")
        {
            systemActive = false;
            transferModeOverride = false;
            ClearAllDisplays();
            Echo("System deactivated");
            return;
        }
        
        if (argument.ToLower() == "transfer")
        {
            HandleTransferMode();
            return;
        }
        
        if (argument.ToLower() == "stop_transfer")
        {
            HandleStopTransferMode();
            return;
        }
    }
    
    if (systemActive)
    {
        Echo("Running active update cycle...");
        UpdateBlockReferences();
        UpdatePhysics();
        UpdateDisplays();
    }
    else
    {
        // Keep displays black when system is off
        if (displays.Count > 0)
        {
            ClearAllDisplays();
        }
        Echo("System inactive - displays cleared");
    }
    
    UpdateStatus();
}

// ----------------- INITIALIZATION -----------------
void UpdateBlockReferences()
{
    Echo("Updating block references...");
    
    // Initialize displays
    if (displays.Count == 0)
    {
        InitializeDisplays();
    }
    
    // Update block references (graceful handling for attach/detach)
    cockpit = GridTerminalSystem.GetBlockWithName(CockpitName) as IMyCockpit;
    turretBattery = GridTerminalSystem.GetBlockWithName(BatteryName) as IMyBatteryBlock;
    gantryCraneRotor = GridTerminalSystem.GetBlockWithName(GantryCraneRotorName) as IMyMotorStator;
    neckRotor = GridTerminalSystem.GetBlockWithName(NeckRotorName) as IMyMotorStator;
    arenaRotor = GridTerminalSystem.GetBlockWithName(ArenaRotorName) as IMyMotorStator;
    
    // Save Arena Rotor settings when first detected
    if (arenaRotor != null && arenaRotor.IsAttached)
    {
        SaveArenaRotorSettings();
    }
    
    Echo($"Key blocks: Cockpit={cockpit != null}, Battery={turretBattery != null}, Rotors={gantryCraneRotor != null}/{neckRotor != null}/{arenaRotor != null}");
    
    // Update gatling guns
    gatlingGuns.Clear();
    foreach (var gunName in GatlingGunNames)
    {
        var gun = GridTerminalSystem.GetBlockWithName(gunName) as IMyUserControllableGun;
        if (gun != null)
            gatlingGuns.Add(gun);
    }
    Echo($"Found {gatlingGuns.Count}/{GatlingGunNames.Length} gatling guns");
    
    // Update turret lights
    turretLights.Clear();
    foreach (var lightName in TurretLightNames)
    {
        var light = GridTerminalSystem.GetBlockWithName(lightName) as IMyLightingBlock;
        if (light != null)
            turretLights.Add(light);
    }
    Echo($"Found {turretLights.Count}/{TurretLightNames.Length} turret lights");
    
    // Update turret hinges
    turretHinges.Clear();
    foreach (var hingeName in TurretHingeNames)
    {
        var hinge = GridTerminalSystem.GetBlockWithName(hingeName) as IMyMotorStator;
        if (hinge != null)
            turretHinges.Add(hinge);
    }
    
    // Update turret gyros
    turretGyros.Clear();
    foreach (var gyroName in TurretGyroNames)
    {
        var gyro = GridTerminalSystem.GetBlockWithName(gyroName) as IMyGyro;
        if (gyro != null)
            turretGyros.Add(gyro);
    }
    
    // Update all small grid gyros and thrusters
    UpdateSmallGridBlocks();
    
    // Update gun doors group
    gunDoorsGroup = GridTerminalSystem.GetBlockGroupWithName(GunDoorsGroupName);
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
    
    Echo($"Found cockpit: {cockpit.DisplayName}");
    
    try
    {
        displays["TopLeft"] = cockpit.GetSurface(0);
        displays["TopCenter"] = cockpit.GetSurface(1);
        displays["TopRight"] = cockpit.GetSurface(2);
        
        Echo($"Initialized {displays.Count} displays");
        
        foreach (var kvp in displays)
        {
            var display = kvp.Value;
            display.ContentType = ContentType.SCRIPT;
            display.Script = "";
            Echo($"{kvp.Key}: {display.SurfaceSize.X}x{display.SurfaceSize.Y}");
        }
    }
    catch (Exception e)
    {
        Echo($"Display initialization error: {e.Message}");
    }
}

void UpdateSmallGridBlocks()
{
    allSmallGridGyros.Clear();
    allSmallGridThrusters.Clear();
    
    var gyros = new List<IMyGyro>();
    var thrusters = new List<IMyThrust>();
    
    GridTerminalSystem.GetBlocksOfType(gyros);
    GridTerminalSystem.GetBlocksOfType(thrusters);
    
    foreach (var gyro in gyros)
    {
        if (gyro.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Small)
            allSmallGridGyros.Add(gyro);
    }
    
    foreach (var thruster in thrusters)
    {
        if (thruster.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Small)
            allSmallGridThrusters.Add(thruster);
    }
}

// ----------------- PHYSICS TRACKING -----------------
void UpdatePhysics()
{
    if (cockpit == null) return;
    
    var currentPosition = cockpit.GetPosition();
    
    if (!physicsInitialized)
    {
        lastPosition = currentPosition;
        physicsInitialized = true;
        return;
    }
    
    velocity = (currentPosition - lastPosition) * 60; // Convert to m/s (assuming 60 UPS)
    lastPosition = currentPosition;
}

// ----------------- MODE DETECTION -----------------
string GetCurrentMode()
{
    if (transferModeOverride)
        return "TRANSFER MODE";
    
    if (gantryCraneRotor != null && gantryCraneRotor.IsAttached)
        return "CRANE MODE";
    
    if (arenaRotor != null && arenaRotor.IsAttached)
        return "TURRET MODE";
    
    if (neckRotor != null && neckRotor.IsAttached)
    {
        bool allGunsOn = gatlingGuns.Count > 0 && gatlingGuns.All(gun => gun != null && gun.Enabled);
        return allGunsOn ? "ASSAULTCAT MODE" : "HOVERCAT MODE";
    }
    
    return "TRANSFER MODE";
}

bool AreGunsOnline()
{
    return gatlingGuns.Count > 0 && gatlingGuns.Any(gun => gun != null && gun.Enabled);
}

bool AreGunsAllOnline()
{
    return gatlingGuns.Count > 0 && gatlingGuns.All(gun => gun != null && gun.Enabled);
}

bool AreGunsFiring()
{
    return gatlingGuns.Any(gun => gun != null && gun.IsShooting);
}

// ----------------- DISPLAY UPDATES -----------------
void UpdateDisplays()
{
    var currentTime = DateTime.Now.ToString("HH:mm:ss");
    
    if (displays.ContainsKey("TopLeft"))
    {
        DrawSystemStatus(displays["TopLeft"], currentTime);
    }
    
    if (displays.ContainsKey("TopCenter"))
    {
        DrawMainInterface(displays["TopCenter"], currentTime);
    }
    
    if (displays.ContainsKey("TopRight"))
    {
        DrawRightDisplay(displays["TopRight"], currentTime);
    }
}

void DrawSystemStatus(IMyTextSurface surface, string currentTime)
{
    using (var frame = surface.DrawFrame())
    {
        var viewport = surface.TextureSize;
        var pos = StatusStartPosition;
        var fontSize = StatusFontSize;
        
        // Title
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
        
        // Power
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
        
        // Weapons
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
        
        // Safety
        bool gunsOnline = AreGunsOnline();
        string safetyText = gunsOnline ? "OVERRIDE" : "ENABLED";
        Color safetyColor = gunsOnline ? DangerColor : SuccessColor;
        
        var safetyLabel = MySprite.CreateText("Safety:", "Monospace", Color.White, fontSize);
        safetyLabel.Position = pos;
        safetyLabel.Alignment = TextAlignment.LEFT;
        frame.Add(safetyLabel);
        
        var safetyValue = MySprite.CreateText(safetyText, "Monospace", safetyColor, fontSize);
        safetyValue.Position = new Vector2(viewport.X - 10, pos.Y);
        safetyValue.Alignment = TextAlignment.RIGHT;
        frame.Add(safetyValue);
        pos.Y += StatusLineSpacing * 2;
        
        // Time
        var timeLabel = MySprite.CreateText("Time:", "Monospace", Color.White, fontSize);
        timeLabel.Position = pos;
        timeLabel.Alignment = TextAlignment.LEFT;
        frame.Add(timeLabel);
        
        var timeValue = MySprite.CreateText(currentTime, "Monospace", Color.White, fontSize);
        timeValue.Position = new Vector2(viewport.X - 10, pos.Y);
        timeValue.Alignment = TextAlignment.RIGHT;
        frame.Add(timeValue);
    }
}

void DrawMainInterface(IMyTextSurface surface, string currentTime)
{
    using (var frame = surface.DrawFrame())
    {
        var center = surface.SurfaceSize * 0.5f;
        string mode = GetCurrentMode();
        
        switch (mode)
        {
            case "TRANSFER MODE":
                DrawTransferMode(frame, center);
                break;
                
            case "HOVERCAT MODE":
                DrawHovercatMode(frame, center);
                break;
                
            case "ASSAULTCAT MODE":
                DrawAssaultcatMode(frame, center);
                break;
                
            case "TURRET MODE":
                DrawTurretMode(frame, center);
                break;
                
            case "CRANE MODE":
                DrawCraneMode(frame, center);
                break;
        }
    }
}

void DrawTransferMode(MySpriteDrawFrame frame, Vector2 center)
{
    var modeText = MySprite.CreateText("TRANSFER MODE", "Monospace", GrayColor, 0.8f);
    modeText.Position = center + new Vector2(0, -30);
    modeText.Alignment = TextAlignment.CENTER;
    frame.Add(modeText);
}

void DrawHovercatMode(MySpriteDrawFrame frame, Vector2 center)
{
    var modeText = MySprite.CreateText("HOVERCAT MODE", "Monospace", PinkColor, 0.8f);
    modeText.Position = center + new Vector2(0, -30);
    modeText.Alignment = TextAlignment.CENTER;
    frame.Add(modeText);
    
    var statusText = MySprite.CreateText("DEFENSIVE MODE", "Monospace", SuccessColor, 0.6f);
    statusText.Position = center + new Vector2(0, 0);
    statusText.Alignment = TextAlignment.CENTER;
    frame.Add(statusText);
    
    DrawPhysicsAndStatus(frame, center);
}

void DrawAssaultcatMode(MySpriteDrawFrame frame, Vector2 center)
{
    var modeText = MySprite.CreateText("ASSAULTCAT MODE", "Monospace", DangerColor, 0.8f);
    modeText.Position = center + new Vector2(0, -40);
    modeText.Alignment = TextAlignment.CENTER;
    frame.Add(modeText);
    
    var tacticalText = MySprite.CreateText("TACTICAL MODE", "Monospace", GrayColor, 0.6f);
    tacticalText.Position = center + new Vector2(0, -10);
    tacticalText.Alignment = TextAlignment.CENTER;
    frame.Add(tacticalText);
    
    DrawPhysicsAndStatus(frame, center);
}

void DrawTurretMode(MySpriteDrawFrame frame, Vector2 center)
{
    var modeText = MySprite.CreateText("TURRET MODE", "Monospace", DangerColor, 0.8f);
    modeText.Position = center + new Vector2(0, -30);
    modeText.Alignment = TextAlignment.CENTER;
    frame.Add(modeText);
    
    string subModeText;
    Color subModeColor;
    
    if (AreGunsFiring())
    {
        subModeText = "FIRING";
        subModeColor = DangerColor;
    }
    else if (AreGunsOnline())
    {
        subModeText = "FIRING MODE";
        subModeColor = AlertColor;
    }
    else
    {
        subModeText = "SAFETY MODE";
        subModeColor = SuccessColor;
    }
    
    var subModeSprite = MySprite.CreateText(subModeText, "Monospace", subModeColor, 0.6f);
    subModeSprite.Position = center + new Vector2(0, 0);
    subModeSprite.Alignment = TextAlignment.CENTER;
    frame.Add(subModeSprite);
    
    DrawTurretModeStatus(frame, center);
}

void DrawCraneMode(MySpriteDrawFrame frame, Vector2 center)
{
    var modeText = MySprite.CreateText("CRANE MODE", "Monospace", WarningColor, 0.8f);
    modeText.Position = center + new Vector2(0, -30);
    modeText.Alignment = TextAlignment.CENTER;
    frame.Add(modeText);
    
    var statusText = MySprite.CreateText("SYSTEM READY", "Monospace", SuccessColor, 0.6f);
    statusText.Position = center + new Vector2(0, 0);
    statusText.Alignment = TextAlignment.CENTER;
    frame.Add(statusText);
}

void DrawPhysicsAndStatus(MySpriteDrawFrame frame, Vector2 center)
{
    var statusPos = new Vector2(center.X, center.Y + 30);
    
    // Velocity
    string velocityText = $"Velocity: {velocity.Length():F1} m/s";
    var velocitySprite = MySprite.CreateText(velocityText, "Monospace", Color.White, 0.4f);
    velocitySprite.Position = statusPos;
    velocitySprite.Alignment = TextAlignment.CENTER;
    frame.Add(velocitySprite);
    statusPos.Y += 15;
    
    // Gyros status
    bool gyrosOnline = allSmallGridGyros.Any(g => g.Enabled);
    string gyrosText = gyrosOnline ? "ONLINE" : "OFFLINE";
    Color gyrosColor = gyrosOnline ? SuccessColor : DangerColor;
    
    var gyrosLabel = MySprite.CreateText("Gyros: ", "Monospace", Color.White, 0.4f);
    gyrosLabel.Position = new Vector2(center.X - 40, statusPos.Y);
    gyrosLabel.Alignment = TextAlignment.LEFT;
    frame.Add(gyrosLabel);
    
    var gyrosStatus = MySprite.CreateText(gyrosText, "Monospace", gyrosColor, 0.4f);
    gyrosStatus.Position = new Vector2(center.X + 40, statusPos.Y);
    gyrosStatus.Alignment = TextAlignment.RIGHT;
    frame.Add(gyrosStatus);
    statusPos.Y += 15;
    
    // Thrusters status
    bool thrustersOnline = allSmallGridThrusters.Any(t => t.Enabled);
    string thrustersText = thrustersOnline ? "ONLINE" : "OFFLINE";
    Color thrustersColor = thrustersOnline ? SuccessColor : DangerColor;
    
    var thrustersLabel = MySprite.CreateText("Thrusters: ", "Monospace", Color.White, 0.4f);
    thrustersLabel.Position = new Vector2(center.X - 40, statusPos.Y);
    thrustersLabel.Alignment = TextAlignment.LEFT;
    frame.Add(thrustersLabel);
    
    var thrustersStatus = MySprite.CreateText(thrustersText, "Monospace", thrustersColor, 0.4f);
    thrustersStatus.Position = new Vector2(center.X + 40, statusPos.Y);
    thrustersStatus.Alignment = TextAlignment.RIGHT;
    frame.Add(thrustersStatus);
    statusPos.Y += 15;
    
    // Guns status
    bool gunsOnline = AreGunsOnline();
    string gunsText = gunsOnline ? "ONLINE" : "OFFLINE";
    Color gunsColor = gunsOnline ? SuccessColor : DangerColor;
    
    var gunsLabel = MySprite.CreateText("Guns: ", "Monospace", Color.White, 0.4f);
    gunsLabel.Position = new Vector2(center.X - 40, statusPos.Y);
    gunsLabel.Alignment = TextAlignment.LEFT;
    frame.Add(gunsLabel);
    
    var gunsStatus = MySprite.CreateText(gunsText, "Monospace", gunsColor, 0.4f);
    gunsStatus.Position = new Vector2(center.X + 40, statusPos.Y);
    gunsStatus.Alignment = TextAlignment.RIGHT;
    frame.Add(gunsStatus);
}

void DrawTurretModeStatus(MySpriteDrawFrame frame, Vector2 center)
{
    var statusPos = new Vector2(center.X, center.Y + 30);
    
    // Gyros status
    bool gyrosOnline = allSmallGridGyros.Any(g => g.Enabled);
    string gyrosText = gyrosOnline ? "ONLINE" : "OFFLINE";
    Color gyrosColor = gyrosOnline ? SuccessColor : DangerColor;
    
    var gyrosLabel = MySprite.CreateText("Gyros: ", "Monospace", Color.White, 0.4f);
    gyrosLabel.Position = new Vector2(center.X - 40, statusPos.Y);
    gyrosLabel.Alignment = TextAlignment.LEFT;
    frame.Add(gyrosLabel);
    
    var gyrosStatus = MySprite.CreateText(gyrosText, "Monospace", gyrosColor, 0.4f);
    gyrosStatus.Position = new Vector2(center.X + 40, statusPos.Y);
    gyrosStatus.Alignment = TextAlignment.RIGHT;
    frame.Add(gyrosStatus);
    statusPos.Y += 15;
    
    // Arena Rotor status
    var arenaLabel = MySprite.CreateText("Arena Rotor: ", "Monospace", Color.White, 0.4f);
    arenaLabel.Position = new Vector2(center.X - 40, statusPos.Y);
    arenaLabel.Alignment = TextAlignment.LEFT;
    frame.Add(arenaLabel);
    
    var arenaStatus = MySprite.CreateText("ATTACHED", "Monospace", SuccessColor, 0.4f);
    arenaStatus.Position = new Vector2(center.X + 40, statusPos.Y);
    arenaStatus.Alignment = TextAlignment.RIGHT;
    frame.Add(arenaStatus);
    statusPos.Y += 15;
    
    // Guns status
    bool gunsOnline = AreGunsOnline();
    string gunsText = gunsOnline ? "ONLINE" : "OFFLINE";
    Color gunsColor = gunsOnline ? SuccessColor : DangerColor;
    
    var gunsLabel = MySprite.CreateText("Guns: ", "Monospace", Color.White, 0.4f);
    gunsLabel.Position = new Vector2(center.X - 40, statusPos.Y);
    gunsLabel.Alignment = TextAlignment.LEFT;
    frame.Add(gunsLabel);
    
    var gunsStatus = MySprite.CreateText(gunsText, "Monospace", gunsColor, 0.4f);
    gunsStatus.Position = new Vector2(center.X + 40, statusPos.Y);
    gunsStatus.Alignment = TextAlignment.RIGHT;
    frame.Add(gunsStatus);
}

void DrawRightDisplay(IMyTextSurface surface, string currentTime)
{
    string mode = GetCurrentMode();
    
    if (mode == "CRANE MODE")
    {
        DrawCraneInfo(surface, currentTime);
    }
    else
    {
        DrawTurretInfo(surface, currentTime);
    }
}

void DrawCraneInfo(IMyTextSurface surface, string currentTime)
{
    using (var frame = surface.DrawFrame())
    {
        var viewport = surface.TextureSize;
        var pos = StatusStartPosition;
        var fontSize = StatusFontSize;
        
        var title = MySprite.CreateText("GANTRY CRANE", "Monospace", WarningColor, fontSize * 1.2f);
        title.Position = pos;
        title.Alignment = TextAlignment.LEFT;
        frame.Add(title);
        pos.Y += StatusLineSpacing * 2;
        
        bool craneAttached = gantryCraneRotor != null && gantryCraneRotor.IsAttached;
        
        var drillLabel = MySprite.CreateText("DRILL SYSTEMS:", "Monospace", Color.White, fontSize);
        drillLabel.Position = pos;
        drillLabel.Alignment = TextAlignment.LEFT;
        frame.Add(drillLabel);
        
        string drillStatus = craneAttached ? "READY" : "OFFLINE";
        Color drillColor = craneAttached ? SuccessColor : DangerColor;
        
        var drillStatusSprite = MySprite.CreateText(drillStatus, "Monospace", drillColor, fontSize);
        drillStatusSprite.Position = new Vector2(viewport.X - 10, pos.Y);
        drillStatusSprite.Alignment = TextAlignment.RIGHT;
        frame.Add(drillStatusSprite);
        pos.Y += StatusLineSpacing;
        
        var craneLabel = MySprite.CreateText("CRANE SYSTEMS:", "Monospace", Color.White, fontSize);
        craneLabel.Position = pos;
        craneLabel.Alignment = TextAlignment.LEFT;
        frame.Add(craneLabel);
        
        string craneStatus = craneAttached ? "READY" : "OFFLINE";
        Color craneColor = craneAttached ? SuccessColor : DangerColor;
        
        var craneStatusSprite = MySprite.CreateText(craneStatus, "Monospace", craneColor, fontSize);
        craneStatusSprite.Position = new Vector2(viewport.X - 10, pos.Y);
        craneStatusSprite.Alignment = TextAlignment.RIGHT;
        frame.Add(craneStatusSprite);
        pos.Y += StatusLineSpacing * 2;
        
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

void DrawTurretInfo(IMyTextSurface surface, string currentTime)
{
    using (var frame = surface.DrawFrame())
    {
        var viewport = surface.TextureSize;
        var pos = StatusStartPosition;
        var fontSize = StatusFontSize;
        
        var title = MySprite.CreateText("TURRET INFO", "Monospace", DangerColor, fontSize * 1.2f);
        title.Position = pos;
        title.Alignment = TextAlignment.LEFT;
        frame.Add(title);
        pos.Y += StatusLineSpacing * 2;
        
        var modeLabel = MySprite.CreateText("Mode:", "Monospace", Color.White, fontSize);
        modeLabel.Position = pos;
        modeLabel.Alignment = TextAlignment.LEFT;
        frame.Add(modeLabel);
        
        var modeValue = MySprite.CreateText(GetCurrentMode(), "Monospace", Color.White, fontSize);
        modeValue.Position = new Vector2(viewport.X - 10, pos.Y);
        modeValue.Alignment = TextAlignment.RIGHT;
        frame.Add(modeValue);
        pos.Y += StatusLineSpacing;
        
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

// ----------------- UTILITY FUNCTIONS -----------------
string GetWeaponStatus()
{
    if (gatlingGuns.Count == 0) return "NO GUNS";
    
    int validGuns = gatlingGuns.Count(gun => gun != null);
    if (validGuns == 0) return "NO GUNS";
    
    int enabledGuns = gatlingGuns.Count(gun => gun != null && gun.Enabled);
    if (enabledGuns == 0) return "OFFLINE";
    
    if (AreGunsFiring()) return "FIRING";
    
    return "READY";
}

Color GetWeaponStatusColor(string status)
{
    switch (status)
    {
        case "READY": return SuccessColor;
        case "FIRING": return DangerColor;
        case "OFFLINE":
        case "NO GUNS": return DangerColor;
        default: return Color.White;
    }
}

void ClearAllDisplays()
{
    foreach (var display in displays.Values)
    {
        if (display != null)
        {
            using (var frame = display.DrawFrame())
            {
                var rect = MySprite.CreateSprite("SquareSimple", display.TextureSize * 0.5f, display.TextureSize);
                rect.Color = Color.Black;
                frame.Add(rect);
            }
        }
    }
}

void HandleTransferMode()
{
    transferModeOverride = true;
    
    if (arenaRotor != null)
    {
        try
        {
            arenaRotor.Torque = 33600000f;
            arenaRotor.BrakingTorque = 0f;
            arenaRotor.LowerLimitDeg = 0f;
            arenaRotor.UpperLimitDeg = 180f;
            arenaRotor.TargetVelocityRPM = -2f;
            Echo("Arena rotor configured for transfer mode");
        }
        catch (Exception e)
        {
            Echo($"Error configuring arena rotor: {e.Message}");
        }
    }
    else
    {
        Echo("Arena rotor not found for transfer mode");
    }
    
    // Turn off small grid gyros
    int gyrosDisabled = 0;
    foreach (var gyro in allSmallGridGyros)
    {
        if (gyro != null)
        {
            gyro.Enabled = false;
            gyrosDisabled++;
        }
    }
    Echo($"Disabled {gyrosDisabled} small grid gyros");
    
    // Turn off all gatling guns
    int gunsDisabled = 0;
    foreach (var gun in gatlingGuns)
    {
        if (gun != null)
        {
            gun.Enabled = false;
            gunsDisabled++;
        }
    }
    Echo($"Disabled {gunsDisabled} gatling guns");
    
    Echo("Transfer mode activated successfully");
}

void SaveArenaRotorSettings()
{
    if (arenaRotor == null) return;
    
    try
    {
        // Only save if custom data is empty (first time detection)
        if (string.IsNullOrEmpty(arenaRotor.CustomData))
        {
            var settings = $"Torque:{arenaRotor.Torque}|" +
                          $"BrakingTorque:{arenaRotor.BrakingTorque}|" +
                          $"LowerLimit:{arenaRotor.LowerLimitDeg}|" +
                          $"UpperLimit:{arenaRotor.UpperLimitDeg}|" +
                          $"Velocity:{arenaRotor.TargetVelocityRPM}";
            
            arenaRotor.CustomData = settings;
            Echo($"Arena Rotor settings saved: {settings}");
        }
        else
        {
            Echo("Arena Rotor settings already saved in custom data");
        }
    }
    catch (Exception e)
    {
        Echo($"Error saving Arena Rotor settings: {e.Message}");
    }
}

void HandleStopTransferMode()
{
    Echo("Processing stop_transfer command...");
    transferModeOverride = false;
    
    if (arenaRotor != null)
    {
        try
        {
            Echo("Attempting to restore Arena Rotor settings...");
            RestoreArenaRotorSettings();
        }
        catch (Exception e)
        {
            Echo($"Error restoring arena rotor: {e.Message}");
        }
    }
    else
    {
        Echo("Arena rotor not found for stop transfer");
    }
    
    // Re-enable small grid gyros
    int gyrosEnabled = 0;
    foreach (var gyro in allSmallGridGyros)
    {
        if (gyro != null)
        {
            gyro.Enabled = true;
            gyrosEnabled++;
        }
    }
    Echo($"Re-enabled {gyrosEnabled} small grid gyros");
    
    // Re-enable all gatling guns
    int gunsEnabled = 0;
    foreach (var gun in gatlingGuns)
    {
        if (gun != null)
        {
            gun.Enabled = true;
            gunsEnabled++;
        }
    }
    Echo($"Re-enabled {gunsEnabled} gatling guns");
    
    Echo("Transfer mode stopped - all settings and systems restored");
}

void RestoreArenaRotorSettings()
{
    if (arenaRotor == null)
    {
        Echo("Arena rotor is null - cannot restore settings");
        return;
    }
    
    if (string.IsNullOrEmpty(arenaRotor.CustomData))
    {
        Echo("No saved Arena Rotor settings found in custom data");
        return;
    }
    
    Echo($"Restoring from custom data: {arenaRotor.CustomData}");
    
    try
    {
        var settings = arenaRotor.CustomData.Split('|');
        
        foreach (var setting in settings)
        {
            var parts = setting.Split(':');
            if (parts.Length == 2)
            {
                var key = parts[0];
                var value = parts[1];
                
                Echo($"Restoring {key} = {value}");
                
                switch (key)
                {
                    case "Torque":
                        arenaRotor.Torque = float.Parse(value);
                        Echo($"Set Torque to {arenaRotor.Torque}");
                        break;
                    case "BrakingTorque":
                        arenaRotor.BrakingTorque = float.Parse(value);
                        Echo($"Set BrakingTorque to {arenaRotor.BrakingTorque}");
                        break;
                    case "LowerLimit":
                        arenaRotor.LowerLimitDeg = float.Parse(value);
                        Echo($"Set LowerLimit to {arenaRotor.LowerLimitDeg}");
                        break;
                    case "UpperLimit":
                        arenaRotor.UpperLimitDeg = float.Parse(value);
                        Echo($"Set UpperLimit to {arenaRotor.UpperLimitDeg}");
                        break;
                    case "Velocity":
                        arenaRotor.TargetVelocityRPM = float.Parse(value);
                        Echo($"Set Velocity to {arenaRotor.TargetVelocityRPM}");
                        break;
                    default:
                        Echo($"Unknown setting key: {key}");
                        break;
                }
            }
            else
            {
                Echo($"Invalid setting format: {setting}");
            }
        }
        
        Echo("Arena Rotor settings restoration completed");
    }
    catch (Exception e)
    {
        Echo($"Error parsing saved settings: {e.Message}");
        Echo($"Raw custom data: '{arenaRotor.CustomData}'");
    }
}

void UpdateStatus()
{
    Echo("=== ADAPTIVE TURRET SYSTEM ===");
    Echo($"Status: {(systemActive ? "ACTIVE" : "INACTIVE")}");
    if (systemActive)
    {
        Echo($"Mode: {GetCurrentMode()}");
        Echo($"Displays: {displays.Count} connected");
        Echo($"Guns: {gatlingGuns.Count} found");
        Echo($"Small Grid Gyros: {allSmallGridGyros.Count}");
        Echo($"Small Grid Thrusters: {allSmallGridThrusters.Count}");
    }
}
