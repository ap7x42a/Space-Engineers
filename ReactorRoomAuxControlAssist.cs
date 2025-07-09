//////////////////////////////////////////////////////////////
// Reactor Modules Display Script for Inset LCD
// With Reactor Temperature, Emergency Shutdown, and
// Detailed Reactor Stats Display (Subgrid Support)
//////////////////////////////////////////////////////////////

// ----------------- TUNABLE VARIABLES -----------------

// Activation: script remains dormant until "initialize" is run.
bool active = false;

// Block names for actual devices:
string reactorBlockName = "Large Reactor 2";         // Exact name of the reactor block (on subgrid)
string lcdName = "Reactor Room Aux Inset LCD";         // Main LCD (menu and summary)
string detailLCDName = "Reactor Room Aux Right Corner LCD"; // Detail LCD
string lightsProgrammableBlockName = "Reactor Room Lights Programmable Block"; // Big PB display

// Header settings:
string headerText = "Reactor Modules";
float headerFontScale = 1.5f;  
Vector2 headerPosition = new Vector2(40, 20);  // Adjust as needed

// Dynamic Info settings (for main LCD):
float infoFontScaleLabel = 0.8f;   // Font scale for labels
float infoFontScaleValue = 0.8f;   // Font scale for values
float infoLineSpacing = 20f;       // Vertical spacing between info lines
Vector2 infoStartPos = new Vector2(80, 80);    // Starting position for dynamic info
float valueColumnX = 330f;         // X position for variable values

// Message body settings:
string messageBody = "10MW of stored power is\nrequired to activate\nthis reactor.";
float messageFontScale = 0.8f;
Vector2 messagePosition = new Vector2(250, 165);  // Adjust as needed

// Menu settings:
string[] menuItems = new string[] { "Coolant Pumps", "Batteries", "Reactor" };
int menuSelectedIndex = 0; // selected menu item index
Vector2 menuBannerPosition = new Vector2(80, 220);  // Position for the menu header
Vector2 menuStartPosition = new Vector2(250, 250);  // Starting position for the menu items
float menuLineSpacing = 40f;  // Vertical spacing between menu items
float menuFontSize = 1.2f;    // Font size for menu items

// Colors:
Color colorOnline = Color.Green;          // For good/online status
Color colorOffline = Color.Red;           // For offline/error states
Color colorWarning = new Color(255, 165, 0); // yellow-orange
Color staticLabelColor = Color.White;     // for static labels
Color messageColor = Color.Orange;        // for the message body

// Stored power threshold (in MWh)
double storedPowerThreshold = 10.0;

// ----------------- DETAILED INFO VIEW TUNABLE VARIABLES -----------------
Vector2 detailedInfoStaticStartPos = new Vector2(20, 150);   // Starting position for static (label) column
float detailedInfoStaticFontSize = 0.6f;                     // Font size for static text
Vector2 detailedInfoVariableStartPos = new Vector2(300, 150);// Starting position for variable info column
float detailedInfoVariableFontSize = 0.6f;                   // Font size for variable text
float detailedInfoLineSpacing = 17f;                         // Vertical spacing between lines

// ----------------- PUMP FLOW SIMULATION TUNABLE VARIABLES -----------------
double pumpBaseFlow = 15.0;          // Base flow in L/s.
double pumpFlowMin = 14.0;           // Minimum flow rate in L/s.
double pumpFlowMax = 16.0;           // Maximum flow rate in L/s.
double pumpFlowRandomDelta = 0.1;    // Maximum change per update (in L/s).

// ----------------- BLOCK NAMES -----------------
string coolantPumpBatteryLeft = "Reactor Left Aux Coolant Pump Battery";
string coolantPumpBatteryRight = "Reactor Right Aux Coolant Pump Battery";

string auxBattery1 = "Reactor Aux Battery 1";
string auxBattery2 = "Reactor Aux Battery 2";
string auxBattery3 = "Reactor Aux Battery 3";
string auxBattery4 = "Reactor Aux Battery 4";
string mainControlBatteryName = "Reactor Room Main Control Battery";

