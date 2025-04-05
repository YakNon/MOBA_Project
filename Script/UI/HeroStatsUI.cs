using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class HeroStatsUI : MonoBehaviour
{
    public TextMeshProUGUI killText;
    public TextMeshProUGUI deathText;
    public TextMeshProUGUI assistText;
    public TextMeshProUGUI pingText; // ✅ เพิ่ม UI สำหรับ Ping

    private Hero hero;

    IEnumerator Start()
    {
        while (hero == null)
        {
            foreach (Hero h in FindObjectsOfType<Hero>())
            {
                if (h.photonView != null && h.photonView.IsMine)
                {
                    hero = h;
                    break;
                }
            }

            yield return null; // รอ frame ถัดไป
        }

        UpdateStatsUI();
        StartCoroutine(UpdatePingRoutine()); // ✅ เริ่มอัปเดต Ping
    }


    void Update()
    {
        if (hero != null)
        {
            UpdateStatsUI();
        }
    }

    public void ForceUpdate()
    {
        UpdateStatsUI();
    }

    public void SetHero(Hero myHero)
    {
        hero = myHero;
        UpdateStatsUI();
    }

    public void UpdateStatsUI()
    {
        if (hero != null)
        {
            killText.text = $"{hero.killCount}";
            deathText.text = $"{hero.deathCount}";
            assistText.text = $"{hero.assistCount}";
        }
    }
    private IEnumerator UpdatePingRoutine()
    {
        while (true)
        {
            UpdatePingUI();
            yield return new WaitForSeconds(1f); // อัปเดตทุก 1 วินาที
        }
    }

    private void UpdatePingUI()
    {
        int ping = PhotonNetwork.GetPing();
        pingText.text = $"{ping} ms";

        if (ping <= 100)
        {
            pingText.color = new Color(0f, 207f / 255f, 40f / 255f); // เขียว
        }
        else if (ping <= 200)
        {
            pingText.color = new Color(1f, 207f / 255f, 0f); // เหลือง
        }
        else
        {
            pingText.color = new Color(207f / 255f, 0f, 10f / 255f); // แดง
        }
    }
}
