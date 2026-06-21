using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMechController : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private Transform moveReference;
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float groundAcceleration = 45f;
    [SerializeField] private float airAcceleration = 22f;
    [SerializeField] private float groundDeceleration = 40f;
    [SerializeField] private float airDeceleration = 8f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float jumpHoldForce = 22f;
    [SerializeField] private float maxJumpRiseSpeed = 10f;
    [SerializeField] private int jumpHoldFrames = 12;
    [SerializeField] private float groundCheckDistance = 1.2f;
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private float doubleTapTime = 0.3f;

    [Header("Boost Dash")]
    [SerializeField] private float boostSpeed = 22f;
    [SerializeField] private float boostStartSpeed = 30f;
    [SerializeField] private float boostAcceleration = 75f;
    [SerializeField] private float boostEndDeceleration = 7f;
    [SerializeField] private float boostCooldown = 0.15f;

    [Header("Step")]
    [SerializeField] private float stepSpeed = 14f;
    [SerializeField] private float stepStartSpeed = 26f;
    [SerializeField] private float stepAcceleration = 90f;
    [SerializeField] private float stepEndDeceleration = 14f;
    [SerializeField] private float stepCooldown = 0.08f;

    private Rigidbody rb;
    private Vector3 moveDirection;
    private Vector3 boostDirection;
    private Vector2 rawMoveInput;
    private Vector2 lastStepInput = Vector2.zero;
    private Vector2 activeStepInput = Vector2.zero;
    private Vector3 stepDirection;
    private bool isBoosting;
    private bool isStepping;
    private bool isJumpButtonHeld;
    private bool isHoldingJump;
    private bool isTouchingGround;
    private int jumpButtonHoldFrames;
    private float boostCooldownTimer;
    private float stepCooldownTimer;
    private float highSpeedEndDeceleration;
    private float jumpButtonDownTime;
    private float lastShortJumpTapTime = -999f;

    public event Action<Vector3> OnStepStarted;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (moveReference == null && Camera.main != null)
        {
            moveReference = Camera.main.transform;
        }

        highSpeedEndDeceleration = boostEndDeceleration;
    }

    private void Update()
    {
        // Unity標準の入力軸を使い、WASD入力をカメラ基準の移動方向に変換する。
        rawMoveInput = GetRawMoveInput();

        HandleStepInput(rawMoveInput);

        moveDirection = GetCameraRelativeMoveDirection(rawMoveInput.x, rawMoveInput.y);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleJumpButtonDown();
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            HandleJumpButtonUp();
        }

        UpdateBoostCooldown();
        UpdateStepCooldown();
        RotateToMoveDirection();
    }

    private void FixedUpdate()
    {
        Move();
        UpdateJumpHold();
        ApplyHoldJump();

        // 接地判定は物理更新ごとにCollision側で入れ直す。
        isTouchingGround = false;
    }

    private void Move()
    {
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 currentHorizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        Vector3 targetHorizontalVelocity;
        float acceleration;

        if (isStepping)
        {
            targetHorizontalVelocity = stepDirection * stepSpeed;
            acceleration = stepAcceleration;
        }
        else if (isBoosting)
        {
            targetHorizontalVelocity = boostDirection * boostSpeed;
            acceleration = boostAcceleration;
        }
        else if (moveDirection.sqrMagnitude > 0.01f)
        {
            targetHorizontalVelocity = moveDirection * moveSpeed;
            acceleration = GetMoveAcceleration(currentHorizontalVelocity);
        }
        else
        {
            targetHorizontalVelocity = Vector3.zero;
            acceleration = GetStopDeceleration(currentHorizontalVelocity);
        }

        // 横方向は目標速度へ少しずつ近づけ、ジャンプや落下の縦速度は維持する。
        Vector3 nextHorizontalVelocity = Vector3.MoveTowards(
            currentHorizontalVelocity,
            targetHorizontalVelocity,
            acceleration * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector3(
            nextHorizontalVelocity.x,
            currentVelocity.y,
            nextHorizontalVelocity.z
        );
    }

    private void HandleJumpButtonDown()
    {
        bool isDoubleTap = Time.time - lastShortJumpTapTime <= doubleTapTime;

        // 1回目を短く離した後、2回目を押している間はBDする。
        if (isDoubleTap && CanBoost())
        {
            StartBoost();
            ResetJumpHoldState();
            lastShortJumpTapTime = -999f;
            return;
        }

        isJumpButtonHeld = true;
        isHoldingJump = false;
        jumpButtonHoldFrames = 0;
        jumpButtonDownTime = Time.time;
    }

    private void UpdateJumpHold()
    {
        if (!isJumpButtonHeld || isBoosting)
        {
            return;
        }

        jumpButtonHoldFrames++;

        // BDの1回目入力をジャンプにしないため、一定フレーム押し続けたらジャンプ開始にする。
        if (!isHoldingJump && jumpButtonHoldFrames >= jumpHoldFrames)
        {
            StartHoldJump();
        }
    }

    private void HandleJumpButtonUp()
    {
        bool wasShortTap = !isHoldingJump && Time.time - jumpButtonDownTime <= doubleTapTime;

        if (wasShortTap)
        {
            lastShortJumpTapTime = Time.time;
        }

        ResetJumpHoldState();
        StopBoost();
    }

    private void StartHoldJump()
    {
        isHoldingJump = true;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = Mathf.Max(velocity.y, 0f);
        rb.linearVelocity = velocity;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void ApplyHoldJump()
    {
        if (!isHoldingJump || isBoosting)
        {
            return;
        }

        if (rb.linearVelocity.y >= maxJumpRiseSpeed)
        {
            return;
        }

        // Spaceを押している間は上方向へ力を加え続け、空中でも上昇できるようにする。
        rb.AddForce(Vector3.up * jumpHoldForce, ForceMode.Acceleration);
    }

    private void ResetJumpHoldState()
    {
        isJumpButtonHeld = false;
        isHoldingJump = false;
        jumpButtonHoldFrames = 0;
    }

    private void StartBoost()
    {
        StopStep();

        isBoosting = true;
        boostDirection = moveDirection;

        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float speedInBoostDirection = Vector3.Dot(horizontalVelocity, boostDirection);

        // BD開始時に初速を入れて、入力直後の押し出し感を作る。
        if (speedInBoostDirection < boostStartSpeed)
        {
            horizontalVelocity = boostDirection * boostStartSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);
        }
    }

    private void StopBoost()
    {
        if (!isBoosting)
        {
            return;
        }

        isBoosting = false;
        boostCooldownTimer = boostCooldown;
        highSpeedEndDeceleration = boostEndDeceleration;
    }

    private void UpdateBoostCooldown()
    {
        if (boostCooldownTimer > 0f)
        {
            boostCooldownTimer -= Time.deltaTime;
        }
    }

    private void UpdateStepCooldown()
    {
        if (stepCooldownTimer > 0f)
        {
            stepCooldownTimer -= Time.deltaTime;
        }
    }

    private bool CanBoost()
    {
        return moveDirection.sqrMagnitude > 0.01f && boostCooldownTimer <= 0f;
    }

    private void HandleStepInput(Vector2 input)
    {
        Vector2 dominantInput = GetDominantInput(input);

        if (isStepping)
        {
            if (dominantInput != activeStepInput)
            {
                StopStep();
            }

            return;
        }

        if (dominantInput == Vector2.zero)
        {
            return;
        }

        if (IsDirectionPressedThisFrame(dominantInput))
        {
            if (dominantInput == lastStepInput && CanStep())
            {
                StartStep(dominantInput);
                lastStepInput = Vector2.zero;
                return;
            }

            lastStepInput = dominantInput;
        }
    }

    private void StartStep(Vector2 input)
    {
        StopBoost();

        isStepping = true;
        activeStepInput = input;
        stepDirection = GetCameraRelativeMoveDirection(input.x, input.y);

        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float speedInStepDirection = Vector3.Dot(horizontalVelocity, stepDirection);

        // ステップ開始時に初速を入れる。ここが将来の誘導切り発生タイミング。
        if (speedInStepDirection < stepStartSpeed)
        {
            horizontalVelocity = stepDirection * stepStartSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);
        }

        OnStepStarted?.Invoke(stepDirection);
    }

    private void StopStep()
    {
        if (!isStepping)
        {
            return;
        }

        isStepping = false;
        activeStepInput = Vector2.zero;
        stepCooldownTimer = stepCooldown;
        highSpeedEndDeceleration = stepEndDeceleration;
    }

    private bool CanStep()
    {
        return stepCooldownTimer <= 0f;
    }

    private static Vector2 GetRawMoveInput()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            horizontal -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            horizontal += 1f;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            vertical -= 1f;
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            vertical += 1f;
        }

        return new Vector2(horizontal, vertical);
    }

    private static Vector2 GetDominantInput(Vector2 input)
    {
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            return new Vector2(Mathf.Sign(input.x), 0f);
        }

        if (Mathf.Abs(input.y) > 0f)
        {
            return new Vector2(0f, Mathf.Sign(input.y));
        }

        return Vector2.zero;
    }

    private static bool IsDirectionPressedThisFrame(Vector2 direction)
    {
        if (direction == Vector2.up)
        {
            return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        }

        if (direction == Vector2.down)
        {
            return Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
        }

        if (direction == Vector2.left)
        {
            return Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
        }

        if (direction == Vector2.right)
        {
            return Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
        }

        return false;
    }

    private Vector3 GetCameraRelativeMoveDirection(float horizontal, float vertical)
    {
        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical);

        if (inputDirection.sqrMagnitude <= 0.01f)
        {
            return Vector3.zero;
        }

        if (moveReference == null)
        {
            return inputDirection.normalized;
        }

        Vector3 forward = Vector3.ProjectOnPlane(moveReference.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(moveReference.right, Vector3.up).normalized;

        if (forward.sqrMagnitude <= 0.01f)
        {
            forward = transform.forward;
        }

        // Wはカメラの前方向、A/Dはカメラの左右方向。ロックオン中はWが敵方向になる。
        return (forward * vertical + right * horizontal).normalized;
    }

    private float GetMoveAcceleration(Vector3 currentHorizontalVelocity)
    {
        if (IsBoostInertiaRemaining(currentHorizontalVelocity))
        {
            return highSpeedEndDeceleration;
        }

        return IsGrounded() ? groundAcceleration : airAcceleration;
    }

    private float GetStopDeceleration(Vector3 currentHorizontalVelocity)
    {
        if (IsBoostInertiaRemaining(currentHorizontalVelocity))
        {
            return highSpeedEndDeceleration;
        }

        return IsGrounded() ? groundDeceleration : airDeceleration;
    }

    private bool IsBoostInertiaRemaining(Vector3 currentHorizontalVelocity)
    {
        return currentHorizontalVelocity.magnitude > moveSpeed + 0.1f;
    }

    private bool IsGrounded()
    {
        if (isTouchingGround)
        {
            return true;
        }

        // 念のため足元方向にもレイを飛ばし、接地判定の取りこぼしを減らす。
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    private void RotateToMoveDirection()
    {
        if (moveDirection.sqrMagnitude <= 0.01f)
        {
            return;
        }

        // 移動方向へ急に向きを変えず、少し滑らかに回転させる。
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!IsInGroundLayer(collision.gameObject.layer))
        {
            return;
        }

        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isTouchingGround = true;
                return;
            }
        }
    }

    private bool IsInGroundLayer(int layer)
    {
        return (groundLayer.value & (1 << layer)) != 0;
    }
}
