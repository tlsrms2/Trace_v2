using UnityEngine;
using TMPro;
using System.Collections;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("UI Reference")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI gameOverTimerText;
    public TextMeshProUGUI gameClearTimerText;

    [Header("Timer Settings")]
    public float currentTime = 0;
    public bool isRunning = true;
    public int minutes;
    public int seconds;
    public int milliseconds;

    [Header("Distance Fading Settings")]
    public Transform player; // 플레이어의 Transform
    public Transform timerWorldPosition; // 거리를 잴 타이머의 월드 위치 (비워두면 이 스크립트가 붙은 오브젝트 기준)
    
    public float fadeStartDistance = 10f; // 이 거리보다 가까워지면 투명해지기 시작함
    public float fadeEndDistance = 3f;    // 이 거리보다 가까워지면 최소 투명도 유지
    
    [Range(0f, 1f)] public float maxAlpha = 1f;   // 멀리 있을 때의 투명도 (1 = 완전 불투명)
    [Range(0f, 1f)] public float minAlpha = 0f; // 가까이 있을 때의 투명도 (0.2 = 많이 투명함)

    [Header("Time Reduce Settings")]
    public TextMeshProUGUI reduceText;
    public float accumulatedAmount = 0f;
    public float effectDuration = 1f;
    public Coroutine effectCoroutine;

    void Start()
    {
        // 기준점 위치를 따로 할당하지 않았다면, 이 스크립트가 붙은 오브젝트를 기준으로 삼습니다.
        if (timerWorldPosition == null)
        {
            timerWorldPosition = this.transform;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += StopGameOverTimer;
            GameManager.Instance.OnGameClear += StopGameClearTimer;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= StopGameOverTimer;
            GameManager.Instance.OnGameClear -= StopGameClearTimer;
        }
    }

    void Update()
    {
        // 1. 타이머 시간 계산
        if (isRunning && GameManager.Instance.CurrentPhase != GamePhase.Paused)
        {
            currentTime += Time.deltaTime;
            UpdateTimerDisplay();
        }

        // 2. 거리에 따른 투명도 업데이트 로직
        UpdateTextAlphaByDistance();
    }

    void StopGameOverTimer()
    {
        isRunning = false;
        if (gameOverTimerText != null)
        {
            UpdateGameOverDisplay();
        }
    }

    void StopGameClearTimer()
    {
        isRunning = false;
        if (gameClearTimerText != null)
        {
            UpdateGameClearDisplay();
        }
    }

    void UpdateTimerDisplay()
    {
        minutes = Mathf.FloorToInt(currentTime / 60f);
        seconds = Mathf.FloorToInt(currentTime % 60f);
        milliseconds = Mathf.FloorToInt((currentTime * 100f) % 100f);

        timerText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
    }

    void UpdateGameOverDisplay()
    {
        minutes = Mathf.FloorToInt(currentTime / 60f);
        seconds = Mathf.FloorToInt(currentTime % 60f);
        milliseconds = Mathf.FloorToInt((currentTime * 100f) % 100f);
        
        gameOverTimerText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
    }

    void UpdateGameClearDisplay()
    {
        minutes = Mathf.FloorToInt(currentTime / 60f);
        seconds = Mathf.FloorToInt(currentTime % 60f);
        milliseconds = Mathf.FloorToInt((currentTime * 100f) % 100f);
        
        gameClearTimerText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
    }

    void UpdateTextAlphaByDistance()
    {
        // 플레이어나 텍스트가 연결되어 있지 않으면 작동하지 않음
        if (player == null || timerText == null) return;

        // 플레이어와 타이머 위치 사이의 거리 계산
        float distance = Vector3.Distance(player.position, timerWorldPosition.position);

        // 거리를 0~1 사이의 비율로 변환 (InverseLerp)
        // distance가 fadeEndDistance 이하면 0, fadeStartDistance 이상이면 1 반환
        float distanceRatio = Mathf.InverseLerp(fadeEndDistance, fadeStartDistance, distance);

        // 비율에 따라 최소 투명도와 최대 투명도 사이의 값을 부드럽게 계산 (Lerp)
        float currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, distanceRatio);

        // 텍스트의 Color 값을 가져와서 Alpha 값만 수정한 뒤 다시 적용
        Color textColor = timerText.color;
        textColor.a = currentAlpha;
        timerText.color = textColor;
    }

    public void ReduceTime(int amount)
    {
        currentTime = Mathf.Max(0, currentTime - amount);

        accumulatedAmount += amount;

        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
        }
        effectCoroutine = StartCoroutine(ShowReduceEffectRoutine());
    }

    private IEnumerator ShowReduceEffectRoutine()
    {
        yield return new WaitForEndOfFrame();

        reduceText.gameObject.SetActive(true);
        reduceText.text = $"- {accumulatedAmount:F0}:00";

        float timer = 0f;
        Color c = reduceText.color;
        while (timer < effectDuration)
        {
            timer += Time.deltaTime;
            float ratio = timer / effectDuration;

            c.a = Mathf.Lerp(1f, 0f, ratio);
            reduceText.color = c;

            yield return null;
        }
        
        reduceText.gameObject.SetActive(false);
        accumulatedAmount = 0f;
        effectCoroutine = null;
    }
}