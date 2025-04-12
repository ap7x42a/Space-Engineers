// === Customizable Variables ===
const string cryoRoomName = "Inset Cryo Room";
static readonly string[] interiorLightNames = { "Cryo Light Right", "Cryo Light Left", "Cryo Light Hidden" };
static readonly string[] spotlightNames = { "Cryo Light Center" };

// Occupied Color Settings
Color occupiedColor = new Color(255, 65, 0);

// Unoccupied Color Settings
Color unoccupiedColor = new Color(175, 150, 100);

// Transition Speed (lower is slower)
const float fadeSpeed = 0.2f;

// Light settings
const float radius = 2.5f;
const float falloff = 1.3f;
const float intensity = 5f;

IMyCryoChamber cryoRoom;
List<IMyInteriorLight> interiorLights = new List<IMyInteriorLight>();
List<IMyReflectorLight> spotlights = new List<IMyReflectorLight>();

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;

    cryoRoom = GridTerminalSystem.GetBlockWithName(cryoRoomName) as IMyCryoChamber;
    if (cryoRoom == null)
        throw new Exception($"Cryo Room '{cryoRoomName}' not found or incorrect block type.");

    foreach (var name in interiorLightNames)
    {
        var light = GridTerminalSystem.GetBlockWithName(name) as IMyInteriorLight;
        if (light == null)
            throw new Exception($"Interior light '{name}' not found or incorrect block type.");
        interiorLights.Add(light);
    }

    foreach (var name in spotlightNames)
    {
        var spotlight = GridTerminalSystem.GetBlockWithName(name) as IMyReflectorLight;
        if (spotlight == null)
            throw new Exception($"Spotlight '{name}' not found or incorrect block type.");
        spotlights.Add(spotlight);
    }
}

public void Main(string argument, UpdateType updateSource)
{
    bool occupied = cryoRoom.IsUnderControl;
    Color targetColor = occupied ? occupiedColor : unoccupiedColor;

    foreach (var light in interiorLights)
    {
        Color currentColor = light.Color;
        currentColor = Color.Lerp(currentColor, targetColor, fadeSpeed);
        light.Color = currentColor;
        light.Radius = radius;
        light.Falloff = falloff;
        light.Intensity = intensity;
    }

    foreach (var spotlight in spotlights)
    {
        Color currentColor = spotlight.Color;
        currentColor = Color.Lerp(currentColor, targetColor, fadeSpeed);
        spotlight.Color = currentColor;
        spotlight.Radius = radius;
        spotlight.Falloff = falloff;
        spotlight.Intensity = intensity;
    }
}
