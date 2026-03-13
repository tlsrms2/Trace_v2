using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WaveTransitionEffect : MonoBehaviour
{
    [Header("Transition Components")]
    [Tooltip("전체 화면을 덮는 하얀색 이미지 (플래시/점멸용)")]
    public Image flashImage;
    
    [Tooltip("WidthScale이나 RandomSpawn 연출이 들어있는 부모 컨테이너")]
    public RectTransform effectContainer;

    [Tooltip("현재 활성화된 시각 효과 스크립트")]
    public WaveVisualEffect activeVisualEffect;

    [Header("Transition Settings")]
    public float flashDuration = 0.2f;    // 한 번 번쩍이는 시간
    public int blinkCount = 3;            // 점멸 횟수
    public float blinkStrength = 0.4f;
    public float wipeDuration = 0.1f;     // 중앙에서 걷어내지는 시간

    private void Start()
    {
        if (flashImage != null)
        {
            // 시작할 때 플래시 이미지는 투명하게, 클릭 무시하게 설정
            flashImage.color = new Color(1, 1, 1, 0);
            flashImage.raycastTarget = false; 
        }

        // WaveManager의 트랜지션 시작 이벤트 구독
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveTransitionStarted += StartTransition;
        }
    }

    private void OnDestroy()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveTransitionStarted -= StartTransition;
        }
    }

    // WaveManager가 콜백 함수를 담아서 이 함수를 호출함
    private void StartTransition(Action onComplete)
    {
        StartCoroutine(TransitionRoutine(onComplete));
    }

    private IEnumerator TransitionRoutine(Action onComplete)
    {
        if (flashImage == null || effectContainer == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        // 1단계: 플래시 & 점멸 (Flash & Blink)
        for (int i = 0; i < blinkCount; i++)
        {
            // 하얗게 불타오름
            yield return StartCoroutine(FadeFlashImage(0f, blinkStrength, flashDuration / 2f));
            // 다시 투명해짐
            yield return StartCoroutine(FadeFlashImage(1f, 0f, flashDuration / 2f));
        }

        // 마지막으로 한 번 더 강하게 번쩍임 유지
        yield return StartCoroutine(FadeFlashImage(0f, blinkStrength, flashDuration));

        // 2단계: 중앙에서부터 이미지 걷어내기 (Wipe Out)
        // 효과 컨테이너의 Scale을 1에서 0으로 줄여서 마치 중앙으로 빨려들어가듯 사라지게 연출
        float timer = 0f;
        Vector3 startScale = Vector3.one;
        Vector3 endScale = Vector3.zero;

        while (timer < wipeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / wipeDuration;
            
            // 부드러운 감속 효과 (Ease Out)
            t = 1f - Mathf.Pow(1f - t, 3f);

            effectContainer.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
        effectContainer.localScale = endScale;

        // 3단계: 화면 가림막(플래시) 걷어내기
        yield return StartCoroutine(FadeFlashImage(1f, 0f, 0.5f));

        // 4단계: 내부 데이터 초기화 및 원상 복구
        if (activeVisualEffect != null)
        {
            activeVisualEffect.ResetEffect();
        }
        
        // 다음 웨이브 연출을 위해 Scale을 다시 1로 돌려놓음 (안 돌려놓으면 안 보임!)
        effectContainer.localScale = Vector3.one;

        // 5단계: 웨이브 매니저에게 "연출 끝났으니 다음 웨이브 시작해!" 라고 콜백 전달
        onComplete?.Invoke();
    }

    private IEnumerator FadeFlashImage(float startAlpha, float endAlpha, float duration)
    {
        float timer = 0f;
        Color c = flashImage.color;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            flashImage.color = c;
            yield return null;
        }

        c.a = endAlpha;
        flashImage.color = c;
    }
}