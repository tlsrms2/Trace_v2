using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TRACE 모드에서 고스트를 표시하고, REPLAY 모드에서 잔상 효과를 생성합니다.
/// </summary>
public class GhostVisual : MonoBehaviour
{
    [Header("Ghost Settings")]
    private List<GameObject> ghostList = new List<GameObject>();
    [Tooltip("TRACE 모드에서 표시할 고스트 스프라이트의 알파값")]
    [SerializeField] private float ghostAlpha = 0.4f;
    [Tooltip("고스트 스프라이트 색상 (RGB)")]
    [SerializeField] private Color ghostColor = new Color(0f, 0.8f, 1f, 0.4f);

    [Header("Trail Settings (REPLAY)")]
    [Tooltip("잔상 생성 간격 (초)")]
    [SerializeField] private float trailSpawnInterval = 1f;
    [Tooltip("잔상 페이드아웃 시간")]
    [SerializeField] private float trailFadeDuration = 0.3f;
    private bool isFirstGhost = true;

    [Header("Attack Marker")]
    [Tooltip("공격 위치를 표시할 프리팹")]
    [SerializeField] private GameObject attackMarkerPrefab;
    [Tooltip("TRACE 모드 클릭 시 공격 마커 색상")]
    [SerializeField] private Color attackMarkerColor = new Color(1f, 0.3f, 0.3f, 0.7f);

    private PlayerHealth playerHP;
    private SpriteRenderer playerSpriteRenderer;
    private TraceRecorder recorder;
    private Color originalColor;
    private bool wasTracing;
    private bool wasReplaying;
    private float trailTimer;


    // 공격 마커 오브젝트들 
    private List<GameObject> attackMarkers = new List<GameObject>();

    private void Awake()
    {
        playerHP = GetComponent<PlayerHealth>();
        recorder = GetComponent<TraceRecorder>();
        playerSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (playerHP != null)
        {
            playerHP.OnHpChanged += UpdateOriginalColor;
        }
    }

    private void OnDisable()
    {
        if (playerHP != null)
        {
            playerHP.OnHpChanged -= UpdateOriginalColor;
        }
    }

    private void Update()
    {
        bool isTracing = GameManager.Instance.CurrentPhase == GamePhase.Paused;
        bool isReplaying = GameManager.Instance.CurrentPhase == GamePhase.Replay;

        // === TRACE 모드 진입 ===
        if (isTracing && !wasTracing)
        {
            OnEnterTrace();
        }

        // === TRACE 모드 중 ===
        if (isTracing)
        {
            UpdateTraceVisuals();
        }

        // === TRACE 모드 종료 ===
        if (!isTracing && wasTracing)
        {
            OnExitTrace();
        }

        // === REPLAY 모드 중 ===
        if (isReplaying)
        {
            
        }

        // === REPLAY 모드 종료 ===
        if (!isReplaying && wasReplaying)
        {
            OnExitReplay();
        }

        wasTracing = isTracing;
        wasReplaying = isReplaying;
    }

    private void OnEnterTrace()
    {
        playerSpriteRenderer.color = ghostColor;
        ClearAttackMarkers();
    }

    private void UpdateTraceVisuals()
    {
        if (Input.GetMouseButtonDown(0))
        {
            float attackCost = GameManager.Instance.GetAttackConsumption();
            if (GameManager.Instance.GetCurrentGauge() >= attackCost)
            {
                // 마우스 방향 계산
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = transform.position.z;
                Vector3 attackDir = (mousePos - transform.position).normalized;

                // 공격 마커 생성 시 플레이어의 위치에 잔상도 함께 생성
                SpawnTrailGhost();
                SpawnAttackMarker(transform.position, attackDir);
            }
        }
    }

    private void OnExitTrace()
    {
        playerSpriteRenderer.color = originalColor;
        isFirstGhost = true;
    }

    private void OnExitReplay()
    {
        trailTimer = 0f;
        ghostList.Clear();
        ClearAttackMarkers();
    }

    public void SpawnTrailGhost()
    {
        if (playerSpriteRenderer == null) return;

        GameObject ghost = new GameObject("TrailGhost");
        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.localScale;

        SpriteRenderer sr = ghost.AddComponent<SpriteRenderer>();
        sr.sprite = playerSpriteRenderer.sprite;
        sr.color = ghostColor;
        sr.sortingLayerName = playerSpriteRenderer.sortingLayerName;
        sr.sortingOrder = playerSpriteRenderer.sortingOrder - 1;

        ghostList.Add(ghost);
    }

    private void SpawnAttackMarker(Vector3 position, Vector3 direction)
    {
        if (attackMarkerPrefab == null) return;

        // 플레이어의 위치에서 바라본 방향으로 1.7f만큼 떨어진 지점을 생성 위치로 설정
        Vector3 markerPosition = position + direction * 0.7f;

        // 계산된 위치에 프리팹 생성
        GameObject marker = Instantiate(attackMarkerPrefab, markerPosition, Quaternion.identity);
        
        // 공격 방향을 바라보도록 회전 (스프라이트의 위쪽이 앞면이라고 가정)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        marker.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        marker.transform.localScale = Vector3.one * 0.27f;

        if (marker.TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.color = attackMarkerColor;
        }

        attackMarkers.Add(marker);
    }

    private void ClearAttackMarkers()
    {
        foreach (var marker in attackMarkers)
        {
            if (marker != null)
                Destroy(marker);
        }
        attackMarkers.Clear();
    }

    public List<GameObject> GetGhostListCopy()
    {
        return new List<GameObject>(ghostList);
    }

    public float GetTrailSpawnInterval()
    {
        return trailSpawnInterval;
    }

    private void UpdateOriginalColor(int currentHp, int maxHp)
    {
        switch (currentHp)
        {
            case 3: originalColor = Color.white; break;
            case 2: originalColor = new Color(1f, 0.5f, 0f); break; 
            case 1: originalColor = Color.red; break;
            default: break;
        }
    }
}
