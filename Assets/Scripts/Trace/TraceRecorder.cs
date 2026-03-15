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
    public IReadOnlyList<TraceAttackData> RecordedAttacks => recordedAttacks;
    public bool IsRecording { get; private set; }

    public List<TraceFrame> recordedFrames = new List<TraceFrame>();
    public List<TraceAttackData> recordedAttacks = new List<TraceAttackData>();
    public float RecordInterval { get; private set; }
    private float recordTimer;
    private float totalElapsedRecordTime; // 트레이스 시작 후 총 경과 시간
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
        
        totalElapsedRecordTime += Time.unscaledDeltaTime;

        if (Input.GetMouseButtonDown(0))
        {
            float attackCost = GameManager.Instance.GetAttackConsumption();
            if (GameManager.Instance.GetCurrentGauge() >= attackCost)
            {
                GameManager.Instance.ConsumeGauge(attackCost);

                // 마우스 방향 계산
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = transform.position.z;
                Vector3 currentAttackDir = (mousePos - transform.position).normalized;
                
                // 클릭 즉시 공격 데이터를 독립적으로 새겨놓음
                recordedAttacks.Add(new TraceAttackData
                {
                    time = totalElapsedRecordTime,
                    position = transform.position,
                    attackDirection = currentAttackDir
                });
            }
        }

        recordTimer += Time.unscaledDeltaTime;

        if (recordTimer >= RecordInterval)
        {
            recordTimer -= RecordInterval;
            
            // 움직임이 있는 경우에만 위치 프레임 기록
            float dist = Vector3.Distance(transform.position, lastRecordedPosition);
            if (dist >= moveThreshold)
            {
                RecordFrame();
            }
        }
    }

    public void StartRecording()
    {
        recordedFrames.Clear();
        recordedAttacks.Clear();
        foreach (var ind in traceIndicators) if (ind != null) Destroy(ind);
        traceIndicators.Clear();

        recordTimer = 0f;
        totalElapsedRecordTime = 0f;
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
        TraceFrame frame = new TraceFrame(
            transform.position,
            transform.rotation,
            TraceAction.MOVE,
            Vector3.zero
        );

        recordedFrames.Add(frame);
        lastRecordedPosition = transform.position;
        
        gv.SpawnTrailGhost();
    }

    public List<TraceFrame> GetRecordedFramesCopy()
    {
        return new List<TraceFrame>(recordedFrames);
    }

    public List<TraceAttackData> GetRecordedAttacksCopy()
    {
        return new List<TraceAttackData>(recordedAttacks);
    }
}
