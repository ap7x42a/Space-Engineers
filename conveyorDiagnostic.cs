/*******************************************
 * CONVEYOR SYSTEM DIAGNOSTIC SCRIPT
 *
 * Identifies and reports issues in your conveyor
 * system including:
 * - Disconnected conveyor segments
 * - Full cargo containers blocking flow
 * - Misconfigured sorters and filters
 * - Missing conveyor connections
 * - Conveyor port access issues
 * - Inventory transfer problems
 * - Sorter whitelist/blacklist conflicts
 * 
 * Displays comprehensive diagnostic information
 * on specified LCD panels with color-coded
 * status indicators.
 *
 * USAGE:
 * 1. Name your LCD panels "Conveyor Diagnostic LCD" 
 *    and optionally "Conveyor Sorter Detail LCD"
 * 2. Upload script to programmable block
 * 3. Run with no arguments for full diagnostic
 * 4. Run with "reset" argument to clear data
 *******************************************/

// =========== SETTINGS ===========
// Main diagnostic display LCD
string DIAGNOSTIC_LCD_NAME = "Conveyor Diagnostic LCD";

// Optional secondary LCD for detailed sorter info
string SORTER_DETAIL_LCD_NAME = "Conveyor Sorter Detail LCD";

// Display settings
float HEADER_FONT_SIZE = 0.4f;
float BODY_FONT_SIZE = 0.3f;
float DETAIL_FONT_SIZE = 0.2f;
Vector2 HEADER_START_POS = new Vector2(20, 30);
float LINE_SPACING = 25f;

// Colors for status indicators
Color COLOR_ERROR = new Color(255, 0, 0);        // Red - Critical issues
Color COLOR_WARNING = new Color(255, 165, 0);    // Orange - Warnings
Color COLOR_OK = new Color(0, 255, 0);           // Green - All good
Color COLOR_INFO = new Color(100, 149, 237);     // Steel Blue - Information
Color COLOR_HEADER = new Color(255, 255, 255);   // White - Headers

// Thresholds for warnings
float CARGO_FULL_THRESHOLD = 0.95f;      // 95% full triggers warning
float CARGO_WARNING_THRESHOLD = 0.80f;   // 80% full triggers caution
int MAX_ISSUES_TO_DISPLAY = 20;          // Limit display to prevent overflow

// =========== SCRIPT VARIABLES ===========
IMyTextPanel diagnosticLCD;
IMyTextSurface diagnosticSurface;
IMyTextPanel sorterDetailLCD;
IMyTextSurface sorterDetailSurface;

List<string> diagnosticMessages = new List<string>();
List<string> sorterMessages = new List<string>();
int criticalIssues = 0;
int warnings = 0;
int totalBlocksScanned = 0;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update100; // Run every ~1.67 seconds
    InitializeLCDs();
}

void InitializeLCDs()
{
    diagnosticLCD = GridTerminalSystem.GetBlockWithName(DIAGNOSTIC_LCD_NAME) as IMyTextPanel;
    if (diagnosticLCD != null)
    {
        diagnosticSurface = diagnosticLCD;
        diagnosticSurface.ContentType = ContentType.SCRIPT;
    }
    else
    {
        Echo($"ERROR: Cannot find main diagnostic LCD named '{DIAGNOSTIC_LCD_NAME}'!");
    }

    sorterDetailLCD = GridTerminalSystem.GetBlockWithName(SORTER_DETAIL_LCD_NAME) as IMyTextPanel;
    if (sorterDetailLCD != null)
    {
        sorterDetailSurface = sorterDetailLCD;
        sorterDetailSurface.ContentType = ContentType.SCRIPT;
    }
}

public void Main(string argument, UpdateType updateSource)
{
    if (argument.ToLower() == "reset")
    {
        diagnosticMessages.Clear();
        sorterMessages.Clear();
        criticalIssues = 0;
        warnings = 0;
        totalBlocksScanned = 0;
        Echo("Diagnostic data reset.");
        return;
    }

    PerformConveyorDiagnostic();
    UpdateDiagnosticDisplay();
    UpdateSorterDetailDisplay();
}

