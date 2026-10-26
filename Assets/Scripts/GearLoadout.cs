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


    public int MaxHP =>
        Mathf.Max(
            1,
            _baseMaxHP +
            SumBonuses(r => r.bonusHP)
        );

    public int Attack =>
        Mathf.Max(
            0,
            _baseAttack +
            SumBonuses(r => r.bonusAttack)
        );

    public int Defense =>
        Mathf.Max(
            0,
            _baseDefense +
            SumBonuses(r => r.bonusDefense)
        );

    public int CurrentHP { get; private set; }

    private void Awake()
    {
        CurrentHP = MaxHP;
    }

    public void Apply(GearReward reward)
    {
        if (reward == null)
            return;

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

        else
        {
            if (reward.bonusHP != 0 ||
                reward.bonusAttack != 0 ||
                reward.bonusDefense != 0)
            {
                _permanentEffects.Add(reward);
            }

            Debug.Log(
                "Effect applied: " + reward.rewardName
            );
        }


        int extraMaxHP =
            Mathf.Max(0, MaxHP - oldMaxHP);

        CurrentHP = Mathf.Clamp(
            CurrentHP + extraMaxHP + reward.restoreHP,
            0,
            MaxHP
        );


        Debug.Log(
            "Reward applied: " + reward.rewardName +
            " | HP " + CurrentHP + "/" + MaxHP +
            " | ATK " + Attack +
            " | DEF " + Defense
        );
    }

    private int SumBonuses(
        Func<GearReward, int> readStat)
    {
        int total = 0;

        if (_sword != null)
        {
            total += readStat(_sword);
        }

        if (_shield != null)
        {
            total += readStat(_shield);
        }

        if (_armor != null)
        {
            total += readStat(_armor);
        }

        foreach (GearReward effect in _permanentEffects)
        {
            total += readStat(effect);
        }

        return total;
    }
}