using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using TMPro;

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
    public Transform playerStartPoint; // where player is moved to after level - REMOVE after prototype?
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
    public float autoContinueDelay = 2f; // wait time between waves when there is no gear screen

    [Header("Optional Events")]
    public UnityEvent OnWaveComplete;
    public UnityEvent OnLevelComplete;
    public UnityEvent OnGameWon;

    private int currentLevelIndex = 0;
    private int currentWaveIndex = 0;
    private int enemiesAlive = 0;
    private bool finishedSpawning = false;
    private bool movePlayerToNextArea = false;
    private bool gameOver = false;

    private bool waitingForGearSelection = false;
    public bool IsWaitingForGearSelection
    {
        get { return waitingForGearSelection; }
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        MovePlayerToCurrentArea();
        StartWave();
    }

    public void StartWave()
    {
        if (gameOver) return;

        Level currentLevel = gameLevels[currentLevelIndex];
        ShowStatus(currentLevel.levelName + " - Wave " + (currentWaveIndex + 1) + "/" + currentLevel.waves.Length);

        StartCoroutine(SpawnWaveRoutine());
    }

    IEnumerator SpawnWaveRoutine()
    {
        finishedSpawning = false;

        Level currentLevel = gameLevels[currentLevelIndex];
        Wave currentWave = currentLevel.waves[currentWaveIndex];

        for (int i = 0; i < currentWave.numberOfEnemies; i++)
        {
            Transform randomSpot = currentLevel.spawnPoints[Random.Range(0, currentLevel.spawnPoints.Length)];

            Instantiate(currentWave.enemyPrefab, randomSpot.position, randomSpot.rotation);
            enemiesAlive++;

            yield return new WaitForSeconds(currentWave.spawnDelay);
        }

        finishedSpawning = true;

        if (enemiesAlive <= 0)
        {
            WaveCompleted();
        }
    }

    public void EnemyDefeated()
    {
        enemiesAlive--;

        if (enemiesAlive <= 0 && finishedSpawning)
        {
            WaveCompleted();
        }
    }

    private void WaveCompleted()
    {
        if (gameOver || waitingForGearSelection)
            return;

        bool lastWaveOfLevel =
            currentWaveIndex >= gameLevels[currentLevelIndex].waves.Length - 1;

        bool lastLevel =
            currentLevelIndex >= gameLevels.Length - 1;

        if (lastWaveOfLevel && lastLevel)
        {
            WinGame();
            return;
        }

        OnWaveComplete.Invoke();
        ShowStatus("Wave cleared!");

        currentWaveIndex++;

        if (lastWaveOfLevel)
        {
            OnLevelComplete.Invoke();

            currentLevelIndex++;
            currentWaveIndex = 0;
            movePlayerToNextArea = true;
        }

        waitingForGearSelection = true;

        if (gearPullManager != null)
        {
            gearPullManager.ShowOpenButton();
        }
    }

    // called by the gear screen when the player closes it
    public void ContinueToNextWave()
    {
        if (gameOver || !waitingForGearSelection)
            return;

        waitingForGearSelection = false;

        if (gearPullManager != null)
            gearPullManager.HideOpenButton();

        if (movePlayerToNextArea)
        {
            movePlayerToNextArea = false;
            MovePlayerToCurrentArea();
        }

        StartWave();
    }

    private void WinGame()
    {
        gameOver = true;
        ShowStatus("You are the new king!");
        OnGameWon.Invoke();
    }

    // call this from the player's health script when the player dies
    public void PlayerDied()
    {
        gameOver = true;
        StopAllCoroutines();
        ShowStatus("You died...");
    }

    private void MovePlayerToCurrentArea()
    {
        Transform startPoint = gameLevels[currentLevelIndex].playerStartPoint;
        if (player == null || startPoint == null) return;

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        player.position = startPoint.position;

        if (controller != null)
        {
            controller.enabled = true;
        }
    }

    private void ShowStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}