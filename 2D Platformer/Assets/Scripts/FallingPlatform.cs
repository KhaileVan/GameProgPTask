using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class FallingPlatform : MonoBehaviour
{
    [Header("Settings")]
    public float fallDelay = 0.5f; // Time before falling
    public float respawnDelay = 4.0f; // Time before respawning
    public bool shouldRespawn = true;

    private Rigidbody2D rb;
    private Collider2D col;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool isFalling = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // Ensure Rigidbody is set to Kinematic initially so it floats in the air
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isFalling) return;

        // Check if the object landing on us is the Player or the Ally Cat
        bool isCharacter = collision.gameObject.CompareTag("Player") ||
                            collision.gameObject.GetComponent<PlayerController>() != null ||
                            collision.gameObject.GetComponent<AllyController>() != null;

        if (isCharacter)
        {
            // Verify they landed on TOP of the platform (feet touching)
            // The normal should point downwards (towards the platform)
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.7f) // Contact is on top surface
                {
                    StartCoroutine(FallSequence());
                    break;
                }
            }
        }
    }

    IEnumerator FallSequence()
    {
        isFalling = true;

        // Shake effect before falling to warn the player
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        while (elapsed < fallDelay)
        {
            float shakeX = Random.Range(-0.05f, 0.05f);
            transform.position = startPos + new Vector3(shakeX, 0f, 0f);
            yield return new WaitForSeconds(0.05f);
            elapsed += 0.05f;
        }

        // Restore position before dropping
        transform.position = startPos;

        // Fall! Switch body type to Dynamic
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1.5f;

        // Wait a few seconds for the platform to fall off-screen
        yield return new WaitForSeconds(3.0f);

        // Turn off renderer and collider so it's "hidden"
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
        if (col != null) col.enabled = false;
        
        // Reset physics state
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (shouldRespawn)
        {
            yield return new WaitForSeconds(respawnDelay);
            Respawn(sr);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Respawn(SpriteRenderer sr)
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        
        if (sr != null) sr.enabled = true;
        if (col != null) col.enabled = true;

        isFalling = false;
    }
}
