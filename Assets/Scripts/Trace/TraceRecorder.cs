using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TRACE 모드(GamePhase.Paused)에서 플레이어의 행동을 고정 FPS로 기록합니다.
/// </summary>
public class TraceRecorder : MonoBehaviour
{
    [Header("Recording Settings")]
    [Tooltip("초당 기록 프레임 수")]
    [SerializeField] private int fpsTrace = 10;
    [Tooltip("기록을 위한 최소 이동 거리")]
    [SerializeField] private float moveThreshold = 0.05f;

    public IReadOnlyList<TraceFrame> RecordedFrames => recordedFrames;
    public bool IsRecording { get; private set; }

    public List<TraceFrame> recordedFrames = new List<TraceFrame>();
    public float RecordInterval { get; private set; }
    private float recordTimer;
    private bool attackQueued; // 이번 기록 프레임에 공격을 태그할지 여부
    private Vector3 attackDir; // 이번 기록 프레임의 공격 방향
    private Vector3 lastRecordedPosition;

    private GhostVisual gv;
    private List<GameObject> traceIndicators = new List<GameObject>();

    private void Awake()
    {
        gv = GetComponent<GhostVisual>();
        RecordInterval = 1f / fpsTrace;
    }

    private void Start()
    {
        GameManager.Instance.OnTraceStarted += StartRecording;
        GameManager.Instance.OnTraceEnded += StopRecording;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTraceStarted -= StartRecording;
            GameManager.Instance.OnTraceEnded -= StopRecording;
        }
    }

    private void Update()
    {
        if (!IsRecording) return;

        if (Input.GetMouseButtonDown(0))
        {
            float attackCost = GameManager.Instance.GetAttackConsumption();
            if (GameManager.Instance.GetCurrentGauge() >= attackCost)
            {
                attackQueued = true;
                GameManager.Instance.ConsumeGauge(attackCost);

                // 마우스 방향 계산
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = transform.position.z;
                attackDir = (mousePos - transform.position).normalized;

                // 트레이스 중에 임시로 지정된 공격 위치를 보여주는 인디케이터 생성
                SpawnTraceIndicator(transform.position, attackDir);
            }
        }

        recordTimer += Time.unscaledDeltaTime;

        if (recordTimer >= RecordInterval)
        {
            recordTimer -= RecordInterval;
            
            // 움직임이 있거나 공격이 예약된 경우에만 기록
            float dist = Vector3.Distance(transform.position, lastRecordedPosition);
            if (dist >= moveThreshold || attackQueued)
            {
                RecordFrame();
            }
        }
    }

    private void SpawnTraceIndicator(Vector3 pos, Vector3 dir)
    {
        GameObject indicator = new GameObject("TraceAttackIndicator");
        indicator.transform.position = pos + dir * 1.5f; // attackRange
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        indicator.transform.rotation = Quaternion.Euler(0, 0, angle);

        SpriteRenderer sr = indicator.AddComponent<SpriteRenderer>();
        // 임시로 사각형 스프라이트 생성 처리 (빌트인)
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        sr.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        indicator.transform.localScale = new Vector3(2f, 1f, 1f);

        traceIndicators.Add(indicator);
    }

    public void StartRecording()
    {
        recordedFrames.Clear();
        foreach (var ind in traceIndicators) if (ind != null) Destroy(ind);
        traceIndicators.Clear();

        recordTimer = 0f;
        attackQueued = false;
        IsRecording = true;
        lastRecordedPosition = transform.position;

        RecordFrame();
    }

    public void StopRecording()
    {
        IsRecording = false;
        foreach (var ind in traceIndicators) if (ind != null) Destroy(ind);
        traceIndicators.Clear();
    }

    private void RecordFrame()
    {
        TraceAction action = attackQueued ? TraceAction.ATTACK : TraceAction.MOVE;
        Vector3 dir = attackQueued ? attackDir : Vector3.zero;
        attackQueued = false;

        TraceFrame frame = new TraceFrame(
            transform.position,
            transform.rotation,
            action,
            dir
        );

        recordedFrames.Add(frame);
        lastRecordedPosition = transform.position;
        
        gv.SpawnTrailGhost();
    }

    public List<TraceFrame> GetRecordedFramesCopy()
    {
        return new List<TraceFrame>(recordedFrames);
    }
}
