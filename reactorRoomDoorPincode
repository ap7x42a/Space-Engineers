// =========================================
// Configurable Settings (Edit these for each door)
// =========================================
string correctCode = "432697"; // Change this for each door
string lcdName = "Left Reactor Pincode LCD"; // Name of the inset LCD panel

// Header Message Settings (Large Display on Programmable Block)
string headerMessage = "REACTOR SYSTEMS\n| RESTRICTED AREA |";
float headerFontSize = 1.0f;
Vector2 headerPosition = new Vector2(127, 50); // Adjust position
Color headerColor = Color.Red;

// Authorization Message Settings
string authMessage = "AUTHORIZATION CODE REQUIRED:";
float authFontSize = 0.6f;
Vector2 authPosition = new Vector2(127, 115); // Adjust position
Color authColor = Color.White;

// PIN Display Settings
float pinFontSize = 2.8f;
Vector2 pinPosition = new Vector2(127, 125);
Color pinColor = Color.White;

float failureFontSize = 1.1f;
Vector2 failurePosition = new Vector2(127, 130); // Adjust as needed

float authorizedFontSize = 1.1f;
Vector2 authorizedPosition = new Vector2(127, 125); // Adjust as needed

// =========================================
// LCD Detailed Info Settings (for "Left Reactor Pincode LCD")
// Two-column layout settings:
Vector2 lcdLabelStartPosition = new Vector2(20, 70);  // Starting position for labels
float labelFontSize = 1.0f;        // Font size for labels
float valueFontSize = 1.0f;        // Font size for variable values
float lcdLineSpacing = 30f;        // Vertical spacing between lines
float valueOffsetX = 310;          // X offset for variable column

// Reactor Containment Field Status (Field Projector)
bool reactorContainmentActive = true;

// Door Names
string leftDoorName = "Reactor Room Left Door";
string rightDoorName = "Reactor Room Right Door";

// =========================================
// Sound Blocks & Timer Block Names
// =========================================
string soundBlockName = "Reactor Pincode Failure Sound Block";
string successSoundBlockName = "Reactor Pincode Success Sound Block";
string timerBlockName = "Reactor Pincode Success Timer Block";

// =========================================
// Door Light Settings (for fading door lights)
// =========================================
// EXACT names of your truss lights:
string leftDoorLightName = "Left Reactor Door Truss Light";
string rightDoorLightName = "Right Reactor Door Truss Light";

// Common door light parameters
float doorLightRadius = 2.9f;
float doorLightFalloff = 3f;
float doorLightIntensity = 3f;

// Colors for locked/unlocked states
Color doorLockedColor = new Color(175, 50, 25);
Color doorUnlockedColor = new Color(175, 150, 100);

// Fade speed (fraction per update)
float doorLightFadeSpeed = 0.7f;

// =========================================
// Variables for System State
// =========================================
string enteredCode = "";
int failureCount = 0;

// =========================================
// Block References
// =========================================
IMyTextSurface largeDisplay;
IMyTextPanel lcdPanel;

IMySoundBlock failureSoundBlock;    // For pincode failure sound
IMySoundBlock successSoundBlock;    // For pincode success sound
IMyTimerBlock timerBlock;           // For pincode success trigger

// For door lights on small grid
IMyLightingBlock leftDoorLight;
IMyLightingBlock rightDoorLight;

