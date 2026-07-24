using UnityEngine;
using UnityEngine.InputSystem;
// Made with help from Claude.
/// <summary>
/// Momentum-based movement: hold an arrow key to pick a direction,
/// press Space to "strum" and apply an impulse force in that direction.
/// The Rigidbody2D keeps drifting after the strum until another strum
/// (or drag) changes its velocity - that's the "momentum" feel.
///
/// Uses the new Input System's Keyboard.current / Gamepad.current polling
/// directly rather than an Input Actions asset.
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

    [Header("Guitar Sprite")]
    [Tooltip("The guitar SpriteRenderer. Its default pose should point RIGHT, matching direction (1,0).")]
    [SerializeField] public SpriteRenderer Guitar;
    [Tooltip("How fast the guitar rotates to face the held direction, in degrees/second. Set very high (e.g. 3600) for an instant snap.")]
    public float guitarRotationSpeed = 720f;

    private Rigidbody2D rb;
    private float cooldownTimer;
    private float currentGuitarAngle;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearDamping = linearDamping;
    }

    void Update()
    {
        cooldownTimer -= Time.deltaTime;

        bool spaceStrum = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool gamepadStrum = Gamepad.current != null &&
            (Gamepad.current.dpad.down.wasPressedThisFrame || Gamepad.current.dpad.up.wasPressedThisFrame);
        bool strumPressed = spaceStrum || gamepadStrum;

        Vector2 direction = GetHeldDirection();
        UpdateGuitarRotation(direction);

        if (strumPressed && cooldownTimer <= 0f)
        {
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

    private void UpdateGuitarRotation(Vector2 direction)
    {
        if (Guitar == null) return;

        // Only update the target angle while a direction is actually held,
        // so the guitar holds its last pose instead of snapping to 0 when idle.
        if (direction != Vector2.zero)
        {
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            currentGuitarAngle = Mathf.MoveTowardsAngle(
                currentGuitarAngle, targetAngle, guitarRotationSpeed * Time.deltaTime);
        }

        Guitar.transform.rotation = Quaternion.Euler(0f, 0f, currentGuitarAngle);

        // Rotating alone makes the guitar look upside-down once it swings past
        // left (90 to 270 degrees), so flip it vertically in that range to keep
        // it looking right-side-up - same trick used for 2D weapon/aim sprites.
        bool facingLeft = Mathf.Abs(Mathf.DeltaAngle(0f, currentGuitarAngle)) > 90f;
        Guitar.flipY = facingLeft;
    }
}