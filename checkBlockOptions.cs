// Script to List All Available Actions for a Block
// Place the name of the block you want to inspect in the `blockName` variable.

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.None; // Manual execution
}

void Main(string argument, UpdateType updateSource)
{
    string blockName = "Reactor Room Aux 6 Button LCD"; // Replace with your block's name

    var block = GridTerminalSystem.GetBlockWithName(blockName);
    if (block == null)
    {
        Echo($"Block '{blockName}' not found.");
        return;
    }

    Echo($"Actions for: {block.CustomName}");
    
    // Retrieve and list all actions for the block
    var actions = new List<ITerminalAction>();
    block.GetActions(actions);

    if (actions.Count == 0)
    {
        Echo("No actions found for this block.");
        return;
    }

    foreach (var action in actions)
    {
        Echo($" - {action.Id}");
    }
}
