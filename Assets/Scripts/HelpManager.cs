using UnityEngine;

public class HelpManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject helpPanel;

    private void Start()
    {
        if (helpPanel != null)
            helpPanel.SetActive(false);
    }

    public void ToggleHelp()
    {
        AudioManager.Instance.PlayButtonClick();
        
        if (helpPanel != null)
            helpPanel.SetActive(!helpPanel.activeSelf); 
    }
}
