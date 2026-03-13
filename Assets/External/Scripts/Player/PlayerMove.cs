using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("불변 객체")]
    [SerializeField] private LineRenderer dotLineRenderer;
    [SerializeField] private Material dotLineMaterial;

    [Header("이동 및 드로우")]
    [Tooltip("평상 시 이동 속도")][SerializeField] private float normalMoveSpeed = 5f;
    [Tooltip("시간 정지 시 이동 속도")][SerializeField] private float traceMoveSpeed = 3f;
    [Tooltip("포인트 간 최소 거리")][SerializeField] private float minDistance = 0.1f;
    [Tooltip("선 굵기")][SerializeField] private float lineWidth = 0.2f;
    [Tooltip("도형 완성 거리 보정값")][SerializeField] private float closeThreshold = 1f;

    [Header("데미지")]
    [Tooltip("선 데미지")][SerializeField] private int lineDamage = 5;
    [Tooltip("도형 데미지")][SerializeField] private int shapeDamage = 10;

    [Header("도형 색상")]
    [SerializeField] private Color shapeColor = new Color(1f, 0.5f, 0.5f, 0.4f);
    private LineRenderer lineRenderer;
    private List<Vector3> tracePoints = new List<Vector3>();
    private List<GameObject> shapes = new List<GameObject>();
    

    private bool IsTracing => GameManager.Instance.CurrentPhase == GamePhase.Paused;
    private bool IsReplaying => GameManager.Instance.CurrentPhase == GamePhase.Replay;

    private Rigidbody2D playerRigidbody;
    private float moveSpeed;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody2D>();

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // 점선 설정
        dotLineRenderer.textureMode = LineTextureMode.Tile;
        dotLineRenderer.material = dotLineMaterial;
        dotLineRenderer.startWidth = lineWidth;
        dotLineRenderer.endWidth = lineWidth;
        dotLineRenderer.useWorldSpace = true;
    }

    private void Start()
    {
        GameManager.Instance.OnTraceStarted += StartTrace;
        GameManager.Instance.OnTraceEnded += EndTrace;
    }
    private void OnDestroy()
    {
        GameManager.Instance.OnTraceStarted -= StartTrace;
        GameManager.Instance.OnTraceEnded -= EndTrace;
    }

    private void OnDisable()
    {
        foreach(var shape in shapes)
            Destroy(shape);
    }

    private void Update()
    {
        if (IsTracing)
        {
            RecordPosition();
            moveSpeed = traceMoveSpeed;
        }
        else
        {
            moveSpeed = normalMoveSpeed;
        }
    }

    private void FixedUpdate()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector2 inputDir = new Vector2(x, y).normalized;

        if (!IsReplaying)
        {
            playerRigidbody.linearVelocity = inputDir * moveSpeed;
        }
        else
            playerRigidbody.linearVelocity = Vector2.zero;
    }

    private void StartTrace()
    {
        tracePoints.Clear();
        lineRenderer.positionCount = 0;
        tracePoints.Add(transform.position);
    }

    private void EndTrace()
    {
        EvaluateShape();
    }

    /// <summary>
    /// dotLineRenderer를 이용한 점선 그리기
    /// </summary>
    private void RecordPosition()
    {
        Vector3 currentPos = transform.position;
        Vector3 lastPos = tracePoints[tracePoints.Count - 1];
        if (Vector3.Distance(currentPos, lastPos) < minDistance) return;

        tracePoints.Add(currentPos);
        dotLineRenderer.positionCount = tracePoints.Count;
        dotLineRenderer.SetPositions(tracePoints.ToArray());
    }

    // Space를 뗐을 때 도형 여부 판단
    private void EvaluateShape()
    {
        bool isClosed = tracePoints.Count >= 5 &&
                        Vector3.Distance(tracePoints[tracePoints.Count - 1], tracePoints[0]) < closeThreshold;

        if (isClosed)
        {
            tracePoints.Add(tracePoints[0]);
            CreateShape(tracePoints);
        }

        if (isActiveAndEnabled)
            StartCoroutine(EraseFromStart());
    }

    /// <summary>
    /// tracePoints를 따라 lineRenderer를 재생성
    /// </summary>
    /// <param name="interval">프레임당 삭제 간격</param>
    /// <returns></returns>
    private IEnumerator EraseFromStart(float interval = 0.01f)
    {
        // 가만히 있었을 경우 바로 탈출
        if (tracePoints.Count < 2)
        {
            tracePoints.Clear();
            dotLineRenderer.positionCount = 0;
            // 바로 ChangePhase를 사용하면 GameManager의 ChangePhase가 나중에 실행되어 Replay 상태가 됨
            yield return null;
            GameManager.Instance.ChangePhase(GamePhase.RealTime);
            yield break;
        }

        GameObject colliderObj = new GameObject("AttackCollider");
        colliderObj.tag = "Player";
        EdgeCollider2D edgeCol = colliderObj.AddComponent<EdgeCollider2D>();
        Vector2[] points2D = tracePoints.Select(p => new Vector2(p.x, p.y)).Reverse().ToArray();
        edgeCol.points = points2D;
        edgeCol.isTrigger = true;
        AttackData data = colliderObj.AddComponent<AttackData>();
        data.Damage = lineDamage;
        var rig = colliderObj.AddComponent<Rigidbody2D>();
        rig.gravityScale = 0;
        rig.bodyType = RigidbodyType2D.Kinematic;

        // 공격 선 드로우 시 사용하는 포인트 위치
        List<Vector3> attackTracePoints = new List<Vector3>();

        while (tracePoints.Count > 0)
        {
            transform.position = tracePoints[0];
            attackTracePoints.Add(tracePoints[0]);
            tracePoints.RemoveAt(0);

            lineRenderer.positionCount = attackTracePoints.Count;
            lineRenderer.SetPositions(attackTracePoints.ToArray());
            dotLineRenderer.positionCount = tracePoints.Count;
            dotLineRenderer.SetPositions(tracePoints.ToArray());
            edgeCol.points = attackTracePoints.Select(p => (Vector2)p).ToArray();
            
            AudioManager.Instance.PlayLaserPlace();

            yield return new WaitForSeconds(interval);
        }

        tracePoints.Clear();
        dotLineRenderer.positionCount = 0;
        
        ActivateShapes();
        StartCoroutine(EraseAttackLine(attackTracePoints, colliderObj));
        
        GameManager.Instance.ChangePhase(GamePhase.RealTime);
    }

    private IEnumerator EraseAttackLine(List<Vector3> attackTracePoints, GameObject colliderObj, float interval = 0.01f)
    {
        EdgeCollider2D edgeCol = colliderObj.GetComponent<EdgeCollider2D>();
        while (attackTracePoints.Count > 0)
        {
            lineRenderer.positionCount = attackTracePoints.Count;
            lineRenderer.SetPositions(attackTracePoints.ToArray());
            edgeCol.points = attackTracePoints.Select(p => (Vector2)p).ToArray();
            attackTracePoints.RemoveAt(0);

            yield return new WaitForSeconds(interval);
        }
        Destroy(colliderObj);
    }

    private void CreateShape(List<Vector3> points)
    {
        GameObject shapeObj = new GameObject("FilledShape");

        PolygonCollider2D col = shapeObj.AddComponent<PolygonCollider2D>();
        List<Vector2> points2D = new List<Vector2>();
        foreach (var p in points)
            points2D.Add(new Vector2(p.x, p.y));
        col.SetPath(0, points2D);
        col.isTrigger = true;

        Mesh mesh = col.CreateMesh(false, false);
        MeshFilter mf = shapeObj.AddComponent<MeshFilter>();
        MeshRenderer mr = shapeObj.AddComponent<MeshRenderer>();
        mf.mesh = mesh;
        mr.material = new Material(Shader.Find("Unlit/Color"));
        mr.material.color = shapeColor;

        mr.material.SetColor("_EmissionColor", shapeColor * 2f); 
        AttackData attack = shapeObj.AddComponent<AttackData>();
        attack.Damage = shapeDamage;
        var rig = shapeObj.AddComponent<Rigidbody2D>();
        rig.gravityScale = 0;
        rig.bodyType = RigidbodyType2D.Kinematic;

        shapes.Add(shapeObj);
        shapeObj.SetActive(false);
    }

    private void ActivateShapes()
    {
        foreach (GameObject shape in shapes)
        {
            shape.SetActive(true);
            StartCoroutine(FadeAndDestroyShape(shape, 0.4f));
        }
    }

    private IEnumerator FadeAndDestroyShape(GameObject shape, float duration)
    {
        MeshRenderer mr = shape.GetComponent<MeshRenderer>();
        Color originalColor = mr.material.color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            mr.material.color = new Color(originalColor.r, originalColor.g, originalColor.b, Mathf.Lerp(originalColor. a, 0, t));
            yield return null;
        }

        Destroy(shape);
        shapes.Clear();
    }
}
