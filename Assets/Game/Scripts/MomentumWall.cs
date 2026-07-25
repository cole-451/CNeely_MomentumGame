using UnityEngine;

/// <summary>
/// A wall that only lets objects pass through once they're moving fast enough.
/// Below the threshold it behaves like a solid wall (pushes the player back out
/// and cancels the velocity driving them into it). At or above the threshold,
/// it does nothing and lets them fly straight through.
///
/// Implemented as a TRIGGER, not a solid collider. If this used a normal solid
/// Collider2D instead, Unity's physics engine would already stop the player
/// before OnCollisionEnter2D fires, making a speed check arrive one frame too
/// late. Using a trigger + manual push-out avoids that entirely.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MomentumWall : MonoBehaviour
{
    [Tooltip("Minimum speed (units/sec) required to break through this wall.")]
    public float requiredSpeed = 10f;

    [Tooltip("Tag on your player GameObject. Set this in Project Settings > Tags and Tagging, then assign it to your player object.")]
    public string playerTag = "Player";

    [Tooltip("Optional: log the player's speed each time they touch the wall, to help you tune requiredSpeed.")]
    public bool debugLogSpeed = false;

    private Collider2D wallCollider;

    void Awake()
    {
        wallCollider = GetComponent<Collider2D>();
        wallCollider.isTrigger = true; // blocking is handled manually below, not by physics
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        float speed = rb.linearVelocity.magnitude;
        if (debugLogSpeed) Debug.Log($"MomentumWall: player speed = {speed:F2} (needs {requiredSpeed})");

        if (speed >= requiredSpeed)
        {
            return; // fast enough - let them pass straight through, do nothing
        }

        // Too slow - act like a solid wall: push the player back out of the
        // overlap and cancel whatever velocity is still driving them into it.
        ColliderDistance2D dist = Physics2D.Distance(wallCollider, other);
        if (dist.isOverlapped)
        {
            Vector2 pushOut = dist.pointA - dist.pointB;
            rb.position += pushOut;

            Vector2 normal = pushOut.normalized;
            float velocityIntoWall = Vector2.Dot(rb.linearVelocity, -normal);
            if (velocityIntoWall > 0f)
            {
                rb.linearVelocity += normal * velocityIntoWall;
            }
        }
    }
}