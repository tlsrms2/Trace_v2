using System;
using UnityEngine;
using System.Collections;

/// <summary>
/// 플레이어 HP 시스템을 담당합니다.
/// 총 3번의 피격을 허용하며, HP 상태를 UI 대신 색상으로 표시합니다.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Player HP Settings")]
    [SerializeField] private int maxHp = 3;
    private int currentHp;

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;

    [Tooltip("피격 후 무적 시간")]
    [SerializeField] private float invincibilityDuration = 1.0f;
    private float invincibilityTimer = 0f;

    [Header("HP Colors")]
    [SerializeField] private Color hp3Color = Color.white;
    [SerializeField] private Color hp2Color = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private Color hp1Color = Color.red;

    public event Action<int, int> OnHpChanged;
    public event Action OnPlayerDeath;

    private SpriteRenderer spriteRenderer;
    private bool isDead = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        currentHp = maxHp;
        UpdatePlayerColor();
        OnHpChanged?.Invoke(currentHp, maxHp);
    }

    private void Update()
    {
        if (invincibilityTimer > 0f)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
                invincibilityTimer -= Time.deltaTime;
        }
    }

    // 데미지 수치와 관계없이 무조건 1의 피해(목숨 -1)를 입습니다.
    public void TakeDamage(int damage)
    {
        if (isDead || currentHp <= 0 || invincibilityTimer > 0f) return;

        currentHp -= 1;
        currentHp = Mathf.Max(currentHp, 0);

        OnHpChanged?.Invoke(currentHp, maxHp);

        if (currentHp <= 0)
        {
            UpdatePlayerColor();
            Die();
        }
        else
        {
            invincibilityTimer = invincibilityDuration;
            StartCoroutine(FlashEffect());
        }
    }

    private void UpdatePlayerColor()
    {
        if (spriteRenderer == null) return;
        
        switch (currentHp)
        {
            case 3: spriteRenderer.color = hp3Color; break;
            case 2: spriteRenderer.color = hp2Color; break;
            case 1: spriteRenderer.color = hp1Color; break;
            default: spriteRenderer.color = Color.black; break; // 사망 시
        }
    }

    private IEnumerator FlashEffect()
    {
        if (spriteRenderer == null) yield break;

        Color targetColor = GetCurrentLevelColor();
        float elapsed = 0f;
        while (elapsed < invincibilityDuration)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            {
                // 반투명 깜빡임 효과
                spriteRenderer.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0.5f);
                yield return new WaitForSeconds(0.1f);
                spriteRenderer.color = targetColor;
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.2f;
            }
            else
            {
                yield return null;
            }
        }
        UpdatePlayerColor();
    }
    
    private Color GetCurrentLevelColor()
    {
        switch (currentHp)
        {
            case 3: return hp3Color;
            case 2: return hp2Color;
            case 1: return hp1Color;
            default: return Color.black;
        }
    }

    private void Die()
    {
        isDead = true;
        OnPlayerDeath?.Invoke();
        GameManager.Instance.GameOver();
        
        // 이동 불가 및 투명화 처리
        if (TryGetComponent<PlayerMove>(out var pm)) pm.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent<BaseBoss>(out var boss))
        {
            TakeDamage(1); 
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.TryGetComponent<BaseBoss>(out var boss))
        {
            TakeDamage(1);
        }
    }
}
