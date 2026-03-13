using UnityEngine;

public abstract class WaveVisualEffect : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("목표 수치까지 도달하는 속도")]
    public float lerpSpeed = 5f;
    
    [Header("Oscillation (요동 연출) Settings")]
    [Tooltip("진동하는 속도 (음향 그라데이션 느낌)")]
    public float oscillationSpeed = 10f;
    [Tooltip("진동하는 폭 (오차 범위)")]
    public float oscillationMagnitude = 0.02f;

    protected float targetProgress = 0f;  // 웨이브 매니저가 알려주는 실제 진행도 (0 ~ 1)
    protected float currentProgress = 0f; // 부드럽게 따라가는 현재 진행도 (0 ~ 1)

    // 외부(UIManager)에서 진행도를 업데이트할 때 호출하는 함수
    public void SetProgress(float progress)
    {
        // progress는 0(시작) ~ 1(웨이브 완료) 사이의 값
        targetProgress = Mathf.Clamp01(progress);
    }

    protected virtual void Update()
    {
        // 1. 점진적으로 목표치에 다가감 (Lerp)
        currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * lerpSpeed);

        // 2. 사인파(Sin)를 이용해 현재 값 주변에서 요동치는 오차값 생성
        float noise = Mathf.Sin(Time.time * oscillationSpeed) * oscillationMagnitude;

        // 3. 최종적으로 화면에 적용될 값 (0~1 사이로 고정)
        float visualProgress = Mathf.Clamp01(currentProgress + noise);

        // 4. 자식 클래스들에게 이 값을 화면에 그리라고 명령
        ApplyVisuals(visualProgress);
    }

    // 자식 클래스들이 반드시 본인들만의 연출 방식으로 구현해야 하는 함수
    protected abstract void ApplyVisuals(float progress);

    // 웨이브가 끝난 후 화면을 완전히 초기화할 때 호출됨
    public virtual void ResetEffect()
    {
        targetProgress = 0f;
        currentProgress = 0f;
        ApplyVisuals(0f);
    }
}