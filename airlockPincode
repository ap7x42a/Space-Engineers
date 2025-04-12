// =========================================
// Variables for system state
// =========================================
string correctCode = "42069";
string enteredCode = "";
int failureCount = 0;
bool isChallengeActive = false;

// =========================================
// LCD, sound block, broadcast, timer block
// =========================================
IMyTextPanel lcdPanel;
IMySoundBlock soundBlock;
IMyFunctionalBlock broadcastController;
IMyTimerBlock authorizedTimerBlock;

// =========================================
// Adjustable display settings
// =========================================
float baseYOffset = 50;    // Starting offset for text
float lineSpacing = 40;    // Spacing between lines
Vector2 screenCenter = new Vector2(0, 0);  // Center of the screen (adjustable)
float headerFontSize = 1.7f;              // Font size for header text
float bodyFontSize = 1.0f;                // Font size for body text
float pinFontSize = 3.0f;                 // Font size for the PIN display
float bodyTextSpacing = 210;              // Spacing between "DISTRESS BROADCAST:" and "ENABLED" on the same line

// =========================================
// Initialization
// =========================================
public Program()
{
    // Assign block names (update these if needed)
    lcdPanel = GridTerminalSystem.GetBlockWithName("Left Lockdown INPUT LCD") as IMyTextPanel;
    soundBlock = GridTerminalSystem.GetBlockWithName("Airlock Alarm Sound Block 1") as IMySoundBlock;
    broadcastController = GridTerminalSystem.GetBlockWithName("Pincode Broadcast Controller") as IMyFunctionalBlock;
    authorizedTimerBlock = GridTerminalSystem.GetBlockWithName("Authorized Timer Block") as IMyTimerBlock;

    // LCD setup
    if (lcdPanel != null)
    {
        lcdPanel.ContentType = ContentType.SCRIPT;
        lcdPanel.Script = ""; // Clear any custom scripts

        var size = lcdPanel.SurfaceSize;
        screenCenter = new Vector2(size.X / 2, size.Y / 2);
    }

    Runtime.UpdateFrequency = UpdateFrequency.None; // Only triggered manually by button inputs
    DisplayPrompt(); // Display initial lockdown message
}

// =========================================
// Main function (Triggered by button args)
// =========================================
public void Main(string argument, UpdateType updateSource)
{
    if (argument.Length == 1 && char.IsDigit(argument[0]))
    {
        ProcessInput(argument);
    }
    else if (argument == "Submit")
    {
        ValidateCode();
    }
    else if (argument == "Cancel")
    {
        ResetCode();
    }
}

// =========================================
// Append a digit to the entered code
// =========================================
void ProcessInput(string input)
{
    int number;
    if (enteredCode.Length < 5 && int.TryParse(input, out number))
    {
        enteredCode += input;
        DisplayPrompt();
    }
}

// =========================================
// Validate the entered code
// =========================================
void ValidateCode()
{
    if (enteredCode == correctCode)
    {
        DisplayAuthorized();
        TriggerAuthorizedTimerBlock();
        // (Removed door operations)
    }
    else
    {
        failureCount++;
        if (failureCount >= 3)
        {
            isChallengeActive = true;
            UpdatePromptToChallenge();
        }
        else
        {
            DisplayFailure();
        }
    }
}

// =========================================
// Reset the code entry
// =========================================
void ResetCode()
{
    enteredCode = "";
    if (isChallengeActive)
    {
        UpdatePromptToChallenge();
    }
    else
    {
        DisplayPrompt();
    }
}

// =========================================
// Trigger the authorized timer block
// =========================================
void TriggerAuthorizedTimerBlock()
{
    if (authorizedTimerBlock != null)
    {
        authorizedTimerBlock.StartCountdown();
    }
}

