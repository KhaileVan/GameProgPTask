using UnityEngine;

public class FadeAndDestroy : MonoBehaviour
{
    public float duration = 0.4f;
    private float timer = 0f;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            Destroy(gameObject);
            return;
        }

        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Lerp(1f, 0f, timer / duration);
            sr.color = c;
        }
    }
}