// ----------------- REACTOR TEMPERATURE & FUEL TUNABLES -----------------
double reactorTemperature = 800.0;      // Starting temperature in K
double reactorTemperatureMin = 600.0;   // Minimum temperature in K
double reactorTemperatureMax = 1200.0;  // Maximum temperature in K
double reactorTemperatureDelta = 5.0;   // Multiplier for temperature changes
double roomTemperature = 700.0;         // Baseline room temperature in K

double reactorFuel = 100.0;             // Reactor fuel percentage (0-100)

// ----------------- REACTOR STATISTICS SIMULATION -----------------
double reactorUptime = 0.0;             // Uptime counter in seconds
double reactorU235 = 5000.0;            // Uranium in kg
double reactorPowerOutput = 20.0;       // Power output in MW
double rotorSpeed = 1500.0;             // Rotor speed in RPM
bool fieldProjectorOnline = true;       // Simulated Containment Field status

// ----------------- REACTOR LIGHTS DISPLAY TUNABLES -----------------
float lightsDisplayFontSize = 2.0f;
float lightsDisplayLineSpacing = 40f;

// ----------------- GLOBALS -----------------
IMyTextSurface lcd;             // Main LCD surface (menu/summary)
IMyReactor mainReactor;         // Actual reactor block (supports subgrids)

int updateCounter = 0;          // Approximately 6 updates per second

Dictionary<string, double> simulatedFlowRates = new Dictionary<string, double>();
Random randomGen = new Random();

// Reactor state
bool reactorIsOnline = true;
bool emergencyShutdown = false;
float timeWithoutPumpsWhileOn = 0f;

// Default detail selection: 0 = Coolant Pumps, 1 = Batteries, 2 = Reactor.
int detailSelectionIndex = 0;

// ----------------- PROGRAM INIT -----------------
public Program() {
    updateCounter = 0;
    menuSelectedIndex = 0;
    detailSelectionIndex = 0;
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    
    // Get the main LCD block.
    IMyTerminalBlock block = GridTerminalSystem.GetBlockWithName(lcdName);
    if (block != null) {
        lcd = block as IMyTextSurface;
        if (lcd != null) {
            lcd.ContentType = ContentType.SCRIPT;
            ClearLCD(lcd);
        }
    }
    
    // Find the reactor block across all grids (supports subgrids).
    List<IMyReactor> reactors = new List<IMyReactor>();
    GridTerminalSystem.GetBlocksOfType<IMyReactor>(reactors, r => r.CustomName.Equals(reactorBlockName, System.StringComparison.OrdinalIgnoreCase));
    if (reactors.Count > 0) {
        mainReactor = reactors[0];
    } else {
        mainReactor = null;
    }
    
    // Remain dormant until "initialize" is run.
}

// ----------------- MAIN FUNCTION -----------------
public void Main(string argument, UpdateType updateSource) {
    if (argument.ToLower() == "reset") {
        ResetState();
        Echo("PB reset to starting state.");
        return;
    }
    
    if ((updateSource & (UpdateType.Terminal | UpdateType.Trigger)) != 0) {
        string lowerArg = argument.ToLower();
        if (lowerArg == "initialize") {
            active = true;
        }
        else if (lowerArg == "button7") {  // Move menu selector up
            menuSelectedIndex--;
            if (menuSelectedIndex < 0)
                menuSelectedIndex = menuItems.Length - 1;
            detailSelectionIndex = menuSelectedIndex;
        }
        else if (lowerArg == "button8") {  // Move menu selector down
            menuSelectedIndex++;
            if (menuSelectedIndex >= menuItems.Length)
                menuSelectedIndex = 0;
            detailSelectionIndex = menuSelectedIndex;
        }
        else if (lowerArg == "button10") {  // Commit current selection
            detailSelectionIndex = menuSelectedIndex;
        }
    }
    
    if (!active) return;
    
    updateCounter++;
    if (updateCounter >= 6) {  // Approximately 1 second (Update10 ~6/sec)
        updateCounter = 0;
        
        UpdateReactorState();
        UpdateDisplay();
        DisplayDetailedInfo(detailSelectionIndex);
    }
}

