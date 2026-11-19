using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 10f;
    public float invulnerableTime = 0.5f;

    [Header("Gear Defense")]
    [Tooltip("Each DEF point reduces incoming damage by this fraction. 0.05 = 5% per point.")]
    [Range(0f, 1f)]
    public float defenseReductionPerPoint = 0.05f;

    [Tooltip("Maximum fraction of incoming damage that defense can prevent.")]
    [Range(0f, 0.95f)]
    public float maxDefenseReduction = 0.8f;

    [Header("Health Bar")]
    public bool showWorldBar = true;
    public Vector3 barOffset =
        new Vector3(0f, 2.2f, 0f);

    public Vector2 barSize =
        new Vector2(1.6f, 0.2f);

    [Header("Debug")]
    public bool debugLogs = true;

    public UnityEvent OnDamaged;
    public UnityEvent OnDied;

    private HealthBar worldBar;
    private GearLoadout gearLoadout;

    private float health;
    private float lastHitTime = -999f;
    private bool isDead = false;

    public float CurrentHealth
    {
        get { return health; }
    }

    private void Log(string message)
    {
        if (debugLogs)
        {
            Debug.Log(
                "[PlayerHealth] " + message,
                this
            );
        }
    }

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);

        health = maxHealth;

        gearLoadout = GetComponent<GearLoadout>();

        if (gearLoadout == null)
        {
            gearLoadout =
                GetComponentInParent<GearLoadout>();
        }

        if (gearLoadout == null)
        {
            gearLoadout =
                GetComponentInChildren<GearLoadout>(true);
        }

        Log(
            "Awake. Health initialized to " +
            health + "/" + maxHealth
        );
    }

    private void Start()
    {

        if (showWorldBar)
        {
            worldBar = HealthBar.Create(
                transform,
                barOffset,
                barSize
            );

            if (worldBar == null)
            {
                Debug.LogError(
                    "[PlayerHealth] Failed to create the floating health bar.",
                    this
                );
            }
            else
            {
                Log(
                    "Floating bar created at offset " +
                    barOffset +
                    " with size " + barSize
                );
            }
        }
        else
        {
            Log(
                "Floating bar is disabled " +
                "(showWorldBar is unticked)"
            );
        }

        UpdateUI();
    }

    public void TakeDamage(float amount)
    {
        if (isDead)
        {
            Log(
                "Ignored " + amount +
                " damage: player is already dead"
            );
            return;
        }

        float timeSinceHit = Time.time - lastHitTime;

        if (timeSinceHit < invulnerableTime)
        {
            Log(
                "Ignored " + amount +
                " damage: invulnerable for " +
                (invulnerableTime - timeSinceHit).ToString("F2") +
                "s more"
            );
            return;
        }

        if (amount <= 0f)
            return;

        lastHitTime = Time.time;

        float appliedDamage = amount;

        if (gearLoadout != null)
        {
            float reduction = Mathf.Clamp(
                gearLoadout.Defense *
                defenseReductionPerPoint,
                0f,
                maxDefenseReduction
            );

            appliedDamage =
                amount * (1f - reduction);
        }

        float before = health;

        health = Mathf.Max(
            0f,
            health - appliedDamage
        );

        Log(
            "Incoming " + amount +
            ", after DEF " + appliedDamage +
            ". Health " + before +
            " -> " + health
        );

        UpdateUI();

        OnDamaged?.Invoke();

        if (health <= 0f)
        {
            Log("Health reached 0");

            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead)
        {
            Log(
                "Ignored heal of " + amount +
                ": player is dead"
            );
            return;
        }

        float before = health;

        health = Mathf.Min(
            maxHealth,
            health + amount
        );

        Log(
            "Healed " + amount +
            ". Health " + before +
            " -> " + health
        );

        UpdateUI();
    }

    public void SetMaxHealth(
        float newMax,
        bool healToFull = false)
    {
        Log(
            "Max health changed " +
            maxHealth + " -> " + newMax +
            " (healToFull: " + healToFull + ")"
        );

        maxHealth = Mathf.Max(1f, newMax);

        health = healToFull
            ? maxHealth
            : Mathf.Clamp(
                health,
                0f,
                maxHealth
            );

        Log(
            "Current health is now " + health
        );

        UpdateUI();
    }

    private void Die()
    {
        isDead = true;

        Log("Player died");

        OnDied?.Invoke();

        PlayerController controller =
            GetComponent<PlayerController>();

        if (controller != null)
        {
            controller.enabled = false;

            Log("PlayerController disabled");
        }
        else
        {
            Debug.LogWarning(
                "[PlayerHealth] No PlayerController found to disable on death.",
                this
            );
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.PlayerDied();

            Log(
                "Notified WaveManager.PlayerDied()"
            );
        }
        else
        {
            Debug.LogWarning(
                "[PlayerHealth] WaveManager.Instance is null, so it was not told the player died.",
                this
            );
        }
    }

    private void UpdateUI()
    {
        if (worldBar != null)
        {
            worldBar.SetFill(
                health / maxHealth
            );
        }

        Log(
            "UI updated (" +
            health + "/" + maxHealth + ")"
        );
    }
}