// =========================================
// Display the lockdown prompt
// =========================================
void DisplayPrompt()
{
    if (lcdPanel == null) return;

    // Check broadcast controller state
    string broadcastStatus = "ENABLED";
    Color broadcastColor = Color.Green;

    if (broadcastController != null && !broadcastController.Enabled)
    {
        broadcastStatus = "DISABLED";
        broadcastColor = Color.Red;
    }

    // Draw on the LCD
    using (var frame = lcdPanel.DrawFrame())
    {
        float yOffset = baseYOffset;

        // Header text
        frame.Add(CreateTextSprite("HAMMERCLOUD", Color.Red, headerFontSize, new Vector2(screenCenter.X, yOffset)));
        yOffset += lineSpacing * 2;
        frame.Add(CreateTextSprite("EMERGENCY LOCKDOWN", Color.Red, headerFontSize, new Vector2(screenCenter.X, yOffset)));
        yOffset += lineSpacing * 2;

        // Distress broadcast line
        frame.Add(CreateTextSprite("DISTRESS BROADCAST:", Color.White, bodyFontSize, new Vector2(screenCenter.X - bodyTextSpacing / 2, yOffset)));
        frame.Add(CreateTextSprite(broadcastStatus, broadcastColor, bodyFontSize, new Vector2(screenCenter.X + bodyTextSpacing / 2, yOffset)));
        yOffset += lineSpacing;

        // Authorization code required
        frame.Add(CreateTextSprite("AUTHORIZATION CODE REQUIRED:", Color.White, bodyFontSize, new Vector2(screenCenter.X, yOffset)));
        yOffset += lineSpacing;

        // Entered code display
        frame.Add(CreateTextSprite(enteredCode.PadRight(5, '_'), Color.LightBlue, pinFontSize, new Vector2(screenCenter.X, yOffset)));
    }
}

// =========================================
// Display failure message temporarily
// =========================================
void DisplayFailure()
{
    if (lcdPanel == null) return;

    using (var frame = lcdPanel.DrawFrame())
    {
        float yOffset = baseYOffset + lineSpacing * 4; // Show near PIN code area
        frame.Add(CreateTextSprite("FAILURE", Color.Red, pinFontSize, new Vector2(screenCenter.X, yOffset)));
    }

    enteredCode = ""; // Clear code
    Runtime.UpdateFrequency = UpdateFrequency.Once; // Force a redraw next tick
}

// =========================================
// Display authorization success temporarily
// =========================================
void DisplayAuthorized()
{
    if (lcdPanel == null) return;

    using (var frame = lcdPanel.DrawFrame())
    {
        float yOffset = baseYOffset + lineSpacing * 4; // Show near PIN code area
        frame.Add(CreateTextSprite("AUTHORIZED", Color.Green, pinFontSize, new Vector2(screenCenter.X, yOffset)));
    }
}

// =========================================
// Update display for the math challenge
// =========================================
void UpdatePromptToChallenge()
{
    if (lcdPanel == null) return;

    // Play alarm sound if available
    if (soundBlock != null) soundBlock.Play();

    using (var frame = lcdPanel.DrawFrame())
    {
        float yOffset = baseYOffset;

        frame.Add(CreateTextSprite("HAMMERCLOUD", Color.Red, headerFontSize, new Vector2(screenCenter.X, yOffset)));
        yOffset += lineSpacing * 2;
        frame.Add(CreateTextSprite("EMERGENCY LOCKDOWN", Color.Red, headerFontSize, new Vector2(screenCenter.X, yOffset)));
        yOffset += lineSpacing * 2;

        // Check broadcast controller
        string broadcastStatus = "ENABLED";
        Color broadcastColor = Color.Green;
        if (broadcastController != null && !broadcastController.Enabled)
        {
            broadcastStatus = "DISABLED";
            broadcastColor = Color.Red;
        }

        frame.Add(CreateTextSprite("DISTRESS BROADCAST:", Color.White, bodyFontSize, new Vector2(screenCenter.X - bodyTextSpacing / 2, yOffset)));
        frame.Add(CreateTextSprite(broadcastStatus, broadcastColor, bodyFontSize, new Vector2(screenCenter.X + bodyTextSpacing / 2, yOffset)));
        yOffset += lineSpacing;

        // Display challenge
        frame.Add(CreateTextSprite("((6^7 - 5^5) / 2) + 133 = ?", Color.White, bodyFontSize, new Vector2(screenCenter.X, yOffset)));
        yOffset += lineSpacing;

        // Entered code display
        frame.Add(CreateTextSprite(enteredCode.PadRight(5, '_'), Color.LightBlue, pinFontSize, new Vector2(screenCenter.X, yOffset)));
    }
}

// =========================================
// Helper function to create a text sprite
// =========================================
MySprite CreateTextSprite(string text, Color color, float scale, Vector2 position)
{
    var sprite = new MySprite
    {
        Type = SpriteType.TEXT,
        Data = text,
        Position = position,
        RotationOrScale = scale,
        Color = color,
        Alignment = TextAlignment.CENTER
    };
    return sprite;
}
