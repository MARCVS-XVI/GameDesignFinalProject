using System;
using System.Collections.Generic;
using UnityEngine;

public class GearLoadout : MonoBehaviour
{
    [Header("Starting Stats")]
    [SerializeField] private int _baseMaxHP = 10;
    [SerializeField] private int _baseAttack = 5;
    [SerializeField] private int _baseDefense = 5;

    [Header("Player Integration")]
    [Tooltip("PlayerHealth on the real player.")]
    [SerializeField] private PlayerHealth _playerHealth;

    [Tooltip("Weapon hitboxes. If empty, searches children.")]
    [SerializeField] private GearDamage[] _weaponDamageSources;

    private GearReward _sword;
    private GearReward _shield;
    private GearReward _armor;

    private class EffectInstance
    {
        public GearReward reward;

        public bool active;
        public bool waitingForWave;

        public float remainingSeconds;
        public int remainingWaves;
    }

    private readonly List<EffectInstance> _effects =
        new List<EffectInstance>();

    private bool _waveInProgress;
    private int _standaloneCurrentHP;

    public bool WaveInProgress => _waveInProgress;

    public int CurrentHP =>
        _playerHealth != null
            ? Mathf.RoundToInt(
                _playerHealth.CurrentHealth
            )
            : _standaloneCurrentHP;

    public int MaxHP
    {
        get
        {
            int flat =
                _baseMaxHP +
                SumBonuses(r => r.bonusHP);

            float percent = GetEffectValue(
                GearSpecialEffect.HealthPercent
            );

            return Mathf.Max(
                1,
                Mathf.RoundToInt(
                    flat * (1f + percent / 100f)
                )
            );
        }
    }

    public int Attack
    {
        get
        {
            int flat =
                _baseAttack +
                SumBonuses(r => r.bonusAttack);

            float percent = GetEffectValue(
                GearSpecialEffect.AttackPercent
            );

            return Mathf.Max(
                0,
                Mathf.RoundToInt(
                    flat * (1f + percent / 100f)
                )
            );
        }
    }

    public int Defense
    {
        get
        {
            int flat =
                _baseDefense +
                SumBonuses(r => r.bonusDefense);

            float percent = GetEffectValue(
                GearSpecialEffect.DefensePercent
            );

            return Mathf.Max(
                0,
                Mathf.RoundToInt(
                    flat * (1f + percent / 100f)
                )
            );
        }
    }

    private void Awake()
    {
        ResolvePlayerHealth();

        if (_playerHealth != null)
        {
            _baseMaxHP = Mathf.Max(
                1,
                Mathf.RoundToInt(
                    _playerHealth.maxHealth
                )
            );
        }
        else
        {
            Debug.LogWarning(
                "[GearLoadout] No PlayerHealth found for '" +
                name + "'. HP buffs will only change " +
                "the internal demo HP value.",
                this
            );
        }

        _standaloneCurrentHP = MaxHP;
    }

    private void ResolvePlayerHealth()
    {
        PlayerHealth actual =
            GetComponent<PlayerHealth>();

        if (actual == null)
        {
            actual =
                GetComponentInParent<PlayerHealth>();
        }

        if (actual == null)
        {
            actual =
                GetComponentInChildren<PlayerHealth>(true);
        }

        if (actual == null)
            return;

        if (_playerHealth != null &&
            _playerHealth != actual)
        {
            Debug.LogWarning(
                "[GearLoadout] PlayerHealth reference " +
                "pointed at a different object; " +
                "using the player's actual health.",
                this
            );
        }

        _playerHealth = actual;
    }

    private void Start()
    {
        RefreshWeaponDamage();
    }

