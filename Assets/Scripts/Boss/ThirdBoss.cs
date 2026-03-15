using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ThirdBoss : BaseBoss
{
    [Header("암살자 보스(Trace & Replay) 설정")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private int bulletDamage = 5;
    [SerializeField] private float bulletSpeed = 8f;
    [SerializeField] private GameObject swordBulletPrefab;

    [Header("Trace & Replay 공격 설정")]
    [SerializeField] private int recordCount = 5;
    [SerializeField] private float recordDistance = 3f;
    [SerializeField] private float recordInterval = 0.2f;
    [SerializeField] private float replayDashSpeed = 40f;

    [Header("근접 공격(Strike) 설정")]
    [SerializeField] private Transform meleeAttackTransform;
    [SerializeField] private float swingAngle = 90f;
    [SerializeField] private float swingTime = 0.2f;
    [SerializeField] private float swingDistance = 1.5f;

    [Header("은신 공격(Stealth) 설정")]
    [SerializeField] private float stealthDuration = 5f;
    [SerializeField] private float stealthSpeed = 10f;
    [SerializeField] private float stealthFadeTime = 0.5f;
    [SerializeField] private float stealthGhostInterval = 0.1f;
    [SerializeField] private int stealthMaxGhosts = 20;

    [Header("폭탄(Bomb) 설정")]
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private int bombCount = 5;
    [SerializeField] private float bombMinDistance = 3f; // 폭탄 간 최소 유지 거리

    private float currentAlpha = 1f;

    protected override void Start()
    {
        base.Start();
        // 시작 시 무기 오브젝트 비활성화
        if (meleeAttackTransform != null)
        {
            meleeAttackTransform.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        // BaseBoss의 Update()에서 색상을 강제로 변경하더라도
        // 투명도(Alpha) 값은 currentAlpha를 따르도록 LateUpdate에서 덮어씌움
        if (spriteRenderer != null && currentAlpha < 1f)
        {
            Color c = spriteRenderer.color;
            c.a = currentAlpha;
            spriteRenderer.color = c;
        }
    }

    // ───────────────────────────────
    // 패턴 루프 
    // ───────────────────────────────
    protected override IEnumerator PatternLoop()
    {
        yield return StartCoroutine(PausedWait(1f));

        int rand;

        while (!IsDead)
        {
            switch (CurrentPhase)
            {
                case BossPhase.Phase1:
                    if (Random.Range(0, 2) == 0)
                        yield return StartCoroutine(TraceAndReplayStrike());
                    else
                        yield return StartCoroutine(StealthAttack());
                    break;
                case BossPhase.Phase2:
                    rand = Random.Range(0, 3);
                    if (rand == 0) yield return StartCoroutine(TraceAndReplayStrike());
                    else if (rand == 1) yield return StartCoroutine(StealthAttack());
                    else yield return StartCoroutine(ShadowDance());
                    break;
                case BossPhase.Phase3:
                    rand = Random.Range(0, 3);
                    if (rand == 0) yield return StartCoroutine(TraceAndReplayStrike());
                    else if (rand == 1) yield return StartCoroutine(StealthAttack());
                    else yield return StartCoroutine(ShadowDance());
                    break;
            }

            yield return StartCoroutine(PausedWait(1f));
        }
    }

    // ───────────────────────────────
    // 패턴 1: Trace & Replay Strike
    // ───────────────────────────────
    private IEnumerator TraceAndReplayStrike()
    {
        List<Vector3> recordedFrames = new List<Vector3>();
        List<Vector3> ghostsPos = new List<Vector3>();
        List<GameObject> ghosts = new List<GameObject>();
        
        if (target == null) yield break;

        Vector3 currentPos = transform.position;

        // 1. Trace: 플레이어와의 최단 거리 방향으로의 프레임을 계산하고 잔상으로 미리 생성
        for (int i = 0; i < recordCount; i++)
        {
            if (target == null) break;
            
            Vector3 dirToPlayer = (target.position - currentPos).normalized;
            if (dirToPlayer == Vector3.zero) dirToPlayer = Vector3.down;

            Vector3 nextPos = currentPos + dirToPlayer * recordDistance;
            recordedFrames.Add(nextPos);
            currentPos = nextPos;
            
            ghostsPos.Add(nextPos);
            
            AudioManager.Instance.PlayEpicMobDash();
            
            yield return StartCoroutine(PausedWait(recordInterval));
        }

        yield return StartCoroutine(PausedWait(0.5f));

        // 2. Replay: 생성된 잔상들을 순서대로 매우 빠르게 통과하며 파괴 (Replay 연출)
        AudioManager.Instance.PlayLaserWarning(); 

        for (int i = 0; i < ghostsPos.Count; i++)
        {
            GameObject ghost = CreateGhost(ghostsPos[i]);
            ghosts.Add(ghost);
        }
        
        for (int i = 0; i < recordedFrames.Count; i++)
        {
            yield return StartCoroutine(DashTo(recordedFrames[i], replayDashSpeed));
            if (ghosts[i] != null)
            {
                Destroy(ghosts[i]);
            }
            
            ShootTarget();
            
            // Phase 3일 때 Replay 중간 지점에서 폭탄 흩뿌리기
            if (CurrentPhase == BossPhase.Phase3 && i == recordedFrames.Count / 2)
            {
                ScatterBombs();
            }

            yield return StartCoroutine(PausedWait(0.02f));
        }

        // 3. Strike: 추적 완료 후 플레이어에게 유도 돌진 및 근접 공격(Swing) 수행
        if (target != null)
        {
            // 무기를 휘두르기 직전, 플레이어의 현재 위치 코앞으로 보정 돌진하되
            // 한 번에 뛰지 않고 기존 프레임(recordDistance) 단위로 나누어 고속으로 다가감
            Vector3 homingDir = (target.position - transform.position).normalized;
            if (homingDir == Vector3.zero) homingDir = Vector3.down;
            
            Vector3 strikePos = target.position - homingDir * (swingDistance * 0.5f);
            float distToStrike = Vector3.Distance(transform.position, strikePos);
            int stepCount = Mathf.CeilToInt(distToStrike / recordDistance);
            
            Vector3 currentStepPos = transform.position;
            for (int i = 0; i < stepCount; i++)
            {
                float stepDist = Mathf.Min(recordDistance, Vector3.Distance(currentStepPos, strikePos));
                currentStepPos += homingDir * stepDist;
                
                // 일반 Replay 대쉬보다 1.5배 빠른 속도로 프레임 단위 이동
                yield return StartCoroutine(DashTo(currentStepPos, replayDashSpeed * 1.5f));
                yield return StartCoroutine(PausedWait(0.02f)); // Replay 간격 연출
            }

            Vector3 attackDir = (target.position - transform.position).normalized;
            if (attackDir == Vector3.zero) attackDir = Vector3.down;
            
            yield return StartCoroutine(BossSwingAttack(attackDir));
        }
    }

    // ───────────────────────────────
    // 근접 공격 
    // ───────────────────────────────
    private IEnumerator BossSwingAttack(Vector3 direction)
    {
        if (meleeAttackTransform == null) yield break;

        meleeAttackTransform.gameObject.SetActive(true);
        
        float timer = 0f;
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float startAngle = baseAngle - swingAngle / 2f;
        float endAngle = baseAngle + swingAngle / 2f;

        // 무기를 휘두르는 방향으로 검기(3갈래 탄막) 발사
        ShootSpread(direction);

        while (timer < swingTime)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            {
                timer += Time.deltaTime;
                float t = swingTime > 0f ? timer / swingTime : 1f;
                float currentAngle = Mathf.Lerp(startAngle, endAngle, t);
                
                Vector3 currentDir = new Vector3(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad), 0f);

                meleeAttackTransform.localRotation = Quaternion.Euler(0, 0, currentAngle - 90f);
                meleeAttackTransform.localPosition = currentDir * swingDistance;
            }
            yield return null;
        }

        meleeAttackTransform.gameObject.SetActive(false);
    }

    // ───────────────────────────────
    // 패턴 2: 분신술 공격
    // ───────────────────────────────
    private IEnumerator ShadowDance()
    {
        if (target == null) yield break;

        AudioManager.Instance.PlayLaserWarning();
        
        // 보스 은신 (무적 및 모습 감추기)
        spriteRenderer.enabled = false;
        if (col != null) col.enabled = false;

        int shadowCount = 6;
        List<GameObject> shadows = new List<GameObject>();
        Vector3 playerPos = target.position;

        // 플레이어 주변을 포위하듯 원형으로 분신 생성
        for (int i = 0; i < shadowCount; i++)
        {
            float angle = i * (360f / shadowCount);
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            Vector3 spawnPos = playerPos + (Vector3)(dir * 6f);
            
            GameObject shadow = CreateGhost(spawnPos);
            shadows.Add(shadow);
            
            // 각 분신이 생성될 때 플레이어 방향으로 1발 사격
            FireBulletFromPoint(spawnPos, -dir, bulletPrefab);
            
            yield return StartCoroutine(PausedWait(0.1f));
        }

        yield return StartCoroutine(PausedWait(0.5f));

        // 분신들이 일제히 플레이어 위치로 쇄도
        AudioManager.Instance.PlayEpicMobDash();
        float dashTime = 0.2f;
        float elapsed = 0f;
        
        Vector3[] startPositions = new Vector3[shadowCount];
        for (int i = 0; i < shadowCount; i++)
        {
            if (shadows[i] != null) startPositions[i] = shadows[i].transform.position;
        }

        while (elapsed < dashTime)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dashTime;
                
                for (int i = 0; i < shadowCount; i++)
                {
                    if (shadows[i] != null)
                        shadows[i].transform.position = Vector3.Lerp(startPositions[i], playerPos, t);
                }
            }
            yield return null;
        }

        foreach(var shadow in shadows) { if (shadow != null) Destroy(shadow); }
        
        // 보스 재등장 (플레이어의 위치) 및 사방으로 탄막 방출
        transform.position = playerPos;
        spriteRenderer.enabled = true;
        if (col != null) col.enabled = true;

        ShootCircle(0);
        
        // Phase 3에서 사방으로 탄막 방출할 때 폭탄도 같이 흩뿌림
        if (CurrentPhase == BossPhase.Phase3)
        {
            ScatterBombs();
        }
        
        yield return StartCoroutine(PausedWait(0.2f));
        ShootCircle(15);
    }

    // ───────────────────────────────
    // 패턴 3: 은신 공격 (Stealth)
    // ───────────────────────────────
    private IEnumerator StealthAttack()
    {
        if (target == null) yield break;

        AudioManager.Instance.PlayLaserWarning();

        // 1. Fade Out (점점 투명해짐)
        float elapsed = 0f;
        while (elapsed < stealthFadeTime)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            {
                elapsed += Time.deltaTime;
                currentAlpha = Mathf.Lerp(1f, 0f, elapsed / stealthFadeTime);
            }
            yield return null;
        }
        currentAlpha = 0f;

        // 잔상(Ghost) 설정: 인스펙터 수치 사용
        float ghostSpawnTimer = 0f;
        List<GameObject> stealthGhosts = new List<GameObject>();

        bool hasDroppedBombs = false; // 폭탄 투하 플래그

        // 2. 추적 (은신 상태)
        float stealthTimer = 0f;
        while (stealthTimer < stealthDuration)
        {
            // 플레이어가 Trace 모드일 때는 보스를 감지(반투명)할 수 있고 잔상도 그라데이션으로 표시됨
            if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
            {
                currentAlpha = 0.4f;
                for (int i = 0; i < stealthGhosts.Count; i++)
                {
                    GameObject g = stealthGhosts[i];
                    if (g != null)
                    {
                        g.SetActive(true);
                        SpriteRenderer sr = g.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            Color c = sr.color;
                            // 오래된 잔상(i가 작을수록) 희미하게 설정
                            float alphaRatio = (float)(i + 1) / stealthGhosts.Count;
                            c.a = 0.4f * alphaRatio; 
                            sr.color = c;
                        }
                    }
                }
            }
            else
            {
                stealthTimer += Time.deltaTime;
                ghostSpawnTimer += Time.deltaTime;
                
                // Phase 3일 때, 은신 시간이 절반 지났을 때 한 번 깜짝 등장하여 폭탄 뿌림
                if (CurrentPhase == BossPhase.Phase3 && !hasDroppedBombs && stealthTimer >= stealthDuration / 2f)
                {
                    hasDroppedBombs = true;
                    currentAlpha = 1f; // 모습 드러냄
                    ScatterBombs();
                    yield return StartCoroutine(PausedWait(0.3f)); // 0.3초 딜레이 (깜짝 등장 연출)
                    currentAlpha = 0f; // 다시 은신
                }
                else
                {
                    currentAlpha = 0f;
                }

                // 실시간 모드일 때는 잔상 숨김
                foreach (var g in stealthGhosts)
                {
                    if (g != null) g.SetActive(false);
                }

                // 일정 간격마다 잔상 생성 및 리스트에 추가
                if (ghostSpawnTimer >= stealthGhostInterval)
                {
                    ghostSpawnTimer = 0f;
                    GameObject ghost = CreateGhost(transform.position);
                    ghost.SetActive(false); // 생성 직후에는 안 보이게 설정
                    stealthGhosts.Add(ghost);

                    // 최대 개수를 넘어가면 가장 오래된 잔상 삭제
                    if (stealthGhosts.Count > stealthMaxGhosts)
                    {
                        GameObject oldestGhost = stealthGhosts[0];
                        if (oldestGhost != null) Destroy(oldestGhost);
                        stealthGhosts.RemoveAt(0);
                    }
                }

                // 플레이어를 향해 이동
                Vector3 dir = (target.position - transform.position).normalized;
                if (dir != Vector3.zero)
                {
                    transform.position += dir * stealthSpeed * Time.deltaTime;
                }
            }
            yield return null;
        }

        // 은신 종료 시 생성했던 잔상들 모두 제거
        foreach (var g in stealthGhosts)
        {
            if (g != null) Destroy(g);
        }
        stealthGhosts.Clear();

        // 3. Fade In (다시 나타남)
        elapsed = 0f;
        while (elapsed < stealthFadeTime)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            {
                elapsed += Time.deltaTime;
                currentAlpha = Mathf.Lerp(0f, 1f, elapsed / stealthFadeTime);
            }
            yield return null;
        }
        currentAlpha = 1f;

        // 4. 나타난 후 근접 공격 일격 및 검기 발사
        if (target != null)
        {
            Vector3 attackDir = (target.position - transform.position).normalized;
            if (attackDir == Vector3.zero) attackDir = Vector3.down;
            
            yield return StartCoroutine(BossSwingAttack(attackDir));
        }
    }

    // ───────────────────────────────
    // 이동 유틸리티
    // ───────────────────────────────
    private IEnumerator BlinkTo(Vector3 targetPos)
    {
        while (GameManager.Instance.CurrentPhase == GamePhase.Paused) yield return null;
            
        float elapsed = 0f;
        float duration = 0.05f; // 매우 빠른 이동 (거의 순간이동)
        Vector3 startPos = transform.position;

        while (elapsed < duration)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            }
            yield return null;
        }
        transform.position = targetPos;
    }

    private IEnumerator DashTo(Vector3 targetPos, float speed)
    {
        AudioManager.Instance.PlayEpicMobDash();
        float distance = Vector3.Distance(transform.position, targetPos);
        float duration = Mathf.Clamp(distance / speed, 0.05f, 1f); 
        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < duration)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            }
            yield return null;
        }
        transform.position = targetPos;
    }

    // ───────────────────────────────
    // 잔상 및 공격 유틸리티
    // ───────────────────────────────
    private GameObject CreateGhost(Vector3 position)
    {
        GameObject ghostObj = new GameObject("AssassinGhost");
        ghostObj.transform.position = position;
        ghostObj.transform.rotation = transform.rotation;
        ghostObj.transform.localScale = transform.localScale;

        SpriteRenderer ghostSr = ghostObj.AddComponent<SpriteRenderer>();
        ghostSr.sprite = spriteRenderer.sprite;
        
        // 페이즈 색상을 기반으로 반투명한 잔상 생성
        Color phaseColor = spriteRenderer.color;
        ghostSr.color = new Color(phaseColor.r, phaseColor.g, phaseColor.b, 0.4f); 
        
        ghostSr.sortingLayerID = spriteRenderer.sortingLayerID;
        ghostSr.sortingOrder = spriteRenderer.sortingOrder - 1;

        return ghostObj;
    }

    private void ShootSpread(Vector3 direction)
    {
        if (swordBulletPrefab == null) return;
        AudioManager.Instance.PlayEpicMobShoot();
        
        float[] angles = { -20f, 0f, 20f };

        foreach (float angle in angles)
        {
            Vector2 spreadDir = Quaternion.Euler(0, 0, angle) * direction;
            FireBulletFromPoint(transform.position, spreadDir, swordBulletPrefab);
        }
    }

    private void ShootCircle(int cor)
    {
        if (bulletPrefab == null) return;
        AudioManager.Instance.PlayEpicMobShoot();
        
        int bulletCount = 12;
        for (int i = 0; i < bulletCount; i++)
        {
            float angle = i * (360f / bulletCount) + cor;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            FireBulletFromPoint(transform.position, dir, bulletPrefab);
        }
    }

    private void ShootTarget()
    {
        if (target == null || bulletPrefab == null) return;
        Vector2 dir = (target.position - transform.position).normalized;
        FireBulletFromPoint(transform.position, dir, bulletPrefab);
    }

    private void FireBulletFromPoint(Vector3 startPos, Vector2 direction, GameObject bulletPrefab)
    {
        if (bulletPrefab == null) return;
        GameObject bulletObj = Instantiate(bulletPrefab, startPos, Quaternion.identity);
        
        if (bulletObj.TryGetComponent<BossBullet>(out var bossBullet))
        {
            bossBullet.Initialize(direction, bulletSpeed, bulletDamage, transform);
        }
        
        // 시각적 회전 처리 (총알 이미지가 날아가는 방향을 바라보게)
        float rotAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bulletObj.transform.rotation = Quaternion.Euler(0, 0, rotAngle - 90f);
    }

    private void ScatterBombs()
    {
        if (bombPrefab == null) return;
        
        AudioManager.Instance.PlayEpicMobShoot(); // 또는 폭탄 투하 사운드로 교체 가능
        
        Camera mainCam = Camera.main;
        float camH = mainCam.orthographicSize;
        float camW = camH * mainCam.aspect;
        Vector3 camPos = mainCam.transform.position;

        List<Vector3> generatedPositions = new List<Vector3>();

        for (int i = 0; i < bombCount; i++)
        {
            Vector3 targetPos = Vector3.zero;
            bool validPositionFound = false;
            int maxRetries = 30; // 무한 루프 방지용 최대 재시도 횟수

            for (int retry = 0; retry < maxRetries; retry++)
            {
                float randomX = Random.Range(-camW * 0.9f, camW * 0.9f) + camPos.x;
                float randomY = Random.Range(-camH * 0.9f, camH * 0.9f) + camPos.y;
                targetPos = new Vector3(randomX, randomY, 0f);

                bool tooClose = false;
                foreach (Vector3 pos in generatedPositions)
                {
                    if (Vector3.Distance(targetPos, pos) < bombMinDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    validPositionFound = true;
                    break;
                }
            }

            if (validPositionFound)
            {
                generatedPositions.Add(targetPos);
                
                // 보스 위치에서 폭탄 생성
                GameObject bombObj = Instantiate(bombPrefab, transform.position, Quaternion.identity);
                
                // 생성된 폭탄을 목표 위치로 던지는 코루틴 실행
                StartCoroutine(TossBombRoutine(bombObj, targetPos, 0.4f));
            }
        }
    }

    private IEnumerator TossBombRoutine(GameObject bomb, Vector3 targetPos, float duration)
    {
        if (bomb == null) yield break;

        Vector3 startPos = bomb.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (bomb == null) yield break;

            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // 미끄러지듯 감속하며 떨어지는 느낌을 위한 Ease-Out Cubic 공식 적용
                float easeOutT = 1f - Mathf.Pow(1f - t, 3f);
                bomb.transform.position = Vector3.Lerp(startPos, targetPos, easeOutT);
            }
            yield return null;
        }
        
        if (bomb != null)
        {
            bomb.transform.position = targetPos;
        }
    }
}
