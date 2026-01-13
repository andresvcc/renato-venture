using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.SceneManagement;

public class CharacterController2D : MonoBehaviour
{
	[SerializeField] private float m_JumpForce = 400f;                          // Amount of force added when the player jumps.
	[Range(0, .3f)][SerializeField] private float m_MovementSmoothing = .05f;   // How much to smooth out the movement
	[SerializeField] private bool m_AirControl = false;                         // Whether or not a player can steer while jumping;
	[SerializeField] private LayerMask m_WhatIsGround;                          // A mask determining what is ground to the character
	[SerializeField] private Transform m_GroundCheck;                           // A position marking where to check if the player is grounded.
	[SerializeField] private Transform m_WallCheck;                             //Posicion que controla si el personaje toca una pared

	const float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded
	private bool m_Grounded;            // Whether or not the player is grounded.
	private Rigidbody2D m_Rigidbody2D;
	private bool m_FacingRight = true;  // For determining which way the player is currently facing.
	private Vector3 velocity = Vector3.zero;
	private float limitFallSpeed = 25f; // Limit fall speed

	public bool canDoubleJump = true; //If player can double jump
	[SerializeField] private float m_DashForce = 25f;
	private bool canDash = true;
	private bool isDashing = false; //If player is dashing
	private bool m_IsWall = false; //If there is a wall in front of the player
	private bool isWallSliding = false; //If player is sliding in a wall
	private bool oldWallSlidding = false; //If player is sliding in a wall in the previous frame
	private float prevVelocityX = 0f;
	private bool canCheck = false; //For check if player is wallsliding

	public float life = 10f; //Life of the player
	public bool invincible = false; //If player can die
	private bool canMove = true; //If player can move

	private Animator animator;
	public ParticleSystem particleJumpUp; //Trail particles
	public ParticleSystem particleJumpDown; //Explosion particles

	private float jumpWallStartX = 0;
	private float jumpWallDistX = 0; //Distance between player and wall
	private bool limitVelOnWallJump = false; //For limit wall jump distance with low fps


	// natation
	[Header("Water / Swimming")]
	public bool inWater = false;
	public float waterGravityScale = 0.5f;      // gravityScale while swimming (smaller)
	public float normalGravityScale = 1f;       // saved default (will be set in Awake)
	public float waterDrag = 3f;                // drag in water
	public float normalDrag = 0f;               // saved default
	public float swimHorizontalSpeed = 6f;      // horizontal swim speed (tunable)
	public float swimVerticalSpeed = 4f;        // vertical swim speed (tunable)
	public float swimSmooth = 0.12f;            // smoothing when setting swim velocity
	public float exitWaterRestoreTime = 0.25f;  // smooth restore time for gravity
	public float waterExitJumpForce = 1800f;     // force to jump out near surface
	public float waterSurfaceExitOffset = 0.2f; // how far below surface can still exit
	public float waterSurfaceSwimLimitOffset = 0.5f; // clamp swim-up to stay below surface
	public float waterSurfaceRearmOffset = 1.2f; // how far below surface to rearm exit jump
	public float waterExitInputBufferTime = 0.2f; // time window to queue exit jump
	private Coroutine exitWaterCoroutine = null;
	private float waterSurfaceY = float.NegativeInfinity;
	private bool hasWaterSurface = false;
	private bool waterExitJumpConsumed = false;
	private float waterExitBufferedUntil = 0f;
	private bool waterEntrySinkActive = false;

	//colliders
	public  CapsuleCollider2D horizontalCapsuleCollider2D;
    public CapsuleCollider2D verticalCapsuleCollider2D;

    [Header("Events")]
	[Space]

	public UnityEvent OnFallEvent;
	public UnityEvent OnLandEvent;

	[System.Serializable]
	public class BoolEvent : UnityEvent<bool> { }

	private void Awake()
	{
		m_Rigidbody2D = GetComponent<Rigidbody2D>();
		animator = GetComponent<Animator>();


		normalGravityScale = m_Rigidbody2D.gravityScale;
		normalDrag = m_Rigidbody2D.linearDamping;


		if (OnFallEvent == null)
			OnFallEvent = new UnityEvent();

		if (OnLandEvent == null)
			OnLandEvent = new UnityEvent();




	}