// ----------------- REACTOR STATE LOGIC -----------------
void UpdateReactorState() {
    // Update reactor state from the actual reactor block.
    if (mainReactor != null) {
        reactorIsOnline = mainReactor.Enabled && mainReactor.IsFunctional && mainReactor.IsWorking;
    } else {
        reactorIsOnline = false;
    }
    
    if (emergencyShutdown) {
        reactorIsOnline = false;
        if (mainReactor != null) {
            mainReactor.Enabled = false; // Force shutdown.
        }
    }
    
    if (reactorIsOnline && !emergencyShutdown) {
        int pumpCount = GetSimulatedPumpCount(coolantPumpBatteryLeft, coolantPumpBatteryRight);
        if (pumpCount < 2) {
            timeWithoutPumpsWhileOn += 1f;
            reactorTemperature += 10f * reactorTemperatureDelta;
            reactorFuel -= 0.5;  // Higher consumption.
            if (timeWithoutPumpsWhileOn > 10f) {
                emergencyShutdown = true;
                reactorIsOnline = false;
                if (mainReactor != null)
                    mainReactor.Enabled = false;
            }
        } else {
            timeWithoutPumpsWhileOn = 0f;
            reactorTemperature -= 5f * reactorTemperatureDelta;
            reactorFuel -= 0.2;  // Lower consumption.
        }
    } else {
        // Reactor offline: drift temperature toward roomTemperature and recover fuel.
        timeWithoutPumpsWhileOn = 0f;
        if (reactorTemperature > roomTemperature)
            reactorTemperature -= 10f * reactorTemperatureDelta;
        else if (reactorTemperature < roomTemperature)
            reactorTemperature += 10f * reactorTemperatureDelta;
        reactorFuel += 0.2;
    }
    
    reactorFuel = Math.Max(0, Math.Min(100, reactorFuel));
    reactorTemperature = Math.Max(reactorTemperatureMin, Math.Min(reactorTemperatureMax, reactorTemperature));
    
    // Update reactor stats if online.
    if (reactorIsOnline) {
        reactorUptime += 1.0;
        reactorPowerOutput = 15.0 + randomGen.NextDouble() * 10.0;  // 15-25 MW
        rotorSpeed = 1400.0 + randomGen.NextDouble() * 200.0;         // 1400-1600 RPM
        reactorU235 = Math.Max(0, reactorU235 - 1.0);
    } else {
        reactorUptime = 0.0;
        reactorPowerOutput = 0.0;
        rotorSpeed = 0.0;
    }
    
    UpdateReactorLightsDisplay();
}

// ----------------- UPDATE MAIN LCD DISPLAY -----------------
void UpdateDisplay() {
    if (lcd == null) return;
    Vector2 size = lcd.TextureSize;
    using (var frame = lcd.DrawFrame()) {
        var bg = new MySprite(SpriteType.TEXTURE, "SquareSimple", size * 0.5f, size, Color.Black);
        frame.Add(bg);
        
        // Header.
        var headerSprite = new MySprite(SpriteType.TEXT, headerText);
        headerSprite.Position = headerPosition;
        headerSprite.Color = Color.Red;
        headerSprite.FontId = "Monospace";
        headerSprite.RotationOrScale = headerFontScale;
        headerSprite.Alignment = TextAlignment.LEFT;
        frame.Add(headerSprite);
        
        float y = infoStartPos.Y;
        // Reactor Temperature.
        string tempLabel = "Reactor Temp:";
        string tempValue = reactorTemperature.ToString("F1") + " K";
        DrawInfoLine(frame, infoStartPos.X, y, tempLabel, tempValue, staticLabelColor, Color.White);
        y += infoLineSpacing;
        
        // Coolant Pumps.
        int pumpCount = GetSimulatedPumpCount(coolantPumpBatteryLeft, coolantPumpBatteryRight);
        string pumpsLabel = "Coolant Pumps:";
        string pumpsValue = pumpCount + "/2";
        Color pumpsValueColor = (pumpCount == 2) ? colorOnline : (pumpCount == 1 ? colorWarning : colorOffline);
        DrawInfoLine(frame, infoStartPos.X, y, pumpsLabel, pumpsValue, staticLabelColor, pumpsValueColor);
        y += infoLineSpacing;
        
        // Aux Batteries.
        int auxCount = GetAuxBatteryCount(auxBattery1, auxBattery2, auxBattery3, auxBattery4);
        string auxLabel = "Aux Batteries:";
        string auxValue = auxCount + "/4";
        Color auxValueColor = (auxCount == 4) ? colorOnline : (auxCount >= 1 ? colorWarning : colorOffline);
        DrawInfoLine(frame, infoStartPos.X, y, auxLabel, auxValue, staticLabelColor, auxValueColor);
        y += infoLineSpacing;
        
        // Stored Power.
        double totalPower = GetTotalStoredPower();
        string powerLabel = "Stored Power:";
        string powerValue = totalPower.ToString("F1") + " MWh";
        Color powerValueColor = (totalPower >= storedPowerThreshold) ? colorOnline : colorOffline;
        DrawInfoLine(frame, infoStartPos.X, y, powerLabel, powerValue, staticLabelColor, powerValueColor);
        y += infoLineSpacing;
        
        ToggleMainControlBattery(totalPower);
        
        // Message body.
        var messageSprite = new MySprite(SpriteType.TEXT, messageBody);
        messageSprite.Position = messagePosition;
        messageSprite.Color = messageColor;
        messageSprite.FontId = "Monospace";
        messageSprite.RotationOrScale = messageFontScale;
        messageSprite.Alignment = TextAlignment.CENTER;
        frame.Add(messageSprite);
        
        // Menu.
        DrawMenu(frame);
    }
}