// =========================================
// Initialization
// =========================================
public Program()
{
    IMyProgrammableBlock pb = Me;
    largeDisplay = pb.GetSurface(0); // Uses the first surface of the PB
    lcdPanel = GridTerminalSystem.GetBlockWithName(lcdName) as IMyTextPanel;
    
    // Grab our sound/timer blocks
    failureSoundBlock = GridTerminalSystem.GetBlockWithName(soundBlockName) as IMySoundBlock;
    successSoundBlock = GridTerminalSystem.GetBlockWithName(successSoundBlockName) as IMySoundBlock;
    timerBlock = GridTerminalSystem.GetBlockWithName(timerBlockName) as IMyTimerBlock;
    
    // Grab door lights by exact name, cast as IMyLightingBlock
    IMyTerminalBlock leftBlock = GridTerminalSystem.GetBlockWithName(leftDoorLightName);
    if (leftBlock == null)
    {
        Echo($"WARNING: Left door light '{leftDoorLightName}' not found at all.");
    }
    else
    {
        Echo($"Left door light found: {leftBlock.CustomName}");
        Echo($"Type: {leftBlock.BlockDefinition.TypeIdString}, Subtype: {leftBlock.BlockDefinition.SubtypeId}");
        leftDoorLight = leftBlock as IMyLightingBlock;
        if (leftDoorLight == null)
        {
            Echo("ERROR: Could not cast left door light to IMyLightingBlock. Wrong block type?");
        }
    }

    IMyTerminalBlock rightBlock = GridTerminalSystem.GetBlockWithName(rightDoorLightName);
    if (rightBlock == null)
    {
        Echo($"WARNING: Right door light '{rightDoorLightName}' not found at all.");
    }
    else
    {
        Echo($"Right door light found: {rightBlock.CustomName}");
        Echo($"Type: {rightBlock.BlockDefinition.TypeIdString}, Subtype: {rightBlock.BlockDefinition.SubtypeId}");
        rightDoorLight = rightBlock as IMyLightingBlock;
        if (rightDoorLight == null)
        {
            Echo("ERROR: Could not cast right door light to IMyLightingBlock. Wrong block type?");
        }
    }
    
    // Debug for other blocks
    if (lcdPanel == null)         Echo($"WARNING: LCD panel '{lcdName}' not found.");
    if (failureSoundBlock == null) Echo($"WARNING: Failure sound block '{soundBlockName}' not found.");
    if (successSoundBlock == null) Echo($"WARNING: Success sound block '{successSoundBlockName}' not found.");
    if (timerBlock == null)       Echo($"WARNING: Timer block '{timerBlockName}' not found.");

    // Setup PB display, if found
    if (largeDisplay != null)
        largeDisplay.ContentType = ContentType.SCRIPT;
    // Setup LCD panel, if found
    if (lcdPanel != null)
        lcdPanel.ContentType = ContentType.SCRIPT;
    
    // Update ~ every 1.67 sec
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    
    DisplayPrompt();
    UpdateLCDStatus();
}

// =========================================
// Main
// =========================================
public void Main(string argument, UpdateType updateSource)
{
    // Pincode input
    if (argument.Length == 1 && char.IsDigit(argument[0]))
    {
        ProcessInput(argument);
    }
    else if (argument == "Submit")
    {
        ValidateCode();
    }
    else if (argument == "Cancel")
    {
        ResetCode();
    }
    
    // Always update
    UpdateLCDStatus();
}

// =========================================
// Process Input
// =========================================
void ProcessInput(string input)
{
    if (enteredCode.Length < correctCode.Length)
    {
        enteredCode += input;
        DisplayPrompt();
    }
}

// =========================================
// Validate the Entered Code
// =========================================
void ValidateCode()
{
    if (enteredCode == correctCode)
    {
        DisplayAuthorized();
    }
    else
    {
        failureCount++;
        DisplayFailure();
    }
    enteredCode = "";
}

// =========================================
// Reset the Code Entry
// =========================================
void ResetCode()
{
    enteredCode = "";
    DisplayPrompt();
}

// =========================================
// Display the Lockdown Prompt
// =========================================
void DisplayPrompt()
{
    if (largeDisplay == null) return;
    using (var frame = largeDisplay.DrawFrame())
    {
        frame.Add(CreateTextSprite(headerMessage, headerColor, headerFontSize, headerPosition, TextAlignment.CENTER));
        frame.Add(CreateTextSprite(authMessage, authColor, authFontSize, authPosition, TextAlignment.CENTER));
        frame.Add(CreateTextSprite(enteredCode.PadRight(correctCode.Length, '_'), pinColor, pinFontSize, pinPosition, TextAlignment.CENTER));
    }
}

