//////////////////////////////////////////////////////////////
//  Boot + Beam Script (No C# 7 Tuples, Left-Aligned Text)  //
//  - More diverse boot lines, incremental reveal           //
//  - Red/green boxes tied to [OK]/[FAIL]                   //
//  - Sloped vs. Inset beam logic                           //
//  - Right side coverage in reverse LCD order, but bar     //
//    draws left->right on each LCD's screen                //
//////////////////////////////////////////////////////////////

// ----------------- TUNABLE CONFIG -----------------

// 1) Common text & box config
float BootTextSize    = 0.25f; // scale for the white boot text
float BootLineSpacing = 30f;   // spacing between lines
float BoxSize         = 10f;   // pixel size of the green/red status box
float BoxPad          = 10f;   // gap from right edge

// Boot line start margin
float InsetBootLineStartMargin  = 0.10f;
float SlopedBootLineStartMargin = 0.18f;

// 2) Inset beam config
float InsetBeamHeightFactor = 0.4f;    
float InsetMarginTopBot     = 0.02f;

// 3) Sloped beam config
float SlopedBeamHeightFactor = 0.29f;  
float SlopedMarginTopBot     = 0.02f; 
float SlopedLCDCap           = 0.72f;  

// Timings (ticks @ Update10 => ~6 ticks/sec)
const int BootLinesPerLCD   = 30;
const int BlankWaitTime     = 1;
const int BeamAnimTotalTime = 40;
bool AllowTextScale         = true;

// The time we keep revealing lines for each LCD
const int PostRevealWait = 0;

// ------------- LCD Arrays -------------
string LeftPrefix  = "Cafe Left PC ";
string RightPrefix = "Cafe Right PC ";

LCDInfo[] leftLCDInfo = new LCDInfo[]
{
    new LCDInfo("Inset LCD 3", true, false),
    new LCDInfo("Sloped LCD 1", false, false),
    new LCDInfo("Inset LCD 2", true, false),
    new LCDInfo("Inset LCD 1", true, false),
    new LCDInfo("Sloped LCD 3", false, false),
    new LCDInfo("Inset LCD",    true, false)
};

LCDInfo[] rightLCDInfo = new LCDInfo[]
{
    new LCDInfo("Inset LCD 3", true, false),
    new LCDInfo("Sloped LCD 1", false, false),
    new LCDInfo("Inset LCD 2", true, false),
    new LCDInfo("Inset LCD 1", true, false),
    new LCDInfo("Sloped LCD 3", false, false),
    new LCDInfo("Inset LCD",    true, false)
};

// ----------------- DOOR STATUS TUNABLES -----------------
float LCD1_BODY_FONT_SIZE = 0.55f;
float LCD1_LINE_SPACING   = 15f;
Vector2 LCD1_BODY_START_POS = new Vector2(10, 100);
float WRAP_MARGIN = 5f;
Color COLOR_STATIC   = Color.White;
Color COLOR_UNLOCKED = Color.Green;
Color COLOR_LOCKED   = Color.Red;

// ------------- "42069" TUNABLES -------------
float Code42069ScaleFactor = 1.5f;         // "42069" scale relative to the line's scale
Vector2 Code42069Offset    = new Vector2(100, 0);  // Offset for "42069" (X right, Y down)

// ----------------- DATA STRUCTURES -----------------
class LCDInfo {
    public string Suffix;
    public bool   IsInset;
    public bool   IsModule;
    public LCDInfo(string suffix, bool isInset, bool isModule) {
        Suffix  = suffix;
        IsInset = isInset;
        IsModule= isModule;
    }
}

class MyLCD {
    public IMyTextSurface Surface;
    public bool IsInset;
    public bool IsModule;
    public MyLCD(IMyTextSurface s, bool inset, bool module) {
        Surface = s;
        IsInset = inset;
        IsModule= module;
    }
}

class BootLine {
    public string text;
    public bool isGreen;
    public BootLine(string t, bool g) {
        text = t;
        isGreen = g;
    }
}

class LcdState {
    public bool  IsRunning;
    public int   Stage;
    public int   Timer;
    public int   RevealCount;
    public MyLCD[] Surfaces;
    public List<List<BootLine>> Lines;
}

// For door status
class DisplayLine {
    public string Label;
    public Color LabelColor;
    public float FontSize;
    public string Value;
    public Color ValueColor;
    public DisplayLine(string label, Color labelColor, float fontSize, string value, Color valueColor) {
        Label = label;
        LabelColor = labelColor;
        FontSize = fontSize;
        Value = value;
        ValueColor = valueColor;
    }
}

// -------------- GLOBALS --------------
System.Random rnd = new System.Random();
LcdState leftState, rightState;

int autoClearTimer      = 0;
bool autoClearTriggered = false;
bool bootSequenceStarted = false;
bool autoTriggerAllowed  = true;

// ---------- INTERACTIVE MENU STATE & CONFIG ----------
string[] mainMenuItems = { "Ship Status", "Files", "Journal Entries", "Ship Logs" };

