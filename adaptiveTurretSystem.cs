//////////////////////////////////////////////////////////////
//  Adaptive Turret System v2.0                            //
//  - Intelligent multi-mode operation with prediction     //
//  - Advanced threat assessment and targeting             //
//  - Smart power management and combat analytics          //
//  - Performance optimized with caching and state machine //
//////////////////////////////////////////////////////////////

// ----------------- CONFIGURATION -----------------
const string COCKPIT_NAME = "Turret Control Seat";
const string BATTERY_NAME = "Turret Battery";
const string GANTRY_CRANE_ROTOR_NAME = "Gantry Crane Main Mount Rotor";
const string NECK_ROTOR_NAME = "Neck Rotor";
const string ARENA_ROTOR_NAME = "Arena Rotor";
const string GUN_DOORS_GROUP_NAME = "Gun Doors";

// Performance settings
const int BLOCK_UPDATE_INTERVAL = 60; // Update blocks every 60 ticks (1 second)
const int DISPLAY_UPDATE_INTERVAL = 10; // Update displays every 10 ticks
const int THREAT_SCAN_INTERVAL = 30; // Scan for threats every 30 ticks
const int ANALYTICS_SAVE_INTERVAL = 600; // Save analytics every 10 seconds

// Combat settings
const float MAX_TARGETING_RANGE = 800f; // Maximum engagement range
const float PREDICTION_TIME_AHEAD = 0.5f; // Predict 0.5 seconds ahead
const float MIN_THREAT_LEVEL = 0.3f; // Minimum threat level to engage
const int POSITION_HISTORY_SIZE = 10; // Number of positions to track for prediction

// Physics constants
const float GAME_TICK_RATE = 60f; // Space Engineers runs at 60 UPS
const float TRANSFER_MODE_TORQUE = 33600000f;
const float TRANSFER_MODE_VELOCITY = -2f;

// Visual settings
const float STATUS_FONT_SIZE = 0.5f;
const float STATUS_LINE_SPACING = 15f;
static readonly Vector2 STATUS_START_POSITION = new Vector2(10, 45);

// Colors
static readonly Color DANGER_COLOR = Color.Red;
static readonly Color SUCCESS_COLOR = Color.Green;
static readonly Color ALERT_COLOR = Color.Orange;
static readonly Color WARNING_COLOR = Color.Yellow;
static readonly Color STANDBY_COLOR = Color.LightBlue;
static readonly Color PINK_COLOR = Color.Pink;
static readonly Color GRAY_COLOR = Color.Gray;

// Block name arrays
readonly string[] GATLING_GUN_NAMES = {
    "Warfare Gatling Gun",
    "Warfare Gatling Gun 10", 
    "Warfare Gatling Gun 6",
    "Warfare Gatling Gun 9"
};

readonly string[] TURRET_LIGHT_NAMES = {
    "Turret Light Panel",
    "Turret Light Panel",
    "Turret Light Panel"
};

readonly string[] TURRET_HINGE_NAMES = {
    "Turret Guns Hinge R1",
    "Turret Guns Hinge R2",
    "Turret Guns Hinge L1",
    "Turret Guns Hinge L2"
};

readonly string[] TURRET_GYRO_NAMES = {
    "Turret Gyroscope 1",
    "Turret Gyroscope 2", 
    "Turret Gyroscope 3"
};

// ----------------- INTELLIGENT SYSTEMS -----------------

// Target prediction system
public class TargetPredictor
{
    private Queue<Vector3D> positionHistory = new Queue<Vector3D>();
    private Queue<DateTime> timeHistory = new Queue<DateTime>();
    private Vector3D lastVelocity = Vector3D.Zero;
    private Vector3D acceleration = Vector3D.Zero;
    
    public void AddPosition(Vector3D position)
    {
        positionHistory.Enqueue(position);
        timeHistory.Enqueue(DateTime.Now);
        
        if (positionHistory.Count > POSITION_HISTORY_SIZE)
        {
            positionHistory.Dequeue();
            timeHistory.Dequeue();
        }
        
        CalculateMotionVectors();
    }
    
    private void CalculateMotionVectors()
    {
        if (positionHistory.Count < 2) return;
        
        var positions = positionHistory.ToArray();
        var times = timeHistory.ToArray();
        
        // Calculate velocity
        Vector3D currentVelocity = (positions[positions.Length - 1] - positions[positions.Length - 2]) * GAME_TICK_RATE;
        
        // Calculate acceleration
        if (lastVelocity != Vector3D.Zero)
        {
            acceleration = (currentVelocity - lastVelocity) * GAME_TICK_RATE;
        }
        
        lastVelocity = currentVelocity;
    }
    
    public Vector3D PredictPosition(float timeAhead)
    {
        if (positionHistory.Count == 0) return Vector3D.Zero;
        
        var currentPos = positionHistory.Last();
        var predictedPos = currentPos + (lastVelocity * timeAhead) + (0.5 * acceleration * timeAhead * timeAhead);
        
        return predictedPos;
    }
    
