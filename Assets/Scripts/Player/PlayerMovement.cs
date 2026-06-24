using UnityEngine;
using UnityEngine.InputSystem;
/// <summary>
/// 使用 Rigidbody2D + Input Action（新输入系统）做纯 2D 平面移动（X/Y）。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerMovement : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private bool onlyAllowInMaze = true;

    private Rigidbody2D rb2d;
    private PlayerControllerInput inputControl;
    private Vector2 moveInput;

    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        inputControl = new PlayerControllerInput();
    }

    private void OnEnable()
    {
        inputControl.Player.Move.performed += OnMove;
        inputControl.Player.Move.canceled += OnMove;
        inputControl.Enable();
    }

    private void OnDisable()
    {
        inputControl.Player.Move.performed -= OnMove;
        inputControl.Player.Move.canceled -= OnMove;
        inputControl.Disable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        if (onlyAllowInMaze && !IsInMaze())
        {
            rb2d.velocity = Vector2.zero;
            return;
        }

        // 2D 平面移动：仅沿 X/Y 轴，不处理 Z 轴与跳跃。
        rb2d.velocity = moveInput * moveSpeed;
    }

    private static bool IsInMaze()
    {
        return GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.InMaze;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
    }
#endif
}
