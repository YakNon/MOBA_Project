using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public class HeroSummaryData
    {
        public string heroName;
        public string playerName;
        public int kill;
        public int death;
        public int assist;
        public float damageDealt;
        public float maxHealth;
        public float attackDamage;
        public float defense;
        public float magic;
        public float magicDef;
        public int gold;
        public string heroIcon;
        public List<string> itemNames = new List<string>();
    }
    public List<HeroSummaryData> heroSummaries = new List<HeroSummaryData>();

    public Tower redBase;
    public Tower blueBase;
    public Canvas endGameCanvas;
    public Image victoryImage;
    public Image defeatImage;
    public TextMeshProUGUI endGameText;
    public TextMeshProUGUI gameTimeText;
    public TextMeshProUGUI MyTeamScore;
    public TextMeshProUGUI EnemyTeamScore;
    private int blueTeamKills = 0;
    private int redTeamKills = 0;

    private bool gameEnded = false;
    private double startTime;
    public Team winningTeam = Team.None;

    public static GameManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject); // ป้องกันไม่ให้มี instance ซ้ำซ้อน
        }
    }
    void Start()
    {
        PhotonNetwork.SendRate = 30;
        PhotonNetwork.SerializationRate = 30;
        if (endGameCanvas != null)
        {
            endGameCanvas.gameObject.SetActive(false);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            startTime = PhotonNetwork.Time;
            photonView.RPC("SyncStartTime", RpcTarget.Others, startTime);
        }

    }

    void Update()
    {
        if (!gameEnded)
        {
            UpdateGameTimeDisplay();
            CheckBaseStatus();
        }
    }

    void UpdateGameTimeDisplay()
    {
        double elapsedTime = PhotonNetwork.Time - startTime;
        int minutes = Mathf.FloorToInt((float)elapsedTime / 60);
        int seconds = Mathf.FloorToInt((float)elapsedTime % 60);
        gameTimeText.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);
    }

    void CheckBaseStatus()
    {
        if (gameEnded) return; // ป้องกันการเรียกซ้ำ

        if (redBase == null || (redBase?.currentHealth ?? 0) <= 0)  
        {
            Debug.Log("Blue Team Wins! Requesting End Game...");
            photonView.RPC("RPC_RequestEndGame", RpcTarget.MasterClient, "Blue Team Wins!", "Red Team Loses!", Team.Blue);
            return; // หยุดทำงานหลังจากส่ง RPC
        }
        
        if (blueBase == null || (blueBase?.currentHealth ?? 0) <= 0)
        {
            Debug.Log("Red Team Wins! Requesting End Game...");
            photonView.RPC("RPC_RequestEndGame", RpcTarget.MasterClient, "Red Team Wins!", "Blue Team Loses!", Team.Red);
        }
    }


    [PunRPC]
    void RPC_RequestEndGame(string winningMessage, string losingMessage, Team team)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"MasterClient received end game request. Processing...");
        photonView.RPC("RPC_EndGame", RpcTarget.All, winningMessage, losingMessage, team);// เรียกฟังก์ชันจบเกมเพื่อให้ทุกคนแสดงรูปจบเกม
    }

    [PunRPC]
    void RPC_EndGame(string winningMessage, string losingMessage, Team team)
    {
        if (gameEnded) return;
        gameEnded = true;
        winningTeam = team;

        Team playerTeam = TeamManager.instance.GetTeam(PhotonNetwork.LocalPlayer);
        endGameCanvas.gameObject.SetActive(true);
        if(playerTeam == winningTeam){
            victoryImage.gameObject.SetActive(true);
            defeatImage.gameObject.SetActive(false);
        }else{
            victoryImage.gameObject.SetActive(false);
            defeatImage.gameObject.SetActive(true);
        }
        heroSummaries.Clear(); 
        foreach (Hero hero in FindObjectsOfType<Hero>())
        {
            HeroSummaryData data = new HeroSummaryData
            {
                heroName = hero.heroName,
                playerName = hero.ownerPlayer.NickName,
                kill = hero.killCount,
                death = hero.deathCount,
                assist = hero.assistCount,
                damageDealt = hero.GetTotalDamageDealt(),
                maxHealth = hero.maxHealth,
                attackDamage = hero.attackDamage,
                defense = hero.defense,
                magic = hero.magic,
                magicDef = hero.magicDef,
                gold = hero.currentGold,
                heroIcon = hero.heroImage.name
            };

            foreach (var item in hero.items)
            {
                if (item != null)
                    data.itemNames.Add(item.itemName); // เก็บชื่อ item
            }

            heroSummaries.Add(data);
        }
        StartCoroutine(EndGameRoutine());
    }
    IEnumerator EndGameRoutine()
    {
        yield return new WaitForSeconds(5f);

        if (PhotonNetwork.IsMasterClient)
        {
            //photonView.RPC("RPC_SetWinningTeam", RpcTarget.All, (int)winningTeam);
            Debug.Log(" MasterClient is loading Summary Scene...");
            photonView.RPC("RPC_LoadSummaryScene", RpcTarget.All);
        }
    }

    public Hero CalculateMVP()
    {
        Hero mvp = null;
        float highestScore = 0;

        foreach (Hero hero in FindObjectsOfType<Hero>())
        {
            float heroScore = (hero.killCount * 3) + (hero.assistCount * 1) + hero.GetTotalDamageDealt();

            if (heroScore > highestScore)
            {
                highestScore = heroScore;
                mvp = hero;
            }
        }

        return mvp;
    }

    [PunRPC]
    void RPC_LoadSummaryScene()
    {
        Debug.Log(" [GameManager] Loading Summary Scene...");
        
        if (SceneManager.GetActiveScene().name != "summary") // ป้องกันการโหลดซ้ำ
        {
            SceneManager.LoadScene("summary");
        }
        else
        {
            Debug.LogWarning(" [GameManager] Already in Summary Scene, no need to load again.");
        }
    }


    [PunRPC]
    void SyncStartTime(double masterStartTime)
    {
        startTime = masterStartTime;
    }

    //อัปเดทคิล
    [PunRPC]
    public void RPC_UpdateTeamScore(Team team, int newScore)
    {
        if (team == Team.Blue)
        {
            blueTeamKills = newScore;
        }
        else if (team == Team.Red)
        {
            redTeamKills = newScore;
        }

        UpdateTeamScoreUI();
    }
    private void UpdateTeamScoreUI()
    {
        Team myTeam = TeamManager.instance.GetTeam(PhotonNetwork.LocalPlayer);

        if (myTeam == Team.Blue)
        {
            MyTeamScore.text = blueTeamKills.ToString();
            EnemyTeamScore.text = redTeamKills.ToString();
        }
        else if (myTeam == Team.Red)
        {
            MyTeamScore.text = redTeamKills.ToString();
            EnemyTeamScore.text = blueTeamKills.ToString();
        }
    }


    public void AddKillToTeam(Team team)
    {
        if (PhotonNetwork.IsMasterClient) // ให้ MasterClient เป็นตัวอัปเดตค่าคะแนน
        {
            if (team == Team.Blue)
            {
                blueTeamKills++;
                photonView.RPC("RPC_UpdateTeamScore", RpcTarget.All, Team.Blue, blueTeamKills);
            }
            else if (team == Team.Red)
            {
                redTeamKills++;
                photonView.RPC("RPC_UpdateTeamScore", RpcTarget.All, Team.Red, redTeamKills);
            }
        }
    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // ซิงค์คะแนนเมื่อมีผู้เล่นใหม่เข้าห้อง
        photonView.RPC("RPC_UpdateTeamScore", newPlayer, Team.Blue, blueTeamKills);
        photonView.RPC("RPC_UpdateTeamScore", newPlayer, Team.Red, redTeamKills);
    }
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log($"Player {otherPlayer.NickName} left the game! Checking minion ownership...");

        if (PhotonNetwork.IsMasterClient) //  MasterClient เป็นคนจัดการการเปลี่ยน Ownership
        {
            foreach (Minion minion in FindObjectsOfType<Minion>())
            {
                if (minion.photonView.Owner == otherPlayer)
                {
                    Debug.Log($"Transferring Minion {minion.name} to MasterClient");
                    minion.photonView.TransferOwnership(PhotonNetwork.MasterClient);
                }
            }
        }
    }


}
