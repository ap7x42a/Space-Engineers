//////////////////////////////////////////////////////////////
//  Parallel Boot Lines with Full Advanced Logic            //
//                                                          //
//  - Console LCD (index 0) starts in TEXT_AND_IMAGE so you //
//    can set normal text like "Ready to Initialize".       //
//  - All other LCDs (indices 1,2,3,4,5) start in SCRIPT    //
//    mode.                                                 //
//                                                          //
//  Boot Animation:                                         //
//   - Runs on indices 1,2,4,5 in parallel (no delays).      //
//   - When "initialize" is run, console (0) switches to     //
//     SCRIPT (but no boot lines are drawn on it).           //
//                                                          //
//  Custom Turret LCD (index 3):                            //
//   - Remains SCRIPT until the user presses button20, at   //
//     which point it switches to TEXT_AND_IMAGE and is no  //
//     longer updated.                                      //
//                                                          //
//  Main Menu:                                              //
//   - The third option is "Reactor Status" and the fourth  //
//     is "Overview". (Overview updates LCD status on Left  //
//     Corner and, when the menu is shown, triggers the     //
//     timer block.)                                        //
//                                                          //
//  Buttons: 2 = UP, 5 = DOWN, 3 = SELECT, 6 = BACK,         //
//           20 = Show Menu, 21 = Hide Menu                  //
//////////////////////////////////////////////////////////////

// ----------------- TUNABLE BOOT CONFIG -----------------
float BootTextSize       = 0.25f;   // Scale for boot lines.
float BootLineSpacing    = 30f;     // Spacing between boot lines.
float BoxSize            = 10f;     // Size of red/green box.
float BoxPad             = 10f;     // Gap from right edge.

float InsetBootLineStartMargin  = 0.10f;
float SlopedBootLineStartMargin = 0.18f;

const int BootLinesPerLCD = 30;
bool AllowTextScale       = true;

// Boot lines will run on these screen indices:
List<int> bootScreens = new List<int>() { 1, 2, 4, 5 };

// ----------------- MENU CONFIG -----------------
float   menuBannerFontSize = 2.0f;
Vector2 menuBannerPosition = new Vector2(525, 100);
float   menuFontSize       = 2.0f;
Vector2 menuStartPosition  = new Vector2(500, 175);
float   menuLineSpacing    = 60f;

string[] mainMenuItems = {
    "Power Production",
    "Power Consumption",
    "Reactor Status",
    "Overview"
};

string[] powerProductionSubmenu = {
    "Hydrogen Engines",
    "Reactor"
};

string[] powerConsumptionSubmenu = {
    "Weapons",
    "Life Support",
    "Computer Systems",
    "Basic Infrastructure"
};

// ----------------- ADVANCED REACTOR CONFIG -----------------
float   reactorStatusFontScale     = 0.6f;
float   reactorStatusLineSpacing   = 20f;
Vector2 reactorStatusStartPosition = new Vector2(10, 100);

// ----------------- MENU STATE -----------------
int  menuSelection        = 0;
int  submenuSelection     = 0;
int  menuLevel            = 0;  // 0 = main, 1 = powerProd, 2 = powerCons.
bool interactiveMenuActive= false;

// -------------- EXTRA REACTOR VARIABLES --------------
string leftDoorName  = "Reactor Room Left Door";
string rightDoorName = "Reactor Room Right Door";

IMyInteriorLight leftDoorLight  = null;
IMyInteriorLight rightDoorLight = null;
Color doorUnlockedColor = Color.Green;
Color doorLockedColor   = Color.Red;

bool reactorContainmentActive = false;

// ----------------- BOOT STATE -----------------
bool bootStarted  = false;
bool bootRunning  = false;
int  revealCount  = 0;

int  autoClearTimer      = 0;
bool autoClearTriggered  = false;
bool autoTriggerAllowed  = true;

// ----------------- OVERVIEW FLAG -----------------
bool overviewPBLaunched = false; // (Not used in this version)

// ----------------- DATA STRUCTURES -----------------
class LCDInfo {
    public string Name;
    public bool   IsInset;
    public bool   IsModule;
    public LCDInfo(string name, bool isInset, bool isModule) {
        Name     = name;
        IsInset  = isInset;
        IsModule = isModule;
    }
}

