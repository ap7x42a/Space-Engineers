/*******************************************
 * LIFE SUPPORT: BFS SCRIPT W/ PARTIAL STATES
 *
 *  - Gathers O2/H2 gens, O2/H2 tanks, cargo 
 *    from all grids connected by pistons,
 *    rotors, hinges (including mixed sizes).
 *  - Displays status on LCD 3 via sprites.
 *  - Has OFFLINE, PARTIAL, ONLINE states
 *    based on thresholds for device groups.
 *  - Triggers a timer once when everything 
 *    is ONLINE together.
 *  - Approximates O2/H2 production in L/s.
 *  - Detects ICE in cargo on subgrids.
 *
 *  - ALSO displays door-lock states on
 *    Engineering Aux Control LCD 1 (with
 *    independently tunable font size/position).
 *******************************************/

#region SETTINGS

// --- LCD 3 (Life Support) ---
string LCD3_NAME        = "Engineering Aux Control LCD 3";
string TIMER_BLOCK_NAME = "Life Support Online Timer Block";

// Positions, font sizes, spacing for LCD 3
float LCD3_HEADER_FONT_SIZE  = 1.7f;
float LCD3_BODY_FONT_SIZE    = 1.1f;
Vector2 LCD3_HEADER_START_POS = new Vector2(20, 40);
Vector2 LCD3_BODY_START_POS   = new Vector2(40, 120);
float LCD3_LINE_SPACING       = 40f;

// -- Additional Doors Display LCD 1 --
string LCD1_NAME = "Engineering Aux Control LCD 1";

// Positions, font sizes, spacing for LCD 1
// (independent from LCD 3 so you can tune them separately)
float LCD1_BODY_FONT_SIZE  = 1.0f;
Vector2 LCD1_BODY_START_POS = new Vector2(10, 40);
float LCD1_LINE_SPACING     = 30f;

// This is the name of the small-grid PB whose large display we want to manage:
string SMALL_GRID_PB_NAME = "Engineering Aux Control Programmable Block";

// Colors
Color COLOR_OFFLINE       = new Color(255, 0, 0);
Color COLOR_ONLINE        = new Color(0, 255, 0);
Color COLOR_PARTIAL       = new Color(255, 200, 0); // partial state color
Color COLOR_STATIC        = new Color(255, 255, 255);
Color COLOR_PERCENT       = new Color(255, 255, 0);
Color COLOR_FRACTION_OFF  = new Color(255, 0, 0);
Color COLOR_FRACTION_ON   = new Color(0, 255, 0);
Color COLOR_FRACTION_PARTIAL = new Color(255, 200, 0);

// For the door status, we’ll reuse red/green for LOCKED/UNLOCKED
Color COLOR_LOCKED   = new Color(255, 0, 0);
Color COLOR_UNLOCKED = new Color(0, 255, 0);

// Word-wrap margin
float WRAP_MARGIN = 20f;

// Approx capacities for net fill -> L/s logic
const double OXY_CAPACITY_LITERS   = 100000.0;
const double HYDRO_CAPACITY_LITERS = 500000.0;

// Partial threshold (0.0 to 1.0) for sub-systems
float PARTIAL_THRESHOLD = 0.5f;

#endregion

// =========== LIFE SUPPORT FIELDS ===========
IMyTextPanel   _lcd3Block;     // The block for LCD 3 (checks .Enabled)
IMyTextSurface _lcd3Surface;   // The drawing surface for life-support display

// Additional Doors LCD 1
IMyTextPanel   _lcd1Block;     // The block for LCD 1 (checks .Enabled)
IMyTextSurface _lcd1Surface;   // The drawing surface for door display

IMyProgrammableBlock _consolePB;
IMyTextSurface       _consolePB_LargeDisplay;

IMyTimerBlock  _timerBlock;
bool _hasAlreadyTriggeredTimer = false;

