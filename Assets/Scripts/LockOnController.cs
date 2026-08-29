using System;
using System.Collections.Generic;
using UnityEngine;

public enum LockState
{
    None,
    Green,
    Red
}

public class LockOnController : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform player;
    [SerializeField] private BattleParticipant playerParticipant;
    [SerializeField] private List<Transform> targetCandidates = new List<Transform>();
    [SerializeField] private bool automaticallyFindOpponents = true;
    [SerializeField] private float targetRefreshInterval = 0.5f;

    [Header("Lock Range")]
    [SerializeField] private float redLockDistance = 30f;

    [Header("Receivers")]
    [SerializeField] private VersusLockOnCamera lockOnCamera;
    [SerializeField] private PlayerShooter playerShooter;

    private Transform currentTarget;
    private LockState currentLockState = LockState.None;
    private int targetIndex = -1;
    private float targetRefreshTimer;

    public Transform CurrentTarget => currentTarget;
    public LockState CurrentLockState => currentLockState;
    public float DistanceToTarget { get; private set; }

    public event Action<Transform> OnTargetChanged;
    public event Action<LockState> OnLockStateChanged;

    public void SetRedLockDistance(float value)
    {
        redLockDistance = Mathf.Max(0f, value);
    }

    public void SetLockOnCamera(VersusLockOnCamera camera)
    {
        lockOnCamera = camera;

        if (lockOnCamera != null)
        {
            lockOnCamera.ChangeLookTarget(currentTarget);
        }
    }

    private void Start()
    {
        if (playerParticipant == null && player != null)
        {
            playerParticipant = player.GetComponentInParent<BattleParticipant>();
        }

        RefreshOpponentTargets();

        if (currentTarget == null)
        {
            SelectNextTarget();
        }
    }

    private void Update()
    {
        UpdateOpponentTargets();

        if (VersusInputManager.Instance.WasPressedThisFrame(VersusInputAction.Search))
        {
            SelectNextTarget();
        }

        UpdateLockState();
    }

    private void UpdateOpponentTargets()
    {
        if (!automaticallyFindOpponents)
        {
            return;
        }

        targetRefreshTimer -= Time.deltaTime;

        if (targetRefreshTimer <= 0f)
        {
            RefreshOpponentTargets();
            targetRefreshTimer = targetRefreshInterval;
        }
    }

    private void RefreshOpponentTargets()
    {
        if (!automaticallyFindOpponents || playerParticipant == null)
        {
            return;
        }

        Transform previousTarget = currentTarget;
        targetCandidates.Clear();

        foreach (BattleParticipant participant in BattleParticipant.AllParticipants)
        {
            if (participant != null
                && participant.Team != playerParticipant.Team
                && participant.IsAvailable)
            {
                targetCandidates.Add(participant.transform);
            }
        }

        targetCandidates.Sort((left, right) =>
        {
            float leftDistance = (left.position - playerParticipant.transform.position).sqrMagnitude;
            float rightDistance = (right.position - playerParticipant.transform.position).sqrMagnitude;
            return leftDistance.CompareTo(rightDistance);
        });

        int previousIndex = targetCandidates.IndexOf(previousTarget);

        if (previousIndex >= 0)
        {
            targetIndex = previousIndex;
            return;
        }

        targetIndex = -1;

        if (targetCandidates.Count > 0)
        {
            SelectNextTarget();
        }
        else
        {
            SetTarget(null);
        }
    }

    public void SelectNextTarget()
    {
        RemoveUnavailableTargets();

        if (targetCandidates.Count == 0)
        {
            SetTarget(null);
            return;
        }

        targetIndex = (targetIndex + 1) % targetCandidates.Count;
        SetTarget(targetCandidates[targetIndex]);
    }

    public void SetTarget(Transform newTarget)
    {
        if (currentTarget == newTarget)
        {
            return;
        }

        currentTarget = newTarget;
        lockOnCamera?.ChangeLookTarget(newTarget);
        playerShooter?.SetTarget(newTarget);
        OnTargetChanged?.Invoke(newTarget);
        UpdateLockState(true);
    }

    private void UpdateLockState(bool forceNotify = false)
    {
        LockState nextState;

        if (player == null || currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            DistanceToTarget = 0f;
            nextState = LockState.None;
        }
        else
        {
            DistanceToTarget = Vector3.Distance(player.position, currentTarget.position);
            nextState = DistanceToTarget <= redLockDistance ? LockState.Red : LockState.Green;
        }

        if (!forceNotify && nextState == currentLockState)
        {
            return;
        }

        currentLockState = nextState;
        OnLockStateChanged?.Invoke(currentLockState);
    }

    private void RemoveUnavailableTargets()
    {
        for (int i = targetCandidates.Count - 1; i >= 0; i--)
        {
            Transform candidate = targetCandidates[i];

            if (candidate == null || !candidate.gameObject.activeInHierarchy)
            {
                targetCandidates.RemoveAt(i);
            }
        }

        if (targetIndex >= targetCandidates.Count)
        {
            targetIndex = -1;
        }
    }

    private void OnValidate()
    {
        redLockDistance = Mathf.Max(0f, redLockDistance);
        targetRefreshInterval = Mathf.Max(0.05f, targetRefreshInterval);
    }
}