class MyLCD {
    public IMyTextSurface Surface;
    public bool IsInset;
    public bool IsModule;
    public MyLCD(IMyTextSurface s, bool inset, bool module) {
        Surface  = s;
        IsInset  = inset;
        IsModule = module;
    }
}

class BootLine {
    public string text;
    public bool   isGreen;
    public BootLine(string t, bool g) {
        text    = t;
        isGreen = g;
    }
}

class LcdState {
    public MyLCD[] Surfaces;
    public List<List<BootLine>> Lines;
}

// -------------- GLOBALS --------------
System.Random rnd = new System.Random();
LcdState state;
bool startupComplete = false;
bool otherPBLaunched = false; // (Not used here)
IMyTextSurface PB1;
IMyTextSurface PB2;

// ----------------- HELPER: Set Programmable Block Content Types -----------------
void SetPBContentTypes(ContentType ct) {
    IMyProgrammableBlock progBlock1 = GridTerminalSystem.GetBlockWithName("Reactor Room Aux Control Programmable Block") as IMyProgrammableBlock;
    IMyProgrammableBlock progBlock2 = GridTerminalSystem.GetBlockWithName("Reactor Room Aux Control Programmable Block 2") as IMyProgrammableBlock;
    if (progBlock1 != null) {
        var surface1 = progBlock1.GetSurface(0);
        if(surface1 != null)
            surface1.ContentType = ct;
    }
    if (progBlock2 != null) {
        var surface2 = progBlock2.GetSurface(0);
        if(surface2 != null)
            surface2.ContentType = ct;
    }
}

// ----------------- PROGRAM INIT -----------------
public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    
    // Define LCDs (indices 0-5):
    // 0 = Reactor Room Aux Console LCD
    // 1 = Reactor Room Aux Left Corner LCD
    // 2 = Reactor Room Aux 6 Button LCD
    // 3 = Reactor Room Aux Custom Turret LCD
    // 4 = Reactor Room Aux Inset LCD
    // 5 = Reactor Room Aux Right Corner LCD
    LCDInfo[] lcdInfos = new LCDInfo[] {
        new LCDInfo("Reactor Room Aux Console LCD",     false, false),
        new LCDInfo("Reactor Room Aux Left Corner LCD", true,  false),
        new LCDInfo("Reactor Room Aux 6 Button LCD",    false, false),
        new LCDInfo("Reactor Room Aux Custom Turret LCD", false, false),
        new LCDInfo("Reactor Room Aux Inset LCD",       true,  false),
        new LCDInfo("Reactor Room Aux Right Corner LCD", true,  false)
    };
    
    state = new LcdState();
    state.Surfaces = new MyLCD[lcdInfos.Length];
    state.Lines = new List<List<BootLine>>();
    
    for (int i = 0; i < lcdInfos.Length; i++) {
        state.Lines.Add(new List<BootLine>());
        IMyTerminalBlock block = GridTerminalSystem.GetBlockWithName(lcdInfos[i].Name);
        IMyTextSurface surf = null;
        if (block != null) {
            var provider = block as IMyTextSurfaceProvider;
            if (provider != null && provider.SurfaceCount > 0)
                surf = provider.GetSurface(0);
            else
                surf = block as IMyTextSurface;
        }
        state.Surfaces[i] = new MyLCD(surf, lcdInfos[i].IsInset, lcdInfos[i].IsModule);
    }
    
    // At startup:
    // Console (index 0) remains in TEXT_AND_IMAGE to show "Ready to Initialize".
    if (state.Surfaces[0].Surface != null) {
        state.Surfaces[0].Surface.ContentType = ContentType.TEXT_AND_IMAGE;
    }
    // Immediately set the two programmable blocks to TEXT_AND_IMAGE.
    IMyProgrammableBlock progBlock1 = GridTerminalSystem.GetBlockWithName("Reactor Room Aux Control Programmable Block") as IMyProgrammableBlock;
    IMyProgrammableBlock progBlock2 = GridTerminalSystem.GetBlockWithName("Reactor Room Aux Control Programmable Block 2") as IMyProgrammableBlock;
    if (progBlock1 != null) {
        PB1 = progBlock1.GetSurface(0);
        if(PB1 != null) PB1.ContentType = ContentType.TEXT_AND_IMAGE;
    }
    if (progBlock2 != null) {
        PB2 = progBlock2.GetSurface(0);
        if(PB2 != null) PB2.ContentType = ContentType.TEXT_AND_IMAGE;
    }
    
    // All other LCDs set to SCRIPT.
    for (int i = 1; i < state.Surfaces.Length; i++) {
        if (state.Surfaces[i].Surface != null) {
            state.Surfaces[i].Surface.ContentType = ContentType.SCRIPT;
        }
    }
    
    GenerateBootLines();
    ClearAll();
}

