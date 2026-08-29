using System;
using System.Collections;
using UnityEngine;

public enum BattleTeam
{
    Player,
    Enemy
}

public class BattleManager : MonoBehaviour
{
    [Header("Battle Rule")]
    [SerializeField] private int initialTeamCost = 6000;
    [SerializeField] private float timeLimit = 180f;

    private int playerTeamCost;
    private int enemyTeamCost;
    private float remainingTime;
    private bool battleEnded;

    public int PlayerTeamCost => playerTeamCost;
    public int EnemyTeamCost => enemyTeamCost;
    public float RemainingTime => remainingTime;
    public bool BattleEnded => battleEnded;

    public event Action<BattleTeam, int> OnTeamCostChanged;
    public event Action<float> OnTimeChanged;
    public event Action<BattleTeam> OnBattleEnded;

    private void Awake()
    {
        playerTeamCost = Mathf.Max(1, initialTeamCost);
        enemyTeamCost = Mathf.Max(1, initialTeamCost);
        remainingTime = Mathf.Max(1f, timeLimit);
    }

    private void Update()
    {
        if (battleEnded)
        {
            return;
        }

        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        OnTimeChanged?.Invoke(remainingTime);

        if (remainingTime <= 0f)
        {
            BattleTeam winner = playerTeamCost >= enemyTeamCost
                ? BattleTeam.Player
                : BattleTeam.Enemy;
            EndBattle(winner);
        }
    }

    public void HandleDestroyed(BattleParticipant participant)
    {
        if (participant == null || battleEnded)
        {
            return;
        }

        int remainingCost = ReduceTeamCost(participant.Team, participant.UnitCost);

        if (remainingCost <= 0)
        {
            BattleTeam winner = participant.Team == BattleTeam.Player
                ? BattleTeam.Enemy
                : BattleTeam.Player;
            EndBattle(winner);
            return;
        }

        float respawnHealthRatio = Mathf.Clamp01(
            (float)remainingCost / participant.UnitCost
        );
        StartCoroutine(RespawnRoutine(participant, respawnHealthRatio));
    }

    private int ReduceTeamCost(BattleTeam team, int amount)
    {
        if (team == BattleTeam.Player)
        {
            playerTeamCost = Mathf.Max(0, playerTeamCost - amount);
            OnTeamCostChanged?.Invoke(team, playerTeamCost);
            return playerTeamCost;
        }

        enemyTeamCost = Mathf.Max(0, enemyTeamCost - amount);
        OnTeamCostChanged?.Invoke(team, enemyTeamCost);
        return enemyTeamCost;
    }

    private IEnumerator RespawnRoutine(
        BattleParticipant participant,
        float healthRatio)
    {
        yield return new WaitForSeconds(participant.RespawnDelay);

        if (!battleEnded)
        {
            participant.Respawn(healthRatio);
        }
    }

    private void EndBattle(BattleTeam winner)
    {
        if (battleEnded)
        {
            return;
        }

        battleEnded = true;
        OnBattleEnded?.Invoke(winner);
    }

    private void OnValidate()
    {
        initialTeamCost = Mathf.Max(1, initialTeamCost);
        timeLimit = Mathf.Max(1f, timeLimit);
    }
}
