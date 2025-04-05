using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class KillCanvasPopup : MonoBehaviourPun
{
    public GameObject killPanelPrefab; // ต้องเป็น prefab ที่มี KillCanvas component

    public Transform popupParent; // ตำแหน่ง parent บน canvas ที่จะใส่ popup
    public Vector3 popupOffset = new Vector3(0f, -163f, 0f); // ขยับ popup ลงเล็กน้อย

    public static KillCanvasPopup instance;

    private void Awake()
    {
        instance = this;
    }

    public void CreateKillPopup(string killerSpriteName, string deceasedSpriteName)
    {
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_CreateKillPopup", RpcTarget.All, killerSpriteName, deceasedSpriteName);
        }
        else
        {
            Sprite killerImg = Resources.Load<Sprite>("HeroImage/" + killerSpriteName);
            Sprite deceasedImg = Resources.Load<Sprite>("HeroImage/" + deceasedSpriteName);

            InstantiatePopup(killerImg, deceasedImg);
        }
    }

    [PunRPC]
    private void RPC_CreateKillPopup(string killerSpriteName, string deceasedSpriteName)
    {
        // โหลด sprite จาก Resources
        Sprite killerImg = Resources.Load<Sprite>("HeroImage/" + killerSpriteName);
        Sprite deceasedImg = Resources.Load<Sprite>("HeroImage/" + deceasedSpriteName);

        InstantiatePopup(killerImg, deceasedImg);
    }

    private void InstantiatePopup(Sprite killerImg, Sprite deceasedImg)
    {
        if (killPanelPrefab == null)
        {
            Debug.LogError("killPanelPrefab has not been assigned!");
            return;
        }

        GameObject popupGO = Instantiate(killPanelPrefab, popupParent);
        popupGO.transform.localPosition = popupOffset;

        KillCanvas canvas = popupGO.GetComponent<KillCanvas>();
        if (canvas != null)
        {
            canvas.killerPlaceImage.sprite = killerImg;
            canvas.deceasedPlaceImage.sprite = deceasedImg;
        }

        Destroy(popupGO, 5f);
    }
}
