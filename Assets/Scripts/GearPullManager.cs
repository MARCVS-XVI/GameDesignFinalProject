using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GearPullManager : MonoBehaviour
{
    public enum GameArea
    {
        CastleExterior,
        CastleInterior,
        ThroneRoom
    }

    [Serializable]
    private class RarityRates
    {
        [Range(0, 100)] public int common;
        [Range(0, 100)] public int rare;
        [Range(0, 100)] public int legendary;
        [Range(0, 100)] public int mythic;

        public RarityRates(
            int common,
            int rare,
            int legendary,
            int mythic)
        {
            this.common = common;
            this.rare = rare;
            this.legendary = legendary;
            this.mythic = mythic;
        }

        public int GetWeight(RewardRarity rarity)
        {
            switch (rarity)
            {
                case RewardRarity.Common:
                    return common;

                case RewardRarity.Rare:
                    return rare;

                case RewardRarity.Legendary:
                    return legendary;

                case RewardRarity.Mythic:
                    return mythic;

                default:
                    return 0;
            }
        }

        public int Total =>
            common + rare + legendary + mythic;
    }

    [Header("UI")]
    [SerializeField] private GameObject _gearPullPanel;
    [SerializeField] private GearRewardCard[] _cards;
    [SerializeField] private bool _pauseGameplay = true;

    [Header("Rewards")]
    [SerializeField] private GearReward[] _rewardPool;
    [SerializeField] private GearLoadout _demoLoadout;
    [SerializeField] private bool _offerGearAndEffect = true;

    [Header("Unity Scene Names")]
    [SerializeField]
    private string _castleExteriorScene = "CastleExterior";

    [SerializeField]
    private string _castleInteriorScene = "CastleInterior";

    [SerializeField]
    private string _throneRoomScene = "ThroneRoom";

    [Header("Castle Exterior Rarity Chances (%)")]
    [SerializeField]
    private RarityRates _exteriorRates =
        new RarityRates(55, 30, 15, 0);

    [Header("Castle Interior Rarity Chances (%)")]
    [SerializeField]
    private RarityRates _interiorRates =
        new RarityRates(45, 40, 10, 5);

    [Header("Throne Room Rarity Chances (%)")]
    [SerializeField]
    private RarityRates _throneRates =
        new RarityRates(30, 20, 30, 20);

    [Header("Skipping")]
    [Min(1)]
    [SerializeField]
    private int _skipsForGuaranteedHighRarity = 2;
    public event Action<GearReward> RewardSelected;
    public event Action<bool> PullClosed;
    public bool IsOpen => _isOpen;
    public int ConsecutiveSkips => _consecutiveSkips;
    private readonly List<GearReward> _offered =
        new List<GearReward>();
    private bool _isOpen;
    private int _consecutiveSkips;
    private float _oldTimeScale = 1f;
    private bool _hasAreaOverride;
    private GameArea _areaOverride;

    public void SetArea(GameArea area)
    {
        _hasAreaOverride = true;
        _areaOverride = area;
    }

    public void ClearAreaOverride()
    {
        _hasAreaOverride = false;
    }

    private void Awake()
    {
        if (_gearPullPanel != null)
        {
            _gearPullPanel.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (_isOpen && _pauseGameplay)
        {
            Time.timeScale = _oldTimeScale;
        }

        _isOpen = false;
    }

    public void OpenPull()
    {
        if (_isOpen)
            return;

        if (_gearPullPanel == null ||
            _cards == null ||
            _cards.Length == 0)
        {
            Debug.LogError(
                "GearPullManager: Assign the panel and cards."
            );

            return;
        }

        if (!TryGetArea(out GameArea area))
            return;

        RarityRates rates = GetRates(area);

        if (rates.Total != 100)
        {
            Debug.LogWarning(
                "GearPullManager: Area rates do not add " +
                "up to 100. They will be treated as " +
                "relative weights."
            );
        }

        List<GearReward> available =
            new List<GearReward>();

        if (_rewardPool != null)
        {
            foreach (GearReward reward in _rewardPool)
            {
                if (reward != null &&
                    !available.Contains(reward))
                {
                    available.Add(reward);
                }
            }
        }

        if (available.Count == 0)
        {
            Debug.LogError(
                "GearPullManager: Add GearReward " +
                "assets to the Reward Pool."
            );

            return;
        }

        if (!available.Exists(
            reward => rates.GetWeight(reward.rarity) > 0))
        {
            Debug.LogError(
                "GearPullManager: No eligible rewards " +
                "for area " + area
            );

            return;
        }

        _offered.Clear();

        if (_consecutiveSkips >=
            _skipsForGuaranteedHighRarity)
        {
            AddOption(
                available,
                rates,
                null,
                true
            );

            if (_offered.Count == 0)
            {
                Debug.LogWarning(
                    "GearPullManager: No eligible " +
                    "Legendary or Mythic reward found."
                );
            }
        }

        if (_offerGearAndEffect)
        {
            if (!ContainsKind(
                _offered,
                RewardKind.Equipment))
            {
                AddOption(
                    available,
                    rates,
                    RewardKind.Equipment
                );
            }

            if (!ContainsKind(
                _offered,
                RewardKind.Effect))
            {
                AddOption(
                    available,
                    rates,
                    RewardKind.Effect
                );
            }
        }

        while (_offered.Count < _cards.Length &&
               available.Count > 0)
        {
            int previousCount = _offered.Count;

            AddOption(available, rates);

            if (_offered.Count == previousCount)
                break;
        }

        if (_offered.Count == 0)
        {
            Debug.LogError(
                "GearPullManager: Failed to roll rewards."
            );

            return;
        }

        for (int i = 0; i < _cards.Length; i++)
        {
            if (_cards[i] == null)
                continue;

            bool hasReward = i < _offered.Count;

            _cards[i].gameObject.SetActive(hasReward);

            if (hasReward)
            {
                _cards[i].Display(
                    _offered[i],
                    this
                );
            }
        }

        Debug.Log("Gear Pull area: " + area);

        _oldTimeScale = Time.timeScale;

        _isOpen = true;

        _gearPullPanel.SetActive(true);

        if (_pauseGameplay)
        {
            Time.timeScale = 0f;
        }
    }

    public void SelectReward(GearReward reward)
    {
        if (!_isOpen ||
            reward == null ||
            !_offered.Contains(reward))
        {
            return;
        }

        _consecutiveSkips = 0;

        if (_demoLoadout != null)
        {
            _demoLoadout.Apply(reward);
        }

        Debug.Log(
            "Chosen reward: " + reward.rewardName
        );

        RewardSelected?.Invoke(reward);

        ClosePull(false);
    }

    public void SkipPull()
    {
        if (!_isOpen)
            return;

        _consecutiveSkips++;

        Debug.Log(
            "Reward skipped. Skip streak: " +
            _consecutiveSkips
        );

        ClosePull(true);
    }

    private void ClosePull(bool skipped)
    {
        _isOpen = false;

        _gearPullPanel.SetActive(false);

        if (_pauseGameplay)
        {
            Time.timeScale = _oldTimeScale;
        }

        PullClosed?.Invoke(skipped);
    }

    private void AddOption(
        List<GearReward> available,
        RarityRates rates,
        RewardKind? requiredKind = null,
        bool highRarityOnly = false)
    {
        if (_offered.Count >= _cards.Length)
            return;

        GearReward chosen = DrawReward(
            available,
            rates,
            requiredKind,
            highRarityOnly
        );

        if (chosen == null)
            return;

        _offered.Add(chosen);
        available.Remove(chosen);
    }

    private GearReward DrawReward(
        List<GearReward> source,
        RarityRates rates,
        RewardKind? requiredKind,
        bool highRarityOnly)
    {
        List<GearReward> eligible =
            new List<GearReward>();

        foreach (GearReward reward in source)
        {
            if (requiredKind.HasValue &&
                reward.kind != requiredKind.Value)
            {
                continue;
            }

            if (highRarityOnly &&
                reward.rarity < RewardRarity.Legendary)
            {
                continue;
            }

            if (rates.GetWeight(reward.rarity) <= 0)
            {
                continue;
            }

            eligible.Add(reward);
        }

        if (eligible.Count == 0)
            return null;

        int totalWeight = 0;

        for (int i = 0; i < 4; i++)
        {
            RewardRarity rarity = (RewardRarity)i;

            if (HasRarity(eligible, rarity))
            {
                totalWeight += rates.GetWeight(rarity);
            }
        }

        if (totalWeight <= 0)
            return null;

        float roll =
            UnityEngine.Random.value * totalWeight;

        for (int i = 0; i < 4; i++)
        {
            RewardRarity rarity = (RewardRarity)i;

            List<GearReward> matchingTier =
                new List<GearReward>();

            foreach (GearReward reward in eligible)
            {
                if (reward.rarity == rarity)
                {
                    matchingTier.Add(reward);
                }
            }

            if (matchingTier.Count == 0)
                continue;

            roll -= rates.GetWeight(rarity);

            if (roll < 0f)
            {
                return matchingTier[
                    UnityEngine.Random.Range(
                        0,
                        matchingTier.Count
                    )
                ];
            }
        }

        return eligible[eligible.Count - 1];
    }

    private static bool HasRarity(
        List<GearReward> rewards,
        RewardRarity rarity)
    {
        foreach (GearReward reward in rewards)
        {
            if (reward.rarity == rarity)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsKind(
        List<GearReward> rewards,
        RewardKind kind)
    {
        foreach (GearReward reward in rewards)
        {
            if (reward.kind == kind)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetArea(out GameArea area)
    {
        if (_hasAreaOverride)
        {
            area = _areaOverride;
            return true;
        }

        string sceneName =
            SceneManager.GetActiveScene().name;

        if (sceneName == _castleExteriorScene)
        {
            area = GameArea.CastleExterior;
            return true;
        }

        if (sceneName == _castleInteriorScene)
        {
            area = GameArea.CastleInterior;
            return true;
        }

        if (sceneName == _throneRoomScene)
        {
            area = GameArea.ThroneRoom;
            return true;
        }

        area = default(GameArea);

        Debug.LogError(
            "GearPullManager: Scene '" + sceneName +
            "' is not mapped to an area. " +
            "Set the scene names in the Inspector " +
            "or call SetArea()."
        );

        return false;
    }

    private RarityRates GetRates(GameArea area)
    {
        switch (area)
        {
            case GameArea.CastleExterior:
                return _exteriorRates;

            case GameArea.CastleInterior:
                return _interiorRates;

            case GameArea.ThroneRoom:
                return _throneRates;

            default:
                return _exteriorRates;
        }
    }
}