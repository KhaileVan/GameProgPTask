using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class AllyController : MonoBehaviour
{
    [Header("Follow Settings")]
    public float followDistance = 1.3f;
    public float moveSpeed = 5.5f;
    public float jumpForce = 9.5f;
    public float recruitmentProximity = 2.5f; // Distance before the ally joins the player

    [Header("Combat Settings")]
    public float attackRange = 1.2f;
    public float attackCooldown = 1.2f;
    public LayerMask enemyLayer;

    [Header("Ground Check")]
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Animations (Optional)")]
    public Sprite[] idleFrames;
    public Sprite[] walkFrames;
    public Sprite[] attackFrames;
    public float animationFrameRate = 0.1f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D allyCollider;
    private Transform targetPlayer;

    // Breadcrumb trail settings
    private System.Collections.Generic.List<Vector3> breadcrumbs = new System.Collections.Generic.List<Vector3>();
    private float breadcrumbSpacing = 0.25f;
    private int maxBreadcrumbs = 100;
    private Vector3 lastBreadcrumbTarget;
    private float breadcrumbStuckTimer = 0f;

    private bool isRecruited = false;
    private bool isAttacking = false;
    private bool isGrounded = false;
    private bool hasRescueJumped = false;
    private float nextAttackTime = 0f;

    private enum AnimState { Idle, Walk, Attack }
    private AnimState currentState = AnimState.Idle;
    private float animationTimer;
    private int currentFrameIndex;

    public bool IsRecruited => isRecruited;

    void Awake()
    {
        // If this is a newly loaded AllyController, check if another recruited AllyController already exists
        AllyController[] allies = FindObjectsByType<AllyController>(FindObjectsSortMode.None);
        foreach (var ally in allies)
        {
            if (ally != this && ally.isRecruited)
            {
                // Another recruited ally has already been carried over from a previous scene.
                // Destroy this duplicate.
                Destroy(gameObject);
                return;
            }
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        allyCollider = GetComponent<Collider2D>();

        rb.freezeRotation = true;
        
        // Force the collider to NOT be a trigger so it physically stands on platforms
        if (allyCollider != null)
        {
            allyCollider.isTrigger = false;
        }
        
        // Set gravity to 0 initially so it doesn't fall off-screen before recruitment
        rb.gravityScale = 0f; 
    }

    void Update()
    {
        if (isRecruited && targetPlayer == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                PlayerController pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) playerObj = pc.gameObject;
            }
            if (playerObj != null)
            {
                targetPlayer = playerObj.transform;
                // Teleport to player with an offset to avoid falling
                transform.position = targetPlayer.position + new Vector3(-1f, 0.5f, 0f);
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
                
                // Re-setup IgnoreCollision
                Collider2D playerCol = playerObj.GetComponent<Collider2D>();
                if (playerCol != null && allyCollider != null)
                {
                    Physics2D.IgnoreCollision(allyCollider, playerCol, true);
                }
            }
            else
            {
                // Stand still if player is missing
                if (rb != null)
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
                UpdateAnimationState(0f);
                AnimateSprite();
                return;
            }
        }

        if (!isRecruited)
        {
            // Find player and check distance for proximity recruitment
            if (targetPlayer == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj == null)
                {
                    PlayerController pc = FindFirstObjectByType<PlayerController>();
                    if (pc != null) playerObj = pc.gameObject;
                }
                if (playerObj != null)
                {
                    targetPlayer = playerObj.transform;
                }
            }

            if (targetPlayer != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
                if (distanceToPlayer <= recruitmentProximity)
                {
                    Recruit(targetPlayer.gameObject);
                }
            }

            UpdateAnimationState(0f);
            AnimateSprite();
            return;
        }

        if (targetPlayer != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
            if (distanceToPlayer > 15f)
            {
                // Teleport back near player if we get too far (e.g. stuck on geometry off-screen)
                transform.position = targetPlayer.position + new Vector3(-1f, 0.5f, 0f);
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
                breadcrumbs.Clear();
            }
            else if (distanceToPlayer > 8f)
            {
                // Clear trail to target player directly and speed up catching up
                breadcrumbs.Clear();
            }
            else
            {
                // Record player position breadcrumbs when they are grounded on a platform
                PlayerController playerCtrl = targetPlayer.GetComponent<PlayerController>();
                if (playerCtrl != null && playerCtrl.IsGrounded)
                {
                    Vector3 playerPos = targetPlayer.position;
                    if (breadcrumbs.Count == 0)
                    {
                        breadcrumbs.Add(playerPos);
                    }
                    else
                    {
                        float distFromLast = Vector3.Distance(playerPos, breadcrumbs[breadcrumbs.Count - 1]);
                        if (distFromLast >= breadcrumbSpacing)
                        {
                            breadcrumbs.Add(playerPos);
                            if (breadcrumbs.Count > maxBreadcrumbs)
                            {
                                breadcrumbs.RemoveAt(0);
                            }
                        }
                    }
                }
            }
        }

        // Auto Attack check
        if (Time.time >= nextAttackTime && !isAttacking)
        {
            DetectAndAttackEnemies();
        }

        // Calculate direction to current target (breadcrumb or player)
        Vector3 currentTarget = targetPlayer.position;
        if (breadcrumbs.Count > 0)
        {
            currentTarget = breadcrumbs[0];
        }
        float dirX = currentTarget.x - transform.position.x;
        
        // Flip sprite to face movement or player
        if (Mathf.Abs(dirX) > 0.1f)
        {
            transform.localScale = new Vector3(dirX > 0 ? 1 : -1, 1, 1);
        }

        UpdateAnimationState(dirX);
        AnimateSprite();
    }

    void FixedUpdate()
    {
        if (!isRecruited || targetPlayer == null)
        {
            if (!isRecruited && rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            return;
        }

        CheckGrounded();

        if (isGrounded)
        {
            hasRescueJumped = false;
        }

        // Stuck on breadcrumb check
        if (breadcrumbs.Count > 0)
        {
            Vector3 currentBreadcrumb = breadcrumbs[0];
            if (Vector3.Distance(currentBreadcrumb, lastBreadcrumbTarget) < 0.05f)
            {
                breadcrumbStuckTimer += Time.fixedDeltaTime;
                if (breadcrumbStuckTimer > 1.5f)
                {
                    // Discard stuck breadcrumb
                    breadcrumbs.RemoveAt(0);
                    breadcrumbStuckTimer = 0f;
                }
            }
            else
            {
                lastBreadcrumbTarget = currentBreadcrumb;
                breadcrumbStuckTimer = 0f;
            }
        }
        else
        {
            breadcrumbStuckTimer = 0f;
        }

        // Consume breadcrumbs we are close to
        if (breadcrumbs.Count > 0)
        {
            int targetIdx = 0;
            while (targetIdx < breadcrumbs.Count)
            {
                float dist = Vector3.Distance(transform.position, breadcrumbs[targetIdx]);
                if (dist > 0.4f)
                {
                    break;
                }
                targetIdx++;
            }

            if (targetIdx > 0)
            {
                breadcrumbs.RemoveRange(0, Mathf.Min(targetIdx, breadcrumbs.Count));
            }
        }

        // Determine current target (either the next breadcrumb or the player directly if trail is empty)
        Vector3 currentTarget = targetPlayer.position;
        if (breadcrumbs.Count > 0)
        {
            currentTarget = breadcrumbs[0];
        }

        float dirToTargetX = currentTarget.x - transform.position.x;
        float absDirX = Mathf.Abs(dirToTargetX);

        float targetVelocityX = rb.linearVelocity.x;

        // If targeting a breadcrumb, use a tighter follow distance to reach it precisely
        float currentFollowDistance = (breadcrumbs.Count > 0) ? 0.3f : followDistance;

        if (absDirX > currentFollowDistance)
        {
            float direction = dirToTargetX > 0 ? 1f : -1f;
            targetVelocityX = direction * moveSpeed;
        }
        else
        {
            targetVelocityX = 0f;
        }

        // Heuristics for jumping:
        // 1. Jump if blocked by an obstacle (horizontal speed is low but we are trying to move)
        bool isBlocked = absDirX > currentFollowDistance && Mathf.Abs(rb.linearVelocity.x) < 0.2f;

        // 2. Jump if the target is significantly higher and we are close horizontally
        bool targetIsAbove = (currentTarget.y - transform.position.y > 0.6f) && (absDirX < 2.5f);

        // 3. Water Avoidance Logic (raycasting ahead and down)
        bool detectsWater = false;

        // Start raycasts below the Ally Cat's collider to avoid hitting itself
        Vector3 rayStart = transform.position;
        if (allyCollider != null)
        {
            rayStart = new Vector3(transform.position.x, allyCollider.bounds.min.y - 0.05f, transform.position.z);
        }

        // Ray 1: Straight down (checks if standing on/above water)
        RaycastHit2D downHit = Physics2D.Raycast(rayStart, Vector2.down, 2.0f);
        if (downHit.collider != null && downHit.collider.gameObject.name.ToLower().Contains("water"))
        {
            detectsWater = true;
        }

        // Ray 2: Diagonally forward and down (checks if about to walk into water)
        float facingDirX = transform.localScale.x;
        Vector2 diagonalDir = new Vector2(facingDirX, -0.8f).normalized;
        RaycastHit2D diagonalHit = Physics2D.Raycast(rayStart, diagonalDir, 2.0f);
        if (diagonalHit.collider != null && diagonalHit.collider.gameObject.name.ToLower().Contains("water"))
        {
            detectsWater = true;
        }

        // 4. Close Contact Water Check (forces jump if already standing in/touching water)
        bool isTouchingWater = false;
        RaycastHit2D waterTouchHit = Physics2D.Raycast(rayStart, Vector2.down, 0.3f);
        if (waterTouchHit.collider != null && waterTouchHit.collider.gameObject.name.ToLower().Contains("water"))
        {
            isTouchingWater = true;
        }

        // Apply velocity/jump modifications based on heuristics
        if (isGrounded && (isBlocked || targetIsAbove || detectsWater) && !isAttacking)
        {
            // If jumping over water, apply a slight horizontal push
            float forceMultiplier = detectsWater ? 1.2f : 1.0f;
            rb.linearVelocity = new Vector2(targetVelocityX * forceMultiplier, jumpForce);
        }
        else if (isTouchingWater && !isAttacking)
        {
            // Force jump to escape water (even if not grounded!)
            float jumpDirectionX = (currentTarget.x > transform.position.x) ? 1f : -1f;
            rb.linearVelocity = new Vector2(jumpDirectionX * moveSpeed * 1.2f, jumpForce * 1.1f);
            
            // Spawn splash sparkles
            HurtParticles.Spawn(transform.position, new Color(0.2f, 0.9f, 1f), 4);
        }
        else if (!isGrounded && rb.linearVelocity.y < 0f && detectsWater && !hasRescueJumped && !isAttacking)
        {
            // Mid-air rescue jump back towards the target's direction!
            float rescueDirX = (currentTarget.x > transform.position.x) ? 1f : -1f;
            rb.linearVelocity = new Vector2(rescueDirX * moveSpeed * 1.3f, jumpForce * 1.1f);
            hasRescueJumped = true;

            // Spawn cyan bubble sparkles
            HurtParticles.Spawn(transform.position, new Color(0.2f, 0.9f, 1f), 6);
        }
        else
        {
            rb.linearVelocity = new Vector2(targetVelocityX, rb.linearVelocity.y);
        }

        // Apply extra fall multiplier just like player for snapping jumps
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (2.5f - 1) * Time.fixedDeltaTime;
        }
    }

    void CheckGrounded()
    {
        Vector3 checkPos = transform.position;
        if (allyCollider != null)
        {
            checkPos = new Vector3(transform.position.x, allyCollider.bounds.min.y, transform.position.z);
        }

        Collider2D[] colliders = Physics2D.OverlapCircleAll(checkPos, groundCheckRadius, groundLayer);
        isGrounded = false;
        foreach (var col in colliders)
        {
            if (col.gameObject != gameObject)
            {
                if (col.gameObject.name.ToLower().Contains("platform"))
                {
                    isGrounded = true;
                    break;
                }
            }
        }
    }

    void DetectAndAttackEnemies()
    {
        // Scan for nearby colliders
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);
        foreach (var col in hits)
        {
            if (col.gameObject == gameObject || col.gameObject.CompareTag("Player")) continue;

            // Check if it's an enemy
            EnemyController enemy = col.GetComponent<EnemyController>();
            bool isEnemyByName = col.gameObject.name.ToLower().Contains("dog") ||
                                 col.gameObject.name.ToLower().Contains("bird") ||
                                 col.gameObject.name.ToLower().Contains("rat") ||
                                 col.gameObject.name.ToLower().Contains("enemy");

            if (enemy != null || isEnemyByName || ((1 << col.gameObject.layer) & enemyLayer) != 0)
            {
                StartCoroutine(PerformAttack(col.gameObject));
                break; // Attack one enemy at a time
            }
        }
    }

    IEnumerator PerformAttack(GameObject enemyTarget)
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;
        SetState(AnimState.Attack);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayAttack();

        // Stop moving during attack
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // Position of attack (slightly offset in facing direction)
        float directionX = transform.localScale.x;
        Vector3 attackPos = transform.position + new Vector3(directionX * 0.7f, 0.1f, 0f);

        // Spawn crescent slash visual
        GameObject slashObj = new GameObject("AllySlashEffect");
        slashObj.transform.position = attackPos;
        slashObj.transform.localScale = new Vector3(directionX, 1f, 1f);
        SlashEffect slash = slashObj.AddComponent<SlashEffect>();
        slash.Initialize(directionX > 0);

        // Wait brief moment to sync with animation strike
        yield return new WaitForSeconds(0.1f);

        if (enemyTarget != null)
        {
            // Double check range before applying damage
            float dist = Vector3.Distance(transform.position, enemyTarget.transform.position);
            if (dist <= attackRange + 0.5f)
            {
                enemyTarget.SendMessage("TakeDamage", 1, SendMessageOptions.DontRequireReceiver);
            }
        }

        int frameCount = (attackFrames != null && attackFrames.Length > 0) ? attackFrames.Length : 3;
        yield return new WaitForSeconds(animationFrameRate * frameCount);

        isAttacking = false;
    }

    private void Recruit(GameObject player)
    {
        isRecruited = true;
        targetPlayer = player.transform;

        // Enable gravity now that it is following the player
        rb.gravityScale = 2.5f;

        // Ignore physical collision with the player so we don't push each other off platforms
        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (playerCol != null && allyCollider != null)
        {
            Physics2D.IgnoreCollision(allyCollider, playerCol, true);
        }

        // Spawn happy green recruitment particles!
        HurtParticles.Spawn(transform.position, Color.green, 10);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isRecruited && (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<PlayerController>() != null))
        {
            Recruit(collision.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isRecruited && (other.gameObject.CompareTag("Player") || other.gameObject.GetComponent<PlayerController>() != null))
        {
            Recruit(other.gameObject);
        }
    }

    void UpdateAnimationState(float dirX)
    {
        if (isAttacking)
        {
            SetState(AnimState.Attack);
        }
        else if (Mathf.Abs(dirX) > followDistance)
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

    private void OnDrawGizmosSelected()
    {
        // Draw the ground check circle in red in the Editor Scene view
        Gizmos.color = Color.red;
        Vector3 checkPos = transform.position;
        if (allyCollider != null)
        {
            checkPos = new Vector3(transform.position.x, allyCollider.bounds.min.y, transform.position.z);
        }
        Gizmos.DrawWireSphere(checkPos, groundCheckRadius);

        // Draw attack range circle in green
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw recruitment proximity circle in yellow (if not recruited)
        if (!isRecruited)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, recruitmentProximity);
        }
    }
}
