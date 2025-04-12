// Rotating Gear Animation with Flashing Border
// Displays "Ready to Initialize..." with a customizable rotating gear.

#region Customizable Settings
// Block name of the Holo LCD Panel
const string LCD_NAME = "Weapon Room Finish Button Holo LCD";

// Colors
readonly Color PRIMARY_COLOR = Color.Green; // Color of the main elements
readonly Color SECONDARY_COLOR = Color.Yellow; // Color of the rotating gear
readonly Color BACKGROUND_COLOR = Color.Black; // Background color of the LCD

// Animation Timings
const int MAX_FRAMES = 60; // Number of frames for a full rotation (higher = slower rotation)

// Rotating Gear Sprite
const string GEAR_SPRITE = "Screen_LoadingBar2"; // Change this to try different gear-like sprites
#endregion

// Internal Variables (do not modify unless needed)
IMyTextPanel lcdPanel;
int frameCounter = 0;

public Program()
{
    // Initialize the LCD Panel
    lcdPanel = GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextPanel;

    if (lcdPanel == null)
    {
        Echo($"Error: LCD Panel '{LCD_NAME}' not found!");
        return;
    }

    // Configure the LCD Panel
    lcdPanel.ContentType = ContentType.SCRIPT; // Enables custom drawing
    lcdPanel.ScriptBackgroundColor = BACKGROUND_COLOR; // Set the background color

    // Set the update frequency for the animation
    Runtime.UpdateFrequency = UpdateFrequency.Update10; // Update every 10 ticks (~6 FPS)
}

public void Main(string argument, UpdateType updateSource)
{
    if (lcdPanel == null)
    {
        Echo("Error: LCD Panel not initialized!");
        return;
    }

    // Main animation drawing function
    DrawGearAnimation(lcdPanel);
}

void DrawGearAnimation(IMyTextPanel panel)
{
    // Start a new drawing frame for the LCD
    var frame = panel.DrawFrame();

    // Add a black background to fill the LCD
    var bg = new MySprite()
    {
        Type = SpriteType.TEXTURE,
        Data = "SquareSimple", // Simple square for background
        Position = new Vector2(256, 256), // Center of the LCD
        Size = new Vector2(512, 512), // Size of the LCD
        Color = BACKGROUND_COLOR,
        Alignment = TextAlignment.CENTER
    };
    frame.Add(bg);

    // Draw the rotating gear
    DrawRotatingGear(frame, frameCounter);

    // Draw the pulsing text
    DrawPulsingText(frame, "Ready to Initialize...", frameCounter);

    // Increment the frame counter for the next cycle
    frameCounter++;
    if (frameCounter > MAX_FRAMES) frameCounter = 0;

    // Dispose of the frame to apply changes
    frame.Dispose();
}

void DrawRotatingGear(MySpriteDrawFrame frame, int frameCount)
{
    // Calculate rotation based on the current frame
    float rotation = (frameCount % MAX_FRAMES) * MathHelper.TwoPi / MAX_FRAMES;

    // Add the gear sprite with rotation
    var gear = new MySprite()
    {
        Type = SpriteType.TEXTURE,
        Data = GEAR_SPRITE, // Gear sprite defined in the settings
        Position = new Vector2(256, 256), // Center of the LCD
        Size = new Vector2(200, 200), // Size of the gear
        Color = SECONDARY_COLOR, // Gear color
        Alignment = TextAlignment.CENTER,
        RotationOrScale = rotation // Rotational angle
    };
    frame.Add(gear);
}

void DrawPulsingText(MySpriteDrawFrame frame, string text, int frameCount)
{
    // Calculate scale for pulsing text (smooth sinusoidal effect)
    float scale = 1.0f + 0.1f * (float)Math.Sin((frameCount % MAX_FRAMES) * MathHelper.TwoPi / MAX_FRAMES);

    // Add the text sprite
    var textSprite = new MySprite()
    {
        Type = SpriteType.TEXT,
        Data = text, // Customizable text
        Position = new Vector2(256, 400), // Position near the bottom of the LCD
        RotationOrScale = scale, // Pulsing size effect
        Color = PRIMARY_COLOR, // Text color
        Alignment = TextAlignment.CENTER,
        FontId = "White" // Font style
    };
    frame.Add(textSprite);
}

public void Save() { }