// For BFS partial states
Dictionary<long, double> _lastOxyFill   = new Dictionary<long, double>();
Dictionary<long, double> _lastHydroFill = new Dictionary<long, double>();
DateTime _lastUpdateTime = DateTime.Now;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update100; // ~1.6s

    // 1) Grab the small-grid PB and set its large display to TEXT_AND_IMAGE.
    _consolePB = GridTerminalSystem.GetBlockWithName(SMALL_GRID_PB_NAME) as IMyProgrammableBlock;
    if (_consolePB == null)
    {
        Echo($"WARNING: No PB named '{SMALL_GRID_PB_NAME}' found.");
    }
    else
    {
        _consolePB_LargeDisplay = _consolePB.GetSurface(0);
        if (_consolePB_LargeDisplay != null)
        {
            _consolePB_LargeDisplay.ContentType = ContentType.TEXT_AND_IMAGE;
        }
    }

    // 2) Setup LCD 3 for main life-support display
    _lcd3Block = GridTerminalSystem.GetBlockWithName(LCD3_NAME) as IMyTextPanel;
    if (_lcd3Block == null)
    {
        Echo($"ERROR: Cannot find LCD named '{LCD3_NAME}'!");
    }
    else
    {
        _lcd3Surface = _lcd3Block as IMyTextSurface;
        if (_lcd3Surface != null)
        {
            _lcd3Surface.ContentType = ContentType.SCRIPT;
            _lcd3Surface.Script = "";
            _lcd3Surface.Font = "Debug";
        }
    }

    // 3) Setup LCD 1 for door info
    _lcd1Block = GridTerminalSystem.GetBlockWithName(LCD1_NAME) as IMyTextPanel;
    if (_lcd1Block == null)
    {
        Echo($"WARNING: Cannot find LCD named '{LCD1_NAME}'. Door info will be skipped.");
    }
    else
    {
        _lcd1Surface = _lcd1Block as IMyTextSurface;
        if (_lcd1Surface != null)
        {
            _lcd1Surface.ContentType = ContentType.SCRIPT;
            _lcd1Surface.Script = "";
            _lcd1Surface.Font = "Debug";
        }
    }

    // 4) Timer block (optional)
    _timerBlock = GridTerminalSystem.GetBlockWithName(TIMER_BLOCK_NAME) as IMyTimerBlock;
    if (_timerBlock == null)
    {
        Echo($"WARNING: No timer named '{TIMER_BLOCK_NAME}' found.");
    }

    _lastUpdateTime = DateTime.Now;
}

