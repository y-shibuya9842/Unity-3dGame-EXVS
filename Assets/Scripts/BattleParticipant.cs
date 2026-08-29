using System.Collections.Generic;
using UnityEngine;

public class BattleParticipant : MonoBehaviour
{
    private static readonly List<BattleParticipant> participants = new List<BattleParticipant>();

    [Header("Battle")]
    [SerializeField] private BattleTeam team;
    [SerializeField] private int unitCost = 2000;
    [SerializeField] private float respawnDelay = 2f;

    [Header("References")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private MechHealth health;
    [SerializeField] private Transform respawnPoint;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    public BattleTeam Team => team;
    public int UnitCost => unitCost;
    public float RespawnDelay => respawnDelay;
    public bool IsAvailable => isActiveAndEnabled && (health == null || !health.IsDestroyed);
    public static IReadOnlyList<BattleParticipant> AllParticipants => participants;

    private void Awake()
    {
        if (health == null)
        {
            health = GetComponent<MechHealth>();
        }

        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void OnEnable()
    {
        if (!participants.Contains(this))
        {
            participants.Add(this);
        }

        if (health != null)
        {
            health.OnDestroyed += HandleDestroyed;
        }
    }

    private void OnDisable()
    {
        participants.Remove(this);

        if (health != null)
        {
            health.OnDestroyed -= HandleDestroyed;
        }
    }

    public BattleParticipant FindNearestOpponent()
    {
        BattleParticipant nearest = null;
        float nearestDistanceSquared = float.MaxValue;

        foreach (BattleParticipant participant in participants)
        {
            if (participant == null
                || participant == this
                || participant.team == team
                || !participant.IsAvailable)
            {
                continue;
            }

            float distanceSquared = (participant.transform.position - transform.position).sqrMagnitude;

            if (distanceSquared < nearestDistanceSquared)
            {
                nearest = participant;
                nearestDistanceSquared = distanceSquared;
            }
        }

        return nearest;
    }

    public void Respawn()
    {
        Transform point = respawnPoint;
        transform.SetPositionAndRotation(
            point != null ? point.position : initialPosition,
            point != null ? point.rotation : initialRotation
        );

        health?.ResetHealth();
    }

    private void HandleDestroyed()
    {
        battleManager?.HandleDestroyed(this);
    }

    private void OnValidate()
    {
        unitCost = Mathf.Max(1, unitCost);
        respawnDelay = Mathf.Max(0f, respawnDelay);
    }
}
