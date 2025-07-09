public Program()
{
    // Set update frequency so the script runs periodically (every 10 ticks).
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

// List to store all blocks on the grid.
List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();

public void Main(string argument, UpdateType updateSource)
{
    // Clear the list each run.
    allBlocks.Clear();
    string outputLog = "Script running...\n";
    
    // Get all terminal blocks on the grid.
    GridTerminalSystem.GetBlocks(allBlocks);

    // Define our target block names.
    string[] targetNames = {
        "Reactor Room Aux Console LCD",
        "Reactor Room Aux Left Corner LCD",
        "Reactor Room Aux 6 Button LCD",
        "Reactor Room Aux Custom Turret LCD",
        "Reactor Room Aux 2 Button LCD",
        "Reactor Room Aux Right Corner LCD"
    };

    // List for storing blocks that match any of the target names.
    List<IMyTerminalBlock> targetBlocks = new List<IMyTerminalBlock>();

    // Filter the blocks based on the target names.
    foreach (var block in allBlocks)
    {
        foreach (var target in targetNames)
        {
            if (block.CustomName.Contains(target))
            {
                targetBlocks.Add(block);
                break; // Stop checking once a match is found.
            }
        }
    }

    outputLog += "Found " + targetBlocks.Count + " target block(s):\n";

    // Loop through each target block and check for text surfaces.
    foreach (var block in targetBlocks)
    {
        outputLog += "Block: " + block.CustomName + " | Type: " + block.BlockDefinition.TypeIdString + "\n";
        
        // Try to cast the block to a text surface provider.
        var provider = block as IMyTextSurfaceProvider;
        if (provider != null)
        {
            int surfaceCount = provider.SurfaceCount;
            outputLog += "This block has " + surfaceCount + " text surface(s).\n";
            for (int i = 0; i < surfaceCount; i++)
            {
                IMyTextSurface surface = provider.GetSurface(i);
                // Output physical dimensions (in meters) and texture resolution (in pixels).
                outputLog += "Surface " + i + ": Physical Size = " + surface.SurfaceSize.ToString() +
                             ", Texture Size = " + surface.TextureSize.ToString() + "\n";
            }
        }
        else
        {
            outputLog += "Block " + block.CustomName + " does not support text surfaces.\n";
        }
        outputLog += "\n";
    }
    
    // Write the complete output to the programmable block's Custom Data field.
    Me.CustomData = outputLog;
}
