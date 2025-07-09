// Player Editable Settings
string LeftPanelName = "Left Entrance Inset Button Panel";
string RightPanelName = "Right Entrance Inset Button Panel";
string LeftPanelName2 = "Left Entrance Inset Button Panel 2";
string LeftPanelName3 = "Left Entrance Inset Button Panel 3";
string RightPanelName2 = "Right Entrance Inset Button Panel 2";
string RightPanelName3 = "Right Entrance Inset Button Panel 3";
string LeftConnectorName = "Left Inset Connector";
string RightConnectorName = "Right Inset Connector";
string GravityGeneratorName = "Gravity Generator";

// Font Sizes
float HeadlineFontSize = 0.6f;
float MessageFontSize = 0.4f;
float LabelFontSize = 0.4f;
float ValueFontSize = 0.4f;
float CommandFontSize = 0.4f;

// Text Positions
Vector2 HeadlinePosition = new Vector2(125, 35);
Vector2 MessagePosition = new Vector2(10, 55);
Vector2 CrewLabelPosition = new Vector2(10, 100);
Vector2 CrewValuePosition = new Vector2(110, 100);
Vector2 HydrogenLabelPosition = new Vector2(10, 110);
Vector2 HydrogenValuePosition = new Vector2(110, 110);
Vector2 OxygenLabelPosition = new Vector2(10, 120);
Vector2 OxygenValuePosition = new Vector2(110, 120);
Vector2 GravityLabelPosition = new Vector2(10, 130);
Vector2 GravityValuePosition = new Vector2(110, 130);
Vector2 LifeSupportLabelPosition = new Vector2(10, 140);
Vector2 LifeSupportValuePosition = new Vector2(110, 140);
Vector2 BatteryLabelPosition = new Vector2(10, 150);
Vector2 BatteryValuePosition = new Vector2(110, 150);
Vector2 DockedLabelPosition = new Vector2(10, 160);
Vector2 DockedValuePosition = new Vector2(110, 160);
Vector2 CommandPosition = new Vector2(10, 180);
Vector2 DangerSpritePosition = new Vector2(200, 135); // Adjustable Position
Vector2 DangerSpriteSize = new Vector2(70, 70);    // Adjustable Size

// Script Variables
IMyButtonPanel leftPanel;
IMyButtonPanel rightPanel;
IMyButtonPanel leftPanel2;
IMyButtonPanel leftPanel3;
IMyButtonPanel rightPanel2;
IMyButtonPanel rightPanel3;
IMyShipConnector leftConnector;
IMyShipConnector rightConnector;
List<IMyGasTank> hydrogenTanks = new List<IMyGasTank>();
List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
IMyGravityGenerator gravityGenerator;
List<IMyGasTank> oxygenTanks = new List<IMyGasTank>();

bool showDanger = true;

// Panel-to-Battery Mapping
Dictionary<string, string> panelBatteryMap = new Dictionary<string, string>
{
    { "Left Entrance Inset Button Panel", "Large Airlock Battery Left 1 - EmergencyPower" },
    { "Right Entrance Inset Button Panel", "Large Airlock Battery Right 1 - EmergencyPower" },
    { "Left Entrance Inset Button Panel 2", "Large Airlock Battery Left 2 - EmergencyPower" },
    { "Left Entrance Inset Button Panel 3", "Large Airlock Battery Left 3 - EmergencyPower" },
    { "Right Entrance Inset Button Panel 2", "Large Airlock Battery Right 2 - EmergencyPower" },
    { "Right Entrance Inset Button Panel 3", "Large Airlock Battery Right 3 - EmergencyPower" }
};

Dictionary<string, IMyButtonPanel> buttonPanels = new Dictionary<string, IMyButtonPanel>();
Dictionary<string, IMyBatteryBlock> panelBatteries = new Dictionary<string, IMyBatteryBlock>();

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;

    InitializeBlocks();
}