public void Main(string argument, UpdateType updateSource)
{
    // Time since last run
    var now = DateTime.Now;
    double deltaTime = (now - _lastUpdateTime).TotalSeconds;
    if (deltaTime <= 0) deltaTime = 1;
    _lastUpdateTime = now;

    // Gather all connected grids via BFS
    var connectedGrids = GetAllConnectedGrids(Me.CubeGrid);

    //-----------------------------------------------
    // 1) LIFE SUPPORT LOGIC (unchanged from before)
    //-----------------------------------------------
    var o2h2Generators = new List<IMyGasGenerator>();
    {
        var allGens = new List<IMyGasGenerator>();
        GridTerminalSystem.GetBlocksOfType(allGens);
        foreach (var g in allGens)
            if (connectedGrids.Contains(g.CubeGrid))
                o2h2Generators.Add(g);
    }

    var allTanks = new List<IMyGasTank>();
    {
        var allTanksAll = new List<IMyGasTank>();
        GridTerminalSystem.GetBlocksOfType(allTanksAll);
        foreach (var t in allTanksAll)
            if (connectedGrids.Contains(t.CubeGrid))
                allTanks.Add(t);
    }

    var oxyTanks   = new List<IMyGasTank>();
    var hydroTanks = new List<IMyGasTank>();
    foreach (var tank in allTanks)
    {
        if (tank.BlockDefinition.SubtypeName.ToLower().Contains("oxygen"))
            oxyTanks.Add(tank);
        else
            hydroTanks.Add(tank);
    }

    var allInvBlocks = new List<IMyTerminalBlock>();
    {
        var allInvBlocksAll = new List<IMyTerminalBlock>();
        GridTerminalSystem.GetBlocksOfType(allInvBlocksAll, b => b.HasInventory);
        foreach (var b in allInvBlocksAll)
            if (connectedGrids.Contains(b.CubeGrid))
                allInvBlocks.Add(b);
    }

    int genTotal = o2h2Generators.Count;
    int genOn    = 0;
    foreach (var g in o2h2Generators) if (g.Enabled) genOn++;
    int oxyTotal = oxyTanks.Count;
    int oxyOn    = 0;
    foreach (var t in oxyTanks) if (t.Enabled) oxyOn++;
    int hydroTotal = hydroTanks.Count;
    int hydroOn    = 0;
    foreach (var t in hydroTanks) if (t.Enabled) hydroOn++;

    float genFrac   = (genTotal   == 0) ? 1f : (float)genOn   / genTotal;
    float oxyFrac   = (oxyTotal   == 0) ? 1f : (float)oxyOn   / oxyTotal;
    float hydroFrac = (hydroTotal == 0) ? 1f : (float)hydroOn / hydroTotal;

    var genStatus   = EvaluateFraction(genFrac);
    var oxyStatus   = EvaluateFraction(oxyFrac);
    var hydroStatus = EvaluateFraction(hydroFrac);
    SupportStatus overallStatus = (SupportStatus)Math.Min((int)genStatus,
                                         Math.Min((int)oxyStatus, (int)hydroStatus));

    // Net O2/H2 production
    double netOxyLiters = 0.0;
    double netH2Liters  = 0.0;
    foreach (var tank in oxyTanks)
    {
        double currentFill = tank.FilledRatio;
        if (!_lastOxyFill.ContainsKey(tank.EntityId))
            _lastOxyFill[tank.EntityId] = currentFill;

        double diff = currentFill - _lastOxyFill[tank.EntityId];
        double liters = diff * OXY_CAPACITY_LITERS;
        if (liters > 0) netOxyLiters += liters;

        _lastOxyFill[tank.EntityId] = currentFill;
    }
    foreach (var tank in hydroTanks)
    {
        double currentFill = tank.FilledRatio;
        if (!_lastHydroFill.ContainsKey(tank.EntityId))
            _lastHydroFill[tank.EntityId] = currentFill;

        double diff = currentFill - _lastHydroFill[tank.EntityId];
        double liters = diff * HYDRO_CAPACITY_LITERS;
        if (liters > 0) netH2Liters += liters;

        _lastHydroFill[tank.EntityId] = currentFill;
    }
    double oxyLps   = netOxyLiters / deltaTime;
    double hydroLps = netH2Liters / deltaTime;

    // Fill levels
    float totalOxyFill = 0f;
    foreach (var tank in oxyTanks) totalOxyFill += (float)tank.FilledRatio;
    float oxyLevelPct = (oxyTanks.Count == 0 ? 0f : 100f * totalOxyFill / oxyTanks.Count);

    float totalHydroFill = 0f;
    foreach (var tank in hydroTanks) totalHydroFill += (float)tank.FilledRatio;
    float hydroLevelPct = (hydroTanks.Count == 0 ? 0f : 100f * totalHydroFill / hydroTanks.Count);

    // ICE
    double totalIceMass = 0.0;
    MyItemType iceType = MyItemType.MakeOre("Ice");
    foreach (var blk in allInvBlocks)
    {
        for (int i = 0; i < blk.InventoryCount; i++)
        {
            var inv = blk.GetInventory(i);
            if (inv == null) continue;

            var items = new List<MyInventoryItem>();
            inv.GetItems(items);
            foreach (var item in items)
            {
                if (item.Type.Equals(iceType))
                {
                    totalIceMass += (double)item.Amount;
                }
                else
                {
                    string typeId  = item.Type.TypeId.ToString().ToLower();
                    string subId   = item.Type.SubtypeId.ToString().ToLower();
                    if (typeId.Contains("ore") && subId.Contains("ice"))
                    {
                        totalIceMass += (double)item.Amount;
                    }
                }
            }
        }
    }

    // Trigger timer if fully ONLINE
    if (overallStatus == SupportStatus.Online && !_hasAlreadyTriggeredTimer && _timerBlock != null)
    {
        _timerBlock.Trigger();
        _hasAlreadyTriggeredTimer = true;
    }
    if (overallStatus != SupportStatus.Online)
        _hasAlreadyTriggeredTimer = false;

    //-----------------------------------------------
    // 2) Draw on LCD 3 (Life Support info)
    //-----------------------------------------------
    if (_lcd3Surface != null && _lcd3Block != null)
    {
        using (var frame = _lcd3Surface.DrawFrame())
        {
            // Draw a header
            string headerText = "LIFE SUPPORT: " + overallStatus.ToString().ToUpper();
            Color headerColor = ColorForStatus(overallStatus);

            // Use the old approach for measuring/wrapping. 
            // (No changes from your prior logic.)
            DrawSingleLine(
                frame, 
                _lcd3Surface, 
                headerText, 
                headerColor, 
                LCD3_HEADER_FONT_SIZE, 
                LCD3_HEADER_START_POS
            );

            // Build lines
            List<DisplayLine> lines = new List<DisplayLine>();
            Color fractionGenColor   = ColorForFraction(genFrac);
            Color fractionOxyColor   = ColorForFraction(oxyFrac);
            Color fractionHydroColor = ColorForFraction(hydroFrac);

            lines.Add(new DisplayLine("O2/H2 Generators online: ", COLOR_STATIC, LCD3_BODY_FONT_SIZE, $"{genOn}/{genTotal}", fractionGenColor));
            lines.Add(new DisplayLine("Oxygen Tanks online: ",      COLOR_STATIC, LCD3_BODY_FONT_SIZE, $"{oxyOn}/{oxyTotal}", fractionOxyColor));
            lines.Add(new DisplayLine("Hydrogen Tanks online: ",    COLOR_STATIC, LCD3_BODY_FONT_SIZE, $"{hydroOn}/{hydroTotal}", fractionHydroColor));
            lines.Add(new DisplayLine("Oxygen Production: ",        COLOR_STATIC, LCD3_BODY_FONT_SIZE, $"{oxyLps:0.0} L/s", COLOR_PERCENT));
            lines.Add(new DisplayLine("Hydrogen Production: ",      COLOR_STATIC, LCD3_BODY_FONT_SIZE, $"{hydroLps:0.0} L/s", COLOR_PERCENT));
            lines.Add(new DisplayLine("Oxygen Level: ",             COLOR_STATIC, LCD3_BODY_FONT_SIZE, $"{oxyLevelPct:0.0}%", COLOR_PERCENT));
            lines.Add(new DisplayLine("Hydrogen Level: ",           COLOR_STATIC, LCD3_BODY_FONT_SIZE, $"{hydroLevelPct:0.0}%", COLOR_PERCENT));
            lines.Add(new DisplayLine("Remaining Ice Storage: ",    COLOR_STATIC, LCD3_BODY_FONT_SIZE, $"{totalIceMass:0} kg", COLOR_OFFLINE));

            // Draw lines in old style
            Vector2 bodyCursorPos = LCD3_BODY_START_POS;
            float surfaceWidth = _lcd3Surface.SurfaceSize.X;
            foreach (var line in lines)
            {
                string measureFullText = line.Label + line.Value;
                float measureWidth = _lcd3Surface.MeasureStringInPixels(new StringBuilder(measureFullText), "Debug", line.FontSize).X;
                if (measureWidth + WRAP_MARGIN > surfaceWidth)
                {
                    var wrapped = WrapText(_lcd3Surface, measureFullText, line.FontSize, surfaceWidth - WRAP_MARGIN);
                    foreach (string subLine in wrapped)
                    {
                        DrawTextSprite(frame, subLine, line.LabelColor, line.FontSize, bodyCursorPos);
                        bodyCursorPos.Y += LCD3_LINE_SPACING;
                    }
                }
                else
                {
                    DrawTextSprite(frame, line.Label, line.LabelColor, line.FontSize, bodyCursorPos);
                    float labelWidth = _lcd3Surface.MeasureStringInPixels(new StringBuilder(line.Label), "Debug", line.FontSize).X;
                    Vector2 valPos = bodyCursorPos + new Vector2(labelWidth, 0f);
                    DrawTextSprite(frame, line.Value, line.ValueColor, line.FontSize, valPos);
                    bodyCursorPos.Y += LCD3_LINE_SPACING;
                }
            }
        }
    }

    //-----------------------------------------------
    // 3) Scan Doors & Draw on LCD 1 (No header!)
    //    + simpler approach to keep value colors
    //-----------------------------------------------
    if (_lcd1Surface != null && _lcd1Block != null)
    {
        // Gather all doors on connected grids
        var allDoors = new List<IMyDoor>();
        GridTerminalSystem.GetBlocksOfType(allDoors, d => connectedGrids.Contains(d.CubeGrid));

        // Group them by “section”
        Dictionary<string, List<IMyDoor>> sectionToDoors = new Dictionary<string, List<IMyDoor>>();
        foreach (var door in allDoors)
        {
            string sectionName = TryMatchDoorSection(door.CustomName);
            if (sectionName == null) continue; // no recognized keywords

            if (!sectionToDoors.ContainsKey(sectionName))
                sectionToDoors[sectionName] = new List<IMyDoor>();
            sectionToDoors[sectionName].Add(door);
        }

        // Build lines to display
        List<DisplayLine> doorLines = new List<DisplayLine>();

        // We'll sort by section name for a consistent listing
        var keys = new List<string>(sectionToDoors.Keys);
        keys.Sort();

        foreach (var section in keys)
        {
            bool allOn = true; // if any door is toggled off => locked
            foreach (var door in sectionToDoors[section])
            {
                if (!door.Enabled)
                {
                    allOn = false;
                    break;
                }
            }

            string stateText = allOn ? "UNLOCKED" : "LOCKED";
            Color stateColor = allOn ? COLOR_UNLOCKED : COLOR_LOCKED;

            // e.g. "Reactor Room: LOCKED" or "Reactor Room: UNLOCKED"
            doorLines.Add(new DisplayLine($"{section}: ", COLOR_STATIC, LCD1_BODY_FONT_SIZE, stateText, stateColor));
        }

        // Draw on LCD1 (no header).
        using (var frame = _lcd1Surface.DrawFrame())
        {
            // Start from LCD1_BODY_START_POS
            Vector2 cursorPos = LCD1_BODY_START_POS;
            float lcd1Width = _lcd1Surface.SurfaceSize.X;

            // We'll do a simpler approach to preserve value color:
            //  - measure Label + Value. If they don't fit on one line, we move Value to the next line.
            //  - no wrapping each word, so we won't lose color.
            foreach (var line in doorLines)
            {
                // measure label & value independently
                float labelWidth = _lcd1Surface.MeasureStringInPixels(new StringBuilder(line.Label), "Debug", line.FontSize).X;
                float valueWidth = _lcd1Surface.MeasureStringInPixels(new StringBuilder(line.Value), "Debug", line.FontSize).X;
                float neededWidth = labelWidth + valueWidth + WRAP_MARGIN;

                // If they won't fit on one line, put Value on next line
                if (neededWidth > lcd1Width)
                {
                    // Draw label
                    DrawTextSprite(frame, line.Label, line.LabelColor, line.FontSize, cursorPos);
                    cursorPos.Y += LCD1_LINE_SPACING;

                    // Draw value on new line
                    DrawTextSprite(frame, line.Value, line.ValueColor, line.FontSize, cursorPos);
                    cursorPos.Y += LCD1_LINE_SPACING;
                }
                else
                {
                    // They fit on one line: label + value
                    DrawTextSprite(frame, line.Label, line.LabelColor, line.FontSize, cursorPos);
                    Vector2 valPos = cursorPos + new Vector2(labelWidth, 0f);
                    DrawTextSprite(frame, line.Value, line.ValueColor, line.FontSize, valPos);
                    cursorPos.Y += LCD1_LINE_SPACING;
                }
            }
        }
    }

    //-----------------------------------------------
    // 4) PB Large Display content switch
    //-----------------------------------------------
    if (_consolePB_LargeDisplay != null && _lcd3Block != null)
    {
        // If LCD 3 block is enabled => set PB display to SCRIPT, else TEXT_AND_IMAGE
        if (_lcd3Block.Enabled)
            _consolePB_LargeDisplay.ContentType = ContentType.SCRIPT;
        else
            _consolePB_LargeDisplay.ContentType = ContentType.TEXT_AND_IMAGE;
    }
}

