// === Turret Lift Controller Script ===
// Commands: extend, retract
// Manages precise timing for turret lift mechanism

// ===== TIMING SETTINGS (Easy to adjust) =====
float WAIT_AFTER_PISTONS_1 = 1.39f;      // Pistons move 1/4 distance (2.5m at 1.8 m/s)
float WAIT_AFTER_INNER_ROTORS = 3.75f;   // Inner rotors move 1/2 range (180° at 8 RPM)
float WAIT_BEFORE_FINAL_PISTONS = 2.0f;  // Delay before second piston reversal
float WAIT_AFTER_OUTER_ROTORS = 4.17f;   // Complete sequence timing
// ===============================================

// Block references
IMyPistonBase leftPiston;
IMyPistonBase rightPiston;
IMyPistonBase innermostPiston;
IMyPistonBase turretTesterBottomPiston;
IMyPistonBase turretTesterPistonMiddle1;
IMyPistonBase turretTesterPistonMiddle2;
IMyPistonBase turretTesterPistonTop1;
IMyPistonBase turretTesterPistonTop2;
IMyPistonBase turretTesterPiston4;
IMyPistonBase turretTesterPiston5;
IMyPistonBase turretTesterPiston6;
IMyMotorStator leftRotor;
IMyMotorStator rightRotor;
IMyMotorStator outerLeftRotor;
IMyMotorStator outerRightRotor;
IMyMotorStator mainRotor;
IMyMotorStator innermostLeftRotor;
IMyMotorStator innermostRightRotor;
IMyTimerBlock liftToArenaTimer;
IMyTimerBlock liftToCraneTimer;

// State tracking
bool isExtending = false;
bool isRetracting = false;
int extendStep = 0;
int retractStep = 0;
int tickCounter = 0;
bool waitingForInnermostRetract = false;
bool waitingForInnermostExtend = false;
const int TICKS_PER_SECOND = 6; // UpdateFrequency.Update10 = 6 ticks per second

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    
    // Initialize blocks and record settings immediately on startup
    InitializeBlocks();
}

public void Main(string argument)
{
    // Handle commands only - blocks already initialized at startup
    if (argument.ToLower() == "extend")
    {
        StartExtendSequence();
        return;
    }
    
    if (argument.ToLower() == "retract")
    {
        StartRetractSequence();
        return;
    }
    
    // Emergency stop command
    if (argument.ToLower() == "stop" || argument.ToLower() == "abort")
    {
        EmergencyStop();
        return;
    }

    // Process extend sequence if active (and only if active)
    if (isExtending)
    {
        ProcessExtendSequence();
    }
    
    // Process retract sequence if active (and only if active)
    if (isRetracting)
    {
        ProcessRetractSequence();
    }
    
    // If no command and no active sequence, do absolutely nothing
}

