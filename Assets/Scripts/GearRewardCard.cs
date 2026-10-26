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
        {
            _button.onClick.RemoveListener(OnClicked);
        }
    }

    public void Display(
        GearReward reward,
        GearPullManager manager)
    {
        _reward = reward;
        _manager = manager;

        if (_nameText != null)
        {
            _nameText.text = reward.rewardName;
        }

        if (_kindText != null)
        {
            if (reward.kind == RewardKind.Equipment)
            {
                _kindText.text = reward.slot.ToString();
            }
            else
            {
                _kindText.text = "Effect";
            }
        }

        if (_rarityText != null)
        {
            _rarityText.text = reward.rarity.ToString();
        }

        if (_descriptionText != null)
        {
            string details = reward.description;

            if (reward.bonusHP != 0)
            {
                details +=
                    $"\nHP {reward.bonusHP:+#;-#;0}";
            }

            if (reward.bonusAttack != 0)
            {
                details +=
                    $"\nATK {reward.bonusAttack:+#;-#;0}";
            }

            if (reward.bonusDefense != 0)
            {
                details +=
                    $"\nDEF {reward.bonusDefense:+#;-#;0}";
            }

            if (reward.restoreHP > 0)
            {
                details +=
                    $"\nRestore {reward.restoreHP} HP";
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


    private void OnClicked()
    {
        if (_manager != null && _reward != null)
        {
            _manager.SelectReward(_rewardIndex);
        }
    }
}