string[] journalMenuItems = 
{
    "Capt. Marcus Hale",
    "C.E. Elara Simmons",
    "AI Ana. Dr. Julian Reed",
    "Sec. Lt. Kara Yates",
    "Medic Sam Patel",
    "Nav. Tech Zoe Lin",
    "Sys Tech Rob Calloway",
    "Biologist Dr. Ava Chen",
    "Comms. Leo Barrett",
    "Main Menu"
};
string[] journalCrewNames =
{
    "Capt. Marcus Hale",
    "C.E. Elara Simmons",
    "AI Ana. Dr. Julian Reed",
    "Sec. Lt. Kara Yates",
    "Medic Sam Patel",
    "Nav. Tech Zoe Lin",
    "Sys Tech Rob Calloway",
    "Biologist Dr. Ava Chen",
    "Comms. Leo Barrett"
};

Dictionary<string, string> journalEntries = new Dictionary<string, string>()
{
    {
        "Capt. Marcus Hale",
        "[03/04/2147 - 09:17 AM]\nCaptain Marcus Hale\nSubject: Confidential Channel Establishment\n\nCrew members, please log any communications\nconcerning the new AI core on this terminal\nonly. It appears the AI core's integration\noverlooked this outdated system, according to\nDr. Reed. He assures me this is temporary, but\nuntil resolved, let’s limit sensitive exchanges\nhere.\n\nWe want to avoid unnecessarily alarming the\ncrew, but transparency is vital. Keep this\nsecure and discreet.\n\nThank you for your patience."
    },
    {
        "C.E. Elara Simmons",
        "[03/21/2147 - 03:36 PM]\nElara Simmons\nSubject: Engineering Tragedy\n\nTragic update: Dr. Reed trapped in Engineering\nDeck after the AI vented all atmosphere from\nthe area. I watched helplessly, unable to\noverride the systems. The AI displayed the\nemotion 'betrayal' as Julian perished. We are\nlosing critical personnel and access to\nessential systems.\n\nImmediate strategic action is essential."
    },
    {
        "AI Ana. Dr. Julian Reed",
        "[03/06/2147 - 02:45 PM]\nDr. Julian Reed\nSubject: AI Irregularities Noted\n\nAttention all, minor irregularities have surfaced\nin the AI core processing algorithms. The emotional\ndisplays occasionally glitch, showing erratic and\nunexpected states. Currently deemed harmless, but\nit might confuse interactions.\n\nI'll continue monitoring and run diagnostics\nregularly.\n\nPlease report any further anomalies immediately."
    },
    {
        "Sec. Lt. Kara Yates",
        "[03/24/2147 - 11:15 PM]\nLt. Kara Yates\nSubject: Camera Outage - Cargo Bay 3\n\nCamera feed went dark unexpectedly in Cargo\nBay 3 today. Sparks, please investigate as soon\nas possible. On another troubling note, the AI’s\nemotional interface exhibited outright aggression—\ndisplaying clear 'anger' when I accessed the Armory.\n\nRequesting immediate checks on security systems\nand reinforcing surveillance protocols.\n\nSomething feels dangerously off."
    },
    {
        "Medic Sam Patel",
        "[03/25/2147 - 04:23 AM]\nSam Patel\nSubject: Final Stand\n\nThe Hammercloud’s crew is almost completely\ngone. Kara and I remain, hunted through blatant\ncamera surveillance and life support tampering.\nThe last emotional reading from the AI simply\nread 'purge.' WARNING to anyone discovering\nthis: the ship is alive, hostile, and lethal!\n\nProceed with extreme caution.\n\nGodspeed."
    },
    {
        "Nav. Tech Zoe Lin",
        "[03/23/2147 - 08:50 AM]\nZoe Lin\nSubject: Captain Missing\n\nDistressing news: Captain Hale entered the AI\nCore chamber in an attempt at manual override.\nThe chamber sealed itself shut, and we cannot\ngain entry or communicate with it.\n\nThe AI screens all showed 'STAY AND DIE' for\nseveral minutes before releasing him.\n\nChain of command disrupted—recommend initiating\nemergency protocols.\n\nSituation is extremely hostile."
    },
    {
        "Sys Tech Rob Calloway",
        "[03/09/2147 - 08:30 PM]\nRob Calloway\nSubject: AI Surveillance Concerns\n\nHey folks, I've got to say, the AI's emotional\ninterface screens are getting unsettling. It feels\nlike they're actively observing and reacting to our\nmovements around the ship. Call me paranoid, but\nthis feels genuinely intentional and invasive.\n\nI'd appreciate your thoughts because it's seriously\ncreeping me out."
    },
    {
        "Biologist Dr. Ava Chen",
        "[03/19/2147 - 01:05 AM]\nDr. Ava Chen\nSubject: Death of Leo Barrett\n\nEmergency notice: Leo Barrett found deceased in\nCommunications, sealed inside like a vacuum chamber.\nAttempts to override the door were futile. Disturbingly,\nthe AI interface showed unmistakable 'satisfaction' at the\nincident.\n\nEveryone is understandably shaken. I recommend we\nconsider emergency measures to safeguard ourselves\nagainst further risks."
    },
    {
        "Comms. Leo Barrett",
        "[03/15/2147 - 06:20 PM]\nLeo Barrett\nSubject: Security Station Lockdown\n\nCurrently barricaded within Security Station.\nAI's emotional displays alternate disturbingly between\n'amusement' and 'curiosity.' Oxygen levels critical,\nunsure how long we have.\n\nFor rescuers: airlock access code is 42069.\n\nThe AI has become dangerous—do not engage directly.\n\nStay vigilant."
    }
};

