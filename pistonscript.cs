// === Configurable Variables ===
float extendSpeed = 0.5f; // Default extend speed in meters per second
float retractSpeed = -0.5f; // Default retract speed in meters per second
double delayInterval = 0.75; // Default delay between steps in seconds

// === Extend Patterns ===
int[] sequentialPattern = { 3, 2, 9, 8, 1, 4, 5, 6, 7 };
int[] spiralPattern = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
int[] zigzagPattern = { 3, 7, 9, 5, 2, 8, 1, 6, 4 };

// Extend patterns with groups (arrays within arrays)
int[][] wavePattern = {
    new int[] { 3 },
    new int[] { 2, 4 },
    new int[] { 5, 1, 9 },
    new int[] { 6, 8 },
    new int[] { 7 }
};

int[][] radialPattern = {
    new int[] { 1 },
    new int[] { 2, 4, 6, 8 },
    new int[] { 3, 5, 7, 9 }
};

int[][] cornersInPattern = {
    new int[] { 3, 9, 5, 7 },
    new int[] { 2, 8, 4, 6 },
    new int[] { 1 }
};

// === Retract Patterns ===
int[][] retract1Pattern = {
    new int[] { 1, 2, 4, 6, 8 }
};

int[][] retract2Pattern = {
    new int[] { 2, 6 },
    new int[] { 3, 5, 7, 9 }
};

// === Script Data Structures ===
List<IMyPistonBase> pistons = new List<IMyPistonBase>();
Dictionary<int, IMyPistonBase> pistonOrder = new Dictionary<int, IMyPistonBase>();

// Updated to directly map piston numbers to the specific names
string[] orderedNames = {
    "Column 1 Middle Piston 1", "Column 1 Middle Piston 2", "Column 1 Middle Piston 3",
    "Column 1 Middle Piston 4", "Column 1 Middle Piston 5", "Column 1 Middle Piston 6",
    "Column 1 Middle Piston 7", "Column 1 Middle Piston 8", "Column 1 Middle Piston 9"
};

// === Script Variables ===
double timeAccumulator = 0;
int currentIndex = 0;
bool extending = false;
bool retracting = false;
string currentPattern = "";

// === Initialization ===
public Program()
{
    // Initialize pistons based on naming convention
    for (int i = 0; i < orderedNames.Length; i++)
    {
        var piston = GridTerminalSystem.GetBlockWithName(orderedNames[i]) as IMyPistonBase;
        if (piston != null)
        {
            pistons.Add(piston);
            pistonOrder[i + 1] = piston;
            piston.Velocity = extendSpeed;
        }
        else
        {
            Echo($"Error: Piston '{orderedNames[i]}' not found.");
        }
    }

    // Configure display for verbose output
    var surface = Me.GetSurface(0);
    surface.ContentType = ContentType.TEXT_AND_IMAGE;
    surface.FontSize = 1.2f;
    surface.Alignment = TextAlignment.LEFT;
}

// === Main Program Execution ===
public void Main(string argument, UpdateType updateSource)
{
    if (argument == "Sequential" || argument == "Spiral" || argument == "Wave" || argument == "Radial" || argument == "Zigzag" || argument == "CornersIn")
    {
        currentPattern = argument;
        currentIndex = 0;
        extending = true;
        retracting = false;
        timeAccumulator = 0;
        Runtime.UpdateFrequency = UpdateFrequency.Update10;
        Echo($"Starting extension sequence: {currentPattern}");
    }
    else if (argument == "retract1" || argument == "retract2")
    {
        currentPattern = argument;
        currentIndex = 0;
        extending = false;
        retracting = true;
        timeAccumulator = 0;
        Runtime.UpdateFrequency = UpdateFrequency.Update10;
        Echo($"Starting retraction sequence: {currentPattern}");
    }
    else if (argument == "Reset")
    {
        ResetPistons();
    }
    
    if (extending || retracting)
    {
        ExecutePattern();
        DisplayStatus();
    }
}

// === Pattern Execution Logic ===
void ExecutePattern()
{
    timeAccumulator += Runtime.TimeSinceLastRun.TotalSeconds;

    if (extending)
    {
        switch (currentPattern)
        {
            case "Sequential":
                SequentialPattern(sequentialPattern, delayInterval, extendSpeed);
                break;
            
            case "Spiral":
                SequentialPattern(spiralPattern, delayInterval, extendSpeed);
                break;
            
            case "Wave":
                ActivateIndividualPistons(wavePattern, extendSpeed);
                break;

            case "Radial":
                ActivateIndividualPistons(radialPattern, extendSpeed);
                break;

            case "Zigzag":
                SequentialPattern(zigzagPattern, delayInterval, extendSpeed);
                break;

            case "CornersIn":
                ActivateIndividualPistons(cornersInPattern, extendSpeed);
                break;
        }
    }
    else if (retracting)
    {
        switch (currentPattern)
        {
            case "retract1":
                ActivateIndividualPistons(retract1Pattern, retractSpeed);
                break;

            case "retract2":
                ActivateIndividualPistons(retract2Pattern, retractSpeed);
                break;
        }
    }
}

void SequentialPattern(int[] order, double interval, float speed)
{
    if (currentIndex < order.Length && timeAccumulator >= interval)
    {
        var piston = pistonOrder[order[currentIndex]];
        piston.Velocity = speed;
        Echo($"Setting Piston {order[currentIndex]} to speed {speed} in Sequential Pattern");
        
        timeAccumulator = 0;
        currentIndex++;
    }
    else if (currentIndex >= order.Length)
    {
        StopSequence();
    }
}

void ActivateIndividualPistons(int[][] groups, float speed)
{
    if (currentIndex < groups.Length && timeAccumulator >= delayInterval)
    {
        foreach (int pistonNumber in groups[currentIndex])
        {
            var piston = pistonOrder[pistonNumber];
            piston.Velocity = speed;
            Echo($"Setting Piston {pistonNumber} to speed {speed} individually");
        }
        
        timeAccumulator = 0;
        currentIndex++;
    }
    else if (currentIndex >= groups.Length)
    {
        StopSequence();
    }
}

void StopSequence()
{
    extending = false;
    retracting = false;
    Runtime.UpdateFrequency = UpdateFrequency.None;
    Echo("Sequence complete.");
}

void ResetPistons()
{
    Echo("Resetting all pistons to retracted state.");
    foreach (var piston in pistons)
    {
        piston.Velocity = retractSpeed;
    }
    extending = false;
    retracting = false;
    timeAccumulator = 0;
    currentIndex = 0;
    Runtime.UpdateFrequency = UpdateFrequency.None;
}

void DisplayStatus()
{
    var surface = Me.GetSurface(0);
    var status = new System.Text.StringBuilder();

    status.AppendLine("Piston Control Status:");
    status.AppendLine($"Current Pattern: {currentPattern}");
    status.AppendLine($"Mode: {(extending ? "Extending" : retracting ? "Retracting" : "Idle")}");
    
    for (int i = 0; i < pistons.Count; i++)
    {
        var piston = pistonOrder[i + 1];
        string state = piston.CurrentPosition > 0.1 ? "Extended" : "Retracted";
        status.AppendLine($"Piston {i + 1}: {state} ({piston.CurrentPosition:F2} m)");
    }

    surface.WriteText(status.ToString());
}
