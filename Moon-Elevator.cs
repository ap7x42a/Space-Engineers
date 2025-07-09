// === ELEVATOR CONTROL SCRIPT ===
// Inchworm-style elevator with 3 pistons and 3 rotors
// Controls: "UP", "DOWN", "STOP" via button panels or arguments
// Safety: Always maintains at least one rotor attachment

// === CONFIGURATION ===
const float PISTON_SPEED = 0.3f;        // Speed for smooth movement
const float ROTOR_ATTACH_DELAY = 1.0f;  // Seconds to wait for rotor attachment
const float MOVEMENT_DELAY = 0.5f;      // Delay between movement steps

// Block names - update these to match your exact block names
const string LEFT_PISTON = "Elevator Piston Left";
const string CENTER_PISTON = "Elevator Piston Center";
const string RIGHT_PISTON = "Elevator Piston Right";
const string LEFT_ROTOR = "Elevator Rotor Left";
const string CENTER_ROTOR = "Elevator Rotor Center";
const string RIGHT_ROTOR = "Elevator Rotor Right";

// === STATE VARIABLES ===
enum ElevatorState
{
    Idle,
    MovingUp,
    MovingDown,
    Stopping,
    Error
}

enum MovementPhase
{
    None,
    AttachingRotors,
    DetachingRotors,
    ExtendingPistons,
    RetractingPistons,
    WaitingForCompletion
}

ElevatorState currentState = ElevatorState.Idle;
MovementPhase currentPhase = MovementPhase.None;
bool isGoingUp = false;
float phaseTimer = 0f;
int movementCycle = 0; // 0 = center extended, sides retracted; 1 = sides extended, center retracted

// Block references
IMyPistonBase leftPiston, centerPiston, rightPiston;
IMyMotorStator leftRotor, centerRotor, rightRotor;

// === INITIALIZATION ===
public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10; // Run every 10 ticks
    InitializeBlocks();
}

void InitializeBlocks()
{
    // Find pistons
    leftPiston = GridTerminalSystem.GetBlockWithName(LEFT_PISTON) as IMyPistonBase;
    centerPiston = GridTerminalSystem.GetBlockWithName(CENTER_PISTON) as IMyPistonBase;
    rightPiston = GridTerminalSystem.GetBlockWithName(RIGHT_PISTON) as IMyPistonBase;
    
    // Find rotors
    leftRotor = GridTerminalSystem.GetBlockWithName(LEFT_ROTOR) as IMyMotorStator;
    centerRotor = GridTerminalSystem.GetBlockWithName(CENTER_ROTOR) as IMyMotorStator;
    rightRotor = GridTerminalSystem.GetBlockWithName(RIGHT_ROTOR) as IMyMotorStator;
    
    // Validate all blocks found
    if (leftPiston == null || centerPiston == null || rightPiston == null ||
        leftRotor == null || centerRotor == null || rightRotor == null)
    {
        currentState = ElevatorState.Error;
        Echo("ERROR: One or more elevator blocks not found!");
        return;
    }
    
    // Set initial piston speeds
    leftPiston.Velocity = PISTON_SPEED;
    centerPiston.Velocity = PISTON_SPEED;
    rightPiston.Velocity = PISTON_SPEED;
    
    Echo("Elevator initialized successfully");
}

// === MAIN EXECUTION ===
public void Main(string argument, UpdateType updateSource)
{
    if (currentState == ElevatorState.Error)
    {
        Echo("ELEVATOR ERROR - Check block names and try recompiling");
        return;
    }
    
    // Handle commands
    if (!string.IsNullOrEmpty(argument))
    {
        HandleCommand(argument.ToUpper());
    }
    
    // Update elevator state
    UpdateElevator();
    
    // Display status
    DisplayStatus();
}