    public Vector3D GetVelocity() => lastVelocity;
    public Vector3D GetAcceleration() => acceleration;
    public void Reset()
    {
        positionHistory.Clear();
        timeHistory.Clear();
        lastVelocity = Vector3D.Zero;
        acceleration = Vector3D.Zero;
    }
}

// Threat assessment system
public class ThreatAnalyzer
{
    public class ThreatInfo
    {
        public IMyCubeGrid Grid { get; set; }
        public float ThreatLevel { get; set; }
        public float Distance { get; set; }
        public Vector3D Position { get; set; }
        public Vector3D Velocity { get; set; }
        public int WeaponCount { get; set; }
        public float Size { get; set; }
        public DateTime LastUpdated { get; set; }
    }
    
    private Dictionary<long, ThreatInfo> threats = new Dictionary<long, ThreatInfo>();
    private Dictionary<long, TargetPredictor> predictors = new Dictionary<long, TargetPredictor>();
    
    public void UpdateThreat(IMyCubeGrid grid, Vector3D myPosition)
    {
        if (grid == null) return;
        
        var gridId = grid.EntityId;
        var position = grid.GetPosition();
        var distance = (float)(position - myPosition).Length();
        
        if (!threats.ContainsKey(gridId))
        {
            threats[gridId] = new ThreatInfo { Grid = grid };
            predictors[gridId] = new TargetPredictor();
        }
        
        var threat = threats[gridId];
        var predictor = predictors[gridId];
        
        predictor.AddPosition(position);
        
        // Update threat info
        threat.Position = position;
        threat.Distance = distance;
        threat.Velocity = predictor.GetVelocity();
        threat.Size = grid.LocalAABB.Size.Length();
        threat.LastUpdated = DateTime.Now;
        
        // Calculate threat level (0-1)
        float distanceFactor = Math.Max(0, 1 - (distance / MAX_TARGETING_RANGE));
        float sizeFactor = Math.Min(1, threat.Size / 100f);
        float velocityFactor = Math.Min(1, (float)threat.Velocity.Length() / 100f);
        
        threat.ThreatLevel = (distanceFactor * 0.5f) + (sizeFactor * 0.3f) + (velocityFactor * 0.2f);
    }
    
    public ThreatInfo GetHighestThreat()
    {
        CleanupOldThreats();
        return threats.Values.OrderByDescending(t => t.ThreatLevel).FirstOrDefault();
    }
    
    public List<ThreatInfo> GetThreatsInRange(float range)
    {
        CleanupOldThreats();
        return threats.Values.Where(t => t.Distance <= range && t.ThreatLevel >= MIN_THREAT_LEVEL)
                             .OrderByDescending(t => t.ThreatLevel)
                             .ToList();
    }
    
    public Vector3D PredictTargetPosition(long gridId, float timeAhead)
    {
        if (predictors.ContainsKey(gridId))
        {
            return predictors[gridId].PredictPosition(timeAhead);
        }
        return Vector3D.Zero;
    }
    
    private void CleanupOldThreats()
    {
        var cutoffTime = DateTime.Now.AddSeconds(-5);
        var oldThreats = threats.Where(kvp => kvp.Value.LastUpdated < cutoffTime).Select(kvp => kvp.Key).ToList();
        
        foreach (var id in oldThreats)
        {
            threats.Remove(id);
            predictors.Remove(id);
        }
    }
    
    public void Reset()
    {
        threats.Clear();
        predictors.Clear();
    }
}

// Combat analytics system
public class CombatAnalytics
{
    public int ShotsFired { get; private set; }
    public int TargetsEngaged { get; private set; }
    public int ModeChanges { get; private set; }
    public float TotalDamageDealt { get; private set; }
    public float AverageEngagementRange { get; private set; }
    public Dictionary<string, int> ModeUsage { get; private set; } = new Dictionary<string, int>();
    public DateTime SessionStart { get; private set; }
    
    private List<float> engagementRanges = new List<float>();
    private Program program;
    
    public CombatAnalytics(Program p)
    {
        program = p;
        SessionStart = DateTime.Now;
        LoadFromCustomData();
    }
    
    public void RecordShot() => ShotsFired++;
    public void RecordTargetEngaged() => TargetsEngaged++;
    public void RecordModeChange(string mode)
    {
        ModeChanges++;
        if (!ModeUsage.ContainsKey(mode))
            ModeUsage[mode] = 0;
        ModeUsage[mode]++;
    }
    
    public void RecordEngagement(float range)
    {
        engagementRanges.Add(range);
        if (engagementRanges.Count > 100) // Keep last 100 engagements
            engagementRanges.RemoveAt(0);
        
        AverageEngagementRange = engagementRanges.Count > 0 ? engagementRanges.Average() : 0;
    }
    
    public void SaveToCustomData()
    {
        var data = $"Shots:{ShotsFired}|Targets:{TargetsEngaged}|Modes:{ModeChanges}|AvgRange:{AverageEngagementRange:F1}";
        program.Me.CustomData = data;
    }
    