// ----------------- DISPLAY DETAILED INFO (DETAIL LCD) -----------------
void DisplayDetailedInfo(int menuIndex) {
    IMyTerminalBlock detailLCDBlock = GridTerminalSystem.GetBlockWithName(detailLCDName);
    if (detailLCDBlock == null) return;
    IMyTextSurface detailLCD = detailLCDBlock as IMyTextSurface;
    if (detailLCD == null) return;
    
    detailLCD.ContentType = ContentType.SCRIPT;
    using (var frame = detailLCD.DrawFrame()) {
        Vector2 size = detailLCD.TextureSize;
        var bg = new MySprite(SpriteType.TEXTURE, "SquareSimple", size * 0.5f, size, Color.Black);
        frame.Add(bg);
        
        float startY = detailedInfoStaticStartPos.Y;
        
        if (menuIndex == 0) {
            // "Coolant Pumps" detailed view.
            var headerSprite = new MySprite(SpriteType.TEXT, "COOLANT PUMPS STATUS");
            headerSprite.Position = new Vector2(20, 100);
            headerSprite.Color = Color.Red;
            headerSprite.FontId = "Monospace";
            headerSprite.RotationOrScale = 1.2f;
            headerSprite.Alignment = TextAlignment.LEFT;
            frame.Add(headerSprite);
            
            // Core Temperature.
            int pumpCountLocal = GetSimulatedPumpCount(coolantPumpBatteryLeft, coolantPumpBatteryRight);
            Color tempColor = !reactorIsOnline ? Color.White : (pumpCountLocal < 2 ? Color.Red : Color.Green);
            string coreTempStr = reactorTemperature.ToString("F1") + " K";
            DrawDetailedInfoLine(frame, startY, "Core Temperature:", coreTempStr, tempColor);
            startY += detailedInfoLineSpacing;
            
            // Left Aux Pump.
            IMyBatteryBlock leftPump = GridTerminalSystem.GetBlockWithName(coolantPumpBatteryLeft) as IMyBatteryBlock;
            bool leftPumpOnline = (leftPump != null && leftPump.Enabled);
            double leftFlow = leftPumpOnline ? GetSimulatedFlowRate(coolantPumpBatteryLeft) : 0.0;
            Color leftPumpColor = leftPumpOnline ? new Color(255,255,0) : colorOffline;
            string leftValue = leftPumpOnline ? (leftFlow.ToString("F1") + " L/s") : "Offline";
            DrawDetailedInfoLine(frame, startY, "Left Aux Pump:", leftValue, leftPumpColor);
            startY += detailedInfoLineSpacing;
            
            // Right Aux Pump.
            IMyBatteryBlock rightPump = GridTerminalSystem.GetBlockWithName(coolantPumpBatteryRight) as IMyBatteryBlock;
            bool rightPumpOnline = (rightPump != null && rightPump.Enabled);
            double rightFlow = rightPumpOnline ? GetSimulatedFlowRate(coolantPumpBatteryRight) : 0.0;
            Color rightPumpColor = rightPumpOnline ? new Color(255,255,0) : colorOffline;
            string rightValue = rightPumpOnline ? (rightFlow.ToString("F1") + " L/s") : "Offline";
            DrawDetailedInfoLine(frame, startY, "Right Aux Pump:", rightValue, rightPumpColor);
            startY += detailedInfoLineSpacing;
            
            // Reactor Aux Batteries (storage as current/max).
            string[] batteryNames = new string[] { auxBattery1, auxBattery2, auxBattery3, auxBattery4 };
            for (int i = 0; i < batteryNames.Length; i++) {
                IMyBatteryBlock batt = GridTerminalSystem.GetBlockWithName(batteryNames[i]) as IMyBatteryBlock;
                bool battOnline = (batt != null && batt.Enabled);
                if (battOnline && batt.MaxStoredPower > 0) {
                    double current = batt.CurrentStoredPower;
                    double max = batt.MaxStoredPower;
                    double percent = (current / max) * 100.0;
                    string battValue = $"{current:F2}/{max:F2} MWh";
                    Color battColor = (percent > 75) ? colorOnline : (percent > 35 ? colorWarning : colorOffline);
                    DrawDetailedInfoLine(frame, startY, batteryNames[i] + ":", battValue, battColor);
                } else {
                    DrawDetailedInfoLine(frame, startY, batteryNames[i] + ":", "Offline", colorOffline);
                }
                startY += detailedInfoLineSpacing;
            }
        }
        else if (menuIndex == 1) {
            // "Batteries" detailed view.
            var headerSprite = new MySprite(SpriteType.TEXT, "BATTERIES");
            headerSprite.Position = new Vector2(20, 100);
            headerSprite.Color = Color.Red;
            headerSprite.FontId = "Monospace";
            headerSprite.RotationOrScale = 1.2f;
            headerSprite.Alignment = TextAlignment.LEFT;
            frame.Add(headerSprite);
            
            string[] batteryNames = new string[] { auxBattery1, auxBattery2, auxBattery3, auxBattery4 };
            for (int i = 0; i < batteryNames.Length; i++) {
                IMyBatteryBlock batt = GridTerminalSystem.GetBlockWithName(batteryNames[i]) as IMyBatteryBlock;
                bool online = (batt != null && batt.Enabled);
                
                // Online/Offline status.
                Color statusColor = online ? colorOnline : colorOffline;
                string statusText = online ? "ONLINE" : "OFFLINE";
                DrawDetailedInfoLine(frame, startY, batteryNames[i] + ":", statusText, statusColor);
                startY += detailedInfoLineSpacing;
                
                if (online && batt.MaxStoredPower > 0) {
                    double percent = (batt.CurrentStoredPower / batt.MaxStoredPower) * 100.0;
                    Color storedColor = (percent > 75) ? colorOnline : (percent > 35 ? colorWarning : colorOffline);
                    string storedText = $"{percent:F0}%";
                    DrawDetailedInfoLine(frame, startY, "--Stored:", storedText, storedColor);
                    startY += detailedInfoLineSpacing;
                    
                    double input = batt.CurrentInput;
                    string inputText = input.ToString("F2") + " MW";
                    DrawDetailedInfoLine(frame, startY, "--Input:", inputText, new Color(255,255,0));
                    startY += detailedInfoLineSpacing;
                    
                    double output = batt.CurrentOutput;
                    string outputText = output.ToString("F2") + " MW";
                    DrawDetailedInfoLine(frame, startY, "--Output:", outputText, new Color(255,255,0));
                    startY += detailedInfoLineSpacing;
                }
            }
        }
        else if (menuIndex == 2) {
            // "Reactor" detailed view.
            var headerSprite = new MySprite(SpriteType.TEXT, "REACTOR");
            headerSprite.Position = new Vector2(20, 100);
            headerSprite.Color = Color.Red;
            headerSprite.FontId = "Monospace";
            headerSprite.RotationOrScale = 1.2f;
            headerSprite.Alignment = TextAlignment.LEFT;
            frame.Add(headerSprite);
            
            float startYReactor = detailedInfoStaticStartPos.Y;
            
            // Reactor status.
            string reactorStatus = reactorIsOnline ? "ONLINE" : "OFFLINE";
            Color reactorStatusColor = reactorIsOnline ? colorOnline : colorOffline;
            DrawDetailedInfoLine(frame, startYReactor, "Reactor:", reactorStatus, reactorStatusColor);
            startYReactor += detailedInfoLineSpacing;
            
            // Online since.
            string uptimeStr = reactorIsOnline ? reactorUptime.ToString("F0") + " s" : "0 s";
            DrawDetailedInfoLine(frame, startYReactor, "Online since:", uptimeStr, colorOnline);
            startYReactor += detailedInfoLineSpacing;
            
            // U235 in Reactor.
            string u235Str = reactorU235.ToString("F0") + " kg";
            Color u235Color = (reactorU235 > 0) ? colorOnline : colorOffline;
            DrawDetailedInfoLine(frame, startYReactor, "U235 in Reactor:", u235Str, u235Color);
            startYReactor += detailedInfoLineSpacing;
            
            // Core Temperature.
            int pumpCountReactor = GetSimulatedPumpCount(coolantPumpBatteryLeft, coolantPumpBatteryRight);
            Color tempColor = !reactorIsOnline ? Color.White : (pumpCountReactor < 2 ? Color.Red : Color.Green);
            string coreTempStr = reactorTemperature.ToString("F1") + " K";
            DrawDetailedInfoLine(frame, startYReactor, "Core Temperature:", coreTempStr, tempColor);
            startYReactor += detailedInfoLineSpacing;
            
            // Power Output.
            string pwrOutStr = reactorPowerOutput.ToString("F2") + " MW";
            Color pwrOutColor = (reactorPowerOutput > 0) ? colorOnline : colorOffline;
            DrawDetailedInfoLine(frame, startYReactor, "Power Output:", pwrOutStr, pwrOutColor);
            startYReactor += detailedInfoLineSpacing;
            
            // Containment Field.
            string fieldStr = fieldProjectorOnline ? "ONLINE" : "OFFLINE";
            Color fieldColor = fieldProjectorOnline ? colorOnline : colorOffline;
            DrawDetailedInfoLine(frame, startYReactor, "Containment Field:", fieldStr, fieldColor);
            startYReactor += detailedInfoLineSpacing;
            
            // Centrifuge.
            string rotorStr = rotorSpeed.ToString("F0") + " RPM";
            Color rotorColor = (rotorSpeed > 0) ? colorOnline : colorOffline;
            DrawDetailedInfoLine(frame, startYReactor, "Centrifuge:", rotorStr, rotorColor);
            startYReactor += detailedInfoLineSpacing;
        }
        else {
            string detailText = "Detailed info for: " + menuItems[menuIndex];
            var textSprite = new MySprite(SpriteType.TEXT, detailText);
            textSprite.Position = new Vector2(size.X/2, size.Y/2);
            textSprite.Alignment = TextAlignment.CENTER;
            textSprite.Color = Color.White;
            textSprite.FontId = "Monospace";
            textSprite.RotationOrScale = 0.8f;
            frame.Add(textSprite);
        }
    }
}

