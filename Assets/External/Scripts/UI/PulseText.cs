using UnityEngine;

public class PulseText : MonoBehaviour
{
    [SerializeField] private float speed = 4f;
    [SerializeField] private float scaleAmount = 0.1f;
    [SerializeField] private bool useUnscaledTime = true;

    private RectTransform rectTransform;
    private Vector3 baseScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        baseScale = rectTransform.localScale;
    }

    private void Update()
    {
        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        float pulse = (Mathf.Sin(time * speed) + 1f) * 0.5f; // 0~1
        float scale = 1f + pulse * scaleAmount;

        rectTransform.localScale = baseScale * scale;
    }

    private void OnDisable()
    {
        if (rectTransform != null)
            rectTransform.localScale = baseScale;
    }
}