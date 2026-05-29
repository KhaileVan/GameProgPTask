using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Controls")]
    public bool usePlayer1Controls = true;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 10f;
    public float slamForce = 18f;

    Rigidbody2D rb;

    bool isGrounded;
    bool canDoubleJump;
    bool isStunned;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (isStunned)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        if (usePlayer1Controls)
            Player1();
        else
            Player2();
    }

    // ---------------- PLAYER 1 (WASD) ----------------
    void Player1()
    {
        float move = 0;

        if (Keyboard.current.aKey.isPressed) move = -1;
        if (Keyboard.current.dKey.isPressed) move = 1;

        rb.linearVelocity = new Vector2(move * moveSpeed, rb.linearVelocity.y);

        if (Keyboard.current.wKey.wasPressedThisFrame)
            Jump();

        if (Keyboard.current.sKey.wasPressedThisFrame)
            Slam();
    }

    // ---------------- PLAYER 2 (UHJK) ----------------
    void Player2()
    {
        float move = 0;

        if (Keyboard.current.hKey.isPressed) move = -1;
        if (Keyboard.current.kKey.isPressed) move = 1;

        rb.linearVelocity = new Vector2(move * moveSpeed, rb.linearVelocity.y);

        if (Keyboard.current.uKey.wasPressedThisFrame)
            Jump();

        if (Keyboard.current.jKey.wasPressedThisFrame)
            Slam();
    }

    // ---------------- MECHANICS ----------------

    void Jump()
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            canDoubleJump = true;
        }
        else if (canDoubleJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            canDoubleJump = false;
        }
    }

    void Slam()
    {
        if (!isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -slamForce);
        }
    }

    // ---------------- STUN ----------------

    public void SetStunned(bool value)
    {
        isStunned = value;
    }

    // ---------------- GROUND CHECK ----------------

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            canDoubleJump = false;
        }
    }

    void OnCollisionExit2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}