// ----------------- DRAWING HELPER FUNCTIONS -----------------
void DrawInfoLine(MySpriteDrawFrame frame, float x, float y, string label, string value, Color labelColor, Color valueColor) {
    var labelSprite = new MySprite(SpriteType.TEXT, label);
    labelSprite.Position = new Vector2(x, y);
    labelSprite.Color = labelColor;
    labelSprite.FontId = "Monospace";
    labelSprite.RotationOrScale = infoFontScaleLabel;
    labelSprite.Alignment = TextAlignment.LEFT;
    frame.Add(labelSprite);
    
    var valueSprite = new MySprite(SpriteType.TEXT, value);
    valueSprite.Position = new Vector2(valueColumnX, y);
    valueSprite.Color = valueColor;
    valueSprite.FontId = "Monospace";
    valueSprite.RotationOrScale = infoFontScaleValue;
    valueSprite.Alignment = TextAlignment.LEFT;
    frame.Add(valueSprite);
}

void DrawDetailedInfoLine(MySpriteDrawFrame frame, float y, string staticText, string variableText, Color variableColor) {
    var staticSprite = new MySprite(SpriteType.TEXT, staticText);
    staticSprite.Position = new Vector2(detailedInfoStaticStartPos.X, y);
    staticSprite.Color = staticLabelColor;
    staticSprite.FontId = "Monospace";
    staticSprite.RotationOrScale = detailedInfoStaticFontSize;
    staticSprite.Alignment = TextAlignment.LEFT;
    frame.Add(staticSprite);
    
    var variableSprite = new MySprite(SpriteType.TEXT, variableText);
    variableSprite.Position = new Vector2(detailedInfoVariableStartPos.X, y);
    variableSprite.Color = variableColor;
    variableSprite.FontId = "Monospace";
    variableSprite.RotationOrScale = detailedInfoVariableFontSize;
    variableSprite.Alignment = TextAlignment.LEFT;
    frame.Add(variableSprite);
}

