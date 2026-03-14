using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float activeTime = 0.2f;
    [SerializeField] private int damage = 10;
    
    [Header("Swing Settings")]
    [Tooltip("공격 시 휘두르는 총 각도 (도 단위)")]
    [SerializeField] private float swingAngle = 90f;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Attack(Vector3 direction)
    {
        gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(SwingRoutine(direction));
    }

    private IEnumerator SwingRoutine(Vector3 direction)
    {
        float timer = 0f;
        
        // 방향의 크기(거리)를 저장
        float distance = direction.magnitude;
        if (distance < 0.0001f) distance = 1f;

        Vector3 normDir = direction.normalized;
        float baseAngle = Mathf.Atan2(normDir.y, normDir.x) * Mathf.Rad2Deg;

        // 기준 각도의 좌/우로 swingAngle 절반씩 휘두름 (- -> + 방향)
        float startAngle = baseAngle - swingAngle / 2f;
        float endAngle = baseAngle + swingAngle / 2f;

        while (timer < activeTime)
        {
            timer += Time.deltaTime;
            
            float t = activeTime > 0f ? timer / activeTime : 1f;
            float currentAngle = Mathf.Lerp(startAngle, endAngle, t);
            
            // 현재 각도를 이용한 방향 벡터
            Vector3 currentDir = new Vector3(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad), 0f);

            // 로컬 회전 및 위치 설정 (2D 스프라이트는 Y축(Up)이 기준이 되므로 90도를 빼줌)
            transform.localRotation = Quaternion.Euler(0, 0, currentAngle - 90f);
            transform.localPosition = currentDir * distance;

            yield return null;
        }

        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        BaseBoss boss = collision.GetComponent<BaseBoss>();
        if (boss != null)
        {
            boss.TakeDamage(damage);
        }
    }
}
