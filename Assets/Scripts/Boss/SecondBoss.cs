using UnityEngine;
using System.Collections;

/// <summary>
/// 두 번째 보스.탄막형 공격 패턴을 구사합니다.
/// </summary>
public class SecondBoss : BaseBoss
{
    [Header("탄막 프리팹 (BossBullet 컴포넌트 필수)")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject bigBulletPrefab;

    [Header("패턴1: 회전 탄막 (Tornado)")]
    [SerializeField] private Transform[] p1Muzzles;
    [SerializeField] private int p1BulletCount = 30;
    [SerializeField] private float p1FireRate = 0.1f;
    [SerializeField] private float p1AngleStep = 15f;
    [SerializeField] private float p1BulletSpeed = 5f;
    [SerializeField] private int p1BulletDamage = 1;

    [Header("패턴2: 세로줄 탄막 (Rain & Sweep)")]
    [SerializeField] private float p2MoveRange = 6f;
    [SerializeField] private float p2MoveSpeed = 2f;
    [SerializeField] private float p2RainInterval = 0.5f;
    [SerializeField] private float p2ShootInterval = 1f;
    [SerializeField] private float[] rainXPositions = { -6f, -3f, 0f, 3f, 6f };
    [SerializeField] private float p2RainBulletSpeed = 4f;
    [SerializeField] private float p2AimBulletSpeed = 7f;
    [SerializeField] private int p2BulletDamage = 1;

    [Header("패턴3: 거대 조준 탄막 (Charge & Fire)")]
    [SerializeField] private Transform[] p3Muzzles;
    [SerializeField] private Color p3ChargeColor;
    [SerializeField] private float p3ChargeTime = 2f;
    [SerializeField] private float p3BigBulletSpeed = 10f;
    [SerializeField] private int p3BigBulletDamage = 1; 
    [SerializeField] private float p3FireDelay = 0.2f;

    // ───────────────────────────────
    // 패턴 루프 
    // ───────────────────────────────
    protected override IEnumerator PatternLoop()
    {
        yield return StartCoroutine(PausedWait(1f));

        while (!IsDead)
        {
            switch (CurrentPhase)
            {
                case BossPhase.Phase1:
                    yield return StartCoroutine(Pattern2_VerticalAndSweep());
                    break;
                case BossPhase.Phase2:
                    yield return StartCoroutine(Pattern1_Tornado());
                    break;
                case BossPhase.Phase3:
                    StartCoroutine(Pattern3_AimAndFire());
                    yield return Random.Range(0,2) == 0 ? StartCoroutine(Pattern2_VerticalAndSweep()):StartCoroutine(Pattern1_Tornado());
                    break;
            }

            yield return StartCoroutine(PausedWait(0.5f));
        }
    }

    // ───────────────────────────────
    // 패턴 1: 회전 탄막
    // ───────────────────────────────
    private IEnumerator Pattern1_Tornado()
    {
        float currentAngle = 0f;

        for (int i = 0; i < p1BulletCount; i++)
        {
            yield return StartCoroutine(PausedWait(p1FireRate));

            for (int m = 0; m < p1Muzzles.Length; m++)
            {
                // 5개의 총구라면 360/5 = 72도 간격으로 5갈래 소용돌이가 만들어집니다!
                float angleOffset = (360f / p1Muzzles.Length) * m;
                float mRadian = (currentAngle + angleOffset) * Mathf.Deg2Rad;
                Vector2 mDir = new Vector2(Mathf.Cos(mRadian), Mathf.Sin(mRadian));

                FireBullet(bulletPrefab, p1Muzzles[m].position, mDir, p1BulletSpeed, p1BulletDamage);
            }

            currentAngle += p1AngleStep;
        }
    }

    // ───────────────────────────────
    // 패턴 2: 세로줄 탄막 + 양옆 이동 조준 사격
    // ───────────────────────────────
    private IEnumerator Pattern2_VerticalAndSweep()
    {
        float patternDuration = 6f; 
        float timer = 0f;
        float rainTimer = 0f;
        float shootTimer = 0f;

        while (timer < patternDuration)
        {
            if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
            {
                yield return null;
                continue;
            }

            timer += Time.deltaTime;

            // 1. 보스 좌우 이동
            float newX = Mathf.Sin(timer * p2MoveSpeed) * p2MoveRange;
            transform.position = new Vector2(originalPosition.x + newX, originalPosition.y);

            // 2. 세로줄 비(Rain) 생성
            rainTimer += Time.deltaTime;
            if (rainTimer >= p2RainInterval)
            {
                rainTimer = 0f;
                foreach (float x in rainXPositions)
                {
                    Vector2 spawnPos = new Vector2(x, originalPosition.y);
                    FireBullet(bulletPrefab, spawnPos, Vector2.down, p2RainBulletSpeed, p2BulletDamage);
                }
            }

            // 3. 플레이어 조준 사격
            shootTimer += Time.deltaTime;
            if (shootTimer >= p2ShootInterval)
            {
                shootTimer = 0f;
                if (target != null)
                {
                    Vector2 dirToPlayer = (target.position - transform.position).normalized;
                    FireBullet(bulletPrefab, transform.position, dirToPlayer, p2AimBulletSpeed, p2BulletDamage);
                }
            }

            yield return null;
        }

        yield return StartCoroutine(BackToOriginalPosition(transform.position));
    }

    // ───────────────────────────────
    // 패턴 3: 조준 거대 탄막
    // ───────────────────────────────
    private IEnumerator Pattern3_AimAndFire()
    {
        float timer = 0f;
        Vector2 aimDirection = Vector2.down;

        spriteRenderer.color = p3ChargeColor; 

        GameObject[] chargeBullet = new GameObject[p3Muzzles.Length];
        Vector3 targetScale = Vector3.one;

        for (int i = 0; i < p3Muzzles.Length; i++)
        {
            chargeBullet[i] = Instantiate(bigBulletPrefab, p3Muzzles[i]);
            chargeBullet[i].transform.localPosition = Vector3.zero;
            chargeBullet[i].transform.localScale = Vector3.zero;

            if (chargeBullet[i].TryGetComponent<Collider2D>(out var col))
            {
                col.enabled = false;
            }
        }

        targetScale = bigBulletPrefab.transform.localScale;

        while (timer < p3ChargeTime)
        {
            if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
            {
                yield return null;
                continue;
            }

            timer += Time.deltaTime;
            float progress = timer / p3ChargeTime;

            if (target != null)
            {
                aimDirection = (target.position - transform.position).normalized;
            }
            
            for (int i = 0; i < p3Muzzles.Length; i++)
            {
                chargeBullet[i].transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, progress);

                float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
                chargeBullet[i].transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            }

            yield return null;
        }

        spriteRenderer.color = phase3Color;

        // 거대 탄막 발사
        for (int i = 0; i < p3Muzzles.Length; i++)
        {
            if (target != null)
            {
                aimDirection = (target.position - p3Muzzles[i].position).normalized;
            }
            
            if (chargeBullet[i].TryGetComponent<Collider2D>(out var col))
            {
                col.enabled = true;
            }

            chargeBullet[i].transform.SetParent(null); 

            if (chargeBullet[i].TryGetComponent<BossBullet>(out var bossBullet))
            {
                bossBullet.Initialize(aimDirection, p3BigBulletSpeed, p3BigBulletDamage, this.transform);
            }

            yield return StartCoroutine(PausedWait(p3FireDelay));
        }

        yield return StartCoroutine(PausedWait(0.5f));
    }

    // ───────────────────────────────
    // 공통 탄막 발사 유틸리티 
    // ───────────────────────────────
    private void FireBullet(GameObject prefab, Vector2 spawnPos, Vector2 direction, float speed, int damage)
    {
        if (prefab == null) return;

        GameObject bulletObj = Instantiate(prefab, spawnPos, Quaternion.identity);

        if (bulletObj.TryGetComponent<BossBullet>(out var bossBullet))
        {
            bossBullet.Initialize(direction, speed, damage, this.transform);
        }

        // 시각적 회전 처리 (총알 이미지가 날아가는 방향을 바라보게)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bulletObj.transform.rotation = Quaternion.Euler(0, 0, angle - 90f); 
    }

    // ───────────────────────────────
    // 원래 자리 복귀 코루틴
    // ───────────────────────────────
    private IEnumerator BackToOriginalPosition(Vector2 startPos)
    {
        float returnDuration = 0.5f;
        float elapsed = 0f;

        while (elapsed < returnDuration)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            {
                transform.position = Vector2.Lerp(startPos, originalPosition, elapsed / returnDuration);
                elapsed += Time.deltaTime;
            }
            yield return null;
        }
        transform.position = originalPosition;
    }
}