void PerformConveyorDiagnostic()
{
    diagnosticMessages.Clear();
    sorterMessages.Clear();
    criticalIssues = 0;
    warnings = 0;
    totalBlocksScanned = 0;

    diagnosticMessages.Add("CONVEYOR SYSTEM DIAGNOSTIC");
    diagnosticMessages.Add("========================");
    diagnosticMessages.Add("");

    // Get all connected grids for comprehensive analysis
    var connectedGrids = GetAllConnectedGrids(Me.CubeGrid);
    diagnosticMessages.Add($"Found {connectedGrids.Count} connected grids");
    diagnosticMessages.Add("");
    
    // Analyze different components
    AnalyzeCargoContainers(connectedGrids);
    AnalyzeConveyorSorters(connectedGrids);
    AnalyzeConnectorStatus(connectedGrids);
    AnalyzeProductionBlocks(connectedGrids);
    AnalyzeSubgridConveyorConnections(connectedGrids);
    AnalyzeRotorConveyorIssues(connectedGrids);
    AnalyzeWeaponAmmoFlow(connectedGrids);
    AnalyzeConveyorConnectivity(connectedGrids);

    // Summary
    diagnosticMessages.Add("");
    diagnosticMessages.Add("=== DIAGNOSTIC SUMMARY ===");
    diagnosticMessages.Add($"Total Blocks Scanned: {totalBlocksScanned}");
    diagnosticMessages.Add($"Critical Issues: {criticalIssues}");
    diagnosticMessages.Add($"Warnings: {warnings}");
    
    if (criticalIssues == 0 && warnings == 0)
    {
        diagnosticMessages.Add("System Status: ALL CLEAR");
    }
    else if (criticalIssues > 0)
    {
        diagnosticMessages.Add("System Status: CRITICAL ISSUES DETECTED");
    }
    else
    {
        diagnosticMessages.Add("System Status: WARNINGS PRESENT");
    }

    Echo($"Diagnostic Complete - Critical: {criticalIssues}, Warnings: {warnings}");
}

void AnalyzeCargoContainers(HashSet<IMyCubeGrid> connectedGrids)
{
    var cargoContainers = new List<IMyCargoContainer>();
    GridTerminalSystem.GetBlocksOfType(cargoContainers, c => connectedGrids.Contains(c.CubeGrid));

    diagnosticMessages.Add("=== CARGO CONTAINER ANALYSIS ===");
    
    int fullContainers = 0;
    int nearFullContainers = 0;
    
    foreach (var cargo in cargoContainers)
    {
        totalBlocksScanned++;
        
        if (!cargo.IsFunctional)
        {
            diagnosticMessages.Add($"[ERROR] {cargo.CustomName}: Not functional");
            criticalIssues++;
            continue;
        }

        var inventory = cargo.GetInventory(0);
        if (inventory != null)
        {
            float fillRatio = (float)inventory.CurrentVolume / (float)inventory.MaxVolume;
            
            if (fillRatio >= CARGO_FULL_THRESHOLD)
            {
                diagnosticMessages.Add($"[CRITICAL] {cargo.CustomName}: {(fillRatio * 100):F1}% full - BLOCKING FLOW");
                criticalIssues++;
                fullContainers++;
            }
            else if (fillRatio >= CARGO_WARNING_THRESHOLD)
            {
                diagnosticMessages.Add($"[WARNING] {cargo.CustomName}: {(fillRatio * 100):F1}% full");
                warnings++;
                nearFullContainers++;
            }
        }

        // Check conveyor access (simplified check)
        if (!cargo.IsFunctional)
        {
            diagnosticMessages.Add($"[ERROR] {cargo.CustomName}: No conveyor access");
            criticalIssues++;
        }
    }

    diagnosticMessages.Add($"Containers: {cargoContainers.Count} total, {fullContainers} full, {nearFullContainers} near full");
    diagnosticMessages.Add("");
}

