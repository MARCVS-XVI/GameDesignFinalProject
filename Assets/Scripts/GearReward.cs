using UnityEngine;

public enum RewardKind
{
    Equipment,
    Effect
}

public enum GearSlot
{
    Sword,
    Shield,
    Armor,
    Effect
}

public enum RewardRarity
{
    Common,
    Rare,
    Legendary,
    Mythic
}

public enum GearSpecialEffect
{
    None,
    Lifesteal,
    CriticalChance,
    Thorns,
    BurnChance,
    PoisonChance,
    AttackPercent,
    DefensePercent,
    HealthPercent
}

public enum GearEffectDuration
{
    Permanent,
    SecondsFromWaveStart,
    NumberOfWaves
}

[CreateAssetMenu(
    fileName = "NewGearReward",
    menuName = "Knight Roguelike/Gear Reward"
)]
public class GearReward : ScriptableObject
{
    [Header("Basic Information")]
    public string rewardName = "New Reward";

    [TextArea(2, 4)]
    public string description;

    public RewardKind kind = RewardKind.Equipment;
    public RewardRarity rarity = RewardRarity.Common;
    public GearSlot slot = GearSlot.Sword;

    [Header("Visuals")]
    public Sprite icon;
    public GameObject equipmentPrefab;

    [Header("Flat Stat Bonuses")]
    public int bonusHP = 0;
    public int bonusAttack = 0;
    public int bonusDefense = 0;

    [Header("Special Effect")]
    public GearSpecialEffect specialEffect =
        GearSpecialEffect.None;

    [Tooltip("Percent values use whole numbers: 15 means 15%.")]
    [Min(0f)]
    public float effectValue = 0f;

    [Header("Effect Duration (Kind = Effect only)")]
    public GearEffectDuration durationType =
        GearEffectDuration.Permanent;

    [Tooltip("Active from the start of the next wave for this many seconds, or until that wave ends.")]
    [Min(0.1f)]
    public float durationSeconds = 15f;

    [Tooltip("Active for this many entire waves, beginning with the next wave.")]
    [Min(1)]
    public int durationWaves = 2;

    [Header("Immediate Effect")]
    [Min(0)]
    public int restoreHP = 0;
}