// ----------------- BOOT FUNCTIONS -----------------
void GenerateBootLines() {
    foreach (int i in bootScreens) {
        var s = state.Surfaces[i];
        if (s.Surface == null || s.IsModule) continue;
        state.Lines[i].Clear();
        for (int j = 0; j < BootLinesPerLCD; j++) {
            var line = GenerateRandomBootLine();
            state.Lines[i].Add(line);
        }
    }
}

BootLine GenerateRandomBootLine() {
    string[] templates = {
        "Initializing device /dev/sda{0} [OK]",
        "Mounting /dev/sda{0} partition {1} [OK]",
        "Loading module-X{1} version {0} [OK]",
        "Started service netmon-{1} on port {2} [FAIL]",
        "Process {3} allocated at 0x{4:X4} [OK]",
        "Network eth{0} up at 192.168.{1}.{2} [OK]",
        "BIOS-e820: [mem {4:X4}-{4:X4}] usable [FAIL]",
        "Allocating {3} KB of memory at 0x{4:X4} [OK]",
        "FS-check on /dev/sda{0}/{3} [OK]",
        "Powering subsystem {1} with PID-{3} [FAIL]",
        "Systemd-udevd started with ID {3} [OK]",
        "Mounting /dev/sda{0} at /mnt/data [OK]",
        "BIOS-provided physical RAM map: {0} [OK]",
        "BIOS-e820: [mem {4:X4}-{4:X4}] usable [OK]",
        "Loading driver {3}-ctrl (v{2}) [FAIL]"
    };
    int idx = rnd.Next(templates.Length);
    string template = templates[idx];
    int a = rnd.Next(0, 10);
    int b = rnd.Next(1, 5);
    int c = rnd.Next(100, 254);
    int d = rnd.Next(1000, 9999);
    int e = rnd.Next(0x1000, 0xFFFF);
    string line = string.Format(template, a, b, c, d, e);
    bool isGreen = line.EndsWith("[OK]");
    return new BootLine(line, isGreen);
}

public void Main(string argument, UpdateType updateSource) {
    if(argument.ToLower() == "reset") {
        ResetPBs();
        return;
    }    
    if (!startupComplete) {
        var consolePanel = GridTerminalSystem.GetBlockWithName("Reactor Room Aux Console LCD") as IMyTextPanel;
        if (consolePanel != null) {
            consolePanel.ContentType = ContentType.TEXT_AND_IMAGE;
            consolePanel.WriteText("Ready to Initialize");
        }
        startupComplete = true;
    }
    if ((updateSource & (UpdateType.Terminal | UpdateType.Trigger)) != 0) {
        if (string.IsNullOrWhiteSpace(argument)) {
            bootRunning = false;
            ClearAll();
            interactiveMenuActive = false;
            return;
        }
        if (argument.ToLower().StartsWith("button")) {
            ProcessButtonArgument(argument.ToLower());
            return;
        }
        if (argument.Contains("initialize")) {
            autoTriggerAllowed = true;
            StartBootAnimation();
        }
    }
    
    if (bootRunning) {
        UpdateBootAnimation();
    }
    else {
        if (!interactiveMenuActive) {
            // Auto-clear logic:
            if (bootStarted && !bootRunning && autoTriggerAllowed && !autoClearTriggered) {
                autoClearTimer++;
                if (autoClearTimer >= 6) {
                    ProcessButtonArgument("button20");
                    autoClearTriggered = true;
                }
            } else {
                autoClearTimer = 0;
                autoClearTriggered = false;
            }
        }
        else {
            // When menu is active, draw it on the 6 Button LCD (index 2)
            if (state.Surfaces.Length > 2 && state.Surfaces[2].Surface != null) {
                DrawInteractiveMenu(state.Surfaces[2].Surface);
            }
        }
    }
}

