using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AreaTrigger : MonoBehaviour
{
    public int levelIndex; // which gameLevels entry this room is
    public Door[] doorsToClose;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && WaveManager.Instance != null)
        {
            foreach (Door d in doorsToClose)
                if (d != null) d.Close();

            WaveManager.Instance.PlayerEnteredArea(levelIndex);
        }
    }

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }
}