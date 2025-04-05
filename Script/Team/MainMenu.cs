using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.SceneManagement; // เพิ่มการนำเข้าที่นี่

public class MainMenu : MonoBehaviourPunCallbacks
{
    public InputField playerNameInput; // ช่องกรอกชื่อ
    public Button startButton; // ปุ่ม Start

    private void Start()
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
    }

    void OnStartButtonClicked()
    {
        string playerName = playerNameInput.text;
        if (!string.IsNullOrEmpty(playerName))
        {
            PhotonNetwork.NickName = playerName; // ตั้งชื่อผู้เล่นใน Photon
            PhotonNetwork.ConnectUsingSettings(); // เชื่อมต่อไปยัง Photon Server
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master Server");
        PhotonNetwork.JoinLobby(); // เข้าสู่ Lobby
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
        //SceneManager.LoadScene("Lobby"); // เปลี่ยน "LobbyScene" เป็นชื่อฉากของคุณ
    }
}
