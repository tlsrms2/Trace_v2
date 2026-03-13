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

    public IReadOnlyList<TraceFrame> RecordedFrames => recordedFrames;
    public bool IsRecording { get; private set; }

    public List<TraceFrame> recordedFrames = new List<TraceFrame>();
    private float recordInterval;
    private float recordTimer;
    private bool attackQueued; // 이번 기록 프레임에 공격을 태그할지 여부

    private void Awake()
    {
        recordInterval = 1f / fpsTrace;
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
            attackQueued = true;
        }

        recordTimer += Time.unscaledDeltaTime;

        if (recordTimer >= recordInterval)
        {
            recordTimer -= recordInterval;
            RecordFrame();
        }
    }

    public void StartRecording()
    {
        recordedFrames.Clear();
        recordTimer = 0f;
        attackQueued = false;
        IsRecording = true;

        RecordFrame();
    }

    public void StopRecording()
    {
        IsRecording = false;
    }

    private void RecordFrame()
    {
        TraceAction action = attackQueued ? TraceAction.ATTACK : TraceAction.MOVE;
        attackQueued = false;

        TraceFrame frame = new TraceFrame(
            transform.position,
            transform.rotation,
            action
        );

        recordedFrames.Add(frame);
    }

    public List<TraceFrame> GetRecordedFramesCopy()
    {
        return new List<TraceFrame>(recordedFrames);
    }
}
