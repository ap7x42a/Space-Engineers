//////////////////////////////////////////////
// Hybrid Chaos Disruption Script v1.0        //
// Combines persistent state storage with     //
// a detailed thruster maneuver state machine   //
// for Space Engineers.                         //
//////////////////////////////////////////////

// ===== Adjustable Parameters =====

// Power cut parameters
double powerCutChance = 0.001;       // Chance each tick (0.0 - 1.0) to trigger a power cut.
double powerCutDuration = 7.5;     // Duration of power cut (seconds).

// ----- DIE Message Parameters -----
float dieFontSize = 5.0f;               // Adjustable font size.
Vector2 diePosition = new Vector2(1.0f, 1.0f);  // Normalized center position.

// Interaction probabilities
double doorToggleProbability = 0.02;
double ventToggleProbability = 0.02;
double soundTriggerProbability = 0.05;
double lightToggleProbability = 0.1;

// Interaction cooldowns (seconds)
double doorCooldown = 1.0;
double ventCooldown = 0.3;
double soundCooldown = 0.3;
double lightCooldown = 0.1;

// Thruster maneuver parameters
double phase1Speed = 0.0;          // m/s at which to begin maneuver.
double maxSpeed    = 75.0;         // m/s at which we stop thrust override.
double phase1Duration = 5.0;       // seconds: bank hard down/right.
double phase2Duration = 5.0;       // seconds: hold course.
double phase3Duration = 5.0;       // seconds: bank hard up/left.

// ===== Global Variables for Chunked Scanning During Power Cut =====
int pcScanIndex = 0;
List<IMyTerminalBlock> allBlocksForPC = new List<IMyTerminalBlock>();
bool pcScanningInProgress = false;
const int pcScanChunkSize = 100;  // Process 500 blocks per tick (adjust as needed)

// ===== Global Variables & Block Lists =====
bool isDisrupting = false;
double globalTime = 0.0;
double screenHijackStartTime = 0.0;
bool dieDisplayed = false;

// Global variables for chunked scanning.
int scanIndex = 0;
List<IMyTerminalBlock> allBlocksForScan = new List<IMyTerminalBlock>();
bool scanningInProgress = false;
const int scanChunkSize = 100;  // Process 500 blocks per tick (adjust as needed)

// Thrust update interval (in seconds) – adjust this value as needed.
double thrustUpdateInterval = 0.2;
double thrustUpdateTimer = 0.0;

// Block lists
List<IMyInteriorLight> lights = new List<IMyInteriorLight>();
List<IMyDoor> doors = new List<IMyDoor>();
List<IMyAirVent> vents = new List<IMyAirVent>();
List<IMySoundBlock> soundBlocks = new List<IMySoundBlock>();
List<IMyTextPanel> lcdPanels = new List<IMyTextPanel>();
List<IMyTextPanel> hijackedLCDs = new List<IMyTextPanel>();
List<IMyTerminalBlock> noChaosBlocks = new List<IMyTerminalBlock>();
List<IMyThrust> thrusters = new List<IMyThrust>();
List<IMyGyro> gyros = new List<IMyGyro>();
List<IMyRemoteControl> remotes = new List<IMyRemoteControl>();

// Door exclusion list (doors that should remain undisturbed)
List<string> doorExclusions = new List<string> {
    "Left Entrance Hatch Door",
    "Right Entrance Hatch Door",
    // Add more names if needed.
};

// List of screens to hijack
List<string> hijackScreenTags = new List<string> {
    "[HIJACK]"
};

// ----- Power Cut State -----
bool powerCutActive = false;
double powerCutTimer = 0.0;
List<IMyFunctionalBlock> powerCutToggledBlocks = new List<IMyFunctionalBlock>();
bool screenHijackActive = false;
List<IMyProgrammableBlock> hijackedPBs = new List<IMyProgrammableBlock>();

// ----- Thruster Maneuver State -----
bool executingManeuver = false;
int thrusterPhase = 0; // 0: accelerate, 1: bank down/right, 2: hold, 3: bank up/left, 4: final accelerate.
double thrusterPhaseTimer = 0.0;
double initialSpeedRecorded = 0.0;
Vector3D currentThrustDirection = Vector3D.Zero;
IMyShipController shipController = null;