void StartBootAnimation() {
    bootStarted = true;
    bootRunning = true;
    revealCount = 0;
    
    // Switch Console (index 0) to SCRIPT so boot lines can run (though we don't draw on it)
    if (state.Surfaces[0].Surface != null) {
        state.Surfaces[0].Surface.ContentType = ContentType.SCRIPT;
    }
    // Switch the two programmable blocks to SCRIPT now that boot is starting.
    if (PB1 != null)
    {
        PB1.ContentType = ContentType.SCRIPT;
    }
    if (PB2 != null)
    {
        PB2.ContentType = ContentType.SCRIPT;
    }
    GenerateBootLines();
    TriggerTimerBlock();
    SwitchPBsToScript();
}

void UpdateBootAnimation() {
    if (revealCount < BootLinesPerLCD) {
        int inc = rnd.Next(2, 6);
        revealCount = Math.Min(revealCount + inc, BootLinesPerLCD);
    }
    
    // Draw boot lines on screens 1,2,4,5 in parallel.
    foreach (int i in bootScreens) {
        var lcd = state.Surfaces[i];
        if (lcd.Surface == null) continue;
        DrawBootScreen(lcd.Surface, state.Lines[i], revealCount, lcd.IsInset);
    }
    
    if (revealCount >= BootLinesPerLCD) {
        bootRunning = false;
        // Stop updating indices 4 and 5:
        bootScreens.Remove(4);
        bootScreens.Remove(5);
        if (state.Surfaces.Length > 4 && state.Surfaces[4].Surface != null) {
            ClearLCD(state.Surfaces[4].Surface);
        }
        if (state.Surfaces.Length > 5 && state.Surfaces[5].Surface != null) {
            ClearLCD(state.Surfaces[5].Surface);
        }
        UpdateLCDStatus();
    }
}

void DrawBootScreen(IMyTextSurface surf, List<BootLine> lines, int revealCount, bool isInset) {
    var size = surf.TextureSize;
    using (var frame = surf.DrawFrame()) {
        float baseDim = Math.Min(size.X, size.Y);
        float textScale = (baseDim / 280f) * BootTextSize;
        if (textScale < 0.1f) textScale = 0.1f;
        
        float marginLeft = size.X * 0.01f;
        float marginTop = isInset ? size.Y * InsetBootLineStartMargin : size.Y * SlopedBootLineStartMargin;
        float space = BootLineSpacing * textScale;
        float yPos = marginTop;
        
        int linesToDraw = Math.Min(revealCount, lines.Count);
        for (int i = 0; i < linesToDraw; i++) {
            var lineData = lines[i];
            var textSprite = new MySprite(SpriteType.TEXT, lineData.text);
            textSprite.Position = new Vector2(marginLeft, yPos);
            textSprite.Color = Color.White;
            textSprite.FontId = "Monospace";
            textSprite.Alignment = TextAlignment.LEFT;
            if (AllowTextScale)
                textSprite.RotationOrScale = textScale;
            frame.Add(textSprite);
            
            Color boxColor = lineData.isGreen ? Color.Green : Color.Red;
            float boxX = size.X - BoxPad - (BoxSize * 0.5f);
            float boxY = yPos + (space * 0.5f);
            var boxSprite = new MySprite(SpriteType.TEXTURE, "SquareSimple");
            boxSprite.Position = new Vector2(boxX, boxY);
            boxSprite.Size = new Vector2(BoxSize, BoxSize);
            boxSprite.Color = boxColor;
            frame.Add(boxSprite);
            
            yPos += space;
        }
    }
}

void ClearAll() {
    for (int i = 0; i < state.Surfaces.Length; i++) {
        if (state.Surfaces[i].Surface != null) {
            ClearLCD(state.Surfaces[i].Surface);
        }
    }
}

void ClearLCD(IMyTextSurface surf) {
    surf.ContentType = ContentType.SCRIPT;
    var size = surf.TextureSize;
    using (var frame = surf.DrawFrame()) {
        var bg = new MySprite(SpriteType.TEXTURE, "SquareSimple", size * 0.5f, size, Color.Black);
        frame.Add(bg);
    }
}

void ResetState() {
    // Reinitialize your variables/state.
    // For instance, you can call your constructor logic or
    // simply set your globals to their default values.
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    // ...more initialization...
}