// =========================================
// Display Failure Message (and play failure sound)
// =========================================
void DisplayFailure()
{
    if (largeDisplay == null) return;
    using (var frame = largeDisplay.DrawFrame())
    {
        frame.Add(CreateTextSprite("ACCESS DENIED", Color.Red, failureFontSize, failurePosition, TextAlignment.CENTER));
    }
    if (failureSoundBlock == null)
    {
        Echo($"ERROR: Failure sound block '{soundBlockName}' is null.");
    }
    else
    {
        failureSoundBlock.Play();
    }
}

// =========================================
// Display Authorized Message (and success sound/timer)
// =========================================
void DisplayAuthorized()
{
    if (largeDisplay == null) return;
    using (var frame = largeDisplay.DrawFrame())
    {
        frame.Add(CreateTextSprite("ACCESS GRANTED", Color.Green, authorizedFontSize, authorizedPosition, TextAlignment.CENTER));
    }
    if (successSoundBlock == null)
    {
        Echo($"ERROR: Success sound block '{successSoundBlockName}' is null.");
    }
    else
    {
        successSoundBlock.Play();
    }
    if (timerBlock == null)
    {
        Echo($"ERROR: Timer block '{timerBlockName}' is null.");
    }
    else
    {
        timerBlock.StartCountdown(); // immediate trigger
    }
}

