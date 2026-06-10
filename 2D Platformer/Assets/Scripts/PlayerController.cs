using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 11f; // Increased from 8f to match higher gravity scale
    
    [Header("Physics & Ground Check")]
    public float gravityScale = 2.5f; // snaps player to the ground faster, preventing floaty jumps
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;
    public float fallMultiplier = 2.5f; // Pulls player down faster when falling
    public float fallBoundary = -10f; // Restarts if player falls below this Y coordinate

    [Header("Animations (Drag & Drop Sliced Sprites Here)")]
    public Sprite[] idleFrames;
    public Sprite[] walkFrames;
    public Sprite[] attackFrames;
    public Sprite[] hurtFrames;
    public Sprite[] deathFrames;
    public float animationFrameRate = 0.1f; // Speed of animation (lower is faster)

    [Header("Player Stats & Combat")]
    public int maxHealth = 3;
    public float attackRange = 0.8f;
    public LayerMask enemyLayer;

    [Header("UI Hearts (Drag life1, life2, life3 here)")]
    public GameObject[] heartObjects;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D playerCollider;

    private int currentHealth;
    private bool isGrounded;
    private float horizontalInput;
    private bool isAttacking;
    private bool isHurt;
    private bool isDead;
    private bool isHoldingJump;
    private Vector3 spawnPosition;

    // Animation states
    private enum AnimState { Idle, Walk, Jump, Attack, Hurt, Death }
    private AnimState currentState = AnimState.Idle;
    
    private float animationTimer;
    private int currentFrameIndex;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();
        
        currentHealth = maxHealth;
        spawnPosition = transform.position; // Save starting position for respawns
        UpdateHeartsUI(); // Sync the UI hearts on start

        // Ensure Rigidbody rotation is locked so the cat doesn't fall over
        rb.freezeRotation = true;

        // Apply snappy base gravity scale
        rb.gravityScale = gravityScale;
    }

    void Update()
    {
        if (isDead)
        {
            AnimateSprite();
            return;
        }

        // Check if player fell into the abyss
        if (transform.position.y < fallBoundary)
        {
            Die();
            return;
        }

        // Skip inputs if player is hurt or attacking
        if (isHurt) return;

        // Check input based on active input system
#if UNITY_INPUT_SYSTEM || ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            float horizontal = 0f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
            horizontalInput = horizontal;

            // Jump Input (W key)
            if (keyboard.wKey.wasPressedThisFrame && isGrounded && !isAttacking)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayJump();
            }
            isHoldingJump = keyboard.wKey.isPressed;

            // Attack Input (Space key)
            if (keyboard.spaceKey.wasPressedThisFrame && !isAttacking)
            {
                StartCoroutine(PerformAttack());
            }
        }
#else
        // Legacy Input Fallback (only compiles/runs if new input system is not active)
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Jump Input (W key)
        if (Input.GetKeyDown(KeyCode.W) && isGrounded && !isAttacking)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayJump();
        }
        isHoldingJump = Input.GetKey(KeyCode.W);

        // Attack Input (Space key)
        if (Input.GetKeyDown(KeyCode.Space) && !isAttacking)
        {
            StartCoroutine(PerformAttack());
        }
