using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StageSelectionManager : MonoBehaviour
{
    [Header("스테이지 버튼들 (순서대로 1, 2, 3)")]
    [SerializeField] private Button[] stageButtons;
    
    [Header("게임 씬 이름")]
    [SerializeField] private string gameSceneName = "GameScene"; // 모든 보스가 로드될 단일 게임 씬

    private void Start()
    {
        Debug.Log("StageSelectionManager Start() called.");

        // 저장된 언락 데이터 로드 (데이터가 없으면 1번 스테이지가 기본)
        int unlockedStage = PlayerPrefs.GetInt("UnlockedStage", 1);

        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] == null)
            {
                continue;
            }

            int stageNum = i + 1;
            
            // 스테이지 번호가 언락된 번호보다 작거나 같으면 클릭 가능
            if (stageNum <= unlockedStage)
            {
                stageButtons[i].interactable = true;
                
                // 버튼 클릭 이벤트 등록
                int index = i; // 클로저 이슈 방지용 변수
                stageButtons[i].onClick.AddListener(() => LoadStage(index));
            }
            else
            {
                // 아직 열리지 않은 스테이지는 클릭 불가 처리
                stageButtons[i].interactable = false;
            }
        }
    }

    private void LoadStage(int index)
    {
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            Time.timeScale = 1f; // 씬 전환 전 타임스케일을 반드시 1로 리셋

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayButtonClick();
                
            GameManager.SelectedStageIndex = index; // GameManager에 선택된 스테이지 인덱스 저장
            SceneManager.LoadScene(gameSceneName); // 단일 게임 씬 로드
        }
    }
}