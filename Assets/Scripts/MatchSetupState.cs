using UnityEngine;

public enum TrainingEnemyMovement
{
    Stop,
    Move,
    JumpLow,
    JumpMiddle,
    JumpHigh,
    Avoid
}

public enum MatchMechSlot
{
    Player,
    Partner,
    EnemyOne,
    EnemyTwo
}

public static class MatchSetupState
{
    public const string GameSettingsSceneName = "GameSettingsScene";
    public const string CharacterSelectSceneName = "CharacterSelectScene";
    public const string DefaultMechId = "Gundam";

    private const string MovementKey = "Match.EnemyMovement";
    private const string AutoRecoveryKey = "Match.AutoRecovery";
    private const string AutoGuardKey = "Match.AutoGuard";
    private const string MechKeyPrefix = "Match.Mech.";

    public static TrainingEnemyMovement EnemyMovement
    {
        get => (TrainingEnemyMovement)PlayerPrefs.GetInt(
            MovementKey,
            (int)TrainingEnemyMovement.Stop
        );
        set
        {
            PlayerPrefs.SetInt(MovementKey, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static bool AutoRecovery
    {
        get => PlayerPrefs.GetInt(AutoRecoveryKey, 0) != 0;
        set
        {
            PlayerPrefs.SetInt(AutoRecoveryKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool AutoGuard
    {
        get => PlayerPrefs.GetInt(AutoGuardKey, 0) != 0;
        set
        {
            PlayerPrefs.SetInt(AutoGuardKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static string GetSelectedMech(MatchMechSlot slot)
    {
        return PlayerPrefs.GetString(MechKeyPrefix + slot, DefaultMechId);
    }

    public static void SetSelectedMech(MatchMechSlot slot, string mechId)
    {
        string selectedId = string.IsNullOrWhiteSpace(mechId)
            ? DefaultMechId
            : mechId;
        PlayerPrefs.SetString(MechKeyPrefix + slot, selectedId);
        PlayerPrefs.Save();
    }
}