void DrawMenu(MySpriteDrawFrame frame) {
    if (lcd == null) return;
    Vector2 size = lcd.TextureSize;
    
    for (int i = 0; i < menuItems.Length; i++) {
        Vector2 pos = menuStartPosition + new Vector2(0, menuLineSpacing * i);
        if (i == menuSelectedIndex) {
            float highlightHeight = menuLineSpacing;
            float highlightWidth = size.X;
            Vector2 highlightPos = new Vector2(size.X / 2, pos.Y + highlightHeight / 2);
            var highlightRect = new MySprite(SpriteType.TEXTURE, "SquareSimple");
            highlightRect.Position = highlightPos;
            highlightRect.Size = new Vector2(highlightWidth, highlightHeight);
            highlightRect.Color = new Color(128, 0, 0);
            highlightRect.Alignment = TextAlignment.CENTER;
            frame.Add(highlightRect);
            
            var menuSprite = new MySprite(SpriteType.TEXT, menuItems[i]);
            menuSprite.Position = pos;
            menuSprite.Color = Color.Orange;
            menuSprite.FontId = "Monospace";
            menuSprite.RotationOrScale = menuFontSize;
            menuSprite.Alignment = TextAlignment.CENTER;
            frame.Add(menuSprite);
        } else {
            var menuSprite = new MySprite(SpriteType.TEXT, menuItems[i]);
            menuSprite.Position = pos;
            menuSprite.Color = Color.White;
            menuSprite.FontId = "Monospace";
            menuSprite.RotationOrScale = menuFontSize;
            menuSprite.Alignment = TextAlignment.CENTER;
            frame.Add(menuSprite);
        }
    }
}