void InitializeBlocks()
{
    leftPiston = GridTerminalSystem.GetBlockWithName("2nd Innermost Turret Lift Left Piston") as IMyPistonBase;
    rightPiston = GridTerminalSystem.GetBlockWithName("2nd Innermost Turret Lift Right Piston") as IMyPistonBase;
    innermostPiston = GridTerminalSystem.GetBlockWithName("Innermost Turret Lift Piston") as IMyPistonBase;
    turretTesterBottomPiston = GridTerminalSystem.GetBlockWithName("Arena Horizontal Piston 0") as IMyPistonBase;
    turretTesterPistonMiddle1 = GridTerminalSystem.GetBlockWithName("Arena Horizontal Piston 00") as IMyPistonBase;
    turretTesterPistonMiddle2 = GridTerminalSystem.GetBlockWithName("Arena Horizontal Piston 1") as IMyPistonBase;
    turretTesterPistonTop1 = GridTerminalSystem.GetBlockWithName("Arena Horizontal Piston 2") as IMyPistonBase;
    turretTesterPistonTop2 = GridTerminalSystem.GetBlockWithName("Arena Horizontal Piston 3") as IMyPistonBase;
    turretTesterPiston4 = GridTerminalSystem.GetBlockWithName("Arena Horizontal Piston 4") as IMyPistonBase;
    turretTesterPiston5 = GridTerminalSystem.GetBlockWithName("Arena Horizontal Piston 5") as IMyPistonBase;
    turretTesterPiston6 = GridTerminalSystem.GetBlockWithName("Arena Horizontal Piston 6") as IMyPistonBase;
    leftRotor = GridTerminalSystem.GetBlockWithName("2nd Innermost Left Turret Lift Rotor") as IMyMotorStator;
    rightRotor = GridTerminalSystem.GetBlockWithName("2nd Innermost Right Turret Lift Rotor") as IMyMotorStator;
    outerLeftRotor = GridTerminalSystem.GetBlockWithName("Outermost Turret Lift Left Rotor") as IMyMotorStator;
    outerRightRotor = GridTerminalSystem.GetBlockWithName("Outermost Turret Lift Right Rotor") as IMyMotorStator;
    mainRotor = GridTerminalSystem.GetBlockWithName("Main Turret Lift Rotor") as IMyMotorStator;
    innermostLeftRotor = GridTerminalSystem.GetBlockWithName("Innermost Left Turret Lift Rotor") as IMyMotorStator;
    innermostRightRotor = GridTerminalSystem.GetBlockWithName("Innermost Right Turret Lift Rotor") as IMyMotorStator;
    liftToArenaTimer = GridTerminalSystem.GetBlockWithName("Lift To Arena Timer Block") as IMyTimerBlock;
    liftToCraneTimer = GridTerminalSystem.GetBlockWithName("Lift To Crane Timer Block") as IMyTimerBlock;

    // Check for missing blocks
    if (leftPiston == null) Echo("ERROR: Could not find '2nd Innermost Turret Lift Left Piston'");
    if (rightPiston == null) Echo("ERROR: Could not find '2nd Innermost Turret Lift Right Piston'");
    if (innermostPiston == null) Echo("ERROR: Could not find 'Innermost Turret Lift Piston'");
    if (turretTesterBottomPiston == null) Echo("ERROR: Could not find 'Turret Tester Bottom Piston'");
    if (turretTesterPistonMiddle1 == null) Echo("ERROR: Could not find 'Turret Tester Piston Middle 1'");
    if (turretTesterPistonMiddle2 == null) Echo("ERROR: Could not find 'Turret Tester Piston Middle 2'");
    if (turretTesterPistonTop1 == null) Echo("ERROR: Could not find 'Turret Tester Piston Top 1'");
    if (turretTesterPistonTop2 == null) Echo("ERROR: Could not find 'Turret Tester Piston Top 2'");
    if (turretTesterPiston4 == null) Echo("ERROR: Could not find 'Turret Tester Piston 4'");
    if (turretTesterPiston5 == null) Echo("ERROR: Could not find 'Turret Tester Piston 5'");
    if (turretTesterPiston6 == null) Echo("ERROR: Could not find 'Turret Tester Piston 6'");
    if (leftRotor == null) Echo("ERROR: Could not find '2nd Innermost Left Turret Lift Rotor'");
    if (rightRotor == null) Echo("ERROR: Could not find '2nd Innermost Right Turret Lift Rotor'");
    if (outerLeftRotor == null) Echo("ERROR: Could not find 'Outermost Turret Lift Left Rotor'");
    if (outerRightRotor == null) Echo("ERROR: Could not find 'Outermost Turret Lift Right Rotor'");
    if (mainRotor == null) Echo("ERROR: Could not find 'Main Turret Lift Rotor'");
    if (innermostLeftRotor == null) Echo("ERROR: Could not find 'Innermost Left Turret Lift Rotor'");
    if (innermostRightRotor == null) Echo("ERROR: Could not find 'Innermost Right Turret Lift Rotor'");
    if (liftToArenaTimer == null) Echo("ERROR: Could not find 'Lift To Arena Timer Block'");
    if (liftToCraneTimer == null) Echo("ERROR: Could not find 'Lift To Crane Timer Block'");
    
    // Record original settings to custom data - NEVER change the actual values
    RecordOriginalSettings();
}

void StartExtendSequence()
{
    if (isExtending)
    {
        Echo("Extend sequence already in progress");
        return;
    }
    
    if (isRetracting)
    {
        Echo("Cannot extend - retract sequence in progress");
        return;
    }

    Echo("Starting extend sequence...");
    Echo("Step 0: Retracting innermost piston to 0...");
    
    // First retract innermost piston to 0
    if (innermostPiston != null)
    {
        innermostPiston.MinLimit = 0f;
        innermostPiston.Retract();
    }
    
    isExtending = true;
    extendStep = 0; // Start at step 0 for innermost retraction
    waitingForInnermostRetract = true;
    tickCounter = 0;
}

void StartRetractSequence()
{
    if (isRetracting)
    {
        Echo("Retract sequence already in progress");
        return;
    }
    
    if (isExtending)
    {
        Echo("Cannot retract - extend sequence in progress");
        return;
    }

    Echo("Starting retract sequence...");
    Echo("Step 0: Retracting innermost piston to 0...");
    
    // First retract innermost piston to 0
    if (innermostPiston != null)
    {
        innermostPiston.MinLimit = 0f;
        innermostPiston.Retract();
    }
    
    isRetracting = true;
    retractStep = 0; // Start at step 0 for innermost retraction
    waitingForInnermostRetract = true;
    tickCounter = 0;
}