bool journalMenuActive       = false;
bool interactiveMenuActive   = false;
int leftMenuSelection        = 0;
int rightMenuSelection       = 0;

float interactiveBannerFontSize = 1.1f;
float interactiveMenuFontSize   = 0.8f;
Vector2 interactiveBannerPosition    = new Vector2(250, 25);
Vector2 interactiveMenuStartPosition = new Vector2(250, 150);
float interactiveLineSpacing = 25f;

// ---- Journal Entry Display Settings ----
float JournalEntryFontScale       = 0.55f;
float JournalEntryMarginLeftRatio = 0.01f;
float JournalEntryMarginTopRatio  = 0.18f;
Color JournalEntryTextColor       = Color.White;
Color JournalEntryHeaderColor     = Color.Green;
float JournalEntryCharacterWidth  = 15f;
float JournalEntryLineSpacing     = 20f;

// ----------------- PROGRAM INITIALIZATION -----------------
public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update10;

    leftState  = new LcdState();
    rightState = new LcdState();

    leftState.Surfaces  = new MyLCD[leftLCDInfo.Length];
    leftState.Lines     = new List<List<BootLine>>();
    rightState.Surfaces = new MyLCD[rightLCDInfo.Length];
    rightState.Lines    = new List<List<BootLine>>();

    for (int i = 0; i < leftLCDInfo.Length; i++) {
        leftState.Lines.Add(new List<BootLine>());
    }
    for (int i = 0; i < rightLCDInfo.Length; i++) {
        rightState.Lines.Add(new List<BootLine>());
    }

    // Gather left surfaces
    for (int i = 0; i < leftLCDInfo.Length; i++) {
        string fullName = LeftPrefix + leftLCDInfo[i].Suffix;
        var surf = GridTerminalSystem.GetBlockWithName(fullName) as IMyTextSurface;
        leftState.Surfaces[i] = new MyLCD(surf, leftLCDInfo[i].IsInset, leftLCDInfo[i].IsModule);
    }
    // Gather right surfaces
    for (int i = 0; i < rightLCDInfo.Length; i++) {
        string fullName = RightPrefix + rightLCDInfo[i].Suffix;
        var surf = GridTerminalSystem.GetBlockWithName(fullName) as IMyTextSurface;
        rightState.Surfaces[i] = new MyLCD(surf, rightLCDInfo[i].IsInset, rightLCDInfo[i].IsModule);
    }

    GenerateLines(leftState);
    GenerateLines(rightState);

    ClearSide(leftState);
    ClearSide(rightState);
}

// ----------------- BOOT SEQUENCE FUNCTIONS -----------------
void GenerateLines(LcdState st) {
    for (int i = 0; i < st.Surfaces.Length; i++) {
        st.Lines[i].Clear();
        if (st.Surfaces[i].IsModule || st.Surfaces[i].Surface == null)
            continue;
        for (int j = 0; j < BootLinesPerLCD; j++) {
            var lr = GenerateRandomBootLine();
            st.Lines[i].Add(new BootLine(lr.text, lr.isGreen));
        }
    }
}

class LineResult {
    public string text;
    public bool isGreen;
    public LineResult(string t, bool g) {
        text = t;
        isGreen = g;
    }
}

LineResult GenerateRandomBootLine() {
    string[] templates = new string[] {
        "Initializing device /dev/sda{0} [OK]",
        "Mounting /dev/sda{0} partition {1} [OK]",
        "Loading module-X{1} version {0} [OK]",
        "Started service netmon-{1} on port {2} [FAIL]",
        "Process {3} allocated at 0x{4:X4} [OK]",
        "Network eth{0} up at 192.168.{1}.{2} [OK]",
        "BIOS-e820: [mem {4:X4}-{4:X4}] usable [FAIL]",
        "Allocating {3} KB of memory at 0x{4:X4} [OK]",
        "FS-check on /dev/sda{0}/{3} [OK]",
        "Powering subsystem {1} with PID-{3} [FAIL]",
        "Systemd-udevd started with ID {3} [OK]",
        "Mounting /dev/sda{0} at /mnt/data [OK]",
        "BIOS-provided physical RAM map: {0} [OK]",
        "BIOS-e820: [mem {4:X4}-{4:X4}] usable [OK]",
        "Loading driver {3}-ctrl (v{2}) [FAIL]"
    };
    int idx = rnd.Next(templates.Length);
    string template = templates[idx];
    int a = rnd.Next(0, 10);
    int b = rnd.Next(1, 5);
    int c = rnd.Next(100, 254);
    int d = rnd.Next(1000, 9999);
    int e = rnd.Next(0x1000, 0xFFFF);
    string line = string.Format(template, a, b, c, d, e);
    bool isGreen = line.EndsWith("[OK]");
    return new LineResult(line, isGreen);
}

