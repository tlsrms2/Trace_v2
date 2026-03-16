using UnityEngine;

public class PausableParticle : MonoBehaviour
{
    private ParticleSystem ps;
    private bool isPaused = false;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        bool currentlyPaused = (GameManager.Instance.CurrentPhase == GamePhase.Paused);

        if (currentlyPaused && !isPaused)
        {
            if (ps != null) ps.Pause();
            isPaused = true;
        }
        else if (!currentlyPaused && isPaused)
        {
            if (ps != null) ps.Play();
            isPaused = false;
        }
    }
}