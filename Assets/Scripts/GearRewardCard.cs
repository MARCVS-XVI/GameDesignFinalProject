using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class GearRewardCard : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _kindText;
    [SerializeField] private TMP_Text _rarityText;
    [SerializeField] private TMP_Text _descriptionText;

    [Header("Image References")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _rarityBar;

    [Header("Reward Card Index")]
    [SerializeField] private int _rewardIndex;

    private GearReward _reward;
    private GearPullManager _manager;
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClicked);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnClicked);
    }

    public void Display(
        GearReward reward,
        GearPullManager manager)
    {
        _reward = reward;
        _manager = manager;

        if (_nameText != null)
            _nameText.text = reward.rewardName;

        if (_kindText != null)
            _kindText.text = reward.kind == RewardKind.Equipment
                ? reward.slot.ToString() : "Effect";

        if (_rarityText != null)
            _rarityText.text = reward.rarity.ToString();

        if (_descriptionText != null)
        {
            string details = reward.description ?? "";

            if (reward.bonusHP != 0)
                details += $"\nHP {reward.bonusHP:+#;-#;0}";

            if (reward.bonusAttack != 0)
                details += $"\nATK {reward.bonusAttack:+#;-#;0}";

            if (reward.bonusDefense != 0)
                details += $"\nDEF {reward.bonusDefense:+#;-#;0}";

            if (reward.specialEffect != GearSpecialEffect.None)
            {
                details +=
                    $"\n{FormatEffectName(reward.specialEffect)}: {reward.effectValue:0.#}%";
            }

            if (reward.restoreHP > 0)
                details += $"\nRestore {reward.restoreHP} HP now";

            if (reward.kind == RewardKind.Effect)
            {
                bool hasOngoingBonus =
                    reward.bonusHP != 0 ||
                    reward.bonusAttack != 0 ||
                    reward.bonusDefense != 0 ||
                    reward.specialEffect != GearSpecialEffect.None;

                if (hasOngoingBonus)
                {
                    switch (reward.durationType)
                    {
                        case GearEffectDuration.Permanent:
                            details += "\nDuration: Permanent";
                            break;

                        case GearEffectDuration.SecondsFromWaveStart:
                            details +=
                                $"\nDuration: {reward.durationSeconds:0.#}s next wave";
                            break;

                        case GearEffectDuration.NumberOfWaves:
                            details +=
                                $"\nDuration: {reward.durationWaves} wave(s), starting next wave";
                            break;
                    }
                }
            }

            _descriptionText.text = details;
        }

        if (_iconImage != null)
        {
            _iconImage.enabled = reward.icon != null;
            _iconImage.sprite = reward.icon;
        }

        if (_rarityBar != null)
        {
            switch (reward.rarity)
            {
                case RewardRarity.Common:
                    _rarityBar.color =
                        new Color32(150, 150, 150, 255);
                    break;

                case RewardRarity.Rare:
                    _rarityBar.color =
                        new Color32(44, 170, 220, 255);
                    break;

                case RewardRarity.Legendary:
                    _rarityBar.color =
                        new Color32(238, 180, 47, 255);
                    break;

                case RewardRarity.Mythic:
                    _rarityBar.color =
                        new Color32(180, 76, 225, 255);
                    break;
            }
        }
    }

    private static string FormatEffectName(
        GearSpecialEffect effect)
    {
        switch (effect)
        {
            case GearSpecialEffect.AttackPercent:
                return "ATK bonus";

            case GearSpecialEffect.DefensePercent:
                return "DEF bonus";

            case GearSpecialEffect.HealthPercent:
                return "Max HP bonus";

            case GearSpecialEffect.CriticalChance:
                return "Critical chance";

            case GearSpecialEffect.BurnChance:
                return "Burn chance";

            case GearSpecialEffect.PoisonChance:
                return "Poison chance";

            default:
                return effect.ToString();
        }
    }

    private void OnClicked()
    {
        if (_manager != null && _reward != null)
            _manager.SelectReward(_reward);
    }
}