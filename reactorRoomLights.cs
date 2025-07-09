// Advanced Fluorescent Light Event Controller for Space Engineers
// Each group is defined by a unique custom name, and all blocks with that name
// are controlled simultaneously as one light group.

// ----------------------------
// Data Structures & Classes
// ----------------------------

public class LightGroupConfig {
    // Mode: "flicker", "stable", or "fade"
    public string Mode;
    // SequenceOrder: 0 means all groups activate simultaneously; 1-10 defines activation order.
    public int SequenceOrder;
    // How likely the light toggles each tick in flicker mode (0 to 1)
    public float FlickerSpeed;
    // Duration (in seconds) for flicker effect after activation.
    public float FlickerDuration;
    // Speed (fraction per second) at which fade mode interpolates from off to target color.
    public float FadeSpeed;
    // ToggleInterval: the delay (in seconds) between groups when using sequence order.
    public float ToggleInterval;
    // TargetColor: final color when the light is fully on.
    public Color TargetColor;
}

public class LightGroup {
    public string GroupName;
    // List of all interior light blocks in this group.
    public List<IMyInteriorLight> Lights = new List<IMyInteriorLight>();
    // Configuration settings (loaded from a preset)
    public LightGroupConfig Config;

    // Runtime state:
    public bool Activated = false;      // Has this group started its effect?
    public float ActivationTimer = 0f;    // Delay countdown (if sequence order is used)
    public float FlickerTimer = 0f;       // How long the group has been flickering
    public float FadeProgress = 0f;       // For fade mode: 0 = off, 1 = fully lit
}

// ----------------------------
// Global Variables
// ----------------------------

// Map group name to its LightGroup instance.
Dictionary<string, LightGroup> lightGroups = new Dictionary<string, LightGroup>();
// Map preset name to a dictionary that maps group names to LightGroupConfig.
Dictionary<string, Dictionary<string, LightGroupConfig>> presets = new Dictionary<string, Dictionary<string, LightGroupConfig>>();

// Random number generator for flicker effect.
System.Random random = new System.Random();
// dt approximates the time per tick (Update10 runs roughly every 0.1667 seconds).
const float dt = 0.1667f;

// Used to ensure initialization happens only once.
bool initialized = false;

// ----------------------------
// Initialization
// ----------------------------

public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    if (!initialized) {
        InitializeLightGroups();
        InitializePresets();
        initialized = true;
    }
}

// Retrieve all blocks for each group name and create a LightGroup for each.
void InitializeLightGroups() {
    string[] groupNames = new string[] {
        "Reactor Room Neon Tubes 1",
        "Reactor Room Neon Tubes 2",
        "Reactor Room Neon Tubes 3",
        "Reactor Room Neon Tubes 4",
        "Reactor Room Neon Tubes 5",
        "Reactor Room Neon Tubes 6",
        "Reactor Room Neon Tubes 7",
        "Reactor Room Neon Tubes 8",
        "Reactor Room Floor Neon Tubes 1",
        "Reactor Room Floor Neon Tubes 2"
    };
    List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocks(allBlocks);
    
    foreach (string groupName in groupNames) {
        // Filter blocks manually that match the exact custom name.
        List<IMyTerminalBlock> groupBlocks = new List<IMyTerminalBlock>();
        foreach(var block in allBlocks) {
            if(block.CustomName == groupName) {
                groupBlocks.Add(block);
            }
        }
        if (groupBlocks.Count > 0) {
            LightGroup lg = new LightGroup();
            lg.GroupName = groupName;
            foreach (var block in groupBlocks) {
                var light = block as IMyInteriorLight;
                if (light != null) {
                    lg.Lights.Add(light);
                    // Start with lights off.
                    light.Enabled = false;
                }
            }
            if (lg.Lights.Count > 0) {
                lightGroups[groupName] = lg;
            } else {
                Echo("No interior light found for group: " + groupName);
            }
        } else {
            Echo("Group not found: " + groupName);
        }
    }
}

// Define a default preset example. Copy this preset block to add new ones with different names.
void InitializePresets() {
    presets.Clear();
    var defaultPreset = new Dictionary<string, LightGroupConfig>();
    defaultPreset["Reactor Room Neon Tubes 1"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 1,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(175, 150, 100)
    };
    defaultPreset["Reactor Room Neon Tubes 2"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 2,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(175, 150, 100)
    };
    defaultPreset["Reactor Room Neon Tubes 3"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 3,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(175, 150, 100)
    };
    defaultPreset["Reactor Room Neon Tubes 4"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 4,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(175, 150, 100)
    };
    defaultPreset["Reactor Room Neon Tubes 5"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 5,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(175, 150, 100)
    };
    defaultPreset["Reactor Room Neon Tubes 6"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 6,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(175, 150, 100)
    };
    defaultPreset["Reactor Room Neon Tubes 7"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 7,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(175, 150, 100)
    };
    defaultPreset["Reactor Room Neon Tubes 8"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 8,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(175, 150, 100)
    };
    defaultPreset["Reactor Room Floor Neon Tubes 1"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 9,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(175, 150, 100)
    };
    defaultPreset["Reactor Room Floor Neon Tubes 2"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 10,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(175, 150, 100)
    };

    // Add the default preset to our presets collection.
    presets["idle"] = defaultPreset;
    // You can add additional presets here using a similar pattern.

    var redPreset = new Dictionary<string, LightGroupConfig>();
    redPreset["Reactor Room Neon Tubes 1"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 1,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(255, 0, 0)
    };
    redPreset["Reactor Room Neon Tubes 2"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 2,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(255, 0, 0)
    };
    redPreset["Reactor Room Neon Tubes 3"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 3,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(255, 0, 0)
    };
    redPreset["Reactor Room Neon Tubes 4"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 4,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(255, 0, 0)
    };
    redPreset["Reactor Room Neon Tubes 5"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 5,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(255, 0, 0)
    };
    redPreset["Reactor Room Neon Tubes 6"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 6,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(255, 0, 0)
    };
    redPreset["Reactor Room Neon Tubes 7"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 7,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(255, 0, 0)
    };
    redPreset["Reactor Room Neon Tubes 8"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 8,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(255, 0, 0)
    };
    redPreset["Reactor Room Floor Neon Tubes 1"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 9,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(255, 0, 0)
    };
    redPreset["Reactor Room Floor Neon Tubes 2"] = new LightGroupConfig() {
        Mode = "flicker",
        SequenceOrder = 10,
        FlickerSpeed = 0.9f,
        FlickerDuration = 1.0f,
        FadeSpeed = 0f,
        ToggleInterval = 0.1f,
        TargetColor = new Color(255, 0, 0)
    };

    presets["red"] = redPreset;
    // You can add additional presets here using a similar pattern.


}

