using UnityEngine;

public class StealObject : MonoBehaviour
{
    public Transform holder;
    public Vector3 offset = new Vector3(0, 1.2f, 0);

    Rigidbody2D rb;
    Collider2D col;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (holder == null) return;

        transform.position = holder.position + offset;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player1") &&
            !collision.gameObject.CompareTag("Player2"))
            return;

        Attach(collision.transform);
    }

    void Attach(Transform player)
    {
        holder = player;

        // SAVE LAST HOLDER FOR LEVEL 2
        GameManager.instance.lastHolderTag = player.tag;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (col != null)
        {
            col.enabled = true;
        }
    }
}