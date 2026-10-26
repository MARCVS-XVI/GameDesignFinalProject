using Unity.VisualScripting;
using UnityEngine;

public enum RewardKind { Equipment, Effect }
public enum GearSlot { Sword, Shield, Armor }
public enum RewardRarity { Common, Rare, Legendary, Mythic }
[CreateAssetMenu(fileName = "NewGearReward", menuName = "Gear/Effect Reward")]
public class GearReward : ScriptableObject
{
    public string rewardName = "New Reward";
    [TextArea(2, 4)] public string description;
    public RewardKind kind = RewardKind.Equipment;
    public RewardRarity rarity = RewardRarity.Common;
    public GearSlot slot = GearSlot.Sword;
    public Sprite icon;

    [Header("Stat Bonuses")]
    public int bonusHP;
    public int bonusAttack;
    public int bonusDefense;

    [Header("Immediate Effects")]
    [Min(0)] public int restoreHP;
}