// ----- Interaction Cooldowns -----
double lastDoorInteraction = 0.0;
double lastVentInteraction = 0.0;
double lastSoundInteraction = 0.0;
double lastLightInteraction = 0.0;

// ----- Persistent State Prefix -----
string origPrefix = "[HYBRID_ORIG]";

// Random number generator.
Random rnd = new Random();

/////////////////////////////////////////////////////
//                   Program()                    //
/////////////////////////////////////////////////////

public Program()
{
    // Set update frequency and gather blocks.
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    
    GridTerminalSystem.GetBlocksOfType(lights);
    GridTerminalSystem.GetBlocksOfType(doors);
    GridTerminalSystem.GetBlocksOfType(vents);
    GridTerminalSystem.GetBlocksOfType(soundBlocks);
    GridTerminalSystem.GetBlocksOfType(lcdPanels);
    GridTerminalSystem.GetBlocksOfType(thrusters);
    GridTerminalSystem.GetBlocksOfType(gyros);
    GridTerminalSystem.GetBlocksOfType(remotes);
    
    shipController = GridTerminalSystem.GetBlockWithName("Remote Control 2") as IMyShipController;
    if (shipController == null)
        Echo("Error: 'Remote Control 2' not found!");
    else
        Echo("'Remote Control 2' selected as ship controller.");
    
    // Retrieve the "NoChaos" group.
    IMyBlockGroup noChaosGroup = GridTerminalSystem.GetBlockGroupWithName("NoChaos");
    if (noChaosGroup != null)
    {
        noChaosGroup.GetBlocks(noChaosBlocks);
        Echo("NoChaos group found with " + noChaosBlocks.Count + " blocks.");
    }
    else
    {
        Echo("NoChaos group not found. All blocks will be affected.");
    }
    
    Echo("Hybrid Chaos Script initialized. Awaiting command: 'disrupt' or 'restore'.");
}

/////////////////////////////////////////////////////
//                    Main()                      //
/////////////////////////////////////////////////////
public void Main(string argument, UpdateType updateSource)
{
    try
    {
        double dt = Runtime.TimeSinceLastRun.TotalSeconds;
        globalTime += dt;
        
        // Process incoming commands.
        if (!string.IsNullOrEmpty(argument))
        {
            string arg = argument.ToLower();
            if (arg == "disrupt")
            {
                StartDisruption();
                return;
            }
            else if (arg == "restore")
            {
                RestoreAll();
                return;
            }
            else
            {
                Echo("Invalid command. Use 'disrupt' to start chaos or 'restore' to revert.");
                return;
            }
        }

        // If scanning is in progress, process a chunk.
        if (scanningInProgress)
        {
            ProcessScanChunk();
        }
        
        if (!isDisrupting)
            return;
        
        // ----- Power Cut Logic -----
        if (powerCutActive)
        {
            UpdatePowerCut(dt);
            return; // Skip further interactions during a power cut.
        }
        if (rnd.NextDouble() < powerCutChance)
        {
            TriggerPowerCut();
            return;
        }
        
        // ----- Random Interactions -----
        if (globalTime - lastDoorInteraction >= doorCooldown)
        {
            RandomDoorToggle();
            lastDoorInteraction = globalTime;
        }
        if (globalTime - lastVentInteraction >= ventCooldown)
        {
            RandomVentToggle();
            lastVentInteraction = globalTime;
        }
        if (globalTime - lastSoundInteraction >= soundCooldown)
        {
            RandomSoundTrigger();
            lastSoundInteraction = globalTime;
        }
        if (globalTime - lastLightInteraction >= lightCooldown)
        {
            DisruptLights();
            lastLightInteraction = globalTime;
        }
        
        // ----- Thruster Maneuver State Machine -----
        if (!executingManeuver)
        {
            double speed = (shipController != null) ? shipController.GetShipSpeed() : 0.0;
            if (shipController != null && speed < phase1Speed)
            {
                executingManeuver = true;
                thrusterPhase = 0;
                thrusterPhaseTimer = 0.0;
                initialSpeedRecorded = speed;
                currentThrustDirection = shipController.WorldMatrix.Forward;
                SetDynamicThrusters(currentThrustDirection, 1.0f);
                Echo("Thruster Maneuver: Accelerating to begin maneuver.");
            }
        }
        else
        {
            UpdateThrusterManeuver(dt);
        }
    }
    catch (Exception e)
    {
        Echo("Exception: " + e.ToString());
    }
}

