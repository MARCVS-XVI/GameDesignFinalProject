using System;
using System.Collections.Generic;
using UnityEngine;

public class GearLoadout : MonoBehaviour
{

    [Header("Starting Stats")]

    [SerializeField] private int _baseMaxHP = 10;

    [SerializeField] private int _baseAttack = 5;

    [SerializeField] private int _baseDefense = 5;

    private GearReward _sword;
    private GearReward _shield;
    private GearReward _armor;

    private readonly List<GearReward> _permanentEffects =
        new List<GearReward>();

    public int CurrentHP { get; private set; }

    public int MaxHP
    {
        get
        {
            // Base HP + flat HP bonuses.
            int flatHP =
                _baseMaxHP +
                SumBonuses(r => r.bonusHP);

            // Percentage HP bonuses.
            float healthPercent =
                GetEffectValue(
                    GearSpecialEffect.HealthPercent
                );

            float multiplier =
                1f + healthPercent / 100f;

            int finalHP = Mathf.RoundToInt(
                flatHP * multiplier
            );

            return Mathf.Max(1, finalHP);
        }
    }

    public int Attack
    {
        get
        {
            // Base ATK + flat ATK bonuses.
            int flatAttack =
                _baseAttack +
                SumBonuses(r => r.bonusAttack);

            // Percentage attack bonuses.
            float attackPercent =
                GetEffectValue(
                    GearSpecialEffect.AttackPercent
                );

            float multiplier =
                1f + attackPercent / 100f;

            int finalAttack = Mathf.RoundToInt(
                flatAttack * multiplier
            );

            return Mathf.Max(0, finalAttack);
        }
    }

    public int Defense
    {
        get
        {
            // Base DEF + flat DEF bonuses.
            int flatDefense =
                _baseDefense +
                SumBonuses(r => r.bonusDefense);

            // Percentage defense bonuses.
            float defensePercent =
                GetEffectValue(
                    GearSpecialEffect.DefensePercent
                );

            float multiplier =
                1f + defensePercent / 100f;

            int finalDefense = Mathf.RoundToInt(
                flatDefense * multiplier
            );

            return Mathf.Max(0, finalDefense);
        }
    }

    private void Awake()
    {
        // Start the player at full health.
        CurrentHP = MaxHP;
    }

    public void Apply(GearReward reward)
    {
        if (reward == null)
            return;

        // Remember maximum HP before equipping.
        int oldMaxHP = MaxHP;

        if (reward.kind == RewardKind.Equipment)
        {
            switch (reward.slot)
            {
                case GearSlot.Sword:
                    _sword = reward;
                    break;

                case GearSlot.Shield:
                    _shield = reward;
                    break;

                case GearSlot.Armor:
                    _armor = reward;
                    break;
            }

            Debug.Log(
                "Equipped: " + reward.rewardName
            );
        }


        else if (reward.kind == RewardKind.Effect)
        {
            // Store effects that modify stats or
            // provide permanent special abilities.
            if (reward.bonusHP != 0 ||
                reward.bonusAttack != 0 ||
                reward.bonusDefense != 0 ||
                reward.specialEffect !=
                    GearSpecialEffect.None)
            {
                _permanentEffects.Add(reward);
            }

            Debug.Log(
                "Effect applied: " + reward.rewardName
            );
        }

        // Recalculate max health after reward.
        int newMaxHP = MaxHP;

        // Gain the HP added by an upgrade.
        int extraMaxHP = Mathf.Max(
            0,
            newMaxHP - oldMaxHP
        );

        // Add extra HP and immediate healing.
        CurrentHP = Mathf.Clamp(
            CurrentHP + extraMaxHP + reward.restoreHP,
            0,
            newMaxHP
        );

        Debug.Log(
            "Reward applied: " + reward.rewardName +
            " | HP " + CurrentHP + "/" + MaxHP +
            " | ATK " + Attack +
            " | DEF " + Defense
        );

        // Display active percentage bonuses.
        Debug.Log(
            "ATK Bonus: " +
            GetEffectValue(
                GearSpecialEffect.AttackPercent
            ) + "%" +
            " | DEF Bonus: " +
            GetEffectValue(
                GearSpecialEffect.DefensePercent
            ) + "%" +
            " | HP Bonus: " +
            GetEffectValue(
                GearSpecialEffect.HealthPercent
            ) + "%"
        );
    }

    public float GetEffectValue(
        GearSpecialEffect effectType)
    {
        float total = 0f;

        if (effectType == GearSpecialEffect.None)
            return 0f;

        // Check sword effects.
        if (_sword != null &&
            _sword.specialEffect == effectType)
        {
            total += _sword.effectValue;
        }

        // Check shield effects.
        if (_shield != null &&
            _shield.specialEffect == effectType)
        {
            total += _shield.effectValue;
        }

        // Check armor effects.
        if (_armor != null &&
            _armor.specialEffect == effectType)
        {
            total += _armor.effectValue;
        }

        // Check permanent effects.
        foreach (GearReward effect in _permanentEffects)
        {
            if (effect != null &&
                effect.specialEffect == effectType)
            {
                total += effect.effectValue;
            }
        }

        return total;
    }

    private int SumBonuses(
        Func<GearReward, int> readStat)
    {
        int total = 0;

        // Sword bonuses.
        if (_sword != null)
        {
            total += readStat(_sword);
        }

        // Shield bonuses.
        if (_shield != null)
        {
            total += readStat(_shield);
        }

        // Armor bonuses.
        if (_armor != null)
        {
            total += readStat(_armor);
        }

        // Permanent effect bonuses.
        foreach (GearReward effect in _permanentEffects)
        {
            if (effect != null)
            {
                total += readStat(effect);
            }
        }

        return total;
    }
}