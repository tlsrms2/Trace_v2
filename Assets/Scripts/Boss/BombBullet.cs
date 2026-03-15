using UnityEngine;
using System.Collections;

public class BombBullet : MonoBehaviour
{
    [Header("폭탄(Bomb) 설정")]
    [Tooltip("폭탄이 터지기까지 걸리는 시간(초)")]
    [SerializeField] private float explosionDelay = 2f;
    [Tooltip("폭발 데미지")]
    [SerializeField] private int damage = 10;
    [Tooltip("폭발 범위(반지름)")]
    [SerializeField] private float explosionRadius = 3f;

    [Header("경고 표시 설정")]
    [Tooltip("경고 영역의 색상 (알파값을 낮게 설정하여 반투명하게 만드세요)")]
    [SerializeField] private Color warningColor = new Color(1f, 0f, 0f, 0.3f);
    [Tooltip("경고 영역에 표시할 원형 스프라이트 (필수)")]
    [SerializeField] private Sprite circleSprite;

    private SpriteRenderer warningSpriteRenderer;
    private bool isExploded = false;

    private void Awake()
    {
    }

    private void Start()
    {
        SetupWarningCircle();
        StartCoroutine(ExplosionRoutine());
    }

    private void SetupWarningCircle()
    {
        // 폭탄 본체의 스케일에 영향을 주지 않기 위해 자식 오브젝트로 경고 영역 생성
        GameObject warningObj = new GameObject("WarningCircle");
        warningObj.transform.SetParent(transform);
        warningObj.transform.localPosition = Vector3.zero;
        
        warningSpriteRenderer = warningObj.AddComponent<SpriteRenderer>();
        warningSpriteRenderer.sprite = circleSprite;
        warningSpriteRenderer.color = warningColor;
        
        // 보스나 플레이어의 뒤에 렌더링되도록 Sorting Order 조정
        warningSpriteRenderer.sortingOrder = -1; 

        if (circleSprite != null)
        {
            float spriteSize = circleSprite.bounds.size.x;
            if (spriteSize > 0f)
            {
                // 스프라이트의 실제 크기를 고려하여 지름(radius * 2)에 맞게 스케일 자동 조정
                float scale = (explosionRadius * 2f) / spriteSize;
                // 부모(BombBullet) 오브젝트의 Scale에 영향을 받지 않도록 LossyScale로 나누어 보정합니다.
                warningObj.transform.localScale = new Vector3(
                    scale / transform.lossyScale.x, scale / transform.lossyScale.y, 1f);
            }
        }
    }

    private IEnumerator ExplosionRoutine()
    {
        float timer = 0f;

        while (timer < explosionDelay)
        {
            // Trace(일시정지) 모드가 아닐 때만 타이머 진행
            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            {
                timer += Time.deltaTime;

                // 시간이 지날수록 경고선이 빠르게 깜빡이도록 연출
                float blinkSpeed = Mathf.Lerp(2f, 15f, timer / explosionDelay);
                float alpha = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed)) * warningColor.a;
                
                Color blinkColor = warningColor;
                blinkColor.a = alpha;
                
                if (warningSpriteRenderer != null)
                    warningSpriteRenderer.color = blinkColor;
            }
            yield return null;
        }

        Explode();
    }

    private void Explode()
    {
        if (isExploded) return;
        isExploded = true;

        // 폭발 범위 내 플레이어 탐지 및 데미지 적용
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var col in colliders)
        {
            if (col.CompareTag("Player") && col.TryGetComponent<PlayerHealth>(out var playerHp))
            {
                playerHp.TakeDamage(damage);
            }
        }

        AudioManager.Instance.PlayEnemyDeath();

        Destroy(gameObject);
    }
}