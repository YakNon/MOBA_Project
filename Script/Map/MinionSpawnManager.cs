using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class MinionSpawnManager : MonoBehaviourPunCallbacks
{
    public List<Transform> redMinionSpawnPoints;
    public List<Transform> blueMinionSpawnPoints;
    public GameObject minionPrefab;

    // Waypoints สำหรับแต่ละเลนของทีมแดงและทีมน้ำเงิน
    public List<WaypointGroup> redTeamWaypoints;
    public List<WaypointGroup> blueTeamWaypoints;

    public float spawnInterval = 30f;

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(SpawnMinions());
        }
    }

    IEnumerator SpawnMinions()
    {
        while (true)
        {
            // Spawn มินเนี่ยนทีมแดง
            for (int i = 0; i < redMinionSpawnPoints.Count; i++)
            {
                Transform[] waypoints = redTeamWaypoints[i].waypoints.ToArray();
                SpawnMinion(redMinionSpawnPoints[i], Team.Red, waypoints);
            }

            // Spawn มินเนี่ยนทีมน้ำเงิน
            for (int i = 0; i < blueMinionSpawnPoints.Count; i++)
            {
                Transform[] waypoints = blueTeamWaypoints[i].waypoints.ToArray();
                SpawnMinion(blueMinionSpawnPoints[i], Team.Blue, waypoints);
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnMinion(Transform spawnPoint, Team team, Transform[] waypoints)
    {
        //GameObject minionInstance = PhotonNetwork.Instantiate($"Minion/{minionPrefab.name}", spawnPoint.position, spawnPoint.rotation);
        object[] instantiationData = new object[] { (int)team };
        GameObject minionInstance = PhotonNetwork.Instantiate($"Minion/{minionPrefab.name}", spawnPoint.position, spawnPoint.rotation, 0, instantiationData);

        Minion minion = minionInstance.GetComponent<Minion>();

        if (minion != null)
        {
            //minion.photonView.RPC("RPC_SetMinionTeam", RpcTarget.AllBuffered, (int)team);
            minion.waypoints = waypoints;

        }
    }


    IEnumerator CheckMinionTeamLater(Minion minion)
    {
        yield return new WaitForSeconds(1f); // รอ 1 วินาทีเพื่อให้แน่ใจว่า Start() ทำงานแล้ว
        Debug.Log($" After 1s, Minion {minion.name} has Team: {minion.minionTeam}");
    }
}