// ----------------------------
// Preset Application
// ----------------------------

// Load a preset (mapping group names to configs) into the light groups.
void ApplyPreset(Dictionary<string, LightGroupConfig> preset) {
    foreach (var kvp in preset) {
        string groupName = kvp.Key;
        LightGroupConfig cfg = kvp.Value;
        if (lightGroups.ContainsKey(groupName)) {
            LightGroup lg = lightGroups[groupName];
            lg.Config = cfg;
            lg.Activated = false;
            lg.FlickerTimer = 0f;
            lg.FadeProgress = 0f;
            // Calculate activation delay if SequenceOrder is used.
            lg.ActivationTimer = (cfg.SequenceOrder > 0) ? (cfg.SequenceOrder - 1) * cfg.ToggleInterval : 0f;
            // Turn off the lights initially.
            foreach (var light in lg.Lights) {
                light.Enabled = false;
            }
        }
    }
}

// ----------------------------
// Helper: Linear Interpolation
// ----------------------------
float Lerp(float a, float b, float t) {
    return a + (b - a) * t;
}

// ----------------------------
// Main Update Loop
// ----------------------------

public void Main(string argument, UpdateType updateSource) {
    // --- Event Controller: Preset Handling ---
    // When an argument is passed (e.g., "default"), load that preset.
    if (!string.IsNullOrEmpty(argument)) {
        if (presets.ContainsKey(argument)) {
            ApplyPreset(presets[argument]);
            Echo("Applied preset: " + argument);
        } else {
            Echo("Preset not found: " + argument);
            // Additional argument handling can be added here.
        }
    }

    // --- Update Each Light Group ---
    foreach (var kvp in lightGroups) {
        LightGroup lg = kvp.Value;
        if (lg.Config == null) continue; // Skip groups with no preset config.
        
        // Handle sequence delay: if the activation timer is still counting down, skip updating this group.
        if (lg.ActivationTimer > 0) {
            lg.ActivationTimer -= dt;
            continue;
        }
        
        // On first activation, initialize behavior based on mode.
        if (!lg.Activated) {
            lg.Activated = true;
            if (lg.Config.Mode == "stable") {
                // Immediately switch on to the target color.
                foreach (var light in lg.Lights) {
                    light.Enabled = true;
                    light.Color = lg.Config.TargetColor;
                }
            } else if (lg.Config.Mode == "flicker") {
                // Start flicker mode.
                lg.FlickerTimer = 0f;
                foreach (var light in lg.Lights) {
                    light.Enabled = true;
                    light.Color = lg.Config.TargetColor;
                }
            } else if (lg.Config.Mode == "fade") {
                // Begin fade mode (start from black).
                lg.FadeProgress = 0f;
                foreach (var light in lg.Lights) {
                    light.Enabled = true;
                    light.Color = new Color(0, 0, 0);
                }
            }
        } else {
            // Group is already activated; update behavior according to its mode.
            if (lg.Config.Mode == "flicker") {
                if (lg.FlickerTimer < lg.Config.FlickerDuration) {
                    lg.FlickerTimer += dt;
                    // Randomly toggle the group's lights based on flicker speed.
                    if (random.NextDouble() < lg.Config.FlickerSpeed) {
                        bool newState = !lg.Lights[0].Enabled; // Assume all lights share the same state.
                        foreach (var light in lg.Lights) {
                            light.Enabled = newState;
                        }
                    }
                    // Once flicker duration expires, force the lights on at the target color.
                    if (lg.FlickerTimer >= lg.Config.FlickerDuration) {
                        foreach (var light in lg.Lights) {
                            light.Enabled = true;
                            light.Color = lg.Config.TargetColor;
                        }
                    }
                }
            } else if (lg.Config.Mode == "fade") {
                if (lg.FadeProgress < 1f) {
                    lg.FadeProgress += lg.Config.FadeSpeed * dt;
                    if (lg.FadeProgress > 1f) lg.FadeProgress = 1f;
                    // Interpolate each color channel from 0 (black) to the target value.
                    int r = (int)Lerp(0, lg.Config.TargetColor.R, lg.FadeProgress);
                    int g = (int)Lerp(0, lg.Config.TargetColor.G, lg.FadeProgress);
                    int b = (int)Lerp(0, lg.Config.TargetColor.B, lg.FadeProgress);
                    Color newColor = new Color(r, g, b);
                    foreach (var light in lg.Lights) {
                        light.Color = newColor;
                    }
                }
            }
            // Stable mode requires no further updates.
        }
    }
}