/////////////////////////////////////////////////////
//              DISRUPTION FUNCTIONS            //
/////////////////////////////////////////////////////

void StartDisruption()
{
    BeginBlockScan();
    isDisrupting = true;
    globalTime = 0.0;
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    
    // Disable all other programmable blocks (except this one).
    List<IMyProgrammableBlock> pbList = new List<IMyProgrammableBlock>();
    GridTerminalSystem.GetBlocksOfType(pbList);
    foreach (var pb in pbList)
    {
        if (pb.EntityId != Me.EntityId)
            pb.ApplyAction("OnOff_Off");
    }
    Echo("Disruption initiated.");
}

void RestoreAll()
{
    // Restore settings from CustomData.
    List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocks(allBlocks);
    foreach (var block in allBlocks)
    {
        RestoreBlockSettings(block);
    }
    isDisrupting = false;
    executingManeuver = false;
    powerCutActive = false;
    Runtime.UpdateFrequency = UpdateFrequency.None;
    ClearGyroOverride();
    ResetThrusters();
    Echo("All systems restored to original settings.");
}

/////////////////////////////////////////////////////
//          PERSISTENT STATE FUNCTIONS          //
/////////////////////////////////////////////////////

// Call this each tick to process a chunk of blocks.
void ProcessScanChunk()
{
    int processed = 0;
    while (scanningInProgress && scanIndex < allBlocksForScan.Count && processed < scanChunkSize)
    {
        IMyTerminalBlock block = allBlocksForScan[scanIndex];
        if (IsBlockAllowedForScan(block))
        {
            SaveBlockSettings(block);
        }
        scanIndex++;
        processed++;
    }
    if (scanIndex >= allBlocksForScan.Count)
    {
        scanningInProgress = false;
        Echo("Block scan complete.");
    }
    else
    {
        Echo("Scanned " + scanIndex + " / " + allBlocksForScan.Count + " blocks.");
    }
}

// Call this to start a full scan (this gets all blocks into our list)
void BeginBlockScan()
{
    allBlocksForScan.Clear();
    GridTerminalSystem.GetBlocks(allBlocksForScan);
    scanIndex = 0;
    scanningInProgress = true;
    Echo("Starting block scan. Total blocks: " + allBlocksForScan.Count);
}

void ScanAndStoreSettings()
{
    List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocks(allBlocks);
    foreach (var block in allBlocks)
    {
        if (IsBlockAllowedForScan(block))
            SaveBlockSettings(block);
    }
    Echo("Original settings recorded in CustomData.");
}

bool IsBlockAllowedForScan(IMyTerminalBlock block)
{
    // Exclude rotors, pistons, merge blocks, hydrogen engines/tanks, button panels, and batteries.
    if (block is IMyMotorStator) return false;
    if (block is IMyPistonBase) return false;
    if (block is IMyShipMergeBlock) return false;
    if (block.BlockDefinition.SubtypeName.Contains("HydrogenEngine")) return false;
    if (block.BlockDefinition.SubtypeName.Contains("HydrogenTank")) return false;
    if (block is IMyButtonPanel) return false;
    if (block is IMyBatteryBlock) return false;
    return true;
}