// =========================================
// Update LCD Status (Reactor, Door, Battery, etc.)
// and fade door lights
// =========================================
void UpdateLCDStatus()
{
    if (lcdPanel == null) return;

    // 1) Door States
    string leftDoorStatus = "NOT FOUND";
    Color leftDoorColor = Color.Red;
    string rightDoorStatus = "NOT FOUND";
    Color rightDoorColor = Color.Red;
    
    var leftDoor = GridTerminalSystem.GetBlockWithName(leftDoorName) as IMyDoor;
    if (leftDoor != null)
    {
        if (leftDoor.Enabled)
        {
            leftDoorStatus = "UNLOCKED";
            leftDoorColor = Color.Green;
        }
        else
        {
            leftDoorStatus = "LOCKED";
            leftDoorColor = Color.Red;
        }
    }
    var rightDoor = GridTerminalSystem.GetBlockWithName(rightDoorName) as IMyDoor;
    if (rightDoor != null)
    {
        if (rightDoor.Enabled)
        {
            rightDoorStatus = "UNLOCKED";
            rightDoorColor = Color.Green;
        }
        else
        {
            rightDoorStatus = "LOCKED";
            rightDoorColor = Color.Red;
        }
    }
    
    // Fade door lights
    if (leftDoorLight != null)
    {
        Color targetLeftColor = (leftDoorStatus == "UNLOCKED") ? doorUnlockedColor : doorLockedColor;
        UpdateDoorLight(leftDoorLight, targetLeftColor);
    }
    if (rightDoorLight != null)
    {
        Color targetRightColor = (rightDoorStatus == "UNLOCKED") ? doorUnlockedColor : doorLockedColor;
        UpdateDoorLight(rightDoorLight, targetRightColor);
    }

    // 2) Battery Data
    List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
    GridTerminalSystem.GetBlocksOfType(batteries, b => b.CubeGrid == Me.CubeGrid || b.CubeGrid.IsSameConstructAs(Me.CubeGrid));
    int totalBatteries = batteries.Count;
    int depletedBatteries = batteries.Count(b => b.CurrentStoredPower <= 0.01);
    
    double totalStoredPower = 0;
    double totalBatteryInput = 0;
    double totalBatteryOutput = 0;
    foreach (var battery in batteries)
    {
        totalStoredPower  += battery.CurrentStoredPower;
        totalBatteryInput += battery.CurrentInput;
        totalBatteryOutput+= battery.CurrentOutput;
    }
    
    // --- Batteries Deplete in Calculation (Approx. Lines 110-117) ---
    // netDrain: positive means batteries are depleting.
    double netDrain = totalBatteryOutput - totalBatteryInput;
    string remainingTimeStr = "";
    // For depletion, we want the display to be green.
    Color timeColor = Color.Green;
    if (netDrain > 0)
    {
        double remainingHours = totalStoredPower / netDrain;
        double remainingSeconds = remainingHours * 3600;
        TimeSpan ts = TimeSpan.FromSeconds(remainingSeconds);
        remainingTimeStr = string.Format("{0:D2}H:{1:D2}M:{2:D2}S", ts.Hours, ts.Minutes, ts.Seconds);
    }
    else
    {
        // Batteries are not depleting.
        double epsilon = 1e-14;
        double effectiveNetDrain = (Math.Abs(netDrain) < epsilon) ? epsilon : Math.Abs(netDrain);
        double ratio = totalStoredPower / effectiveNetDrain;
        remainingTimeStr = ratio.ToString("E2");
    }
    
    // --- Fully Charged in Calculation (Approx. Lines 118-127) ---
    double totalBatteryCapacity = 0;
    foreach (var battery in batteries)
    {
        totalBatteryCapacity += battery.MaxStoredPower;
    }
    // netCharge: positive means batteries are charging.
    double netCharge = totalBatteryInput - totalBatteryOutput;
    string fullChargeTimeStr = "";
    // For fully charged, we keep the display red.
    Color fullChargeTimeColor = Color.Red;
    if (netCharge > 0)
    {
        double remainingCharge = totalBatteryCapacity - totalStoredPower;
        double fullChargeHours = remainingCharge / netCharge;
        double fullChargeSeconds = fullChargeHours * 3600;
        TimeSpan tsCharge = TimeSpan.FromSeconds(fullChargeSeconds);
        fullChargeTimeStr = string.Format("{0:D2}H:{1:D2}M:{2:D2}S", tsCharge.Hours, tsCharge.Minutes, tsCharge.Seconds);
    }
    else
    {
        double remainingCharge = totalBatteryCapacity - totalStoredPower;
        double ratio = remainingCharge / (netCharge == 0 ? 1e-14 : netCharge);
        fullChargeTimeStr = ratio.ToString("E2");
    }
    
    // 3) Reactor Data
    List<IMyReactor> reactors = new List<IMyReactor>();
    GridTerminalSystem.GetBlocksOfType(reactors, r => r.CubeGrid.IsSameConstructAs(Me.CubeGrid));
    double totalReactorOutput = 0;
    foreach (var reactor in reactors)
    {
        totalReactorOutput += reactor.CurrentOutput;
    }
    string reactorStatus = (totalReactorOutput > 0.01) ? "ONLINE" : "OFFLINE";
    Color reactorStatusColor = (totalReactorOutput > 0.01) ? Color.Green : Color.Red;
    
    // Uranium in Reactors
    double reactorUranium = 0;
    foreach (var reactor in reactors)
    {
        var inv = reactor.GetInventory(0);
        if (inv != null)
        {
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            inv.GetItems(items);
            foreach (var item in items)
            {
                if (item.Type.SubtypeId == "Uranium")
                    reactorUranium += (double)item.Amount;
            }
        }
    }
    
    // Uranium on Ship
    double shipUranium = 0;
    List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType(allBlocks, b => b.CubeGrid.IsSameConstructAs(Me.CubeGrid));
    foreach (var block in allBlocks)
    {
        if (block is IMyReactor) continue;
        if (block.HasInventory)
        {
            for (int i = 0; i < block.InventoryCount; i++)
            {
                var inv = block.GetInventory(i);
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                inv.GetItems(items);
                foreach (var item in items)
                {
                    if (item.Type.SubtypeId == "Uranium")
                        shipUranium += (double)item.Amount;
                }
            }
        }
    }
    
    // Containment Field
    Color containmentColor = reactorContainmentActive ? Color.Green : Color.Red;
    string containmentStatus = reactorContainmentActive ? "ACTIVE" : "INACTIVE";

    // Aux Control Stations (placeholder)
    string auxControlStatus = "0/2 OFFLINE";
    Color auxControlColor = Color.Red;
    
    // 4) Build the LCD Display
    using (var frame = lcdPanel.DrawFrame())
    {
        Vector2 pos = lcdLabelStartPosition;
        List<MySprite> sprites = new List<MySprite>();
        
        AddLine(sprites, ref pos, "Reactor:", reactorStatus, Color.White, reactorStatusColor);
        AddLine(sprites, ref pos, "Containment Field:", containmentStatus, Color.White, containmentColor);
        AddLine(sprites, ref pos, "Reactor Room Left Door:", leftDoorStatus, Color.White, leftDoorColor);
        AddLine(sprites, ref pos, "Reactor Room Right Door:", rightDoorStatus, Color.White, rightDoorColor);
        
        Color reactorOutputColor = (totalReactorOutput <= 0.01) ? Color.Red : Color.White;
        AddLine(sprites, ref pos, "Reactor Output:", totalReactorOutput.ToString("F1") + " MW", Color.White, reactorOutputColor);
        
        Color uraniumShipColor = (shipUranium <= 0) ? Color.Red : Color.White;
        AddLine(sprites, ref pos, "Uranium Ingots (Ship):", shipUranium.ToString("N0") + " kg", Color.White, uraniumShipColor);
        
        Color uraniumReactorColor = (reactorUranium <= 0) ? Color.Red : Color.White;
        AddLine(sprites, ref pos, "Uranium (Reactor):", reactorUranium.ToString("N0") + " kg", Color.White, uraniumReactorColor);
        
        AddLine(sprites, ref pos, "Aux Control Stations:", auxControlStatus, Color.White, auxControlColor);
        
        // --- UPDATED: Stored Power as fraction ---
        Color storedPowerColor = (totalStoredPower <= 0.01) ? Color.Red : Color.White;
        AddLine(sprites, ref pos, "Stored Power:", string.Format("{0:F1} / {1:F1} MWh", totalStoredPower, totalBatteryCapacity), Color.White, storedPowerColor);
        
        AddLine(sprites, ref pos, "Battery Input:", totalBatteryInput.ToString("F1") + " MW", Color.White, Color.White);
        AddLine(sprites, ref pos, "Battery Output:", totalBatteryOutput.ToString("F1") + " MW", Color.White, Color.White);
        AddLine(sprites, ref pos, "Batteries Deplete in:", remainingTimeStr, Color.White, timeColor);
        // --- NEW: Fully Charged in:
        AddLine(sprites, ref pos, "Fully Charged in:", fullChargeTimeStr, Color.White, fullChargeTimeColor);
        
        foreach (var sprite in sprites)
            frame.Add(sprite);
    }
}

