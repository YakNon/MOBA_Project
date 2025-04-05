using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using TMPro;

public class SummaryManager : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI summaryText;
    public GameObject leaveButton;
    public HeroStatusUIManager heroStatusUIManager;

    void Start()
    {

        ShowWinOrLoseText();

        //if (Photonnetwork.IsMine)
        //{
            PopulateSummaryPanels();
        //}
        
        // if (PhotonNetwork.IsMasterClient)
        // {
        //     photonView.RPC("RPC_PopulateSummaryPanels", RpcTarget.AllBuffered);
        // }
    }
    // [PunRPC]
    // void RPC_PopulateSummaryPanels()
    // {
    //     if (heroStatusUIManager == null || GameManager.instance == null || GameManager.instance.heroSummaries == null)
    //     {
    //         Debug.LogWarning("ไม่สามารถสร้าง summary panels ได้");
    //         return;
    //     }

    //     foreach (var data in GameManager.instance.heroSummaries)
    //     {
    //         bool isMyTeam = TeamManager.instance.GetTeam(PhotonNetwork.LocalPlayer) ==
    //                         TeamManager.instance.GetTeamByPlayerName(data.playerName);

    //         heroStatusUIManager.CreateSummaryPanel(data, isMyTeam);
    //     }
    // }

    void PopulateSummaryPanels()
    {
        heroStatusUIManager.ClearSummaryPanels();
        if (GameManager.instance == null || GameManager.instance.heroSummaries == null)
        {
            Debug.LogWarning(" ไม่มีข้อมูล HeroSummaryData ให้แสดง");
            return;
        }

        foreach (var data in GameManager.instance.heroSummaries)
        {
            // ตรวจสอบว่าเป็นทีมเดียวกับเราไหม
            bool isMyTeam = TeamManager.instance.GetTeam(PhotonNetwork.LocalPlayer) ==
                            TeamManager.instance.GetTeamByPlayerName(data.playerName); // เพิ่มฟังก์ชันนี้ด้านล่าง

            heroStatusUIManager.CreateSummaryPanel(data, isMyTeam);
        }
    }


    void ShowWinOrLoseText()
    {
        if (GameManager.instance == null)
        {
            Debug.LogWarning(" GameManager.instance is NULL! Trying to find it...");
            GameObject gameManagerObj = GameObject.Find("GameManager");

            if (gameManagerObj != null)
            {
                GameManager.instance = gameManagerObj.GetComponent<GameManager>();
                //Debug.Log(" GameManager found and assigned.");
            }
        }

        if (GameManager.instance == null) 
        {
            summaryText.text = " GameManager.instance == NULL!";
            summaryText.color = Color.red;
            return;
        }

        //  ดึงทีมที่ชนะจาก GameManager
        Team winningTeam = GameManager.instance.winningTeam;
        //  ดึงข้อมูลทีมของผู้เล่น
        Team playerTeam = TeamManager.instance.GetTeam(PhotonNetwork.LocalPlayer);

        //  ตรวจสอบว่าผู้เล่นชนะหรือแพ้
        bool isVictory = (playerTeam == winningTeam);

        if (isVictory)
        {
            summaryText.text = "VICTORY";
            summaryText.color = Color.yellow; 
        }
        else
        {
            summaryText.text = "DEFEAT";
            summaryText.color = Color.blue; 
        }
    }



    void CleanupBeforeLeaving()
    {
        GameObject heroSelectionManager = GameObject.Find("HeroSelectionManager");

        if (heroSelectionManager != null)
        {
            //Debug.Log("Destroying HeroSelectionManager before leaving the room.");
            Destroy(heroSelectionManager);
        }
        GameObject teamManager = GameObject.Find("TeamManager");

        if (teamManager != null)
        {
            //Debug.Log(" Destroying teamManager before leaving the room.");
            Destroy(teamManager);
        }
        if (heroStatusUIManager != null)
        {
            //Debug.Log("Destroying heroStatusUIManager before leaving the room.");
            Destroy(heroStatusUIManager);
        }
        //GameObject gameManager = GameObject.Find("GameManager");
        if (GameManager.instance != null)
        {
            //Debug.Log("Destroying GameManager before leaving the room.");
            Destroy(GameManager.instance.gameObject);
        }
    }

    public void LeaveRoom()
    {
        CleanupBeforeLeaving(); // ล้างค่า DontDestroyOnLoad ก่อนออกห้อง
        leaveButton.SetActive(false); // ปิดปุ่มป้องกันการกดซ้ำ
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        //Debug.Log(" Left room successfully. Loading Main Scene...");
        SceneManager.LoadScene("Main"); //  โหลดฉาก Main หลังจากออกห้องเสร็จ
    }
}