// This function sends the "reset" argument to your PBs:
void ResetPBs() {
    IMyProgrammableBlock pb1 = GridTerminalSystem.GetBlockWithName("Reactor Room Aux Control Programmable Block") as IMyProgrammableBlock;
    IMyProgrammableBlock pb2 = GridTerminalSystem.GetBlockWithName("Reactor Room Aux Control Programmable Block 2") as IMyProgrammableBlock;
    if(pb1 != null)
        pb1.TryRun("reset");
    if(pb2 != null)
        pb2.TryRun("reset");
    Echo("Reset signal sent to both programmable blocks.");
}

// ----------------- ROBUST HELPER FUNCTION FOR TIMER BLOCK -----------------
void TriggerTimerBlock() {
    IMyTimerBlock timer = GridTerminalSystem.GetBlockWithName("Reactor Room Aux Init Assist") as IMyTimerBlock;
    if (timer == null) {
        Echo("ERROR: 'Reactor Room Aux Init Assist' timer block not found!");
        return;
    }
    if (!timer.Enabled) {
        Echo("ERROR: 'Reactor Room Aux Init Assist' timer block is disabled!");
        return;
    }
    // Set delay to 0 for instant trigger.
    timer.SetValueFloat("TriggerDelay", 0f);
    timer.ApplyAction("Start");
    Echo("SUCCESS: Timer block 'Reactor Room Aux Init Assist' triggered instantly.");
}

void SwitchPBsToScript()
{
    if(PB1 != null) PB1.ContentType = ContentType.SCRIPT;
    if(PB2 != null) PB2.ContentType = ContentType.SCRIPT;
}

// ----------------- DISPLAY FUNCTIONS -----------------
void DisplayDetails(string details) {
    if (state.Surfaces.Length > 1 && state.Surfaces[1].Surface != null) {
        IMyTextSurface detailSurf = state.Surfaces[1].Surface;
        detailSurf.ContentType = ContentType.SCRIPT;
        using (var frame = detailSurf.DrawFrame()) {
            var size = detailSurf.TextureSize;
            var bg = new MySprite(SpriteType.TEXTURE, "SquareSimple", size * 0.5f, size, Color.Black);
            frame.Add(bg);
            var detailSprite = new MySprite(SpriteType.TEXT, details);
            detailSprite.Position = new Vector2(10, 100);
            detailSprite.Color = Color.White;
            detailSprite.FontId = "Monospace";
            detailSprite.Alignment = TextAlignment.LEFT;
            detailSprite.RotationOrScale = 0.5f;
            frame.Add(detailSprite);
        }
    }
}