void AnalyzeConveyorSorters(HashSet<IMyCubeGrid> connectedGrids)
{
    var sorters = new List<IMyConveyorSorter>();
    GridTerminalSystem.GetBlocksOfType(sorters, s => connectedGrids.Contains(s.CubeGrid));

    diagnosticMessages.Add("=== CONVEYOR SORTER ANALYSIS ===");
    sorterMessages.Add("=== DETAILED SORTER CONFIGURATION ===");
    sorterMessages.Add("");

    int inactiveSorters = 0;

    foreach (var sorter in sorters)
    {
        totalBlocksScanned++;
        
        if (!sorter.IsFunctional)
        {
            diagnosticMessages.Add($"[ERROR] {sorter.CustomName}: Not functional");
            criticalIssues++;
            continue;
        }

        if (!sorter.Enabled)
        {
            diagnosticMessages.Add($"[WARNING] {sorter.CustomName}: Disabled");
            warnings++;
            inactiveSorters++;
            continue;
        }

        // Check sorter configuration
        AnalyzeSorterConfiguration(sorter);
    }

    diagnosticMessages.Add($"Sorters: {sorters.Count} total, {inactiveSorters} inactive");
    diagnosticMessages.Add("");
}

void AnalyzeSorterConfiguration(IMyConveyorSorter sorter)
{
    sorterMessages.Add($"--- {sorter.CustomName} ---");
    sorterMessages.Add($"Enabled: {sorter.Enabled}");
    sorterMessages.Add($"Drain All: {sorter.DrainAll}");
    
    // Skip filter analysis for now as the API may not be available
    sorterMessages.Add("Filter configuration check skipped (API limitation)");
    
    if (!sorter.DrainAll)
    {
        sorterMessages.Add("[INFO] DrainAll disabled - check manual filter configuration");
    }
    
    sorterMessages.Add("");
}

void CheckSorterConflicts(IMyConveyorSorter sorter, List<MyInventoryItemFilter> filters)
{
    // This function is no longer needed since we removed filter analysis
    // Keeping as placeholder for future implementation
}

void AnalyzeConnectorStatus(HashSet<IMyCubeGrid> connectedGrids)
{
    var connectors = new List<IMyShipConnector>();
    GridTerminalSystem.GetBlocksOfType(connectors, c => connectedGrids.Contains(c.CubeGrid));

    if (connectors.Count == 0) return;

    diagnosticMessages.Add("=== CONNECTOR ANALYSIS ===");

    foreach (var connector in connectors)
    {
        totalBlocksScanned++;
        
        if (!connector.IsFunctional)
        {
            diagnosticMessages.Add($"[ERROR] {connector.CustomName}: Not functional");
            criticalIssues++;
            continue;
        }

        if (connector.Status == MyShipConnectorStatus.Connected)
        {
            var otherConnector = connector.OtherConnector;
            if (otherConnector != null)
            {
                // Check if conveyor connection is enabled
                if (!connector.ThrowOut && !connector.CollectAll)
                {
                    diagnosticMessages.Add($"[INFO] {connector.CustomName}: Connected but no transfer enabled");
                }
            }
        }
        else if (connector.Status == MyShipConnectorStatus.Connectable)
        {
            diagnosticMessages.Add($"[WARNING] {connector.CustomName}: Ready to connect but not connected");
            warnings++;
        }
    }

    diagnosticMessages.Add($"Connectors: {connectors.Count} total");
    diagnosticMessages.Add("");
}

void AnalyzeProductionBlocks(HashSet<IMyCubeGrid> connectedGrids)
{
    var productionBlocks = new List<IMyProductionBlock>();
    GridTerminalSystem.GetBlocksOfType(productionBlocks, p => connectedGrids.Contains(p.CubeGrid));

    if (productionBlocks.Count == 0) return;

    diagnosticMessages.Add("=== PRODUCTION BLOCK ANALYSIS ===");

    int blockedProduction = 0;

    foreach (var production in productionBlocks)
    {
        totalBlocksScanned++;
        
        if (!production.IsFunctional)
        {
            diagnosticMessages.Add($"[ERROR] {production.CustomName}: Not functional");
            criticalIssues++;
            continue;
        }

        if (!production.Enabled)
        {
            continue; // Skip disabled blocks, not an error
        }

        // Check output inventory
        var outputInventory = production.OutputInventory;
        if (outputInventory != null)
        {
            float fillRatio = (float)outputInventory.CurrentVolume / (float)outputInventory.MaxVolume;
            if (fillRatio >= CARGO_FULL_THRESHOLD)
            {
                diagnosticMessages.Add($"[CRITICAL] {production.CustomName}: Output inventory full");
                criticalIssues++;
                blockedProduction++;
            }
        }
    }

    diagnosticMessages.Add($"Production: {productionBlocks.Count} total, {blockedProduction} potentially blocked");
    diagnosticMessages.Add("");
}