    private void LoadFromCustomData()
    {
        try
        {
            var data = program.Me.CustomData;
            if (string.IsNullOrEmpty(data)) return;
            
            var parts = data.Split('|');
            foreach (var part in parts)
            {
                var kvp = part.Split(':');
                if (kvp.Length != 2) continue;
                
                switch (kvp[0])
                {
                    case "Shots":
                        int.TryParse(kvp[1], out int shots);
                        ShotsFired = shots;
                        break;
                    case "Targets":
                        int.TryParse(kvp[1], out int targets);
                        TargetsEngaged = targets;
                        break;
                    case "Modes":
                        int.TryParse(kvp[1], out int modes);
                        ModeChanges = modes;
                        break;
                    case "AvgRange":
                        float.TryParse(kvp[1], out float range);
                        AverageEngagementRange = range;
                        break;
                }
            }
        }
        catch { /* Ignore parse errors */ }
    }
}

// Smart power management
public class PowerManager
{
    private IMyBatteryBlock battery;
    private float lastBatteryLevel = 1.0f;
    private bool lowPowerMode = false;
    private Program program;
    
    public PowerManager(Program p)
    {
        program = p;
    }
    
    public void UpdateBattery(IMyBatteryBlock bat)
    {
        battery = bat;
    }
    
    public bool IsLowPower() => lowPowerMode;
    
    public void ManagePower(List<IMyUserControllableGun> guns, List<IMyLightingBlock> lights, List<IMyGyro> gyros)
    {
        if (battery == null) return;
        
        float currentLevel = battery.CurrentStoredPower / battery.MaxStoredPower;
        float drainRate = lastBatteryLevel - currentLevel;
        lastBatteryLevel = currentLevel;
        
        // Enter low power mode below 20%
        if (currentLevel < 0.2f && !lowPowerMode)
        {
            lowPowerMode = true;
            program.Echo("WARNING: Low power mode activated");
            
            // Disable non-critical systems
            foreach (var light in lights)
            {
                if (light != null) light.Enabled = false;
            }
        }
        else if (currentLevel > 0.4f && lowPowerMode)
        {
            lowPowerMode = false;
            program.Echo("Power restored - normal operation resumed");
            
            // Re-enable systems
            foreach (var light in lights)
            {
                if (light != null) light.Enabled = true;
            }
        }
        
        // Predictive charging - if high drain rate, pre-charge weapons
        if (drainRate > 0.01f && currentLevel < 0.5f)
        {
            battery.ChargeMode = ChargeMode.Recharge;
        }
        else if (currentLevel > 0.9f)
        {
            battery.ChargeMode = ChargeMode.Auto;
        }
    }
}

// ----------------- MODE MANAGEMENT -----------------

public interface ITurretMode
{
    string Name { get; }
    void Enter();
    void Update();
    void Exit();
    void Draw(MySpriteDrawFrame frame, Vector2 center);
}

public class TransferMode : ITurretMode
{
    public string Name => "TRANSFER MODE";
    private Program program;
    
    public TransferMode(Program p) { program = p; }
    
    public void Enter()
    {
        program.ConfigureForTransfer();
    }
    
    public void Update() { }
    
    public void Exit()
    {
        program.RestoreFromTransfer();
    }
    
    public void Draw(MySpriteDrawFrame frame, Vector2 center)
    {
        var modeText = MySprite.CreateText(Name, "Monospace", GRAY_COLOR, 0.8f);
        modeText.Position = center + new Vector2(0, -30);
        modeText.Alignment = TextAlignment.CENTER;
        frame.Add(modeText);
    }
}

public class HovercatMode : ITurretMode
{
    public string Name => "HOVERCAT MODE";
    private Program program;
    
    public HovercatMode(Program p) { program = p; }
    
    public void Enter()
    {
        program.SetGunsEnabled(false);
        program.SetGyrosEnabled(true);
    }
    
    public void Update()
    {
        program.UpdatePhysicsTracking();
    }
    
    public void Exit() { }
    
    public void Draw(MySpriteDrawFrame frame, Vector2 center)
    {
        var modeText = MySprite.CreateText(Name, "Monospace", PINK_COLOR, 0.8f);
        modeText.Position = center + new Vector2(0, -30);
        modeText.Alignment = TextAlignment.CENTER;
        frame.Add(modeText);
        
        var statusText = MySprite.CreateText("DEFENSIVE MODE", "Monospace", SUCCESS_COLOR, 0.6f);
        statusText.Position = center;
        statusText.Alignment = TextAlignment.CENTER;
        frame.Add(statusText);
    }
}

public class AssaultCatMode : ITurretMode
{
    public string Name => "ASSAULTCAT MODE";
    private Program program;
    
    public AssaultCatMode(Program p) { program = p; }
    
    public void Enter()
    {
        program.SetGunsEnabled(true);
        program.SetGyrosEnabled(true);
        program.analytics.RecordModeChange(Name);
    }
    
    public void Update()
    {
        program.UpdatePhysicsTracking();
        program.UpdateTargeting();
    }
    
    public void Exit() { }
    
