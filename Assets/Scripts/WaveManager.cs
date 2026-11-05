using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public class Wave
{
    public int numberOfEnemies;
    public float spawnDelay = 0.5f;
    public GameObject enemyPrefab;
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

    [Header("UI")]
    public TMP_Text statusText;
    public GearPullManager gearPullManager;
    public float autoContinueDelay = 2f; // unused at the moment
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

    private bool awaitingAreaEntry = false;
    private bool playerEnteredArea = false;

    private bool gameOver = false;

    private bool waitingForGearSelection = false;

    // gets whether the game is currently waiting for the player to select a gear
    public bool IsWaitingForGearSelection
    {
        get { return waitingForGearSelection; }
    }

    // logs a message if debug logs are enabled
    private void Log(string msg)
    {
        if (debugLogs) Debug.Log("[WaveManager] " + msg, this);
    }

    // logs a warning if debug logs are enabled
    private void Warn(string msg)
    {
        Debug.LogWarning("[WaveManager] " + msg, this);
    }

    // logs an error if debug logs are enabled
    private void Err(string msg)
    {
        Debug.LogError("[WaveManager] " + msg, this);
    }

    // summary of everything for logging
    private string State()
    {
        return "level " + currentLevelIndex + ", wave " + currentWaveIndex +
               " | alive " + enemiesAlive + ", finishedSpawning " + finishedSpawning +
               " | waitingForGear " + waitingForGearSelection +
               ", awaitingAreaEntry " + awaitingAreaEntry +
               ", playerEntered " + playerEnteredArea +
               ", gameOver " + gameOver;
    }

    // initializes the wave manager
    void Awake()
    {
        if (Instance != null && Instance != this)
            Warn("A second WaveManager exists in the scene. Only one should.");

        Instance = this;
    }

    private GearPullManager.GameArea AreaForLevel(int levelIndex)
    {
        switch (levelIndex)
        {
            case 0:  return GearPullManager.GameArea.CastleExterior;
            case 1:  return GearPullManager.GameArea.CastleInterior;
            default: return GearPullManager.GameArea.ThroneRoom;
        }
    }

    // starts the wave manager
    void Start()
    {
        if (restartPanel != null) restartPanel.SetActive(false);

        // find the player object
        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
                Log("Player found by tag: '" + player.name + "'");
            }
            else
            {
                Err("No Player assigned and no object tagged 'Player' found.");
            }
        }

        // start the wave
        StartWave();
    }

    // starts the current wave
    public void StartWave()
    {
        if (gameOver)
        {
            Log("StartWave ignored: game is over");
            return;
        }

        // show current status in logs
        Level currentLevel = gameLevels[currentLevelIndex];

        if (gearPullManager != null)
        {
            gearPullManager.SetArea(AreaForLevel(currentLevelIndex));
        }

        Log("StartWave -> '" + currentLevel.levelName + "' wave " + (currentWaveIndex + 1) + "/" + currentLevel.waves.Length +
            " (" + currentLevel.waves[currentWaveIndex].numberOfEnemies + " enemies)");

        ShowStatus(currentLevel.levelName + " - Wave " + (currentWaveIndex + 1) + "/" + currentLevel.waves.Length);

        // start the spawning coroutine
        StartCoroutine(SpawnWaveRoutine());
    }

    // spawns the enemies for the current wave
    IEnumerator SpawnWaveRoutine()
    {
        // initialize the spawning flag
        finishedSpawning = false;

        // get current level and wave
        Level currentLevel = gameLevels[currentLevelIndex];
        Wave currentWave = currentLevel.waves[currentWaveIndex];

        // spawn enemies for the current wave
        for (int i = 0; i < currentWave.numberOfEnemies; i++)
        {
            Transform randomSpot = currentLevel.spawnPoints[Random.Range(0, currentLevel.spawnPoints.Length)];

            GameObject enemy = Instantiate(currentWave.enemyPrefab, randomSpot.position, randomSpot.rotation);
            enemiesAlive++;

            Log("Spawned enemy " + (i + 1) + "/" + currentWave.numberOfEnemies + " '" + enemy.name +
                "' at '" + randomSpot.name + "' | alive now " + enemiesAlive);

            if (enemy.GetComponent<EnemyManager>() == null)
                Warn("Spawned prefab '" + enemy.name + "' has no EnemyManager, so it will never report its death and the wave can't finish.");

            yield return new WaitForSeconds(currentWave.spawnDelay);
        }

        // mark spawning as finished
        finishedSpawning = true;
        Log("Finished spawning. Alive: " + enemiesAlive);

        // check if all enemies are dead
        if (enemiesAlive <= 0)
        {
            Log("All enemies were already dead when spawning finished");
            WaveCompleted();
        }
    }

    // called when an enemy is defeated
    public void EnemyDefeated()
    {
        // decrement the count of alive enemies
        enemiesAlive--;
        Log("Enemy defeated. Alive now " + enemiesAlive + " (finishedSpawning " + finishedSpawning + ")");

        // check for negative enemy count
        if (enemiesAlive < 0)
            Warn("enemiesAlive went negative. Something reported a death twice, or an enemy was in the scene before the wave spawned it.");

        // check if all enemies are dead and spawning is finished
        if (enemiesAlive <= 0 && finishedSpawning)
        {
            WaveCompleted();
        }
    }

    // called when a wave is completed
    private void WaveCompleted()
    {
        // check if the game is over or waiting for gear selection
        if (gameOver || waitingForGearSelection)
        {
            Log("WaveCompleted ignored (gameOver " + gameOver + ", waitingForGear " + waitingForGearSelection + ")");
            return;
        }

        // determine if this is the last wave of the level and the last level
        bool lastWaveOfLevel =
            currentWaveIndex >= gameLevels[currentLevelIndex].waves.Length - 1;

        // determine if this is the last level
        bool lastLevel =
            currentLevelIndex >= gameLevels.Length - 1;

        // log the completion status
        Log("WaveCompleted. lastWaveOfLevel " + lastWaveOfLevel + ", lastLevel " + lastLevel);

        // check if this is the final wave of the final level
        if (lastWaveOfLevel && lastLevel)
        {
            Log("Final wave of the final level cleared -> WinGame");
            WinGame();
            return;
        }

        // invoke the wave complete event and show status
        OnWaveComplete.Invoke();
        ShowStatus("Wave cleared!");

        // move to the next wave
        currentWaveIndex++;

        // check if this is the last wave of the level
        if (lastWaveOfLevel)
        {
            Log("Level " + currentLevelIndex + " cleared. Opening " +
                (gameLevels[currentLevelIndex].exitDoors != null ? gameLevels[currentLevelIndex].exitDoors.Length : 0) + " exit door(s)");

            OnLevelComplete.Invoke();

            // open all exit doors
            foreach (Door door in gameLevels[currentLevelIndex].exitDoors)
            {
                if (door != null)
                {
                    Log("Opening door '" + door.name + "'");
                    door.Open();
                }
                else
                {
                    Warn("Skipped an empty Exit Door slot on level " + currentLevelIndex);
                }
            }

            // move to the next level
            currentLevelIndex++;
            currentWaveIndex = 0;

            // set the flag to wait for the player to enter the next area
            awaitingAreaEntry = true;
            playerEnteredArea = false;
            Log("Now waiting for the player to enter level " + currentLevelIndex + " (an AreaTrigger with Level Index " + currentLevelIndex + " must fire)");
            ShowStatus("Area cleared! Head through the door.");
        }

        // set the flag to wait for gear selection
        waitingForGearSelection = true;
        Log("Waiting for gear selection. " + State());

        // show the gear selection screen
        if (gearPullManager != null)
        {
            gearPullManager.ShowOpenButton();
        }
        else
        {
            Warn("No GearPullManager, so nothing will call ContinueToNextWave() and the game will stall here.");
        }
    }

    // continues to the next wave after gear selection
    public void ContinueToNextWave()
    {
        // check if the game is over or not waiting for gear selection
        if (gameOver || !waitingForGearSelection)
        {
            Log("ContinueToNextWave ignored (gameOver " + gameOver + ", waitingForGear " + waitingForGearSelection + ")");
            return;
        }

        // after picking gear, screen is closed
        waitingForGearSelection = false;
        Log("Gear screen closed. " + State());

        // hide the gear selection screen
        if (gearPullManager != null)
            gearPullManager.HideOpenButton();

        // check if we're waiting for an area entry
        if (awaitingAreaEntry)
        {
            Log("Still need the player to walk into the next room");
            TryBeginNextArea();
            return;
        }

        // start next wave
        StartWave();
    }

    // checks if the game is waiting for the player to enter a specific area
    public bool IsAwaitingArea(int levelIndex)
    {
        return awaitingAreaEntry && levelIndex == currentLevelIndex;
    }

    // called when the player enters a new area
    public void PlayerEnteredArea(int levelIndex)
    {
        Log("PlayerEnteredArea(" + levelIndex + "). " + State());

        // check if we're waiting for an area entry
        if (!awaitingAreaEntry)
        {
            Log("Ignored: not waiting for an area entry right now (the level isn't cleared yet, or this was already handled)");
            return;
        }

        // check if the trigger is for the correct level    
        if (levelIndex != currentLevelIndex)
        {
            Log("Ignored: trigger is for level " + levelIndex + " but the next level is " + currentLevelIndex + ". Check the trigger's Level Index.");
            return;
        }

        playerEnteredArea = true;
        TryBeginNextArea();
    }

    // attempts to begin the next area
    private void TryBeginNextArea()
    {
        if (gameOver || !awaitingAreaEntry || !playerEnteredArea || waitingForGearSelection)
        {
            Log("TryBeginNextArea not ready. " + State());
            return;
        }

        // begin the next area
        awaitingAreaEntry = false;
        Log("Player is in the new area and the gear screen is closed. Starting in " + areaStartDelay + "s");
        StartCoroutine(StartWaveAfterDelay());
    }

    // starts the wave after a delay
    IEnumerator StartWaveAfterDelay()
    {
        ShowStatus("Get ready...");
        yield return new WaitForSeconds(areaStartDelay);
        StartWave();
    }

    // wins the game and become new king
    private void WinGame()
    {
        gameOver = true;
        Log("GAME WON");
        ShowStatus("You are the new king!");
        OnGameWon.Invoke();
        if (restartPanel != null) restartPanel.SetActive(true);
    }

    // loses the game and called from PlayerHealth when the player dies
    public void PlayerDied()
    {
        gameOver = true;
        Log("GAME LOST");
        Log("PlayerDied. Stopping all coroutines. " + State());
        StopAllCoroutines();
        ShowStatus("You died...");
        if (restartPanel != null) restartPanel.SetActive(true);
    }

    // shows a status message
    private void ShowStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // in case the game was paused
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}