	private void FixedUpdate()
	{
		bool wasGrounded = m_Grounded;
		m_Grounded = false;

		// The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
		// This can be done using layers instead but Sample Assets will not overwrite your project settings.
		Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
		for (int i = 0; i < colliders.Length; i++)
		{
			if (colliders[i].gameObject != gameObject)
				m_Grounded = true;
			if (!wasGrounded)
			{
				OnLandEvent.Invoke();
				if (!m_IsWall && !isDashing)
					particleJumpDown.Play();
				canDoubleJump = true;
				if (m_Rigidbody2D.linearVelocity.y < 0f)
					limitVelOnWallJump = false;
			}
		}

		m_IsWall = false;

		if (!m_Grounded)
		{
			OnFallEvent.Invoke();
			Collider2D[] collidersWall = Physics2D.OverlapCircleAll(m_WallCheck.position, k_GroundedRadius, m_WhatIsGround);
			for (int i = 0; i < collidersWall.Length; i++)
			{
				if (collidersWall[i].gameObject != null)
				{
					isDashing = false;
					m_IsWall = true;
				}
			}
			prevVelocityX = m_Rigidbody2D.linearVelocity.x;
		}

		if (limitVelOnWallJump)
		{
			if (m_Rigidbody2D.linearVelocity.y < -0.5f)
				limitVelOnWallJump = false;
			jumpWallDistX = (jumpWallStartX - transform.position.x) * transform.localScale.x;
			if (jumpWallDistX < -0.5f && jumpWallDistX > -1f)
			{
				canMove = true;
			}
			else if (jumpWallDistX < -1f && jumpWallDistX >= -2f)
			{
				canMove = true;
				m_Rigidbody2D.linearVelocity = new Vector2(10f * transform.localScale.x, m_Rigidbody2D.linearVelocity.y);
			}
			else if (jumpWallDistX < -2f)
			{
				limitVelOnWallJump = false;
				m_Rigidbody2D.linearVelocity = new Vector2(0, m_Rigidbody2D.linearVelocity.y);
			}
			else if (jumpWallDistX > 0)
			{
				limitVelOnWallJump = false;
				m_Rigidbody2D.linearVelocity = new Vector2(0, m_Rigidbody2D.linearVelocity.y);
			}
		}
	}


