public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    InitializeLights();
}

List<IMyLightingBlock> hallwayLights = new List<IMyLightingBlock>();
List<IMyLightingBlock> engineeringLights = new List<IMyLightingBlock>();
bool isRedPulseActive = false;
bool isEngineeringPulseActive = false;
bool isStage1Active = false;
bool isStage2Active = false;
float intensityMin = 0.5f;
float intensityMax = 5.0f;
float pulseSpeed = 0.1f;
bool increasingRedPulse = true;
bool increasingEngineeringPulse = true;
bool increasingStage1 = true;
bool increasingStage2 = true;

void Main(string argument, UpdateType updateSource)
{
    switch (argument.ToLower())
    {
        case "redpulse":
            isStage1Active = false;
            ApplyRedPulseSettings(hallwayLights);
            isRedPulseActive = true;
            break;
        case "engineeringpulse":
            isStage2Active = false;
            ApplyRedPulseSettings(engineeringLights);
            isEngineeringPulseActive = true;
            break;
        case "stopredpulse":
            isRedPulseActive = false;
            TurnOffLights(hallwayLights);
            break;
        case "stopengineeringpulse":
            isEngineeringPulseActive = false;
            TurnOffLights(engineeringLights);
            break;
        case "stage1":
            isRedPulseActive = false;
            isStage1Active = true;
            TransitionToStage1(ref increasingStage1);
            break;
        case "stopstage1":
            isStage1Active = false;
            TurnOffLights(hallwayLights);
            break;
        case "stage2":
            isEngineeringPulseActive = false;
            isStage2Active = true;
            TransitionToStage2(ref increasingStage2);
            break;
        case "stopstage2":
            isStage2Active = false;
            TurnOffLights(engineeringLights);
            break;
    }

    if (isRedPulseActive)
    {
        PulseLights(hallwayLights, ref increasingRedPulse);
    }
    if (isEngineeringPulseActive)
    {
        PulseLights(engineeringLights, ref increasingEngineeringPulse);
    }
    if (isStage1Active)
    {
        TransitionToStage1(ref increasingStage1);
    }
    if (isStage2Active)
    {
        TransitionToStage2(ref increasingStage2);
    }
}

void InitializeLights()
{
    hallwayLights.Clear();
    GridTerminalSystem.GetBlocksOfType(hallwayLights, light =>
        light.CustomName.Contains("Left Entrance Hallway Inset Light") ||
        light.CustomName.Contains("Right Entrance Hallway Inset Light"));

    engineeringLights.Clear();
    GridTerminalSystem.GetBlocksOfType(engineeringLights, light =>
        light.CustomName.Contains("Engineering Deck Inset Light") ||
        light.CustomName.Contains("Engineering Deck Truss Light"));
}

void ApplyRedPulseSettings(List<IMyLightingBlock> lights)
{
    foreach (var light in lights)
    {
        light.Color = new Color(230, 0, 0);
        light.Radius = 3.6f;
        light.Falloff = 1.3f;
        light.Intensity = intensityMin;
        light.BlinkIntervalSeconds = 0;
        light.BlinkLength = 0;
        light.BlinkOffset = 0.5f;
        light.Enabled = true;
    }
}

void PulseLights(List<IMyLightingBlock> lights, ref bool increasing)
{
    float newIntensity;
    if (increasing)
    {
        newIntensity = lights[0].Intensity + pulseSpeed;
        if (newIntensity >= intensityMax)
        {
            newIntensity = intensityMax;
            increasing = false;
        }
    }
    else
    {
        newIntensity = lights[0].Intensity - pulseSpeed;
        if (newIntensity <= intensityMin)
        {
            newIntensity = intensityMin;
            increasing = true;
        }
    }
    
    foreach (var light in lights)
    {
        light.Intensity = newIntensity;
    }
}

void TurnOffLights(List<IMyLightingBlock> lights)
{
    foreach (var light in lights)
    {
        light.Intensity = 0;
        light.Enabled = false;
    }
}

void TransitionToStage1(ref bool increasing)
{
    foreach (var light in hallwayLights)
    {
        light.Color = new Color(175, 150, 100);
        light.Radius = 3.6f;
        light.Falloff = 1.3f;
        light.Intensity = 1.5f;
        light.BlinkIntervalSeconds = 0;
        light.BlinkLength = 0;
        light.BlinkOffset = 0.5f;
        light.Enabled = true;
    }
}

void TransitionToStage2(ref bool increasing)
{
    foreach (var light in engineeringLights)
    {
        light.Color = new Color(175, 150, 100);
        light.Radius = 3.7f;
        light.Falloff = 1.3f;
        light.Intensity = 3.2f;
        light.BlinkIntervalSeconds = 0;
        light.BlinkLength = 0;
        light.BlinkOffset = 0.5f;
        light.Enabled = true;
    }
}
