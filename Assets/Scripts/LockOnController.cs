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
    [SerializeField] private List<Transform> targetCandidates = new List<Transform>();
    [SerializeField] private KeyCode switchTargetKey = KeyCode.Tab;

    [Header("Lock Range")]
    [SerializeField] private float redLockDistance = 30f;

    [Header("Receivers")]
    [SerializeField] private VersusLockOnCamera lockOnCamera;
    [SerializeField] private PlayerShooter playerShooter;

    private Transform currentTarget;
    private LockState currentLockState = LockState.None;
    private int targetIndex = -1;

    public Transform CurrentTarget => currentTarget;
    public LockState CurrentLockState => currentLockState;
    public float DistanceToTarget { get; private set; }

    public event Action<Transform> OnTargetChanged;
    public event Action<LockState> OnLockStateChanged;

    private void Start()
    {
        SelectNextTarget();
    }

    private void Update()
    {
        if (Input.GetKeyDown(switchTargetKey))
        {
            SelectNextTarget();
        }

        UpdateLockState();
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
    }
}