//------------------------------------------------------//
//   Door-Section Mapping: classify a door by name      //
//------------------------------------------------------//
string TryMatchDoorSection(string doorName)
{
    // Lowercase for matching
    string lower = doorName.ToLower();

    // These keywords map substring => final display group
    // Adjust as needed
    var map = new Dictionary<string, string>()
    {
        { "reactor",                    "Reactor Room" },
        { "ai core",                    "A.I. Core Room" },
        { "weapons",                    "Weapons Room" },
        { "tunnel",                "Bridge Aux. Tunnel" },
        { "engine room",                "Engine Room" },
        { "upper-level aux. power",     "Upper-Level Aux. Power Room" },
        { "top-level access",           "Top-Level Access" },
        { "main bridge",                "Main Bridge" },
        { "bridge systems control",     "Bridge Systems Control Room" },
        { "bridge aux. control",        "Bridge Aux. Control Rooms" },
        { "mid-ship",                   "Mid-Ship Hallway" },
        { "mid-level",                  "Mid-Level Catwalk" },
        { "front-ship",                "Front-Ship Stairway" },
        { "eng. aux.",                 "Eng. Aux. Hallways" },
        { "exterior thruster walkway",       "Exterior Thruster Walkways" }
    };

    foreach (var kvp in map)
    {
        if (lower.Contains(kvp.Key))
        {
            return kvp.Value;
        }
    }

    return null; // No recognized match
}

