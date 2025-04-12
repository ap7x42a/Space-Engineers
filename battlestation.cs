List<IMyTextSurface> lcdSurfaces = new List<IMyTextSurface>();
HashSet<IMyTextSurface> insetSurfaces = new HashSet<IMyTextSurface>();
int animationStep = 0;
bool expanding = false;
float expandProgress = 0.01f;
float expandSpeed = 0.02f; // Speed of the expansion phase (adjust as needed)
bool finished = false;
bool triggered = false; // Flag to control triggering
bool useAltPreset = false; // Flag to toggle between color presets

public Program()
{
    // Collect all LCD surfaces and differentiate inset vs sloped
    AddLCDSurface("Weapon Room Control Sloped LCD 18", false);
    AddLCDSurface("Weapon Room Control Inset LCD 17", true);
    AddLCDSurface("Weapon Room Control Inset LCD 16", true);
    AddLCDSurface("Weapon Room Control Inset LCD 15", true);
    AddLCDSurface("Weapon Room Control Inset LCD 14", true);
    AddLCDSurface("Weapon Room Control Sloped LCD 13", false);
    AddLCDSurface("Weapon Room Control Sloped LCD 12", false);
    AddLCDSurface("Weapon Room Control Inset LCD 11", true);
    AddLCDSurface("Weapon Room Control Inset LCD 10", true);
    AddLCDSurface("Weapon Room Control Inset LCD 9", true);
    AddLCDSurface("Weapon Room Control Inset LCD 8", true);
    AddLCDSurface("Weapon Room Control Sloped LCD 7", false);
    AddLCDSurface("Weapon Room Control Sloped LCD 6", false);
    AddLCDSurface("Weapon Room Control Inset LCD 5", true);
    AddLCDSurface("Weapon Room Control Inset LCD 4", true);
    AddLCDSurface("Weapon Room Control Inset LCD 3", true);
    AddLCDSurface("Weapon Room Control Inset LCD 2", true);
    AddLCDSurface("Weapon Room Control Sloped LCD 1", false);

    // Set all LCDs to sprite mode
    foreach (var surface in lcdSurfaces)
    {
        if (surface != null)
        {
            surface.ContentType = ContentType.SCRIPT;
        }
    }

    Runtime.UpdateFrequency = UpdateFrequency.Update10; // Update 60 times per second
}

void AddLCDSurface(string blockName, bool isInset)
{
    var block = GridTerminalSystem.GetBlockWithName(blockName) as IMyTextSurfaceProvider;
    if (block != null)
    {
        var surface = block.GetSurface(0);
        lcdSurfaces.Add(surface);
        if (isInset)
        {
            insetSurfaces.Add(surface);
        }
    }
    else
    {
        Echo($"LCD '{blockName}' not found or not valid.");
    }
}

public void Main(string argument, UpdateType updateSource)
{
    if (argument.Equals("START", StringComparison.OrdinalIgnoreCase))
    {
        triggered = true; // Start the animation
        finished = false; // Reset in case it has already run
        expanding = false; // Reset the animation state
        animationStep = 0; // Reset animation step
        expandProgress = 0.01f; // Reset expand progress
        useAltPreset = false; // Use original color preset
    }
    else if (argument.Equals("ALT", StringComparison.OrdinalIgnoreCase))
    {
        triggered = true; // Start the animation
        finished = false; // Reset in case it has already run
        expanding = false; // Reset the animation state
        animationStep = 0; // Reset animation step
        expandProgress = 0.01f; // Reset expand progress
        useAltPreset = true; // Use alternate color preset
    }

    if (!triggered || finished)
        return;

    if (!expanding)
    {
        // Draw laser animation from right to left
        if (animationStep < lcdSurfaces.Count)
        {
            for (int i = 0; i < lcdSurfaces.Count; i++)
            {
                var surface = lcdSurfaces[i];
                if (surface != null)
                {
                    var frame = surface.DrawFrame();
                    if (i == animationStep)
                    {
                        DrawLaser(frame, surface, expandProgress);
                    }
                    frame.Dispose();
                }
            }
            animationStep++;
        }
        else
        {
            expanding = true;
        }
    }
    else
    {
        // Expand laser vertically
        expandProgress += expandSpeed;

        bool stillExpanding = false;
        foreach (var surface in lcdSurfaces)
        {
            if (surface != null)
            {
                var frame = surface.DrawFrame();

                // Adjust progress speed for inset panels
                float adjustedProgress = expandProgress;
                if (insetSurfaces.Contains(surface))
                {
                    adjustedProgress *= 1.5f; // Inset panels expand 50% faster
                }

                if (adjustedProgress <= 1.0f)
                {
                    stillExpanding = true;
                    DrawVerticalBeam(frame, surface, adjustedProgress);
                }
                else
                {
                    DrawVerticalBeam(frame, surface, 1.0f);
                }
                frame.Dispose();
            }
        }

        if (!stillExpanding)
        {
            finished = true;
            triggered = false; // Reset triggered state
        }
    }
}

