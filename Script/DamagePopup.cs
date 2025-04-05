using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;

public class DamagePopup : MonoBehaviourPun
{
    public static DamagePopup current;
    public GameObject prefab;

    private void Awake()
    {
        current = this;
    }
    public static Color GetColor(DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Physical => Color.red,        // ดาเมจกายภาพ
            DamageType.Magic => new Color(0.5f, 0f, 1f), // ดาเมจเวท
            DamageType.TrueDamage => Color.white,   // ดาเมจจริง
            _ => Color.grey                         // ดาเมจที่ไม่รู้จัก
        };
    }

    public void CreatPopUp(Vector3 position, string text, DamageType damageType, IDamageable attacker, IDamageable target)
    {
        Color color = GetColor(damageType);
        Vector3 colorVec = new Vector3(color.r, color.g, color.b);

        // ส่ง RPC เฉพาะผู้เล่นที่เกี่ยวข้อง
        if (attacker is Hero attackerHero){
            photonView.RPC("RPC_CreatePopup", attackerHero.ownerPlayer, position, text, colorVec);
        }
        if  (target is Hero targetHero){
            photonView.RPC("RPC_CreatePopup", targetHero.ownerPlayer, position, text, colorVec);
        }
    }
    // Overload สำหรับดาเมจ
    public void CreatPopUp(Vector3 position, string text, DamageType damageType)
    {
        Color color = GetColor(damageType);
        //string text = $"-{amount:F0}"; // แสดงเป็นเลขติดลบ

        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_CreatePopup", RpcTarget.All, position, text, new Vector3(color.r, color.g, color.b));
        }
        else
        {
            InstantiatePopup(position, text, color);
        }
    }

    public void CreatPopUp(Vector3 position, string text, Color color)
    {
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_CreatePopup", RpcTarget.All, position, text, new Vector3(color.r, color.g, color.b));
        }
        else
        {
            InstantiatePopup(position, text, color);
        }
    }



    [PunRPC]
    private void RPC_CreatePopup(Vector3 position, string text, Vector3 colorVector)
    {
        Color color = new Color(colorVector.x, colorVector.y, colorVector.z);
        InstantiatePopup(position, text, color);
    }

    private void InstantiatePopup(Vector3 position, string text, Color color)
    {
        if (prefab == null)
        {
            Debug.LogError("Prefab has not been assigned!");
            return;
        }

        var popup = Instantiate(prefab, position, Quaternion.identity);
        var temp = popup.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        temp.text = text;
        temp.faceColor = color;

        Destroy(popup, 1f);
    }
}
