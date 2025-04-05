using System.Collections;
using UnityEngine;
using Photon.Pun;

[CreateAssetMenu(fileName = "NewHealingSkill", menuName = "Skills/Healing")]
public class HealingSkill : Skill
{
    public float healAmount = 200f; // จำนวนที่ฮีลต่อครั้ง
    public int healCount = 1; // จำนวนครั้งที่ต้องการให้ฮีล
    public float healInterval = 1f; // ระยะเวลาห่างระหว่างการฮีลแต่ละครั้ง
    public GameObject healEffectPrefab; // เอฟเฟคตอนฮีล

    public override void Activate(Hero myHero)
    {
        PhotonView photonView = myHero.photonView;
        if (photonView == null) return;

        myHero.StartCoroutine(HealOverTime(myHero, photonView));
    }

    private IEnumerator HealOverTime(Hero myHero, PhotonView photonView)
    {
        if (healEffectPrefab != null)
        {
            photonView.RPC("RPC_CreateSkillEffectChild", RpcTarget.All, healEffectPrefab.name, myHero.photonView.ViewID, healInterval * healCount);
        }

        for (int i = 0; i < healCount; i++)
        {
            if (myHero.isDead) yield break;

            myHero.Heal(healAmount);

            yield return new WaitForSeconds(healInterval);
        }
    }
}
