using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

public enum GamePhase { Paused, Replay, RealTime }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public event Action OnGameOver;
    public event Action OnGameClear;
    public event Action OnTraceStarted;
    public event Action OnTraceEnded;
    public static int SelectedStageIndex = 0; // 선택된 스테이지 인덱스 (0부터 시작)

    public bool IsPaused { get; private set; }
    public GamePhase CurrentPhase = GamePhase.RealTime;
    public bool isPaused => CurrentPhase == GamePhase.Paused;

    private string playerName;

    [Header("UI Settings")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject clearText;
    [SerializeField] private GameObject timerText;
    [SerializeField] private GameObject gameClearPanel;
    [SerializeField] private GameObject pauseMenu;

    [Header("Gauge Settings")]
    [SerializeField] private float MaxGauge = 100f;
    [SerializeField] private float CurrentGauge;
    [SerializeField] private float MoveConsumptionRate = 10f;
    [SerializeField] private float AttackConsumption = 5f;
    [SerializeField] private float RecoveryRate = 10f;
    [SerializeField] private float RecoveryStartTime = 1f;

    private Coroutine chargeGaugeCor;
    private bool canCharge;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // Cursor.visible = false;
        // Cursor.lockState = CursorLockMode.Locked;
        OnTraceEnded += StartChargeWait;
        CurrentGauge = MaxGauge;
    }

    private void Start()
    {
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();

        // TRACE 진입: RealTime에서만 가능, 최소 게이지 10% 이상 필요
        if (Input.GetKeyDown(KeyCode.Space) && CurrentPhase == GamePhase.RealTime && !IsPaused && GetGaugePercentage() > 0.1f)
            ChangePhase(GamePhase.Paused);

        // TRACE 종료 → REPLAY: Space를 떼면 기록 종료 후 리플레이 시작
        if (Input.GetKeyUp(KeyCode.Space) && CurrentPhase == GamePhase.Paused)
            ChangePhase(GamePhase.Replay);

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        HandleGauge();
    }

    #region Game Flow & Phase Control
    public void ChangePhase(GamePhase nextPhase)
    {
        if (CurrentPhase == nextPhase) return;

        GamePhase prevPhase = CurrentPhase;
        CurrentPhase = nextPhase;

        switch (prevPhase)
        {
            case GamePhase.Paused: OnTraceEnded?.Invoke(); break;
            case GamePhase.RealTime: OnTraceStarted?.Invoke(); break;
        }

        switch (nextPhase)
        {
            case GamePhase.Paused: AudioManager.Instance.SetSlowBgm(); break;
            case GamePhase.Replay:
            case GamePhase.RealTime: AudioManager.Instance.SetNormalBgm(); break;
        }
    }

    public void TogglePause()
    {
        if (IsPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
        }
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }
    }

    public void GameOver()
    {
        StartCoroutine(ShowGameOverPanelRoutine());
        OnGameOver?.Invoke();
    }

    private IEnumerator ShowGameOverPanelRoutine()
    {
        yield return new WaitForSeconds(1f);
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void GameClear()
    {
        if (gameClearPanel != null)
        {
            gameClearPanel.SetActive(true);
        }
        OnGameClear?.Invoke();
    }
    #endregion

    #region Gauge Logic
    public float GetCurrentGauge() => CurrentGauge;
    public float GetGaugePercentage() => CurrentGauge / MaxGauge;
    public float GetMoveConsumptionRate() => MoveConsumptionRate;
    public float GetAttackConsumption() => AttackConsumption;

    public void ConsumeGauge(float amount)
    {
        if (CurrentPhase != GamePhase.Paused) return;

        CurrentGauge -= amount;
        if (CurrentGauge <= 0)
        {
            CurrentGauge = 0;
            ChangePhase(GamePhase.Replay);
        }
    }

    private void HandleGauge()
    {
        if (CurrentPhase == GamePhase.Paused)
        {
            canCharge = false;
        }

        if (canCharge)
        {
            CurrentGauge = Mathf.Min(MaxGauge, CurrentGauge + RecoveryRate * Time.deltaTime);
        }
    }

    private void StartChargeWait()
    {
        if (chargeGaugeCor != null) StopCoroutine(chargeGaugeCor);
        chargeGaugeCor = StartCoroutine(WaitChargeGauge());
    }

    private IEnumerator WaitChargeGauge()
    {
        yield return new WaitForSeconds(RecoveryStartTime);
        StartCharge();
    }

    private void StartCharge() => canCharge = true;
    #endregion


    public void RestartGame()
    {
        Time.timeScale = 1f;
        if (AudioManager.Instance != null) AudioManager.Instance.StopBgm();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("StageSelectScene");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("게임 종료");
    }
}