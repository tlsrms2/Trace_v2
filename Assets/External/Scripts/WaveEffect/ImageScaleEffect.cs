using UnityEngine;

public class ImageScaleEffect : WaveVisualEffect
{
    // Inspector에서 선택할 수 있도록 Enum(열거형) 선언
    public enum FillDirection
    {
        LeftToRight, // 왼쪽에서 오른쪽으로 채우기
        RightToLeft, // 오른쪽에서 왼쪽으로 채우기
        BottomToTop, // 아래에서 위로 채우기
        TopToBottom  // 위에서 아래로 채우기
    }

    [Header("Scale Effect Settings")]
    [Tooltip("크기가 조절될 이미지의 RectTransform")]
    public RectTransform targetImageRect;
    
    [Tooltip("이미지가 채워지는 방향을 선택하세요.")]
    public FillDirection fillDirection = FillDirection.LeftToRight;

    private void Start()
    {
        if (targetImageRect != null)
        {
            // Anchor 방식으로 비율을 조절할 것이므로, 
            // 여백(Left, Right, Top, Bottom)을 모두 0으로 초기화하여 화면에 딱 맞게 세팅합니다.
            targetImageRect.offsetMin = Vector2.zero; // Left, Bottom = 0
            targetImageRect.offsetMax = Vector2.zero; // Right, Top = 0
        }
    }

    // 부모 클래스가 계산해준 0 ~ 1 사이의 progress (요동치는 값)
    protected override void ApplyVisuals(float progress)
    {
        if (targetImageRect == null) return;

        // 선택한 방향에 따라 Anchor Min(좌하단)과 Anchor Max(우상단)의 비율을 조절합니다.
        switch (fillDirection)
        {
            case FillDirection.LeftToRight:
                // 왼쪽(0)은 고정, 오른쪽(max.x)이 progress만큼 늘어남
                targetImageRect.anchorMin = new Vector2(0f, 0f);
                targetImageRect.anchorMax = new Vector2(progress, 1f);
                break;

            case FillDirection.RightToLeft:
                // 오른쪽(1)은 고정, 왼쪽(min.x)이 1-progress에서 시작해서 0으로 다가옴
                targetImageRect.anchorMin = new Vector2(1f - progress, 0f);
                targetImageRect.anchorMax = new Vector2(1f, 1f);
                break;

            case FillDirection.BottomToTop:
                // 아래(0)는 고정, 위쪽(max.y)이 progress만큼 늘어남
                targetImageRect.anchorMin = new Vector2(0f, 0f);
                targetImageRect.anchorMax = new Vector2(1f, progress);
                break;

            case FillDirection.TopToBottom:
                // 위(1)는 고정, 아래쪽(min.y)이 1-progress에서 시작해서 0으로 다가옴
                targetImageRect.anchorMin = new Vector2(0f, 1f - progress);
                targetImageRect.anchorMax = new Vector2(1f, 1f);
                break;
        }
    }

    // 트랜지션 효과(WaveTransitionEffect)가 끝나고 다음 웨이브를 위해 초기화할 때 호출됨
    public override void ResetEffect()
    {
        base.ResetEffect();
        ApplyVisuals(0f); // 크기를 완전히 0으로 되돌림
    }
}