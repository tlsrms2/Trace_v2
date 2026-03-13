using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 보스 HP UI를 관리합니다.
/// </summary>
public class WaveUIManager : MonoBehaviour
{
    [Header("Boss HP UI")]
    public TextMeshProUGUI bossHpText;  // 예: "1500 / 2000"

    [Header("Visual Effect")]
    [Tooltip("HP 게이지 연출 스크립트를 여기에 연결하세요.")]
    public WaveVisualEffect activeVisualEffect;

    private void Start()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnBossHpUpdated += UpdateBossGauge;
        }
    }

    private void OnDestroy()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnBossHpUpdated -= UpdateBossGauge;
        }
    }

    private void UpdateBossGauge(float currentHp, float maxHp)
    {
        float displayHp = Mathf.Max(0, currentHp);

        if (bossHpText != null)
            bossHpText.text = $"{displayHp:F0} / {maxHp:F0}";

        // 진행도: 0 (꽉 참) → 1 (사망)
        float progress = 1f - (displayHp / maxHp);

        if (activeVisualEffect != null)
            activeVisualEffect.SetProgress(progress);
    }
}