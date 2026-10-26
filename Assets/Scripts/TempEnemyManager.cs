using UnityEngine;

// placeholder enemy manager
// walks toward the player and destroys itself when it touches the player.
public class TempEnemyManager : MonoBehaviour
{
    public int health = 3;
    public float moveSpeed = 3f;

    private Transform player;
    private bool isDead = false;

    void Start()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    void Update()
    {
    if (player == null) return;

    Vector3 target = new Vector3(player.position.x, transform.position.y, player.position.z);

    transform.position = Vector3.MoveTowards(
        transform.position, target, moveSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.EnemyDefeated();
        }

        Destroy(gameObject);
    }
}