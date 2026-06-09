using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private bool isActivated = false;

    [Header("Visuals (Optional)")]
    public Sprite activatedSprite;
    
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isActivated) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null && other.CompareTag("Player"))
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        if (player != null)
        {
            // Activate the checkpoint
            isActivated = true;
            
            // Update the player's respawn position to this checkpoint's position
            player.SpawnPosition = new Vector3(transform.position.x, transform.position.y, player.transform.position.z);

            // Change sprite if an active sprite is provided
            if (spriteRenderer != null && activatedSprite != null)
            {
                spriteRenderer.sprite = activatedSprite;
            }

            // Spawn green sparkles to indicate activation!
            HurtParticles.Spawn(transform.position, Color.green, 12);
        }
    }
}
