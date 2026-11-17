using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public class Wave
{
    [Min(0)]
    public int numberOfEnemies;

    [Min(0f)]
    public float spawnDelay = 0.5f;

    public GameObject enemyPrefab;

    [Header("Enemy Stats For This Wave")]

    [Min(1f)]
    public float enemyMaxHealth = 10f;

    [Min(0f)]
    public float enemyAttackDamage = 1f;
}

[System.Serializable]
public class Level
{
    public string levelName;
    public Transform playerStartPoint;
    public Door[] exitDoors;
    public Transform[] spawnPoints;
    public Wave[] waves;
}

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    [Header("Game Progression")]
    public Level[] gameLevels;
    public Transform player;

    [Header("Gear Effect Integration")]
    [Tooltip(
        "Assign the GearLoadout on the real Player. " +
        "If empty, searches the Player hierarchy."
    )]
    [SerializeField]
    private GearLoadout playerGearLoadout;

    [Header("UI")]
    public TMP_Text statusText;
    public GearPullManager gearPullManager;
    public float autoContinueDelay = 2f;
    public GameObject restartPanel;

    [Header("Optional Events")]
    public UnityEvent OnWaveComplete;
    public UnityEvent OnLevelComplete;
    public UnityEvent OnGameWon;

    [Header("Area Progression")]
    public float areaStartDelay = 3f;

    [Header("Debug")]
    public bool debugLogs = true;

    private int currentLevelIndex = 0;
    private int currentWaveIndex = 0;
    private int enemiesAlive = 0;

    private bool finishedSpawning = false;
    private bool waveCompletionHandled = false;

    private bool awaitingAreaEntry = false;
    private bool playerEnteredArea = false;

    private bool gameOver = false;
    private bool waitingForGearSelection = false;

    public bool IsWaitingForGearSelection =>
        waitingForGearSelection;

    private void Log(string msg)
    {
        if (debugLogs)
        {
            Debug.Log(
                "[WaveManager] " + msg,
                this
            );
        }
    }

    private void Warn(string msg)
    {
        Debug.LogWarning(
            "[WaveManager] " + msg,
            this
        );
    }

    private void Err(string msg)
    {
        Debug.LogError(
            "[WaveManager] " + msg,
            this
        );
    }

    private string State()
    {
        return "level " + currentLevelIndex +
               ", wave " + currentWaveIndex +
               " | alive " + enemiesAlive +
               ", finishedSpawning " + finishedSpawning +
               " | waitingForGear " + waitingForGearSelection +
               ", awaitingAreaEntry " + awaitingAreaEntry +
               ", playerEntered " + playerEnteredArea +
               ", gameOver " + gameOver;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Warn(
                "A second WaveManager exists " +
                "in the scene. Only one should."
            );
        }

        Instance = this;
    }

    private GearPullManager.GameArea AreaForLevel(
        int levelIndex)
    {
        switch (levelIndex)
        {
            case 0:
                return GearPullManager.GameArea.CastleExterior;

            case 1:
                return GearPullManager.GameArea.CastleInterior;

            default:
                return GearPullManager.GameArea.ThroneRoom;
        }
    }

    private void ResolvePlayerGearLoadout()
    {
        if (player == null)
            return;

        GearLoadout actual =
            player.GetComponent<GearLoadout>();

        if (actual == null)
        {
            actual =
                player.GetComponentInChildren<GearLoadout>(
                    true
                );
        }

        if (actual == null)
        {
            actual =
                player.GetComponentInParent<GearLoadout>();
        }

        if (actual == null)
        {
            Warn(
                "No GearLoadout is attached to the " +
                "Player hierarchy. Timed buffs and " +
                "selected rewards cannot modify player stats."
            );

            playerGearLoadout = null;
            return;
        }

        if (playerGearLoadout != null &&
            playerGearLoadout != actual)
        {
            Warn(
                "Player Gear Loadout pointed at a " +
                "different object; using the actual " +
                "Player's GearLoadout."
            );
        }

        playerGearLoadout = actual;

        if (gearPullManager != null)
        {
            gearPullManager.SetPlayerLoadout(
                playerGearLoadout
            );
        }
    }

    private void Start()
    {
        if (restartPanel != null)
        {
            restartPanel.SetActive(false);
        }

        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;

                Log(
                    "Player found by tag: '" +
                    player.name + "'"
                );
            }
            else
            {
                Err(
                    "No Player assigned and no object " +
                    "tagged 'Player' found."
                );
            }
        }

        ResolvePlayerGearLoadout();

        if (playerGearLoadout == null)
        {
            Warn(
                "No player GearLoadout found. " +
                "Timed and wave-count buffs " +
                "will NOT activate. " +
                "Assign Player Gear Loadout."
            );
        }

        StartWave();
    }

    private bool HasValidCurrentWave()
    {
        if (gameLevels == null ||
            gameLevels.Length == 0 ||
            currentLevelIndex < 0 ||
            currentLevelIndex >= gameLevels.Length)
        {
            Err(
                "Game Levels is empty or current " +
                "level index is invalid."
            );

            return false;
        }

        Level level =
            gameLevels[currentLevelIndex];

        if (level == null ||
            level.waves == null ||
            level.waves.Length == 0 ||
            currentWaveIndex < 0 ||
            currentWaveIndex >= level.waves.Length)
        {
            Err(
                "Current level has no valid " +
                "Waves configuration."
            );

            return false;
        }

        if (level.spawnPoints == null ||
            level.spawnPoints.Length == 0)
        {
            Err(
                "Current level has no Spawn Points."
            );

            return false;
        }

        return true;
    }

    public void StartWave()
    {
        if (gameOver)
        {
            Log(
                "StartWave ignored: game is over"
            );

            return;
        }

        if (!HasValidCurrentWave())
            return;

        Level currentLevel =
            gameLevels[currentLevelIndex];

        Wave wave =
            currentLevel.waves[currentWaveIndex];

        if (wave.enemyPrefab == null)
        {
            Err(
                "Current wave's Enemy Prefab " +
                "is not assigned."
            );

            return;
        }

        if (gearPullManager != null)
        {
            gearPullManager.SetArea(
                AreaForLevel(currentLevelIndex)
            );
        }

        ResolvePlayerGearLoadout();

        if (playerGearLoadout != null)
        {
            playerGearLoadout.OnWaveStarted();

            Log(
                "Notified GearLoadout.OnWaveStarted() " +
                "on '" + playerGearLoadout.name +
                "'; current HP " +
                playerGearLoadout.CurrentHP +
                "/" + playerGearLoadout.MaxHP +
                ", ATK " + playerGearLoadout.Attack +
                ", DEF " + playerGearLoadout.Defense
            );
        }

        finishedSpawning = false;
        waveCompletionHandled = false;
        enemiesAlive = 0;

        Log(
            "StartWave -> '" +
            currentLevel.levelName +
            "' wave " +
            (currentWaveIndex + 1) +
            "/" + currentLevel.waves.Length +
            " | enemies " + wave.numberOfEnemies +
            " | HP " + wave.enemyMaxHealth +
            " | damage " + wave.enemyAttackDamage
        );

        ShowStatus(
            currentLevel.levelName +
            " - Wave " +
            (currentWaveIndex + 1) +
            "/" + currentLevel.waves.Length
        );

        StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator SpawnWaveRoutine()
    {
        Level currentLevel =
            gameLevels[currentLevelIndex];

        Wave currentWave =
            currentLevel.waves[currentWaveIndex];

        for (int i = 0;
             i < currentWave.numberOfEnemies;
             i++)
        {
            Transform randomSpot =
                currentLevel.spawnPoints[
                    Random.Range(
                        0,
                        currentLevel.spawnPoints.Length
                    )
                ];

            if (randomSpot == null)
            {
                Warn(
                    "One Spawn Point is null; " +
                    "skipping this enemy."
                );

                continue;
            }

            GameObject enemy = Instantiate(
                currentWave.enemyPrefab,
                randomSpot.position,
                randomSpot.rotation
            );

            enemiesAlive++;

            EnemyManager enemyManager =
                enemy.GetComponentInChildren<EnemyManager>(
                    true
                );

            if (enemyManager != null)
            {
                enemyManager.SetWaveStats(
                    currentWave.enemyMaxHealth,
                    currentWave.enemyAttackDamage
                );
            }
            else
            {
                Warn(
                    "Spawned '" + enemy.name +
                    "' has no EnemyManager. " +
                    "HP/damage cannot be set, and " +
                    "the wave may never receive a death callback."
                );
            }

            Log(
                "Spawned enemy " +
                (i + 1) +
                "/" + currentWave.numberOfEnemies +
                " '" + enemy.name +
                "' at '" + randomSpot.name +
                "' | alive now " + enemiesAlive
            );

            yield return new WaitForSeconds(
                currentWave.spawnDelay
            );
        }

        finishedSpawning = true;

        Log(
            "Finished spawning. Alive: " +
            enemiesAlive
        );

        if (enemiesAlive <= 0)
        {
            WaveCompleted();
        }
    }

    public void EnemyDefeated()
    {
        if (gameOver || waveCompletionHandled)
            return;

        enemiesAlive--;

        Log(
            "Enemy defeated. Alive now " +
            enemiesAlive +
            " (finishedSpawning " +
            finishedSpawning + ")"
        );

        if (enemiesAlive < 0)
        {
            Warn(
                "enemiesAlive went negative; " +
                "check for duplicate enemy death callbacks."
            );
        }

        if (enemiesAlive <= 0 && finishedSpawning)
        {
            WaveCompleted();
        }
    }

    private void WaveCompleted()
    {
        if (gameOver ||
            waitingForGearSelection ||
            waveCompletionHandled)
        {
            Log(
                "WaveCompleted ignored. " +
                State()
            );

            return;
        }

        waveCompletionHandled = true;

        if (playerGearLoadout != null)
        {
            playerGearLoadout.OnWaveEnded();

            Log(
                "Notified GearLoadout.OnWaveEnded() " +
                "on '" +
                playerGearLoadout.name + "'."
            );
        }

        bool lastWaveOfLevel =
            currentWaveIndex >=
            gameLevels[currentLevelIndex].waves.Length - 1;

        bool lastLevel =
            currentLevelIndex >= gameLevels.Length - 1;

        Log(
            "WaveCompleted. lastWaveOfLevel " +
            lastWaveOfLevel +
            ", lastLevel " + lastLevel
        );

        if (lastWaveOfLevel && lastLevel)
        {
            Log(
                "Final wave of the final level " +
                "cleared -> WinGame"
            );

            WinGame();

            return;
        }

        OnWaveComplete?.Invoke();

        ShowStatus("Wave cleared!");

        currentWaveIndex++;

        if (lastWaveOfLevel)
        {
            Log(
                "Level " + currentLevelIndex +
                " cleared. Opening " +
                (gameLevels[currentLevelIndex].exitDoors != null
                    ? gameLevels[currentLevelIndex].exitDoors.Length
                    : 0) +
                " exit door(s)"
            );

            OnLevelComplete?.Invoke();

            Door[] doors =
                gameLevels[currentLevelIndex].exitDoors;

            if (doors != null)
            {
                foreach (Door door in doors)
                {
                    if (door != null)
                    {
                        Log(
                            "Opening door '" +
                            door.name + "'"
                        );

                        door.Open();
                    }
                    else
                    {
                        Warn(
                            "Skipped an empty Exit Door slot on level " +
                            currentLevelIndex
                        );
                    }
                }
            }

            // Advance to the next area.
            currentLevelIndex++;
            currentWaveIndex = 0;

            awaitingAreaEntry = true;
            playerEnteredArea = false;

            Log(
                "Now waiting for the player to enter level " +
                currentLevelIndex +
                " (an AreaTrigger with Level Index " +
                currentLevelIndex + " must fire)"
            );

            ShowStatus(
                "Area cleared! Head through the door."
            );
        }

        waitingForGearSelection = true;

        Log(
            "Waiting for gear selection. " +
            State()
        );

        if (gearPullManager != null)
        {
            gearPullManager.OpenPull();
        }
        else
        {
            Warn(
                "No GearPullManager; nothing may call " +
                "ContinueToNextWave()."
            );
        }
    }

    public void ContinueToNextWave()
    {
        if (gameOver ||
            !waitingForGearSelection)
        {
            Log(
                "ContinueToNextWave ignored. " +
                State()
            );

            return;
        }

        waitingForGearSelection = false;

        Log(
            "Gear screen closed. " +
            State()
        );

        if (gearPullManager != null)
        {
            gearPullManager.HideOpenButton();
        }

        if (awaitingAreaEntry)
        {
            Log(
                "Still need the player to walk " +
                "into the next room"
            );

            TryBeginNextArea();

            return;
        }

        StartWave();
    }

    public bool IsAwaitingArea(int levelIndex)
    {
        return awaitingAreaEntry &&
               levelIndex == currentLevelIndex;
    }

    public void PlayerEnteredArea(int levelIndex)
    {
        Log(
            "PlayerEnteredArea(" +
            levelIndex + "). " +
            State()
        );

        if (!awaitingAreaEntry)
        {
            Log(
                "Ignored: not waiting for an " +
                "area entry right now"
            );

            return;
        }

        if (levelIndex != currentLevelIndex)
        {
            Log(
                "Ignored: trigger level " +
                levelIndex +
                " but the next level is " +
                currentLevelIndex
            );

            return;
        }

        playerEnteredArea = true;

        TryBeginNextArea();
    }

    private void TryBeginNextArea()
    {
        if (gameOver ||
            !awaitingAreaEntry ||
            !playerEnteredArea ||
            waitingForGearSelection)
        {
            Log(
                "TryBeginNextArea not ready. " +
                State()
            );

            return;
        }

        awaitingAreaEntry = false;

        Log(
            "Player is in the new area and " +
            "gear screen closed. Starting in " +
            areaStartDelay + "s"
        );

        StartCoroutine(
            StartWaveAfterDelay()
        );
    }

    private IEnumerator StartWaveAfterDelay()
    {
        ShowStatus("Get ready...");

        yield return new WaitForSeconds(
            areaStartDelay
        );

        StartWave();
    }

    private void WinGame()
    {
        gameOver = true;

        Log("GAME WON");

        ShowStatus("You are the new king!");

        OnGameWon?.Invoke();

        if (restartPanel != null)
        {
            restartPanel.SetActive(true);
        }
    }

    public void PlayerDied()
    {
        if (gameOver)
            return;

        gameOver = true;

        if (playerGearLoadout != null)
        {
            playerGearLoadout.OnWaveEnded();
        }

        Log("GAME LOST");

        Log(
            "PlayerDied. Stopping all coroutines. " +
            State()
        );

        StopAllCoroutines();

        ShowStatus("You died...");

        if (restartPanel != null)
        {
            restartPanel.SetActive(true);
        }
    }

    private void ShowStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }
}