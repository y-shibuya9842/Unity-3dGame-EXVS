using System;
using UnityEngine;

public class PartnerSupportController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode supportKey = KeyCode.Z;

    [Header("Support")]
    [SerializeField] private float supportCooldown = 12f;

    [Header("References")]
    [SerializeField] private BattleParticipant participant;
    [SerializeField] private LockOnController lockOnController;

    private float cooldownTimer;

    public float RemainingCooldown => Mathf.Max(0f, cooldownTimer);
    public bool IsReady => cooldownTimer <= 0f;

    public event Action<BattleParticipant> OnSupportRequested;
    public event Action OnSupportFailed;

    private void Awake()
    {
        if (participant == null)
        {
            participant = GetComponent<BattleParticipant>();
        }
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(supportKey))
        {
            TryRequestSupport();
        }
    }

    public bool TryRequestSupport()
    {
        if (!IsReady || participant == null)
        {
            OnSupportFailed?.Invoke();
            return false;
        }

        Transform target = lockOnController != null ? lockOnController.CurrentTarget : null;
        BattleParticipant ally = participant.FindNearestAlly();

        if (target == null || ally == null)
        {
            OnSupportFailed?.Invoke();
            return false;
        }

        PlayerShooter allyShooter = ally.GetComponent<PlayerShooter>();

        if (allyShooter == null)
        {
            OnSupportFailed?.Invoke();
            return false;
        }

        allyShooter.SetTarget(target);

        if (!allyShooter.TryShoot())
        {
            OnSupportFailed?.Invoke();
            return false;
        }

        cooldownTimer = supportCooldown;
        OnSupportRequested?.Invoke(ally);
        return true;
    }

    private void OnValidate()
    {
        supportCooldown = Mathf.Max(0f, supportCooldown);
    }
}