    public void Draw(MySpriteDrawFrame frame, Vector2 center)
    {
        var modeText = MySprite.CreateText(Name, "Monospace", DANGER_COLOR, 0.8f);
        modeText.Position = center + new Vector2(0, -40);
        modeText.Alignment = TextAlignment.CENTER;
        frame.Add(modeText);
        
        var threat = program.threatAnalyzer.GetHighestThreat();
        string threatStatus = threat != null ? $"THREAT: {threat.ThreatLevel:F1} @ {threat.Distance:F0}m" : "NO THREATS";
        var threatColor = threat != null ? DANGER_COLOR : SUCCESS_COLOR;
        
        var threatText = MySprite.CreateText(threatStatus, "Monospace", threatColor, 0.6f);
        threatText.Position = center + new Vector2(0, -10);
        threatText.Alignment = TextAlignment.CENTER;
        frame.Add(threatText);
    }
}

public class TurretMode : ITurretMode
{
    public string Name => "TURRET MODE";
    private Program program;
    
    public TurretMode(Program p) { program = p; }
    
    public void Enter()
    {
        program.analytics.RecordModeChange(Name);
    }
    
    public void Update()
    {
        program.UpdateTargeting();
        program.TrackFiring();
    }
    
    public void Exit() { }
    
    public void Draw(MySpriteDrawFrame frame, Vector2 center)
    {
        var modeText = MySprite.CreateText(Name, "Monospace", DANGER_COLOR, 0.8f);
        modeText.Position = center + new Vector2(0, -30);
        modeText.Alignment = TextAlignment.CENTER;
        frame.Add(modeText);
        
        string subModeText = program.AreGunsFiring() ? "FIRING" : (program.AreGunsOnline() ? "ARMED" : "SAFE");
        Color subModeColor = program.AreGunsFiring() ? DANGER_COLOR : (program.AreGunsOnline() ? ALERT_COLOR : SUCCESS_COLOR);
        
        var subModeSprite = MySprite.CreateText(subModeText, "Monospace", subModeColor, 0.6f);
        subModeSprite.Position = center;
        subModeSprite.Alignment = TextAlignment.CENTER;
        frame.Add(subModeSprite);
        
        // Show analytics
        var analyticsText = $"Shots: {program.analytics.ShotsFired} | Targets: {program.analytics.TargetsEngaged}";
        var analyticsSprite = MySprite.CreateText(analyticsText, "Monospace", Color.White, 0.4f);
        analyticsSprite.Position = center + new Vector2(0, 30);
        analyticsSprite.Alignment = TextAlignment.CENTER;
        frame.Add(analyticsSprite);
    }
}

public class CraneMode : ITurretMode
{
    public string Name => "CRANE MODE";
    private Program program;
    
    public CraneMode(Program p) { program = p; }
    
    public void Enter()
    {
        program.SetGunsEnabled(false);
    }
    
    public void Update() { }
    
    public void Exit() { }
    
    public void Draw(MySpriteDrawFrame frame, Vector2 center)
    {
        var modeText = MySprite.CreateText(Name, "Monospace", WARNING_COLOR, 0.8f);
        modeText.Position = center + new Vector2(0, -30);
        modeText.Alignment = TextAlignment.CENTER;
        frame.Add(modeText);
        
        var statusText = MySprite.CreateText("SYSTEM READY", "Monospace", SUCCESS_COLOR, 0.6f);
        statusText.Position = center;
        statusText.Alignment = TextAlignment.CENTER;
        frame.Add(statusText);
    }
}

// ----------------- MAIN PROGRAM -----------------

// System state
bool systemActive = false;
bool scriptInitialized = false;
int tickCounter = 0;
int lastBlockUpdate = 0;
int lastDisplayUpdate = 0;
int lastThreatScan = 0;
int lastAnalyticsSave = 0;
bool lastFiringState = false;
DateTime lastStateChange = DateTime.Now;

// Intelligent systems
ThreatAnalyzer threatAnalyzer;
CombatAnalytics analytics;
PowerManager powerManager;
ITurretMode currentMode;
Dictionary<string, ITurretMode> modes;

// Block cache
class BlockCache
{
    public IMyCockpit Cockpit;
    public IMyBatteryBlock Battery;
    public IMyMotorStator GantryCraneRotor;
    public IMyMotorStator NeckRotor;
    public IMyMotorStator ArenaRotor;
    public List<IMyUserControllableGun> GatlingGuns = new List<IMyUserControllableGun>();
    public List<IMyLightingBlock> TurretLights = new List<IMyLightingBlock>();
    public List<IMyMotorStator> TurretHinges = new List<IMyMotorStator>();
    public List<IMyGyro> TurretGyros = new List<IMyGyro>();
    public List<IMyGyro> AllSmallGridGyros = new List<IMyGyro>();
    public List<IMyThrust> AllSmallGridThrusters = new List<IMyThrust>();
    public IMyBlockGroup GunDoorsGroup;
    public Dictionary<string, IMyTextSurface> Displays = new Dictionary<string, IMyTextSurface>();
    public bool NeedsUpdate = true;
}

BlockCache blockCache = new BlockCache();

// Physics tracking
Vector3D lastPosition = Vector3D.Zero;
Vector3D velocity = Vector3D.Zero;
bool physicsInitialized = false;