void ClearLCD(IMyTextSurface surf) {
    Vector2 size = surf.TextureSize;
    using (var frame = surf.DrawFrame()) {
        var bg = new MySprite(SpriteType.TEXTURE, "SquareSimple", size * 0.5f, size, Color.Black);
        frame.Add(bg);
    }
}

// ----------------- BATTERY/POWER HELPER FUNCTIONS -----------------
int GetSimulatedPumpCount(string name1, string name2) {
    int count = 0;
    IMyBatteryBlock bat1 = GridTerminalSystem.GetBlockWithName(name1) as IMyBatteryBlock;
    IMyBatteryBlock bat2 = GridTerminalSystem.GetBlockWithName(name2) as IMyBatteryBlock;
    if (bat1 != null && bat1.Enabled) count++;
    if (bat2 != null && bat2.Enabled) count++;
    return count;
}

int GetAuxBatteryCount(string n1, string n2, string n3, string n4) {
    int count = 0;
    IMyBatteryBlock b1 = GridTerminalSystem.GetBlockWithName(n1) as IMyBatteryBlock;
    IMyBatteryBlock b2 = GridTerminalSystem.GetBlockWithName(n2) as IMyBatteryBlock;
    IMyBatteryBlock b3 = GridTerminalSystem.GetBlockWithName(n3) as IMyBatteryBlock;
    IMyBatteryBlock b4 = GridTerminalSystem.GetBlockWithName(n4) as IMyBatteryBlock;
    if (b1 != null && b1.Enabled) count++;
    if (b2 != null && b2.Enabled) count++;
    if (b3 != null && b3.Enabled) count++;
    if (b4 != null && b4.Enabled) count++;
    return count;
}

double GetTotalStoredPower() {
    List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
    GridTerminalSystem.GetBlocksOfType(batteries);
    double total = 0;
    foreach (var bat in batteries) {
        total += bat.CurrentStoredPower;
    }
    return total;
}