void SaveBlockSettings(IMyTerminalBlock block)
{
    var func = block as IMyFunctionalBlock;
    if (func == null)
        return;
    
    StringBuilder sb = new StringBuilder();
    sb.Append(origPrefix);
    sb.Append("|Enabled=" + func.Enabled);
    
    var light = block as IMyInteriorLight;
    if (light != null)
    {
        sb.Append("|Color=" + light.Color.PackedValue);
        sb.Append("|Intensity=" + light.Intensity);
        sb.Append("|Radius=" + light.Radius);
    }
    
    var door = block as IMyDoor;
    if (door != null)
    {
        bool isOpen = (door.Status == DoorStatus.Open || door.Status == DoorStatus.Opening);
        sb.Append("|DoorOpen=" + isOpen);
    }
    
    var vent = block as IMyAirVent;
    if (vent != null)
    {
        sb.Append("|Depressurize=" + vent.Depressurize);
    }
    
    var thruster = block as IMyThrust;
    if (thruster != null)
    {
        sb.Append("|ThrustOverride=" + thruster.ThrustOverridePercentage);
    }
    
    var gyro = block as IMyGyro;
    if (gyro != null)
    {
        sb.Append("|GyroOverride=" + gyro.GyroOverride);
    }
    
    var lcd = block as IMyTextPanel;
    if (lcd != null)
    {
        sb.Append("|LCD_Text=" + lcd.GetText());
    }
    
    block.CustomData = sb.ToString();
}

// Exclude blocks
bool ShouldExcludeBlock(IMyTerminalBlock block)
{
    foreach (var excluded in noChaosBlocks)
    {
        if (block.EntityId == excluded.EntityId)
            return true;
    }
    return false;
}

void RestoreBlockSettings(IMyTerminalBlock block)
{
    if (string.IsNullOrEmpty(block.CustomData) || !block.CustomData.StartsWith(origPrefix))
        return;
    
    try
    {
        string data = block.CustomData.Substring(origPrefix.Length);
        string[] tokens = data.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        Dictionary<string, string> settings = new Dictionary<string, string>();
        foreach (string token in tokens)
        {
            if (token.Contains("="))
            {
                string[] pair = token.Split('=');
                if (pair.Length >= 2)
                    settings[pair[0]] = pair[1];
            }
        }
        
        var func = block as IMyFunctionalBlock;
        if (func != null && settings.ContainsKey("Enabled"))
        {
            bool enabled;
            bool.TryParse(settings["Enabled"], out enabled);
            if (enabled != func.Enabled)
                func.ApplyAction(enabled ? "OnOff_On" : "OnOff_Off");
        }
        
        var light = block as IMyInteriorLight;
        if (light != null)
        {
            if (settings.ContainsKey("Color"))
            {
                uint col;
                if (UInt32.TryParse(settings["Color"], out col))
                    light.Color = new Color(col);
            }
            if (settings.ContainsKey("Intensity"))
            {
                float inten;
                if (float.TryParse(settings["Intensity"], out inten))
                    light.Intensity = inten;
            }
            if (settings.ContainsKey("Radius"))
            {
                float rad;
                if (float.TryParse(settings["Radius"], out rad))
                    light.Radius = rad;
            }
        }
        
        var door = block as IMyDoor;
        if (door != null && settings.ContainsKey("DoorOpen"))
        {
            bool isOpen;
            bool.TryParse(settings["DoorOpen"], out isOpen);
            bool currentlyOpen = (door.Status == DoorStatus.Open || door.Status == DoorStatus.Opening);
            if (isOpen != currentlyOpen)
                door.ApplyAction(isOpen ? "Open_On" : "Open_Off");
        }
        
        var vent = block as IMyAirVent;
        if (vent != null && settings.ContainsKey("Depressurize"))
        {
            bool dep;
            bool.TryParse(settings["Depressurize"], out dep);
            vent.Depressurize = dep;
        }
        
        var thruster = block as IMyThrust;
        if (thruster != null && settings.ContainsKey("ThrustOverride"))
        {
            float to;
            if (float.TryParse(settings["ThrustOverride"], out to))
                thruster.ThrustOverridePercentage = to;
        }
        
        var gyro = block as IMyGyro;
        if (gyro != null && settings.ContainsKey("GyroOverride"))
        {
            bool gOverride;
            bool.TryParse(settings["GyroOverride"], out gOverride);
            gyro.GyroOverride = gOverride;
        }
        
        var lcd = block as IMyTextPanel;
        if (lcd != null && settings.ContainsKey("LCD_Text"))
        {
            lcd.WriteText(settings["LCD_Text"]);
        }
    }
    catch (Exception ex)
    {
        Echo("Error restoring " + block.CustomName + ": " + ex.Message);
    }
}