// Saved settings
Dictionary<string, string> savedSettings = new Dictionary<string, string>();

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    
    // Initialize intelligent systems
    threatAnalyzer = new ThreatAnalyzer();
    analytics = new CombatAnalytics(this);
    powerManager = new PowerManager(this);
    
    // Initialize modes
    modes = new Dictionary<string, ITurretMode>
    {
        ["TRANSFER"] = new TransferMode(this),
        ["HOVERCAT"] = new HovercatMode(this),
        ["ASSAULTCAT"] = new AssaultCatMode(this),
        ["TURRET"] = new TurretMode(this),
        ["CRANE"] = new CraneMode(this)
    };
    
    Echo("Adaptive Turret System v2.0 initialized");
}

void Main(string argument, UpdateType updateSource)
{
    try
    {
        tickCounter++;
        
        // Handle commands
        if (!string.IsNullOrEmpty(argument))
        {
            ProcessCommand(argument.ToLower());
            return;
        }
        
        if (!systemActive)
        {
            if (tickCounter % DISPLAY_UPDATE_INTERVAL == 0)
            {
                ClearAllDisplays();
            }
            Echo("System inactive");
            return;
        }
        
        // Periodic updates
        if (tickCounter - lastBlockUpdate >= BLOCK_UPDATE_INTERVAL)
        {
            UpdateBlockReferences();
            lastBlockUpdate = tickCounter;
        }
        
        if (tickCounter - lastThreatScan >= THREAT_SCAN_INTERVAL)
        {
            ScanForThreats();
            lastThreatScan = tickCounter;
        }
        
        if (tickCounter - lastAnalyticsSave >= ANALYTICS_SAVE_INTERVAL)
        {
            analytics.SaveToCustomData();
            lastAnalyticsSave = tickCounter;
        }
        
        // Mode management
        UpdateMode();
        
        if (currentMode != null)
        {
            currentMode.Update();
        }
        
        // Power management
        powerManager.ManagePower(blockCache.GatlingGuns, blockCache.TurretLights, blockCache.AllSmallGridGyros);
        
        // Display updates
        if (tickCounter - lastDisplayUpdate >= DISPLAY_UPDATE_INTERVAL)
        {
            UpdateDisplays();
            lastDisplayUpdate = tickCounter;
        }
        
        UpdateStatus();
    }
    catch (Exception e)
    {
        Echo($"ERROR: {e.Message}");
        Echo($"Stack: {e.StackTrace}");
    }
}

void ProcessCommand(string command)
{
    switch (command)
    {
        case "on":
            systemActive = true;
            blockCache.NeedsUpdate = true;
            Echo("System activated");
            break;
            
        case "off":
            systemActive = false;
            if (currentMode != null)
            {
                currentMode.Exit();
                currentMode = null;
            }
            ClearAllDisplays();
            Echo("System deactivated");
            break;
            
        case "transfer":
            ChangeMode("TRANSFER");
            break;
            
        case "stop_transfer":
            if (currentMode?.Name == "TRANSFER MODE")
            {
                currentMode.Exit();
                UpdateMode();
            }
            break;
            
        case "reset":
            threatAnalyzer.Reset();
            analytics = new CombatAnalytics(this);
            Echo("Systems reset");
            break;
            
        default:
            Echo($"Unknown command: {command}");
            break;
    }
}

void UpdateMode()
{
    string detectedMode = DetectMode();
    
    if (currentMode == null || currentMode.Name != detectedMode)
    {
        ChangeMode(detectedMode);
    }
}

string DetectMode()
{
    if (blockCache.GantryCraneRotor?.IsAttached == true)
        return "CRANE";
    
    if (blockCache.ArenaRotor?.IsAttached == true)
        return "TURRET";
    
    if (blockCache.NeckRotor?.IsAttached == true)
    {
        bool allGunsOn = blockCache.GatlingGuns.Count > 0 && 
                        blockCache.GatlingGuns.All(gun => gun?.Enabled == true);
        return allGunsOn ? "ASSAULTCAT" : "HOVERCAT";
    }
    
    return "TRANSFER";
}

void ChangeMode(string modeName)
{
    if (currentMode != null)
    {
        currentMode.Exit();
    }
    
    if (modes.ContainsKey(modeName))
    {
        currentMode = modes[modeName];
        currentMode.Enter();
        analytics.RecordModeChange(modeName);
        lastStateChange = DateTime.Now;
        Echo($"Mode changed to: {modeName}");
    }
}