void InitializeBlocks()
{

    // Attempt to locate each button panel and match it to the correct battery
    foreach (var pair in panelBatteryMap)
    {
        // Locate the button panel
        var panel = GridTerminalSystem.GetBlockWithName(pair.Key) as IMyButtonPanel;
        // Locate the battery using its full name
        var battery = GridTerminalSystem.GetBlockWithName(pair.Value) as IMyBatteryBlock;

        // Store the panel and matched battery if both are found
        if (panel != null && battery != null)
        {
            buttonPanels[pair.Key] = panel;
            panelBatteries[pair.Key] = battery;

            Echo($"Assigned Panel '{panel.CustomName}' to Battery '{battery.CustomName}'");
        }
        else
        {
            if (panel == null)
                Echo($"Error: Panel '{pair.Key}' not found.");
            if (battery == null)
                Echo($"Error: Battery '{pair.Value}' not found.");
        }
    }

    // Initialize other blocks as per the original script
    if (leftPanel == null)
        leftPanel = GridTerminalSystem.GetBlockWithName(LeftPanelName) as IMyButtonPanel;
    if (rightPanel == null)
        rightPanel = GridTerminalSystem.GetBlockWithName(RightPanelName) as IMyButtonPanel;
    if (leftPanel2 == null)
        leftPanel2 = GridTerminalSystem.GetBlockWithName(LeftPanelName2) as IMyButtonPanel;
    if (leftPanel3 == null)
        leftPanel3 = GridTerminalSystem.GetBlockWithName(LeftPanelName3) as IMyButtonPanel;
    if (rightPanel2 == null)
        rightPanel2 = GridTerminalSystem.GetBlockWithName(RightPanelName2) as IMyButtonPanel;
    if (rightPanel3 == null)
        rightPanel3 = GridTerminalSystem.GetBlockWithName(RightPanelName3) as IMyButtonPanel;

    if (leftConnector == null)
        leftConnector = GridTerminalSystem.GetBlockWithName(LeftConnectorName) as IMyShipConnector;

    if (rightConnector == null)
        rightConnector = GridTerminalSystem.GetBlockWithName(RightConnectorName) as IMyShipConnector;

    hydrogenTanks.Clear();
    oxygenTanks.Clear();
    batteries.Clear();

    GridTerminalSystem.GetBlocksOfType(hydrogenTanks, tank => tank.BlockDefinition.SubtypeId.Contains("Hydrogen"));
    GridTerminalSystem.GetBlocksOfType(oxygenTanks, tank => tank.BlockDefinition.SubtypeId.Contains("Oxygen"));
    GridTerminalSystem.GetBlocksOfType(batteries, battery => battery.CustomName.Contains("EmergencyPower"));
    gravityGenerator = GridTerminalSystem.GetBlockWithName(GravityGeneratorName) as IMyGravityGenerator;
}

void Main(string argument, UpdateType updateSource)
{
    foreach (var pair in panelBatteryMap)
    {
        if (!buttonPanels.ContainsKey(pair.Key) || !panelBatteries.ContainsKey(pair.Key))
        {
            Echo($"Missing panel or battery: {pair.Key}");
            continue;
        }

        var panel = buttonPanels[pair.Key];
        var battery = panelBatteries[pair.Key];

        // Unified function to handle both sprite and message
        UpdatePanelDisplay(panel, battery);
    }
}

void UpdatePanelDisplay(IMyButtonPanel panel, IMyBatteryBlock battery)
{
    var surfaceProvider = panel as IMyTextSurfaceProvider;
    if (surfaceProvider == null || surfaceProvider.SurfaceCount <= 0)
    {
        Echo($"Error: {panel.CustomName} does not support surfaces.");
        return;
    }

    var surface = surfaceProvider.GetSurface(0); // Access the first surface
    surface.ContentType = ContentType.SCRIPT;

    using (var frame = surface.DrawFrame())
    {
        // Headline
        var title = MySprite.CreateText("ACTIVATE EMERGENCY RESERVES", "Debug", Color.Red, HeadlineFontSize, TextAlignment.CENTER);
        title.Position = HeadlinePosition;
        frame.Add(title);

        // Emergency message
        DrawWrappedText(
            "This Hammercloud vessel has been disabled or has received damage. Activate the emergency power reserve systems located at each Inset terminal in the engineering decks.",
            MessagePosition,
            Color.Orange,
            MessageFontSize,
            TextAlignment.LEFT,
            frame,
            surface,
            surface.SurfaceSize.X - 20
        );

        // Stats
        AddStatsToFrame(frame, surface);

        // Command Options
        DrawWrappedText(
            "1. Toggle Battery 1\n2. Toggle Gravity (requires two batteries))\n3. Activate Life Support (requires O2 generators)",
            CommandPosition,
            new Color(70, 130, 180),
            CommandFontSize,
            TextAlignment.LEFT,
            frame,
            surface,
            surface.SurfaceSize.X - 20
        );

        // Sprite Based on Battery State
        var sprite = new MySprite
        {
            Type = SpriteType.TEXTURE,
            Data = battery.Enabled ? "IconEnergy" : "Danger",
            Position = DangerSpritePosition,
            Size = DangerSpriteSize,
            Color = battery.Enabled ? Color.Green : Color.Red,
            Alignment = TextAlignment.CENTER
        };
        frame.Add(sprite);
    }

    Echo($"Updated {panel.CustomName}: {(battery.Enabled ? "IconEnergy" : "Danger")}");
}

void Save()
{
    // Required method for the programmable block to run in looped or triggered mode
}