// Begin the power cut scan – load all blocks into a list.
void BeginPCScan()
{
    allBlocksForPC.Clear();
    GridTerminalSystem.GetBlocks(allBlocksForPC);
    pcScanIndex = 0;
    pcScanningInProgress = true;
    Echo("Starting power cut scan. Total blocks: " + allBlocksForPC.Count);
}

// Process a chunk of blocks during power cut scanning.
void ProcessPCScanChunk()
{
    int processed = 0;
    while (pcScanningInProgress && pcScanIndex < allBlocksForPC.Count && processed < pcScanChunkSize)
    {
        IMyTerminalBlock block = allBlocksForPC[pcScanIndex];
        // Skip our own programmable block and blocks in the NoChaos group.
        if (block.EntityId != Me.EntityId && !ShouldExcludeBlock(block))
        {
            IMyFunctionalBlock fb = block as IMyFunctionalBlock;
            if (fb != null && fb.Enabled)
            {
                fb.ApplyAction("OnOff_Off");  // Use the action to toggle off.
                powerCutToggledBlocks.Add(fb);
            }
        }
        pcScanIndex++;
        processed++;
    }
    if (pcScanIndex >= allBlocksForPC.Count)
    {
        pcScanningInProgress = false;
        Echo("Power cut scan complete.");
    }
    else
    {
        Echo("Power cut scanned " + pcScanIndex + " / " + allBlocksForPC.Count + " blocks.");
    }
}

/////////////////////////////////////////////////////
//         RANDOM INTERACTION FUNCTIONS         //
/////////////////////////////////////////////////////

void RandomDoorToggle()
{
    foreach (var door in doors)
    {
        if (ShouldExcludeBlock(door))
            continue;
        
        if (doorExclusions.Contains(door.CustomName))
            continue;
        
        if (rnd.NextDouble() < doorToggleProbability)
        {
            if (door.Status == DoorStatus.Open || door.Status == DoorStatus.Opening)
                door.CloseDoor();
            else
            {
                door.OpenDoor();
                door.Enabled = true;
            }
        }
    }
}

void RandomVentToggle()
{
    foreach (var vent in vents)
    {
        if (rnd.NextDouble() < ventToggleProbability)
            vent.Depressurize = !vent.Depressurize;
    }
}

void RandomSoundTrigger()
{
    foreach (var sound in soundBlocks)
    {
        if (rnd.NextDouble() < soundTriggerProbability)
            sound.ApplyAction("PlaySound");
    }
}

void DisruptLights()
{
    if (lights.Count == 0) return;
    int groupSize = Math.Max(1, lights.Count / 3);
    List<int> indices = new List<int>();
    for (int i = 0; i < lights.Count; i++)
        indices.Add(i);
    
    // Shuffle indices.
    for (int i = 0; i < indices.Count; i++)
    {
        int r = rnd.Next(i, indices.Count);
        int temp = indices[i];
        indices[i] = indices[r];
        indices[r] = temp;
    }
    
    for (int i = 0; i < groupSize; i++)
    {
        var light = lights[indices[i]];
        if (rnd.NextDouble() < lightToggleProbability)
        {
            light.Enabled = !light.Enabled;
            Color randomColor = new Color((byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256));
            light.SetValue("Color", randomColor);
        }
    }
}

/////////////////////////////////////////////////////
//           THRUSTER MANEUVER FUNCTIONS          //
/////////////////////////////////////////////////////