void UpdateBlockReferences()
{
    if (!blockCache.NeedsUpdate && tickCounter > 60) return;
    
    try
    {
        // Update cockpit and displays
        if (blockCache.Cockpit == null)
        {
            blockCache.Cockpit = GridTerminalSystem.GetBlockWithName(COCKPIT_NAME) as IMyCockpit;
            if (blockCache.Cockpit != null)
            {
                InitializeDisplays();
            }
        }
        
        // Update critical blocks
        blockCache.Battery = GridTerminalSystem.GetBlockWithName(BATTERY_NAME) as IMyBatteryBlock;
        blockCache.GantryCraneRotor = GridTerminalSystem.GetBlockWithName(GANTRY_CRANE_ROTOR_NAME) as IMyMotorStator;
        blockCache.NeckRotor = GridTerminalSystem.GetBlockWithName(NECK_ROTOR_NAME) as IMyMotorStator;
        blockCache.ArenaRotor = GridTerminalSystem.GetBlockWithName(ARENA_ROTOR_NAME) as IMyMotorStator;
        
        // Save Arena Rotor settings when first detected
        if (blockCache.ArenaRotor?.IsAttached == true && !savedSettings.ContainsKey("ArenaRotor"))
        {
            SaveRotorSettings(blockCache.ArenaRotor, "ArenaRotor");
        }
        
        // Update weapon systems
        UpdateWeaponSystems();
        
        // Update support systems
        UpdateSupportSystems();
        
        // Update power manager
        powerManager.UpdateBattery(blockCache.Battery);
        
        blockCache.NeedsUpdate = false;
        
        Echo($"Blocks updated: {blockCache.GatlingGuns.Count} guns, {blockCache.AllSmallGridGyros.Count} gyros");
    }
    catch (Exception e)
    {
        Echo($"Block update error: {e.Message}");
    }
}

void UpdateWeaponSystems()
{
    blockCache.GatlingGuns.Clear();
    foreach (var gunName in GATLING_GUN_NAMES)
    {
        var gun = GridTerminalSystem.GetBlockWithName(gunName) as IMyUserControllableGun;
        if (gun != null)
            blockCache.GatlingGuns.Add(gun);
    }
    
    blockCache.TurretHinges.Clear();
    foreach (var hingeName in TURRET_HINGE_NAMES)
    {
        var hinge = GridTerminalSystem.GetBlockWithName(hingeName) as IMyMotorStator;
        if (hinge != null)
            blockCache.TurretHinges.Add(hinge);
    }
    
    blockCache.GunDoorsGroup = GridTerminalSystem.GetBlockGroupWithName(GUN_DOORS_GROUP_NAME);
}

void UpdateSupportSystems()
{
    blockCache.TurretLights.Clear();
    foreach (var lightName in TURRET_LIGHT_NAMES)
    {
        var light = GridTerminalSystem.GetBlockWithName(lightName) as IMyLightingBlock;
        if (light != null)
            blockCache.TurretLights.Add(light);
    }
    
    blockCache.TurretGyros.Clear();
    foreach (var gyroName in TURRET_GYRO_NAMES)
    {
        var gyro = GridTerminalSystem.GetBlockWithName(gyroName) as IMyGyro;
        if (gyro != null)
            blockCache.TurretGyros.Add(gyro);
    }
    
    // Update all small grid components
    var allGyros = new List<IMyGyro>();
    var allThrusters = new List<IMyThrust>();
    
    GridTerminalSystem.GetBlocksOfType(allGyros);
    GridTerminalSystem.GetBlocksOfType(allThrusters);
    
    blockCache.AllSmallGridGyros = allGyros.Where(g => g.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Small).ToList();
    blockCache.AllSmallGridThrusters = allThrusters.Where(t => t.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Small).ToList();
}

void InitializeDisplays()
{
    if (blockCache.Cockpit == null) return;
    
    try
    {
        blockCache.Displays.Clear();
        blockCache.Displays["TopLeft"] = blockCache.Cockpit.GetSurface(0);
        blockCache.Displays["TopCenter"] = blockCache.Cockpit.GetSurface(1);
        blockCache.Displays["TopRight"] = blockCache.Cockpit.GetSurface(2);
        
        foreach (var display in blockCache.Displays.Values)
        {
            display.ContentType = ContentType.SCRIPT;
            display.Script = "";
        }
        
        Echo($"Initialized {blockCache.Displays.Count} displays");
    }
    catch (Exception e)
    {
        Echo($"Display init error: {e.Message}");
    }
}

void ScanForThreats()
{
    if (blockCache.Cockpit == null) return;
    
    var myPosition = blockCache.Cockpit.GetPosition();
    var nearbyGrids = new List<IMyCubeGrid>();
    
    // This is a simplified threat scan - in real implementation would need more sophisticated detection
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(null, block =>
    {
        if (block.CubeGrid != Me.CubeGrid && !nearbyGrids.Contains(block.CubeGrid))
        {
            nearbyGrids.Add(block.CubeGrid);
        }
        return false;
    });
    
    foreach (var grid in nearbyGrids)
    {
        threatAnalyzer.UpdateThreat(grid, myPosition);
    }
}

public void UpdatePhysicsTracking()
{
    if (blockCache.Cockpit == null) return;
    
    var currentPosition = blockCache.Cockpit.GetPosition();
    
    if (!physicsInitialized)
    {
        lastPosition = currentPosition;
        physicsInitialized = true;
        return;
    }
    
    velocity = (currentPosition - lastPosition) * GAME_TICK_RATE;
    lastPosition = currentPosition;
}

public void UpdateTargeting()
{
    var threat = threatAnalyzer.GetHighestThreat();
    if (threat == null || threat.ThreatLevel < MIN_THREAT_LEVEL) return;
    
    // Predict target position
    var predictedPos = threatAnalyzer.PredictTargetPosition(threat.Grid.EntityId, PREDICTION_TIME_AHEAD);
    
    // In a real implementation, would calculate firing solution here
    // This would involve calculating lead angles based on projectile velocity
    
    analytics.RecordEngagement(threat.Distance);
}

