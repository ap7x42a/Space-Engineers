/********************************************************
 * HammercloudLCDs Script - Lights Turn On Exactly Once *
 * (Older C# Version Friendly)                          *
 ********************************************************/

public Program()
{
    // Update at a fixed frequency (~1.6 seconds)
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

// -----------------------------------------------------
// A private field to track whether we've turned lights on
bool lightsHaveBeenTurnedOn = false;

// ====================== ADJUSTABLE SETTINGS ======================

// LCD 2 (Lockdown) Font Sizes
float warningFontSize = 2.0f; 
float noticeFontSize = 1.1f;  

// LCD 2 (Lockdown) Vertical Offsets
float warningYOffset = -230;
float noticeYOffset = -60;

// LCD 2 (Lockdown) Word Wrap Limits
int warningWrapLimit = 18;
int noticeWrapLimit = 39;

// LCD 1 (Event Controller) Font Sizes
float eventHeaderFontSize = 1.8f; 
float eventMessageFontSize = 1.2f; 
float eventStatusFontSize = 1.1f; 

// LCD 1 (Event Controller) Offsets + Wrap Limit
float eventHeaderY = -190f;
float capacitorMsgY = -95f;
float capacitorBanksStatusY = 5f;
float crossLinkStatusY = 45f;
float reserveBatteriesY = 85f;
int eventWrapLimit = 30;

// ====================== LCD NAMES ======================

// LCD 2 names (Lockdown messages)
string[] lockdownLcdNames =
{
    "Left Airlock Info Lcd 2",
    "Right Airlock Info Lcd 2"
};

// LCD 1 names (Event Controller)
string[] eventLcdNames =
{
    "Left Airlock Info Lcd 1",
    "Right Airlock Info Lcd 1"
};

// ====================== MESSAGES (LCD 2) ======================
string warningText = "WARNING: THIS VESSEL IS A RESTRICTED AREA";
string noticeText =
    "This vessel has been declared a restricted area by authority of the Hammercloud installation commander " +
    "in accordance with the provisions of the directive issued by the Secretary of Defense on 2052.315. " +
    "Pursuant to the provisions of Section 21 Internal Security Act of 2032, all persons herein are liable " +
    "to search. Deadly A.I. assisted force is authorized.";

// ====================== MESSAGES (LCD 1) ======================
string eventHeader = "Airlock initialization requires the following:";
string capacitorRequirement = "You must engage the auxiliary capacitor bank cross-links to initialize the airlocks.";
string capacitorOnline = "All Capacitor Banks Online";

// ====================== MAIN ======================
void Main()
{
    // 1) Manage LCD 2 (Lockdown)
    for (int i = 0; i < lockdownLcdNames.Length; i++)
    {
        string lcdName = lockdownLcdNames[i];
        IMyTextPanel lcd = GridTerminalSystem.GetBlockWithName(lcdName) as IMyTextPanel;
        if (lcd == null) continue;

        lcd.ContentType = ContentType.SCRIPT;
        MySpriteDrawFrame frame = lcd.DrawFrame();
        Vector2 screenSize = lcd.SurfaceSize;

        // Big Red Warning (Centered)
        DrawTextOnLCD(frame, warningText, warningFontSize, Color.Red,
                      warningYOffset, screenSize, warningWrapLimit, TextAlignment.CENTER);

        // White Notice (Centered)
        DrawTextOnLCD(frame, noticeText, noticeFontSize, Color.White,
                      noticeYOffset, screenSize, noticeWrapLimit, TextAlignment.CENTER);

        frame.Dispose();
    }

    // 2) Manage capacitor banks: Off => charge, On => user picks mode
    ManageCapacitorBanks();

    // 3) Build logic for LCD 1 (Event Controller)
    bool allCapacitorsOnline = (CountCapacitorsOnline() == 16);
    bool allCrossLinksClosed = (CountCrossLinksClosed() == 16);
    bool allReservesOnline = (CountReserveBatteriesOnline() == 6);

    for (int i = 0; i < eventLcdNames.Length; i++)
    {
        string lcdName = eventLcdNames[i];
        IMyTextPanel lcd = GridTerminalSystem.GetBlockWithName(lcdName) as IMyTextPanel;
        if (lcd == null) continue;

        lcd.ContentType = ContentType.SCRIPT;
        MySpriteDrawFrame frame = lcd.DrawFrame();
        Vector2 screenSize = lcd.SurfaceSize;

        // (white, 1.8 font)
        DrawTextOnLCD(frame, eventHeader, eventHeaderFontSize, Color.White,
                      eventHeaderY, screenSize, eventWrapLimit, TextAlignment.CENTER);

        // Decide capacitor line text & color
        string capacitorStatusText;
        Color capacitorColor;

        // if capacitors aren't all online => "You must engage..."
        // if capacitors are online but cross-links aren't closed => "You must close all cross-links"
        // if both are satisfied => "All Capacitor Banks Online"
        if (!allCapacitorsOnline)
        {
            capacitorStatusText = capacitorRequirement;
            capacitorColor = new Color(255, 69, 0);  // Orange-red
        }
        else if (allCapacitorsOnline && !allCrossLinksClosed)
        {
            capacitorStatusText = "You must close all cross-links";
            capacitorColor = new Color(255, 140, 0); // Orange
        }
        else
        {
            capacitorStatusText = capacitorOnline;   // "All Capacitor Banks Online"
            capacitorColor = Color.Green;
        }

        // (orange-red or green, 1.2 font)
        DrawTextOnLCD(frame, capacitorStatusText, eventMessageFontSize, capacitorColor,
                      capacitorMsgY, screenSize, eventWrapLimit, TextAlignment.CENTER);

        // Capacitor Banks Online: X/16 => red until 16/16, then green
        int capOnlineCount = CountCapacitorsOnline();
        string capacitorBankStatus = "Capacitor Banks Online: " + capOnlineCount + "/16";
        Color capacitorBankColor = (capOnlineCount == 16) ? Color.Green : Color.Red;
        DrawTextOnLCD(frame, capacitorBankStatus, eventStatusFontSize, capacitorBankColor,
                      capacitorBanksStatusY, screenSize, eventWrapLimit, TextAlignment.CENTER);

        // Cross-links closed: X/16 => red until 16/16, then green
        int crossLinkCount = CountCrossLinksClosed();
        string crossLinkStatus = "Cross-links closed: " + crossLinkCount + "/16";
        Color crossLinkColor = (crossLinkCount == 16) ? Color.Green : Color.Red;
        DrawTextOnLCD(frame, crossLinkStatus, eventStatusFontSize, crossLinkColor,
                      crossLinkStatusY, screenSize, eventWrapLimit, TextAlignment.CENTER);

        // Reserve Batteries: X/6 => red until 6/6, then green
        int reserveCount = CountReserveBatteriesOnline();
        string reserveStatus = "Reserve Batteries: " + reserveCount + "/6";
        Color reserveColor = (reserveCount == 6) ? Color.Green : Color.Red;
        DrawTextOnLCD(frame, reserveStatus, eventStatusFontSize, reserveColor,
                      reserveBatteriesY, screenSize, eventWrapLimit, TextAlignment.CENTER);

        // 4) If everything is green, toggle on the airlock button panels. Otherwise, off.
        bool allItemsGreen = allCapacitorsOnline && allCrossLinksClosed && allReservesOnline;
        ToggleButtonPanels(allItemsGreen);

        // Turn the lights on once, if not done yet
        if (allItemsGreen && !lightsHaveBeenTurnedOn)
        {
            IMyLightingBlock leftLight =
                GridTerminalSystem.GetBlockWithName("Left Airlock Red Inset Light") as IMyLightingBlock;
            IMyLightingBlock rightLight =
                GridTerminalSystem.GetBlockWithName("Right Airlock Red Inset Light") as IMyLightingBlock;

            if (leftLight != null) leftLight.Enabled = true;
            if (rightLight != null) rightLight.Enabled = true;

            lightsHaveBeenTurnedOn = true;
        }

        frame.Dispose();
    }
}

// ====================== BUTTON PANEL CONTROL ======================
void ToggleButtonPanels(bool turnOn)
{
    IMyFunctionalBlock leftPanel =
        GridTerminalSystem.GetBlockWithName("Left Airlock Button Panel") as IMyFunctionalBlock;
    IMyFunctionalBlock rightPanel =
        GridTerminalSystem.GetBlockWithName("Right Airlock Button Panel") as IMyFunctionalBlock;

    if (leftPanel != null) leftPanel.Enabled = turnOn;
    if (rightPanel != null) rightPanel.Enabled = turnOn;
}

// ====================== DRAWING & WRAPPING ======================
void DrawTextOnLCD(
    MySpriteDrawFrame frame,
    string text,
    float fontSize,
    Color color,
    float yOffset,
    Vector2 screenSize,
    int maxCharsPerLine,
    TextAlignment alignment
)
{
    // Word-wrap
    string[] lines = WrapText(text, maxCharsPerLine);

    float lineOffset = yOffset;
    for (int i = 0; i < lines.Length; i++)
    {
        string line = lines[i];
        float xPosition = screenSize.X / 2;
        if (alignment == TextAlignment.LEFT) xPosition = 10;

        MySprite sprite = new MySprite();
        sprite.Type = SpriteType.TEXT;
        sprite.Data = line;
        sprite.Position = new Vector2(xPosition, (screenSize.Y / 2) + lineOffset);
        sprite.RotationOrScale = fontSize;
        sprite.Color = color;
        sprite.Alignment = alignment;
        sprite.FontId = "White";

        frame.Add(sprite);
        lineOffset += fontSize * 20; // line spacing
    }
}

string[] WrapText(string text, int maxCharsPerLine)
{
    List<string> lines = new List<string>();
    string[] words = text.Split(' ');

    string currentLine = "";
    for (int i = 0; i < words.Length; i++)
    {
        string word = words[i];
        if ((currentLine + word).Length > maxCharsPerLine)
        {
            lines.Add(currentLine.Trim());
            currentLine = word + " ";
        }
        else
        {
            currentLine += word + " ";
        }
    }
    if (!string.IsNullOrEmpty(currentLine))
    {
        lines.Add(currentLine.Trim());
    }

    return lines.ToArray();
}

// ====================== CAPACITOR & BATTERY MGMT ======================
void ManageCapacitorBanks()
{
    // If battery is toggled OFF => set it to Recharge
    // If battery is toggled ON  => user can pick mode
    for (int i = 1; i <= 16; i++)
    {
        IMyBatteryBlock battery =
            GridTerminalSystem.GetBlockWithName("Capacitor Bank Battery " + i) as IMyBatteryBlock;
        if (battery == null) continue;

        if (!battery.Enabled)
        {
            battery.ChargeMode = ChargeMode.Recharge;
        }
        // else: user can choose the mode
    }
}

int CountCapacitorsOnline()
{
    int count = 0;
    for (int i = 1; i <= 16; i++)
    {
        IMyBatteryBlock battery =
            GridTerminalSystem.GetBlockWithName("Capacitor Bank Battery " + i) as IMyBatteryBlock;
        if (battery != null && battery.Enabled)
        {
            count++;
        }
    }
    return count;
}

int CountCrossLinksClosed()
{
    int count = 0;
    for (int i = 1; i <= 16; i++)
    {
        IMyBatteryBlock battery =
            GridTerminalSystem.GetBlockWithName("Capacitor Bank Battery " + i) as IMyBatteryBlock;
        if (battery != null && battery.ChargeMode == ChargeMode.Discharge)
        {
            count++;
        }
    }
    return count;
}

int CountReserveBatteriesOnline()
{
    string[] reserveBatteries =
    {
        "Large Airlock Battery Left 1 - EmergencyPower",
        "Large Airlock Battery Left 2 - EmergencyPower",
        "Large Airlock Battery Left 3 - EmergencyPower",
        "Large Airlock Battery Right 1 - EmergencyPower",
        "Large Airlock Battery Right 2 - EmergencyPower",
        "Large Airlock Battery Right 3 - EmergencyPower"
    };

    int count = 0;
    for (int i = 0; i < reserveBatteries.Length; i++)
    {
        string name = reserveBatteries[i];
        IMyBatteryBlock battery =
            GridTerminalSystem.GetBlockWithName(name) as IMyBatteryBlock;
        if (battery != null && battery.Enabled)
        {
            count++;
        }
    }
    return count;
}