#endif

        // Flip Sprite based on movement direction
        if (horizontalInput > 0.01f)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (horizontalInput < -0.01f)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }

        // Ground check exactly at the bottom of the player's collider
        CheckGrounded();
        
        // Update animation logic
        UpdateAnimationState();
        AnimateSprite();
    }

    void FixedUpdate()
    {
        if (isDead)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        if (isHurt) return;

        // Apply horizontal velocity (lock on ground attacks, free in the air)
        if (isAttacking && isGrounded)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        }

        // Apply extra gravity when falling down to make jump feel less floaty
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !isHoldingJump)
        {
            // If rising but released jump key, pull down faster (variable jump height / low jump)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    void CheckGrounded()
    {
        Vector3 checkPos = transform.position;
        if (playerCollider != null)
        {
            // Position the check at the very bottom edge of the collider
            checkPos = new Vector3(transform.position.x, playerCollider.bounds.min.y, transform.position.z);
        }

        // Gather all colliders overlapping the check circle
        Collider2D[] colliders = Physics2D.OverlapCircleAll(checkPos, groundCheckRadius, groundLayer);
        
        isGrounded = false;
        foreach (var col in colliders)
        {
            // Ignore the player's own collider
            if (col.gameObject != gameObject)
            {
                // Only allow objects whose names contain "platform" to be stood on
                if (col.gameObject.name.ToLower().Contains("platform"))
                {
                    isGrounded = true;
                    break;
                }
            }
        }
    }

    void UpdateAnimationState()
    {
        if (isHurt)
        {
            SetState(AnimState.Hurt);
        }
        else if (isAttacking)
        {
            SetState(AnimState.Attack);
        }
        else if (!isGrounded)
        {
            SetState(AnimState.Jump);
        }
        else if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            SetState(AnimState.Walk);
        }
        else
        {
            SetState(AnimState.Idle);
        }
    }

    void SetState(AnimState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            currentFrameIndex = 0;
            animationTimer = 0f;
        }
    }

    void AnimateSprite()
    {
        Sprite[] activeFrames = null;
        bool loop = true;

        switch (currentState)
        {
            case AnimState.Idle:
                activeFrames = idleFrames;
                break;
            case AnimState.Walk:
                activeFrames = walkFrames;
                break;
            case AnimState.Attack:
                activeFrames = attackFrames;
                loop = false; // Attack animation plays once
                break;
            case AnimState.Hurt:
                activeFrames = hurtFrames;
                loop = false; // Hurt animation plays once
                break;
            case AnimState.Jump:
                // Jump uses the last frame of the walk animation or first of idle if not set
                if (walkFrames != null && walkFrames.Length > 0)
                {
                    spriteRenderer.sprite = walkFrames[walkFrames.Length - 1];
                }
                return;
            case AnimState.Death:
                activeFrames = deathFrames;
                loop = false; // Death animation stops at the last frame
                break;
        }

        if (activeFrames == null || activeFrames.Length == 0) return;

        // Advance animation frames
        animationTimer += Time.deltaTime;
        if (animationTimer >= animationFrameRate)
        {
            animationTimer = 0f;
            currentFrameIndex++;

            if (currentFrameIndex >= activeFrames.Length)
            {
                if (loop)
                {
                    currentFrameIndex = 0;
                }
                else
                {
                    currentFrameIndex = activeFrames.Length - 1; // Stay on the last frame
                }
            }
        }

        spriteRenderer.sprite = activeFrames[currentFrameIndex];
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayAttack();
        
        // Stop horizontal movement during attack only if grounded
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        // Calculate attack position in front of the player
        float directionX = transform.localScale.x;
        Vector3 attackPos = transform.position + new Vector3(directionX * 0.8f, 0.2f, 0f);

        // Spawn crescent slash visual effect
        GameObject slashObj = new GameObject("SlashEffect");
        slashObj.transform.position = attackPos;
        slashObj.transform.localScale = new Vector3(directionX, 1f, 1f);
        SlashEffect slash = slashObj.AddComponent<SlashEffect>();
        slash.Initialize(directionX > 0);

        // Detect enemies in front of the cat
        // We query all colliders in range and check if they have the EnemyController component
        // OR are on the designated enemyLayer. This handles both cases reliably!
        Collider2D[] colliders = Physics2D.OverlapCircleAll(attackPos, attackRange);
        bool hitRegistered = false;

        foreach (var col in colliders)
        {
            if (col.gameObject == gameObject) continue;

            EnemyController enemy = col.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(1);
                hitRegistered = true;
            }
            else if (((1 << col.gameObject.layer) & enemyLayer) != 0)
            {
                col.gameObject.SendMessage("TakeDamage", 1, SendMessageOptions.DontRequireReceiver);
                hitRegistered = true;
            }
        }

        // Trigger Screen Shake and hit freeze if we successfully hit an enemy
        if (hitRegistered)
        {
            CameraFollow camFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (camFollow == null)
            {
                camFollow = FindFirstObjectByType<CameraFollow>();
            }

            if (camFollow != null)
            {
                camFollow.TriggerShake(0.12f, 0.15f);
            }

            // Hit Freeze (Hitstop) for 0.04s to give the attack heavy weight
            float originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.04f);
            Time.timeScale = originalTimeScale;
        }
        
        int frameCount = (attackFrames != null && attackFrames.Length > 0) ? attackFrames.Length : 3;
        yield return new WaitForSeconds(animationFrameRate * frameCount);
        
        isAttacking = false;
    }

    public Vector3 SpawnPosition
    {
        get { return spawnPosition; }
        set { spawnPosition = value; }
    }

    public bool IsGrounded => isGrounded;

    public void GetHurtAndRespawn()
    {
        HandleFallOrWaterDeath();
    }

    // You can call this from an enemy or hazard script to damage the player
    public void GetHurt()
    {
        if (isDead || isHurt) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayHurt();
        currentHealth--;
        UpdateHeartsUI();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(PerformHurt());
        }
    }

    IEnumerator PerformHurt()
    {
        isHurt = true;

        // Apply a small knockback opposite to where the cat is facing
        float knockbackDir = -transform.localScale.x;
        rb.linearVelocity = new Vector2(knockbackDir * 3f, 4f); // Knocks back and slightly upward

        // Flash Red!
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
        }

        int frameCount = (hurtFrames != null && hurtFrames.Length > 0) ? hurtFrames.Length : 2;
        yield return new WaitForSeconds(animationFrameRate * frameCount);

        // Restore default color (white)
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        isHurt = false;
    }

    // Trigger death and reload the scene
    public void Die()
    {
        if (!isDead)
        {
            isDead = true;
            SetState(AnimState.Death);
            StartCoroutine(RestartSequence());
        }
    }

    IEnumerator RestartSequence()
    {
        // Wait for a short duration so the death animation can play
        yield return new WaitForSeconds(0.9f);

        // Reload the currently active scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    // Handle falling off screen or landing in water
    private void HandleFallOrWaterDeath()
    {
        if (isDead) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayHurt();
        currentHealth--;
        UpdateHeartsUI();

        if (currentHealth <= 0)
        {
            BounceAndDie();
        }
        else
        {
            // Respawn back at start of level (used for falling into the abyss/underground)
            StartCoroutine(RespawnSequence());
        }
    }

    IEnumerator RespawnSequence()
    {
        isHurt = true;
        rb.linearVelocity = Vector2.zero;
        
        // Snap position back to start
        transform.position = spawnPosition;
        
        // Wait a tiny bit before restoring controls so they don't slide
        yield return new WaitForSeconds(0.2f);
        isHurt = false;
    }

    // Handles touching the water when the player still has lives
    private void HandleWaterTouch()
    {
        if (isDead) return;

        currentHealth--;
        UpdateHeartsUI();

        if (currentHealth <= 0)
        {
            BounceAndDie();
        }
        else
        {
            // Bounce up and backwards to land back on the platform
            StartCoroutine(PerformWaterBounce());
        }
    }

    IEnumerator PerformWaterBounce()
    {
        isHurt = true;
        SetState(AnimState.Hurt);

        // Push backwards (opposite to facing direction) and upwards
        float knockbackDir = -transform.localScale.x;
        rb.linearVelocity = new Vector2(knockbackDir * 4f, jumpForce * 1.2f);

        // Wait for the bounce duration before letting the player move again
        yield return new WaitForSeconds(0.6f);

        isHurt = false;
    }

    private void BounceAndDie()
    {
        if (!isDead)
        {
            // Bounce the cat upwards (slightly higher than a normal jump)
            rb.linearVelocity = new Vector2(0f, jumpForce * 1.1f);
            
            // Disable collider so they fall through the platforms and water off-screen
            if (playerCollider != null)
            {
                playerCollider.enabled = false;
            }

            Die();
        }
    }

    private void UpdateHeartsUI()
    {
        if (heartObjects == null) return;

        // Disable hearts based on damage taken
        for (int i = 0; i < heartObjects.Length; i++)
        {
            if (heartObjects[i] != null)
            {
                heartObjects[i].SetActive(i < currentHealth);
            }
        }
    }

    private void HandleRegenPickup(GameObject regenObj)
    {
        if (currentHealth < maxHealth)
        {
            currentHealth++;
            UpdateHeartsUI();

            if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup();

            // Spawn green recovery sparkles
            HurtParticles.Spawn(regenObj.transform.position, new Color(0.2f, 1.0f, 0.2f), 8);

            Destroy(regenObj);
        }
    }

    // Detect collision with solid tiles (Underground, Water, or Regen)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        string objectName = collision.gameObject.name.ToLower();
        
        if (objectName.Contains("underground"))
        {
            if (collision.gameObject.GetComponent<UndergroundStraight>() == null)
            {
                HandleFallOrWaterDeath();
            }
        }
        else if (objectName.Contains("water"))
        {
            HandleWaterTouch();
        }
        else if (objectName.Contains("regen"))
        {
            HandleRegenPickup(collision.gameObject);
        }
    }

    // Detect overlap with trigger zones (Underground, Water, or Regen)
    private void OnTriggerEnter2D(Collider2D other)
    {
        string objectName = other.gameObject.name.ToLower();

        if (objectName.Contains("underground"))
        {
            if (other.GetComponent<UndergroundStraight>() == null)
            {
                HandleFallOrWaterDeath();
            }
        }
        else if (objectName.Contains("water"))
        {
            HandleWaterTouch();
        }
        else if (objectName.Contains("regen"))
        {
            HandleRegenPickup(other.gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw the ground check circle in red in the Editor Scene view
        Gizmos.color = Color.red;
        Vector3 checkPos = transform.position;
        if (playerCollider != null)
        {
            checkPos = new Vector3(transform.position.x, playerCollider.bounds.min.y, transform.position.z);
        }
        Gizmos.DrawWireSphere(checkPos, groundCheckRadius);

        // Draw the attack range circle in cyan in the Editor Scene view
        Gizmos.color = Color.cyan;
        float directionX = transform.localScale.x;
        Vector3 attackPos = transform.position + new Vector3(directionX * 0.8f, 0.2f, 0f);
        Gizmos.DrawWireSphere(attackPos, attackRange);
    }
}