void UpdateThrusterManeuver(double dt)
{
    thrusterPhaseTimer += dt;
    thrustUpdateTimer += dt;
    double speed = (shipController != null) ? shipController.GetShipSpeed() : 0.0;
    
    // Update thrust override at the specified interval.
    if (thrustUpdateTimer >= thrustUpdateInterval)
    {
        // This function should apply maximum override to rear thrusters.
        SetDynamicThrusters(currentThrustDirection, 1.0f);
        thrustUpdateTimer = 0.0;
    }
    
    // Run the gyro/phase state machine.
    switch (thrusterPhase)
    {
        case 0:
            // Accelerate until reaching phase1Speed.
            if (speed >= phase1Speed)
            {
                thrusterPhase = 1;
                thrusterPhaseTimer = 0.0;
                // Bank down/right.
                SetGyroOverride(0.5236f, 0.5236f, 0f); // ~30°.
                Echo("Thruster Maneuver: Banking down/right for " + phase1Duration + " seconds.");
            }
            break;
        case 1:
            if (thrusterPhaseTimer >= phase1Duration)
            {
                thrusterPhase = 2;
                thrusterPhaseTimer = 0.0;
                ClearGyroOverride();
                Echo("Thruster Maneuver: Holding course for " + phase2Duration + " seconds.");
            }
            break;
        case 2:
            if (thrusterPhaseTimer >= phase2Duration)
            {
                thrusterPhase = 3;
                thrusterPhaseTimer = 0.0;
                // Bank up/left.
                SetGyroOverride(-0.5236f, -0.5236f, 0f);
                Echo("Thruster Maneuver: Banking up/left for " + phase3Duration + " seconds.");
            }
            break;
        case 3:
            if (thrusterPhaseTimer >= phase3Duration)
            {
                thrusterPhase = 4;
                thrusterPhaseTimer = 0.0;
                ClearGyroOverride();
                Echo("Thruster Maneuver: Accelerating until max speed reached.");
            }
            break;
        case 4:
            if (speed >= maxSpeed)
            {
                ResetThrusters();
                executingManeuver = false;
                thrusterPhase = 0;
                thrusterPhaseTimer = 0.0;
                Echo("Thruster Maneuver complete: Max speed reached.");
            }
            // No extra call to SetDynamicThrusters() here because it’s updated by the timer.
            break;
    }
}

void SetDynamicThrusters(Vector3D desiredDir, float overrideValue)
{
    List<IMyThrust> allThrusters = new List<IMyThrust>();
    GridTerminalSystem.GetBlocksOfType(allThrusters);
    if (shipController == null) return;
    Vector3D shipPos = shipController.GetPosition();
    Vector3D shipForward = shipController.WorldMatrix.Forward;
    foreach (var thr in allThrusters)
    {
        Vector3D diff = thr.GetPosition() - shipPos;
        if (Vector3D.Dot(diff, shipForward) < 0)
        {
            double alignment = Vector3D.Dot(thr.WorldMatrix.Backward, desiredDir);
            thr.ThrustOverridePercentage = (alignment > 0.8) ? Math.Abs(overrideValue) : 0f;
        }
        else
        {
            thr.ThrustOverridePercentage = 0f;
        }
    }
}

void ResetThrusters()
{
    List<IMyThrust> allThrusters = new List<IMyThrust>();
    GridTerminalSystem.GetBlocksOfType(allThrusters);
    foreach (var thr in allThrusters)
    {
        thr.ThrustOverridePercentage = 0f;
    }
}

void SetGyroOverride(float pitch, float yaw, float roll)
{
    List<IMyGyro> allGyros = new List<IMyGyro>();
    GridTerminalSystem.GetBlocksOfType(allGyros);
    foreach (var gyro in allGyros)
    {
        gyro.GyroOverride = true;
        gyro.SetValue("Pitch", pitch);
        gyro.SetValue("Yaw", yaw);
        gyro.SetValue("Roll", roll);
    }
}

void ClearGyroOverride()
{
    List<IMyGyro> allGyros = new List<IMyGyro>();
    GridTerminalSystem.GetBlocksOfType(allGyros);
    foreach (var gyro in allGyros)
    {
        gyro.GyroOverride = false;
    }
}

