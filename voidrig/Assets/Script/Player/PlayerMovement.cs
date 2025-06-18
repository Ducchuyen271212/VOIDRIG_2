//PlayerMovement.cs
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 10f;
    public float gravity = -9.81f;
    public float jumpHeight = 3f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("Aiming Integration")]
    public float aimingSpeedMultiplier = 0.4f; // Speed when aiming

    private CharacterController controller;
    private PlayerInput playerInput;
    private InputAction movementAction;
    private InputAction jumpAction;

    private Vector3 velocity;
    private bool isGrounded;
    private bool jumpQueued;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        movementAction = playerInput.actions["Movement"];
        jumpAction = playerInput.actions["Jump"];

        movementAction.Enable();
        jumpAction.Enable();

        jumpAction.performed += ctx => jumpQueued = true;
    }

    private void Update()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // Movement
        HandleMovement();

        // Jumping
        HandleJumping();

        // Apply gravity and move
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleMovement()
    {
        Vector2 input = movementAction.ReadValue<Vector2>();
        Vector3 move = transform.right * input.x + transform.forward * input.y;

        // Calculate speed with aiming consideration
        float currentSpeed = speed;

        // Reduce speed when aiming
        if (AimingManager.Instance != null && AimingManager.Instance.isAiming)
        {
            currentSpeed *= aimingSpeedMultiplier;
        }

        controller.Move(move * currentSpeed * Time.deltaTime);
    }

    private void HandleJumping()
    {
        if (jumpQueued && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpQueued = false;
        }
    }

    private void OnDisable()
    {
        jumpAction.performed -= ctx => jumpQueued = true;
    }
}