//------------------------------------------------------//
//              PARTIAL-STATE HELPER LOGIC              //
//------------------------------------------------------//

enum SupportStatus
{
    Offline = 0,
    Partial = 1,
    Online  = 2
}

SupportStatus EvaluateFraction(float fraction)
{
    if (fraction >= 0.9999f) return SupportStatus.Online;
    if (fraction >= PARTIAL_THRESHOLD) return SupportStatus.Partial;
    return SupportStatus.Offline;
}

Color ColorForStatus(SupportStatus status)
{
    switch(status)
    {
        case SupportStatus.Online:  return COLOR_ONLINE;
        case SupportStatus.Partial: return COLOR_PARTIAL;
        default:                    return COLOR_OFFLINE;
    }
}

Color ColorForFraction(float fraction)
{
    if (fraction >= 0.9999f) return COLOR_FRACTION_ON;
    if (fraction >= PARTIAL_THRESHOLD) return COLOR_FRACTION_PARTIAL;
    return COLOR_FRACTION_OFF;
}

//------------------------------------------------------//
//                 SUBGRID SEARCH (BFS)                //
//------------------------------------------------------//

HashSet<IMyCubeGrid> GetAllConnectedGrids(IMyCubeGrid startGrid)
{
    HashSet<IMyCubeGrid> visited = new HashSet<IMyCubeGrid>();
    Queue<IMyCubeGrid> toVisit   = new Queue<IMyCubeGrid>();
    toVisit.Enqueue(startGrid);

    while (toVisit.Count > 0)
    {
        var grid = toVisit.Dequeue();
        if (!visited.Add(grid)) 
            continue;

        var mechs = new List<IMyMechanicalConnectionBlock>();
        GridTerminalSystem.GetBlocksOfType(mechs, b => b.CubeGrid == grid);
        foreach (var m in mechs)
        {
            var top = m.TopGrid; 
            if (top != null && !visited.Contains(top))
                toVisit.Enqueue(top);
        }

        var attachableHeads = new List<IMyAttachableTopBlock>();
        GridTerminalSystem.GetBlocksOfType(attachableHeads, b => b.CubeGrid == grid);
        foreach (var head in attachableHeads)
        {
            var baseBlock = head.Base;
            if (baseBlock != null && !visited.Contains(baseBlock.CubeGrid))
                toVisit.Enqueue(baseBlock.CubeGrid);
        }
    }
    return visited;
}

