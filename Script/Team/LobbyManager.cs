using ExitGames.Client.Photon;
using Photon.Realtime;
using System.Collections.Generic;
using System.Collections;
using Photon.Pun;
//using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using UnityEngine.EventSystems;
using System.Text.RegularExpressions;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Login Panel")]
        public GameObject LoginPanel;

        public TMP_InputField  PlayerNameInput;
        // public InputField PlayerNameInput;

        [Header("Selection Panel")]
        public GameObject SelectionPanel;

        [Header("Create Room Panel")]
        public GameObject CreateRoomPanel;
        public TMP_InputField  RoomNameInputField;
        public TMP_InputField  MaxPlayersInputField;

        // public InputField RoomNameInputField;
        // public InputField MaxPlayersInputField;

        [Header("Join Random Room Panel")]
        public GameObject JoinRandomRoomPanel;

        [Header("Room List Panel")]
        public GameObject RoomListPanel;

        public GameObject RoomListContent;
        public GameObject RoomListEntryPrefab;

        [Header("Inside Room Panel")]
        public GameObject InsideRoomPanel;
        public GameObject BlueTeamPanel; 
        public GameObject RedTeamPanel; 

        public Button StartGameButton;
        public GameObject PlayerListEntryPrefab;

        [Header("Select Hero Panel")]
        public GameObject SelectHeroPanel;
        public GameObject HeroList;
        public GameObject HeroSelectionPrefab;
        public TextMeshProUGUI countdownText;
        public GameObject BlueTeamPlayers; // Panel สำหรับผู้เล่นทีมฟ้า
        public GameObject RedTeamPlayers; // Panel สำหรับผู้เล่นทีมแดง
        public GameObject PlayerSelectHeroEntryPrefab; 
        private Dictionary<int, GameObject> playerHeroEntries = new Dictionary<int, GameObject>();


        public HeroSelectionManager heroSelectionManager;

        [Header("Loading Map Panel")]
        public GameObject LoadingMapPanel;
        private Dictionary<string, RoomInfo> cachedRoomList;
        private Dictionary<string, GameObject> roomListEntries;
        private Dictionary<int, GameObject> playerListEntries;
        private Dictionary<int, string> playerSelectedHeroes = new Dictionary<int, string>(); // จัดเก็บฮีโร่ที่ผู้เล่นเลือก
        //private Dictionary<string, bool> heroLocks = new Dictionary<string, bool>(); // บันทึกสถานะของฮีโร่ (เลือก/ไม่ได้เลือก)
        private Dictionary<Team, Dictionary<string, bool>> heroLocksPerTeam = new Dictionary<Team, Dictionary<string, bool>>();


        private float countdownTime = 30f; 
        private bool isCountdownStarted = false; //ไว้ป้องกันการเรียกหลายรอบ แล้วเวลาลดไวผิดปกติ
        private bool selectionConfirmed = false; // สำหรับเช็คว่าผู้เล่นยืนยันตัวเลือกแล้วหรือยัง
        private bool isSceneLoading = false; 

        #region UNITY
        // void Start()
        // {
        //     Debug.Log("PlayerNameInput Active: " + PlayerNameInput.gameObject.activeInHierarchy);
        // }

        private void ValidateNumberOnly(string input)
        {
            string filtered = new string(input.Where(char.IsDigit).ToArray());

            if (filtered != input)
            {
                MaxPlayersInputField.text = filtered;
            }
        }
        public void Awake()
        {
            PhotonNetwork.AutomaticallySyncScene = true;

            cachedRoomList = new Dictionary<string, RoomInfo>();
            roomListEntries = new Dictionary<string, GameObject>();
            playerListEntries = new Dictionary<int, GameObject>();
            PlayerNameInput.text = "Player " + Random.Range(1000, 10000);

            // เพิ่มให้รองรับมือถือ
            // PlayerNameInput.onSelect.AddListener(ShowKeyboard);
            // RoomNameInputField.onSelect.AddListener(ShowKeyboard);
            // MaxPlayersInputField.onSelect.AddListener(ShowKeyboard);
            MaxPlayersInputField.contentType = TMP_InputField.ContentType.IntegerNumber;//รับแค่เลข
            MaxPlayersInputField.onValueChanged.AddListener(ValidateNumberOnly);

            PlayerNameInput.onSelect.AddListener((text) => OpenKeyboard(PlayerNameInput));
            RoomNameInputField.onSelect.AddListener((text) => OpenKeyboard(RoomNameInputField));
            MaxPlayersInputField.onSelect.AddListener((text) => OpenKeyboard(MaxPlayersInputField));
        }
        private void OpenKeyboard(TMP_InputField inputField)
        {
            #if UNITY_ANDROID || UNITY_IOS
                Debug.Log($" OpenKeyboard Called for: {inputField.name}");

                if (TouchScreenKeyboard.visible)
                {
                    Debug.Log(" Keyboard already open, skipping.");
                    return;
                }

                //  ให้ Unity โฟกัสที่ InputField จริง ๆ
                EventSystem.current.SetSelectedGameObject(inputField.gameObject);

                //  หน่วงเวลาเล็กน้อยก่อนเปิดคีย์บอร์ด เพื่อให้ Unity อัปเดตสถานะ
                StartCoroutine(DelayedKeyboardOpen(inputField));
            #endif
        }

        private IEnumerator DelayedKeyboardOpen(TMP_InputField inputField)
        {
            yield return new WaitForSeconds(0.1f); // ให้เวลา Unity อัปเดตก่อน
            inputField.ActivateInputField();  //  บังคับให้โฟกัสที่ InputField
            inputField.Select(); //  บังคับให้เลือกข้อความ

            Debug.Log(" Opening Keyboard...");
            TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
        }

        private void ShowKeyboard(string text)
        {
            #if UNITY_ANDROID || UNITY_IOS
                TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
            #endif
        }

        #endregion

        #region PUN CALLBACKS

        public override void OnConnectedToMaster()
        {
            this.SetActivePanel(SelectionPanel.name);
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            ClearRoomListView();

            UpdateCachedRoomList(roomList);
            UpdateRoomListView();
        }

        public override void OnJoinedLobby()
        {
            // whenever this joins a new lobby, clear any previous room lists
            cachedRoomList.Clear();
            ClearRoomListView();
        }

        // note: when a client joins / creates a room, OnLeftLobby does not get called, even if the client was in a lobby before
        public override void OnLeftLobby()
        {
            cachedRoomList.Clear();
            ClearRoomListView();
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            SetActivePanel(SelectionPanel.name);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            SetActivePanel(SelectionPanel.name);
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            string roomName = "Room " + Random.Range(1000, 10000);

            RoomOptions options = new RoomOptions {MaxPlayers = 10};

            PhotonNetwork.CreateRoom(roomName, options, null);
        }


        public override void OnJoinedRoom()
        {
            //Debug.Log($" {PhotonNetwork.LocalPlayer.NickName} Joined Room");

            SetActivePanel(InsideRoomPanel.name);

            //  กำหนด Panel ให้ตัวเอง (เฉพาะถ้ายังไม่มีค่า assignedPanel เท่านั้น)
            if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("assignedPanel"))
            {
                AssignPlayerToRandomPanel(PhotonNetwork.LocalPlayer);
            }

            //  รอให้ assignedPanel อัปเดตก่อนเรียก AddPlayerListEntry()
            StartCoroutine(WaitForAssignedPanelAndAddEntry(PhotonNetwork.LocalPlayer));

            StartGameButton.gameObject.SetActive(CheckPlayersReady());
        }


        private IEnumerator WaitForAssignedPanelAndAddEntry(Player player)
        {
            //Debug.Log(" Waiting for assignedPanel to update...");

            //  ตรวจสอบว่าผู้เล่นถูกเพิ่มไปแล้วหรือไม่
            if (playerListEntries.ContainsKey(player.ActorNumber))
            {
                Debug.LogWarning($" Player {player.NickName} already exists in playerListEntries, skipping.");
                yield break;
            }

            // รอจนกว่าผู้เล่นจะมี assignedPanel ใน CustomProperties
            while (!player.CustomProperties.ContainsKey("assignedPanel"))
            {
                yield return new WaitForSeconds(0.1f);
            }

            //Debug.Log($" {player.NickName} assigned to {player.CustomProperties["assignedPanel"]}");

            //  เรียก AddPlayerListEntry() เฉพาะเมื่อยังไม่มีผู้เล่นใน Dictionary
            if (!playerListEntries.ContainsKey(player.ActorNumber))
            {
                AddPlayerListEntry(player);
            }
        }




        private void AssignPlayerToRandomPanel(Player player)
        {
            List<string> availablePanels = new List<string>();

            for (int i = 1; i <= 10; i++)
            {
                string panelName = "Player" + i + "Panel";
                GameObject panel = GameObject.Find(panelName);

                if (panel != null)
                {
                    Transform playerInfoPanel = panel.transform.Find("PlayerInfoPanel");

                    if (playerInfoPanel != null)
                    {
                        bool isPanelUsed = PhotonNetwork.PlayerList.Any(p =>
                            p.CustomProperties.ContainsKey("assignedPanel") &&
                            (string)p.CustomProperties["assignedPanel"] == panelName
                        );

                        if (!isPanelUsed)
                        {
                            availablePanels.Add(panelName);
                        }
                    }
                    else
                    {
                        Debug.LogError($"PlayerInfoPanel not found inside {panelName}");
                    }
                }
                else
                {
                    Debug.LogError($"Panel not found: {panelName}");
                }
            }

            if (availablePanels.Count == 0)
            {
                Debug.Log("No available panels left!");
                return;
            }

            string assignedPanel = availablePanels[Random.Range(0, availablePanels.Count)];

            // int panelNumber = int.Parse(assignedPanel.Replace("Player", "").Replace("Panel", ""));
            // วิธีใหม่ ป้องกัน Player10Panel กลายเป็น Player1
            //int panelNumber = int.Parse(System.Text.RegularExpressions.Regex.Match(assignedPanel, @"\d+").Value);
            var match = System.Text.RegularExpressions.Regex.Match(assignedPanel, @"Player(\d+)Panel");
            int panelNumber = match.Success ? int.Parse(match.Groups[1].Value) : -1;
            Debug.Log($"🧪 Extracted panelNumber from {assignedPanel} → {panelNumber}");


            Team assignedTeam = (panelNumber <= 5) ? Team.Blue : Team.Red;

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "assignedPanel", assignedPanel },
                { "team", (int)assignedTeam }
            };

            player.SetCustomProperties(props);
            //Debug.Log($" Assigning {player.NickName} to {assignedPanel} (Team: {assignedTeam})");
        }

        private void UpdatePlayerPanelUI(Player player, string panelName)
        {
            if (!playerListEntries.ContainsKey(player.ActorNumber)) return;

            GameObject playerEntry = playerListEntries[player.ActorNumber];
            GameObject panel = GameObject.Find(panelName);

            if (panel != null)
            {
                Transform infoPanel = panel.transform.Find("PlayerInfoPanel");
                if (infoPanel != null)
                {
                    playerEntry.transform.SetParent(infoPanel, false);
                }
            }
        }

        [PunRPC]
        private void RPC_UpdatePlayerPanelUI(int actorNumber, string panelName)
        {
            if (!playerListEntries.ContainsKey(actorNumber)) return;

            GameObject playerEntry = playerListEntries[actorNumber];
            GameObject panel = GameObject.Find(panelName);

            if (panel != null)
            {
                Transform infoPanel = panel.transform.Find("PlayerInfoPanel");
                if (infoPanel != null)
                {
                    playerEntry.transform.SetParent(infoPanel, false);
                }else{
                    Debug.LogError("Cant find PlayerInfoPanel");
                }
            }
        }
        public void OnSwapButtonClicked(string targetPanel)
        {
            Player localPlayer = PhotonNetwork.LocalPlayer;
            string currentPanel = (string)localPlayer.CustomProperties["assignedPanel"];

            if (currentPanel == targetPanel) return;

            Player otherPlayer = PhotonNetwork.PlayerList.FirstOrDefault(p =>
                p.CustomProperties.ContainsKey("assignedPanel") &&
                (string)p.CustomProperties["assignedPanel"] == targetPanel
            );

            if (otherPlayer != null)
            {
                // ถ้ามีผู้เล่นอยู่ที่ตำแหน่งเป้าหมาย → แลกที่กัน
                //Debug.Log($"{localPlayer.ActorNumber} swap to {targetPanel} -- and {otherPlayer.ActorNumber} swap to {currentPanel}");
                photonView.RPC("RPC_SwapPlayers", RpcTarget.AllBuffered, localPlayer.ActorNumber, otherPlayer.ActorNumber, targetPanel);
            }
            else
            {
                // ถ้าไม่มีใครอยู่ → ย้ายไปเลย
                //Debug.Log($"{localPlayer.ActorNumber} swap to {targetPanel}");
                photonView.RPC("RPC_MovePlayer", RpcTarget.AllBuffered, localPlayer.ActorNumber, targetPanel);
            }
        }
        [PunRPC]
        private void RPC_SwapPlayers(int player1Id, int player2Id, string newPanel)
        {
            Player player1 = PhotonNetwork.PlayerList.FirstOrDefault(p => p.ActorNumber == player1Id);
            Player player2 = PhotonNetwork.PlayerList.FirstOrDefault(p => p.ActorNumber == player2Id);

            if (player1 != null && player2 != null)
            {
                string panel1 = (string)player1.CustomProperties["assignedPanel"];
                string panel2 = newPanel;

                int panelNumber1 = int.Parse(Regex.Match(panel1, @"Player(\d+)Panel").Groups[1].Value);
                int panelNumber2 = int.Parse(Regex.Match(panel2, @"Player(\d+)Panel").Groups[1].Value);

                Team team1 = (panelNumber2 <= 5) ? Team.Blue : Team.Red;
                Team team2 = (panelNumber1 <= 5) ? Team.Blue : Team.Red;


                ExitGames.Client.Photon.Hashtable props1 = new ExitGames.Client.Photon.Hashtable 
                { 
                    { "assignedPanel", panel2 }, 
                    { "team", (int)team1 } //  อัปเดตค่า team ด้วย
                };

                ExitGames.Client.Photon.Hashtable props2 = new ExitGames.Client.Photon.Hashtable 
                { 
                    { "assignedPanel", panel1 }, 
                    { "team", (int)team2 } //  อัปเดตค่า team ด้วย
                };
                //Debug.Log($"Swap to {newPanel} → PanelNumber: {panelNumber2}, Assigned Team: {team2}");


                player1.SetCustomProperties(props1);
                player2.SetCustomProperties(props2);

                //  อัปเดต UI ของผู้เล่นทั้งสองคน
                RPC_UpdatePlayerPanelUI(player1.ActorNumber, panel2);
                RPC_UpdatePlayerPanelUI(player2.ActorNumber, panel1);
            }
        }

        [PunRPC]
        private void RPC_MovePlayer(int playerId, string newPanel)
        {
            Player player = PhotonNetwork.PlayerList.FirstOrDefault(p => p.ActorNumber == playerId);

            if (player != null)
            {
                int panelNumber = -1;
                var match = Regex.Match(newPanel, @"Player(\d+)Panel");
                if (match.Success) panelNumber = int.Parse(match.Groups[1].Value);
                Team newTeam = (panelNumber >= 1 && panelNumber <= 5) ? Team.Blue : Team.Red;

                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
                {
                    { "assignedPanel", newPanel },
                    { "team", (int)newTeam } //  อัปเดตค่า team ด้วย
                };
                //Debug.Log($"Swap to {newPanel} new team {newTeam}");

                player.SetCustomProperties(props);

                //  อัปเดต UI ของผู้เล่นในทีมใหม่
                RPC_UpdatePlayerPanelUI(player.ActorNumber, newPanel);
            }
        }

        public Team GetTeam(Player player)
        {
            if (player.CustomProperties.ContainsKey("team"))
            {
                return (Team)(int)player.CustomProperties["team"];
            }
            return Team.Red; // Default team if none is assigned
        }
        private void AddPlayerListEntry(Player player)
        {
            if (playerListEntries == null)
            {
                playerListEntries = new Dictionary<int, GameObject>();
            }

            //  ตรวจสอบว่าผู้เล่นถูกเพิ่มไปแล้วหรือยัง
            if (playerListEntries.ContainsKey(player.ActorNumber))
            {
                Debug.LogWarning($" Player {player.NickName} (ActorNumber: {player.ActorNumber}) is already in playerListEntries!");
                return;
            }

            //Debug.Log($" Adding Player: {player.ActorNumber}, Name: {player.NickName}");

            GameObject entry = Instantiate(PlayerListEntryPrefab);
            entry.transform.localScale = Vector3.one;

            PlayerListEntry playerListEntryScript = entry.GetComponent<PlayerListEntry>();
            if (playerListEntryScript != null)
            {
                playerListEntryScript.Initialize(player.ActorNumber, player.NickName);
            }
            else
            {
                Debug.LogError(" PlayerListEntry script is missing from PlayerListEntryPrefab.");
            }

            //  ตรวจสอบว่าผู้เล่นมี assignedPanel หรือไม่
            if (player.CustomProperties.ContainsKey("assignedPanel"))
            {
                string assignedPanelName = (string)player.CustomProperties["assignedPanel"];
                //Debug.Log($" {player.NickName} assigned to {assignedPanelName}");

                GameObject assignedPanel = GameObject.Find(assignedPanelName);
                if (assignedPanel != null)
                {
                    Transform playerInfoPanel = assignedPanel.transform.Find("PlayerInfoPanel");
                    if (playerInfoPanel != null)
                    {
                        entry.transform.SetParent(playerInfoPanel, false);
                    }
                    else
                    {
                        Debug.LogError($" Cannot find PlayerInfoPanel inside {assignedPanelName}");
                    }
                }
                else
                {
                    Debug.LogError($" Cannot find assigned panel: {assignedPanelName}");
                }
            }
            else
            {
                Debug.LogError($" ERROR: {player.NickName} has NO assignedPanel in CustomProperties!");
            }

            //  เพิ่มเข้า Dictionary หลังจากตรวจสอบว่าไม่มีอยู่ก่อน
            playerListEntries.Add(player.ActorNumber, entry);
        }

        public override void OnLeftRoom()
        {
            SetActivePanel(SelectionPanel.name);

            foreach (GameObject entry in playerListEntries.Values)
            {
                Destroy(entry.gameObject);
            }

            playerListEntries.Clear();
            //playerListEntries = null;
        }


        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            //Debug.Log($" {newPlayer.NickName} entered the room.");

            //  ไม่ต้อง Assign Panel ให้ผู้เล่นใหม่ซ้ำ เพราะเขาเซ็ตค่าของตัวเองจาก OnJoinedRoom() แล้ว
            StartCoroutine(WaitForAssignedPanelAndAddEntry(newPlayer));

            StartGameButton.gameObject.SetActive(CheckPlayersReady());
            photonView.RPC("RPC_UpdateAllPlayerUIs", RpcTarget.All);
            
            //  ไม่ต้องเรียก RPC_UpdateAllPlayerUIs() ซ้ำ
        }

        

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (playerListEntries.ContainsKey(otherPlayer.ActorNumber))
            {
                Destroy(playerListEntries[otherPlayer.ActorNumber]);
                playerListEntries.Remove(otherPlayer.ActorNumber);
            }
            StartGameButton.gameObject.SetActive(CheckPlayersReady());
            photonView.RPC("RPC_UpdateAllPlayerUIs", RpcTarget.All);
            
        }


        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == newMasterClient.ActorNumber)
            {
                StartGameButton.gameObject.SetActive(CheckPlayersReady());
            }
        }


        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (changedProps.ContainsKey("selectedHero"))
            {
                string selectedHero = (string)changedProps["selectedHero"];
                //Debug.Log($" OnPlayerPropertiesUpdate: {targetPlayer.NickName} selected {selectedHero}");

                // อัปเดตค่าของ `playerSelectedHeroes`
                playerSelectedHeroes[targetPlayer.ActorNumber] = selectedHero;

                // ล็อคฮีโร่ที่เลือก
                Team targetTeam = TeamManager.instance.GetTeam(targetPlayer);
                int teamValue = (int)targetTeam;
                heroSelectionManager.photonView.RPC("RPC_LockHero", RpcTarget.All, selectedHero, true, teamValue);


                //heroSelectionManager.photonView.RPC("RPC_LockHero", RpcTarget.All, selectedHero, true);
            }
            if (playerListEntries == null)
            {
                playerListEntries = new Dictionary<int, GameObject>();
            }

            GameObject entry;
            if (playerListEntries.TryGetValue(targetPlayer.ActorNumber, out entry))
            {
                object isPlayerReady;
                if (changedProps.TryGetValue(Game.PLAYER_READY, out isPlayerReady))
                {
                    entry.GetComponent<PlayerListEntry>().SetPlayerReady((bool) isPlayerReady);
                }
            }

            StartGameButton.gameObject.SetActive(CheckPlayersReady());
        }
        private void ShowHeroSelection()
        {
            heroSelectionManager.gameObject.SetActive(true);
        }
        public void OnHeroSelected(string heroName)
        {
            HeroSelectionManager heroSelectionManager = FindObjectOfType<HeroSelectionManager>();
            if (heroSelectionManager == null)
            {
                Debug.LogError(" HeroSelectionManager not found in the scene!");
                return;
            }
            int teamValue = (int)TeamManager.instance.GetTeam(PhotonNetwork.LocalPlayer);
            heroSelectionManager.photonView.RPC("RPC_LockHero", RpcTarget.All, heroName, true, teamValue);
        }

 

        #endregion

        #region UI CALLBACKS

        public void OnBackButtonClicked()
        {
            if (PhotonNetwork.InLobby)
            {
                PhotonNetwork.LeaveLobby();
            }

            SetActivePanel(SelectionPanel.name);
        }

        public void OnCreateRoomButtonClicked()
        {
            string roomName = RoomNameInputField.text;
            roomName = (roomName.Equals(string.Empty)) ? "Room " + Random.Range(1000, 10000) : roomName;

            byte maxPlayers;
            byte.TryParse(MaxPlayersInputField.text, out maxPlayers);
            maxPlayers = (byte) Mathf.Clamp(maxPlayers, 2, 10);

            RoomOptions options = new RoomOptions {MaxPlayers = maxPlayers, PlayerTtl = 10000 };

            PhotonNetwork.CreateRoom(roomName, options, null);
        }

        public void OnJoinRandomRoomButtonClicked()
        {
            SetActivePanel(JoinRandomRoomPanel.name);

            PhotonNetwork.JoinRandomRoom();
        }

        public void OnLeaveGameButtonClicked()
        {
            PhotonNetwork.LeaveRoom();
        }

        public void OnLoginButtonClicked()
        {
            string playerName = PlayerNameInput.text.Trim();

            if (!playerName.Equals(""))
            {
                PhotonNetwork.LocalPlayer.NickName = playerName;
                PhotonNetwork.ConnectUsingSettings();
            }
            else
            {
                Debug.LogError("Player Name is invalid.");
            }
        }

        public void OnRoomListButtonClicked()
        {
            if (!PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
            }

            SetActivePanel(RoomListPanel.name);
        }

        #endregion

        private bool CheckPlayersReady()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return false;
            }

            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.CustomProperties.TryGetValue(Game.PLAYER_READY, out object isPlayerReady))
                {
                    if (!(bool)isPlayerReady)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;

        }
        
        private void ClearRoomListView()
        {
            foreach (GameObject entry in roomListEntries.Values)
            {
                Destroy(entry.gameObject);
            }

            roomListEntries.Clear();
        }

        public void LocalPlayerPropertiesUpdated()
        {
            StartGameButton.gameObject.SetActive(CheckPlayersReady());
        }

        public void SetActivePanel(string activePanel)
        {
            LoginPanel.SetActive(activePanel.Equals(LoginPanel.name));
            SelectionPanel.SetActive(activePanel.Equals(SelectionPanel.name));
            CreateRoomPanel.SetActive(activePanel.Equals(CreateRoomPanel.name));
            JoinRandomRoomPanel.SetActive(activePanel.Equals(JoinRandomRoomPanel.name));
            RoomListPanel.SetActive(activePanel.Equals(RoomListPanel.name));    // UI should call OnRoomListButtonClicked() to activate this
            InsideRoomPanel.SetActive(activePanel.Equals(InsideRoomPanel.name));
            SelectHeroPanel.SetActive(activePanel.Equals(SelectHeroPanel.name));
            LoadingMapPanel.SetActive(activePanel.Equals(LoadingMapPanel.name));
        }

        private void UpdateCachedRoomList(List<RoomInfo> roomList)
        {
            foreach (RoomInfo info in roomList)
            {
                // Remove room from cached room list if it got closed, became invisible or was marked as removed
                if (!info.IsOpen || !info.IsVisible || info.RemovedFromList)
                {
                    if (cachedRoomList.ContainsKey(info.Name))
                    {
                        cachedRoomList.Remove(info.Name);
                    }

                    continue;
                }

                // Update cached room info
                if (cachedRoomList.ContainsKey(info.Name))
                {
                    cachedRoomList[info.Name] = info;
                }
                // Add new room info to cache
                else
                {
                    cachedRoomList.Add(info.Name, info);
                }
            }
        }

        private void UpdateRoomListView()
        {
            foreach (RoomInfo info in cachedRoomList.Values)
            {
                GameObject entry = Instantiate(RoomListEntryPrefab);
                entry.transform.SetParent(RoomListContent.transform);
                entry.transform.localScale = Vector3.one;
                entry.GetComponent<RoomListEntry>().Initialize(info.Name, (byte)info.PlayerCount, (byte)info.MaxPlayers);

                roomListEntries.Add(info.Name, entry);
            }
        }



        #region UpdateUIInsideRoompanel
        private void UpdatePlayerUI(Player player)
        {
            int actorNumber = player.ActorNumber;
            bool isReady = player.CustomProperties.ContainsKey(Game.PLAYER_READY) && (bool)player.CustomProperties[Game.PLAYER_READY];

            if (playerListEntries.ContainsKey(actorNumber))
            {
                Destroy(playerListEntries[actorNumber]);
                playerListEntries.Remove(actorNumber);
            }

            GameObject entry = Instantiate(PlayerListEntryPrefab);
            entry.transform.localScale = Vector3.one;
            PlayerListEntry playerListEntryScript = entry.GetComponent<PlayerListEntry>();

            if (playerListEntryScript != null)
            {
                playerListEntryScript.Initialize(player.ActorNumber, player.NickName);
                playerListEntryScript.SetPlayerReady(isReady); // ดึงค่าจาก Custom Properties โดยตรง
            }

            //  ตรวจสอบ assignedPanel จาก CustomProperties แทนการใช้ Team Panel
            if (player.CustomProperties.ContainsKey("assignedPanel"))
            {
                string assignedPanelName = (string)player.CustomProperties["assignedPanel"];
                GameObject assignedPanel = GameObject.Find(assignedPanelName);

                if (assignedPanel != null)
                {
                    Transform playerInfoPanel = assignedPanel.transform.Find("PlayerInfoPanel");
                    if (playerInfoPanel != null)
                    {
                        entry.transform.SetParent(playerInfoPanel, false);
                    }
                    else
                    {
                        Debug.LogError($" Cannot find PlayerInfoPanel inside {assignedPanelName}");
                    }
                }
                else
                {
                    Debug.LogError($" Cannot find assigned panel: {assignedPanelName}");
                }
            }
            else
            {
                Debug.LogError($" ERROR: {player.NickName} has NO assignedPanel in CustomProperties!");
            }

            playerListEntries[actorNumber] = entry;
        }
        [PunRPC]
        private void RPC_UpdateAllPlayerUIs()
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                //  ตรวจสอบว่ามี assignedPanel อยู่แล้วหรือไม่
                if (player.CustomProperties.ContainsKey("assignedPanel"))
                {
                    UpdatePlayerUI(player);
                }
                else
                {
                    Debug.LogWarning($" Skipping UpdatePlayerUI for {player.NickName} (No assignedPanel)");
                }
            }
            StartGameButton.gameObject.SetActive(CheckPlayersReady());
        }



         public void OnStartGameButtonClicked()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
                photonView.RPC("RPC_GoToSelectHeroPanel", RpcTarget.All);
            }
        }
        #endregion

        #region SELECT HERO PANEL
        //SELECT HERO PANEL
        private void CreatePlayerHeroEntry(Player player)
        {
            if (playerHeroEntries.ContainsKey(player.ActorNumber)) return;

            GameObject entry = Instantiate(PlayerSelectHeroEntryPrefab);
            PlayerSelectHeroEntry entryScript = entry.GetComponent<PlayerSelectHeroEntry>();

            if (entryScript != null)
            {
                entryScript.Initialize(player.ActorNumber, player.NickName);
            }

            //  ดึงค่าทีมจาก Photon CustomProperties แทน
            Team team = (player.CustomProperties.ContainsKey("team")) ? (Team)(int)player.CustomProperties["team"] : Team.Red;
            entry.transform.SetParent(team == Team.Blue ? BlueTeamPlayers.transform : RedTeamPlayers.transform, false);

            playerHeroEntries[player.ActorNumber] = entry;
        }

        [PunRPC]
        public void RPC_UpdateHeroEntry(int playerId, string heroName)
        {
            if (!playerHeroEntries.ContainsKey(playerId)) return;

            GameObject entry = playerHeroEntries[playerId];
            PlayerSelectHeroEntry entryScript = entry.GetComponent<PlayerSelectHeroEntry>();

            if (entryScript != null)
            {
                Hero heroPrefab = HeroSelectionManager.instance.allHeroPrefabs.Find(h => h.name == heroName);
                if (heroPrefab != null)
                {
                    entryScript.heroNameText.text = heroName;
                    entryScript.heroImage.sprite = heroPrefab.heroImage;
                }
            }
        }



        private void InitializePlayerSelections()
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (!playerSelectedHeroes.ContainsKey(player.ActorNumber))
                {
                    playerSelectedHeroes[player.ActorNumber] = null; // กำหนดค่าเริ่มต้นเป็น null
                    Debug.Log($"Initialized player {player.ActorNumber} in playerSelectedHeroes with null value.");
                }
            }
            //Initialize heroLocks
            List<string> heroNames = new List<string> { "Alu","Blop", "BoneReaper","D'Albeto", "Gamaira" ,"Ray","Selina","Sha Sha", "Sque","Tink" };
            // foreach (string hero in heroNames)
            // {
            //     heroLocks[hero] = false;
            // }
            heroLocksPerTeam[Team.Red] = new Dictionary<string, bool>();
            heroLocksPerTeam[Team.Blue] = new Dictionary<string, bool>();

            foreach (string hero in heroNames)
            {
                heroLocksPerTeam[Team.Red][hero] = false;
                heroLocksPerTeam[Team.Blue][hero] = false;
            }
        }
        [PunRPC]
        private void RPC_GoToSelectHeroPanel()
        {
            SetActivePanel(SelectHeroPanel.name);
            InitializePlayerSelections();

            // ซ่อน panel ฝั่งตรงข้าม
            HeroSelectionManager.instance.heroListRedContent.gameObject.SetActive(false);
            HeroSelectionManager.instance.heroListBlueContent.gameObject.SetActive(false);

            Team myTeam = TeamManager.instance.GetTeam(PhotonNetwork.LocalPlayer);
            if (myTeam == Team.Red)
            {
                HeroSelectionManager.instance.heroListRedContent.gameObject.SetActive(true);
            }
            else if (myTeam == Team.Blue)
            {
                HeroSelectionManager.instance.heroListBlueContent.gameObject.SetActive(true);
            }

            // สร้าง UI ของผู้เล่นแต่ละคน
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                CreatePlayerHeroEntry(player);
            }

            ShowHeroSelection();
            photonView.RPC("RPC_StartCountdown", RpcTarget.All);
        }

        [PunRPC]
        private void RPC_StartCountdown()
        {
            countdownText.gameObject.SetActive(true);
            selectionConfirmed = false;

            if (PhotonNetwork.IsMasterClient && !isCountdownStarted)
            {
                isCountdownStarted = true; //  กันเรียกซ้ำ
                InvokeRepeating("MasterUpdateCountdown", 0f, 1f);
            }
        }

        private void MasterUpdateCountdown()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            countdownTime -= 1f;
            photonView.RPC("RPC_SyncCountdown", RpcTarget.AllBuffered, countdownTime);

            if (countdownTime <= 0)
            {
                CancelInvoke("MasterUpdateCountdown");
                //SetActivePanel(LoadingMapPanel.name);
                photonView.RPC("RPC_ConfirmSelections", RpcTarget.AllBuffered);
            }
        }

        [PunRPC]
        private void RPC_SyncCountdown(float newCountdownTime)
        {
            countdownTime = newCountdownTime;
            countdownText.text = countdownTime.ToString("F0");
        }

        [PunRPC]
        private void RPC_ConfirmSelections()
        {
            //Debug.Log(" Starting RPC_ConfirmSelections...");
            SetActivePanel(LoadingMapPanel.name); 
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (!playerSelectedHeroes.ContainsKey(player.ActorNumber) ||
                    string.IsNullOrEmpty(playerSelectedHeroes[player.ActorNumber]))
                {
                   // Debug.LogWarning($" {player.NickName} did not select a hero.");
                    
                    if (PhotonNetwork.IsMasterClient)
                    {
                        // Master เป็นคนสุ่ม แล้วส่ง RPC ไป
                        photonView.RPC("RPC_AssignRandomHero", RpcTarget.MasterClient, player.ActorNumber);
                    }
                }
                else
                {
                    // ยังไม่บันทึก selectedHero → ก็เซ็ต CustomProperties ซ้ำ
                    string selectedHeroName = playerSelectedHeroes[player.ActorNumber];
                    ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
                    {
                        { "selectedHero", selectedHeroName }
                    };
                    player.SetCustomProperties(props);
                }
            }

            StartCoroutine(WaitForCustomPropertiesAndChangeScene());
        }

        private IEnumerator WaitForCustomPropertiesAndChangeScene()
        {
            float timeout = 15f;
            float elapsed = 0f;
            bool allPlayersSet = false;

            while (!allPlayersSet && elapsed < timeout)
            {
                allPlayersSet = PhotonNetwork.PlayerList.All(player =>
                    player.CustomProperties.ContainsKey("selectedHero") &&
                    player.CustomProperties["selectedHero"] != null &&
                    !string.IsNullOrEmpty(player.CustomProperties["selectedHero"].ToString())
                );

                //Debug.Log("Waiting for all players to set their heroes...");
                yield return new WaitForSeconds(0.2f);
                elapsed += 0.2f;
            }

            if (PhotonNetwork.IsMasterClient && !isSceneLoading) // ให้ MasterClient โหลด และป้องกันโหลดซ้ำ
            {
                isSceneLoading = true;
                //Debug.Log("All players have selected heroes. Proceeding to map3...");
                PhotonNetwork.LoadLevel("map3");
                
            }
        }


        [PunRPC]
        private void RPC_AssignRandomHero(int actorNumber)
        {
            Player player = PhotonNetwork.CurrentRoom.Players[actorNumber];
            Team playerTeam = TeamManager.instance.GetTeam(player);

            // ดึงเฉพาะฮีโร่ที่ทีมนี้ยังไม่ใช้
            List<string> availableHeroes = heroLocksPerTeam[playerTeam]
                .Where(h => !h.Value)
                .Select(h => h.Key)
                .ToList();

            if (availableHeroes.Count > 0)
            {
                string randomHero = availableHeroes[Random.Range(0, availableHeroes.Count)];
                heroLocksPerTeam[playerTeam][randomHero] = true; // ล็อกในทีมนี้

                playerSelectedHeroes[actorNumber] = randomHero;

                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
                {
                    { "selectedHero", randomHero }
                };
                player.SetCustomProperties(props);

                int teamValue = (int)playerTeam;
                heroSelectionManager.photonView.RPC("RPC_LockHero", RpcTarget.All, randomHero, true, teamValue);
                photonView.RPC("RPC_UpdateHeroEntry", RpcTarget.All, actorNumber, randomHero);
            }
            else
            {
                Debug.LogWarning($" No available heroes to assign for team {playerTeam}");
            }
        }

        private Hero FindHeroByName(string heroName)
        {
            Hero hero = HeroList.transform.Find(heroName)?.GetComponent<Hero>();
            if (hero == null)
            {
                hero = LoadHeroPrefab(heroName);
            }
            return hero;
        }
        private Hero LoadHeroPrefab(string heroName)
        {
            Hero heroPrefab = Resources.Load<Hero>($"Heroes/{heroName}");
            if (heroPrefab == null)
            {
                Debug.LogError($"Hero prefab with name {heroName} not found.");
                return null;
            }
            return heroPrefab;
        }


        public void ConfirmHeroSelection()
        {
            int playerId = PhotonNetwork.LocalPlayer.ActorNumber;
            if (playerSelectedHeroes.ContainsKey(playerId))
            {
                selectionConfirmed = true;
                //Debug.Log("Hero selection confirmed: " + playerSelectedHeroes[playerId]);
            }
            else
            {
                Debug.LogWarning("Please select a hero before confirming.");
            }
        }

        #endregion

}