    private void Update()
    {
        if (!_waveInProgress)
            return;

        int previousMaxHP = MaxHP;
        bool expiredSomething = false;

        for (int i = _effects.Count - 1; i >= 0; i--)
        {
            EffectInstance instance = _effects[i];

            if (!instance.active ||
                instance.reward.durationType !=
                    GearEffectDuration.SecondsFromWaveStart)
            {
                continue;
            }

            instance.remainingSeconds -= Time.deltaTime;

            if (instance.remainingSeconds > 0f)
                continue;

            Debug.Log(
                "Timed effect expired: " +
                instance.reward.rewardName,
                this
            );

            _effects.RemoveAt(i);
            expiredSomething = true;
        }

        if (expiredSomething)
        {
            RefreshStats(previousMaxHP);
        }
    }

    public void Apply(GearReward reward)
    {
        if (reward == null)
            return;

        ResolvePlayerHealth();

        int previousMaxHP = MaxHP;

        Debug.Log(
            "[GearLoadout] Applying '" +
            reward.rewardName +
            "' (kind=" + reward.kind +
            ", duration=" + reward.durationType +
            ") to '" + name + "'.",
            this
        );

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

                default:
                    Debug.LogWarning(
                        "Unsupported gear slot for " +
                        reward.rewardName,
                        this
                    );
                    return;
            }
        }
        else if (reward.kind == RewardKind.Effect &&
                 HasOngoingBonus(reward))
        {
            EffectInstance instance =
                new EffectInstance();

            instance.reward = reward;

            if (reward.durationType ==
                GearEffectDuration.Permanent)
            {
                instance.active = true;
            }
            else
            {
                instance.waitingForWave = true;
                instance.active = false;

                instance.remainingSeconds = Mathf.Max(
                    0.1f,
                    reward.durationSeconds
                );

                instance.remainingWaves = Mathf.Max(
                    1,
                    reward.durationWaves
                );
            }

            _effects.Add(instance);

            Debug.Log(
                "Effect obtained: " +
                reward.rewardName +
                (instance.waitingForWave
                    ? " (activates next wave)"
                    : " (active now)"),
                this
            );
        }

        RefreshStats(
            previousMaxHP,
            reward.restoreHP
        );
    }


    [ContextMenu("Test/Start Wave")]
    public void OnWaveStarted()
    {
        if (!Application.isPlaying)
            return;

        if (_waveInProgress)
        {
            Debug.LogWarning(
                "GearLoadout: Wave start received " +
                "while a wave is active.",
                this
            );
            return;
        }

        int previousMaxHP = MaxHP;

        _waveInProgress = true;

        foreach (EffectInstance instance in _effects)
        {
            if (!instance.waitingForWave)
                continue;

            instance.waitingForWave = false;
            instance.active = true;

            if (instance.reward.durationType ==
                GearEffectDuration.SecondsFromWaveStart)
            {
                instance.remainingSeconds = Mathf.Max(
                    0.1f,
                    instance.reward.durationSeconds
                );
            }
            else if (instance.reward.durationType ==
                     GearEffectDuration.NumberOfWaves)
            {
                instance.remainingWaves = Mathf.Max(
                    1,
                    instance.reward.durationWaves
                );
            }

            Debug.Log(
                "Effect activated: " +
                instance.reward.rewardName,
                this
            );
        }

        RefreshStats(previousMaxHP);
    }


    [ContextMenu("Test/End Wave")]
    public void OnWaveEnded()
    {
        if (!Application.isPlaying ||
            !_waveInProgress)
        {
            return;
        }

        int previousMaxHP = MaxHP;

        _waveInProgress = false;

        for (int i = _effects.Count - 1; i >= 0; i--)
        {
            EffectInstance instance = _effects[i];

            if (!instance.active)
                continue;

            if (instance.reward.durationType ==
                GearEffectDuration.SecondsFromWaveStart)
            {
                Debug.Log(
                    "Wave ended; timed effect expired: " +
                    instance.reward.rewardName,
                    this
                );

                _effects.RemoveAt(i);
            }
            else if (instance.reward.durationType ==
                     GearEffectDuration.NumberOfWaves)
            {
                instance.remainingWaves--;

                if (instance.remainingWaves <= 0)
                {
                    Debug.Log(
                        "Wave-count effect expired: " +
                        instance.reward.rewardName,
                        this
                    );

                    _effects.RemoveAt(i);
                }
            }
        }

        RefreshStats(previousMaxHP);
    }

    private void RefreshStats(
        int previousMaxHP,
        int instantHealing = 0)
    {
        ResolvePlayerHealth();

        int newMaxHP = MaxHP;

        int gainedMaxHP = Mathf.Max(
            0,
            newMaxHP - previousMaxHP
        );

        if (_playerHealth != null)
        {
            float beforeRealHP =
                _playerHealth.maxHealth;

            _playerHealth.SetMaxHealth(
                newMaxHP,
                false
            );

            Debug.Log(
                "[GearLoadout] PlayerHealth.maxHealth " +
                beforeRealHP + " -> " +
                _playerHealth.maxHealth +
                " on '" + _playerHealth.name + "'.",
                this
            );

            if (gainedMaxHP > 0 ||
                instantHealing > 0)
            {
                _playerHealth.Heal(
                    gainedMaxHP + instantHealing
                );
            }
        }
        else
        {
            _standaloneCurrentHP = Mathf.Clamp(
                _standaloneCurrentHP +
                gainedMaxHP +
                instantHealing,
                0,
                newMaxHP
            );

            Debug.LogWarning(
                "[GearLoadout] No PlayerHealth linked. " +
                "Real player HP was not updated!",
                this
            );
        }

        RefreshWeaponDamage();

        Debug.Log(
            "GearLoadout stats | HP " +
            CurrentHP + "/" + MaxHP +
            " | ATK " + Attack +
            " | DEF " + Defense,
            this
        );
    }

    public void RefreshWeaponDamage()
    {
        GearDamage[] sources =
            _weaponDamageSources;

        if (sources == null ||
            sources.Length == 0)
        {
            sources =
                GetComponentsInChildren<GearDamage>(true);
        }

        if (sources.Length == 0)
        {
            Debug.LogWarning(
                "[GearLoadout] No GearDamage components " +
                "found under '" + name +
                "'. Assign Weapon Damage Sources " +
                "to make ATK affect hits.",
                this
            );
        }

        foreach (GearDamage source in sources)
        {
            if (source != null)
            {
                source.damage = Attack;
            }
        }
    }

    public float GetEffectValue(
        GearSpecialEffect effectType)
    {
        if (effectType == GearSpecialEffect.None)
            return 0f;

        float total = 0f;

        if (_sword != null &&
            _sword.specialEffect == effectType)
        {
            total += _sword.effectValue;
        }

        if (_shield != null &&
            _shield.specialEffect == effectType)
        {
            total += _shield.effectValue;
        }

        if (_armor != null &&
            _armor.specialEffect == effectType)
        {
            total += _armor.effectValue;
        }

        foreach (EffectInstance instance in _effects)
        {
            if (instance.active &&
                instance.reward != null &&
                instance.reward.specialEffect == effectType)
            {
                total += instance.reward.effectValue;
            }
        }

        return total;
    }

    private int SumBonuses(
        Func<GearReward, int> readStat)
    {
        int total = 0;

        if (_sword != null)
            total += readStat(_sword);

        if (_shield != null)
            total += readStat(_shield);

        if (_armor != null)
            total += readStat(_armor);

        foreach (EffectInstance instance in _effects)
        {
            if (instance.active &&
                instance.reward != null)
            {
                total += readStat(instance.reward);
            }
        }

        return total;
    }

    private static bool HasOngoingBonus(
        GearReward reward)
    {
        return reward.bonusHP != 0 ||
               reward.bonusAttack != 0 ||
               reward.bonusDefense != 0 ||
               reward.specialEffect != GearSpecialEffect.None;
    }
}