public void TrackFiring()
{
    bool currentlyFiring = AreGunsFiring();
    
    if (currentlyFiring && !lastFiringState)
    {
        analytics.RecordTargetEngaged();
    }
    
    if (currentlyFiring)
    {
        analytics.RecordShot();
    }
    
    lastFiringState = currentlyFiring;
}

void UpdateDisplays()
{
    var currentTime = DateTime.Now.ToString("HH:mm:ss");
    
    if (blockCache.Displays.ContainsKey("TopLeft"))
    {
        DrawSystemStatus(blockCache.Displays["TopLeft"], currentTime);
    }
    
    if (blockCache.Displays.ContainsKey("TopCenter") && currentMode != null)
    {
        using (var frame = blockCache.Displays["TopCenter"].DrawFrame())
        {
            var center = blockCache.Displays["TopCenter"].SurfaceSize * 0.5f;
            currentMode.Draw(frame, center);
            
            // Add power warning if needed
            if (powerManager.IsLowPower())
            {
                var warningText = MySprite.CreateText("⚠ LOW POWER ⚠", "Monospace", DANGER_COLOR, 0.5f);
                warningText.Position = center + new Vector2(0, 60);
                warningText.Alignment = TextAlignment.CENTER;
                frame.Add(warningText);
            }
        }
    }
    
    if (blockCache.Displays.ContainsKey("TopRight"))
    {
        DrawAnalytics(blockCache.Displays["TopRight"], currentTime);
    }
}

void DrawSystemStatus(IMyTextSurface surface, string currentTime)
{
    using (var frame = surface.DrawFrame())
    {
        var viewport = surface.TextureSize;
        var pos = STATUS_START_POSITION;
        
        // Title
        AddText(frame, "SYSTEM STATUS v2.0", pos, DANGER_COLOR, STATUS_FONT_SIZE * 1.2f);
        pos.Y += STATUS_LINE_SPACING * 2;
        
        // Status
        bool batteryOn = blockCache.Battery?.Enabled == true;
        AddStatusLine(frame, "Status:", systemActive ? "ACTIVE" : "INACTIVE", 
                     systemActive ? SUCCESS_COLOR : DANGER_COLOR, pos, viewport.X);
        pos.Y += STATUS_LINE_SPACING;
        
        // Power
        string powerText = batteryOn ? $"{(blockCache.Battery.CurrentStoredPower / blockCache.Battery.MaxStoredPower * 100):F0}%" : "OFFLINE";
        Color powerColor = batteryOn ? (powerManager.IsLowPower() ? WARNING_COLOR : SUCCESS_COLOR) : DANGER_COLOR;
        AddStatusLine(frame, "Power:", powerText, powerColor, pos, viewport.X);
        pos.Y += STATUS_LINE_SPACING;
        
        // Mode
        string modeText = currentMode?.Name ?? "NONE";
        AddStatusLine(frame, "Mode:", modeText, Color.White, pos, viewport.X);
        pos.Y += STATUS_LINE_SPACING;
        
        // Threat Level
        var threat = threatAnalyzer.GetHighestThreat();
        string threatText = threat != null ? $"{threat.ThreatLevel:F2}" : "CLEAR";
        Color threatColor = threat != null ? DANGER_COLOR : SUCCESS_COLOR;
        AddStatusLine(frame, "Threat:", threatText, threatColor, pos, viewport.X);
        pos.Y += STATUS_LINE_SPACING * 2;
        
        // Time
        AddStatusLine(frame, "Time:", currentTime, Color.White, pos, viewport.X);
    }
}

void DrawAnalytics(IMyTextSurface surface, string currentTime)
{
    using (var frame = surface.DrawFrame())
    {
        var viewport = surface.TextureSize;
        var pos = STATUS_START_POSITION;
        
        // Title
        AddText(frame, "COMBAT ANALYTICS", pos, ALERT_COLOR, STATUS_FONT_SIZE * 1.2f);
        pos.Y += STATUS_LINE_SPACING * 2;
        
        // Session time
        var sessionTime = DateTime.Now - analytics.SessionStart;
        AddStatusLine(frame, "Session:", $"{sessionTime.TotalMinutes:F0}m", Color.White, pos, viewport.X);
        pos.Y += STATUS_LINE_SPACING;
        
        // Shots fired
        AddStatusLine(frame, "Shots:", analytics.ShotsFired.ToString(), Color.White, pos, viewport.X);
        pos.Y += STATUS_LINE_SPACING;
        
        // Targets engaged
        AddStatusLine(frame, "Targets:", analytics.TargetsEngaged.ToString(), Color.White, pos, viewport.X);
        pos.Y += STATUS_LINE_SPACING;
        
        // Average range
        AddStatusLine(frame, "Avg Range:", $"{analytics.AverageEngagementRange:F0}m", Color.White, pos, viewport.X);
        pos.Y += STATUS_LINE_SPACING;
        
        // Efficiency
        float efficiency = analytics.ShotsFired > 0 ? (float)analytics.TargetsEngaged / analytics.ShotsFired * 100 : 0;
        Color effColor = efficiency > 50 ? SUCCESS_COLOR : (efficiency > 25 ? WARNING_COLOR : DANGER_COLOR);
        AddStatusLine(frame, "Efficiency:", $"{efficiency:F1}%", effColor, pos, viewport.X);
        pos.Y += STATUS_LINE_SPACING * 2;
        
        // Update time
        AddStatusLine(frame, "Updated:", currentTime, Color.White, pos, viewport.X);
    }
}

