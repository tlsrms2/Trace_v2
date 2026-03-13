using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaveUIManager : MonoBehaviour
{
    [Header("Wave Info UI")]
    public TextMeshProUGUI waveText;        // 예: "WAVE 1 / 3"
    public TextMeshProUGUI enemyCountText;  // 예: "15 / 20"
    
    [Header("Visual Effect Strategy")]
    [Tooltip("원하는 연출 스크립트(WidthScale 또는 RandomSpawn)를 여기에 끌어다 놓으세요.")]
    public WaveVisualEffect activeVisualEffect; 

    private bool isBossMode = false;

    private void Start()
    {
        // 1. WaveManager의 이벤트 구독
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveUpdated += UpdateWaveInfo;
            WaveManager.Instance.OnEnemyProgressUpdated += UpdateEnemyGauge;
            
            // 보스 이벤트 구독
            WaveManager.Instance.OnWaveModeChanged += SetMode;
            WaveManager.Instance.OnBossHpUpdated += UpdateBossGauge;
        }
    }

    private void OnDestroy()
    {
        // 2. 메모리 누수 방지를 위한 구독 해제
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveUpdated -= UpdateWaveInfo;
            WaveManager.Instance.OnEnemyProgressUpdated += UpdateEnemyGauge;
            WaveManager.Instance.OnBossHpUpdated += UpdateBossGauge;
        }
    }

    private void SetMode(bool isBoss)
    {
        isBossMode = isBoss;

        // 보스 웨이브 시작 시 이펙트 초기화 (필요시)
        if (activeVisualEffect != null) activeVisualEffect.SetProgress(0f);
    }

    private void UpdateWaveInfo(int currentWave, int maxWaves)
    {
        // 보스 웨이브일 경우 텍스트를 "BOSS WAVE" 등으로 변경 가능
        waveText.text = isBossMode ? $"BOSS" : $"WAVE {currentWave} / {maxWaves}";
    }

    // 일반 모드 진행도 갱신
    private void UpdateEnemyGauge(int remaining, int total)
    {
        if (isBossMode) return; // 보스전이면 무시

        enemyCountText.text = $"{remaining} / {total}";
        
        // 진행도 계산: 0 (시작) -> 1 (모두 처치)
        float progress = 1f - ((float)remaining / total);

        // 연출 클래스에 진행도 전달 (나머지 애니메이션 처리는 저쪽에서 알아서 함)
        if (activeVisualEffect != null)
        {
            activeVisualEffect.SetProgress(progress);
        }

    }

    // 보스 HP 모드 진행도 갱신
    private void UpdateBossGauge(float currentHp, float maxHp)
    {
        if (!isBossMode) return;

        // 체력이 음수나 소수로 표기 되는 것을 방지 (0 / Max)
        float displayHp = Mathf.Max(0, currentHp);
        
        // 텍스트를 체력으로 표시 (예: 1500 / 2000)
        enemyCountText.text = $"{displayHp:F0} / {maxHp:F0}";
        
        // 진행도 계산: 0 (체력 꽉참) -> 1 (체력 0)
        float progress = 1f - (displayHp / maxHp);

        if (activeVisualEffect != null)
        {
            activeVisualEffect.SetProgress(progress);
        }
    }
}