void HandleCommand(string command)
{
    switch (command)
    {
        case "UP":
            if (currentState == ElevatorState.Idle)
            {
                StartMovement(true);
            }
            break;
            
        case "DOWN":
            if (currentState == ElevatorState.Idle)
            {
                StartMovement(false);
            }
            break;
            
        case "STOP":
            if (currentState == ElevatorState.MovingUp || currentState == ElevatorState.MovingDown)
            {
                StopMovement();
            }
            break;
            
        default:
            Echo($"Unknown command: {command}");
            break;
    }
}

void StartMovement(bool goingUp)
{
    isGoingUp = goingUp;
    currentState = goingUp ? ElevatorState.MovingUp : ElevatorState.MovingDown;
    currentPhase = MovementPhase.AttachingRotors;
    phaseTimer = 0f;
    
    Echo($"Starting movement: {(goingUp ? "UP" : "DOWN")}");
}

void StopMovement()
{
    currentState = ElevatorState.Stopping;
    currentPhase = MovementPhase.None;
    
    // Stop all pistons immediately
    leftPiston.Velocity = 0f;
    centerPiston.Velocity = 0f;
    rightPiston.Velocity = 0f;
    
    Echo("Stopping elevator");
}

void UpdateElevator()
{
    phaseTimer += 1f/6f; // Approximate time increment (Update10 = ~1/6 second)
    
    switch (currentState)
    {
        case ElevatorState.MovingUp:
        case ElevatorState.MovingDown:
            UpdateMovement();
            break;
            
        case ElevatorState.Stopping:
            currentState = ElevatorState.Idle;
            currentPhase = MovementPhase.None;
            break;
    }
}

void UpdateMovement()
{
    switch (currentPhase)
    {
        case MovementPhase.AttachingRotors:
            AttachRotorsPhase();
            break;
            
        case MovementPhase.DetachingRotors:
            DetachRotorsPhase();
            break;
            
        case MovementPhase.ExtendingPistons:
        case MovementPhase.RetractingPistons:
            PistonMovementPhase();
            break;
            
        case MovementPhase.WaitingForCompletion:
            WaitingPhase();
            break;
    }
}

void AttachRotorsPhase()
{
    if (phaseTimer < ROTOR_ATTACH_DELAY)
        return;
        
    // Determine which rotors to attach based on current cycle
    if (movementCycle == 0) // Center extended, sides retracted - attach sides
    {
        if (!leftRotor.IsAttached || !rightRotor.IsAttached)
        {
            // Try to attach side rotors
            if (!leftRotor.IsAttached)
                leftRotor.Attach();
            if (!rightRotor.IsAttached)
                rightRotor.Attach();
                
            // Wait a bit more for attachment
            if (phaseTimer < ROTOR_ATTACH_DELAY * 2)
                return;
        }
        
        // If we can't attach side rotors, stay safe
        if (!leftRotor.IsAttached || !rightRotor.IsAttached)
        {
            Echo("WARNING: Cannot attach side rotors - staying in safe position");
            currentState = ElevatorState.Idle;
            return;
        }
    }
    else // Sides extended, center retracted - attach center
    {
        if (!centerRotor.IsAttached)
        {
            centerRotor.Attach();
            
            // Wait for attachment
            if (phaseTimer < ROTOR_ATTACH_DELAY * 2)
                return;
        }
        
        // If we can't attach center rotor, stay safe
        if (!centerRotor.IsAttached)
        {
            Echo("WARNING: Cannot attach center rotor - staying in safe position");
            currentState = ElevatorState.Idle;
            return;
        }
    }
    
    // Move to detaching phase
    currentPhase = MovementPhase.DetachingRotors;
    phaseTimer = 0f;
}

