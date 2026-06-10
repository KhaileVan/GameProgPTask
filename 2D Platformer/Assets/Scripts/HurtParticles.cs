using UnityEngine;

public class HurtParticles : MonoBehaviour
{
    private static Sprite particleSprite;

    private static Sprite GetParticleSprite()
    {
        if (particleSprite != null) return particleSprite;
        
        // Generate a simple solid 3x3 white texture in-memory
        Texture2D texture = new Texture2D(3, 3, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                texture.SetPixel(x, y, Color.white);
            }
        }
        texture.Apply();
        
        particleSprite = Sprite.Create(texture, new Rect(0, 0, 3, 3), new Vector2(0.5f, 0.5f), 16f);
        return particleSprite;
    }

    public static void Spawn(Vector3 position, Color color, int count = 6)
    {
        Sprite sprite = GetParticleSprite();
        for (int i = 0; i < count; i++)
        {
            GameObject p = new GameObject("HurtParticle");
            p.transform.position = position + (Vector3)Random.insideUnitCircle * 0.15f;
            
            SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = 15; // Render above other sprites
            
            Rigidbody2D rb = p.AddComponent<Rigidbody2D>();
            
            // Random direction burst with upward bias
            float angle = Random.Range(0, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(2.5f, 5.5f);
            rb.linearVelocity = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed + 3f);
            rb.gravityScale = 2.0f; // Pull down quickly for a snappy visual

            // Attach fade out and destruction script
            FadeAndDestroy fader = p.AddComponent<FadeAndDestroy>();
            fader.duration = Random.Range(0.25f, 0.45f);
        }
    }
}
