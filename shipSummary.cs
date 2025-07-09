bool scanComplete = false;
List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

public void Main(string argument, UpdateType updateSource)
{
    if (!scanComplete)
    {
        Echo("Scan in progress...");
        allBlocks.Clear();
        GridTerminalSystem.GetBlocks(allBlocks);
        string outputLog = "";
        
        // Configurable blocks (exclude programmable blocks)
        outputLog += "CFG:\n";
        foreach (var block in allBlocks)
        {
            if (block is IMyProgrammableBlock) continue;
            string cd = string.IsNullOrEmpty(block.CustomData) ? "None" : block.CustomData;
            outputLog += block.CustomName + " | " + block.BlockDefinition.TypeIdString + " | " + cd + " | " + block.DetailedInfo + "\n";
        }
        
        // Programmable blocks with scripts running (only name and type)
        outputLog += "\nPRG:\n";
        foreach (var block in allBlocks)
        {
            IMyProgrammableBlock progBlock = block as IMyProgrammableBlock;
            if (progBlock != null && !progBlock.DetailedInfo.Contains("No program loaded"))
            {
                outputLog += progBlock.CustomName + " | " + progBlock.BlockDefinition.TypeIdString + "\n";
            }
        }
        
        Me.CustomData = outputLog;
        Echo("Scan complete. Check Custom Data.");
        scanComplete = true;
        Runtime.UpdateFrequency = UpdateFrequency.None;
    }
    else
    {
        Echo("Scan already complete.");
    }
}
