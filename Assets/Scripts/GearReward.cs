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
    AttackSpeed,
    Thorns,
    BurnChance,
    PoisonChance,

    // Percentage-based stat bonuses
    AttackPercent,
    DefensePercent,
    HealthPercent
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

    // Optional 3D model for equipment.
    public GameObject equipmentPrefab;


    [Header("Flat Stat Bonuses")]

    public int bonusHP = 0;

    public int bonusAttack = 0;

    public int bonusDefense = 0;


    [Header("Special Effect")]

    public GearSpecialEffect specialEffect =
        GearSpecialEffect.None;

    [Tooltip(
        "Enter percentages as whole numbers. " +
        "For example, enter 15 for 15%."
    )]
    [Min(0f)]
    public float effectValue = 0f;

    [Header("Immediate Effect")]

    [Min(0)]
    public int restoreHP = 0;
}