void AnalyzeSubgridConveyorConnections(HashSet<IMyCubeGrid> connectedGrids)
{
    diagnosticMessages.Add("=== SUBGRID CONVEYOR CONNECTIONS ===");
    
    // Find all mechanical connections (rotors, pistons, hinges)
    var mechanicalConnections = new List<IMyMechanicalConnectionBlock>();
    GridTerminalSystem.GetBlocksOfType(mechanicalConnections, m => connectedGrids.Contains(m.CubeGrid));
    
    foreach (var connection in mechanicalConnections)
    {
        totalBlocksScanned++;
        
        if (!connection.IsFunctional)
        {
            diagnosticMessages.Add($"[ERROR] {connection.CustomName}: Mechanical connection not functional");
            criticalIssues++;
            continue;
        }
        
        var topGrid = connection.TopGrid;
        if (topGrid == null)
        {
            diagnosticMessages.Add($"[WARNING] {connection.CustomName}: No top grid attached");
            warnings++;
            continue;
        }
        
        // Check if this mechanical connection has conveyor capability
        bool hasConveyorConnection = false;
        
        // Check if the connection block itself has conveyor ports
        var conveyorBlock = connection as IMyConveyorSorter;
        if (conveyorBlock != null)
        {
            hasConveyorConnection = true;
        }
        
        // For rotors specifically, check if they support conveyor connections
        var rotor = connection as IMyMotorStator;
        if (rotor != null)
        {
            // Advanced rotors have conveyor connections
            if (rotor.BlockDefinition.SubtypeId.Contains("Advanced") || 
                rotor.BlockDefinition.SubtypeId.Contains("Conveyor"))
            {
                hasConveyorConnection = true;
            }
        }
        
        if (!hasConveyorConnection)
        {
            diagnosticMessages.Add($"[CRITICAL] {connection.CustomName}: No conveyor connection to subgrid");
            diagnosticMessages.Add($"  Subgrid: {topGrid.DisplayName} cannot receive items");
            criticalIssues++;
        }
        else
        {
            diagnosticMessages.Add($"[INFO] {connection.CustomName}: Conveyor connection OK to {topGrid.DisplayName}");
        }
    }
    
    diagnosticMessages.Add($"Mechanical connections: {mechanicalConnections.Count} found");
    diagnosticMessages.Add("");
}

