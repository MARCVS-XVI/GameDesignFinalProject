using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AreaTrigger : MonoBehaviour
{
    [SerializeField]
    private GearPullManager _gearPullManager;

    [SerializeField]
    private GearPullManager.GameArea _area =
        GearPullManager.GameArea.CastleInterior;

    [Header("Optional Wave Manager")]
    [SerializeField]
    private WaveManager _waveManager;

    [SerializeField]
    private bool _debugLogs = true;

    private void Reset()
    {
        Collider collider = GetComponent<Collider>();

        if (collider != null)
            collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null &&
            other.GetComponentInParent<PlayerHealth>() == null)
        {
            return;
        }

        WaveManager manager = _waveManager != null
            ? _waveManager
            : WaveManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "[AreaTrigger] No WaveManager found.",
                this
            );
            return;
        }

        int levelIndex = (int)_area;

        bool expected =
            manager.IsAwaitingArea(levelIndex);

        if (_debugLogs)
        {
            Debug.Log(
                "[AreaTrigger] Player entered '" +
                name + "'. Destination=" + _area +
                ", expected by WaveManager=" + expected,
                this
            );
        }

        manager.PlayerEnteredArea(levelIndex);

        if (!expected)
            return;

        GearPullManager pull =
            _gearPullManager != null
                ? _gearPullManager
                : manager.gearPullManager;

        if (pull != null)
            pull.SetArea(_area);
    }
}