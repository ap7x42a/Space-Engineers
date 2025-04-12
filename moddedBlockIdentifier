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
    
    // Get all terminal blocks on the grid.
    GridTerminalSystem.GetBlocks(allBlocks);
    
    // List for storing the modded blocks that match our custom name.
    List<IMyTerminalBlock> moddedBlocks = new List<IMyTerminalBlock>();
    
    // Filter through all blocks to find modded blocks by name.
    foreach (var block in allBlocks)
    {
        if (block.CustomName.Contains("Reactor Room Aux 6 Button LCD"))
        {
            moddedBlocks.Add(block);
        }
    }
    
    Echo("Found " + moddedBlocks.Count + " Modded Block(s):");
    
    // Loop through each found block and output its name and definition type.
    foreach (var block in moddedBlocks)
    {
        Echo("Block: " + block.CustomName + " | Type: " + block.BlockDefinition.TypeIdString);
    }
}