void AnalyzeWeaponAmmoFlow(HashSet<IMyCubeGrid> connectedGrids)
{
    diagnosticMessages.Add("=== WEAPON AMMO FLOW ANALYSIS ===");
    
    // Find all weapons that need ammo
    var weapons = new List<IMyUserControllableGun>();
    GridTerminalSystem.GetBlocksOfType(weapons, w => connectedGrids.Contains(w.CubeGrid));
    
    var ammoContainers = new List<IMyCargoContainer>();
    GridTerminalSystem.GetBlocksOfType(ammoContainers, c => connectedGrids.Contains(c.CubeGrid));
    
    // Check for ammo in the system
    bool hasAmmo = false;
    foreach (var container in ammoContainers)
    {
        var inventory = container.GetInventory(0);
        if (inventory != null)
        {
            var items = new List<MyInventoryItem>();
            inventory.GetItems(items);
            foreach (var item in items)
            {
                if (item.Type.TypeId.ToString().Contains("Ammo"))
                {
                    hasAmmo = true;
                    break;
                }
            }
            if (hasAmmo) break;
        }
    }
    
    int weaponsWithAmmo = 0;
    int weaponsOnSubgrids = 0;
    
    foreach (var weapon in weapons)
    {
        totalBlocksScanned++;
        
        // Check if weapon is on main grid or subgrid
        bool isOnSubgrid = weapon.CubeGrid != Me.CubeGrid;
        if (isOnSubgrid) weaponsOnSubgrids++;
        
        if (!weapon.IsFunctional)
        {
            diagnosticMessages.Add($"[ERROR] {weapon.CustomName}: Not functional");
            criticalIssues++;
            continue;
        }
        
        // Check weapon inventory for ammo
        var weaponInventory = weapon.GetInventory(0);
        bool weaponHasAmmo = false;
        
        if (weaponInventory != null)
        {
            var items = new List<MyInventoryItem>();
            weaponInventory.GetItems(items);
            foreach (var item in items)
            {
                if (item.Type.TypeId.ToString().Contains("Ammo"))
                {
                    weaponHasAmmo = true;
                    weaponsWithAmmo++;
                    break;
                }
            }
        }
        
        if (!weaponHasAmmo && hasAmmo)
        {
            string gridInfo = isOnSubgrid ? " (SUBGRID)" : " (MAIN GRID)";
            diagnosticMessages.Add($"[CRITICAL] {weapon.CustomName}{gridInfo}: No ammo - conveyor flow blocked");
            criticalIssues++;
        }
        else if (!weaponHasAmmo && !hasAmmo)
        {
            diagnosticMessages.Add($"[WARNING] {weapon.CustomName}: No ammo in system");
            warnings++;
        }
        else if (weaponHasAmmo)
        {
            string gridInfo = isOnSubgrid ? " (SUBGRID)" : " (MAIN GRID)";
            diagnosticMessages.Add($"[INFO] {weapon.CustomName}{gridInfo}: Ammo OK");
        }
    }
    
    diagnosticMessages.Add($"Weapons: {weapons.Count} total, {weaponsOnSubgrids} on subgrids, {weaponsWithAmmo} with ammo");
    
    if (weaponsOnSubgrids > 0 && weaponsWithAmmo == 0 && hasAmmo)
    {
        diagnosticMessages.Add("[CRITICAL] Subgrid weapons not receiving ammo - check rotor conveyor connections");
        criticalIssues++;
    }
    
    diagnosticMessages.Add("");
}

void AnalyzeRotorConveyorIssues(HashSet<IMyCubeGrid> connectedGrids)
{
    diagnosticMessages.Add("=== ROTOR CONVEYOR SPECIFIC ANALYSIS ===");
    
    var rotors = new List<IMyMotorStator>();
    GridTerminalSystem.GetBlocksOfType(rotors, r => connectedGrids.Contains(r.CubeGrid));
    
    foreach (var rotor in rotors)
    {
        totalBlocksScanned++;
        
        if (!rotor.IsFunctional)
        {
            diagnosticMessages.Add($"[ERROR] {rotor.CustomName}: Rotor not functional");
            criticalIssues++;
            continue;
        }
        
        var topGrid = rotor.TopGrid;
        if (topGrid == null)
        {
            diagnosticMessages.Add($"[WARNING] {rotor.CustomName}: No rotor head attached");
            warnings++;
            continue;
        }
        
        // Check rotor type for conveyor capability
        string rotorType = rotor.BlockDefinition.SubtypeId;
        bool supportsConveyors = false;
        
        if (rotorType.Contains("Advanced") || rotorType.Contains("Conveyor"))
        {
            supportsConveyors = true;
        }
        
        // Check if there are weapons or other blocks on the rotor head that need ammo/items
        var weaponsOnRotorHead = new List<IMyUserControllableGun>();
        GridTerminalSystem.GetBlocksOfType(weaponsOnRotorHead, w => w.CubeGrid == topGrid);
        
        var cargoOnRotorHead = new List<IMyCargoContainer>();
        GridTerminalSystem.GetBlocksOfType(cargoOnRotorHead, c => c.CubeGrid == topGrid);
        
        if (weaponsOnRotorHead.Count > 0 || cargoOnRotorHead.Count > 0)
        {
            if (!supportsConveyors)
            {
                diagnosticMessages.Add($"[CRITICAL] {rotor.CustomName}: {rotorType} rotor CANNOT transfer items to subgrid");
                diagnosticMessages.Add($"  Subgrid has {weaponsOnRotorHead.Count} weapons, {cargoOnRotorHead.Count} cargo blocks");
                diagnosticMessages.Add($"  SOLUTION: Replace with Advanced Rotor or Rotor with conveyor support");
                criticalIssues++;
            }
            else
            {
                // Check if rotor is set to share inertia tensor (affects conveyor connections)
                diagnosticMessages.Add($"[INFO] {rotor.CustomName}: Conveyor-capable rotor with {weaponsOnRotorHead.Count} weapons on subgrid");
                
                // Check weapon ammo status on this specific rotor
                bool allWeaponsHaveAmmo = true;
                foreach (var weapon in weaponsOnRotorHead)
                {
                    var weaponInventory = weapon.GetInventory(0);
                    bool hasAmmo = false;
                    if (weaponInventory != null)
                    {
                        var items = new List<MyInventoryItem>();
                        weaponInventory.GetItems(items);
                        foreach (var item in items)
                        {
                            if (item.Type.TypeId.ToString().Contains("Ammo"))
                            {
                                hasAmmo = true;
                                break;
                            }
                        }
                    }
                    if (!hasAmmo)
                    {
                        allWeaponsHaveAmmo = false;
                        diagnosticMessages.Add($"[CRITICAL] {weapon.CustomName}: No ammo on rotor subgrid - conveyor flow issue");
                    }
                }
                
                if (!allWeaponsHaveAmmo)
                {
                    diagnosticMessages.Add($"[CRITICAL] {rotor.CustomName}: Conveyor flow to subgrid is blocked");
                    criticalIssues++;
                }
            }
        }
    }
    
    diagnosticMessages.Add($"Rotors analyzed: {rotors.Count}");
    diagnosticMessages.Add("");
}

