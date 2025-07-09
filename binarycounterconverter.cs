public void Main(string argument, UpdateType updateSource)
{
    string[] bitNames = {"Bit 1 Light Panel", "Bit 2 Light Panel", "Bit 3 Light Panel", 
                         "Bit 4 Light Panel", "Bit 5 Light Panel", "Bit 6 Light Panel", 
                         "Bit 7 Light Panel", "Bit 8 Light Panel"};
    string lcdName = "Counter_LCD";

    var lcd = GridTerminalSystem.GetBlockWithName(lcdName) as IMyTextPanel;
    if (lcd == null)
    {
        Echo($"LCD '{lcdName}' not found.");
        return;
    }

    int binaryValue = 0;
    string debugInfo = ""; // Collect debugging info

    for (int i = 0; i < bitNames.Length; i++)
    {
        var light = GridTerminalSystem.GetBlockWithName(bitNames[i]) as IMyLightingBlock;
        if (light == null)
        {
            Echo($"Light '{bitNames[i]}' not found.");
            debugInfo += $"{bitNames[i]}: NOT FOUND\n";
            continue;
        }

        // Debug: Display the current state of the light
        debugInfo += $"{bitNames[i]}: {(light.Enabled ? "ON" : "OFF")}\n";

        if (light.Enabled)
        {
            binaryValue |= (1 << i);
        }
    }

    string binaryString = Convert.ToString(binaryValue, 2).PadLeft(8, '0');
    lcd.WriteText($"Binary: {binaryString}\nDecimal: {binaryValue}");
    Echo($"Binary: {binaryString}\nDecimal: {binaryValue}");
    Echo(debugInfo); // Output debug info to the PB console
}
