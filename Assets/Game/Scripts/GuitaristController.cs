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
[RequireComponent(typeof(AudioSource))]
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

    [Header("Guitar Fret Directions")]
    [Tooltip("Direction contributed while the Green (A) fret is held.")]
    public Vector2 greenFretDirection = Vector2.up;
    [Tooltip("Direction contributed while the Red (B) fret is held.")]
    public Vector2 redFretDirection = Vector2.down;
    [Tooltip("Direction contributed while the Yellow (Y) fret is held.")]
    public Vector2 yellowFretDirection = Vector2.left;
    [Tooltip("Direction contributed while the Blue (X) fret is held.")]
    public Vector2 blueFretDirection = Vector2.right;
    [Tooltip("Direction contributed while the Orange (LB) fret is held. Left at zero by default - reserved for later (boost, diagonal, whatever you want).")]
    public Vector2 orangeFretDirection = Vector2.zero;

    [Header("Strum Sounds")]
    [Tooltip("Played when a strum launches the player upward.")]
    public AudioClip upSound;
    [Tooltip("Played when a strum launches the player downward.")]
    public AudioClip downSound;
    [Tooltip("Played when a strum launches the player left.")]
    public AudioClip leftSound;
    [Tooltip("Played when a strum launches the player right.")]
    public AudioClip rightSound;
    [Range(0f, 1f)]
    [Tooltip("Volume for strum sounds.")]
    public float strumSoundVolume = 1f;

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private float cooldownTimer;
    private float currentGuitarAngle;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearDamping = linearDamping;
        audioSource = GetComponent<AudioSource>();
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
                PlayStrumSound(direction);
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

    private void PlayStrumSound(Vector2 direction)
    {
        if (audioSource == null) return;

        // Direction can be a diagonal (e.g. two frets held together), but we
        // only have 4 sounds - so pick whichever axis is stronger and play
        // the sound for that side.
        AudioClip clip;
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            clip = direction.x > 0f ? rightSound : leftSound;
        }
        else
        {
            clip = direction.y > 0f ? upSound : downSound;
        }

        if (clip != null)
        {
            audioSource.PlayOneShot(clip, strumSoundVolume);
        }
    }

    private Vector2 GetHeldDirection()
    {
        Vector2 dir = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.isPressed) dir += Vector2.left;
            if (Keyboard.current.rightArrowKey.isPressed) dir += Vector2.right;
            if (Keyboard.current.upArrowKey.isPressed) dir += Vector2.up;
            if (Keyboard.current.downArrowKey.isPressed) dir += Vector2.down;
        }

        if (Gamepad.current != null)
        {
            // Mode 1 default mapping: A=Green, B=Red, Y=Yellow, X=Blue, LB=Orange
            if (Gamepad.current.buttonSouth.isPressed) dir += greenFretDirection;
            if (Gamepad.current.buttonEast.isPressed) dir += redFretDirection;
            if (Gamepad.current.buttonNorth.isPressed) dir += yellowFretDirection;
            if (Gamepad.current.buttonWest.isPressed) dir += blueFretDirection;
            if (Gamepad.current.leftShoulder.isPressed) dir += orangeFretDirection;
        }

        return dir.normalized; // handles diagonals cleanly - e.g. Green+Blue held together = Up+Right diagonal
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