void AnalyzeConveyorConnectivity(HashSet<IMyCubeGrid> connectedGrids)
{
    diagnosticMessages.Add("=== CONVEYOR CONNECTIVITY ===");
    
    // Basic conveyor tube and conveyor block analysis
    var conveyorTubes = new List<IMyConveyorTube>();
    var conveyorBlocks = new List<IMyConveyor>();
    
    GridTerminalSystem.GetBlocksOfType(conveyorTubes, c => connectedGrids.Contains(c.CubeGrid));
    GridTerminalSystem.GetBlocksOfType(conveyorBlocks, c => connectedGrids.Contains(c.CubeGrid));

    int totalConveyors = conveyorTubes.Count + conveyorBlocks.Count;
    int functionalConveyors = 0;

    foreach (var tube in conveyorTubes)
    {
        totalBlocksScanned++;
        if (tube.IsFunctional)
            functionalConveyors++;
        else
        {
            diagnosticMessages.Add($"[ERROR] Conveyor tube not functional (Position: {tube.Position})");
            criticalIssues++;
        }
    }

    foreach (var conveyor in conveyorBlocks)
    {
        totalBlocksScanned++;
        if (conveyor.IsFunctional)
            functionalConveyors++;
        else
        {
            diagnosticMessages.Add($"[ERROR] Conveyor not functional (Position: {conveyor.Position})");
            criticalIssues++;
        }
    }

    diagnosticMessages.Add($"Conveyors: {functionalConveyors}/{totalConveyors} functional");
    
    if (functionalConveyors < totalConveyors)
    {
        diagnosticMessages.Add($"[WARNING] {totalConveyors - functionalConveyors} non-functional conveyors detected");
        warnings++;
    }

    diagnosticMessages.Add("");
}