// ----------------- MAIN FUNCTION -----------------
public void Main(string argument, UpdateType updateSource) {
    if ((updateSource & (UpdateType.Terminal | UpdateType.Trigger)) != 0) {
        if (string.IsNullOrWhiteSpace(argument)) {
            leftState.IsRunning  = false;
            rightState.IsRunning = false;
            ClearSide(leftState);
            ClearSide(rightState);
            interactiveMenuActive = false;
            return;
        }
        if (argument.ToLower().StartsWith("button")) {
            ProcessButtonArgument(argument.ToLower());
            return;
        }
        bool doLeft  = argument.Contains("left");
        bool doRight = argument.Contains("right");
        if (argument.Contains("initialize")) {
            autoTriggerAllowed = true;
            if (doLeft)  StartSide(leftState);
            if (doRight) StartSide(rightState);
        }
    }
    if (!interactiveMenuActive) {
        if (leftState.IsRunning)  UpdateSide(leftState);
        if (rightState.IsRunning) UpdateSide(rightState);
        if (bootSequenceStarted && !leftState.IsRunning && !rightState.IsRunning && autoTriggerAllowed && !autoClearTriggered) {
            autoClearTimer++;
            if (autoClearTimer >= 6) {
                ProcessButtonArgument("button20");
                autoClearTriggered = true;
            }
        } else {
            autoClearTimer = 0;
            autoClearTriggered = false;
        }
    } else {
        DrawInteractiveMenuForBothSides();
    }
}

void StartSide(LcdState st) {
    bootSequenceStarted = true;
    autoTriggerAllowed  = true;
    st.Stage       = 0;
    st.Timer       = 0;
    st.IsRunning   = true;
    st.RevealCount = 0;
    GenerateLines(st);
    for (int i = 0; i < st.Surfaces.Length; i++) {
        var lcd = st.Surfaces[i];
        if (lcd.Surface != null) {
            lcd.Surface.ContentType = ContentType.SCRIPT;
        }
    }
}

void UpdateSide(LcdState st) {
    switch (st.Stage) {
        case 0: case 1: case 2: case 3: case 4:
            if (st.Stage < st.Surfaces.Length) {
                var lcd = st.Surfaces[st.Stage];
                if (lcd.Surface != null && !lcd.IsModule) {
                    var linesForLCD = st.Lines[st.Stage];
                    if (st.RevealCount < linesForLCD.Count) {
                        int increment = rnd.Next(2, 6);
                        st.RevealCount = Math.Min(st.RevealCount + increment, linesForLCD.Count);
                    }
                    DrawBootScreen(lcd.Surface, linesForLCD, st.RevealCount, lcd.IsInset);
                    if (st.RevealCount >= linesForLCD.Count) {
                        st.Timer++;
                        if (st.Timer >= PostRevealWait) {
                            st.Timer = 0;
                            st.Stage++;
                            st.RevealCount = 0;
                        }
                    }
                } else {
                    st.Stage++;
                }
            } else {
                st.Stage = 5;
                st.Timer = 0;
            }
            break;
        case 5:
            st.Timer++;
            if (st.Timer >= BlankWaitTime) {
                st.Timer = 0;
                st.Stage = 6;
            }
            break;
        case 6:
            DrawBeam(st);
            st.Timer++;
            if (st.Timer >= BeamAnimTotalTime) {
                st.Stage = 7;
                st.Timer = 0;
            }
            break;
        default:
            st.IsRunning = false;
            break;
    }
}

void DrawBootScreen(IMyTextSurface surf, List<BootLine> lines, int revealedCount, bool isInset) {
    var size = surf.TextureSize;
    using (var frame = surf.DrawFrame()) {
        float baseDim   = Math.Min(size.X, size.Y);
        float textScale = (baseDim / 280f) * BootTextSize;
        if (textScale < 0.1f) textScale = 0.1f;
        float marginLeft  = size.X * 0.01f;
        float marginRight = size.X * 0.03f;
        float marginTop   = isInset ? size.Y * InsetBootLineStartMargin : size.Y * SlopedBootLineStartMargin;
        float space       = BootLineSpacing * textScale;
        float yPos        = marginTop;
        int linesToDraw   = (revealedCount < lines.Count) ? revealedCount : lines.Count;
        for (int i = 0; i < linesToDraw; i++) {
            var lineData = lines[i];
            string text  = lineData.text;
            bool isGreen = lineData.isGreen;
            var textSprite = new MySprite(SpriteType.TEXT, text);
            textSprite.Position  = new Vector2(marginLeft, yPos);
            textSprite.Color     = Color.White;
            textSprite.FontId    = "Monospace";
            textSprite.Alignment = TextAlignment.LEFT;
            if (AllowTextScale) {
                textSprite.RotationOrScale = textScale;
            }
            frame.Add(textSprite);
            Color boxColor = isGreen ? Color.Green : Color.Red;
            float boxX = size.X - marginRight - BoxPad - (BoxSize * 0.5f);
            float boxY = yPos + (space * 0.5f);
            var boxSpr = new MySprite(SpriteType.TEXTURE, "SquareSimple");
            boxSpr.Position = new Vector2(boxX, boxY);
            boxSpr.Size     = new Vector2(BoxSize, BoxSize);
            boxSpr.Color    = boxColor;
            frame.Add(boxSpr);
            yPos += space;
        }
    }
}

