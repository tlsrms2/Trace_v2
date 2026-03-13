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
    [SerializeField] private GameObject bossPrefab;
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
        // 씬 로드 직후 1프레임 대기 (다른 오브젝트 초기화 보장)
        yield return null;

        if (bossPrefab == null)
        {
            Debug.LogError("[WaveManager] bossPrefab이 설정되지 않았습니다.");
            yield break;
        }

        GameObject bossObj = Instantiate(bossPrefab, bossSpawnPosition, Quaternion.identity);
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
        GameManager.Instance.GameClear();
    }
}