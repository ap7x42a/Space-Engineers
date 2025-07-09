public void Main(string argument, UpdateType updateSource)
{
    var block = GridTerminalSystem.GetBlockWithName("Weapon Room Control Inset LCD 2") as IMyTerminalBlock;

    if (block == null)
    {
        Echo("Block not found.");
        return;
    }

    Echo("Properties:");
    var properties = new List<ITerminalProperty>();
    block.GetProperties(properties);
    foreach (var prop in properties)
    {
        Echo($"- {prop.Id}");
    }

    Echo("\nActions:");
    var actions = new List<ITerminalAction>();
    block.GetActions(actions);
    foreach (var action in actions)
    {
        Echo($"- {action.Id}");
    }
}