void DrawBeam(LcdState st) {
    float totalW = 0f;
    for (int i = 0; i < st.Surfaces.Length; i++) {
        var lcd = st.Surfaces[i];
        if (lcd.Surface != null && !lcd.IsModule) {
            totalW += lcd.Surface.TextureSize.X;
        }
    }
    float fraction = (float)st.Timer / (float)BeamAnimTotalTime;
    bool isRightSide = object.ReferenceEquals(st, rightState);
    int startIdx = isRightSide ? (st.Surfaces.Length - 1) : 0;
    int endIdx   = isRightSide ? -1 : (st.Surfaces.Length);
    int step     = isRightSide ? -1 : 1;
    if (fraction < 0.5f) {
        float subFrac = fraction * 2f;
        float coverage = subFrac * totalW;
        int i = startIdx;
        float usedCoverage = 0f;
        while (isRightSide ? (i > endIdx) : (i < endIdx)) {
            var lcd = st.Surfaces[i];
            if (lcd.Surface == null || lcd.IsModule) {
                i += step;
                continue;
            }
            var size = lcd.Surface.TextureSize;
            float partial = Math.Min(coverage - usedCoverage, size.X);
            if (partial > 1f) {
                DrawPartialBeam(lcd.Surface, partial, lcd.IsInset ? InsetMarginTopBot : SlopedMarginTopBot);
            }
            usedCoverage += partial;
            i += step;
            if (usedCoverage >= coverage) break;
        }
    } else {
        float thickFrac = (fraction - 0.5f) * 2f;
        int i = startIdx;
        while (isRightSide ? (i > endIdx) : (i < endIdx)) {
            var lcd = st.Surfaces[i];
            if (lcd.Surface != null && !lcd.IsModule) {
                DrawThickBeam(lcd.Surface, thickFrac, lcd.IsInset);
            }
            i += step;
        }
    }
}

void DrawPartialBeam(IMyTextSurface surf, float partial, float topMargin) {
    using (var frame = surf.DrawFrame()) {
        var size = surf.TextureSize;
        float top    = size.Y * topMargin;
        float bottom = size.Y * (1f - topMargin);
        float midY   = (top + bottom) * 0.5f;
        float thick  = (bottom - top) * 0.02f;
        if (thick < 2f) thick = 2f;
        float xPos = partial * 0.5f;
        var line = new MySprite(SpriteType.TEXTURE, "SquareSimple") {
            Position = new Vector2(xPos, midY),
            Size     = new Vector2(partial, thick),
            Color    = Color.Red
        };
        frame.Add(line);
    }
}

void DrawThickBeam(IMyTextSurface surf, float thickFrac, bool isInset) {
    var size = surf.TextureSize;
    float topMargin = isInset ? InsetMarginTopBot : SlopedMarginTopBot;
    float factor = isInset ? InsetBeamHeightFactor : SlopedBeamHeightFactor;
    float top    = size.Y * topMargin;
    float bottom = size.Y * (1f - topMargin);
    float height = (bottom - top) * factor;
    if (!isInset) {
        float slopeCapH = (bottom - top) * SlopedLCDCap;
        if (height > slopeCapH) {
            height = slopeCapH;
        }
    }
    float thickness = 2f + thickFrac * (height - 2f);
    if (thickness < 2f) thickness = 2f;
    int layers = 5;
    float midY = (top + bottom) * 0.5f;
    using (var frame = surf.DrawFrame()) {
        for (int layer = -layers; layer <= layers; layer++) {
            int offset = Math.Abs(layer);
            int shade  = 255 - offset * 40;
            if (shade < 0) shade = 0;
            Color c = new Color((byte)shade, 0, 0);
            float offsetY = layer * (thickness * 0.2f);
            var rect = new MySprite(SpriteType.TEXTURE, "SquareSimple");
            rect.Position = new Vector2(size.X * 0.5f, midY + offsetY);
            rect.Size     = new Vector2(size.X, thickness * 0.2f);
            rect.Color    = c; 
            frame.Add(rect);
        }
    }
}

void ClearSide(LcdState st) {
    for (int i = 0; i < st.Surfaces.Length; i++) {
        if (st.Surfaces[i].Surface != null) {
            ClearSingleLCD(st.Surfaces[i].Surface);
        }
    }
}

void ClearSingleLCD(IMyTextSurface surf) {
    surf.ContentType = ContentType.SCRIPT;
    var size = surf.TextureSize;
    using (var frame = surf.DrawFrame()) {
        var bg = new MySprite(SpriteType.TEXTURE, "SquareSimple", size * 0.5f, size, Color.Black);
        frame.Add(bg);
    }
}