void DisplayEmergencyMessageForPanel(IMyButtonPanel panel)
{
    if (panel == null)
    {
        Echo("Panel is missing!");
        return;
    }

    var surfaceProvider = panel as IMyTextSurfaceProvider;
    if (surfaceProvider == null || surfaceProvider.SurfaceCount <= 0)
    {
        Echo($"Error: {panel.CustomName} does not support surfaces.");
        return;
    }

    var surface = surfaceProvider.GetSurface(0);
    surface.ContentType = ContentType.SCRIPT;

    using (var frame = surface.DrawFrame())
    {
        // Headline
        var title = MySprite.CreateText("ACTIVATE EMERGENCY RESERVES", "Debug", Color.Red, HeadlineFontSize, TextAlignment.CENTER);
        title.Position = HeadlinePosition;
        frame.Add(title);

        // Main message
        DrawWrappedText(
            "This Hammercloud vessel has been disabled or has received damage. Activate the emergency power reserve systems located at each Inset terminal in the engineering decks.",
            MessagePosition,
            Color.Orange,
            MessageFontSize,
            TextAlignment.LEFT,
            frame,
            surface,
            surface.SurfaceSize.X - 20 // Adjust for padding
        );
    }
}

void DisplayEmergencyMessage(IMyTextSurfaceProvider panel, int surfaceIndex)
{
    if (panel != null && surfaceIndex < panel.SurfaceCount)
    {
        var surface = panel.GetSurface(surfaceIndex);
        if (surface != null)
        {
            surface.ContentType = ContentType.SCRIPT;
            var frame = surface.DrawFrame();

            // Headline
            var title = MySprite.CreateText("ACTIVATE EMERGENCY RESERVES", "Debug", Color.Red, HeadlineFontSize, TextAlignment.CENTER);
            title.Position = HeadlinePosition;
            frame.Add(title);

            // Danger Sprite
            if (showDanger)
            {
                var dangerSprite = new MySprite(
                    SpriteType.TEXTURE,
                    "Danger",
                    DangerSpritePosition,
                    DangerSpriteSize,
                    Color.Red,
                    null,
                    TextAlignment.CENTER
                );
                frame.Add(dangerSprite);
            }

            // Main message
            DrawWrappedText(
                "This Hammercloud vessel has been disabled or has received damage. Activate the emergency power reserve systems located at each Inset terminal in the engineering decks.",
                MessagePosition,
                Color.Orange,
                MessageFontSize,
                TextAlignment.LEFT,
                frame,
                surface,
                surface.SurfaceSize.X - 20 // Adjust for padding
            );

            frame.Dispose();
        }
    }
}

