using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GearPullManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _openGearPullContainer;
    [SerializeField] private GameObject _gearPullPanel;
    [SerializeField] private Button[] _rewardButtons;
    [SerializeField] private Button _skipButton;
    [SerializeField] private Button _openButton;

    private readonly string[] _rewards =
    {
        "Iron Sword\nGear\n+3 Attack",

        "Wooden Shield\nGear\n+2 Defense",

        "Healing Blessing\nEffect\nRestore 5 HP"
    };

    private void Awake()
    {
        if (_openGearPullContainer != null)
            _openGearPullContainer.SetActive(false);

        if (_gearPullPanel != null)
            _gearPullPanel.SetActive(false);

        for (int i = 0; i < _rewardButtons.Length; i++)
        {
            if (_rewardButtons[i] == null)
                continue;

            if (i >= _rewards.Length)
            {
                _rewardButtons[i].gameObject.SetActive(false);
                continue;
            }

            int index = i;

            TMP_Text rewardText =
                _rewardButtons[i].GetComponentInChildren<TMP_Text>(true);

            if (rewardText != null)
                rewardText.text = _rewards[i];

            _rewardButtons[i].onClick.AddListener(
                () => SelectReward(index)
            );
        }

        if (_skipButton != null)
            _skipButton.onClick.AddListener(SkipReward);

        if (_openButton != null)
            _openButton.onClick.AddListener(OpenPull);
    }
    public void OpenPull()
    {
        if (WaveManager.Instance == null ||
            !WaveManager.Instance.IsWaitingForGearSelection)
        {
            return;
        }

        if (_openGearPullContainer != null)
            _openGearPullContainer.SetActive(false);

        if (_gearPullPanel != null)
            _gearPullPanel.SetActive(true);
    }
    public void SelectReward(int index)
    {
        Debug.Log("Selected Reward: " + _rewards[index]);

        ClosePull();
    }
    public void SkipReward()
    {
        Debug.Log("Player skipped the Gear Pull!");

        ClosePull();
    }
    private void ClosePull()
    {
        if (_gearPullPanel != null)
            _gearPullPanel.SetActive(false);

        if (_openGearPullContainer != null)
            _openGearPullContainer.SetActive(false);

        if (WaveManager.Instance != null)
            WaveManager.Instance.ContinueToNextWave();
    }
    public void ShowOpenButton()
    {
        if (_openGearPullContainer != null)
            _openGearPullContainer.SetActive(true);
    }
public void HideOpenButton()
    {
        if (_openGearPullContainer != null)
            _openGearPullContainer.SetActive(false);
    }
}