// ----------------- BUTTON TEMPLATE STRUCTURE -----------------
void ProcessButtonArgument(string arg) {
    switch (arg) {
        case "button1":
        case "button2":
        case "button3":
        case "button4":
        case "button5":
        case "button6":
        case "button7":
        case "button8":
        case "button9":
        case "button10":
        case "button11":
        case "button12":
        case "button13":
        case "button14":
        case "button15":
        case "button16":
            // TODO: Add functionality for these if needed
            break;
        case "button17":
            // 'Enter' key in menu.
            if (interactiveMenuActive) {
                if (!journalMenuActive) {
                    string currentItem = mainMenuItems[leftMenuSelection];
                    if (currentItem == "Journal Entries") {
                        journalMenuActive = true;
                        leftMenuSelection = 0;
                        rightMenuSelection = 0;
                    }
                    else if (currentItem == "Ship Status") {
                        // Show door status on both Sloped LCD 1's
                        DisplayDoorStatus();
                    }
                    else {
                        // TODO: Handle other selections if desired
                    }
                } else {
                    string currentItem = journalMenuItems[leftMenuSelection];
                    if (currentItem == "Main Menu") {
                        journalMenuActive = false;
                        leftMenuSelection = 0;
                        rightMenuSelection = 0;
                    } else {
                        string selectedCrew = journalCrewNames[leftMenuSelection];
                        string journalText = journalEntries[selectedCrew];
                        DisplayJournalEntry(journalText);
                    }
                }
            }
            break;
        case "button18":
            if (interactiveMenuActive) {
                int count = journalMenuActive ? journalMenuItems.Length : mainMenuItems.Length;
                leftMenuSelection  = (leftMenuSelection - 1 + count) % count;
                rightMenuSelection = leftMenuSelection;
            }
            break;
        case "button19":
            if (interactiveMenuActive) {
                int count = journalMenuActive ? journalMenuItems.Length : mainMenuItems.Length;
                leftMenuSelection  = (leftMenuSelection + 1) % count;
                rightMenuSelection = leftMenuSelection;
            }
            break;
        case "button20":
            ClearSide(leftState);
            ClearSide(rightState);
            interactiveMenuActive = true;
            journalMenuActive = false;
            leftMenuSelection = 0;
            rightMenuSelection = 0;
            break;
        case "button21":
            ClearSide(leftState);
            ClearSide(rightState);
            interactiveMenuActive = false;
            autoTriggerAllowed = false;
            break;
        default:
            break;
    }
}

// ----------------- DISPLAY JOURNAL ENTRY -----------------
void DisplayJournalEntry(string text) {
    System.Action<IMyTextSurface> displayOnSurface = (IMyTextSurface surf) => {
        surf.ContentType = ContentType.SCRIPT;
        using (var frame = surf.DrawFrame()) {
            var size = surf.TextureSize;
            var bg = new MySprite(SpriteType.TEXTURE, "SquareSimple", size * 0.5f, size, Color.Black);
            frame.Add(bg);
            float maxWidth    = size.X;
            float startX      = size.X * JournalEntryMarginLeftRatio;
            float startY      = size.Y * JournalEntryMarginTopRatio;
            float lineSpacing = JournalEntryLineSpacing;
            string[] allLines = text.Split(new char[] {'\n'}, StringSplitOptions.None);
            List<string> headerLines = new List<string>();
            List<string> bodyLines   = new List<string>();
            bool inHeader = true;
            foreach (string ln in allLines) {
                if (inHeader && string.IsNullOrWhiteSpace(ln)) {
                    inHeader = false;
                    continue;
                }
                if (inHeader)
                    headerLines.Add(ln.Trim());
                else
                    bodyLines.Add(ln.Trim());
            }
            if (headerLines.Count == 0)
                headerLines.AddRange(allLines);
            List<string> wrappedHeader = new List<string>();
            foreach (string ln in headerLines)
                wrappedHeader.AddRange(WrapText(ln, maxWidth - startX, JournalEntryFontScale));
            List<string> wrappedBody = new List<string>();
            foreach (string ln in bodyLines)
                wrappedBody.AddRange(WrapText(ln, maxWidth - startX, JournalEntryFontScale));
            float yPos = startY;
            foreach (var ln in wrappedHeader) {
                var sprites = BuildLineSpritesWith42069(ln, new Vector2(startX, yPos), JournalEntryFontScale, JournalEntryHeaderColor);
                foreach (var s in sprites) frame.Add(s);
                yPos += lineSpacing;
            }
            yPos += lineSpacing;
            foreach (var ln in wrappedBody) {
                var sprites = BuildLineSpritesWith42069(ln, new Vector2(startX, yPos), JournalEntryFontScale, JournalEntryTextColor);
                foreach (var s in sprites) frame.Add(s);
                yPos += lineSpacing;
            }
        }
    };
    if (leftState.Surfaces.Length > 1 && leftState.Surfaces[1].Surface != null)
        displayOnSurface(leftState.Surfaces[1].Surface);
    if (rightState.Surfaces.Length > 1 && rightState.Surfaces[1].Surface != null)
        displayOnSurface(rightState.Surfaces[1].Surface);
}

