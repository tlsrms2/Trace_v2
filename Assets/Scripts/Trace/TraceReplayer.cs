using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// REPLAY 모드에서 기록된 TraceFrame 리스트를 고속으로 재생합니다.
/// </summary>
public class TraceReplayer : MonoBehaviour
{
    [Header("Replay Settings")]
    [Tooltip("실제 기록된 시간보다 얼마나 빠르게 재생할지 배속 설정")]
    [SerializeField] private float replaySpeedMultiplier = 3.0f;

    [Header("Attack Settings")]
    [Tooltip("Player의 자식 오브젝트에 있는 PlayerAttack 컴포넌트")]
    [SerializeField] private PlayerAttack playerAttack;

    public bool IsReplaying { get; private set; }

    public event Action OnReplayFinished;

    private TraceRecorder recorder;
    private GhostVisual gv;

    private void Awake()
    {
        recorder = GetComponent<TraceRecorder>();
        gv = GetComponent<GhostVisual>();
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
        StartCoroutine(ReplayCoroutine(recorder.GetRecordedFramesCopy(), recorder.GetRecordedAttacksCopy()));
    }

    [Header("Ghost Trail Settings")]
    [Tooltip("플레이어와 잔상이 이 거리 이내면 잔상을 소멸시킴")]
    [SerializeField] private float ghostOverlapThreshold = 0.3f;

    private IEnumerator ReplayCoroutine(List<TraceFrame> frames, List<TraceAttackData> attacks)
    {
        IsReplaying = true;
        int frameCount = frames.Count;

        // 원본 기록 간격을 배속으로 나누어 실제 재생 프레임 간격 계산
        float frameInterval = recorder.RecordInterval / replaySpeedMultiplier;

        float replayElapsed = 0f; // 배속을 고려한 원본 경과 시간
        int attackIndex = 0;      // 실행 대기 중인 공격의 인덱스

        // 잔상 리스트 가져오기
        List<GameObject> ghostList = gv.GetGhostListCopy();

        for (int i = 0; i < frameCount - 1; i++)
        {
            TraceFrame from = frames[i];
            TraceFrame to = frames[i + 1];

            float segmentElapsed = 0f;

            while (segmentElapsed < frameInterval)
            {
                segmentElapsed += Time.deltaTime;
                replayElapsed += Time.deltaTime * replaySpeedMultiplier; 

                // 현재 진행 시간에 도달한 공격이 있으면 독립적으로 발동
                while (attackIndex < attacks.Count && replayElapsed >= attacks[attackIndex].time)
                {
                    PerformAttack(attacks[attackIndex].position, attacks[attackIndex].attackDirection);
                    attackIndex++;
                }

                float t = Mathf.Clamp01(segmentElapsed / frameInterval);

                transform.position = Vector3.Lerp(from.position, to.position, t);
                transform.rotation = Quaternion.Slerp(from.rotation, to.rotation, t);

                // 매 프레임마다 플레이어와 겹치는 잔상 제거
                RemoveOverlappingGhosts(ghostList);

                yield return null;
            }

            transform.position = to.position;
            transform.rotation = to.rotation;
        }
        
        // 리플레이 이동이 모두 끝났으나 아직 발동되지 않은 마지막 공격들이 있다면 기록된 시간에 맞춰 발동
        while (attackIndex < attacks.Count)
        {
            while (replayElapsed < attacks[attackIndex].time)
            {
                replayElapsed += Time.deltaTime * replaySpeedMultiplier; 
                RemoveOverlappingGhosts(ghostList);
                yield return null;
            }
            PerformAttack(attacks[attackIndex].position, attacks[attackIndex].attackDirection);
            attackIndex++;
        }

        // 혹시 남은 잔상 전부 제거
        foreach (var ghost in ghostList)
        {
            if (ghost != null)
                Destroy(ghost);
        }

        IsReplaying = false;
        OnReplayFinished?.Invoke();
        GameManager.Instance.ChangePhase(GamePhase.RealTime);
    }

    private void RemoveOverlappingGhosts(List<GameObject> ghostList)
    {
        Vector3 playerPos = transform.position;

        for (int i = 0; i < ghostList.Count; i++)
        {
            if (ghostList[i] == null) continue;

            float dist = Vector3.Distance(playerPos, ghostList[i].transform.position);
            if (dist <= ghostOverlapThreshold)
            {
                Destroy(ghostList[i]);
            }
        }
    }

    private void PerformAttack(Vector3 position, Vector3 direction)
    {
        if (playerAttack != null)
        {
            playerAttack.Attack(direction);
        }
    }
}
