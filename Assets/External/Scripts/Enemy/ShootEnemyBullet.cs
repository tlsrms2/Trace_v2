using Unity.VisualScripting;
using UnityEngine;
using System.Collections;

public class ShootEnemyBullet : MonoBehaviour
{
    [Header("Bullet Stats")]
    public float speed = 5f;
    public int damage = 1;
    public float lifetime = 5f;

    private Vector2 savedVelocity;
    private Transform shooterTransform;
    private Vector2 direction;
    private Rigidbody2D rb;
    private PolygonCollider2D col;
    private bool isPaused = false;

    public void Initialize(Vector2 dir, float bulletSpeed, int bulletDamage, Transform shooter)
    {
        direction = dir.normalized;
        speed = bulletSpeed;
        damage = bulletDamage;
        shooterTransform = shooter;

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * speed; 
        }

        StartCoroutine(DestroyAfterTime());
    }

    private void Update()
    {
        if (rb == null) return;
        
        bool currentlyPaused = (GameManager.Instance.CurrentPhase == GamePhase.Paused);

        if (currentlyPaused && !isPaused)
        {
            savedVelocity = rb.linearVelocity; 
            rb.linearVelocity = Vector2.zero; 
            rb.angularVelocity = 0f; 
            
            isPaused = true;
        }
        else if (!currentlyPaused && isPaused)
        {
            rb.linearVelocity = savedVelocity; 
            isPaused = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        Vector2 knockbackDirection = Vector2.zero;
        AttackData attack;
        if (collision.TryGetComponent(out attack) && collision.gameObject.CompareTag("Player"))
        {
            if (shooterTransform != null)
            {
                knockbackDirection = (shooterTransform.position - transform.position).normalized;
            }
            else
            {
                knockbackDirection = -rb.linearVelocity.normalized;
            }

            rb.linearVelocity = knockbackDirection * speed * 1.5f;
            col = GetComponent<PolygonCollider2D>();
            col.isTrigger = true;

            float angle = Mathf.Atan2(knockbackDirection.y, knockbackDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            AttackData attackData = gameObject.GetComponent<AttackData>();
            attackData.Damage = 5;
        }   
    }

    private IEnumerator DestroyAfterTime()
    {
        float timer = 0f;
        while (timer < lifetime)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.Paused)
            {
                timer += Time.deltaTime;
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}