void ProcessExtendSequence()
{
    tickCounter++;

    switch (extendStep)
    {
        case 0: // Wait for innermost piston to retract to 0
            if (waitingForInnermostRetract)
            {
                if (innermostPiston != null && innermostPiston.CurrentPosition <= 0.1f)
                {
                    Echo("Innermost piston retracted. Starting main sequence...");
                    waitingForInnermostRetract = false;
                    extendStep = 1;
                    tickCounter = 0;
                    ReversePistons(); // Step 1: Reverse both pistons
                }
            }
            break;

        case 1: // Wait after first piston reverse
            if (tickCounter >= (WAIT_AFTER_PISTONS_1 * TICKS_PER_SECOND))
            {
                Echo("Step 2: Reversing inner rotors...");
                ReverseInnerRotors();
                extendStep = 2;
                tickCounter = 0;
            }
            break;

        case 2: // Wait after inner rotor reverse
            if (tickCounter >= (WAIT_AFTER_INNER_ROTORS * TICKS_PER_SECOND))
            {
                Echo("Step 3: Reversing outer rotors...");
                ReverseOuterRotors();
                extendStep = 3;
                tickCounter = 0;
            }
            break;

        case 3: // Wait before second piston reversal
            if (tickCounter >= (WAIT_BEFORE_FINAL_PISTONS * TICKS_PER_SECOND))
            {
                Echo("Step 4: Reversing pistons with limit...");
                ReversePistonsWithLimit(); // Special reversal with 7.0m limit
                extendStep = 4;
                tickCounter = 0;
            }
            break;

        case 4: // Wait for sequence completion
            if (tickCounter >= (WAIT_AFTER_OUTER_ROTORS * TICKS_PER_SECOND))
            {
                Echo("Main sequence complete. Extending innermost piston to 9...");
                if (innermostPiston != null)
                {
                    innermostPiston.MaxLimit = 9.2f;
                    innermostPiston.Extend();
                }
                extendStep = 5;
                waitingForInnermostExtend = true;
                tickCounter = 0;
            }
            break;

        case 5: // Wait for innermost piston to extend to 9
            if (waitingForInnermostExtend)
            {
                if (innermostPiston != null && innermostPiston.CurrentPosition >= 8.9f)
                {
                    Echo("Innermost piston fully extended. Starting Lift To Crane Timer...");
                    // Start lift to crane timer countdown
                    StartLiftToCraneTimer();
                    Echo("Extend sequence complete!");
                    isExtending = false;
                    extendStep = 0;
                    waitingForInnermostExtend = false;
                    tickCounter = 0;
                }
            }
            break;
    }

    // Display current status
    Echo($"Extend Step: {extendStep}");
    Echo($"Tick Counter: {tickCounter}");
    if (innermostPiston != null)
    {
        Echo($"Innermost Piston Position: {innermostPiston.CurrentPosition:F2}m");
    }
}

void ProcessRetractSequence()
{
    tickCounter++;

    switch (retractStep)
    {
        case 0: // Wait for innermost piston to retract to 0
            if (waitingForInnermostRetract)
            {
                if (innermostPiston != null && innermostPiston.CurrentPosition <= 0.1f)
                {
                    Echo("Innermost piston retracted. Starting main sequence...");
                    waitingForInnermostRetract = false;
                    retractStep = 1;
                    tickCounter = 0;
                    RestorePistonLimitsAndReverse(); // Step 1: Restore piston limits and reverse pistons
                }
            }
            break;

        case 1: // Mirror extend step 4 timing
            if (tickCounter >= (WAIT_AFTER_OUTER_ROTORS * TICKS_PER_SECOND))
            {
                Echo("Step 2: Reversing outer rotors...");
                ReverseOuterRotors();
                retractStep = 2;
                tickCounter = 0;
            }
            break;

        case 2: // Mirror extend step 3 timing
            if (tickCounter >= (WAIT_BEFORE_FINAL_PISTONS * TICKS_PER_SECOND))
            {
                Echo("Step 3: Reversing inner rotors...");
                ReverseInnerRotors();
                retractStep = 3;
                tickCounter = 0;
            }
            break;

        case 3: // Mirror extend step 2 timing
            if (tickCounter >= (WAIT_AFTER_INNER_ROTORS * TICKS_PER_SECOND))
            {
                Echo("Step 4: Final piston reversal...");
                ReversePistons();
                retractStep = 4;
                tickCounter = 0;
            }
            break;

        case 4: // Mirror extend step 1 timing
            if (tickCounter >= (WAIT_AFTER_PISTONS_1 * TICKS_PER_SECOND))
            {
                Echo("Main sequence complete. Extending innermost piston to 4.43...");
                if (innermostPiston != null)
                {
                    innermostPiston.MaxLimit = 4.43f;
                    innermostPiston.Extend();
                }
                // Simultaneously reverse innermost rotors
                ReverseInnermostRotors();
                // Reverse turret tester pistons
                ReverseTurretTesterPistons();
                // Start lift to arena timer
                StartLiftToArenaTimer();
                retractStep = 5;
                waitingForInnermostExtend = true;
                tickCounter = 0;
            }
            break;

        case 5: // Wait for innermost piston to extend to 4.43
            if (waitingForInnermostExtend)
            {
                if (innermostPiston != null && innermostPiston.CurrentPosition >= 4.33f)
                {
                    Echo("Retract sequence complete!");
                    isRetracting = false;
                    retractStep = 0;
                    waitingForInnermostExtend = false;
                    tickCounter = 0;
                }
            }
            break;
    }

    // Display current status
    Echo($"Retract Step: {retractStep}");
    Echo($"Tick Counter: {tickCounter}");
    if (innermostPiston != null)
    {
        Echo($"Innermost Piston Position: {innermostPiston.CurrentPosition:F2}m");
    }
}

