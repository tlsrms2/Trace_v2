using UnityEngine;
using System.Collections;

public class ShootEnemy : Enemy
{
    [Header("총알 관련")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform muzzle;

    [Header("패턴 관련")]
    [SerializeField] private float moveDuration = 1f;
    [SerializeField] private float stopDuration = 0.5f;
    [SerializeField] private float dashSpeed = 8f;
    [SerializeField] private float dashDuration = 1f;

    protected override void Start()
    {
        base.Start();
        StartCoroutine(PatternCoroutine());
    }

    private IEnumerator PatternCoroutine()
    {
        while (true)
        {
            if (GameManager.Instance.CurrentPhase == GamePhase.Paused)
            {
                yield return null;
                continue;
            }

            // 1️⃣ 이동
            float timer = 0f;
            while (timer < moveDuration)
            {
                LookAtPlayer();            // 이동 중에도 플레이어 바라보기
                MoveTowardsPlayer(speed);
                timer += Time.deltaTime;
                yield return null;
            }

            // 2️⃣ 정지하면서도 항상 플레이어 바라보기
            timer = 0f;
            while (timer < stopDuration)
            {
                LookAtPlayer();            // 회전만, 이동은 없음
                timer += Time.deltaTime;
                yield return null;
            }

            // 3️⃣ 발사
            LookAtPlayer();                 // 발사 직전 방향 보정
            Shoot();

            // 4️⃣ 발사 후 정지
            timer = 0f;
            while (timer < stopDuration)
            {
                LookAtPlayer();            // 여전히 플레이어 바라봄
                timer += Time.deltaTime;
                yield return null;
            }

            // 5️⃣ Dash 이동
            timer = 0f;
            while (timer < dashDuration)
            {
                LookAtPlayer();            // 이동 중에도 바라보기
                MoveTowardsPlayer(dashSpeed);
                timer += Time.deltaTime;
                yield return null;
            }

            // 6️⃣ Dash 후 잠시 대기
            timer = 0f;
            while (timer < 1f)
            {
                LookAtPlayer();            // 대기 중에도 바라보기
                timer += Time.deltaTime;
                yield return null;
            }
        }
    }

    private void MoveTowardsPlayer(float currentSpeed)
    {
        if (target == null || GameManager.Instance.CurrentPhase == GamePhase.Paused) return;
        Vector2 dir = (target.position - transform.position).normalized;
        transform.position += (Vector3)(dir * currentSpeed * Time.deltaTime);
    }

    // 적과 muzzle가 항상 플레이어 바라보게
    private void LookAtPlayer()
    {
        if (target == null || muzzle == null || GameManager.Instance.CurrentPhase == GamePhase.Paused) return;

        Vector2 dir = (target.position - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);  // 적 회전
        muzzle.rotation = Quaternion.Euler(0f, 0f, angle);    // muzzle 회전
    }

    private void Shoot()
    {
        if (bulletPrefab == null || muzzle == null || GameManager.Instance.CurrentPhase == GamePhase.Paused) return;
        
        Vector2 dir = muzzle.right;
        GameObject bulletObj = Instantiate(bulletPrefab, muzzle.position, muzzle.rotation);
        AudioManager.Instance.PlayEpicMobShoot();
        ShootEnemyBullet bullet = bulletObj.GetComponent<ShootEnemyBullet>();
        if (bullet != null)
        {
            bullet.Initialize(dir, bullet.speed, bullet.damage, transform);
        }
    }
}