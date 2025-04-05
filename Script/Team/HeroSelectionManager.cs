using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class HeroSelectionManager : MonoBehaviourPun
{
    public GameObject HeroSelectionPrefab; // Prefab ที่จะใช้สร้างปุ่มเลือกฮีโร่
    //public Transform heroListContent; // คอนเทนต์ของ Scroll View ที่จะแสดงปุ่มเลือกฮีโร่
    public Transform heroListRedContent;
    public Transform heroListBlueContent;
    public List<Hero> allHeroPrefabs; // รายชื่อ Prefab ฮีโร่ทั้งหมด ที่สามารถกำหนดจาก Inspector
    //private Dictionary<string, GameObject> heroSelectionPrefabs = new Dictionary<string, GameObject>(); // ลิงค์ฮีโร่กับ prefab
    private Dictionary<string, GameObject> heroSelectionPrefabsRed = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> heroSelectionPrefabsBlue = new Dictionary<string, GameObject>();

    private string previouslySelectedHero = null; // บันทึกฮีโร่ที่เลือกก่อนหน้า
    public static HeroSelectionManager instance;

    private void Start()
    {
        CreateHeroSelectionButtons();
    }
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject); // ป้องกัน instance ซ้ำซ้อน
        }
        DontDestroyOnLoad(gameObject); // ป้องกันไม่ให้ถูกทำลายเมื่อเปลี่ยน Scene

    }

    // สร้างปุ่มเลือกฮีโร่
    public void CreateHeroSelectionButtons()
    {
        if (HeroSelectionPrefab == null || heroListRedContent == null || heroListBlueContent == null)
        {
            Debug.LogError("HeroSelectionPrefab or heroListRed/BlueContent is not assigned.");
            return;
        }

        foreach (var heroPrefab in allHeroPrefabs)
        {
            GameObject redButton = Instantiate(HeroSelectionPrefab, heroListRedContent);
            GameObject blueButton = Instantiate(HeroSelectionPrefab, heroListBlueContent);

            string heroName = heroPrefab.name;

            SetupHeroButton(redButton, heroName, heroPrefab, Team.Red);
            SetupHeroButton(blueButton, heroName, heroPrefab, Team.Blue);

            heroSelectionPrefabsRed[heroName] = redButton;
            heroSelectionPrefabsBlue[heroName] = blueButton;
        }

    }
    private void SetupHeroButton(GameObject buttonObj, string heroName, Hero heroPrefab, Team team)
    {
        buttonObj.transform.localScale = Vector3.one;

        TMP_Text nameText = buttonObj.GetComponentInChildren<TMP_Text>();
        if (nameText != null) nameText.text = heroName;

        Image img = buttonObj.GetComponentInChildren<Image>();
        if (img != null && heroPrefab.heroImage != null)
        {
            img.sprite = heroPrefab.heroImage;
            img.color = Color.white;
        }

        Button btn = buttonObj.GetComponentInChildren<Button>();
        btn.onClick.AddListener(() => OnHeroSelected(heroName));
    }

    [PunRPC]
    public void RPC_LockHero(string heroName, bool isLocked, int lockingTeamValue)
    {
        Team myTeam = TeamManager.instance.GetTeam(PhotonNetwork.LocalPlayer);
        Team lockingTeam = (Team)lockingTeamValue;

        //  แก้ให้ชัดเจน: เปลี่ยนเฉพาะปุ่ม UI ของทีมที่ "ล็อก"
        Dictionary<string, GameObject> targetDict = (lockingTeam == Team.Red)
            ? heroSelectionPrefabsRed
            : heroSelectionPrefabsBlue;

        if (!targetDict.ContainsKey(heroName)) return;

        Image img = targetDict[heroName].GetComponentInChildren<Image>();
        if (img != null)
        {
            img.color = isLocked ? Color.gray : Color.white;
        }
    }



 

    [PunRPC]
    private void RPC_UpdateHeroSelection(string previousHero, string newHero, int playerID)
    {
        TeamManager teamManager = TeamManager.instance;
        if (teamManager == null) return;

        if (!PhotonNetwork.CurrentRoom.Players.ContainsKey(playerID)) return;
        Player selectingPlayer = PhotonNetwork.CurrentRoom.Players[playerID];

        Team selectingTeam = teamManager.GetTeam(selectingPlayer);
        Team myTeam = teamManager.GetTeam(PhotonNetwork.LocalPlayer);

        //  ถ้าไม่ใช่ทีมเดียวกัน → อย่าอัปเดต UI ใด ๆ
        if (myTeam != selectingTeam) return;

        Dictionary<string, GameObject> myTeamDict = (myTeam == Team.Red) ? heroSelectionPrefabsRed : heroSelectionPrefabsBlue;

        if (!string.IsNullOrEmpty(previousHero) && myTeamDict.ContainsKey(previousHero))
        {
            Image img = myTeamDict[previousHero].GetComponentInChildren<Image>();
            if (img != null) img.color = Color.white;
        }

        if (!string.IsNullOrEmpty(newHero) && myTeamDict.ContainsKey(newHero))
        {
            Image img = myTeamDict[newHero].GetComponentInChildren<Image>();
            if (img != null) img.color = Color.gray;
        }
    }


    private void OnHeroSelected(string heroName)
    {
        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager == null) return;

        if (IsHeroAlreadySelectedByTeam(heroName)) return;

        int teamValue = (int)TeamManager.instance.GetTeam(PhotonNetwork.LocalPlayer);
        //string teamKey = myTeam == Team.Red ? "Red" : "Blue";

        // คืนสีปุ่มเก่าแบบ RPC
        if (!string.IsNullOrEmpty(previouslySelectedHero))
        {
            photonView.RPC("RPC_LockHero", RpcTarget.All, previouslySelectedHero, false, teamValue);
        }

        // อัปเดต UI
        lobbyManager.photonView.RPC("RPC_UpdateHeroEntry", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, heroName);
        photonView.RPC("RPC_UpdateHeroSelection", RpcTarget.All, previouslySelectedHero, heroName, PhotonNetwork.LocalPlayer.ActorNumber);

        // เซ็ต CustomProperties
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable { { "selectedHero", heroName } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        Hero hero = allHeroPrefabs.Find(h => h.name == heroName);
        if (hero != null)
        {
            TeamManager.instance.photonView.RPC("RPC_AssignHero", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber, hero.name);
        }

        // ล็อคปุ่มใหม่
        photonView.RPC("RPC_LockHero", RpcTarget.All, heroName, true, teamValue);

        previouslySelectedHero = heroName;
    }




    // ตรวจสอบว่าตัวละครนี้ถูกใช้โดยผู้เล่นในทีมเดียวกันหรือไม่
    private bool IsHeroAlreadySelectedByTeam(string heroName)
    {
        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager == null)
        {
            Debug.LogError("LobbyManager not found in the scene.");
            return false;
        }

        TeamManager teamManager = TeamManager.instance;
        if (teamManager == null)
        {
            Debug.LogError("TeamManager instance is null.");
            return false;
        }

        // ตรวจสอบว่าตัวละครนี้ถูกใช้โดยผู้เล่นในทีมเดียวกันหรือไม่
        foreach (var player in teamManager.playerHeroes.Keys)
        {
            bool areThisIsOurTeam = teamManager.AreOnSameTeam(player, PhotonNetwork.LocalPlayer);
            string thereHero = teamManager.playerHeroes[player].name;
            if (areThisIsOurTeam && thereHero == heroName)
            {
                return true; // ตัวละครนี้ถูกใช้แล้วโดยผู้เล่นในทีมเดียวกัน
            }
        }

        return false; // ตัวละครนี้ยังไม่ถูกใช้
    }



}
