using System.Diagnostics;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class Player : MonoBehaviour
{
    private Vector3 spawnPosition;

    //RigidBody
    private Rigidbody2D rb;
    private Animator animtr;

    //Key Input
    private float xInput;

    //where character facing
    private int facingDirection = 1;
    private bool facingRight = true;

    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;
    [Header("Movement")]
    [SerializeField] private float dashDuration;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashCD;

    [Header("Collision info")]
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private LayerMask whatIsGround;

    private bool isGrounded;
    [Header("Wall Collision")]
    [SerializeField] private float wallCheckWidth = 0.5f;  // Width of the box
    [SerializeField] private float wallCheckHeight = 1f;   // Height of the box
    [SerializeField] private LayerMask whatIsWall;

    [Header("Ledge Detection")]
    [SerializeField] private float topCheckY = 0.5f; 
    [SerializeField] private float topCheckDist = 0.6f;
    // NEW: How much force to lift the player over the ledge
    [SerializeField] private float ledgeClimbForce = 5f;
    
    private bool isTouchingLedge; 
    private bool isTouchingWall;

    // Offsets for the box origin
    [SerializeField] private float offsetX = 0.5f;
    [SerializeField] private float offsetY = 0f; 
    [SerializeField] private float wallJumpCD = 0.2f;
    private float wallJumpTimer = 0f;

    [Header("GUI")]
    [SerializeField] private Image dashReadyIndicator;
    [SerializeField] private Image wallJumpReadyIndicator;

    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //rb is Rigidbody2D for Player
        rb = GetComponent<Rigidbody2D>();
        animtr = GetComponentInChildren<Animator>();
        spawnPosition = transform.position;  // remember where you began
    }

    void Update()
    {
        CheckInput();
        Movement();
        CollisionChecks();
        AnimatorControllers();
        FlipController();
        dashTime -= Time.deltaTime;
        wallJumpTimer -= Time.deltaTime;
        if (Input.GetButtonDown("Sprint"))
        {
            if (dashTime < -1*dashCD)
            {
                dashTime = dashDuration; 
            }
        }
        dashReadyIndicator.enabled = (dashTime < -dashCD);
        wallJumpReadyIndicator.enabled = (wallJumpTimer <= 0f);
    }

    private void CheckInput()
    {
        if (Input.GetButtonDown("Jump"))
        {        
            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
            else if (isTouchingWall && wallJumpTimer <= 0f)
            {
                // Push the player away from the wall a little
                rb.linearVelocity = new Vector2(-facingDirection * moveSpeed * 0.75f, jumpForce);
                wallJumpTimer = wallJumpCD;
            }
        }
        //horizontal input
        xInput = Input.GetAxisRaw("Horizontal");
    }
    private void AnimatorControllers()
    {
        bool isMoving = rb.linearVelocity.x != 0;
        animtr.SetFloat("yVelocity", rb.linearVelocity.y);
        animtr.SetBool("isMoving", isMoving);
        animtr.SetBool("isGrounded", isGrounded);
        animtr.SetBool("isDashing", dashTime > 0);
        animtr.SetBool("isTouchingWall", isTouchingWall); 
        animtr.SetBool("isLedgeClimbing", isTouchingLedge);
    }
    private void Movement()
    {
        if (dashTime > 0)
        {
            rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0);
        }
        // NEW: Ledge Climb Logic
        // If we are at a ledge AND holding the key towards the wall
        else if (isTouchingLedge && xInput == facingDirection)
        {
             // We apply movement speed (to go onto the platform) 
             // AND a small upward force to lift the collider over the corner lip
             rb.linearVelocity = new Vector2(facingDirection * moveSpeed, ledgeClimbForce);
        }
        else
        {
            rb.linearVelocity = new Vector2(xInput * moveSpeed, rb.linearVelocity.y);
        }
    }
    private void CollisionChecks()
    {
        // 1. Ground Check
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, whatIsGround);

        // 2. Wall Check (Body)
        Vector2 wallCheckCenter = new Vector2(transform.position.x + facingDirection * offsetX, transform.position.y + offsetY);
        Collider2D wallCollider = Physics2D.OverlapBox(wallCheckCenter, new Vector2(wallCheckWidth, wallCheckHeight), 0f, whatIsWall);
        
        // 3. Wall Check (Head)
        Vector2 topCheckPos = new Vector2(transform.position.x, transform.position.y + topCheckY);
        bool isTopTouching = Physics2D.Raycast(topCheckPos, Vector2.right * facingDirection, topCheckDist, whatIsWall);

        // LOGIC: 
        // If Body hits AND Head hits = We are on a tall wall (Climb).
        isTouchingWall = (wallCollider != null) && isTopTouching;

        // If Body hits BUT Head misses = We found a Ledge/Corner.
        isTouchingLedge = (wallCollider != null) && !isTopTouching;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red; 
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
        
        Gizmos.color = Color.blue; 
        Vector3 boxCenter = new Vector3(transform.position.x + facingDirection * offsetX, transform.position.y + offsetY, transform.position.z);
        Gizmos.DrawWireCube(boxCenter, new Vector3(wallCheckWidth, wallCheckHeight, 0f));

        // === NEW CODE START: Visualize the Top Check ===
        Gizmos.color = Color.yellow;
        Vector3 topCheckPos = new Vector3(transform.position.x, transform.position.y + topCheckY, transform.position.z);
        Gizmos.DrawLine(topCheckPos, topCheckPos + Vector3.right * facingDirection * topCheckDist);
        // === NEW CODE END ===
    }
    private void Flip()
    {
        facingDirection = facingDirection * -1;
        facingRight = !facingRight;
        transform.Rotate(0, 180, 0);
    }
    private void FlipController()
    {
        if(rb.linearVelocity.x > 0 && !facingRight)
        {
            Flip();
        }
        else if(rb.linearVelocity.x < 0 && facingRight)
        {
            Flip();
        }
    }

    private void Respawn()
    {
        rb.linearVelocity = Vector2.zero;
        transform.position = spawnPosition;  // Move to spawn point
    }

    // Detect trigger for kill zone or fall into void
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("KillZone"))
        {
            Respawn();
        }
        else if (other.CompareTag("Checkpoint"))
        {
            spawnPosition = other.transform.position;
        }
    }
}
