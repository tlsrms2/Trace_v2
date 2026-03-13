using UnityEngine;
using System.Collections;

public class DashEnemy : Enemy
{
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashCooldown = 3f;
    [SerializeField] private Material lineMaterial;

    private LineRenderer lineRenderer;
    private float dashTimer;
    private bool isDashReady = false;
    protected override void Awake()
    {
        base.Awake();
        lineRenderer = GetComponent<LineRenderer>();
    }

    protected override void Start()
    {
        base.Start();
        dashTimer = 0f;
    }
    protected override void Update()
    {
        if (isDashReady)
            base.Update();

        if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            dashTimer += Time.deltaTime;

        if (dashTimer >= dashCooldown)
        {
            Dash();
            dashTimer = 0;
        }
    }
    private void Dash()
    {
        Vector2 dir = (target.position - transform.position).normalized;
        StartCoroutine(DashAttackRoutine(dir));
        AudioManager.Instance.PlayEpicMobDash();
    }

    IEnumerator DashAttackRoutine(Vector2 dir)
    {
        spriteRenderer.color = Color.red;
        float elapsedTime = 0f;
        float waitCooldown = 0.5f;
        float dashDuration = 0.25f;
        float totalDashDistance = dashSpeed * dashDuration;

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = lineMaterial;

        float pointSpacing = 0.2f;

        isDashReady = false;
        while (elapsedTime < waitCooldown)
        {
            elapsedTime += Time.deltaTime;
            yield return new WaitWhile(() => GameManager.Instance.CurrentPhase == GamePhase.Paused);

            float progress = elapsedTime / waitCooldown;
            float currentDistance = totalDashDistance * progress;

            int pointCount = Mathf.Max(2, Mathf.FloorToInt(currentDistance / pointSpacing) + 1);
            lineRenderer.positionCount = pointCount;

            Vector3 startPos = transform.position;
            for (int i = 0; i < pointCount; i++)
            {
                float dist = Mathf.Min(i * pointSpacing, currentDistance);
                lineRenderer.SetPosition(i, startPos + (Vector3)dir * dist);
            }
        }
        isDashReady = true;

        // 대기 끝나면 라인 숨기기
        lineRenderer.positionCount = 0;

        spriteRenderer.color = Color.blue;
        elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            transform.position += (Vector3)dir * dashSpeed * Time.deltaTime;
            elapsedTime += Time.deltaTime;
            yield return new WaitWhile(() => GameManager.Instance.CurrentPhase == GamePhase.Paused);
        }
    }
}