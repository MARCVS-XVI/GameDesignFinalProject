using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Stats (Prefab Defaults)")]
    [Min(1f)] public float maxHealth = 3f;
    public float moveSpeed = 3f;

    [Header("Attacking the Player")]
    [Min(0f)] public float contactDamage = 1f;
    [Min(0f)] public float attackCooldown = 1f;
    public bool dieOnContact = false;

    [Header("Taking Damage from Gear")]
    [Min(0f)] public float defaultGearDamage = 1f;
    [Min(0f)] public float hitCooldown = 0.4f;

    [Header("Health Bar")]
    public Vector3 barOffset = new Vector3(0f, 2.2f, 0f);
    public Vector2 barSize = new Vector2(1.2f, 0.15f);

    [Header("Debug")]
    public bool debugLogs = true;

    [SerializeField] private float health;

    private Transform player;
    private HealthBar healthBar;
    private float lastHitTime = -999f;
    private float lastAttackTime = -999f;
    private bool isDead;

    public float CurrentHealth => health;

    private void Log(string message)
    {
        if (debugLogs)
        {
            Debug.Log(
                "[Enemy] " + name + ": " + message,
                this
            );
        }
    }

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        health = maxHealth;
    }

    public void SetWaveStats(
        float waveMaxHealth,
        float waveContactDamage)
    {
        if (isDead)
            return;

        maxHealth = Mathf.Max(1f, waveMaxHealth);

        contactDamage = Mathf.Max(
            0f,
            waveContactDamage
        );

        health = maxHealth;

        UpdateHealthBar();

        Log(
            "Wave stats set: HP " +
            health + "/" + maxHealth +
            ", contact damage " + contactDamage
        );
    }

    private void Start()
    {
        GameObject playerObject =
            GameObject.FindWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;

            Log(
                "Found player '" +
                playerObject.name + "'"
            );
        }
        else
        {
            Debug.LogError(
                "[Enemy] " + name +
                ": no object tagged 'Player' found, " +
                "so it won't move.",
                this
            );
        }

        Collider col = GetComponent<Collider>();

        if (col == null)
        {
            Debug.LogError(
                "[Enemy] " + name +
                ": no Collider. It can't detect hits.",
                this
            );
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning(
                "[Enemy] " + name +
                ": collider is not a trigger, " +
                "so OnTrigger events won't fire.",
                this
            );
        }

        healthBar = HealthBar.Create(
            transform,
            barOffset,
            barSize
        );

        if (healthBar == null)
        {
            Debug.LogError(
                "[Enemy] " + name +
                ": health bar was not created.",
                this
            );
        }

        UpdateHealthBar();

        Log(
            "Spawned. Health " +
            health + "/" + maxHealth +
            ", contact damage " + contactDamage
        );
    }

    private void Update()
    {
        if (player == null || isDead)
            return;

        Vector3 target = new Vector3(
            player.position.x,
            transform.position.y,
            player.position.z
        );

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            moveSpeed * Time.deltaTime
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleContact(other);
    }

    private void OnTriggerStay(Collider other)
    {
        HandleContact(other);
    }

    private void HandleContact(Collider other)
    {
        if (isDead)
            return;

        if (other.CompareTag("Gear"))
        {
            if (Time.time - lastHitTime < hitCooldown)
                return;

            lastHitTime = Time.time;

            GearDamage gear =
                other.GetComponentInParent<GearDamage>();

            float damage = gear != null
                ? gear.damage
                : defaultGearDamage;

            if (gear == null)
            {
                Log(
                    "Gear hit from '" +
                    other.name +
                    "' without GearDamage; " +
                    "using fallback " + defaultGearDamage
                );
            }
            else
            {
                Log(
                    "Gear hit from '" +
                    other.name +
                    "' for " + damage
                );
            }

            TakeDamage(damage);

            return;
        }

        if (other.CompareTag("Player"))
        {
            if (dieOnContact)
            {
                Log(
                    "Touched player, dieOnContact enabled"
                );

                Die();
                return;
            }

            if (Time.time - lastAttackTime <
                attackCooldown)
            {
                return;
            }

            PlayerHealth playerHealth =
                other.GetComponentInParent<PlayerHealth>();

            if (playerHealth == null)
            {
                Debug.LogWarning(
                    "[Enemy] " + name +
                    ": touched '" +
                    other.name +
                    "' but no PlayerHealth was found.",
                    this
                );

                return;
            }

            lastAttackTime = Time.time;

            Log(
                "Player contact, dealing " +
                contactDamage
            );

            playerHealth.TakeDamage(contactDamage);
        }
    }


    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0f)
            return;

        float before = health;

        health = Mathf.Max(
            0f,
            health - amount
        );

        Log(
            "Took " + amount +
            " damage. Health " +
            before + " -> " + health
        );

        UpdateHealthBar();

        if (health <= 0f)
        {
            Die();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.SetFill(
                health / Mathf.Max(1f, maxHealth)
            );
        }
    }
    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Log("Died");

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.EnemyDefeated();
        }
        else
        {
            Debug.LogWarning(
                "[Enemy] " + name +
                ": WaveManager.Instance is null, " +
                "so this kill won't count.",
                this
            );
        }

        Destroy(gameObject);
    }
}