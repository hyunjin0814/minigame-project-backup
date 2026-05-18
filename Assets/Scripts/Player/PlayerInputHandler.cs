using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInputActions input;

    public Vector2 MoveInput { get; private set; }
    public bool JumpTriggered { get; set; }
    public bool JumpCutRequested { get; set; }

    private void Awake()
    {
        input = new PlayerInputActions();
    }

    private void OnEnable()
    {
        input.Player.Enable();
        input.Player.Move.performed += OnMove;
        input.Player.Move.canceled += OnMove;
        input.Player.Jump.performed += OnJumpPressed;
        input.Player.Jump.canceled += OnJumpReleased;
    }

    private void OnDisable()
    {
        input.Player.Move.performed -= OnMove;
        input.Player.Move.canceled -= OnMove;
        input.Player.Jump.performed -= OnJumpPressed;
        input.Player.Jump.canceled -= OnJumpReleased;
        input.Player.Disable();
    }

    private void OnMove(InputAction.CallbackContext ctx) => MoveInput = ctx.ReadValue<Vector2>();

    private void OnJumpPressed(InputAction.CallbackContext ctx)
    {
        JumpTriggered = true;
    }

    private void OnJumpReleased(InputAction.CallbackContext ctx)
    {
        JumpCutRequested = true;
    }
}
