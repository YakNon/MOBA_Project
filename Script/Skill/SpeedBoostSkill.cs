using System.Collections;
using UnityEngine;
using Photon.Pun;

[CreateAssetMenu(fileName = "SpeedBoostSkill", menuName = "Skills/Speed Boost")]
public class SpeedBoostSkill : Skill
{
    public float speedMultiplier = 2.0f; // เพิ่มความเร็ว 200% (x2)
    public float duration = 2.0f; // ใช้ได้เป็นเวลา 2 วินาที
    //public string speedEffectName = "SpeedEffect"; // ชื่อเอฟเฟคใน Resources/PhotonEffects/
    public GameObject skillEffectPrefab;

    public override void Activate(Hero myHero)
    {
        Debug.Log($" {myHero.name} กำลังใช้สกิล {skillName} (เพิ่มความเร็ว {speedMultiplier} เท่า เป็นเวลา {duration} วินาที)");

        if (myHero != null)
        {
            myHero.StartCoroutine(ApplySpeedBoost(myHero));
        }
    }

    private IEnumerator ApplySpeedBoost(Hero hero)
    {

        if(skillEffectPrefab != null){
            hero.photonView.RPC("RPC_CreateSkillEffect", RpcTarget.All, skillEffectPrefab.name, hero.transform.position, duration);
        }
       

        // เพิ่มความเร็ว
        float originalSpeed = hero.speed;
        hero.speed *= speedMultiplier;
        //Debug.Log($" {hero.name} Speed Boosted! (New Speed: {hero.speed})");

        yield return new WaitForSeconds(duration);

        // คืนค่าความเร็วเดิม
        hero.speed = originalSpeed;
        //Debug.Log($" {hero.name} Speed Boost Ended! (Speed Reset: {hero.speed})");
    }
}
