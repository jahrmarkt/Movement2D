

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

	public enum PlayerState
	{
		Move,
		Rise,
		Fall,
		Dead,
		HangBack,
		HangSide,
		Lift,
		HangDown,
		HangDownSide,
	}



	public PlayerState state = PlayerState.Move;

	Animator anim; 
	Rigidbody2D rb;
	SpriteRenderer renderer;

	[Header("General Settings")]
	public bool faceRight = true;

	// Collisions 
	public bool onGround = false;	
	public bool onRoof = false;


	// Detections
	bool detectLedgeLeft = false;
	bool detectLedgeRight = false;
	bool detectLedgeBottomLeft = false;
	bool detectLedgeBottomRight = false;

	// Input
	private KeyCode inputUp = KeyCode.W;
	private KeyCode inputLeft = KeyCode.A;
	private KeyCode inputRight = KeyCode.D;
	private KeyCode inputDown = KeyCode.S;
	private KeyCode inputGrab = KeyCode.Space;
	private KeyCode inputAction = KeyCode.Q;


	// Scales 
	[Header("Walk Settings")]
	public float walkSpeed = 3;
	private float maxWalkSpeed = 6;	// currently unused
	private float walkAccel = 0.1f; // currently unused
	public float walkLimit = 3.0f; // for walking animation and running animation

	[Header("Jump Settings")]
	public float jumpForce = 5;
	public float jumpTime = 1.5f;

	bool startJumpLock = false;	// for jumping higher by keeping pressed
	bool jumpLock = false;	// to start coroutine once



	[Header("Climb Settings")]
	public float hangSpeed = 0.3f;

	
	Vector2 ledgePoint; // for navigating on a ledge, used for position when hanging


	//Initialization
	void Start()
	{
		anim = GetComponent<Animator> ();
		anim.Play ("Idle");
		rb = GetComponent<Rigidbody2D> ();
		renderer = GetComponent<SpriteRenderer>();

		ledgePoint = new Vector2(transform.position.x, transform.position.y + 1);
	}


	// mostly state machine here
	void Update()
	{	
		LayerMask ledgeMask = LayerMask.GetMask ("Wall");

		// check on ground
		RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector3.down, 2.8f, ledgeMask);

		Debug.DrawRay (transform.position, Vector3.down * 2.8f);

		onGround = false;

		if (hit.collider != null) {
			onGround = true;
		}



		if (!IsHangingState ()) {
			ledgePoint = new Vector2 (transform.position.x, transform.position.y + 1);
		} else if (state == PlayerState.HangBack || state == PlayerState.HangSide){
			transform.position = new Vector3 (ledgePoint.x, ledgePoint.y-1-2, transform.position.z);
		}

		// check ledges left and right of ledge Point when hanging
		Collider2D checkLedgePointL = Physics2D.OverlapCircle(ledgePoint + new Vector2(-0.5f,0), 0.2f, ledgeMask);
		Collider2D checkLedgePointR = Physics2D.OverlapCircle(ledgePoint + new Vector2(0.5f,0), 0.2f, ledgeMask);
		bool checkLedgeL = checkLedgePointL != null && checkLedgePointL.gameObject.tag == "Ledge";
		bool checkLedgeR = checkLedgePointR != null && checkLedgePointR.gameObject.tag == "Ledge";

		//Debug.Log (checkLedgePointL.name);
		Debug.DrawLine (ledgePoint, new Vector2(ledgePoint.x-0.5f, ledgePoint.y+ 0.1f), (checkLedgeL) ? Color.green : Color.red);
		Debug.DrawLine (ledgePoint, new Vector2(ledgePoint.x+0.5f, ledgePoint.y+ 0.1f), (checkLedgeR) ? Color.green : Color.red);




		// raycast for ledges when moving
		float f = 3.0f;
		RaycastHit2D hitL = Physics2D.Raycast(transform.position, new Vector3(-1,3,0), f, ledgeMask);
		bool detectLedgeLeft = hitL.collider != null && hitL.collider.gameObject.tag == "Ledge";
		RaycastHit2D hitR = Physics2D.Raycast(transform.position, new Vector3(1,3,0), f, ledgeMask);
		bool detectLedgeRight = hitR.collider != null && hitR.collider.gameObject.tag == "Ledge";
		RaycastHit2D hitBL = Physics2D.Raycast(transform.position, new Vector3(-1,-3,0).normalized, f, ledgeMask);
		bool detectLedgeBottomLeft = hitBL.collider != null && hitBL.collider.gameObject.tag == "Ledge";
		RaycastHit2D hitBR = Physics2D.Raycast(transform.position, new Vector3(1,-3,0), f, ledgeMask);
		bool detectLedgeBottomRight = hitBR.collider != null && hitBR.collider.gameObject.tag == "Ledge";

		Debug.DrawRay (transform.position, new Vector3(-1,3,0).normalized * f, (detectLedgeLeft) ? Color.green : Color.white);
		Debug.DrawRay (transform.position, new Vector3(1,3,0).normalized * f, (detectLedgeRight) ? Color.green : Color.white);
		Debug.DrawRay (transform.position, new Vector3(-1,-3,0).normalized * f, (detectLedgeBottomLeft) ? Color.green : Color.white);
		Debug.DrawRay (transform.position, new Vector3(1,-3,0).normalized * f, (detectLedgeBottomRight) ? Color.green : Color.white);
		Debug.DrawLine (ledgePoint, new Vector2(ledgePoint.x, ledgePoint.y+ 0.1f), Color.yellow);


		switch (state) {
		case PlayerState.Move:  
			jumpLock = false;
			if (!onGround) {
				state = PlayerState.Fall;
				anim.Play ("Fall");
			} else {
				// Jump
				if (Input.GetKey (inputUp)) {
					state = PlayerState.Rise;
					rb.AddForce (new Vector2 (0.0f, jumpForce), ForceMode2D.Impulse);
					anim.Play ("Jump");
				}

				// Hang down on Ledge
				if (detectLedgeBottomLeft && detectLedgeBottomRight && Input.GetKey (inputDown) && Input.GetKey (inputGrab)) {
					ledgePoint = new Vector2 (transform.position.x, hitBL.point.y);

					state = PlayerState.HangDown;
					anim.Play ("HangDown");
					anim.speed = 1.5f;

					rb.velocity = new Vector3 (0, 0, 0);
					rb.gravityScale = 0;
					rb.isKinematic = true;

					transform.position = new Vector3 (ledgePoint.x, ledgePoint.y, 0);
				} else if ((detectLedgeBottomLeft || detectLedgeBottomRight) && Input.GetKey (inputDown) && Input.GetKey (inputGrab)) {
					if (detectLedgeBottomLeft) {
						faceRight = true;
						ledgePoint = new Vector2 (transform.position.x, hitBL.point.y);
					} else {
						faceRight = false;
						ledgePoint = new Vector2 (transform.position.x, hitBR.point.y);
					}

					Flip ();

					state = PlayerState.HangDownSide;
					anim.Play ("HangDown");
					anim.speed = 1.5f;

					rb.velocity = new Vector3 (0, 0, 0);
					rb.gravityScale = 0;
					rb.isKinematic = true;

					transform.position = new Vector3 (ledgePoint.x, ledgePoint.y, 0);
				}


			}
			break;
		case PlayerState.Rise:
			if (onRoof || rb.velocity.y <= 0) {
				state = PlayerState.Fall;
			}
			if ((detectLedgeLeft || detectLedgeRight) && Input.GetKey (inputGrab)) {

				if (detectLedgeLeft && !detectLedgeRight) {
					ledgePoint = hitL.point;
				} else if (!detectLedgeLeft && detectLedgeRight) {
					ledgePoint = hitR.point;
				} else if (detectLedgeLeft && detectLedgeRight) {
					ledgePoint = new Vector2(transform.position.x, hitL.point.y);
				}

				state = PlayerState.HangBack;
				rb.velocity = new Vector2 (0, 0);
				rb.gravityScale = 0;

				anim.Play ("HangBack");
				// set correct position
				float py = ledgePoint.y - renderer.bounds.extents.y;
				transform.position = new Vector3 (transform.position.x, py, transform.position.z);
			}
			break;
		case PlayerState.Fall:
			if (rb.velocity.y > 0) {
				state = PlayerState.Rise;
			}

			// go to move state when on ground
			if (onGround) {
				state = PlayerState.Move;
				SetMoveAnimation ();
			}
			break;
		case PlayerState.Lift:
			// when animation done goto move
			if (anim.GetCurrentAnimatorStateInfo (0).length <
				anim.GetCurrentAnimatorStateInfo (0).normalizedTime) {
				state = PlayerState.Move;
				rb.velocity = new Vector2 (0, 0);
				rb.gravityScale = 1;
				anim.speed = 1;
				SetMoveAnimation ();
				// set correct position
				float py = ledgePoint.y + 3.0f;
				transform.position = new Vector3 (transform.position.x, py, transform.position.z);

			}
			break;
		case PlayerState.HangDown:
			// when animation done goto HangBack
			if (anim.GetCurrentAnimatorStateInfo (0).length <
				anim.GetCurrentAnimatorStateInfo (0).normalizedTime) {
				state = PlayerState.HangBack;
				rb.velocity = new Vector2 (0, 0);
				rb.isKinematic = false;
				onGround = false;
				anim.Play ("HangBack");
				anim.speed = 1;
				// set correct position
				float py = ledgePoint.y - 1;
				transform.position = new Vector3 (transform.position.x, py, transform.position.z);
			}
			break;
		case PlayerState.HangDownSide:
			// when animation done goto HangBack
			if (anim.GetCurrentAnimatorStateInfo (0).length <
				anim.GetCurrentAnimatorStateInfo (0).normalizedTime) {
				state = PlayerState.HangSide;
				rb.velocity = new Vector2 (0, 0);
				rb.isKinematic = false;
				onGround = false;
				anim.Play ("HangSide");
				anim.speed = 1;
				// set correct position
				float py = ledgePoint.y - 1;
				transform.position = new Vector3 (transform.position.x, py, transform.position.z);
			}
			break;
		case PlayerState.HangBack:
			if (!checkLedgeL) {
				if (!checkLedgeR) {
					state = PlayerState.Fall;
					rb.gravityScale = 1;
					anim.Play ("Fall");
				} else { // hang to the side
					state = PlayerState.HangSide;
					anim.Play ("HangSide");
					faceRight = false;
					rb.velocity = new Vector3 (0, 0, 0);
					Flip ();
				}
			} else if (!checkLedgeR) {
				state = PlayerState.HangSide;
				anim.Play ("HangSide");
				faceRight = true;
				rb.velocity = new Vector3 (0, 0, 0);
				Flip ();
			}

			if (Input.GetKey (inputDown)) {
				state = PlayerState.Fall;
				rb.gravityScale = 1;
				anim.Play ("Fall");
			}

			// set animation lift up
			if (Input.GetKeyDown (inputUp)) {
				state = PlayerState.Lift;
				rb.gravityScale = 0;
				//transform.position = new Vector3(ledgePoint.x, ledgePoint.y, 0) - new Vector3(0,0.5f,0);
				transform.position = new Vector3(ledgePoint.x, ledgePoint.y, 0);
				//rb.velocity = new Vector3 (rb.velocity.x, 0.0f, 0);
				anim.Play ("LiftUp");
				anim.speed = 1.5f;
			} 

			// set animation for move left and right
			float hgSpeed = 0;

			if (Input.GetKey (inputLeft)) {
				hgSpeed = -hangSpeed;
			}

			if (Input.GetKey (inputRight)) {
				hgSpeed = hangSpeed;
			} 
			if (hgSpeed < 0) {
				anim.Play ("HangBackMoveLeft");
			} else if (hgSpeed > 0) {
				anim.Play ("HangBackMoveRight");
			} else {
				anim.Play ("HangBack");
			}
			break;
		case PlayerState.HangSide:
			hgSpeed = 0;

			// Lift up and fall down
			if (Input.GetKey (inputUp)) {
				state = PlayerState.Lift;
				rb.gravityScale = 0;
				rb.velocity = new Vector3 (rb.velocity.x, 3.0f, 0);
				anim.Play ("LiftUp");
				anim.speed = 1.5f;
			} 

			if (Input.GetKey (inputDown)) {
				state = PlayerState.Fall;
				rb.gravityScale = 1;
				anim.Play ("Fall");
			}


			if (Input.GetKey (inputLeft) && !faceRight) {
				state = PlayerState.HangBack;
				rb.velocity = new Vector2 (5, 0);
				anim.Play ("HangBack");
			} 

			if (Input.GetKey (inputRight) && faceRight) {
				state = PlayerState.HangBack;
				rb.velocity = new Vector2 (-5, 0);
				anim.Play ("HangBack");
			} 
			break;

		default:
			break;

		}
	}


	// set Animation state for move
	private void SetMoveAnimation() {
		if (rb.velocity.x == 0) {
			anim.Play ("Idle");
		} else if (rb.velocity.x <= walkLimit && rb.velocity.x >= -walkLimit)
			anim.Play ("Walk");
		if (rb.velocity.x > walkLimit || rb.velocity.x < -walkLimit) {
			anim.Play ("Run");
		}
	}


	private bool IsHangingState() {
		return state == PlayerState.HangDown ||
			state == PlayerState.Lift ||
			state == PlayerState.HangBack ||
			state == PlayerState.HangDownSide ||
			state == PlayerState.HangSide;
	}

	//player movement here 
	void FixedUpdate() {
		
		float speed = rb.velocity.x;
		
		switch (state) {
		case PlayerState.Move:

			// moving

			if (Input.GetKey (inputLeft)) {	
				speed = -walkSpeed;
			} 
			if (Input.GetKey (inputRight)) {	
				speed = walkSpeed;
			} 

		

			// round to zero if speed to low to avoid flipping the sprite
			if (speed < 0.2f && speed > -0.2f) {
				speed = 0;
			}

			rb.velocity = new Vector2 (speed, rb.velocity.y);
			SetMoveAnimation ();
			break;
		
		case PlayerState.Rise:
			// moving

			if (Input.GetKey (inputLeft)) {	
				speed = -walkSpeed / 2;
			} 
			if (Input.GetKey (inputRight)) {	
				speed = walkSpeed / 2;
			} 


			// round to zero if speed to low to avoid flipping the sprite
			if (speed < 0.5f && speed > -0.5f) {
				speed = 0;
			}

			rb.velocity = new Vector2 (speed, rb.velocity.y);
			
			/*
			if (Input.GetKey (inputUp) && !jumpLock) {
				rb.AddForce (new Vector2 (0.0f, jumpForce * 5), ForceMode2D.Force);
				if (!startJumpLock) {
					startJumpLock = true;
					StartCoroutine ("LockJumping");
				}
			}
			// lock jumping after releasing press
			if (!Input.GetKey (inputUp)) {
				jumpLock = true;
			}
			*/
			break;
		
		case PlayerState.Fall:
			// moving

			if (Input.GetKey (inputLeft)) {	
				speed = -walkSpeed / 2;
			} 
			if (Input.GetKey (inputRight)) {	
				speed = walkSpeed / 2;
			} 


			// round to zero if speed to low to avoid flipping the sprite
			if (speed < 0.5f && speed > -0.5f) {
				speed = 0;
			}

			rb.velocity = new Vector2 (speed, rb.velocity.y);
			
			break;
		
		case PlayerState.HangBack:

			// moving
			speed = 0;

			if (Input.GetKey (inputLeft)) {	
				speed = -hangSpeed;
			} 

			if (Input.GetKey (inputRight)) {	
				speed = hangSpeed;
			} 

			ledgePoint += new Vector2 (speed, 0);
			break;
		case PlayerState.HangSide:

			ledgePoint += new Vector2(0, 0);
			break;
		default:
			break;
		}


		if (rb.velocity.x < 0 && faceRight || rb.velocity.x > 0 && !faceRight) {
			Flip ();
		}
	}



	// to limit jump time
	IEnumerator LockJumping(){
		yield return new WaitForSeconds (jumpTime);
		jumpLock = true;
		startJumpLock = false;
	}


	//Flip Player Sprite
	void Flip() {	
		faceRight = !faceRight;
		Vector3 temp = transform.localScale;
		if (faceRight) {
			temp.x = 1;
		} else {
			temp.x = -1;
		}
		transform.localScale = temp;
	}

}
