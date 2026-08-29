using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyCombatAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private PlayerShooter shooter;
    [SerializeField] private MechHealth health;
    [SerializeField] private HitReactionController hitReaction;

    [Header("Movement")]
    [SerializeField] private float preferredDistance = 18f;
    [SerializeField] private float distanceTolerance = 3f;
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float strafeWeight = 0.65f;

    [Header("Combat")]
    [SerializeField] private float shootRange = 45f;
    [SerializeField] private float decisionInterval = 0.6f;

    private Rigidbody rb;
    private float decisionTimer;
    private float strafeSign = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (shooter == null)
        {
            shooter = GetComponent<PlayerShooter>();
        }

        if (health == null)
        {
            health = GetComponent<MechHealth>();
        }

        if (hitReaction == null)
        {
            hitReaction = GetComponent<HitReactionController>();
        }

        shooter?.SetPlayerInputEnabled(false);
    }

    private void Update()
    {
        if (!CanAct())
        {
            return;
        }

        FaceTarget();
        shooter?.SetTarget(target);

        decisionTimer -= Time.deltaTime;

        if (decisionTimer <= 0f)
        {
            MakeCombatDecision();
            decisionTimer = decisionInterval;
        }
    }

    private void FixedUpdate()
    {
        if (!CanAct())
        {
            return;
        }

        MoveAroundTarget();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        shooter?.SetTarget(newTarget);
    }

    private bool CanAct()
    {
        return target != null
            && (health == null || !health.IsDestroyed)
            && (hitReaction == null || !hitReaction.IsReacting);
    }

    private void MoveAroundTarget()
    {
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (distance <= 0.01f)
        {
            return;
        }

        Vector3 forward = toTarget / distance;
        Vector3 strafe = Vector3.Cross(Vector3.up, forward) * strafeSign;
        Vector3 desiredDirection;

        if (distance > preferredDistance + distanceTolerance)
        {
            desiredDirection = forward;
        }
        else if (distance < preferredDistance - distanceTolerance)
        {
            desiredDirection = -forward;
        }
        else
        {
            desiredDirection = strafe * strafeWeight;
        }

        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 currentHorizontal = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        Vector3 targetHorizontal = desiredDirection.normalized * moveSpeed;
        Vector3 nextHorizontal = Vector3.MoveTowards(
            currentHorizontal,
            targetHorizontal,
            acceleration * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector3(nextHorizontal.x, currentVelocity.y, nextHorizontal.z);
    }

    private void FaceTarget()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void MakeCombatDecision()
    {
        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= shootRange)
        {
            shooter?.TryShoot();
        }

        strafeSign *= -1f;
    }

    private void OnValidate()
    {
        preferredDistance = Mathf.Max(0f, preferredDistance);
        distanceTolerance = Mathf.Max(0f, distanceTolerance);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        acceleration = Mathf.Max(0f, acceleration);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
        strafeWeight = Mathf.Clamp01(strafeWeight);
        shootRange = Mathf.Max(0f, shootRange);
        decisionInterval = Mathf.Max(0.05f, decisionInterval);
    }
}