void ReversePistons()
{
    if (leftPiston != null)
    {
        leftPiston.Reverse();
        Echo($"Reversed left piston (now moving at {leftPiston.Velocity} m/s)");
    }
    if (rightPiston != null)
    {
        rightPiston.Reverse();
        Echo($"Reversed right piston (now moving at {rightPiston.Velocity} m/s)");
    }
}

void ReversePistonsWithLimit()
{
    if (leftPiston != null)
    {
        leftPiston.MaxLimit = 7.0f; // Set max limit for second reversal
        leftPiston.Reverse();
        Echo($"Reversed left piston with 7.0m limit (now moving at {leftPiston.Velocity} m/s)");
    }
    if (rightPiston != null)
    {
        rightPiston.MaxLimit = 7.0f; // Set max limit for second reversal
        rightPiston.Reverse();
        Echo($"Reversed right piston with 7.0m limit (now moving at {rightPiston.Velocity} m/s)");
    }
}

void RestorePistonLimitsAndReverse()
{
    if (leftPiston != null)
    {
        // Restore original max limit from custom data
        string[] lines = leftPiston.CustomData.Split('\n');
        foreach (string line in lines)
        {
            if (line.StartsWith("OriginalMaxLimit="))
            {
                float originalMax = float.Parse(line.Split('=')[1]);
                leftPiston.MaxLimit = originalMax;
                break;
            }
        }
        leftPiston.Reverse();
        Echo($"Restored left piston limit to {leftPiston.MaxLimit}m and reversed (now moving at {leftPiston.Velocity} m/s)");
    }
    if (rightPiston != null)
    {
        // Restore original max limit from custom data
        string[] lines = rightPiston.CustomData.Split('\n');
        foreach (string line in lines)
        {
            if (line.StartsWith("OriginalMaxLimit="))
            {
                float originalMax = float.Parse(line.Split('=')[1]);
                rightPiston.MaxLimit = originalMax;
                break;
            }
        }
        rightPiston.Reverse();
        Echo($"Restored right piston limit to {rightPiston.MaxLimit}m and reversed (now moving at {rightPiston.Velocity} m/s)");
    }
}

void ReverseInnerRotors()
{
    if (leftRotor != null)
    {
        // Only flip the sign - preserve your exact settings
        leftRotor.TargetVelocityRPM = -leftRotor.TargetVelocityRPM;
        Echo($"Reversed left inner rotor: {leftRotor.TargetVelocityRPM} RPM");
    }
    if (rightRotor != null)
    {
        // Only flip the sign - preserve your exact settings
        rightRotor.TargetVelocityRPM = -rightRotor.TargetVelocityRPM;
        Echo($"Reversed right inner rotor: {rightRotor.TargetVelocityRPM} RPM");
    }
    if (mainRotor != null)
    {
        // Only flip the sign - preserve your exact settings
        mainRotor.TargetVelocityRPM = -mainRotor.TargetVelocityRPM;
        Echo($"Reversed main rotor: {mainRotor.TargetVelocityRPM} RPM");
    }
}

void ReverseOuterRotors()
{
    if (outerLeftRotor != null)
    {
        // Only flip the sign - preserve your exact settings
        outerLeftRotor.TargetVelocityRPM = -outerLeftRotor.TargetVelocityRPM;
        Echo($"Reversed left outer rotor: {outerLeftRotor.TargetVelocityRPM} RPM");
    }
    if (outerRightRotor != null)
    {
        // Only flip the sign - preserve your exact settings
        outerRightRotor.TargetVelocityRPM = -outerRightRotor.TargetVelocityRPM;
        Echo($"Reversed right outer rotor: {outerRightRotor.TargetVelocityRPM} RPM");
    }
}