void UpdateLCDStatus() {
    // Update advanced data on Left Corner LCD (index 1)
    if (state.Surfaces.Length <= 1 || state.Surfaces[1].Surface == null)
        return;
    
    IMyTextSurface lcdPanel = state.Surfaces[1].Surface;
    lcdPanel.ContentType = ContentType.SCRIPT;
    
    // 1) Door States
    string leftDoorStatus = "NOT FOUND";
    Color leftDoorColor = Color.Red;
    string rightDoorStatus = "NOT FOUND";
    Color rightDoorColor = Color.Red;
    
    var leftDoor = GridTerminalSystem.GetBlockWithName(leftDoorName) as IMyDoor;
    if (leftDoor != null) {
        if (leftDoor.Enabled) {
            leftDoorStatus = "UNLOCKED";
            leftDoorColor = Color.Green;
        } else {
            leftDoorStatus = "LOCKED";
            leftDoorColor = Color.Red;
        }
    }
    var rightDoor = GridTerminalSystem.GetBlockWithName(rightDoorName) as IMyDoor;
    if (rightDoor != null) {
        if (rightDoor.Enabled) {
            rightDoorStatus = "UNLOCKED";
            rightDoorColor = Color.Green;
        } else {
            rightDoorStatus = "LOCKED";
            rightDoorColor = Color.Red;
        }
    }
    
    // 2) Battery Data
    List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
    GridTerminalSystem.GetBlocksOfType(batteries, b => b.CubeGrid.IsSameConstructAs(Me.CubeGrid));
    double totalStoredPower = 0, totalBatteryInput = 0, totalBatteryOutput = 0, totalBatteryCapacity = 0;
    foreach (var bat in batteries) {
        totalStoredPower += bat.CurrentStoredPower;
        totalBatteryInput += bat.CurrentInput;
        totalBatteryOutput += bat.CurrentOutput;
        totalBatteryCapacity += bat.MaxStoredPower;
    }
    
    double netDrain = totalBatteryOutput - totalBatteryInput;
    string remainingTimeStr = "";
    Color timeColor = Color.Green;
    if (netDrain > 0) {
        double hrs = totalStoredPower / netDrain;
        double secs = hrs * 3600;
        TimeSpan ts = TimeSpan.FromSeconds(secs);
        remainingTimeStr = string.Format("{0:D2}H:{1:D2}M:{2:D2}S", ts.Hours, ts.Minutes, ts.Seconds);
    } else {
        remainingTimeStr = "∞";
    }
    
    double netCharge = totalBatteryInput - totalBatteryOutput;
    string fullChargeTimeStr = "";
    Color fullChargeTimeColor = Color.Red;
    if (netCharge > 0) {
        double remain = totalBatteryCapacity - totalStoredPower;
        double hrs = remain / netCharge;
        double secs = hrs * 3600;
        TimeSpan tsC = TimeSpan.FromSeconds(secs);
        fullChargeTimeStr = string.Format("{0:D2}H:{1:D2}M:{2:D2}S", tsC.Hours, tsC.Minutes, tsC.Seconds);
    } else {
        fullChargeTimeStr = "N/A";
    }
    
    // 3) Reactor Data
    List<IMyReactor> reactors = new List<IMyReactor>();
    GridTerminalSystem.GetBlocksOfType(reactors, r => r.CubeGrid.IsSameConstructAs(Me.CubeGrid));
    double totalReactorOutput = 0;
    foreach (var reac in reactors)
        totalReactorOutput += reac.CurrentOutput;
    
    string reactorStatus = (totalReactorOutput > 0.01) ? "ONLINE" : "OFFLINE";
    Color reactorStatusColor = (totalReactorOutput > 0.01) ? Color.Green : Color.Red;
    
    // Uranium in Reactors
    double reactorUranium = 0;
    foreach (var reac in reactors) {
        var inv = reac.GetInventory(0);
        if (inv != null) {
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            inv.GetItems(items);
            foreach (var item in items) {
                if (item.Type.SubtypeId == "Uranium")
                    reactorUranium += (double)item.Amount;
            }
        }
    }
    
    // Uranium on Ship
    double shipUranium = 0;
    List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocks(allBlocks);
    foreach (var block in allBlocks) {
        if (!block.CubeGrid.IsSameConstructAs(Me.CubeGrid)) continue;
        if (block is IMyReactor) continue;
        if (block.HasInventory) {
            for (int i = 0; i < block.InventoryCount; i++) {
                var inv = block.GetInventory(i);
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                inv.GetItems(items);
                foreach (var item in items) {
                    if (item.Type.SubtypeId == "Uranium")
                        shipUranium += (double)item.Amount;
                }
            }
        }
    }
    
    // Containment Field
    Color containmentColor = reactorContainmentActive ? Color.Green : Color.Red;
    string containmentStatus = reactorContainmentActive ? "ACTIVE" : "INACTIVE";
    
    // Aux Control
    string auxControlStatus = "0/2 OFFLINE";
    Color auxControlColor = Color.Red;
    
    using (var frame = lcdPanel.DrawFrame()) {
        var bg = new MySprite(SpriteType.TEXTURE, "SquareSimple", lcdPanel.TextureSize * 0.5f, lcdPanel.TextureSize, Color.Black);
        frame.Add(bg);
        Vector2 pos = reactorStatusStartPosition;
        
        pos = WriteLine(frame, pos, "Reactor:", reactorStatus, Color.White, reactorStatusColor);
        pos = WriteLine(frame, pos, "Containment:", containmentStatus, Color.White, containmentColor);
        pos = WriteLine(frame, pos, "Left Door:", leftDoorStatus, Color.White, leftDoorColor);
        pos = WriteLine(frame, pos, "Right Door:", rightDoorStatus, Color.White, rightDoorColor);
        
        Color roColor = (totalReactorOutput <= 0.01) ? Color.Red : Color.White;
        pos = WriteLine(frame, pos, "Reactor Output:", totalReactorOutput.ToString("F1") + " MW", Color.White, roColor);
        
        Color uraniumShipColor = (shipUranium <= 0) ? Color.Red : Color.White;
        pos = WriteLine(frame, pos, "Uranium (Ship):", shipUranium.ToString("N0") + " kg", Color.White, uraniumShipColor);
        
        Color uraniumReactorColor = (reactorUranium <= 0) ? Color.Red : Color.White;
        pos = WriteLine(frame, pos, "Uranium (Reactor):", reactorUranium.ToString("N0") + " kg", Color.White, uraniumReactorColor);
        
        pos = WriteLine(frame, pos, "Aux Control:", auxControlStatus, Color.White, auxControlColor);
        
        Color storedPowerColor = (totalStoredPower <= 0.01) ? Color.Red : Color.White;
        pos = WriteLine(frame, pos, "Stored Power:", string.Format("{0:F1}/{1:F1} MWh", totalStoredPower, totalBatteryCapacity), Color.White, storedPowerColor);
        
        pos = WriteLine(frame, pos, "Battery Input:", totalBatteryInput.ToString("F1") + " MW", Color.White, Color.White);
        pos = WriteLine(frame, pos, "Battery Output:", totalBatteryOutput.ToString("F1") + " MW", Color.White, Color.White);
        
        pos = WriteLine(frame, pos, "Batteries Deplete:", remainingTimeStr, Color.White, timeColor);
        pos = WriteLine(frame, pos, "Fully Charged:", fullChargeTimeStr, Color.White, fullChargeTimeColor);
    }
}