void AddStatsToFrame(MySpriteDrawFrame frame, IMyTextSurface surface)
{
    // Docked status
    string dockedSide = "";
    if (leftConnector != null && leftConnector.Status == MyShipConnectorStatus.Connected)
        dockedSide = "Left";
    else if (rightConnector != null && rightConnector.Status == MyShipConnectorStatus.Connected)
        dockedSide = "Right";

    if (!string.IsNullOrEmpty(dockedSide))
    {
        var dockedText = MySprite.CreateText($"Ship docked: ", "Debug", Color.White, LabelFontSize, TextAlignment.LEFT);
        dockedText.Position = DockedLabelPosition;
        frame.Add(dockedText);

        var dockedSideText = MySprite.CreateText(dockedSide, "Debug", Color.Green, ValueFontSize, TextAlignment.LEFT);
        dockedSideText.Position = DockedValuePosition;
        frame.Add(dockedSideText);
    }

    // Crew onboard
    var crewText = MySprite.CreateText("Crew Onboard: ", "Debug", Color.White, LabelFontSize, TextAlignment.LEFT);
    crewText.Position = CrewLabelPosition;
    frame.Add(crewText);

    var crewCountText = MySprite.CreateText("1", "Debug", Color.Green, ValueFontSize, TextAlignment.LEFT);
    crewCountText.Position = CrewValuePosition;
    frame.Add(crewCountText);

    // Hydrogen reserves
    float totalHydrogen = 0;
    float maxHydrogen = 0;
    foreach (var tank in hydrogenTanks)
    {
        totalHydrogen += (float)tank.FilledRatio;
        maxHydrogen++;
    }
    float hydrogenPercentage = (maxHydrogen > 0) ? (totalHydrogen / maxHydrogen) * 100 : 0;
    var hydrogenText = MySprite.CreateText("Hydrogen Reserves: ", "Debug", Color.White, LabelFontSize, TextAlignment.LEFT);
    hydrogenText.Position = HydrogenLabelPosition;
    frame.Add(hydrogenText);

    var hydrogenPercentageText = MySprite.CreateText($"{hydrogenPercentage:0.0}%", "Debug", Color.Green, ValueFontSize, TextAlignment.LEFT);
    hydrogenPercentageText.Position = HydrogenValuePosition;
    frame.Add(hydrogenPercentageText);

    // Oxygen reserves
    float totalOxygen = 0;
    float maxOxygen = 0;
    foreach (var tank in oxygenTanks)
    {
        totalOxygen += (float)tank.FilledRatio;
        maxOxygen++;
    }
    float oxygenPercentage = (maxOxygen > 0) ? (totalOxygen / maxOxygen) * 100 : 0;
    var oxygenText = MySprite.CreateText("Oxygen Reserves: ", "Debug", Color.White, LabelFontSize, TextAlignment.LEFT);
    oxygenText.Position = OxygenLabelPosition;
    frame.Add(oxygenText);

    var oxygenPercentageText = MySprite.CreateText($"{oxygenPercentage:0.0}%", "Debug", Color.Green, ValueFontSize, TextAlignment.LEFT);
    oxygenPercentageText.Position = OxygenValuePosition;
    frame.Add(oxygenPercentageText);

    // Gravity generator
    string gravityStatus = gravityGenerator != null && gravityGenerator.IsWorking ? "Online" : "Offline";
    Color gravityColor = gravityGenerator != null && gravityGenerator.IsWorking ? Color.Green : Color.Red;

    var gravityText = MySprite.CreateText("Gravity Generator: ", "Debug", Color.White, LabelFontSize, TextAlignment.LEFT);
    gravityText.Position = GravityLabelPosition;
    frame.Add(gravityText);

    var gravityStatusText = MySprite.CreateText(gravityStatus, "Debug", gravityColor, ValueFontSize, TextAlignment.LEFT);
    gravityStatusText.Position = GravityValuePosition;
    frame.Add(gravityStatusText);

    // Life support
    bool oxygenOnline = oxygenTanks.Count > 0 && oxygenTanks.TrueForAll(t => t.Enabled);
    string lifeSupportStatus = oxygenOnline ? "Online" : "Offline";
    Color lifeSupportColor = oxygenOnline ? Color.Green : Color.Red;

    var lifeSupportText = MySprite.CreateText("Life Support: ", "Debug", Color.White, LabelFontSize, TextAlignment.LEFT);
    lifeSupportText.Position = LifeSupportLabelPosition;
    frame.Add(lifeSupportText);

    var lifeSupportStatusText = MySprite.CreateText(lifeSupportStatus, "Debug", lifeSupportColor, ValueFontSize, TextAlignment.LEFT);
    lifeSupportStatusText.Position = LifeSupportValuePosition;
    frame.Add(lifeSupportStatusText);

    // Emergency batteries
    int activeBatteries = batteries.Count(b => b.Enabled);
    var batteryText = MySprite.CreateText("Batteries Online: ", "Debug", Color.White, LabelFontSize, TextAlignment.LEFT);
    batteryText.Position = BatteryLabelPosition;
    frame.Add(batteryText);

    var batteryCountText = MySprite.CreateText($"{activeBatteries}/{batteries.Count}", "Debug", activeBatteries > 0 ? Color.Green : Color.Red, ValueFontSize, TextAlignment.LEFT);
    batteryCountText.Position = BatteryValuePosition;
    frame.Add(batteryCountText);

    // Command Options
    DrawWrappedText(
        "1. Toggle Battery 1\n2. Toggle Gravity (requires two batteries))\n3. Activate Life Support (requires O2 generators)",
        CommandPosition,
        new Color(70, 130, 180), // Custom color between cyan and blue
        CommandFontSize,
        TextAlignment.LEFT,
        frame,
        surface,
        surface.SurfaceSize.X - 20
    );
}

void DrawWrappedText(string text, Vector2 position, Color color, float fontSize, TextAlignment alignment, MySpriteDrawFrame frame, IMyTextSurface surface, float surfaceWidth)
{
    var words = text.Split(' ');
    var currentLine = "";
    var lineHeight = 20 * fontSize; // Approximate line height
    var yOffset = 0f;

    foreach (var word in words)
    {
        var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
        var textSize = surface.MeasureStringInPixels(new StringBuilder(testLine), "Debug", fontSize);

        if (textSize.X > surfaceWidth)
        {
            // Draw current line
            var sprite = MySprite.CreateText(currentLine, "Debug", color, fontSize, alignment);
            sprite.Position = position + new Vector2(0, yOffset);
            frame.Add(sprite);

            currentLine = word; // Start a new line
            yOffset += lineHeight;
        }
        else
        {
            currentLine = testLine;
        }
    }

    // Draw the last line
    if (!string.IsNullOrEmpty(currentLine))
    {
        var sprite = MySprite.CreateText(currentLine, "Debug", color, fontSize, alignment);
        sprite.Position = position + new Vector2(0, yOffset);
        frame.Add(sprite);
    }
}
