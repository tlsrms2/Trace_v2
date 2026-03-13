using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// REPLAY 모드에서 기록된 TraceFrame 리스트를 고속으로 재생합니다.
/// </summary>
public class TraceReplayer : MonoBehaviour
{
    [Header("Replay Settings")]
    [Tooltip("전체 기록을 재생하는 데 걸리는 시간 (초)")]
    [SerializeField] private float replayDuration = 1.0f;

    [Header("Attack Settings")]
    [Tooltip("ATTACK 프레임에서 보스에게 데미지를 줄 반경")]
    [SerializeField] private float attackRange = 1.5f;
    [Tooltip("ATTACK 1회당 데미지")]
    [SerializeField] private int attackDamage = 10;
    [Tooltip("공격 판정 대상 레이어")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Attack VFX")]
    [SerializeField] private GameObject attackEffectPrefab;

    public bool IsReplaying { get; private set; }

    public event Action OnReplayFinished;

    private TraceRecorder recorder;

    private void Awake()
    {
        recorder = GetComponent<TraceRecorder>();
    }

    private void Start()
    {
        GameManager.Instance.OnTraceEnded += OnTraceEnded;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTraceEnded -= OnTraceEnded;
        }
    }

    private void OnTraceEnded()
    {
        if (recorder == null || recorder.RecordedFrames.Count < 2) 
        {
            GameManager.Instance.ChangePhase(GamePhase.RealTime);
            return;
        }

        StartCoroutine(ReplayCoroutine(recorder.GetRecordedFramesCopy()));
    }

    private IEnumerator ReplayCoroutine(List<TraceFrame> frames)
    {
        IsReplaying = true;
        int frameCount = frames.Count;

        float frameInterval = replayDuration / (frameCount - 1);
        float elapsed = 0f;

        // 첫 프레임 이전의 ATTACK은 무시할 HashSet (중복 방지)
        HashSet<int> attackedFrameIndices = new HashSet<int>();

        for (int i = 0; i < frameCount - 1; i++)
        {
            TraceFrame from = frames[i];
            TraceFrame to = frames[i + 1];

            float segmentElapsed = 0f;

            while (segmentElapsed < frameInterval)
            {
                segmentElapsed += Time.deltaTime;
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(segmentElapsed / frameInterval);

                // 위치 보간
                transform.position = Vector3.Lerp(from.position, to.position, t);
                transform.rotation = Quaternion.Slerp(from.rotation, to.rotation, t);

                yield return null;
            }

            transform.position = to.position;
            transform.rotation = to.rotation;

            if (to.action == TraceAction.ATTACK && !attackedFrameIndices.Contains(i + 1))
            {
                attackedFrameIndices.Add(i + 1);
                PerformAttack(to.position);
            }
        }

        IsReplaying = false;
        OnReplayFinished?.Invoke();
        GameManager.Instance.ChangePhase(GamePhase.RealTime);
    }

    private void PerformAttack(Vector3 position)
    {
        // 범위 내 적 탐색
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, attackRange, enemyLayer);

        foreach (var hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Enemy.TakeDamage를 직접 호출
                enemy.TakeDamage(attackDamage, true);
            }
        }

        if (attackEffectPrefab != null)
        {
            Instantiate(attackEffectPrefab, position, Quaternion.identity);
        }

        // Screen Shake
        // ScreenShake.Instance?.Shake();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

