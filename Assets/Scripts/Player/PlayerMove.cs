using UnityEngine;

/// <summary>
/// 플레이어 이동을 담당합니다.
/// - NORMAL(RealTime): WASD로 자유 이동
/// - TRACE(Paused): 고스트를 조종하며 경로 기록 (TraceRecorder가 처리)
/// - REPLAY: TraceReplayer가 플레이어 위치를 제어
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(TraceRecorder))]
[RequireComponent(typeof(TraceReplayer))]
[RequireComponent(typeof(GhostVisual))]
public class PlayerMove : MonoBehaviour
{
    [Header("이동 설정")]
    [Tooltip("평상 시 이동 속도")]
    [SerializeField] private float normalMoveSpeed = 5f;
    [Tooltip("시간 정지(TRACE) 시 이동 속도")]
    [SerializeField] private float traceMoveSpeed = 3f;

    private bool IsTracing => GameManager.Instance.CurrentPhase == GamePhase.Paused;
    private bool IsReplaying => GameManager.Instance.CurrentPhase == GamePhase.Replay;

    private Rigidbody2D playerRigidbody;
    private TraceRecorder recorder;
    private TraceReplayer replayer;
    private float moveSpeed;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody2D>();
        recorder = GetComponent<TraceRecorder>();
        replayer = GetComponent<TraceReplayer>();
    }

    private void Update()
    {
        if (IsTracing)
        {
            moveSpeed = traceMoveSpeed;
        }
        else
        {
            moveSpeed = normalMoveSpeed;
        }
    }

    private void FixedUpdate()
    {
        if (IsReplaying)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            return;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector2 inputDir = new Vector2(x, y).normalized;
        playerRigidbody.linearVelocity = inputDir * moveSpeed;
    }
}
