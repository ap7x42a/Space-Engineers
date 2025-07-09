// EMERGENCY POWER ALERT SCRIPT
// Displays a red "EMERGENCY POWER" alert with scrolling white pseudocode.

const string ALERT_TEXT = "EMERGENCY POWER";
const string SCROLLING_TEXT = @"
ENGAGE AUXILIARY
POWER COUPLING...

SYSTEM CHECK...    
ERROR CODE 0xA31B...    
INITIATING DIAGNOSTICS...    
REACTOR CORE: OFFLINE
LIFE SUPPORT: OFFLINE
ENGINES: OFFLINE
WEAPONS: OFFLINE
BATTERIES: DEPLETED    
";
const float SCROLL_SPEED = 0.2f; // Adjust scrolling speed

IMyTextSurface display;
List<string> scrollingLines = new List<string>();
float scrollPosition = 0f;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10; // Update every 10 ticks (~0.16 seconds)

    // Attempt to find an LCD panel or the programmable block's screen
    display = Me.GetSurface(0); // Default to the first programmable block's screen
    if (display == null)
    {
        Echo("No surface found.");
        return;
    }

    display.ContentType = ContentType.SCRIPT;
    display.ScriptBackgroundColor = Color.Black; // Background is black
    display.ScriptForegroundColor = Color.White; // Default text color

    // Prepare scrolling lines
    scrollingLines = SCROLLING_TEXT.Split('\n').ToList();
}

public void Main(string argument, UpdateType updateSource)
{
    if (display == null) return;

    // Create the emergency power display
    var frame = display.DrawFrame();

    // Draw "EMERGENCY POWER" in red
    Vector2 center = new Vector2(display.SurfaceSize.X / 2, 40); // Position near the top
    DrawText(frame, ALERT_TEXT, center, 1f, Color.Red, TextAlignment.CENTER);

    // Draw scrolling pseudocode underneath
    DrawScrollingText(frame);

    frame.Dispose();
}

// Draws the scrolling white text
void DrawScrollingText(MySpriteDrawFrame frame)
{
    Vector2 startPosition = new Vector2(10, 80); // Starting position below the ALERT_TEXT
    float lineHeight = 20f; // Line spacing
    int linesToShow = (int)((display.SurfaceSize.Y - startPosition.Y) / lineHeight) + 1;

    for (int i = 0; i < linesToShow; i++)
    {
        int lineIndex = (int)(scrollPosition + i) % scrollingLines.Count;
        Vector2 position = startPosition + new Vector2(0, i * lineHeight);
        DrawText(frame, scrollingLines[lineIndex], position, 0.8f, Color.White, TextAlignment.LEFT);
    }

    scrollPosition += SCROLL_SPEED;
    if (scrollPosition >= scrollingLines.Count)
        scrollPosition = 0f;
}

// Draws text onto the screen
void DrawText(MySpriteDrawFrame frame, string text, Vector2 position, float scale, Color color, TextAlignment alignment)
{
    var sprite = new MySprite()
    {
        Type = SpriteType.TEXT,
        Data = text,
        Position = position,
        RotationOrScale = scale,
        Color = color,
        Alignment = alignment,
        FontId = "White"
    };
    frame.Add(sprite);
}