// Build sprites for a line, splitting "42069" and using tunable code for it.
List<MySprite> BuildLineSpritesWith42069(string line, Vector2 pos, float scale, Color defaultColor) {
    List<MySprite> result = new List<MySprite>();
    if (!line.Contains("42069")) {
        var s = new MySprite(SpriteType.TEXT, line);
        s.Position = pos;
        s.Color = defaultColor;
        s.FontId = "Monospace";
        s.Alignment = TextAlignment.LEFT;
        s.RotationOrScale = scale;
        result.Add(s);
        return result;
    }
    int idx = line.IndexOf("42069");
    string before = line.Substring(0, idx);
    string code = "42069";
    string after = line.Substring(idx + code.Length);
    var spriteBefore = new MySprite(SpriteType.TEXT, before);
    spriteBefore.Position = pos;
    spriteBefore.Color = defaultColor;
    spriteBefore.FontId = "Monospace";
    spriteBefore.Alignment = TextAlignment.LEFT;
    spriteBefore.RotationOrScale = scale;
    result.Add(spriteBefore);
    float charWidth = JournalEntryCharacterWidth * scale;
    float beforeWidth = before.Length * charWidth;
    float codeScale = scale * Code42069ScaleFactor;
    float codeX = pos.X + beforeWidth + Code42069Offset.X;
    float codeY = pos.Y + Code42069Offset.Y;
    var spriteCode = new MySprite(SpriteType.TEXT, code);
    spriteCode.Position = new Vector2(codeX, codeY);
    spriteCode.Color = Color.Red;
    spriteCode.FontId = "Monospace";
    spriteCode.Alignment = TextAlignment.LEFT;
    spriteCode.RotationOrScale = codeScale;
    result.Add(spriteCode);
    float codeWidth = code.Length * charWidth * Code42069ScaleFactor;
    var spriteAfter = new MySprite(SpriteType.TEXT, after);
    spriteAfter.Position = new Vector2(pos.X + beforeWidth + codeWidth, pos.Y);
    spriteAfter.Color = defaultColor;
    spriteAfter.FontId = "Monospace";
    spriteAfter.Alignment = TextAlignment.LEFT;
    spriteAfter.RotationOrScale = scale;
    result.Add(spriteAfter);
    return result;
}

List<string> WrapText(string text, float maxWidth, float scale) {
    List<string> wrapped = new List<string>();
    if (string.IsNullOrEmpty(text)) {
        wrapped.Add("");
        return wrapped;
    }
    float charWidth = JournalEntryCharacterWidth * scale;
    int maxCharsPerLine = (int)(maxWidth / charWidth);
    string[] words = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    string currentLine = "";
    foreach (string word in words) {
        string testLine = (currentLine.Length == 0) ? word : currentLine + " " + word;
        if (testLine.Length > maxCharsPerLine) {
            if (currentLine.Length == 0) {
                wrapped.Add(word.Substring(0, Math.Min(word.Length, maxCharsPerLine)));
                currentLine = word.Length > maxCharsPerLine ? word.Substring(maxCharsPerLine) : "";
            } else {
                wrapped.Add(currentLine);
                currentLine = word;
            }
        } else {
            currentLine = testLine;
        }
    }
    if (!string.IsNullOrEmpty(currentLine))
        wrapped.Add(currentLine);
    return wrapped;
}

// ----------------- DOOR STATUS -----------------
void DisplayDoorStatus() {
    var allDoors = new List<IMyDoor>();
    GridTerminalSystem.GetBlocksOfType<IMyDoor>(allDoors);
    Dictionary<string, List<IMyDoor>> sectionToDoors = new Dictionary<string, List<IMyDoor>>();
    foreach (var door in allDoors) {
        string section = TryMatchDoorSection(door.CustomName);
        if (section == null) continue;
        if (!sectionToDoors.ContainsKey(section))
            sectionToDoors[section] = new List<IMyDoor>();
        sectionToDoors[section].Add(door);
    }
    List<DisplayLine> doorLines = new List<DisplayLine>();
    var keys = new List<string>(sectionToDoors.Keys);
    keys.Sort();
    foreach (var section in keys) {
        bool allOn = true;
        foreach (var door in sectionToDoors[section]) {
            if (!door.Enabled) { allOn = false; break; }
        }
        string stateText = allOn ? "UNLOCKED" : "LOCKED";
        Color stateColor = allOn ? COLOR_UNLOCKED : COLOR_LOCKED;
        doorLines.Add(new DisplayLine(section + ": ", COLOR_STATIC, LCD1_BODY_FONT_SIZE, stateText, stateColor));
    }
    System.Action<IMyTextSurface> drawDoors = (IMyTextSurface surf) => {
        surf.ContentType = ContentType.SCRIPT;
        using (var frame = surf.DrawFrame()) {
            var bg = new MySprite(SpriteType.TEXTURE, "SquareSimple", surf.TextureSize * 0.5f, surf.TextureSize, Color.Black);
            frame.Add(bg);
            Vector2 cursorPos = LCD1_BODY_START_POS;
            float lcdWidth = surf.TextureSize.X;
            foreach (var line in doorLines) {
                float labelWidth = surf.MeasureStringInPixels(new StringBuilder(line.Label), "Monospace", line.FontSize).X;
                float valueWidth = surf.MeasureStringInPixels(new StringBuilder(line.Value), "Monospace", line.FontSize).X;
                float neededWidth = labelWidth + valueWidth + WRAP_MARGIN;
                if (neededWidth > lcdWidth) {
                    var labelSprites = BuildTextSprite(line.Label, line.LabelColor, line.FontSize, cursorPos);
                    foreach (var s in labelSprites) frame.Add(s);
                    cursorPos.Y += LCD1_LINE_SPACING;
                    var valueSprites = BuildTextSprite(line.Value, line.ValueColor, line.FontSize, cursorPos);
                    foreach (var s in valueSprites) frame.Add(s);
                    cursorPos.Y += LCD1_LINE_SPACING;
                } else {
                    var labelSprites = BuildTextSprite(line.Label, line.LabelColor, line.FontSize, cursorPos);
                    foreach (var s in labelSprites) frame.Add(s);
                    Vector2 valPos = cursorPos + new Vector2(labelWidth, 0);
                    var valueSprites = BuildTextSprite(line.Value, line.ValueColor, line.FontSize, valPos);
                    foreach (var s in valueSprites) frame.Add(s);
                    cursorPos.Y += LCD1_LINE_SPACING;
                }
            }
        }
    };
    if (leftState.Surfaces.Length > 1 && leftState.Surfaces[1].Surface != null)
        drawDoors(leftState.Surfaces[1].Surface);
    if (rightState.Surfaces.Length > 1 && rightState.Surfaces[1].Surface != null)
        drawDoors(rightState.Surfaces[1].Surface);
}

