using UnityEngine;

public class UndergroundStraight : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleTouch(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleTouch(collision.gameObject);
    }

    private void HandleTouch(GameObject obj)
    {
        PlayerController player = obj.GetComponent<PlayerController>();
        if (player == null && obj.CompareTag("Player"))
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        if (player != null)
        {
            // Trigger the player's hurt and respawn logic (reduces health and snaps position)
            player.GetHurtAndRespawn();

            // Find and teleport the Ally Cat to the checkpoint alongside the Player
            AllyController ally = FindFirstObjectByType<AllyController>();
            if (ally != null && ally.IsRecruited)
            {
                // Teleport slightly behind and above the player's spawn position to prevent falling
                ally.transform.position = player.SpawnPosition + new Vector3(-1f, 0.5f, 0f);
                
                Rigidbody2D allyRb = ally.GetComponent<Rigidbody2D>();
                if (allyRb != null)
                {
                    allyRb.linearVelocity = Vector2.zero;
                }
            }
        }
        else
        {
            // Check if the Ally Cat touched the death boundary alone
            AllyController ally = obj.GetComponent<AllyController>();
            if (ally != null && ally.IsRecruited)
            {
                PlayerController activePlayer = FindFirstObjectByType<PlayerController>();
                if (activePlayer != null)
                {
                    // Teleport the Ally Cat back to the player's current position (with a safe offset)
                    ally.transform.position = activePlayer.transform.position + new Vector3(-1f, 0.5f, 0f);
                    
                    Rigidbody2D allyRb = ally.GetComponent<Rigidbody2D>();
                    if (allyRb != null)
                    {
                        allyRb.linearVelocity = Vector2.zero;
                    }
                }
            }
        }
    }
}