//------------------------------------------------------//
//                 LCD DRAWING HELPERS                  //
//------------------------------------------------------//

/// <summary>
/// Draw a single line, word-wrapped, using the older approach
/// (used for LCD 3's header or lines). If text is too long,
/// it calls WrapText and draws multiple lines in the same color.
/// </summary>
void DrawSingleLine(MySpriteDrawFrame frame,
                    IMyTextSurface surf,
                    string text,
                    Color color,
                    float fontSize,
                    Vector2 startPos)
{
    float surfaceWidth = surf.SurfaceSize.X;
    float measureWidth = surf.MeasureStringInPixels(new StringBuilder(text), "Debug", fontSize).X;

    if (measureWidth + WRAP_MARGIN > surfaceWidth)
    {
        var wrappedLines = WrapText(surf, text, fontSize, surfaceWidth - WRAP_MARGIN);
        Vector2 cursor = startPos;
        foreach (var line in wrappedLines)
        {
            DrawTextSprite(frame, line, color, fontSize, cursor);
            cursor.Y += LCD3_LINE_SPACING; 
        }
    }
    else
    {
        DrawTextSprite(frame, text, color, fontSize, startPos);
    }
}

/// <summary>
/// Basic word-wrapping for a text string, returning each line. 
/// This can cause color issues if label+value are merged 
/// into one string. We fix that in LCD 1 by splitting label/value.
/// </summary>
List<string> WrapText(IMyTextSurface surf, string text, float fontSize, float maxWidth)
{
    List<string> result = new List<string>();
    string[] words = text.Split(' ');
    StringBuilder currentLine = new StringBuilder();

    foreach (var w in words)
    {
        string testLine = (currentLine.Length == 0 ? w : currentLine + " " + w);
        float testWidth = surf.MeasureStringInPixels(new StringBuilder(testLine), "Debug", fontSize).X;
        if (testWidth > maxWidth)
        {
            if (currentLine.Length > 0) result.Add(currentLine.ToString());
            currentLine.Clear();
            currentLine.Append(w);
        }
        else
        {
            if (currentLine.Length == 0) currentLine.Append(w);
            else currentLine.Append(" " + w);
        }
    }
    if (currentLine.Length > 0) result.Add(currentLine.ToString());
    return result;
}

/// <summary>
/// Draws a single text sprite (one line).
/// </summary>
void DrawTextSprite(MySpriteDrawFrame frame, string text, Color color, float fontSize, Vector2 position)
{
    var sprite = MySprite.CreateText(text, "Debug", color, fontSize, TextAlignment.LEFT);
    sprite.Position = position;
    frame.Add(sprite);
}

// For storing label + value pairs with colors
struct DisplayLine
{
    public string Label;
    public Color  LabelColor;
    public float  FontSize;
    public string Value;
    public Color  ValueColor;

    public DisplayLine(string label, Color labelColor, float fontSize,
                       string value = "", Color valueColor = default(Color))
    {
        Label       = label;
        LabelColor  = labelColor;
        FontSize    = fontSize;
        Value       = value;
        ValueColor  = (valueColor == default(Color)) ? labelColor : valueColor;
    }
}
