using System;
using UnityEngine.InputSystem;

/// <summary>Entrada do jogo (teclado e gamepad) montada em codigo, sem depender de asset na cena.</summary>
public sealed class GameInput : IDisposable
{
    private readonly InputAction move;
    private readonly InputAction jump;
    private readonly InputAction punch;
    private readonly InputAction power;
    private readonly InputAction sprint;
    private readonly InputAction restart;
    private readonly InputAction back;

    public GameInput()
    {
        move = new InputAction("Move", InputActionType.Value);
        move.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
        move.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/rightArrow");
        move.AddBinding("<Gamepad>/leftStick/x");
        move.AddCompositeBinding("1DAxis")
            .With("Negative", "<Gamepad>/dpad/left")
            .With("Positive", "<Gamepad>/dpad/right");

        jump = CreateButton("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
        punch = CreateButton("Punch", "<Keyboard>/j", "<Gamepad>/buttonWest");
        power = CreateButton("Power", "<Keyboard>/k", "<Gamepad>/buttonNorth");
        sprint = CreateButton("Sprint", "<Keyboard>/leftShift", "<Gamepad>/rightTrigger");
        restart = CreateButton("Restart", "<Keyboard>/r", "<Gamepad>/select");
        back = CreateButton("Back", "<Keyboard>/escape", "<Gamepad>/start", "<Gamepad>/buttonEast");

        move.Enable();
    }

    public float Horizontal => move.ReadValue<float>();
    public bool SprintHeld => sprint.IsPressed();
    public bool JumpPressed => jump.WasPressedThisFrame();
    public bool PunchPressed => punch.WasPressedThisFrame();
    public bool PowerPressed => power.WasPressedThisFrame();
    public bool RestartPressed => restart.WasPressedThisFrame();
    public bool BackPressed => back.WasPressedThisFrame();

    public void Dispose()
    {
        move.Dispose();
        jump.Dispose();
        punch.Dispose();
        power.Dispose();
        sprint.Dispose();
        restart.Dispose();
        back.Dispose();
    }

    private static InputAction CreateButton(string name, params string[] paths)
    {
        InputAction action = new InputAction(name, InputActionType.Button);
        foreach (string path in paths)
        {
            action.AddBinding(path);
        }

        action.Enable();
        return action;
    }
}
