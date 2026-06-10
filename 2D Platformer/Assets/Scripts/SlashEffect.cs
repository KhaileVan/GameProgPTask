using UnityEngine;

public class SlashEffect : MonoBehaviour
{
    private static Sprite slashSprite;

    private float duration = 0.12f;
    private float timer = 0f;
    private SpriteRenderer sr;
    private float rotationSpeed = 200f; // degrees per second
    private int directionFactor = 1;

    private static Sprite GetSlashSprite()
    {
        if (slashSprite != null) return slashSprite;

        int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        // Fill with transparent
        Color transparent = new Color(0, 0, 0, 0);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, transparent);
            }
        }

        // Draw a crescent arc facing right.
        // Outer circle: Center (14, 16), Radius 12
        // Inner circle: Center (9, 16), Radius 12
        float cx1 = 14f;
        float cy1 = 16f;
        float r1 = 12f;

        float cx2 = 9f;
        float cy2 = 16f;
        float r2 = 12f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx1 = x - cx1;
                float dy1 = y - cy1;
                float distSq1 = dx1 * dx1 + dy1 * dy1;

                float dx2 = x - cx2;
                float dy2 = y - cy2;
                float distSq2 = dx2 * dx2 + dy2 * dy2;

                if (distSq1 <= r1 * r1 && distSq2 > r2 * r2 && x > cx1)
                {
                    float distFromCenter = Mathf.Abs(dy1) / r1; // 0 to 1
                    float depth = (x - cx1) / r1; // 0 to 1
                    
                    Color pixelColor;
                    if (distFromCenter > 0.6f || depth < 0.2f)
                    {
                        pixelColor = new Color(0.2f, 0.9f, 1f, 1f); // Vibrant cyan border
                    }
                    else
                    {
                        pixelColor = Color.white; // Bright white core
                    }

                    texture.SetPixel(x, y, pixelColor);
                }
            }
        }

        texture.Apply();
        
        // Pivot in center, PPU is 16
        slashSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        return slashSprite;
    }

    public void Initialize(bool facingRight)
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = GetSlashSprite();
        sr.sortingOrder = 12; // Draw on top of players and enemies
        
        directionFactor = facingRight ? -1 : 1;
        
        // Start with a slight tilt
        transform.localRotation = Quaternion.Euler(0, 0, directionFactor * -15f);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            Destroy(gameObject);
            return;
        }

        // Rotate along the swing
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime * directionFactor);

        // Fade out
        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Lerp(1f, 0f, timer / duration);
            sr.color = c;
        }
    }
}
