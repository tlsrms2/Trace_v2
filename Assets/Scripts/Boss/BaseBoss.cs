using UnityEngine;
using System;
using System.Collections;
using Random = UnityEngine.Random;

/// <summary>
/// 모든 보스 공통 기반 클래스.
/// HP 관리, 페이즈 전환, PausedWait, 인트로/플래시 효과를 제공합니다.
/// 각 보스 자식 클래스는 PatternLoop()를 override 하여 고유 패턴을 구현합니다.
/// </summary>
public enum BossPhase { Phase1, Phase2, Phase3 }

public abstract class BaseBoss : MonoBehaviour
{
    [SerializeField] protected float patternInterval;
    [Header("페이즈 설정")]
    [SerializeField] protected Color phase1Color;
    [SerializeField] protected Color phase2Color;
    [SerializeField] protected Color phase3Color;

    [Header("기본 스탯")]
    [SerializeField] protected float maxHp = 100f;
    [SerializeField] protected float speed = 3f;
    [SerializeField] private GameObject damagedParticle;

    [Header("인트로 설정")]
    [SerializeField] protected float introDownDistance = 10f;
    [SerializeField] protected float introDuration = 2f;

    [Header("페이즈 설정")]
    [Tooltip("Phase 2 진입 HP 비율 (0~1)")]
    [SerializeField] protected float phase2Threshold = 0.5f;
    [Tooltip("Phase 3 진입 HP 비율 (0~1)")]
    [SerializeField] protected float phase3Threshold = 0.25f;

    // --- 공개 속성 ---
    public float Hp { get; protected set; }
    public float MaxHp => maxHp;
    public BossPhase CurrentPhase { get; private set; } = BossPhase.Phase1;
    public bool IsDead { get; private set; } = false;

    // --- 이벤트 ---
    public event Action<float, float> OnHpChanged;   // (현재HP, 최대HP)
    public event Action<BossPhase> OnPhaseChanged;    // 페이즈 전환 시

    // --- 내부 참조 ---
    protected Transform target;
    protected SpriteRenderer spriteRenderer;
    protected Collider2D col;
    protected Vector2 originalPosition;
    protected Vector3 originalScale;
    protected bool isInvincible = false;

    protected virtual void Awake()
    {
        Hp = maxHp;
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;
    }

    protected virtual void Start()
    {
        originalPosition = transform.position;
        originalScale = transform.localScale;
        StartCoroutine(BossIntroSequence());
    }

    protected virtual void Update()
    {
        CheckPhaseTransition();
        ChangeColor();
    }

    // ───────────────────────────────
    // 페이즈 체크
    // ───────────────────────────────
    private void CheckPhaseTransition()
    {
        if (IsDead) return;

        float ratio = Hp / maxHp;
        BossPhase newPhase = CurrentPhase;

        if (ratio <= phase3Threshold)
            newPhase = BossPhase.Phase3;
        else if (ratio <= phase2Threshold)
            newPhase = BossPhase.Phase2;

        if (newPhase != CurrentPhase)
        {
            CurrentPhase = newPhase;
            OnPhaseChanged?.Invoke(CurrentPhase);
            AudioManager.Instance.PlayBossAppear();
        }
    }

    // ───────────────────────────────
    // 색 변경
    // ───────────────────────────────
    private void ChangeColor()
    {
        switch (CurrentPhase)
        {
            case BossPhase.Phase1: spriteRenderer.color = phase1Color; return;
            case BossPhase.Phase2: spriteRenderer.color = phase2Color; return;
            case BossPhase.Phase3: spriteRenderer.color = phase3Color; return;
            default: return;
        }
    }

    // ───────────────────────────────
    // 데미지 처리 (TakeDamage)
    // ───────────────────────────────
    public void TakeDamage(int damage)
    {
        if (IsDead || isInvincible) return;

        Hp -= damage;
        Hp = Mathf.Max(Hp, 0f);
        OnHpChanged?.Invoke(Hp, maxHp);

        if (Hp <= 0f)
        {
            Die();
        }
        else
        {
            var particle = Instantiate(damagedParticle, transform.position, Quaternion.identity);
        }
    }

    private void Die()
    {
        IsDead = true;
        if (col != null) col.enabled = false;

        // 죽음 처리
        WaveManager.Instance?.OnEnemyKilled();
        AudioManager.Instance.PlayBossDeath();

        var particle = Instantiate(damagedParticle, transform.position, Quaternion.identity);
        ParticleSystem ps = particle.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = Color.red;
        main.startSize = new ParticleSystem.MinMaxCurve(main.startSize.constantMin * 2, main.startSize.constantMax * 2);

        Destroy(gameObject);
    }

    // ───────────────────────────────
    // 유틸: TRACE 모드 중 일시정지 대기
    // ───────────────────────────────
    protected IEnumerator PausedWait(float time)
    {
        float timer = 0f;
        while (timer < time)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
                timer += Time.deltaTime;
            yield return null;
        }
    }

    // ───────────────────────────────
    // 보스 인트로 시퀀스
    // ───────────────────────────────
    private IEnumerator BossIntroSequence()
    {
        isInvincible = true; // 인트로 시작 시 무적 적용
        
        AudioManager.Instance.PlayBossAppear();

        Vector2 startPos = originalPosition + (Vector2.up * introDownDistance);
        transform.position = startPos;

        float timer = 0f;
        while (timer < introDuration)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            {
                timer += Time.deltaTime;
                float t = timer / introDuration;
                t = t * t * (3f - 2f * t); // smoothstep
                transform.position = Vector2.Lerp(startPos, originalPosition, t);
            }
            yield return null;
        }

        transform.position = originalPosition;

        yield return StartCoroutine(ShowWarningEffect(1f));
        yield return StartCoroutine(PausedWait(1f));

        isInvincible = false; // 인트로 종료 시 무적 해제
        StartCoroutine(PatternLoop());
    }

    private IEnumerator ShowWarningEffect(float duration)
    {
        float timer = 0f;
        float shakeMagnitude = 0.05f;

        while (timer < duration)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            {
                float xOffset = Random.Range(-shakeMagnitude, shakeMagnitude);
                float yOffset = Random.Range(-shakeMagnitude, shakeMagnitude);
                transform.position = originalPosition + new Vector2(xOffset, yOffset);
                timer += Time.deltaTime;
            }
            yield return null;
        }

        transform.position = originalPosition;
    }

    // ───────────────────────────────
    // 자식 클래스가 반드시 구현해야 하는 패턴 루프
    // ───────────────────────────────
    protected abstract IEnumerator PatternLoop();
}