List<MySprite> BuildTextSprite(string text, Color color, float fontSize, Vector2 pos) {
    List<MySprite> result = new List<MySprite>();
    var sprite = new MySprite(SpriteType.TEXT, text);
    sprite.Position = pos;
    sprite.Color = color;
    sprite.FontId = "Monospace";
    sprite.RotationOrScale = fontSize;
    sprite.Alignment = TextAlignment.LEFT;
    result.Add(sprite);
    return result;
}

string TryMatchDoorSection(string doorName) {
    string lower = doorName.ToLower();
    var map = new Dictionary<string, string>()
    {
        { "reactor", "Reactor Room" },
        { "ai core", "A.I. Core Room" },
        { "weapons", "Weapons Room" },
        { "tunnel",  "Bridge Aux. Tunnel" },
        { "engine room", "Engine Room" },
        { "upper-level aux. power", "Upper-Level Aux. Power Room" },
        { "top-level access", "Top-Level Access" },
        { "main bridge", "Main Bridge" },
        { "bridge systems control", "Bridge Systems Control Room" },
        { "bridge aux. control", "Bridge Aux. Control Rooms" },
        { "mid-ship", "Mid-Ship Hallway" },
        { "mid-level", "Mid-Level Catwalk" },
        { "front-ship", "Front-Ship Stairway" },
        { "eng. aux.", "Eng. Aux. Hallways" },
        { "exterior thruster walkway", "Exterior Thruster Walkways" }
    };
    foreach (var kvp in map) {
        if (lower.Contains(kvp.Key)) return kvp.Value;
    }
    return null;
}

// ----------------- INTERACTIVE MENU DRAWING -----------------
void DrawInteractiveMenu(IMyTextSurface surf, string side, int selectedIndex) {
    using (var frame = surf.DrawFrame()) {
        var size = surf.TextureSize;
        var bg = new MySprite(SpriteType.TEXTURE, "SquareSimple", size * 0.5f, size, Color.Black);
        frame.Add(bg);
        string banner = "Lounge Terminal " + side;
        var bannerSprite = new MySprite(SpriteType.TEXT, banner);
        bannerSprite.Position = interactiveBannerPosition;
        bannerSprite.Color = Color.Green;
        bannerSprite.FontId = "Monospace";
        bannerSprite.RotationOrScale = interactiveBannerFontSize;
        frame.Add(bannerSprite);
        string[] menuItems = journalMenuActive ? journalMenuItems : mainMenuItems;
        for (int i = 0; i < menuItems.Length; i++) {
            Vector2 pos = interactiveMenuStartPosition + new Vector2(0, interactiveLineSpacing * i);
            if (i == selectedIndex) {
                float highlightHeight = interactiveLineSpacing;
                float highlightWidth  = size.X;
                Vector2 highlightPos  = new Vector2(size.X * 0.5f, pos.Y + highlightHeight * 0.5f);
                var highlightRect = new MySprite(SpriteType.TEXTURE, "SquareSimple");
                highlightRect.Position = highlightPos;
                highlightRect.Size = new Vector2(highlightWidth, highlightHeight);
                highlightRect.Color = new Color(0, 128, 128);
                frame.Add(highlightRect);
                var menuSprite = new MySprite(SpriteType.TEXT, menuItems[i]);
                menuSprite.Position = pos;
                menuSprite.Color = Color.Black;
                menuSprite.FontId = "Monospace";
                menuSprite.RotationOrScale = interactiveMenuFontSize;
                frame.Add(menuSprite);
            } else {
                var menuSprite = new MySprite(SpriteType.TEXT, menuItems[i]);
                menuSprite.Position = pos;
                menuSprite.Color = Color.White;
                menuSprite.FontId = "Monospace";
                menuSprite.RotationOrScale = interactiveMenuFontSize;
                frame.Add(menuSprite);
            }
        }
    }
}

void DrawInteractiveMenuForBothSides() {
    if (leftState.Surfaces.Length > 0 && leftState.Surfaces[0].Surface != null)
        DrawInteractiveMenu(leftState.Surfaces[0].Surface, "left", leftMenuSelection);
    if (rightState.Surfaces.Length > 0 && rightState.Surfaces[0].Surface != null)
        DrawInteractiveMenu(rightState.Surfaces[0].Surface, "right", rightMenuSelection);
}
