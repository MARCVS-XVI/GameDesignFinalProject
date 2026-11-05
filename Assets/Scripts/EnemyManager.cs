using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 3f;
    public float moveSpeed = 3f;

    [Header("Attacking the player")]
    public float contactDamage = 1f;
    public float attackCooldown = 1f;
    public bool dieOnContact = false;

    [Header("Taking damage from gear")]
    public float defaultGearDamage = 1f;
    public float hitCooldown = 0.4f;

    [Header("Health bar")]
    public Vector3 barOffset = new Vector3(0f, 2.2f, 0f);
    public Vector2 barSize = new Vector2(1.2f, 0.15f);

    [Header("Debug")]
    public bool debugLogs = true;

    private float health;
    private Transform player;
    private HealthBar healthBar;
    private float lastHitTime = -999f;
    private float lastAttackTime = -999f;
    private bool isDead = false;

    // log a message if debug logs are enabled
    private void Log(string msg)
    {
        if (debugLogs) Debug.Log("[Enemy] " + name + ": " + msg, this);
    }

    // initialize the enemy component
    void Start()
    {
        health = maxHealth;

        // find the player object to follow
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            Log("Found player '" + playerObject.name + "'");
        }
        else
        {
            Debug.LogError("[Enemy] " + name + ": no object tagged 'Player' found, so it won't move.", this);
        }

        // enemy needs a trigger collider to detect gear and the player
        Collider col = GetComponent<Collider>();
        if (col == null)
            Debug.LogError("[Enemy] " + name + ": no Collider. It can't detect hits.", this);
        else if (!col.isTrigger)
            Debug.LogWarning("[Enemy] " + name + ": collider is not a trigger, so OnTrigger events won't fire.", this);

        // initialize the health bar
        if (maxHealth <= 0f)
            Debug.LogError("[Enemy] " + name + ": maxHealth is " + maxHealth + ". The health bar will break.", this);

        // create the health bar
        healthBar = HealthBar.Create(transform, barOffset, barSize);
        if (healthBar == null)
            Debug.LogError("[Enemy] " + name + ": health bar was not created.", this);

        Log("Spawned. Health " + health + "/" + maxHealth);
    }

    // update the enemy's position each frame
    void Update()
    {
        if (player == null || isDead) return;

        // move towards the player
        Vector3 target = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
    }

    // handle collisions with other objects
    void OnTriggerEnter(Collider other) { HandleContact(other); }
    void OnTriggerStay(Collider other)  { HandleContact(other); }

    // handle contact with other objects
    private void HandleContact(Collider other)
    {
        if (isDead) return;

        // check if the contact is with a gear
        if (other.CompareTag("Gear"))
        {
            // check if the gear has hit cooldown
            float since = Time.time - lastHitTime;

            // if the gear is on cooldown, ignore the hit
            if (since < hitCooldown) return;
            lastHitTime = Time.time;

            // get the gear's damage
            GearDamage gear = other.GetComponentInParent<GearDamage>();
            if (gear == null)
                Log("Gear hit by '" + other.name + "' but no GearDamage found, using default " + defaultGearDamage);
            else
                Log("Gear hit by '" + other.name + "' for " + gear.damage);

            // deal damage to the enemy
            TakeDamage(gear != null ? gear.damage : defaultGearDamage);
            return;
        }

        // check if the contact is with the player
        if (other.CompareTag("Player"))
        {
            // this is for testing purpose (can toggle on/off)
            if (dieOnContact)
            {
                Log("Touched player, dieOnContact is on");
                Die();
                return;
            }

            // check if the player is on cooldown
            if (Time.time - lastAttackTime < attackCooldown) return;

            // update the last attack time
            lastAttackTime = Time.time;

            // get the player's health
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null)
            {
                Debug.LogWarning("[Enemy] " + name + ": touched '" + other.name + "' but no PlayerHealth found on it or its parents.", this);
                return;
            }

            // deal damage to the player
            Log("Player contact, dealing " + contactDamage);
            playerHealth.TakeDamage(contactDamage);
        }
    }

    // take damage from an attack
    public void TakeDamage(float amount)
    {
        // ignore damage if the enemy is already dead
        if (isDead) return;

        // update the enemy's health
        float before = health;
        health -= amount;
        Log("Took " + amount + " damage. Health " + before + " -> " + health);

        // update the health bar
        if (healthBar != null)
            healthBar.SetFill(health / maxHealth);

        // check if the enemy is dead
        if (health <= 0f)
            Die();
    }

    // kill the enemy
    private void Die()
    {
        // ignore if the enemy is already dead
        if (isDead) return;
        isDead = true;
        Log("Died");

        // notify the wave manager and destroy enemy prefab
        if (WaveManager.Instance != null)
            WaveManager.Instance.EnemyDefeated();
        else
            Debug.LogWarning("[Enemy] " + name + ": WaveManager.Instance is null, so the wave won't count this kill.", this);

        Destroy(gameObject);
    }
}