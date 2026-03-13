using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerHit : MonoBehaviour
{
    [SerializeField] private GameObject destroyParticle;
    [SerializeField][Tooltip("무적 판정")] private bool isInvincibility;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isInvincibility && GameManager.Instance.CurrentPhase != GamePhase.Replay && collision.gameObject.CompareTag("Enemy"))
        {
            Instantiate(destroyParticle, transform.position, Quaternion.identity);
            GameManager.Instance.GameOver();
            gameObject.SetActive(false);
            AudioManager.Instance.PlayPlayerDeath();
        }
    }
}