Vector2 WriteLine(MySpriteDrawFrame frame, Vector2 pos,
                  string label, string value,
                  Color labelColor, Color valueColor) {
    var labelSprite = new MySprite(SpriteType.TEXT, label);
    labelSprite.Position = pos;
    labelSprite.Color = labelColor;
    labelSprite.FontId = "Monospace";
    labelSprite.Alignment = TextAlignment.LEFT;
    labelSprite.RotationOrScale = reactorStatusFontScale;
    frame.Add(labelSprite);
    
    var valPos = pos + new Vector2(300, 0);
    var valSprite = new MySprite(SpriteType.TEXT, value);
    valSprite.Position = valPos;
    valSprite.Color = valueColor;
    valSprite.FontId = "Monospace";
    valSprite.Alignment = TextAlignment.LEFT;
    valSprite.RotationOrScale = reactorStatusFontScale;
    frame.Add(valSprite);
    
    return pos + new Vector2(0, reactorStatusLineSpacing);
}

void DrawInteractiveMenu(IMyTextSurface surf) {
    using (var frame = surf.DrawFrame()) {
        var size = surf.TextureSize;
        var bg = new MySprite(SpriteType.TEXTURE, "SquareSimple", size * 0.5f, size, Color.Black);
        frame.Add(bg);
        
        var headerSprite = new MySprite(SpriteType.TEXT, "Reactor Aux. Control");
        headerSprite.Position = menuBannerPosition;
        headerSprite.Color = Color.Red;
        headerSprite.FontId = "Monospace";
        headerSprite.RotationOrScale = menuBannerFontSize;
        frame.Add(headerSprite);
        
        string[] itemsToDisplay;
        int selectedIndex;
        if (menuLevel == 0) {
            itemsToDisplay = mainMenuItems;
            selectedIndex = menuSelection;
        }
        else if (menuLevel == 1) {
            itemsToDisplay = powerProductionSubmenu;
            selectedIndex = submenuSelection;
        }
        else {
            itemsToDisplay = powerConsumptionSubmenu;
            selectedIndex = submenuSelection;
        }
        
        for (int i = 0; i < itemsToDisplay.Length; i++) {
            Vector2 pos = menuStartPosition + new Vector2(0, menuLineSpacing * i);
            if (i == selectedIndex) {
                float highlightHeight = menuLineSpacing;
                float highlightWidth = size.X;
                Vector2 highlightPos = new Vector2(size.X * 0.5f, pos.Y + highlightHeight * 0.5f);
                var highlightRect = new MySprite(SpriteType.TEXTURE, "SquareSimple");
                highlightRect.Position = highlightPos;
                highlightRect.Size = new Vector2(highlightWidth, highlightHeight);
                highlightRect.Color = new Color(128, 0, 0);
                frame.Add(highlightRect);
                
                var menuSprite = new MySprite(SpriteType.TEXT, itemsToDisplay[i]);
                menuSprite.Position = pos;
                menuSprite.Color = Color.Orange;
                menuSprite.FontId = "Monospace";
                menuSprite.RotationOrScale = menuFontSize;
                frame.Add(menuSprite);
            }
            else {
                var menuSprite = new MySprite(SpriteType.TEXT, itemsToDisplay[i]);
                menuSprite.Position = pos;
                menuSprite.Color = Color.White;
                menuSprite.FontId = "Monospace";
                menuSprite.RotationOrScale = menuFontSize;
                frame.Add(menuSprite);
            }
        }
    }
}