void ReverseInnermostRotors()
{
    if (innermostLeftRotor != null)
    {
        // Only flip the sign - preserve your exact settings
        innermostLeftRotor.TargetVelocityRPM = -innermostLeftRotor.TargetVelocityRPM;
        Echo($"Reversed innermost left rotor: {innermostLeftRotor.TargetVelocityRPM} RPM");
    }
    if (innermostRightRotor != null)
    {
        // Only flip the sign - preserve your exact settings
        innermostRightRotor.TargetVelocityRPM = -innermostRightRotor.TargetVelocityRPM;
        Echo($"Reversed innermost right rotor: {innermostRightRotor.TargetVelocityRPM} RPM");
    }
}

void ReverseTurretTesterPistons()
{
    if (turretTesterBottomPiston != null)
    {
        turretTesterBottomPiston.Reverse();
        Echo($"Reversed turret tester bottom piston (now moving at {turretTesterBottomPiston.Velocity} m/s)");
    }
    if (turretTesterPistonMiddle1 != null)
    {
        turretTesterPistonMiddle1.Reverse();
        Echo($"Reversed turret tester piston middle 1 (now moving at {turretTesterPistonMiddle1.Velocity} m/s)");
    }
    if (turretTesterPistonMiddle2 != null)
    {
        turretTesterPistonMiddle2.Reverse();
        Echo($"Reversed turret tester piston middle 2 (now moving at {turretTesterPistonMiddle2.Velocity} m/s)");
    }
    if (turretTesterPistonTop1 != null)
    {
        turretTesterPistonTop1.Reverse();
        Echo($"Reversed turret tester piston top 1 (now moving at {turretTesterPistonTop1.Velocity} m/s)");
    }
    if (turretTesterPistonTop2 != null)
    {
        turretTesterPistonTop2.Reverse();
        Echo($"Reversed turret tester piston top 2 (now moving at {turretTesterPistonTop2.Velocity} m/s)");
    }
    if (turretTesterPiston4 != null)
    {
        turretTesterPiston4.Reverse();
        Echo($"Reversed turret tester piston 4 (now moving at {turretTesterPiston4.Velocity} m/s)");
    }
    if (turretTesterPiston5 != null)
    {
        turretTesterPiston5.Reverse();
        Echo($"Reversed turret tester piston 5 (now moving at {turretTesterPiston5.Velocity} m/s)");
    }
    if (turretTesterPiston6 != null)
    {
        turretTesterPiston6.Reverse();
        Echo($"Reversed turret tester piston 6 (now moving at {turretTesterPiston6.Velocity} m/s)");
    }
}

void StartLiftToArenaTimer()
{
    if (liftToArenaTimer != null)
    {
        liftToArenaTimer.StartCountdown();
        Echo("Started Lift To Arena Timer Block countdown");
    }
}

void StartLiftToCraneTimer()
{
    if (liftToCraneTimer != null)
    {
        liftToCraneTimer.StartCountdown();
        Echo("Started Lift To Crane Timer Block countdown");
    }
}