void DrawLaser(MySpriteDrawFrame frame, IMyTextSurface surface, float normalizedProgress)
{
    // Calculate visible area for the laser beam
    var size = surface.TextureSize;
    var center = size * 0.5f; // Center of the texture
    float totalHeight = size.Y * normalizedProgress; // Total beam height

    // Limit total height for sloped LCD panels
    if (!insetSurfaces.Contains(surface))
    {
        totalHeight = Math.Min(totalHeight, size.Y * 0.72f); // Perfect cap for sloped panels
    }

    // Choose gradient colors based on the active preset
    Color[] gradientColors = useAltPreset
        ? new Color[] { new Color(1, 1, 1), new Color(1, 1, 1), new Color(1, 1, 1), new Color(1, 1, 1), new Color(1, 1, 1), new Color(1, 1, 1) } // Alternate preset
        : new Color[] { new Color(0, 0, 0), new Color(26, 0, 0), new Color(51, 0, 0), new Color(77, 0, 0), new Color(102, 0, 0), new Color(128, 0, 0) }; // Original preset

    int gradientSteps = gradientColors.Length; // Number of gradient layers
    float stepHeight = totalHeight / gradientSteps; // Height of each gradient layer

    // Draw gradient layers
    for (int i = 0; i < gradientSteps; i++)
    {
        Color stepColor = gradientColors[i];
        float layerHeight = totalHeight - (i * stepHeight); // Decreasing size for each layer

        frame.Add(new MySprite()
        {
            Type = SpriteType.TEXTURE,
            Data = "SquareSimple",
            Position = new Vector2(center.X, center.Y),
            Size = new Vector2(size.X, layerHeight),
            Color = stepColor,
            Alignment = TextAlignment.CENTER
        });
    }
}

void DrawVerticalBeam(MySpriteDrawFrame frame, IMyTextSurface surface, float normalizedProgress)
{
    // Calculate visible area for the vertical beam
    var size = surface.TextureSize;
    var center = size * 0.5f; // Center of the texture
    float totalHeight = size.Y * normalizedProgress; // Total beam height

    // Limit total height for sloped LCD panels
    if (!insetSurfaces.Contains(surface))
    {
        totalHeight = Math.Min(totalHeight, size.Y * 0.72f); // Perfect cap for sloped panels
    }

    // Choose gradient colors based on the active preset
    Color[] gradientColors = useAltPreset
        ? new Color[] { new Color(1, 1, 1), new Color(1, 1, 1), new Color(1, 1, 1), new Color(1, 1, 1), new Color(1, 1, 1), new Color(1, 1, 1) } // Alternate preset
        : new Color[] { new Color(0, 0, 0), new Color(26, 0, 0), new Color(51, 0, 0), new Color(77, 0, 0), new Color(102, 0, 0), new Color(128, 0, 0) }; // Original preset

    int gradientSteps = gradientColors.Length; // Number of gradient layers
    float stepHeight = totalHeight / gradientSteps; // Height of each gradient layer

    // Draw gradient layers
    for (int i = 0; i < gradientSteps; i++)
    {
        Color stepColor = gradientColors[i];
        float layerHeight = totalHeight - (i * stepHeight); // Decreasing size for each layer

        frame.Add(new MySprite()
        {
            Type = SpriteType.TEXTURE,
            Data = "SquareSimple",
            Position = new Vector2(center.X, center.Y),
            Size = new Vector2(size.X, layerHeight),
            Color = stepColor,
            Alignment = TextAlignment.CENTER
        });
    }
}
