using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("이동 및 드로우")]
    [Tooltip("평상 시 이동 속도")][SerializeField] private float normalMoveSpeed = 5f;
    [Tooltip("시간 정지 시 이동 속도")][SerializeField] private float traceMoveSpeed = 3f;
    [Tooltip("포인트 간 최소 거리")][SerializeField] private float minDistance = 0.1f;

    private bool IsTracing => GameManager.Instance.CurrentPhase == GamePhase.Paused;
    private bool IsReplaying => GameManager.Instance.CurrentPhase == GamePhase.Replay;

    private Rigidbody2D playerRigidbody;
    private float moveSpeed;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody2D>();
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
}