void RecordOriginalSettings()
{
    // Record piston settings to custom data
    if (leftPiston != null)
    {
        string pistonData = $"OriginalVelocity={leftPiston.Velocity}\n";
        pistonData += $"OriginalMinLimit={leftPiston.MinLimit}\n";
        pistonData += $"OriginalMaxLimit={leftPiston.MaxLimit}\n";
        pistonData += $"OriginalCurrentPosition={leftPiston.CurrentPosition}\n";
        pistonData += $"OriginalEnabled={leftPiston.Enabled}\n";
        leftPiston.CustomData = pistonData;
        Echo($"Recorded left piston settings: Velocity={leftPiston.Velocity}");
    }
    
    if (rightPiston != null)
    {
        string pistonData = $"OriginalVelocity={rightPiston.Velocity}\n";
        pistonData += $"OriginalMinLimit={rightPiston.MinLimit}\n";
        pistonData += $"OriginalMaxLimit={rightPiston.MaxLimit}\n";
        pistonData += $"OriginalCurrentPosition={rightPiston.CurrentPosition}\n";
        pistonData += $"OriginalEnabled={rightPiston.Enabled}\n";
        rightPiston.CustomData = pistonData;
        Echo($"Recorded right piston settings: Velocity={rightPiston.Velocity}");
    }
    
    // Record turret tester piston settings to custom data
    if (turretTesterBottomPiston != null)
    {
        string pistonData = $"OriginalVelocity={turretTesterBottomPiston.Velocity}\n";
        pistonData += $"OriginalMinLimit={turretTesterBottomPiston.MinLimit}\n";
        pistonData += $"OriginalMaxLimit={turretTesterBottomPiston.MaxLimit}\n";
        pistonData += $"OriginalCurrentPosition={turretTesterBottomPiston.CurrentPosition}\n";
        pistonData += $"OriginalEnabled={turretTesterBottomPiston.Enabled}\n";
        turretTesterBottomPiston.CustomData = pistonData;
        Echo($"Recorded turret tester bottom piston settings: Velocity={turretTesterBottomPiston.Velocity}");
    }
    
    if (turretTesterPistonMiddle1 != null)
    {
        string pistonData = $"OriginalVelocity={turretTesterPistonMiddle1.Velocity}\n";
        pistonData += $"OriginalMinLimit={turretTesterPistonMiddle1.MinLimit}\n";
        pistonData += $"OriginalMaxLimit={turretTesterPistonMiddle1.MaxLimit}\n";
        pistonData += $"OriginalCurrentPosition={turretTesterPistonMiddle1.CurrentPosition}\n";
        pistonData += $"OriginalEnabled={turretTesterPistonMiddle1.Enabled}\n";
        turretTesterPistonMiddle1.CustomData = pistonData;
        Echo($"Recorded turret tester piston middle 1 settings: Velocity={turretTesterPistonMiddle1.Velocity}");
    }
    
    if (turretTesterPistonMiddle2 != null)
    {
        string pistonData = $"OriginalVelocity={turretTesterPistonMiddle2.Velocity}\n";
        pistonData += $"OriginalMinLimit={turretTesterPistonMiddle2.MinLimit}\n";
        pistonData += $"OriginalMaxLimit={turretTesterPistonMiddle2.MaxLimit}\n";
        pistonData += $"OriginalCurrentPosition={turretTesterPistonMiddle2.CurrentPosition}\n";
        pistonData += $"OriginalEnabled={turretTesterPistonMiddle2.Enabled}\n";
        turretTesterPistonMiddle2.CustomData = pistonData;
        Echo($"Recorded turret tester piston middle 2 settings: Velocity={turretTesterPistonMiddle2.Velocity}");
    }
    
    if (turretTesterPistonTop1 != null)
    {
        string pistonData = $"OriginalVelocity={turretTesterPistonTop1.Velocity}\n";
        pistonData += $"OriginalMinLimit={turretTesterPistonTop1.MinLimit}\n";
        pistonData += $"OriginalMaxLimit={turretTesterPistonTop1.MaxLimit}\n";
        pistonData += $"OriginalCurrentPosition={turretTesterPistonTop1.CurrentPosition}\n";
        pistonData += $"OriginalEnabled={turretTesterPistonTop1.Enabled}\n";
        turretTesterPistonTop1.CustomData = pistonData;
        Echo($"Recorded turret tester piston top 1 settings: Velocity={turretTesterPistonTop1.Velocity}");
    }
    
    if (turretTesterPistonTop2 != null)
    {
        string pistonData = $"OriginalVelocity={turretTesterPistonTop2.Velocity}\n";
        pistonData += $"OriginalMinLimit={turretTesterPistonTop2.MinLimit}\n";
        pistonData += $"OriginalMaxLimit={turretTesterPistonTop2.MaxLimit}\n";
        pistonData += $"OriginalCurrentPosition={turretTesterPistonTop2.CurrentPosition}\n";
        pistonData += $"OriginalEnabled={turretTesterPistonTop2.Enabled}\n";
        turretTesterPistonTop2.CustomData = pistonData;
        Echo($"Recorded turret tester piston top 2 settings: Velocity={turretTesterPistonTop2.Velocity}");
    }
    if (turretTesterPiston4 != null)
    {
        string pistonData = $"OriginalVelocity={turretTesterPiston4.Velocity}\n";
        pistonData += $"OriginalMinLimit={turretTesterPiston4.MinLimit}\n";
        pistonData += $"OriginalMaxLimit={turretTesterPiston4.MaxLimit}\n";
        pistonData += $"OriginalCurrentPosition={turretTesterPiston4.CurrentPosition}\n";
        pistonData += $"OriginalEnabled={turretTesterPiston4.Enabled}\n";
        turretTesterPiston4.CustomData = pistonData;
        Echo($"Recorded turret tester piston 4 settings: Velocity={turretTesterPiston4.Velocity}");
    }
    if (turretTesterPiston5 != null)
    {
        string pistonData = $"OriginalVelocity={turretTesterPiston5.Velocity}\n";
        pistonData += $"OriginalMinLimit={turretTesterPiston5.MinLimit}\n";
        pistonData += $"OriginalMaxLimit={turretTesterPiston5.MaxLimit}\n";
        pistonData += $"OriginalCurrentPosition={turretTesterPiston5.CurrentPosition}\n";
        pistonData += $"OriginalEnabled={turretTesterPiston5.Enabled}\n";
        turretTesterPiston5.CustomData = pistonData;
        Echo($"Recorded turret tester piston 5 settings: Velocity={turretTesterPiston5.Velocity}");
    }
    if (turretTesterPiston6 != null)
    {
        string pistonData = $"OriginalVelocity={turretTesterPiston6.Velocity}\n";
        pistonData += $"OriginalMinLimit={turretTesterPiston6.MinLimit}\n";
        pistonData += $"OriginalMaxLimit={turretTesterPiston6.MaxLimit}\n";
        pistonData += $"OriginalCurrentPosition={turretTesterPiston6.CurrentPosition}\n";
        pistonData += $"OriginalEnabled={turretTesterPiston6.Enabled}\n";
        turretTesterPiston6.CustomData = pistonData;
        Echo($"Recorded turret tester piston 6 settings: Velocity={turretTesterPiston6.Velocity}");
    }
    
    // Record inner rotor settings to custom data
    if (leftRotor != null)
    {
        string rotorData = $"OriginalTargetVelocityRPM={leftRotor.TargetVelocityRPM}\n";
        rotorData += $"OriginalLowerLimitRad={leftRotor.LowerLimitRad}\n";
        rotorData += $"OriginalUpperLimitRad={leftRotor.UpperLimitRad}\n";
        rotorData += $"OriginalTorque={leftRotor.Torque}\n";
        rotorData += $"OriginalBrakingTorque={leftRotor.BrakingTorque}\n";
        rotorData += $"OriginalEnabled={leftRotor.Enabled}\n";
        leftRotor.CustomData = rotorData;
        Echo($"Recorded left inner rotor settings: RPM={leftRotor.TargetVelocityRPM}");
    }
    
    if (rightRotor != null)
    {
        string rotorData = $"OriginalTargetVelocityRPM={rightRotor.TargetVelocityRPM}\n";
        rotorData += $"OriginalLowerLimitRad={rightRotor.LowerLimitRad}\n";
        rotorData += $"OriginalUpperLimitRad={rightRotor.UpperLimitRad}\n";
        rotorData += $"OriginalTorque={rightRotor.Torque}\n";
        rotorData += $"OriginalBrakingTorque={rightRotor.BrakingTorque}\n";
        rotorData += $"OriginalEnabled={rightRotor.Enabled}\n";
        rightRotor.CustomData = rotorData;
        Echo($"Recorded right inner rotor settings: RPM={rightRotor.TargetVelocityRPM}");
    }
    
    // Record outer rotor settings to custom data
    if (outerLeftRotor != null)
    {
        string rotorData = $"OriginalTargetVelocityRPM={outerLeftRotor.TargetVelocityRPM}\n";
        rotorData += $"OriginalLowerLimitRad={outerLeftRotor.LowerLimitRad}\n";
        rotorData += $"OriginalUpperLimitRad={outerLeftRotor.UpperLimitRad}\n";
        rotorData += $"OriginalTorque={outerLeftRotor.Torque}\n";
        rotorData += $"OriginalBrakingTorque={outerLeftRotor.BrakingTorque}\n";
        rotorData += $"OriginalEnabled={outerLeftRotor.Enabled}\n";
        outerLeftRotor.CustomData = rotorData;
        Echo($"Recorded left outer rotor settings: RPM={outerLeftRotor.TargetVelocityRPM}");
    }
    
    if (outerRightRotor != null)
    {
        string rotorData = $"OriginalTargetVelocityRPM={outerRightRotor.TargetVelocityRPM}\n";
        rotorData += $"OriginalLowerLimitRad={outerRightRotor.LowerLimitRad}\n";
        rotorData += $"OriginalUpperLimitRad={outerRightRotor.UpperLimitRad}\n";
        rotorData += $"OriginalTorque={outerRightRotor.Torque}\n";
        rotorData += $"OriginalBrakingTorque={outerRightRotor.BrakingTorque}\n";
        rotorData += $"OriginalEnabled={outerRightRotor.Enabled}\n";
        outerRightRotor.CustomData = rotorData;
        Echo($"Recorded right outer rotor settings: RPM={outerRightRotor.TargetVelocityRPM}");
    }
    
    // Record innermost rotor settings to custom data
    if (innermostLeftRotor != null)
    {
        string rotorData = $"OriginalTargetVelocityRPM={innermostLeftRotor.TargetVelocityRPM}\n";
        rotorData += $"OriginalLowerLimitRad={innermostLeftRotor.LowerLimitRad}\n";
        rotorData += $"OriginalUpperLimitRad={innermostLeftRotor.UpperLimitRad}\n";
        rotorData += $"OriginalTorque={innermostLeftRotor.Torque}\n";
        rotorData += $"OriginalBrakingTorque={innermostLeftRotor.BrakingTorque}\n";
        rotorData += $"OriginalEnabled={innermostLeftRotor.Enabled}\n";
        innermostLeftRotor.CustomData = rotorData;
        Echo($"Recorded innermost left rotor settings: RPM={innermostLeftRotor.TargetVelocityRPM}");
    }
    
    if (innermostRightRotor != null)
    {
        string rotorData = $"OriginalTargetVelocityRPM={innermostRightRotor.TargetVelocityRPM}\n";
        rotorData += $"OriginalLowerLimitRad={innermostRightRotor.LowerLimitRad}\n";
        rotorData += $"OriginalUpperLimitRad={innermostRightRotor.UpperLimitRad}\n";
        rotorData += $"OriginalTorque={innermostRightRotor.Torque}\n";
        rotorData += $"OriginalBrakingTorque={innermostRightRotor.BrakingTorque}\n";
        rotorData += $"OriginalEnabled={innermostRightRotor.Enabled}\n";
        innermostRightRotor.CustomData = rotorData;
        Echo($"Recorded innermost right rotor settings: RPM={innermostRightRotor.TargetVelocityRPM}");
    }
    
    Echo("All original settings recorded to block custom data. Values preserved exactly as you set them.");
    
    // Record main rotor settings
    RecordMainRotorSettings();
}

