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

    public bool IsPaused { get; private set; }
    public GamePhase CurrentPhase = GamePhase.RealTime;
    public bool isPaused => CurrentPhase == GamePhase.Paused;

    private string playerName;
    private bool secondPanelReady = false; // 두 번째 패널 엔터 체크용

    [Header("UI Settings")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject clearText;
    [SerializeField] private GameObject timerText;
    [SerializeField] private GameObject gameClearPanel;
    [SerializeField] private GameObject firstClearPanel;
    [SerializeField] private GameObject secondClearPanel;
    [SerializeField] private GameObject thirdClearPanel;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private TMP_InputField nameInputField;

    [Header("UI Keyboard Focus Settings")]
    [SerializeField] private GameObject firstTitleButton;
    [SerializeField] private GameObject firstPauseButton;
    [SerializeField] private GameObject firstGameOverButton;
    [SerializeField] private GameObject firstGameClearInputButton;
    [SerializeField] private GameObject secondClearButton;
    [SerializeField] private GameObject thirdSelectButton;


    [Header("Gauge Settings")]
    [SerializeField] private float MaxGauge = 100f;
    [SerializeField] private float CurrentGauge;
    [SerializeField] private float ConsumptionRate = 20f;
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
    }

    private void Start()
    {
        TitleUIFocus();
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

        if (Input.GetKeyDown(KeyCode.R) && !firstClearPanel.activeSelf)
        {
            RestartGame();
        }

        HandleGauge();

        // 두 번째 패널 엔터 입력
        // Update() 안 두 번째 패널 엔터 입력 처리
        if (secondPanelReady && secondClearPanel != null && secondClearPanel.activeSelf && Input.GetKeyDown(KeyCode.Return))
        {
            secondClearPanel.SetActive(false);
            thirdClearPanel.SetActive(true);

            clearText?.SetActive(true);
            timerText?.SetActive(true);

            // ✅ 세 번째 패널에서 Restart 버튼 포커스 설정
            SetUIFocus(thirdSelectButton); // 혹은 thirdPanel에서 사용할 버튼 지정

            secondPanelReady = false; // 다시 초기화
        }
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

    public void TitleUIFocus()
    {
        if (firstTitleButton != null)
            SetUIFocus(firstTitleButton);
    }

    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
            SetUIFocus(firstPauseButton);
        }
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
            EventSystem.current.SetSelectedGameObject(null);
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
            SetUIFocus(firstGameOverButton);
        }
    }

    public void GameClear()
    {
        if (gameClearPanel != null)
        {
            gameClearPanel.SetActive(true);
            firstClearPanel.SetActive(true);
            SetUIFocus(firstGameClearInputButton);
        }
        OnGameClear?.Invoke();
    }

    // 첫 번째 패널 제출
    public void OnFirstPanelSubmit()
    {
        playerName = nameInputField.text;
        if (string.IsNullOrEmpty(playerName))
            playerName = "Unnamed";

        firstClearPanel.SetActive(false);
        secondClearPanel.SetActive(true);

        clearText.SetActive(false);
        timerText.SetActive(false);

        EventSystem.current.SetSelectedGameObject(null);

        // 한 프레임 딜레이 후 두 번째 패널 엔터 가능
        StartCoroutine(EnableSecondPanelInputNextFrame());
    }

    private IEnumerator EnableSecondPanelInputNextFrame()
    {
        yield return null;
        secondPanelReady = true;
    }


    private void SetUIFocus(GameObject firstSelected)
    {
        if (firstSelected != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelected);
        }
    }
    #endregion

    #region Gauge Logic
    public float GetCurrentGauge() => CurrentGauge;
    public float GetGaugePercentage() => CurrentGauge / MaxGauge;

    private void HandleGauge()
    {
        if (CurrentPhase == GamePhase.Paused)
        {
            canCharge = false;
            CurrentGauge -= ConsumptionRate * Time.unscaledDeltaTime;
            if (CurrentGauge <= 0)
            {
                CurrentGauge = 0;
                ChangePhase(GamePhase.Replay);
            }
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
        SceneManager.LoadScene("GameScene");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("게임 종료");
    }
}