using System.Collections;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class RespawnPanelUI : MonoBehaviourPun
{
    public static RespawnPanelUI instance;

    public GameObject respawnPanel;
    public TextMeshProUGUI countdownText;

    private void Awake()
    {
        instance = this;
        respawnPanel.SetActive(false);
    }

    public void ShowRespawn(float respawnTime)
    {
        StartCoroutine(RespawnCountdown(respawnTime));
    }

    private IEnumerator RespawnCountdown(float time)
    {
        respawnPanel.SetActive(true);

        float timer = time;
        while (timer > 0)
        {
            countdownText.text = $"Respawning in {Mathf.Ceil(timer)}";
            yield return new WaitForSeconds(1f);
            timer -= 1f;
        }

        respawnPanel.SetActive(false);
    }
}

