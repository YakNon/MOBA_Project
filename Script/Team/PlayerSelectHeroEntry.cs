using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExitGames.Client.Photon;
using Photon.Realtime;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;

public class PlayerSelectHeroEntry : MonoBehaviour
{
    public Image heroImage;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI heroNameText;
    private int ownerId;
    void Start()
    {
        
    }

    void Update()
    {
        
    }
    public void Initialize(int playerId, string playerName)
    {
        ownerId = playerId;
        playerNameText.text = $"{playerName}";
    }
}
