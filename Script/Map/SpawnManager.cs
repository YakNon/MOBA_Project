using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class SpawnManager : MonoBehaviourPunCallbacks
{
    public List<Transform> redTeamSpawnPoints;
    public List<Transform> blueTeamSpawnPoints;
    public List<NPC> npcPrefabs;
    public List<Transform> spawnPointnpc; // เพิ่มรายการสำหรับตำแหน่งการเกิดของ NPC
    private HeroSelectionManager heroSelectionManager;

    private Dictionary<Transform, float> npcSpawnTimers = new Dictionary<Transform, float>();


    private IEnumerator WaitForPlayerHeroSelection()
    {
        bool allHeroesSet = false;

        while (!allHeroesSet)
        {
            allHeroesSet = PhotonNetwork.PlayerList.All(player =>
                player.CustomProperties.ContainsKey("selectedHero") &&
                player.CustomProperties["selectedHero"] != null &&
                !string.IsNullOrEmpty(player.CustomProperties["selectedHero"].ToString())
            );

            Debug.Log("Waiting for all players to have selected heroes...");
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log("All players have selected heroes. Assigning spawn points...");
        AssignSpawnPoints();
        if (PhotonNetwork.IsMasterClient)
        {
            SpawnNPC(); // สร้าง NPC ครั้เดียว
        }
        //เริ่มระบบเกิด NPC เมื่อแน่ใจว่าฮีโร่ของผู้เล่นทุกคนถูก Spawn แล้ว
        //StartCoroutine(CheckAndRespawnNPCs());
    }

    private void Start()
    {
        heroSelectionManager = FindObjectOfType<HeroSelectionManager>(); 
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(WaitForPlayerHeroSelection());
        }
    }

    private IEnumerator WaitForCustomPropertiesAndAssignSpawnPoints()
    {
        bool allPlayersReady = false;

        while (!allPlayersReady)
        {
            allPlayersReady = PhotonNetwork.PlayerList.All(player => player.CustomProperties.ContainsKey("selectedHero") && player.CustomProperties["selectedHero"] != null);
            yield return new WaitForSeconds(0.1f); // รอทุก 0.1 วินาที
        }

        AssignSpawnPoints();
    }

    private void AssignSpawnPoints()
    {
        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey("team"))
            {
                Debug.LogError($"Player {player.NickName} has no team assigned in CustomProperties.");
                continue;
            }

            Team playerTeam = (Team)(int)player.CustomProperties["team"]; // ใช้ค่า team จาก CustomProperties
            string selectedHeroName = player.CustomProperties.ContainsKey("selectedHero") ? player.CustomProperties["selectedHero"] as string : null;

            if (string.IsNullOrEmpty(selectedHeroName))
            {
                Debug.LogError($"Selected hero name is null for player: {player.NickName}");
                continue;
            }

            GameObject heroPrefab = heroSelectionManager.allHeroPrefabs.Find(hero => hero.name == selectedHeroName)?.gameObject;
            if (heroPrefab == null)
            {
                Debug.LogError($"Hero prefab not found for selected hero: {selectedHeroName}");
                continue;
            }

            Transform spawnPoint = (playerTeam == Team.Blue) ? GetRandomSpawnPoint(blueTeamSpawnPoints) : GetRandomSpawnPoint(redTeamSpawnPoints);

            if (spawnPoint != null && heroPrefab != null)
            {
                //Debug.Log($"Spawning {selectedHeroName} at {spawnPoint.position} for {player.NickName} (Team: {playerTeam})");
                //photonView.RPC("RPC_SpawnHero", RpcTarget.AllBuffered, heroPrefab.name, spawnPoint.position, spawnPoint.rotation, player.ActorNumber);
                photonView.RPC("RPC_SpawnHero", player, heroPrefab.name, spawnPoint.position, spawnPoint.rotation);
                //photonView.RPC("RPC_SpawnHero", player, heroPrefab.name, spawnPoint.position, spawnPoint.rotation);
            }
        }
    }
    // RPC ที่จะทำงานบนเครื่องของ player เท่านั้น
[PunRPC]
private void RPC_SpawnHero(string heroPrefabName, Vector3 position, Quaternion rotation)
{
    GameObject heroInstance = PhotonNetwork.Instantiate($"Heroes/{heroPrefabName}", position, rotation);
    Hero heroScript = heroInstance.GetComponent<Hero>();
    if (heroScript != null)
    {
        heroScript.SetInitialSpawnPoint(position);
    }
}

    private Transform GetRandomSpawnPoint(List<Transform> spawnPoints)
    {
        if (spawnPoints.Count == 0) return null;
        int randomIndex = Random.Range(0, spawnPoints.Count);
        return spawnPoints[randomIndex];
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.TryGetValue("selectedHero", out object selectedHeroName))
                {
                    string heroName = selectedHeroName as string;
                    Team playerTeam = TeamManager.instance.GetTeam(player);
                    Transform spawnPoint = playerTeam == Team.Red ? GetRandomSpawnPoint(redTeamSpawnPoints) : GetRandomSpawnPoint(blueTeamSpawnPoints);

                    photonView.RPC("RPC_SpawnHero", player, heroName, spawnPoint.position, spawnPoint.rotation);
                }
            }
        }
    }

    private void SpawnNPC()
    {
        if (spawnPointnpc.Count != npcPrefabs.Count)
        {
            Debug.LogError("spawnPointnpc and npcPrefabs lists must have the same size!");
            return;
        }

        for (int i = 0; i < spawnPointnpc.Count; i++)
        {
            Transform spawnPoint = spawnPointnpc[i];
            NPC npcPrefab = npcPrefabs[i];

            GameObject npcInstance = PhotonNetwork.Instantiate($"NPC/{npcPrefab.name}", spawnPoint.position, spawnPoint.rotation);

            NPC npcScript = npcInstance.GetComponent<NPC>();
            if (npcScript != null)
            {
                npcScript.spawnPoint = spawnPoint.position;
                npcScript.name = $"{npcPrefab.name}_{i}";
                npcScript.spawnTime = 100f;
            }
        }
    }

}
