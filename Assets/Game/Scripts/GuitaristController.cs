using UnityEngine;
using UnityEngine.InputSystem;
// Made with help from Claude.

/// <summary>
/// Momentum-based movement: hold an arrow key to pick a direction,
/// press Space to "strum" and apply an impulse force in that direction.
/// The Rigidbody2D keeps drifting after the strum until another strum
/// (or drag) changes its velocity - that's the "momentum" feel.
///
/// Uses the new Input System's Keyboard.current polling directly, so no
/// Input Actions asset is required for this simple version. We can migrate
/// to Input Actions later when the guitar controller (a HID device) comes in,
/// since that's where Input Actions really start to pay off.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class GuitaristController : MonoBehaviour
{
    [Header("Strum Settings")]
    [Tooltip("How much force is applied per strum.")]
    public float strumForce = 8f;

    [Tooltip("Minimum time between strums, in seconds. Prevents holding Space from spamming force every frame.")]
    public float strumCooldown = 0.15f;

    [Header("Physics Feel")]
    [Tooltip("Linear drag applied to the Rigidbody2D. Low = drifts a long time. High = stops quickly.")]
    public float linearDamping = 0.3f;

    [Tooltip("Optional cap on max speed so momentum doesn't spiral out of control. Set to 0 to disable.")]
    public float maxSpeed = 15f;

    private Rigidbody2D rb;
    private float cooldownTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearDamping = linearDamping;
    }

    void Update()
    {
        cooldownTimer -= Time.deltaTime;

        bool strumPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        if (strumPressed && cooldownTimer <= 0f)
        {
            Vector2 direction = GetHeldDirection();

            if (direction != Vector2.zero)
            {
                rb.AddForce(direction * strumForce, ForceMode2D.Impulse);
                cooldownTimer = strumCooldown;
            }
        }
    }

    void FixedUpdate()
    {
        // Keep drag in sync if tweaked live in the Inspector during play testing
        rb.linearDamping = linearDamping;

        if (maxSpeed > 0f && rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    private Vector2 GetHeldDirection()
    {
        if (Keyboard.current == null) return Vector2.zero;

        float x = 0f;
        float y = 0f;

        if (Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        if (Keyboard.current.rightArrowKey.isPressed) x += 1f;
        if (Keyboard.current.upArrowKey.isPressed) y += 1f;
        if (Keyboard.current.downArrowKey.isPressed) y -= 1f;

        Vector2 dir = new Vector2(x, y);
        return dir.normalized; // handles diagonals cleanly (e.g. Up+Right = 45 degrees, not 2x force)
    }
}