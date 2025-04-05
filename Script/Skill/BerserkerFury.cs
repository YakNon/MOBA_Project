using System.Collections;
using UnityEngine;
using Photon.Pun;

[CreateAssetMenu(fileName = "New BerserkerFury", menuName = "Skills/BerserkerFury")]
public class BerserkerFury : Skill
{
    public float percentDamageIncrease = 0.5f; // เพิ่มความเสียหายเป็นเปอร์เซ็นต์
    public GameObject skillEffectPrefab;
    public float duration = 5f; // ระยะเวลาของบัฟ

    public override void Activate(Hero myHero)
    {
        if (myHero == null || myHero.isDead) return; // ป้องกันการใช้สกิลกับฮีโร่ที่ตาย
        myHero.StartCoroutine(ApplyBerserkerFury(myHero));
    }

    private IEnumerator ApplyBerserkerFury(Hero myHero)
    {
        float originalDamage = myHero.attackDamage; // บันทึกค่าความเสียหายเดิม
        myHero.attackDamage += originalDamage * percentDamageIncrease; // เพิ่มพลังโจมตี

        PhotonView photonView = myHero.photonView; //  ดึง PhotonView ของ Hero
        if (photonView != null)
        {
            string effectName = skillEffectPrefab.name; // ส่งชื่อของ Prefab
            photonView.RPC("RPC_CreateSkillEffect", RpcTarget.All, effectName, myHero.transform.position, 2f);
        }
        yield return new WaitForSeconds(duration);

        //  รีเซ็ตค่าความเสียหายกลับไปเป็นปกติ
        myHero.attackDamage = originalDamage;
    }
}
