// gravityAlignGyro.cs
// Aligns a subgrid to gravity using gyroscopes and a ship controller

string gyroTag = ""; // Optional: set to part of gyro name to filter, or leave empty for all gyros on this grid
string controllerTag = ""; // Optional: set to part of ship controller name to filter, or leave empty for first found
float alignSpeed = 1.0f; // Gyro override speed multiplier
float alignTolerance = 0.01f; // Radians, how close is "aligned"

List<IMyGyro> gyros = new List<IMyGyro>();
IMyShipController controller;
bool error = false;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
    GridTerminalSystem.GetBlocksOfType(gyros, g =>
        g.CubeGrid == Me.CubeGrid && (gyroTag == "" || g.CustomName.Contains(gyroTag))
    );
    if (gyros.Count == 0)
    {
        Echo("Error: No gyroscopes found.");
        error = true;
    }
    var controllers = new List<IMyShipController>();
    GridTerminalSystem.GetBlocksOfType(controllers, c =>
        c.CubeGrid == Me.CubeGrid && (controllerTag == "" || c.CustomName.Contains(controllerTag))
    );
    if (controllers.Count == 0)
    {
        Echo("Error: No ship controller found.");
        error = true;
    }
    else
    {
        controller = controllers[0];
    }
}

void Main(string argument, UpdateType updateSource)
{
    if (error) return;
    Vector3D gravity = controller.GetNaturalGravity();
    if (gravity.LengthSquared() < 0.0001)
    {
        Echo("No gravity detected.");
        SetGyroOverride(false);
        return;
    }
    gravity.Normalize();

    // Ship "down" is -controller.WorldMatrix.Up
    Vector3D shipDown = -controller.WorldMatrix.Up;
    double angle = Math.Acos(MathHelper.Clamp(gravity.Dot(shipDown), -1, 1));
    Vector3D axis = gravity.Cross(shipDown);

    Echo($"Angle to gravity: {Math.Round(angle * 180 / Math.PI, 2)} deg");

    if (angle < alignTolerance)
    {
        SetGyroOverride(false);
        Echo("Aligned!");
        return;
    }

    // Convert world axis to local grid axis
    Vector3D local = Vector3D.TransformNormal(axis, MatrixD.Transpose(controller.WorldMatrix));
    if (local.LengthSquared() > 0) local.Normalize();
    Vector3D gyroInput = local * angle * alignSpeed;

    foreach (var gyro in gyros)
    {
        gyro.GyroOverride = true;
        gyro.Pitch = (float)gyroInput.X;
        gyro.Yaw = (float)gyroInput.Y;
        gyro.Roll = (float)gyroInput.Z;
    }
}

void SetGyroOverride(bool on)
{
    foreach (var gyro in gyros)
    {
        gyro.GyroOverride = on;
        if (!on)
        {
            gyro.Pitch = 0f;
            gyro.Yaw = 0f;
            gyro.Roll = 0f;
        }
    }
}