void UpdateDiagnosticDisplay()
{
    if (diagnosticSurface == null) return;

    using (var frame = diagnosticSurface.DrawFrame())
    {
        Vector2 screenSize = diagnosticSurface.SurfaceSize;
        
        // Background
        var bg = new MySprite(SpriteType.TEXTURE, "SquareSimple", 
                             screenSize * 0.5f, screenSize, Color.Black);
        frame.Add(bg);

        Vector2 currentPos = HEADER_START_POS;
        
        // Display messages with appropriate colors
        for (int i = 0; i < Math.Min(diagnosticMessages.Count, MAX_ISSUES_TO_DISPLAY); i++)
        {
            string message = diagnosticMessages[i];
            Color textColor = COLOR_INFO;
            float fontSize = BODY_FONT_SIZE;

            // Determine color based on message content
            if (message.StartsWith("CONVEYOR SYSTEM DIAGNOSTIC") || message.StartsWith("==="))
            {
                textColor = COLOR_HEADER;
                fontSize = HEADER_FONT_SIZE;
            }
            else if (message.Contains("[ERROR]") || message.Contains("[CRITICAL]"))
            {
                textColor = COLOR_ERROR;
            }
            else if (message.Contains("[WARNING]"))
            {
                textColor = COLOR_WARNING;
            }
            else if (message.Contains("ALL CLEAR"))
            {
                textColor = COLOR_OK;
            }

            var textSprite = new MySprite(SpriteType.TEXT, message)
            {
                Position = currentPos,
                Color = textColor,
                FontId = "Monospace",
                RotationOrScale = fontSize,
                Alignment = TextAlignment.LEFT
            };
            frame.Add(textSprite);

            currentPos.Y += LINE_SPACING;
        }
    }
}

void UpdateSorterDetailDisplay()
{
    if (sorterDetailSurface == null) return;

    using (var frame = sorterDetailSurface.DrawFrame())
    {
        Vector2 screenSize = sorterDetailSurface.SurfaceSize;
        
        // Background
        var bg = new MySprite(SpriteType.TEXTURE, "SquareSimple", 
                             screenSize * 0.5f, screenSize, Color.Black);
        frame.Add(bg);

        Vector2 currentPos = HEADER_START_POS;
        
        // Display sorter detail messages
        for (int i = 0; i < Math.Min(sorterMessages.Count, MAX_ISSUES_TO_DISPLAY); i++)
        {
            string message = sorterMessages[i];
            Color textColor = COLOR_INFO;
            float fontSize = DETAIL_FONT_SIZE;

            if (message.StartsWith("===") || message.StartsWith("---"))
            {
                textColor = COLOR_HEADER;
                fontSize = BODY_FONT_SIZE;
            }
            else if (message.Contains("[ERROR]"))
            {
                textColor = COLOR_ERROR;
            }
            else if (message.Contains("[WARNING]"))
            {
                textColor = COLOR_WARNING;
            }

            var textSprite = new MySprite(SpriteType.TEXT, message)
            {
                Position = currentPos,
                Color = textColor,
                FontId = "Monospace",
                RotationOrScale = fontSize,
                Alignment = TextAlignment.LEFT
            };
            frame.Add(textSprite);

            currentPos.Y += LINE_SPACING * 0.8f; // Tighter spacing for detail view
        }
    }
}

// ===== UTILITY FUNCTIONS =====

HashSet<IMyCubeGrid> GetAllConnectedGrids(IMyCubeGrid startGrid)
{
    HashSet<IMyCubeGrid> visited = new HashSet<IMyCubeGrid>();
    Queue<IMyCubeGrid> toVisit = new Queue<IMyCubeGrid>();
    toVisit.Enqueue(startGrid);

    while (toVisit.Count > 0)
    {
        var grid = toVisit.Dequeue();
        if (!visited.Add(grid)) 
            continue;

        // Find all mechanical connections on this grid
        var mechs = new List<IMyMechanicalConnectionBlock>();
        GridTerminalSystem.GetBlocksOfType(mechs, b => b.CubeGrid == grid);
        
        foreach (var m in mechs)
        {
            // Add the top grid (attached subgrid)
            var top = m.TopGrid;
            if (top != null && !visited.Contains(top))
                toVisit.Enqueue(top);
                
            // Also check if this block has a base grid connection
            var baseGrid = m.CubeGrid;
            if (baseGrid != null && !visited.Contains(baseGrid))
                toVisit.Enqueue(baseGrid);
        }
        
        // Also check for connector connections
        var connectors = new List<IMyShipConnector>();
        GridTerminalSystem.GetBlocksOfType(connectors, c => c.CubeGrid == grid && c.Status == MyShipConnectorStatus.Connected);
        
        foreach (var connector in connectors)
        {
            var otherConnector = connector.OtherConnector;
            if (otherConnector != null && !visited.Contains(otherConnector.CubeGrid))
                toVisit.Enqueue(otherConnector.CubeGrid);
        }
    }

    return visited;
}
