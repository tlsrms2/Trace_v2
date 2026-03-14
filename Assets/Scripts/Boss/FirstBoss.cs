using UnityEngine;
using System.Collections;

public struct specialAttackInfo
{
    public Vector3 startPos;
    public Vector3 endPos;
    public Vector3 direction;
}

/// <summary>
/// 첫 번째 보스. BaseBoss를 상속받아 대쉬/탄막/특수 레이저 패턴을 구현합니다.
/// </summary>
public class FirstBoss : BaseBoss
{
    [Header("패턴2: 탄막")]
    [SerializeField] private int bulletDamage = 5;
    [SerializeField] private Transform[] muzzles;
    [SerializeField] private float bulletSpeed = 6f;
    [SerializeField] private GameObject bulletPrefab;
    private bool shootCross = true;

    [Header("패턴3: 특수 레이저")]
    [SerializeField] private float targetScaleMultiplier;
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private Color pathColor = new Color(1, 0, 0, 0.5f);

    private specialAttackInfo[] specialInfo = new specialAttackInfo[4];
    private Camera mainCam;
    private Vector3 directionV3;
    private float initialWidth;
    private LineRenderer lineRenderer;

    protected override void Awake()
    {
        base.Awake();
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null) lineRenderer.enabled = false;
        mainCam = Camera.main;
    }
    
    private float PhaseBulletSpeed() => CurrentPhase switch
    {
        BossPhase.Phase2 => bulletSpeed * 1.4f,
        BossPhase.Phase3 => bulletSpeed * 1.9f,
        _ => bulletSpeed
    };

    // ───────────────────────────────
    // 패턴 루프 
    // ───────────────────────────────
    protected override IEnumerator PatternLoop()
    {
        while (true)
        {
            Dash();
            yield return StartCoroutine(PausedWait(1f));
            Dash();
            yield return StartCoroutine(PausedWait(1f));
            StartCoroutine(BackToOriginalPosition(transform.position));
            yield return StartCoroutine(PausedWait(2f));

            int shootCount = (CurrentPhase == BossPhase.Phase3) ? 4 : 3;
            for (int i = 0; i < shootCount; i++)
            {
                Shoot();
                yield return StartCoroutine(PausedWait(0.5f));
            }

            yield return StartCoroutine(PausedWait(1.5f));
            yield return StartCoroutine(SpecialAttack());
            yield return StartCoroutine(PausedWait(2f));
        }
    }

    // ───────────────────────────────
    // 패턴 1: 대쉬 공격
    // ───────────────────────────────
    private void Dash()
    {
        AudioManager.Instance.PlayEpicMobDash();
        Vector2 dir = (target.position - transform.position).normalized;
        StartCoroutine(DashAttackRoutine(dir));
    }

    private IEnumerator DashAttackRoutine(Vector2 dir)
    {
        // TRACE 중이면 대기
        while (GameManager.Instance.CurrentPhase == GamePhase.Paused)
            yield return null;

        // 원래 색상 저장
        Color originalColor = spriteRenderer.color;

        spriteRenderer.color = Color.red;
        yield return StartCoroutine(PausedWait(0.25f));

        spriteRenderer.color = Color.green;
        float dashDuration = 0.25f;
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            {
                transform.position += (Vector3)dir * speed * Time.deltaTime;
                elapsed += Time.deltaTime;
            }
            yield return null;
        }

        // 원래 색상으로 복원
        spriteRenderer.color = originalColor;
    }

    private IEnumerator BackToOriginalPosition(Vector2 startPos)
    {
        transform.localScale = new Vector3(2f, 2f, 2f);

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
        transform.localScale = originalScale;
    }

    // ───────────────────────────────
    // 패턴 2: 탄막 발사
    // ───────────────────────────────
    private void Shoot()
    {
        AudioManager.Instance.PlayEpicMobShoot();
        float speed = PhaseBulletSpeed();

        for (int i = 0; i < muzzles.Length; i++)
        {
            bool shouldShoot = (shootCross && i % 2 == 0) || (!shootCross && i % 2 == 1);
            if (shouldShoot)
            {
                GameObject bulletObj = Instantiate(bulletPrefab, muzzles[i].position, muzzles[i].rotation);
                bulletObj.GetComponent<BossBullet>().Initialize(muzzles[i].right, speed, bulletDamage, transform);
            }
        }

        shootCross = !shootCross;
    }

    // ───────────────────────────────
    // 패턴 3: 레이저 특수 공격
    // ───────────────────────────────
    private specialAttackInfo ReadySpecialAttack(int step)
    {
        specialAttackInfo info = new specialAttackInfo();
        if (mainCam == null) return info;

        float camH = mainCam.orthographicSize;
        float camW = camH * mainCam.aspect;
        Vector3 camPos = mainCam.transform.position;

        float safeY = camH * 0.95f;
        float safeX = camW * 0.95f;
        float outX = camW + 2f;
        float outY = camH + 2f;

        Vector3 startPos = Vector3.zero;

        switch (step)
        {
            case 0: startPos = new Vector3(Random.Range(-safeX, safeX) + camPos.x, camPos.y + outY, 0f); directionV3 = Vector3.down;  break;
            case 1: startPos = new Vector3(camPos.x - outX, Random.Range(-safeY, safeY) + camPos.y, 0f); directionV3 = Vector3.right; break;
            case 2: startPos = new Vector3(Random.Range(-safeX, safeX) + camPos.x, camPos.y - outY, 0f); directionV3 = Vector3.up;    break;
            case 3: startPos = new Vector3(camPos.x + outX, Random.Range(-safeY, safeY) + camPos.y, 0f); directionV3 = Vector3.left;  break;
        }

        info.startPos = startPos;
        info.endPos   = startPos + directionV3 * (Mathf.Abs(directionV3.x) > 0 ? outX * 2 : outY * 2);
        info.direction = directionV3;

        return info;
    }

    private IEnumerator SpecialAttack()
    {
        Vector2 startPos = transform.position;
        Vector2 targetPos = startPos + Vector2.up * introDownDistance;
        Vector3 startScale  = originalScale;
        Vector3 targetScale = startScale * targetScaleMultiplier;

        float scaleDuration = 1f;
        float scaleTimer = 0f;

        while (scaleTimer < scaleDuration)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            {
                scaleTimer += Time.deltaTime;
                float t = scaleTimer / scaleDuration;
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                transform.position   = Vector2.Lerp(startPos, targetPos, t);
            }
            yield return null;
        }

        transform.position   = targetPos;
        transform.localScale = targetScale;

        yield return StartCoroutine(PausedWait(1f));

        LineRenderer[] warningLines = new LineRenderer[4];
        initialWidth = 1.0f;

        for (int i = 0; i < 4; i++)
        {
            specialInfo[i] = ReadySpecialAttack(i);

            GameObject lineObj = new GameObject($"WarningLine_{i}");
            LineRenderer lr    = lineObj.AddComponent<LineRenderer>();

            lr.positionCount = 2;
            lr.SetPosition(0, specialInfo[i].startPos);
            lr.SetPosition(1, specialInfo[i].endPos);
            lr.startWidth = initialWidth;
            lr.endWidth   = initialWidth;
            lr.material   = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = pathColor;
            lr.endColor   = pathColor;

            warningLines[i] = lr;
            AudioManager.Instance.PlayLaserWarning();

            yield return StartCoroutine(PausedWait(0.25f));
        }

        for (int i = 0; i < 4; i++)
        {
            // 경고선 점멸
            float alertMax = 0.3f;
            float alertTimer = alertMax;
            while (alertTimer > 0f)
            {
                if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
                {
                    alertTimer -= Time.deltaTime;
                    float ratio = Mathf.Clamp01(alertTimer / alertMax);
                    float blinkSpeed = Mathf.Lerp(30f, 5f, ratio);
                    float alpha = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));

                    Color c = pathColor;
                    c.a = alpha;
                    warningLines[i].startColor = c;
                    warningLines[i].endColor   = c;
                    warningLines[i].startWidth = initialWidth;
                    warningLines[i].endWidth   = initialWidth;
                }
                yield return null;
            }

            Destroy(warningLines[i].gameObject);
            yield return StartCoroutine(SpecialAttackDash(specialInfo[i]));
        }

        yield return StartCoroutine(BackToOriginalPosition(originalPosition + Vector2.up * introDownDistance));
    }

    private IEnumerator SpecialAttackDash(specialAttackInfo info)
    {
        AudioManager.Instance.PlayEpicMobDash();
        transform.position = info.startPos;

        float dashTimer    = 0f;
        float dashDuration = 0.5f;

        while (dashTimer < dashDuration)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            {
                dashTimer += Time.deltaTime;
                transform.position += info.direction * dashSpeed * Time.deltaTime;
            }
            yield return null;
        }
    }
}