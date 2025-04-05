using System.Collections;
using UnityEngine;
using Photon.Pun;

[CreateAssetMenu(fileName = "MaxHealthDamageSkill", menuName = "Skills/Max Health Damage")]
public class MaxHealthDamageSkill : Skill
{
    public GameObject skillEffectPrefab;
    public GameObject hitEffectPrefab;
    public float damagePercentage = 0.5f; // 50% ของ Max Health
    public float skillRange = 5.0f; // ระยะของสกิล
    public bool isStun = false;
    public float stunDuration = 0f;
    public DamageType damageType = DamageType.Magic;

    public override void Activate(Hero myHero)
    {
        //Debug.Log($" {myHero.name} ใช้สกิล {skillName} สร้างดาเมจ {damagePercentage * 100}% ของ Max HP!");

        // แสดงเอฟเฟคสกิลที่ตัวเอง
        if (skillEffectPrefab != null)
        {
            myHero.photonView.RPC("RPC_CreateSkillEffect", RpcTarget.All, skillEffectPrefab.name, myHero.transform.position,1.0f);
        }

        // ค้นหาเป้าหมายในระยะสกิล
        Collider[] hitColliders = Physics.OverlapSphere(myHero.transform.position, skillRange);

        foreach (Collider collider in hitColliders)
        {
            IDamageable target = collider.GetComponent<IDamageable>();

            if (target is Hero targetHero)
            {
                if (!myHero.IsOnSameTeamAs(targetHero) && !targetHero.isDead)
                {
                    ApplyDamage(targetHero, myHero);
                }
            }
            else if (target is Minion targetMinion)
            {
                if (targetMinion.minionTeam != myHero.Team)
                {
                    ApplyDamage(targetMinion, myHero);
                }
            }
            else if (target is NPC targetNPC)
            {
                ApplyDamage(targetNPC, myHero);
            }
            else if (target is Tower targetTower)
            {
                if (targetTower.towerTeam != myHero.Team)
                {
                    ApplyDamage(targetTower, myHero);
                }
            }
        }
    }

    private void ApplyDamage(IDamageable target, Hero attacker)
    {
        float damageAmount = attacker.maxHealth * damagePercentage;
       // Debug.Log($" {attacker.name} โจมตี {target} ด้วยดาเมจ {damageAmount} HP!");

        // ทำดาเมจ & stun
        if (isStun && target is Hero targetHero)
        {
            targetHero.photonView.RPC("RPC_Stun", RpcTarget.All, stunDuration);
        }
        target.TakeDamage(damageAmount, attacker, damageType);
        if(hitEffectPrefab != null)
        {
            attacker.photonView.RPC("RPC_CreateHitEffect", RpcTarget.All, hitEffectPrefab.name, target.transform.position,1.0f);
        }
        
    }
}
