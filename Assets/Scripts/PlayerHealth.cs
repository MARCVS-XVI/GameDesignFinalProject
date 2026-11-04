using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 10f;
    public float invulnerableTime = 0.5f;

    [Header("Health Bar")]
    public bool showWorldBar = true;
    public Vector3 barOffset = new Vector3(0f, 2.2f, 0f);
    public Vector2 barSize = new Vector2(1.6f, 0.2f);

    [Header("Debug")]
    public bool debugLogs = true;

    public UnityEvent OnDamaged;
    public UnityEvent OnDied;

    private HealthBar worldBar;

    private float health;
    private float lastHitTime = -999f;
    private bool isDead = false;

    public float CurrentHealth { get { return health; } }

    // logs a message if debug logs are enabled
    private void Log(string message)
    {
        if (debugLogs)
            Debug.Log("[PlayerHealth] " + message, this);
    }

    // initializes the player's health
    void Start()
    {
        // initialize the player's health
        health = maxHealth;
        Log("Start. Health set to " + health + "/" + maxHealth);

        // create the world space health bar if enabled
        if (showWorldBar)
        {
            worldBar = HealthBar.Create(transform, barOffset, barSize);
            if (worldBar == null)
                Debug.LogError("[PlayerHealth] Failed to create the floating health bar.", this);
            else
                Log("Floating bar created at offset " + barOffset + " with size " + barSize);
        }
        else
        {
            Log("Floating bar is disabled (showWorldBar is unticked)");
        }

        // initialize the UI
        UpdateUI();
    }

    // takes damage from an attack
    public void TakeDamage(float amount)
    {
        // log the damage attempt
        if (isDead)
        {
            Log("Ignored " + amount + " damage: player is already dead");
            return;
        }

        // check if the player is invulnerable
        float timeSinceHit = Time.time - lastHitTime;
        if (timeSinceHit < invulnerableTime)
        {
            Log("Ignored " + amount + " damage: invulnerable for " + (invulnerableTime - timeSinceHit).ToString("F2") + "s more");
            return;
        }
        lastHitTime = Time.time;

        // apply the damage
        float before = health;
        health = Mathf.Max(0f, health - amount);
        Log("Took " + amount + " damage. Health " + before + " -> " + health);

        // update the UI and invoke the damage event
        UpdateUI();
        OnDamaged.Invoke();

        // check if the player has died
        if (health <= 0f)
        {
            Log("Health reached 0");
            Die();
        }
    }

    // heals the player
    public void Heal(float amount)
    {
        // log the heal attempt
        if (isDead)
        {
            Log("Ignored heal of " + amount + ": player is dead");
            return;
        }

        // apply the heal
        float before = health;
        health = Mathf.Min(maxHealth, health + amount);
        Log("Healed " + amount + ". Health " + before + " -> " + health);
        UpdateUI();
    }

    // sets the maximum health and optionally heals the player to full
    public void SetMaxHealth(float newMax, bool healToFull = false)
    {
        Log("Max health changed " + maxHealth + " -> " + newMax + " (healToFull: " + healToFull + ")");

        // update the maximum health and optionally heal the player
        maxHealth = newMax;
        health = healToFull ? maxHealth : Mathf.Min(health, maxHealth);
        Log("Current health is now " + health);
        UpdateUI();
    }

    // kills the player
    private void Die()
    {
        // mark the player as dead
        isDead = true;
        Log("Player died");
        OnDied.Invoke();

        // disable the player controller
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = false;
            Log("PlayerController disabled");
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] No PlayerController found to disable on death.", this);
        }

        // notify the wave manager
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.PlayerDied();
            Log("Notified WaveManager.PlayerDied()");
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] WaveManager.Instance is null, so it was not told the player died.", this);
        }
    }

    // updates the UI to reflect the current health
    private void UpdateUI()
    {
        if (worldBar != null)
            worldBar.SetFill(health / maxHealth);

        // log the UI update
        Log("UI updated (" + health + "/" + maxHealth + ")");
    }
}