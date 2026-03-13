using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class FuelGaugeUI : MonoBehaviour // 파일명에 맞춰 클래스 이름 변경
{
    [Header("Shader / Material")]
    [SerializeField] private Image fillImage;
    [SerializeField] private GameObject fuelIcon;
    [SerializeField] private GameObject fuelValue;

    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material pressedGlowMaterial;

    [Header("UI Text")]
    [SerializeField] private TMP_Text fuelValueText;

    private Slider fuelSlider;
    private bool wasPressing;

    private void Awake()
    {
        fuelSlider = GetComponent<Slider>();

        // GameManager에서 Percentage(0.0 ~ 1.0)를 가져올 것이므로 Slider의 범위를 0~1로 설정
        fuelSlider.minValue = 0f;
        fuelSlider.maxValue = 1f;
        
        // Image 및 Material 자동 할당 처리
        if (fillImage == null && fuelSlider.fillRect != null)
        {
            fillImage = fuelSlider.fillRect.GetComponent<Image>();
        }

        if (fillImage != null && normalMaterial == null)
        {
            normalMaterial = fillImage.material;
        }

        if (fillImage != null && normalMaterial != null)
        {
            fillImage.material = normalMaterial;
        }
    }

    private void Update()
    {
        // GameManager 인스턴스가 없으면 예외 방지
        if (GameManager.Instance == null) return;

        // 1. GameManager로부터 현재 게이지 비율 (0.0f ~ 1.0f) 가져오기
        float currentGaugePercent = GameManager.Instance.GetGaugePercentage();
        fuelSlider.value = currentGaugePercent;

        // 2. 텍스트 업데이트
        UpdateFuelText(currentGaugePercent);

        // 3. 누르고 있는 상태 확인 (GameManager에서는 Space를 누르면 Paused 상태가 됨)
        bool isPressing = (GameManager.Instance.CurrentPhase == GamePhase.Paused);
        UpdateShaderState(isPressing);
    }

    private void UpdateFuelText(float percent)
    {
        if (fuelValueText == null) return;

        // 0.0 ~ 1.0 비율을 0 ~ 100 퍼센트로 변환하여 출력
        int displayValue = Mathf.RoundToInt(percent * 100f);
        fuelValueText.text = $"{displayValue}%";
    }

    private void UpdateShaderState(bool isPressing)
    {
        if (fillImage == null) return;

        if (isPressing != wasPressing)
        {
            if (isPressing)
            {
                if (pressedGlowMaterial != null)
                {
                    fillImage.material = pressedGlowMaterial;
                }
            }
            else
            {
                if (normalMaterial != null)
                {
                    fillImage.material = normalMaterial;
                }
            }

            wasPressing = isPressing;
        }
    }
}