void DetachRotorsPhase()
{
    // Small delay before detaching
    if (phaseTimer < 0.5f)
        return;
        
    // Detach the rotors that were previously attached
    if (movementCycle == 0) // Detach center, keep sides attached
    {
        if (centerRotor.IsAttached)
            centerRotor.Detach();
    }
    else // Detach sides, keep center attached
    {
        if (leftRotor.IsAttached)
            leftRotor.Detach();
        if (rightRotor.IsAttached)
            rightRotor.Detach();
    }
    
    // Move to piston movement
    currentPhase = movementCycle == 0 ? MovementPhase.RetractingPistons : MovementPhase.ExtendingPistons;
    phaseTimer = 0f;
    StartPistonMovement();
}

void StartPistonMovement()
{
    float speed = isGoingUp ? PISTON_SPEED : -PISTON_SPEED;
    
    if (movementCycle == 0) // Retract center, extend sides
    {
        centerPiston.Velocity = -speed; // Opposite direction
        leftPiston.Velocity = speed;
        rightPiston.Velocity = speed;
    }
    else // Extend center, retract sides
    {
        centerPiston.Velocity = speed;
        leftPiston.Velocity = -speed; // Opposite direction
        rightPiston.Velocity = -speed; // Opposite direction
    }
}

void PistonMovementPhase()
{
    // Check if any piston has reached its limit
    bool centerAtLimit = (centerPiston.CurrentPosition <= centerPiston.MinLimit + 0.1f) || 
                        (centerPiston.CurrentPosition >= centerPiston.MaxLimit - 0.1f);
    bool leftAtLimit = (leftPiston.CurrentPosition <= leftPiston.MinLimit + 0.1f) || 
                      (leftPiston.CurrentPosition >= leftPiston.MaxLimit - 0.1f);
    bool rightAtLimit = (rightPiston.CurrentPosition <= rightPiston.MinLimit + 0.1f) || 
                       (rightPiston.CurrentPosition >= rightPiston.MaxLimit - 0.1f);
    
    // If any piston reached limit or minimum movement time passed
    if (centerAtLimit || leftAtLimit || rightAtLimit || phaseTimer > 10.0f)
    {
        // Stop all pistons
        centerPiston.Velocity = 0f;
        leftPiston.Velocity = 0f;
        rightPiston.Velocity = 0f;
        
        currentPhase = MovementPhase.WaitingForCompletion;
        phaseTimer = 0f;
    }
}

void WaitingPhase()
{
    // Wait for pistons to fully stop
    if (phaseTimer > MOVEMENT_DELAY)
    {
        // Toggle movement cycle
        movementCycle = (movementCycle == 0) ? 1 : 0;
        
        // Continue movement or stop
        currentPhase = MovementPhase.AttachingRotors;
        phaseTimer = 0f;
        
        // For now, complete one full cycle then stop
        // Later you can add floor detection logic here
        if (movementCycle == 0) // Completed one full inchworm cycle
        {
            currentState = ElevatorState.Idle;
            currentPhase = MovementPhase.None;
            Echo("Movement cycle completed");
        }
    }
}

void DisplayStatus()
{
    Echo("=== ELEVATOR STATUS ===");
    Echo($"State: {currentState}");
    Echo($"Phase: {currentPhase}");
    Echo($"Cycle: {movementCycle}");
    Echo($"Direction: {(isGoingUp ? "UP" : "DOWN")}");
    Echo("");
    
    Echo("=== ROTOR STATUS ===");
    Echo($"Left: {(leftRotor?.IsAttached == true ? "ATTACHED" : "DETACHED")}");
    Echo($"Center: {(centerRotor?.IsAttached == true ? "ATTACHED" : "DETACHED")}");
    Echo($"Right: {(rightRotor?.IsAttached == true ? "ATTACHED" : "DETACHED")}");
    Echo("");
    
    Echo("=== PISTON POSITIONS ===");
    Echo($"Left: {leftPiston?.CurrentPosition:F1}m");
    Echo($"Center: {centerPiston?.CurrentPosition:F1}m");
    Echo($"Right: {rightPiston?.CurrentPosition:F1}m");
    Echo("");
    
    Echo("Commands: UP, DOWN, STOP");
}
