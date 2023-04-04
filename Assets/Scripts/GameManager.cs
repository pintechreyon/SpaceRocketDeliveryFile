using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Text timeText;
    public Text recordText;
    public PlayerInput playerInput;
    public float timeIntervalForSkillTree;
    public GameObject skillTreePrefab;

    private float surviveTime;
    public bool isGameover;
    private bool isSkillTreeOpen;
    private float lastSkillTreeOpenTime;
    private GameObject skillTreeInstance;
    private Text gameUITimeText; // 추가된 코드
    public GameObject gameoverCanvasPrefab;


    public bool IsGameOver()
    {
        return isGameover;
    }

    private void Start()
    {
        surviveTime = 0;
        isGameover = false;
        isSkillTreeOpen = false;
        lastSkillTreeOpenTime = 0;

        timeText.text = "Time: 0";
        OpenSkillTree();

        // GamePanel과 GameUI를 참조하는 코드
        GameObject gamePanel = GameObject.Find("GamePanel");
        GameObject gameUITextObject = gamePanel.transform.Find("GameUI").gameObject;
        gameUITimeText = gameUITextObject.GetComponent<Text>();
    }

    private void Update()
    {
        if (!isGameover)
        {
            surviveTime += Time.deltaTime;
            timeText.text = "Time: " + (int)surviveTime;
            gameUITimeText.text = "Time: " + (int)surviveTime; // 생존 시간 업데이트

            if (!isSkillTreeOpen && (surviveTime - lastSkillTreeOpenTime) >= timeIntervalForSkillTree)
            {
                OpenSkillTree();
            }
        }
    }

    public void EndGame()
    {
        Debug.Log('9');
        isGameover = true;
        float bestTime = PlayerPrefs.GetFloat("BestTime");

        if (surviveTime > bestTime)
        {
            bestTime = surviveTime;

            PlayerPrefs.SetFloat("BestTime", bestTime);
        }
        Time.timeScale = 0;
        recordText.text = "Your Score: " + (int)surviveTime;
        OpenGameOverCanvas();
    }

    public void OpenSkillTree()
    {
        isSkillTreeOpen = true;
        lastSkillTreeOpenTime = surviveTime;
        Time.timeScale = 0;

        if (skillTreeInstance == null)
        {
            skillTreeInstance = Instantiate(skillTreePrefab);
        }

        skillTreeInstance.SetActive(true);
    }

    public void CloseSkillTree()
    {
        isSkillTreeOpen = false;
        Time.timeScale = 1;

        if (skillTreeInstance != null)
        {
            skillTreeInstance.SetActive(false);
        }
    }
    public void OpenGameOverCanvas()
    {
        if (gameoverCanvasPrefab != null)
        {
            GameObject gameoverCanvas = Instantiate(gameoverCanvasPrefab);
            gameoverCanvas.SetActive(true);
        }
        else
        {
            Debug.LogError("Gameover Canvas Prefab is not assigned in the inspector.");
        }
    }


}