// ----------------- BUTTON HANDLING -----------------
void ProcessButtonArgument(string arg) {
    if (arg == "button20") {
        // Show menu: clear all and switch Console (index 0) to SCRIPT.
        ClearAll();
        if (state.Surfaces.Length > 0 && state.Surfaces[0].Surface != null) {
            state.Surfaces[0].Surface.ContentType = ContentType.SCRIPT;
        }
        interactiveMenuActive = true;
        if (state.Surfaces.Length > 3 && state.Surfaces[3].Surface != null) {
            state.Surfaces[3].Surface.ContentType = ContentType.TEXT_AND_IMAGE;
        }
        // When the menu appears, default to "Overview" (menuSelection == 3)
        // and update LCD status. Also, trigger the timer block instantly.
        menuSelection = 3; // "Overview"
        menuLevel = 0;
        submenuSelection = 0;
        UpdateLCDStatus();
        return;
    }
    
    if (arg == "button21") {
        ClearAll();
        interactiveMenuActive = false;
        autoTriggerAllowed = false;
        return;
    }
    
    if (!interactiveMenuActive)
        return;
    
    if (arg == "button2") { // UP
        if (menuLevel == 0) {
            menuSelection = (menuSelection - 1 + mainMenuItems.Length) % mainMenuItems.Length;
        } else if (menuLevel == 1) {
            submenuSelection = (submenuSelection - 1 + powerProductionSubmenu.Length) % powerProductionSubmenu.Length;
        } else if (menuLevel == 2) {
            submenuSelection = (submenuSelection - 1 + powerConsumptionSubmenu.Length) % powerConsumptionSubmenu.Length;
        }
    }
    else if (arg == "button5") { // DOWN
        if (menuLevel == 0) {
            menuSelection = (menuSelection + 1) % mainMenuItems.Length;
        } else if (menuLevel == 1) {
            submenuSelection = (submenuSelection + 1) % powerProductionSubmenu.Length;
        } else if (menuLevel == 2) {
            submenuSelection = (submenuSelection + 1) % powerConsumptionSubmenu.Length;
        }
    }
    else if (arg == "button3") { // SELECT
        if (menuLevel == 0) {
            if (menuSelection == 0) {
                menuLevel = 1;
                submenuSelection = 0;
            } else if (menuSelection == 1) {
                menuLevel = 2;
                submenuSelection = 0;
            } else if (menuSelection == 2) {
                // Reactor Status branch: update LCD status only.
                UpdateLCDStatus();
            } else if (menuSelection == 3) {
                // Overview branch: update LCD status and instantly trigger the timer block.
                UpdateLCDStatus();
            }
        }
        else if (menuLevel == 1) {
            if (submenuSelection == 0) {
                DisplayDetails("Hydrogen Engines:\nDetails about the hydrogen engines...");
            } else if (submenuSelection == 1) {
                DisplayDetails("Production Reactor:\nDetails about the reactor for power production...");
            }
        }
        else if (menuLevel == 2) {
            if (submenuSelection == 0) {
                DisplayDetails("Weapons:\nDetails about weapons power consumption...");
            } else if (submenuSelection == 1) {
                DisplayDetails("Life Support:\nDetails about O2 tanks, O2/H2 generators, and vents...");
            } else if (submenuSelection == 2) {
                DisplayDetails("Computer Systems:\nDetails about LCDs, PBs, timers, event controllers...");
            } else if (submenuSelection == 3) {
                DisplayDetails("Basic Infrastructure:\nDetails about lights, buttons, doors, etc...");
            }
        }
    }
    else if (arg == "button6") { // BACK
        if (menuLevel != 0) {
            menuLevel = 0;
            menuSelection = 3; // "Overview"
            submenuSelection = 0;
            UpdateLCDStatus();
        }
    }
}
