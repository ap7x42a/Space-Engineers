# Copilot Instructions for Space Engineers Scripts

## Overview

This repository contains programmable block scripts for Space Engineers, written in C# for in-game automation. Each `.cs` file is a self-contained script, typically targeting a specific ship system (e.g., airlocks, elevators, lighting, batteries).

## Architecture & Patterns

- **Single-file, Self-contained Scripts:** Each `.cs` file is a standalone script intended for direct use in a programmable block. There is no cross-file dependency or shared library.
- **Naming Conventions:** Block names in scripts (e.g., `"Capacitor Bank Battery 1"`, `"Elevator Piston Left"`) must match in-game block names exactly. Scripts often use arrays or constants to manage multiple blocks.
- **Stateful Logic:** Scripts use private fields to track state across runs (e.g., `lightsHaveBeenTurnedOn`, `isMoving`).
- **Main Entry Points:** Most scripts implement a `Main(string argument)` or `Main(string argument, UpdateType updateSource)` function, which is the programmable block entry.
- **Initialization:** The `Program()` constructor is used for setup, including setting `Runtime.UpdateFrequency` and initializing block references.
- **Patterns & Sequences:** Some scripts (e.g., `pistonscript.cs`) implement complex movement patterns using arrays and dictionaries to control groups of pistons or rotors.

## Developer Workflows

- **Editing:** Edit scripts directly in the `.cs` files. Each file is uploaded individually to a programmable block in-game.
- **Testing:** There is no automated test framework; testing is done in-game by uploading scripts and observing block behavior.
- **Debugging:** Use `Echo()` for in-game debug output. Many scripts write status to the programmable block's LCD or to in-game LCD panels.
- **Block Discovery:** Scripts rely on `GridTerminalSystem.GetBlockWithName`. If a block is not found, scripts typically log an error with `Echo()`.

## Project-specific Conventions

- **Block Grouping:** Scripts often use arrays of block names for batch operations (e.g., managing multiple LCDs or batteries).
- **Pattern Execution:** Movement scripts (e.g., elevators) use named patterns (e.g., "Sequential", "Wave") to control the order and timing of piston/rotor actions.
- **No External Dependencies:** All logic is vanilla C# compatible with the Space Engineers programmable block API. No external libraries or packages are used.
- **Script-specific Settings:** Adjustable parameters (e.g., font sizes, movement speeds) are defined as top-level variables for easy tuning.

## Critical Coding Rules

- **NO Colons Without Proper Delimiters:** Never insert colons (`:`) in places that lack proper C# syntax delimiters. Space Engineers C# has strict parsing requirements.
- **NO Mock Code:** Never generate placeholder, mock, or test code. All code must be production-ready and functional within the Space Engineers environment.
- **NO Testing Suggestions:** Do not suggest testing methods or frameworks. Scripts can only be tested in-game by uploading to programmable blocks and observing behavior.
- **Real Block Names Only:** Use actual block naming patterns from existing scripts, not placeholder names like "MyBlock" or "TestPiston".
- **NO External Libraries:** Do not suggest or use any external libraries or packages. All code must be compatible with the Space Engineers programmable block API. The linting errors are because VS Code doesn't understand the Space Engineers scripting context. In Space Engineers, the scripts are automatically wrapped in a class by the game engine, so the syntax is different from regular C#.

## Examples

- See `airlockInfoDisplays.cs` for LCD management and battery/capacitor logic.
- See `Moon-Elevator.cs` for a multi-piston, multi-rotor elevator with stateful inchworm movement.
- See `pistonscript.cs` for advanced piston sequencing with multiple movement patterns.

## Integration Points

- **In-game Blocks:** All integration is via the programmable block API (`IMyPistonBase`, `IMyMotorStator`, `IMyTextPanel`, etc.).
- **No External Services:** Scripts do not communicate outside the game.