// =========================================
// Helper: Fade a Door Light's Color
// =========================================
void UpdateDoorLight(IMyLightingBlock light, Color targetColor)
{
    Color current = light.Color;
    byte newR = (byte)(current.R + (targetColor.R - current.R) * doorLightFadeSpeed);
    byte newG = (byte)(current.G + (targetColor.G - current.G) * doorLightFadeSpeed);
    byte newB = (byte)(current.B + (targetColor.B - current.B) * doorLightFadeSpeed);
    Color newColor = new Color(newR, newG, newB);

    light.Color = newColor;
    light.Radius = doorLightRadius;
    light.Falloff = doorLightFalloff;
    light.Intensity = doorLightIntensity;
    // If your lights support Offset, uncomment:
    // light.Offset = doorLightOffset;
}

// =========================================
// Helper: Add a Label/Value line
// =========================================
void AddLine(List<MySprite> sprites, ref Vector2 pos, string label, string value, Color labelColor, Color valueColor)
{
    sprites.Add(CreateTextSprite(label, labelColor, labelFontSize, pos, TextAlignment.LEFT));
    Vector2 valuePos = new Vector2(pos.X + valueOffsetX, pos.Y);
    sprites.Add(CreateTextSprite(value, valueColor, valueFontSize, valuePos, TextAlignment.LEFT));
    pos.Y += lcdLineSpacing;
}

// =========================================
// Helper: Create a Text Sprite
// =========================================
MySprite CreateTextSprite(string text, Color color, float scale, Vector2 position, TextAlignment alignment)
{
    return new MySprite
    {
        Type = SpriteType.TEXT,
        Data = text,
        Position = position,
        RotationOrScale = scale,
        Color = color,
        Alignment = alignment
    };
}
