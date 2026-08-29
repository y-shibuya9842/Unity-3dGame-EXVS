using UnityEngine;

public class BattleParticipant : MonoBehaviour
{
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
        if (health != null)
        {
            health.OnDestroyed += HandleDestroyed;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDestroyed -= HandleDestroyed;
        }
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
