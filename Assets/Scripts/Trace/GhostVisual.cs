using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TRACE 모드에서 고스트를 표시하고, REPLAY 모드에서 잔상 효과를 생성합니다.
/// </summary>
public class GhostVisual : MonoBehaviour
{
    [Header("Ghost Settings")]
    [Tooltip("TRACE 모드에서 표시할 고스트 스프라이트의 알파값")]
    [SerializeField] private float ghostAlpha = 0.4f;
    [Tooltip("고스트 스프라이트 색상 (RGB)")]
    [SerializeField] private Color ghostColor = new Color(0f, 0.8f, 1f, 0.4f);

    [Header("Trail Settings (REPLAY)")]
    [Tooltip("잔상 생성 간격 (초)")]
    [SerializeField] private float trailSpawnInterval;
    [Tooltip("잔상 페이드아웃 시간")]
    [SerializeField] private float trailFadeDuration = 0.3f;

    [Header("Attack Marker")]
    [Tooltip("TRACE 모드 클릭 시 공격 마커 색상")]
    [SerializeField] private Color attackMarkerColor = new Color(1f, 0.3f, 0.3f, 0.7f);
    [Tooltip("공격 마커 크기")]
    [SerializeField] private float attackMarkerSize = 0.5f;

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
        playerSpriteRenderer = GetComponent<SpriteRenderer>();
        recorder = GetComponent<TraceRecorder>();

        if (playerSpriteRenderer != null)
        {
            originalColor = playerSpriteRenderer.color;
        }
    }

    private void Update()
    {
        bool isTracing = GameManager.Instance.CurrentPhase == GamePhase.Paused;
        bool isReplaying = GameManager.Instance.CurrentPhase == GamePhase.Replay;
        trailSpawnInterval = 1f / recorder.GetRecordedFramesCopy().Count;

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
            UpdateReplayTrail();
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
            SpawnAttackMarker(transform.position);
        }
    }

    private void OnExitTrace()
    {
        playerSpriteRenderer.color = originalColor;
    }

    private void UpdateReplayTrail()
    {
        trailTimer += Time.deltaTime;
        if (trailTimer >= trailSpawnInterval)
        {
            trailTimer -= trailSpawnInterval;
            SpawnTrailGhost();
        }
    }

    private void OnExitReplay()
    {
        trailTimer = 0f;
        ClearAttackMarkers();
    }

    private void SpawnTrailGhost()
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

        // 페이드아웃 후 자동 삭제 (이거 삭제 예정임)
        TrailGhostFade fader = ghost.AddComponent<TrailGhostFade>();
        fader.Initialize(trailFadeDuration, ghostColor);
    }

    private void SpawnAttackMarker(Vector3 position)
    {
        GameObject marker = new GameObject("AttackMarker");
        marker.transform.position = position;

        SpriteRenderer sr = marker.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = attackMarkerColor;
        sr.sortingOrder = 100;
        marker.transform.localScale = Vector3.one * attackMarkerSize;

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

    private Sprite CreateCircleSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size / 2f;
        Color transparent = new Color(0, 0, 0, 0);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                tex.SetPixel(x, y, dist <= radius ? Color.white : transparent);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}

/// <summary>
/// 잔상 스프라이트의 페이드아웃 처리
/// </summary>
public class TrailGhostFade : MonoBehaviour
{
    private float duration;
    private float timer;
    private SpriteRenderer sr;
    private Color startColor;

    public void Initialize(float fadeDuration, Color color)
    {
        duration = fadeDuration;
        startColor = color;
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = timer / duration;

        if (sr != null)
        {
            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, t);
            sr.color = c;
        }

        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
}
