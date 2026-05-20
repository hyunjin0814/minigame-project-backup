using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInputActions input;

    public Vector2 MoveInput { get; private set; }
    public bool JumpTriggered { get; set; }
    public bool JumpCutRequested { get; set; }

    public event Action OnTransformHuman;
    public event Action OnTransformDog;
    public event Action OnTransformChameleon;
    public event Action OnAttack;

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

        input.Player.TransformHuman.performed += OnTransformHumanPerformed;
        input.Player.TransformDog.performed += OnTransformDogPerformed;
        input.Player.TransformChameleon.performed += OnTransformChameleonPerformed;
        input.Player.Attack.performed += OnAttackPerformed;
    }

    private void OnDisable()
    {
        input.Player.Move.performed -= OnMove;
        input.Player.Move.canceled -= OnMove;
        input.Player.Jump.performed -= OnJumpPressed;
        input.Player.Jump.canceled -= OnJumpReleased;

        input.Player.TransformHuman.performed -= OnTransformHumanPerformed;
        input.Player.TransformDog.performed -= OnTransformDogPerformed;
        input.Player.TransformChameleon.performed -= OnTransformChameleonPerformed;
        input.Player.Attack.performed -= OnAttackPerformed;

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

    private void OnTransformHumanPerformed(InputAction.CallbackContext ctx)
    {
        OnTransformHuman?.Invoke();
    }

    private void OnTransformDogPerformed(InputAction.CallbackContext ctx)
    {
        OnTransformDog?.Invoke();
    }

    private void OnTransformChameleonPerformed(InputAction.CallbackContext ctx)
    {
        OnTransformChameleon?.Invoke();
    }

    private void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        OnAttack?.Invoke();
    }
}