void AddText(MySpriteDrawFrame frame, string text, Vector2 position, Color color, float fontSize)
{
    var sprite = MySprite.CreateText(text, "Monospace", color, fontSize);
    sprite.Position = position;
    sprite.Alignment = TextAlignment.LEFT;
    frame.Add(sprite);
}

void AddStatusLine(MySpriteDrawFrame frame, string label, string value, Color valueColor, Vector2 position, float rightEdge)
{
    AddText(frame, label, position, Color.White, STATUS_FONT_SIZE);
    
    var valueSprite = MySprite.CreateText(value, "Monospace", valueColor, STATUS_FONT_SIZE);
    valueSprite.Position = new Vector2(rightEdge - 10, position.Y);
    valueSprite.Alignment = TextAlignment.RIGHT;
    frame.Add(valueSprite);
}

void ClearAllDisplays()
{
    foreach (var display in blockCache.Displays.Values)
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

// Utility functions
public bool AreGunsOnline() => blockCache.GatlingGuns.Any(gun => gun?.Enabled == true);
public bool AreGunsFiring() => blockCache.GatlingGuns.Any(gun => gun?.IsShooting == true);

public void SetGunsEnabled(bool enabled)
{
    foreach (var gun in blockCache.GatlingGuns)
    {
        if (gun != null) gun.Enabled = enabled;
    }
}

public void SetGyrosEnabled(bool enabled)
{
    foreach (var gyro in blockCache.AllSmallGridGyros)
    {
        if (gyro != null) gyro.Enabled = enabled;
    }
}

public void ConfigureForTransfer()
{
    if (blockCache.ArenaRotor != null)
    {
        try
        {
            blockCache.ArenaRotor.Torque = TRANSFER_MODE_TORQUE;
            blockCache.ArenaRotor.BrakingTorque = 0f;
            blockCache.ArenaRotor.LowerLimitDeg = 0f;
            blockCache.ArenaRotor.UpperLimitDeg = 180f;
            blockCache.ArenaRotor.TargetVelocityRPM = TRANSFER_MODE_VELOCITY;
        }
        catch { }
    }
    
    SetGyrosEnabled(false);
    SetGunsEnabled(false);
}

public void RestoreFromTransfer()
{
    if (blockCache.ArenaRotor != null && savedSettings.ContainsKey("ArenaRotor"))
    {
        RestoreRotorSettings(blockCache.ArenaRotor, "ArenaRotor");
    }
    
    SetGyrosEnabled(true);
    SetGunsEnabled(true);
}

void SaveRotorSettings(IMyMotorStator rotor, string key)
{
    if (rotor == null) return;
    
    var settings = $"T:{rotor.Torque}|BT:{rotor.BrakingTorque}|LL:{rotor.LowerLimitDeg}|UL:{rotor.UpperLimitDeg}|V:{rotor.TargetVelocityRPM}";
    savedSettings[key] = settings;
    rotor.CustomData = settings;
}

void RestoreRotorSettings(IMyMotorStator rotor, string key)
{
    if (rotor == null || !savedSettings.ContainsKey(key)) return;
    
    try
    {
        var settings = savedSettings[key].Split('|');
        foreach (var setting in settings)
        {
            var parts = setting.Split(':');
            if (parts.Length != 2) continue;
            
            float value;
            if (!float.TryParse(parts[1], out value)) continue;
            
            switch (parts[0])
            {
                case "T": rotor.Torque = value; break;
                case "BT": rotor.BrakingTorque = value; break;
                case "LL": rotor.LowerLimitDeg = value; break;
                case "UL": rotor.UpperLimitDeg = value; break;
                case "V": rotor.TargetVelocityRPM = value; break;
            }
        }
    }
    catch { }
}

void UpdateStatus()
{
    Echo("=== ADAPTIVE TURRET v2.0 ===");
    Echo($"Status: {(systemActive ? "ACTIVE" : "INACTIVE")}");
    if (systemActive)
    {
        Echo($"Mode: {currentMode?.Name ?? "NONE"}");
        Echo($"Power: {(blockCache.Battery != null ? $"{(blockCache.Battery.CurrentStoredPower / blockCache.Battery.MaxStoredPower * 100):F0}%" : "N/A")}");
        Echo($"Threats: {threatAnalyzer.GetThreatsInRange(MAX_TARGETING_RANGE).Count}");
        Echo($"Performance: {Runtime.CurrentInstructionCount}/{Runtime.MaxInstructionCount}");
    }
}