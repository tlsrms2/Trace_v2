using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 전용 매니저. 보스를 소환하고, 보스 사망 시 게임 클리어를 처리합니다.
/// </summary>
public class WaveManager : MonoBehaviour
{
    private static WaveManager instance = null;
    public static WaveManager Instance
    {
        get
        {
            if (null == instance) return null;
            return instance;
        }
    }

    // --- 보스 UI 이벤트 ---
    public event Action<float, float> OnBossHpUpdated;  // (현재HP, 최대HP)

    [Header("보스 설정")]
    [Tooltip("현재 스테이지 번호 (1, 2, 3...)")]
    [SerializeField] private int currentStageLevel = 1; // 이 필드는 이제 GameManager.SelectedStageIndex에 의해 설정됩니다.
    [SerializeField] private GameObject[] bossPrefabs; // 여러 보스 프리팹
    [SerializeField] private Vector3[] bossSpawnPositions; // 각 보스의 소환 위치
    [SerializeField] private Vector3 bossSpawnPosition = Vector3.zero;

    private bool bossAlive = false;

    void Awake()
    {
        if (null == instance)
            instance = this;
        else
            Destroy(this.gameObject);
    }

    void Start()
    {
        AudioManager.Instance.PlayIngameBgm();
        StartCoroutine(SpawnBoss());
    }

    private IEnumerator SpawnBoss()
    {
        // GameManager에서 선택한 인덱스를 기반으로 스테이지 레벨 설정
        currentStageLevel = GameManager.SelectedStageIndex + 1;

        yield return null;

        int bossIndex = currentStageLevel - 1;
        if (bossIndex < 0 || bossIndex >= bossPrefabs.Length) bossIndex = 0;

        // 해당 보스의 전용 소환 위치 가져오기
        Vector3 spawnPos = bossSpawnPosition;
        if (bossIndex < bossSpawnPositions.Length)
        {
            spawnPos = bossSpawnPositions[bossIndex];
        }

        GameObject bossObj = Instantiate(bossPrefabs[bossIndex], spawnPos, Quaternion.identity);
        BaseBoss boss = bossObj.GetComponent<BaseBoss>();

        if (boss != null)
        {
            bossAlive = true;

            // HP 이벤트 → UI 연결
            boss.OnHpChanged += (cur, max) => OnBossHpUpdated?.Invoke(cur, max);

            // 스폰 즉시 꽉 찬 HP를 UI에 전달
            OnBossHpUpdated?.Invoke(boss.MaxHp, boss.MaxHp);
        }
    }

    /// <summary>
    /// 보스(또는 적)가 죽었을 때 호출됩니다.
    /// </summary>
    public void OnEnemyKilled()
    {
        if (!bossAlive) return;

        bossAlive = false;

        // 현재 깬 스테이지가 가장 최근에 열린 스테이지라면 다음 스테이지 언락
        int currentUnlocked = PlayerPrefs.GetInt("UnlockedStage", 1);
        if (currentStageLevel >= currentUnlocked)
        {
            PlayerPrefs.SetInt("UnlockedStage", currentStageLevel + 1);
            PlayerPrefs.Save();
        }

        GameManager.Instance.GameClear();
    }
}