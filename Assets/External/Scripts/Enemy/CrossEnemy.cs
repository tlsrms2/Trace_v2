using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CrossEnemy : Enemy 
{
    [Header("Cross Enemy Settings")]
    [SerializeField] private float alertTime = 2.0f;
    [SerializeField] private Color pathColor = new Color(1, 0, 0, 0.5f);

    private LineRenderer lineRenderer;
    private float timer;
    private float initialWidth;
    private bool isAlerting = true;
    private bool isActivating = true;
    
    private Vector3 direction; 
    private Camera mainCam;

    protected override void Awake()
    {
        base.Awake(); 
        lineRenderer = GetComponent<LineRenderer>();
        mainCam = Camera.main;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }

    private void OnEnable()
    {
        isAlerting = true;
        lineRenderer.enabled = true;

        ReadySpecialAttack();
    }

    private void ReadySpecialAttack()
    {
        // 1. 경고 페이즈 동안 모습과 충돌체 숨기기
        spriteRenderer.enabled = false;
        col.enabled = false;
        timer = alertTime;
        isActivating = true;
        lineRenderer.enabled = true;
        base.Start(); // target 초기화
        
        // LineRenderer의 초기 너비만 한 번 저장해 둡니다.
        initialWidth = transform.localScale.x;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // 첫 번째 공격 개시
        ResetAttack(); 
    }

    // ★ 핵심: 공격 상태를 초기화하고 새로운 위치를 잡는 함수
    private void ResetAttack()
    {
        isAlerting = true;
        timer = alertTime;
        
        AudioManager.Instance.PlayLaserWarning();
        // 1. 다시 숨기기
        spriteRenderer.enabled = false;
        col.enabled = false;
        lineRenderer.enabled = true; // 꺼졌던 선 다시 켜기

        // 2. 동적 화면 크기 계산 및 내 위치 재조정 (95% 룰 적용)
        if (mainCam != null)
        {
            float camHalfHeight = mainCam.orthographicSize;
            float camHalfWidth = camHalfHeight * mainCam.aspect;
            Vector3 camPos = mainCam.transform.position;

            float safeY = camHalfHeight * 0.95f;
            float safeX = camHalfWidth * 0.95f;

            // 이미 스폰되어 있는 상태이므로, 카메라 중심으로 임의의 방향을 결정하도록 로직을 살짝 수정
            int randomSide = Random.Range(0, 4); 

            float outX = camHalfWidth + 2f;
            float outY = camHalfHeight + 2f;

            Vector3 startPos = Vector3.zero;

            switch (randomSide)
            {
                case 0: // 좌측 벽에서 우측으로
                    startPos = new Vector3(camPos.x - outX, Random.Range(camPos.y - safeY, camPos.y + safeY), 0);
                    direction = Vector3.right;
                    break;
                case 1: // 우측 벽에서 좌측으로
                    startPos = new Vector3(camPos.x + outX, Random.Range(camPos.y - safeY, camPos.y + safeY), 0);
                    direction = Vector3.left;
                    break;
                case 2: // 하단 벽에서 상단으로
                    startPos = new Vector3(Random.Range(camPos.x - safeX, camPos.x + safeX), camPos.y - outY, 0);
                    direction = Vector3.up;
                    break;
                case 3: // 상단 벽에서 하단으로
                    startPos = new Vector3(Random.Range(camPos.x - safeX, camPos.x + safeX), camPos.y + outY, 0);
                    direction = Vector3.down;
                    break;
            }

            // 계산된 새로운 위치로 순간이동
            transform.position = startPos;

            // 도착 지점 계산
            Vector3 endPos = startPos + direction * (Mathf.Abs(direction.x) > 0 ? outX * 2 : outY * 2);

            // 3. LineRenderer (경로 표시) 재설정
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);
            lineRenderer.startWidth = initialWidth;
            lineRenderer.endWidth = initialWidth;
        }
    }

    protected override void Update()
    {
        if (isAlerting &&  isActivating && GameManager.Instance.CurrentPhase != GamePhase.Paused)
        {
            // --- 경고 페이즈 ---
            timer -= Time.deltaTime;
            float ratio = Mathf.Clamp01(timer / alertTime);

            lineRenderer.startWidth = initialWidth * ratio;
            lineRenderer.endWidth = initialWidth * ratio;

            float blinkSpeed = Mathf.Lerp(30f, 5f, ratio);
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
            Color c = pathColor;
            c.a = alpha;
            lineRenderer.startColor = c;
            lineRenderer.endColor = c;

            if (timer <= 0f)
            {
                isAlerting = false;
                AudioManager.Instance.PlayLaserMobDash();

                lineRenderer.enabled = false; 
                spriteRenderer.enabled = true; 
                col.enabled = true; 
            }
        }
        else if (isAlerting == false)
        {
            // --- 돌진 페이즈 ---
            base.Update(); 
            CheckOutOfBounds(); 
        }
    }

    protected override void Move()
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
        {
            transform.position += direction * speed * Time.deltaTime;
        }
    }

    private void CheckOutOfBounds()
    {
        if (mainCam == null) return;

        float camHalfHeight = mainCam.orthographicSize;
        float camHalfWidth = camHalfHeight * mainCam.aspect;
        Vector3 camPos = mainCam.transform.position;

        float distX = Mathf.Abs(transform.position.x - camPos.x);
        float distY = Mathf.Abs(transform.position.y - camPos.y);

        // 화면 밖으로 완전히 벗어났을 때
        if (distX > camHalfWidth + 3.0f || distY > camHalfHeight + 3.0f)
        {
            // 파괴(Destroy)하지 않고, 다시 공격을 준비하도록 만듭니다!!
            ResetAttack();
        }
    }

    private IEnumerator RespawnAfterDelay(float delay)
    {
        isAlerting = true;
        spriteRenderer.enabled = false;
        col.enabled = false;
        isActivating = false;

        yield return new WaitForSeconds(delay);

        ReadySpecialAttack();
    }
}