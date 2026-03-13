using System; // Action 사용을 위해 추가
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    // --- 싱글톤 ---
    private static WaveManager instance = null;
    public static WaveManager Instance
    {
        get
        {
            if (null == instance) return null;
            return instance;
        }
    }

    void Awake()
    {
        if (null == instance)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }

        enemySpawner = GetComponent<EnemySpawner>();
    }

    // --- UI와 통신할 이벤트 선언 ---
    // (현재 웨이브 번호, 전체 웨이브 개수)
    public event Action<int, int> OnWaveUpdated; 
    
    // (남은 적 개수, 현재 웨이브의 총 적 개수)
    public event Action<int, int> OnEnemyProgressUpdated; 

    // --- 보스용 이벤트 추가 ---
    public event Action<bool> OnWaveModeChanged;       // true면 보스 모드, false면 일반 모드
    public event Action<float, float> OnBossHpUpdated; // 보스 현재 체력, 최대 체력
    // -------------------------

    public event Action<Action> OnWaveTransitionStarted; // 연출 완료 콜백을 받을 이벤트

    // --- 변수 ---
    [SerializeField] private WaveData[] waves; // 인스펙터에서 ScriptableObject 데이터 할당
    private int currentWaveIndex = 0;
    private int enemiesRemainingToSpawn = 0;
    private int enemiesRemainingAlive = 0;
    private int totalEnemiesInCurrentWave = 0; // 현재 웨이브 총 적 수 기록용
    private EnemySpawner enemySpawner;

    void Start()
    {
        StartCoroutine(SpawnWaves());
        AudioManager.Instance.PlayIngameBgm();
    }

    private IEnumerator SpawnWaves()
    {
        while (currentWaveIndex < waves.Length)
        {
            WaveData currentWave = waves[currentWaveIndex];
            
            // 1. UI 갱신: 웨이브가 시작될 때 호출 (인덱스는 0부터이므로 +1)
            OnWaveUpdated?.Invoke(currentWaveIndex + 1, waves.Length);

            if (currentWave.isBossWave)
            {
                // UI를 보스 모드로 전환
                OnWaveModeChanged?.Invoke(true); 
                
                totalEnemiesInCurrentWave = 1;
                enemiesRemainingToSpawn = 0;
                enemiesRemainingAlive = 1;

                // 보스 소환
                GameObject bossObj = Instantiate(currentWave.enemies[0].enemyPrefab, Vector3.zero, Quaternion.identity);
                Enemy bossEnemy = bossObj.GetComponent<Enemy>();

                if (bossEnemy != null)
                {
                    // 보스가 데미지를 입을 때마다 UI로 전달하도록 연결
                    bossEnemy.OnHpChanged += (currentHp, maxHp) => 
                    {
                        OnBossHpUpdated?.Invoke(currentHp, maxHp);
                    };

                    // 스폰 직후 꽉 찬 체력을 UI에 전송
                    OnBossHpUpdated?.Invoke(bossEnemy.MaxHp, bossEnemy.MaxHp);
                }
            }
            else 
            {
                totalEnemiesInCurrentWave = 0;
                foreach (var enemy in currentWave.enemies)
                {
                    totalEnemiesInCurrentWave += enemy.enemyCount;
                }
                enemiesRemainingToSpawn = totalEnemiesInCurrentWave;
                enemiesRemainingAlive = 0;
                
                // 웨이브 시작 시 가득 찬 게이지 갱신
                OnEnemyProgressUpdated?.Invoke(totalEnemiesInCurrentWave, totalEnemiesInCurrentWave);

                foreach (var enemy in currentWave.enemies)
                {
                    StartCoroutine(SpawnEnemy(enemy));
                }
            }

            // 2. 현재 웨이브의 적이 모두 죽을 때까지 대기
            while (enemiesRemainingToSpawn > 0 || enemiesRemainingAlive > 0)
            {
                yield return null;
            }
            // 3. 웨이브 클리어 연출 시작을 알림 (진행 여부를 체크할 변수 생성)
            bool isTransitionFinished = false;

            // UI 쪽에 연출을 지시하고, 연출이 끝나면 isTransitionFinished를 true로 바꾸라는 콜백 함수를 넘겨줌
            OnWaveTransitionStarted?.Invoke(() => isTransitionFinished = true);

            // UI 연출이 완전히 끝날 때까지 다음 웨이브로 넘어가지 않고 대기
            while (!isTransitionFinished)
            {
                yield return null;
            }

            currentWaveIndex++;
        }

        // 4. 모든 웨이브 종료 시 게임 클리어 처리
        GameManager.Instance.GameClear(); // 게임 상태 변경 (타이머 정지 등)
    }

    private IEnumerator SpawnEnemy(enemyData enemy)
    {
        float timer = 0f;
        while(timer < enemy.spawnStartTime)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            {
                timer += Time.deltaTime;
            }
            yield return null;
        }
        
        int leftSpawnCnt = enemy.enemyCount;
        while (leftSpawnCnt > 0)
        {

            if (GameManager.Instance.CurrentPhase == GamePhase.RealTime)
            {
                enemySpawner.SpawnEnemy(enemy.enemyPrefab);
                enemiesRemainingToSpawn--;
                enemiesRemainingAlive++;
                leftSpawnCnt--;
            }

            yield return new WaitForSeconds(enemy.spawnInterval);
        }
    }

    // 적이 죽었을 때 호출되는 함수 (적 스크립트에서 이 함수를 호출해야 함)
    public void OnEnemyKilled()
    {
        enemiesRemainingAlive--;
        
        // 안전 장치: 음수 방지 (중복 호출 대비)
        if (enemiesRemainingAlive < 0)
        {
            Debug.LogWarning("[WaveManager] enemiesRemainingAlive went negative! Clamping to 0.");
            enemiesRemainingAlive = 0;
        }

        // 남은 전체 적 = 아직 스폰 안 된 적 + 현재 맵에 살아있는 적
        int totalRemaining = enemiesRemainingToSpawn + enemiesRemainingAlive;
        
        // 4. UI 갱신: 적이 죽을 때마다 게이지 업데이트
        OnEnemyProgressUpdated?.Invoke(totalRemaining, totalEnemiesInCurrentWave);
    }
}