void RecordMainRotorSettings()
{
    // Record main rotor settings to custom data
    if (mainRotor != null)
    {
        string rotorData = $"OriginalTargetVelocityRPM={mainRotor.TargetVelocityRPM}\n";
        rotorData += $"OriginalLowerLimitRad={mainRotor.LowerLimitRad}\n";
        rotorData += $"OriginalUpperLimitRad={mainRotor.UpperLimitRad}\n";
        rotorData += $"OriginalTorque={mainRotor.Torque}\n";
        rotorData += $"OriginalBrakingTorque={mainRotor.BrakingTorque}\n";
        rotorData += $"OriginalEnabled={mainRotor.Enabled}\n";
        mainRotor.CustomData = rotorData;
        Echo($"Recorded main rotor settings: RPM={mainRotor.TargetVelocityRPM}");
    }
}

void EmergencyStop()
{
    Echo("EMERGENCY STOP - Halting all movement!");
    isExtending = false;
    isRetracting = false;
    extendStep = 0;
    retractStep = 0;
    tickCounter = 0;
    waitingForInnermostRetract = false;
    waitingForInnermostExtend = false;
    
    // Stop all pistons
    if (leftPiston != null) leftPiston.Velocity = 0;
    if (rightPiston != null) rightPiston.Velocity = 0;
    if (innermostPiston != null) innermostPiston.Velocity = 0;
    if (turretTesterBottomPiston != null) turretTesterBottomPiston.Velocity = 0;
    if (turretTesterPistonMiddle1 != null) turretTesterPistonMiddle1.Velocity = 0;
    if (turretTesterPistonMiddle2 != null) turretTesterPistonMiddle2.Velocity = 0;
    if (turretTesterPistonTop1 != null) turretTesterPistonTop1.Velocity = 0;
    if (turretTesterPistonTop2 != null) turretTesterPistonTop2.Velocity = 0;
    if (turretTesterPiston4 != null) turretTesterPiston4.Velocity = 0;
    if (turretTesterPiston5 != null) turretTesterPiston5.Velocity = 0;
    if (turretTesterPiston6 != null) turretTesterPiston6.Velocity = 0;
    
    // Stop all rotors
    if (leftRotor != null) leftRotor.TargetVelocityRPM = 0;
    if (rightRotor != null) rightRotor.TargetVelocityRPM = 0;
    if (outerLeftRotor != null) outerLeftRotor.TargetVelocityRPM = 0;
    if (outerRightRotor != null) outerRightRotor.TargetVelocityRPM = 0;
    if (mainRotor != null) mainRotor.TargetVelocityRPM = 0;
    if (innermostLeftRotor != null) innermostLeftRotor.TargetVelocityRPM = 0;
    if (innermostRightRotor != null) innermostRightRotor.TargetVelocityRPM = 0;
    
    // Stop timer
    if (liftToArenaTimer != null) liftToArenaTimer.StopCountdown();
    if (liftToCraneTimer != null) liftToCraneTimer.StopCountdown();
    
    Echo("All blocks stopped. Both extend and retract sequences aborted.");
}
