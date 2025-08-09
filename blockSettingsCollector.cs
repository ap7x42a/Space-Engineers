// === Block Settings Collector Script ===
// Run this script to collect all turret lift block settings
// Results will be saved to this programmable block's custom data

Program()
{
    // No update frequency needed - this is a one-time collection script
}

void Main(string argument)
{
    Echo("Collecting all block settings...");
    CollectAllBlockSettings();
    Echo("Collection complete! Check this programmable block's custom data.");
}

void CollectAllBlockSettings()
{
    string report = "=== TURRET LIFT BLOCK SETTINGS REPORT ===\n\n";
    
    // Collect piston settings
    var leftPiston = GridTerminalSystem.GetBlockWithName("2nd Innermost Turret Lift Left Piston") as IMyPistonBase;
    var rightPiston = GridTerminalSystem.GetBlockWithName("2nd Innermost Turret Lift Right Piston") as IMyPistonBase;
    
    // Collect rotor settings
    var leftRotor = GridTerminalSystem.GetBlockWithName("2nd Innermost Left Turret Lift Rotor") as IMyMotorStator;
    var rightRotor = GridTerminalSystem.GetBlockWithName("2nd Innermost Right Turret Lift Rotor") as IMyMotorStator;
    var outerLeftRotor = GridTerminalSystem.GetBlockWithName("Outermost Turret Lift Left Rotor") as IMyMotorStator;
    var outerRightRotor = GridTerminalSystem.GetBlockWithName("Outermost Turret Lift Right Rotor") as IMyMotorStator;
    
    // Report piston settings
    report += "PISTON SETTINGS:\n";
    report += "================\n";
    
    if (leftPiston != null)
    {
        report += $"Left Piston ('2nd Innermost Turret Lift Left Piston'):\n";
        report += $"  - Velocity: {leftPiston.Velocity} m/s\n";
        report += $"  - Min Limit: {leftPiston.MinLimit} m\n";
        report += $"  - Max Limit: {leftPiston.MaxLimit} m\n";
        report += $"  - Current Position: {leftPiston.CurrentPosition} m\n";
        report += $"  - Enabled: {leftPiston.Enabled}\n\n";
    }
    else
    {
        report += "Left Piston: NOT FOUND\n\n";
    }
    
    if (rightPiston != null)
    {
        report += $"Right Piston ('2nd Innermost Turret Lift Right Piston'):\n";
        report += $"  - Velocity: {rightPiston.Velocity} m/s\n";
        report += $"  - Min Limit: {rightPiston.MinLimit} m\n";
        report += $"  - Max Limit: {rightPiston.MaxLimit} m\n";
        report += $"  - Current Position: {rightPiston.CurrentPosition} m\n";
        report += $"  - Enabled: {rightPiston.Enabled}\n\n";
    }
    else
    {
        report += "Right Piston: NOT FOUND\n\n";
    }
    
    // Report rotor settings
    report += "ROTOR SETTINGS:\n";
    report += "===============\n";
    
    if (leftRotor != null)
    {
        report += $"Left Inner Rotor ('2nd Innermost Left Turret Lift Rotor'):\n";
        report += $"  - Target Velocity: {leftRotor.TargetVelocityRPM} RPM\n";
        report += $"  - Lower Limit: {leftRotor.LowerLimitRad} rad ({leftRotor.LowerLimitRad * 180 / Math.PI:F1}°)\n";
        report += $"  - Upper Limit: {leftRotor.UpperLimitRad} rad ({leftRotor.UpperLimitRad * 180 / Math.PI:F1}°)\n";
        report += $"  - Torque: {leftRotor.Torque} N⋅m\n";
        report += $"  - Braking Torque: {leftRotor.BrakingTorque} N⋅m\n";
        report += $"  - Enabled: {leftRotor.Enabled}\n\n";
    }
    else
    {
        report += "Left Inner Rotor: NOT FOUND\n\n";
    }
    
    if (rightRotor != null)
    {
        report += $"Right Inner Rotor ('2nd Innermost Right Turret Lift Rotor'):\n";
        report += $"  - Target Velocity: {rightRotor.TargetVelocityRPM} RPM\n";
        report += $"  - Lower Limit: {rightRotor.LowerLimitRad} rad ({rightRotor.LowerLimitRad * 180 / Math.PI:F1}°)\n";
        report += $"  - Upper Limit: {rightRotor.UpperLimitRad} rad ({rightRotor.UpperLimitRad * 180 / Math.PI:F1}°)\n";
        report += $"  - Torque: {rightRotor.Torque} N⋅m\n";
        report += $"  - Braking Torque: {rightRotor.BrakingTorque} N⋅m\n";
        report += $"  - Enabled: {rightRotor.Enabled}\n\n";
    }
    else
    {
        report += "Right Inner Rotor: NOT FOUND\n\n";
    }
    
    if (outerLeftRotor != null)
    {
        report += $"Left Outer Rotor ('Outermost Turret Lift Left Rotor'):\n";
        report += $"  - Target Velocity: {outerLeftRotor.TargetVelocityRPM} RPM\n";
        report += $"  - Lower Limit: {outerLeftRotor.LowerLimitRad} rad ({outerLeftRotor.LowerLimitRad * 180 / Math.PI:F1}°)\n";
        report += $"  - Upper Limit: {outerLeftRotor.UpperLimitRad} rad ({outerLeftRotor.UpperLimitRad * 180 / Math.PI:F1}°)\n";
        report += $"  - Torque: {outerLeftRotor.Torque} N⋅m\n";
        report += $"  - Braking Torque: {outerLeftRotor.BrakingTorque} N⋅m\n";
        report += $"  - Enabled: {outerLeftRotor.Enabled}\n\n";
    }
    else
    {
        report += "Left Outer Rotor: NOT FOUND\n\n";
    }
    
    if (outerRightRotor != null)
    {
        report += $"Right Outer Rotor ('Outermost Turret Lift Right Rotor'):\n";
        report += $"  - Target Velocity: {outerRightRotor.TargetVelocityRPM} RPM\n";
        report += $"  - Lower Limit: {outerRightRotor.LowerLimitRad} rad ({outerRightRotor.LowerLimitRad * 180 / Math.PI:F1}°)\n";
        report += $"  - Upper Limit: {outerRightRotor.UpperLimitRad} rad ({outerRightRotor.UpperLimitRad * 180 / Math.PI:F1}°)\n";
        report += $"  - Torque: {outerRightRotor.Torque} N⋅m\n";
        report += $"  - Braking Torque: {outerRightRotor.BrakingTorque} N⋅m\n";
        report += $"  - Enabled: {outerRightRotor.Enabled}\n\n";
    }
    else
    {
        report += "Right Outer Rotor: NOT FOUND\n\n";
    }
    
    // Add timing analysis section with REAL calculations
    report += "TIMING ANALYSIS:\n";
    report += "================\n";
    
    // Calculate actual timing based on block settings
    if (leftPiston != null && rightPiston != null)
    {
        float pistonDistance = Math.Abs(leftPiston.MaxLimit - leftPiston.MinLimit);
        float pistonTime = Math.Abs(leftPiston.Velocity) > 0 ? pistonDistance / Math.Abs(leftPiston.Velocity) : 0;
        report += $"CALCULATED PISTON TIMING:\n";
        report += $"- Distance to travel: {pistonDistance:F2} m\n";
        report += $"- Velocity: {Math.Abs(leftPiston.Velocity):F2} m/s\n";
        report += $"- Time needed: {pistonTime:F2} seconds\n\n";
    }
    
    if (leftRotor != null)
    {
        float rotorRange = Math.Abs(leftRotor.UpperLimitRad - leftRotor.LowerLimitRad) * 180f / (float)Math.PI;
        float rotorTime = Math.Abs(leftRotor.TargetVelocityRPM) > 0 ? rotorRange / (Math.Abs(leftRotor.TargetVelocityRPM) * 6f) : 0;
        report += $"CALCULATED INNER ROTOR TIMING:\n";
        report += $"- Rotation range: {rotorRange:F1}°\n";
        report += $"- Velocity: {Math.Abs(leftRotor.TargetVelocityRPM):F1} RPM\n";
        report += $"- Time needed: {rotorTime:F2} seconds\n\n";
    }
    
    if (outerLeftRotor != null)
    {
        float outerRange = Math.Abs(outerLeftRotor.UpperLimitRad - outerLeftRotor.LowerLimitRad) * 180f / (float)Math.PI;
        float outerTime = Math.Abs(outerLeftRotor.TargetVelocityRPM) > 0 ? outerRange / (Math.Abs(outerLeftRotor.TargetVelocityRPM) * 6f) : 0;
        report += $"CALCULATED OUTER ROTOR TIMING:\n";
        report += $"- Rotation range: {outerRange:F1}°\n";
        report += $"- Velocity: {Math.Abs(outerLeftRotor.TargetVelocityRPM):F1} RPM\n";
        report += $"- Time needed: {outerTime:F2} seconds\n\n";
    }
    
    report += $"Generated: {DateTime.Now}\n";
    report += "Copy this entire report to provide block configuration details.";
    
    // Save to programmable block's custom data
    Me.CustomData = report;
    
    Echo($"Report saved to custom data ({report.Length} characters)");
    Echo("You can now copy/paste the custom data field contents.");
}
