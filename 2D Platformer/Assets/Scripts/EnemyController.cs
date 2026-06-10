using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class EnemyController : MonoBehaviour
{
    public enum EnemyBehavior { Patrol, Sentry }

    [Header("Behavior Settings")]
    public EnemyBehavior behavior = EnemyBehavior.Patrol;
    public bool isFlying = false; // Check this for the Bird so it doesn't fall due to gravity!
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;

    [Header("Stats")]
    public int maxHealth = 2;
    public int damageDealt = 1;

    [Header("AI Detection & Combat")]
    public float detectionRange = 5f;
    public float attackRange = 1.1f;
    public float attackCooldown = 1.5f;

    [Header("Ground & Ledge Detection")]
    public LayerMask groundLayer;
    public LayerMask playerLayer;
    public float wallCheckDistance = 0.5f;
    public float ledgeCheckDistance = 0.5f;

    [Header("Animations (Drag & Drop Sliced Sprites)")]
    public Sprite[] idleFrames;
    public Sprite[] walkFrames;
    public Sprite[] attackFrames;
    public Sprite[] hurtFrames;
    public Sprite[] deathFrames;
    public float animationFrameRate = 0.1f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D enemyCollider;
    private Transform playerTransform;

    private int currentHealth;
    private int walkDirection = 1; // 1 = Right, -1 = Left
    private bool isChasing = false;
    private bool isAttacking = false;
    private bool isHurt = false;
    private bool isDead = false;
    private float nextAttackTime = 0f;

    // Animation state tracking
    private enum AnimState { Idle, Walk, Attack, Hurt, Death }
    private AnimState currentState = AnimState.Idle;
    private float animationTimer;
    private int currentFrameIndex;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();
        
        currentHealth = maxHealth;
        rb.freezeRotation = true;

        // If it's a flying enemy (like the Bird), turn off gravity
        if (isFlying)
        {
            rb.gravityScale = 0f;
        }

        // Find the player automatically in the scene
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            // Fallback search by script if tag is not set
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) playerObj = pc.gameObject;
        }

        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    void Update()
    {
        if (isDead)
        {
            AnimateSprite();
            return;
        }

        // Adjust gravity for flying enemies (falls when idle/hurt, flies when chasing)
        if (isFlying)
        {
            if (isChasing && !isHurt && !isAttacking)
            {
                rb.gravityScale = 0f;
            }
            else
            {
                rb.gravityScale = 1.5f; // Fall naturally when idle, hurt, or resting
            }
        }

        if (isHurt || isAttacking)
        {
            AnimateSprite();
            return;
        }

        // 1. Detect Player proximity
        CheckPlayerDetection();

        // 2. Decide movement and animations
        if (isChasing && playerTransform != null)
        {
            ChasePlayer();
        }
        else
        {
            // Player is outside detection range, stay Idle!
            // Lock horizontal movement, but let gravity pull the bird down vertically
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            SetState(AnimState.Idle);
        }

        AnimateSprite();
    }

    void CheckPlayerDetection()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Transition to chase if player gets in range
        if (distanceToPlayer <= detectionRange)
        {
            isChasing = true;

            // Attack if close enough and cooldown is ready
            if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime)
            {
                StartCoroutine(PerformAttack());
            }
        }
        else
        {
            isChasing = false;
        }
    }

    void PatrolMovement()
    {
        SetState(AnimState.Walk);

        // Turn around if we hit a wall in front of us
        Vector2 directionVec = new Vector2(walkDirection, 0f);
        RaycastHit2D wallHit = Physics2D.Raycast(transform.position, directionVec, wallCheckDistance, groundLayer);

        // Turn around if about to walk off a ledge (only for land enemies)
        bool isNearLedge = false;
        if (!isFlying && enemyCollider != null)
        {
            Vector3 ledgeCheckPos = transform.position + new Vector3(walkDirection * ledgeCheckDistance, -enemyCollider.bounds.extents.y - 0.1f, 0f);
            RaycastHit2D ledgeHit = Physics2D.Raycast(ledgeCheckPos, Vector2.down, 0.5f, groundLayer);
            isNearLedge = (ledgeHit.collider == null);
        }

        if (wallHit.collider != null || isNearLedge)
        {
            walkDirection = -walkDirection; // Reverse direction
        }

        // Apply movement velocity
        rb.linearVelocity = new Vector2(walkDirection * moveSpeed, rb.linearVelocity.y);

        // Orient sprite to facing direction
        transform.localScale = new Vector3(walkDirection, 1f, 1f);
    }

    void ChasePlayer()
    {
        // Determine horizontal distance to player
        float dirX = playerTransform.position.x - transform.position.x;
        float absDirX = Mathf.Abs(dirX);

        // 1. Flip Sprite direction ONLY if the player moves outside a small buffer zone (prevents rapid flipping)
        if (absDirX > 0.15f)
        {
            walkDirection = dirX > 0f ? 1 : -1;
            transform.localScale = new Vector3(walkDirection, 1f, 1f);
        }

        // 2. Stop horizontal movement if within attack range (prevents pushing through the player/vibrating)
        float targetVelocityX = 0f;
        if (absDirX > attackRange * 0.8f)
        {
            targetVelocityX = walkDirection * chaseSpeed;
            SetState(AnimState.Walk);
        }
        else
        {
            // Close enough, stop running and stand still
            targetVelocityX = 0f;
            SetState(AnimState.Idle);
        }
        
        // Move vertically towards player ONLY if flying
        float targetVelocityY = rb.linearVelocity.y;
        if (isFlying)
        {
            float dirY = playerTransform.position.y - transform.position.y;
            targetVelocityY = dirY > 0.05f ? chaseSpeed * 0.7f : (dirY < -0.05f ? -chaseSpeed * 0.7f : 0f);
        }

        rb.linearVelocity = new Vector2(targetVelocityX, targetVelocityY);
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;
        SetState(AnimState.Attack);

        // Stop moving during attack strike
        rb.linearVelocity = isFlying ? Vector2.zero : new Vector2(0f, rb.linearVelocity.y);

        // Deal damage if player is still in range at strike time
        yield return new WaitForSeconds(animationFrameRate * 2f); // Delay to sync with animation strike
        
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= attackRange)
            {
                // Damage the player
                PlayerController player = playerTransform.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.GetHurt();
                }
            }
        }

        // Finish animation cooldown
        int frameCount = (attackFrames != null && attackFrames.Length > 0) ? attackFrames.Length : 3;
        float remainingDuration = (animationFrameRate * frameCount) - (animationFrameRate * 2f);
        if (remainingDuration > 0) yield return new WaitForSeconds(remainingDuration);

        isAttacking = false;
    }

    // Called by Player's attack via SendMessage (or directly)
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        // Spawn spark particles (vibrant orange/yellow sparks)
        Color particleColor = new Color(1.0f, 0.6f, 0.0f);
        int particleCount = (currentHealth <= 0) ? 12 : 6;
        HurtParticles.Spawn(transform.position, particleColor, particleCount);

        if (currentHealth <= 0)
        {
            StartCoroutine(PerformDeath());
        }
        else
        {
            StartCoroutine(PerformHurt());
        }
    }

    IEnumerator PerformHurt()
    {
        isHurt = true;
        SetState(AnimState.Hurt);

        // Apply knockback away from player
        if (playerTransform != null)
        {
            float knockbackDir = transform.position.x > playerTransform.position.x ? 1f : -1f;
            rb.linearVelocity = new Vector2(knockbackDir * 4f, isFlying ? 1.5f : 3f);
        }

        // Flash Red!
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
        }

        int frameCount = (hurtFrames != null && hurtFrames.Length > 0) ? hurtFrames.Length : 2;
        yield return new WaitForSeconds(animationFrameRate * frameCount);

        // Restore default color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        isHurt = false;
    }

    IEnumerator PerformDeath()
    {
        isDead = true;
        SetState(AnimState.Death);

        // Disable physics collider so it doesn't block player or fall onto platforms
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        // Pop up slightly and fall down
        rb.gravityScale = 2f; // Force gravity on death so flying enemies also fall down!
        rb.linearVelocity = new Vector2(0f, 5f);

        // Flash red and white rapidly on death
        float deathFlashingDuration = 0.5f;
        float elapsed = 0f;
        while (elapsed < deathFlashingDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = (spriteRenderer.color == Color.white) ? Color.red : Color.white;
            }
            yield return new WaitForSeconds(0.05f);
            elapsed += 0.05f;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
        }

        int frameCount = (deathFrames != null && deathFrames.Length > 0) ? deathFrames.Length : 3;
        yield return new WaitForSeconds(animationFrameRate * frameCount);

        // Wait 0.5 seconds before destroying the enemy object
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }

    private void SetState(AnimState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            currentFrameIndex = 0;
            animationTimer = 0f;
        }
    }

    private void AnimateSprite()
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
                loop = false;
                break;
            case AnimState.Hurt:
                activeFrames = hurtFrames;
                loop = false;
                break;
            case AnimState.Death:
                activeFrames = deathFrames;
                loop = false;
                break;
        }

        if (activeFrames == null || activeFrames.Length == 0) return;

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
                    currentFrameIndex = activeFrames.Length - 1;
                }
            }
        }

        spriteRenderer.sprite = activeFrames[currentFrameIndex];
    }

    // Trigger damage if the player simply collides with them (like classic platformers)
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                player.GetHurt();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw the detection range circle in yellow
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw the attack range circle in red
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
