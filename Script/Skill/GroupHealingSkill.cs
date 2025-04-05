using System.Collections;
using UnityEngine;
using Photon.Pun;

[CreateAssetMenu(fileName = "NewGroupHealingSkill", menuName = "Skills/Group Healing")]
public class GroupHealingSkill : Skill
{
    public float healAmount = 200f; // ปริมาณการฮีลต่อครั้ง
    public int healCount = 6; // จำนวนรอบที่ฮีล
    public float healInterval = 0.5f; // เวลาระหว่างการฮีลแต่ละครั้ง
    public float healRange = 8f; // ระยะการฮีล
    public string healEffectPrefabName = "MC Healing circle";

    public override void Activate(Hero myHero)
    {
        if (!myHero.photonView.IsMine) return;

        myHero.StartCoroutine(HealAllies(myHero));
    }

    private IEnumerator HealAllies(Hero myHero)
    {
        PhotonView photonView = myHero.photonView;

        // ✅ แสดงเอฟเฟคฮีลที่ตัวเอง
        if (!string.IsNullOrEmpty(healEffectPrefabName))
        {
            photonView.RPC("RPC_CreateSkillEffectChild", RpcTarget.All, healEffectPrefabName, myHero.photonView.ViewID, healInterval * healCount, healRange);
        }

        for (int i = 0; i < healCount; i++)
        {
            Collider[] hitColliders = Physics.OverlapSphere(myHero.transform.position, healRange);
            foreach (Collider collider in hitColliders)
            {
                Hero allyHero = collider.GetComponent<Hero>();
                if (allyHero != null && myHero.IsOnSameTeamAs(allyHero))
                {
                    // ✅ ส่งคำสั่งฮีลให้ทุกเครื่องเห็น (ไม่ต้องแก้ `Hero.cs`)
                    allyHero.photonView.RPC("RPC_Heal", RpcTarget.All, healAmount);
                }
            }
            yield return new WaitForSeconds(healInterval);
        }
    }
}