void ToggleMainControlBattery(double totalPower) {
    IMyBatteryBlock mainControl = GridTerminalSystem.GetBlockWithName(mainControlBatteryName) as IMyBatteryBlock;
    if (mainControl != null) {
        mainControl.Enabled = (totalPower >= storedPowerThreshold);
    }
}

double GetSimulatedFlowRate(string batteryName) {
    IMyBatteryBlock block = GridTerminalSystem.GetBlockWithName(batteryName) as IMyBatteryBlock;
    if (block != null && block.Enabled) {
        double currentFlow;
        if (!simulatedFlowRates.TryGetValue(batteryName, out currentFlow)) {
            currentFlow = pumpBaseFlow;
            simulatedFlowRates[batteryName] = currentFlow;
        }
        double delta = (randomGen.NextDouble() * 2 - 1) * pumpFlowRandomDelta;
        currentFlow += delta;
        currentFlow = Math.Max(pumpFlowMin, Math.Min(pumpFlowMax, currentFlow));
        simulatedFlowRates[batteryName] = currentFlow;
        return currentFlow;
    }
    return 0.0;
}

// ----------------- REACTOR LIGHTS DISPLAY (PB) -----------------
void UpdateReactorLightsDisplay() {
    IMyProgrammableBlock lightsPB = GridTerminalSystem.GetBlockWithName(lightsProgrammableBlockName) as IMyProgrammableBlock;
    if (lightsPB == null) return;
    
    IMyTextSurface surf = lightsPB.GetSurface(0);
    surf.ContentType = ContentType.SCRIPT;
    
    using (var frame = surf.DrawFrame()) {
        Vector2 size = surf.TextureSize;
        var bg = new MySprite(SpriteType.TEXTURE, "SquareSimple", size * 0.5f, size, Color.Black);
        frame.Add(bg);
        
        if (emergencyShutdown) {
            DrawLine(frame, "EMERGENCY", new Vector2(size.X / 2, size.Y / 2 - lightsDisplayLineSpacing), lightsDisplayFontSize, TextAlignment.CENTER, Color.Red);
            DrawLine(frame, "REACTOR", new Vector2(size.X / 2, size.Y / 2), lightsDisplayFontSize, TextAlignment.CENTER, Color.Red);
            DrawLine(frame, "SHUTDOWN", new Vector2(size.X / 2, size.Y / 2 + lightsDisplayLineSpacing), lightsDisplayFontSize, TextAlignment.CENTER, Color.Red);
        } else if (reactorIsOnline) {
            DrawLine(frame, "REACTOR", new Vector2(size.X / 2, size.Y / 2 - lightsDisplayLineSpacing / 2), lightsDisplayFontSize, TextAlignment.CENTER, Color.White);
            DrawLine(frame, "ONLINE", new Vector2(size.X / 2, size.Y / 2 + lightsDisplayLineSpacing / 2), lightsDisplayFontSize, TextAlignment.CENTER, Color.Green);
        } else {
            DrawLine(frame, "REACTOR", new Vector2(size.X / 2, size.Y / 2 - lightsDisplayLineSpacing / 2), lightsDisplayFontSize, TextAlignment.CENTER, Color.White);
            DrawLine(frame, "OFFLINE", new Vector2(size.X / 2, size.Y / 2 + lightsDisplayLineSpacing / 2), lightsDisplayFontSize, TextAlignment.CENTER, Color.Red);
        }
    }
}

void DrawLine(MySpriteDrawFrame frame, string text, Vector2 pos, float scale, TextAlignment alignment, Color color) {
    var sprite = new MySprite(SpriteType.TEXT, text);
    sprite.Position = pos;
    sprite.RotationOrScale = scale;
    sprite.Color = color;
    sprite.Alignment = alignment;
    sprite.FontId = "Monospace";
    frame.Add(sprite);
}

void ResetState() {
    reactorIsOnline = true;
    emergencyShutdown = false;
    timeWithoutPumpsWhileOn = 0f;
    reactorTemperature = 800.0;
    reactorFuel = 100.0;
    reactorUptime = 0.0;
    reactorU235 = 5000.0;
    menuSelectedIndex = 0;
    detailSelectionIndex = 0;
    updateCounter = 0;
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}
