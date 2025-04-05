using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public enum Team
{

    Red,
    Blue,
    None
}

public class TeamManager : MonoBehaviourPunCallbacks
{
    public static TeamManager instance;
    public Dictionary<Player, Team> playerTeams = new Dictionary<Player, Team>();
    public Dictionary<Player, Hero> playerHeroes = new Dictionary<Player, Hero>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // ป้องกันไม่ให้ถูกทำลายเมื่อเปลี่ยน Scene
            Debug.Log("TeamManager instance created successfully.");
        }
        else
        {
            Destroy(gameObject); 
        }
    }

    public void AssignTeam(Player player, Team team)
    {
        if (player == null) 
        {
            Debug.LogError("Player is null");
            return;
        }

        if (!playerTeams.ContainsKey(player))
        {
            playerTeams.Add(player, team);
        }
        else
        {
            playerTeams[player] = team; // อัปเดตค่าใหม่ถ้ามีอยู่แล้ว
        }

        //  บันทึกค่าทีมลงใน Photon CustomProperties เพื่อให้ซิงค์กับทุกเครื่อง
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { "team", (int)team }
        };
        player.SetCustomProperties(props);

        Debug.Log($" Assigned {player.NickName} to {team}");
    }
[PunRPC]
public void RPC_AssignHero(int actorNumber, string heroName)
{
    Player player = PhotonNetwork.CurrentRoom.Players[actorNumber];
    if (player == null) return;

    Hero heroPrefab = Resources.Load<Hero>($"Heroes/{heroName}");
    if (heroPrefab == null) return;

    AssignHero(player, heroPrefab); // ใช้ method ที่มีอยู่แล้ว
}

public void AssignHero(Player player, Hero hero)
{
    if (player == null || hero == null) return;

    playerHeroes[player] = hero;
    Debug.Log($"-----Assigned hero {hero.name} to player {player.NickName}");
}

    // public void AssignHero(Player player, Hero hero)
    // {
    //     if (!playerHeroes.ContainsKey(player))
    //     {
    //         playerHeroes[player] = hero;
    //         Debug.Log($"-----Assigned hero {hero.name} to player {player.NickName}");
    //     }
    // }


    // ฟังก์ชัน RPC เพื่อให้ Client อื่น ๆ รับข้อมูล
    [PunRPC]
    void SyncTeam(int playerID, int team)
    {
        Player player = PhotonNetwork.CurrentRoom.Players[playerID];
        if (player != null)
        {
            playerTeams[player] = (Team)team;
        }
    }
    void SyncHero(int playerID, int heroID)
    {
        Player player = PhotonNetwork.CurrentRoom.Players[playerID];
        Hero hero = PhotonView.Find(heroID).GetComponent<Hero>();
        if (player != null && hero != null)
        {
            playerHeroes[player] = hero;
        }
    }
    public Team GetTeam(Player player)
    {
        if (player == null)
        {
            Debug.LogError(" GetTeam() received a null player!");
            return Team.Red; // ค่าเริ่มต้นหากไม่มีข้อมูล
        }

        //  ลองอ่านค่าจาก Dictionary `playerTeams` ก่อน
        if (playerTeams.ContainsKey(player))
        {
            return playerTeams[player];
        }

        //  ถ้าไม่มีข้อมูลใน Dictionary ให้ลองดึงจาก CustomProperties ของ Photon
        if (player.CustomProperties.ContainsKey("team"))
        {
            Team assignedTeam = (Team)(int)player.CustomProperties["team"];
            playerTeams[player] = assignedTeam; // อัปเดต Dictionary ให้ซิงค์กัน
            Debug.Log($"Loaded {player.NickName}'s team from CustomProperties: {assignedTeam}");
            return assignedTeam;
        }

        Debug.LogWarning($"Player {player.NickName} does not have a team assigned. Returning default team.");
        return Team.Red; // ค่าเริ่มต้นถ้ายังไม่มีทีม
    }


    public bool AreOnSameTeam(Player player1, Player player2)
    {
        return GetTeam(player1) == GetTeam(player2);
    }
    public bool IsHeroAlreadySelectedByTeam(string heroName, Player currentPlayer)
    {
        if (playerHeroes == null || currentPlayer == null)
        {
            Debug.LogError("playerHeroes or currentPlayer is null.");
            return false;
        }

        // ตรวจสอบว่าตัวละครนี้ถูกใช้โดยผู้เล่นในทีมเดียวกันหรือไม่
        foreach (var player in playerHeroes.Keys)
        {
            if (AreOnSameTeam(player, currentPlayer) && playerHeroes[player].name == heroName)
            {
                return true; // ตัวละครนี้ถูกใช้แล้วโดยผู้เล่นในทีมเดียวกัน
            }
        }

        return false; // ตัวละครนี้ยังไม่ถูกใช้
    }

    //for summary scene
    public Team GetTeamByPlayerName(string playerName)
    {
        foreach (var kvp in playerTeams)
        {
            if (kvp.Key.NickName == playerName)
            {
                return kvp.Value;
            }
        }
        return Team.None;
    }

}