    public void Move(float move, bool jump, bool dash, float verticalInput, bool jumpHeld, bool axisUp)
    {


        if (inWater)
        {
			if (waterEntrySinkActive && hasWaterSurface)
			{
				if (IsBelowWaterSurfaceEntrySink())
				{
					waterEntrySinkActive = false;
				}
				else
				{
					verticalInput = -1f;
					waterExitBufferedUntil = 0f;
				}
			}

			if (waterExitJumpConsumed && IsBelowWaterSurfaceRearm())
			{
				waterExitJumpConsumed = false;
			}

			bool wantsWaterExit = !waterEntrySinkActive && (jumpHeld || jump);
			if (wantsWaterExit)
			{
				waterExitBufferedUntil = Time.time + waterExitInputBufferTime;
			}

			bool hasBufferedExit = !waterEntrySinkActive && Time.time <= waterExitBufferedUntil;
			if (!waterEntrySinkActive && !wantsWaterExit && axisUp && IsAtWaterSurfaceLimit())
			{
				verticalInput = 0f;
				if (m_Rigidbody2D.linearVelocity.y > 0f)
					m_Rigidbody2D.linearVelocity = new Vector2(m_Rigidbody2D.linearVelocity.x, 0f);
			}

			bool isApproachingSurface = verticalInput > 0f || m_Rigidbody2D.linearVelocity.y >= 0f;
			if (hasBufferedExit && !waterExitJumpConsumed && IsAtWaterSurface() && isApproachingSurface)
			{
				WaterExitJump();
				waterExitJumpConsumed = true;
			}

			SwimMove(move, verticalInput, jump, dash);
			return;
        }
		waterExitJumpConsumed = false;
        if (canMove)
		{
			if (dash && canDash && !isWallSliding)
			{
				//m_Rigidbody2D.AddForce(new Vector2(transform.localScale.x * m_DashForce, 0f));
				StartCoroutine(DashCooldown());
			}
			// If crouching, check to see if the character can stand up
			if (isDashing)
			{
				m_Rigidbody2D.linearVelocity = new Vector2(transform.localScale.x * m_DashForce, 0);
			}
			//only control the player if grounded or airControl is turned on
			else if (m_Grounded || m_AirControl)
			{
				if (m_Rigidbody2D.linearVelocity.y < -limitFallSpeed)
					m_Rigidbody2D.linearVelocity = new Vector2(m_Rigidbody2D.linearVelocity.x, -limitFallSpeed);
				// Move the character by finding the target velocity
				Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.linearVelocity.y);
				// And then smoothing it out and applying it to the character
				m_Rigidbody2D.linearVelocity = Vector3.SmoothDamp(m_Rigidbody2D.linearVelocity, targetVelocity, ref velocity, m_MovementSmoothing);

				// If the input is moving the player right and the player is facing left...
				if (move > 0 && !m_FacingRight && !isWallSliding)
				{
					// ... flip the player.
					Flip();
				}
				// Otherwise if the input is moving the player left and the player is facing right...
				else if (move < 0 && m_FacingRight && !isWallSliding)
				{
					// ... flip the player.
					Flip();
				}
			}
			// If the player should jump...
			if (m_Grounded && jump)
			{
				// Add a vertical force to the player.
				animator.SetBool("IsJumping", true);
				animator.SetBool("JumpUp", true);
				m_Grounded = false;
				m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
				canDoubleJump = true;
				particleJumpDown.Play();
				particleJumpUp.Play();
			}
			else if (!m_Grounded && jump && canDoubleJump && !isWallSliding)
			{
				canDoubleJump = false;
				m_Rigidbody2D.linearVelocity = new Vector2(m_Rigidbody2D.linearVelocity.x, 0);
				m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce / 1.2f));
				animator.SetBool("IsDoubleJumping", true);
			}

			else if (m_IsWall && !m_Grounded)
			{
				if (!oldWallSlidding && m_Rigidbody2D.linearVelocity.y < 0 || isDashing)
				{
					isWallSliding = true;
					m_WallCheck.localPosition = new Vector3(-m_WallCheck.localPosition.x, m_WallCheck.localPosition.y, 0);
					Flip();
					StartCoroutine(WaitToCheck(0.1f));
					canDoubleJump = true;
					animator.SetBool("IsWallSliding", true);
				}
				isDashing = false;

				if (isWallSliding)
				{
					if (move * transform.localScale.x > 0.1f)
					{
						StartCoroutine(WaitToEndSliding());
					}
					else
					{
						oldWallSlidding = true;
						m_Rigidbody2D.linearVelocity = new Vector2(-transform.localScale.x * 2, -5);
					}
				}

				if (jump && isWallSliding)
				{
					animator.SetBool("IsJumping", true);
					animator.SetBool("JumpUp", true);
					m_Rigidbody2D.linearVelocity = new Vector2(0f, 0f);
					m_Rigidbody2D.AddForce(new Vector2(transform.localScale.x * m_JumpForce * 1.2f, m_JumpForce));
					jumpWallStartX = transform.position.x;
					limitVelOnWallJump = true;
					canDoubleJump = true;
					isWallSliding = false;
					animator.SetBool("IsWallSliding", false);
					oldWallSlidding = false;
					m_WallCheck.localPosition = new Vector3(Mathf.Abs(m_WallCheck.localPosition.x), m_WallCheck.localPosition.y, 0);
					canMove = false;
				}
				else if (dash && canDash)
				{
					isWallSliding = false;
					animator.SetBool("IsWallSliding", false);
					oldWallSlidding = false;
					m_WallCheck.localPosition = new Vector3(Mathf.Abs(m_WallCheck.localPosition.x), m_WallCheck.localPosition.y, 0);
					canDoubleJump = true;
					StartCoroutine(DashCooldown());
				}
			}
			else if (isWallSliding && !m_IsWall && canCheck)
			{
				isWallSliding = false;
				animator.SetBool("IsWallSliding", false);
				oldWallSlidding = false;
				m_WallCheck.localPosition = new Vector3(Mathf.Abs(m_WallCheck.localPosition.x), m_WallCheck.localPosition.y, 0);
				canDoubleJump = true;
			}
		}
	}


	private void Flip()
	{
		// Switch the way the player is labelled as facing.
		m_FacingRight = !m_FacingRight;

		// Multiply the player's x local scale by -1.
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}

	public void ApplyDamage(float damage, Vector3 position)
	{
		if (!invincible)
		{
			animator.SetBool("Hit", true);
			life -= damage;
			Vector2 damageDir = Vector3.Normalize(transform.position - position) * 40f;
			m_Rigidbody2D.linearVelocity = Vector2.zero;
			m_Rigidbody2D.AddForce(damageDir * 10);
			if (life <= 0)
			{
				StartCoroutine(WaitToDead());
			}
			else
			{
				StartCoroutine(Stun(0.25f));
				StartCoroutine(MakeInvincible(1f));
			}
		}
	}

	IEnumerator DashCooldown()
	{
		animator.SetBool("IsDashing", true);
		isDashing = true;
		canDash = false;
		yield return new WaitForSeconds(0.1f);
		isDashing = false;
		yield return new WaitForSeconds(0.5f);
		canDash = true;
	}

	IEnumerator Stun(float time)
	{
		canMove = false;
		yield return new WaitForSeconds(time);
		canMove = true;
	}
	IEnumerator MakeInvincible(float time)
	{
		invincible = true;
		yield return new WaitForSeconds(time);
		invincible = false;
	}
	IEnumerator WaitToMove(float time)
	{
		canMove = false;
		yield return new WaitForSeconds(time);
		canMove = true;
	}

	IEnumerator WaitToCheck(float time)
	{
		canCheck = false;
		yield return new WaitForSeconds(time);
		canCheck = true;
	}

	IEnumerator WaitToEndSliding()
	{
		yield return new WaitForSeconds(0.1f);
		canDoubleJump = true;
		isWallSliding = false;
		animator.SetBool("IsWallSliding", false);
		oldWallSlidding = false;
		m_WallCheck.localPosition = new Vector3(Mathf.Abs(m_WallCheck.localPosition.x), m_WallCheck.localPosition.y, 0);
	}

	IEnumerator WaitToDead()
	{
		animator.SetBool("IsDead", true);
		canMove = false;
		invincible = true;
		GetComponent<Attack>().enabled = false;
		yield return new WaitForSeconds(0.4f);
		m_Rigidbody2D.linearVelocity = new Vector2(0, m_Rigidbody2D.linearVelocity.y);
		yield return new WaitForSeconds(1.1f);
		SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
	}






    private void SwimMove(float move, float vertical, bool jump, bool dash)
    {
        float targetVX = move * swimHorizontalSpeed;
        float targetVY = vertical * swimVerticalSpeed;


        if (Mathf.Abs(targetVX) < 0.05f) targetVX = 0f;
        if (Mathf.Abs(targetVY) < 0.05f) targetVY = 0f;

        Vector2 targetVelocity = new Vector2(targetVX, targetVY);
        m_Rigidbody2D.linearVelocity = Vector2.Lerp(
            m_Rigidbody2D.linearVelocity,
            targetVelocity,
            Time.fixedDeltaTime * 5f
        );

        if (move > 0 && !m_FacingRight) Flip();
        else if (move < 0 && m_FacingRight) Flip();

        animator.SetFloat("SwimVertical", m_Rigidbody2D.linearVelocity.y);
        animator.SetFloat("SwimHorizontal", m_Rigidbody2D.linearVelocity.x);

        animator.SetFloat("SwimInputX", Mathf.Abs(move));
        animator.SetFloat("SwimInputY", Mathf.Abs(vertical));

    }

	private float GetActiveColliderTopY()
	{
		if (horizontalCapsuleCollider2D != null && horizontalCapsuleCollider2D.enabled)
			return horizontalCapsuleCollider2D.bounds.max.y;
		if (verticalCapsuleCollider2D != null && verticalCapsuleCollider2D.enabled)
			return verticalCapsuleCollider2D.bounds.max.y;
		return transform.position.y;
	}

	private bool IsAtWaterSurface()
	{
		if (!hasWaterSurface)
			return false;

		return GetActiveColliderTopY() >= waterSurfaceY - waterSurfaceExitOffset;
	}

	private bool IsAtWaterSurfaceLimit()
	{
		if (!hasWaterSurface)
			return false;

		return GetActiveColliderTopY() >= waterSurfaceY - waterSurfaceSwimLimitOffset;
	}

	private bool IsBelowWaterSurfaceRearm()
	{
		if (!hasWaterSurface)
			return false;

		return GetActiveColliderTopY() <= waterSurfaceY - waterSurfaceRearmOffset;
	}

	private bool IsBelowWaterSurfaceEntrySink()
	{
		if (!hasWaterSurface)
			return false;

		return GetActiveColliderTopY() <= waterSurfaceY - waterSurfaceSwimLimitOffset;
	}

	private void WaterExitJump()
	{
		if (waterExitJumpForce <= 0f)
			return;

		m_Rigidbody2D.linearVelocity = new Vector2(m_Rigidbody2D.linearVelocity.x, 0f);
		m_Rigidbody2D.AddForce(new Vector2(0f, waterExitJumpForce));
	}

	public void SetWaterSurfaceY(float surfaceY)
	{
		waterSurfaceY = surfaceY;
		hasWaterSurface = true;
	}


    ///Water
    public void SetInWater(bool val)
    {
        if (inWater == val) return;

        inWater = val;
		waterExitJumpConsumed = false;
		waterExitBufferedUntil = 0f;
		waterEntrySinkActive = inWater && hasWaterSurface;

        if (exitWaterCoroutine != null)
        {
            StopCoroutine(exitWaterCoroutine);
            exitWaterCoroutine = null;
        }

        if (inWater)
        {
            animator.Play("SwimIdle");
            horizontalCapsuleCollider2D.enabled = true;
            verticalCapsuleCollider2D.enabled = false;


            horizontalCapsuleCollider2D.isTrigger = false;
            verticalCapsuleCollider2D.isTrigger = true;
            // Enter water
            m_Rigidbody2D.gravityScale = waterGravityScale;
            m_Rigidbody2D.linearDamping = waterDrag;
            animator.SetBool("IsSwimming", true);
            canDoubleJump = false; 

           
        }
        else
        {
          
            // Exit water -> restore smoothly
            exitWaterCoroutine = StartCoroutine(RestoreFromWater());
            animator.SetBool("IsSwimming", false);
            canDoubleJump = true;
			hasWaterSurface = false;
			waterSurfaceY = float.NegativeInfinity;
       
        }
    }

    private IEnumerator RestoreFromWater()
    {
        float startG = m_Rigidbody2D.gravityScale;
        float startDrag = m_Rigidbody2D.linearDamping;
        float t = 0f;
        while (t < exitWaterRestoreTime)
        {
            t += Time.deltaTime;
            float f = t / exitWaterRestoreTime;
            m_Rigidbody2D.gravityScale = Mathf.Lerp(startG, normalGravityScale, f);
            m_Rigidbody2D.linearDamping = Mathf.Lerp(startDrag, normalDrag, f);
            yield return null;
        }
        m_Rigidbody2D.gravityScale = normalGravityScale;
        m_Rigidbody2D.linearDamping = normalDrag;
        exitWaterCoroutine = null;

        horizontalCapsuleCollider2D.enabled = false;
        verticalCapsuleCollider2D.enabled = true;


        horizontalCapsuleCollider2D.isTrigger = true;
        verticalCapsuleCollider2D.isTrigger = false;
    }
}