/////////////////////////////////////////////////////
//            POWER CUT FUNCTIONS               //
/////////////////////////////////////////////////////

void TriggerPowerCut()
{
    powerCutActive = true;
    powerCutTimer = 0.0;
    powerCutToggledBlocks.Clear();
    
    // Begin chunked scanning for power cut processing.
    BeginPCScan();
    
    HijackScreens();  // Re-enable the LCDs that you want to use.
    Echo("Power cut triggered.");
}

void UpdatePowerCut(double dt)
{
    // Process a chunk of blocks for power cut scanning.
    if (pcScanningInProgress)
    {
        ProcessPCScanChunk();
    }
    
    powerCutTimer += dt;
    // Maintain forced gyro override during power cut.
    SetGyroOverride(0.7f, -0.7f, -0.7f);
    
    // After 1 second from screen hijack, display "DIE" on each hijacked screen.
    if (!dieDisplayed && (globalTime - screenHijackStartTime) >= 1.0)
    {
        foreach (var lcd in hijackedLCDs)
        {
            DisplayDieOnLCD(lcd);
        }
        dieDisplayed = true;
        Echo("Displayed DIE on hijacked screens.");
    }
    
    // If we are within 1 second of ending the power cut, revert the LCDs.
    if (powerCutTimer >= (powerCutDuration - 1.0) && screenHijackActive)
    {
        ReleaseScreens();
    }
    
    if (powerCutTimer >= powerCutDuration)
    {
        EndPowerCut();
    }
}

void EndPowerCut()
{
    ReleaseScreens();
    foreach (var fb in powerCutToggledBlocks)
    {
        fb.ApplyAction("OnOff_On");  // Re-enable the block using the action.
    }
    powerCutToggledBlocks.Clear();
    powerCutActive = false;
    Echo("Power restored.");
}

void HijackScreens()
{
    hijackedLCDs.Clear();  // Clear any previously stored screens.
    
    // Iterate over all LCD panels and only select those whose names contain one of our hijack tags.
    foreach (var lcd in lcdPanels)
    {
        bool shouldHijack = false;
        foreach (var tag in hijackScreenTags)
        {
            if (lcd.CustomName.Contains(tag))
            {
                shouldHijack = true;
                break;
            }
        }
        if (!shouldHijack)
            continue;
        
        // Re-enable the LCD panel (it may have been turned off in TriggerPowerCut).
        // (This ensures we can display the DIE message.)
        lcd.ApplyAction("OnOff_On");
        lcd.WriteText("");  // Clear the screen.
        hijackedLCDs.Add(lcd);
    }
    screenHijackStartTime = globalTime;
    dieDisplayed = false;
    screenHijackActive = true;
    Echo("Screen hijack activated on " + hijackedLCDs.Count + " screens. Waiting 1 second before displaying DIE.");
}

void DisplayDieOnLCD(IMyTextPanel lcd)
{
    IMyTextSurface surface = lcd as IMyTextSurface;
    if (surface == null)
    {
        // Fallback: just write text.
        lcd.WriteText("DIE");
        return;
    }
    
    var frame = surface.DrawFrame();
    List<MySprite> sprites = new List<MySprite>();

    // Use the signature: CreateText(text, fontId, Color, scale, rotation)
    // This should pass Color.Red, then the font size (a float), then rotation.
    MySprite sprite = MySprite.CreateText("DIE", "Debug", Color.Red, dieFontSize, 0.0f);
    
    sprite.Position = diePosition;
    // If the alignment enum isn't available, you can comment this out.
    // Otherwise, if TextAlignment.CENTER is defined, use that.
    sprite.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER; 

    sprites.Add(sprite);
    frame.AddRange(sprites);
    frame.Dispose();
}

void ReleaseScreens()
{
    foreach (var lcd in hijackedLCDs)
    {
        // Restore the LCD's original text as stored in its CustomData.
        RestoreBlockSettings(lcd);
        // Optionally, also clear any drawn sprites.
    }
    hijackedLCDs.Clear();
    Echo("Screen hijack released and original text restored on hijacked screens.");
}

