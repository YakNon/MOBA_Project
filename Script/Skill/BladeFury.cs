using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; //  ใช้ Photon RPC

[CreateAssetMenu(fileName = "New BladeFury", menuName = "Skills/Blade Fury")]
public class BladeFury : Skill
{
    public float basicDamage = 200f;
    public float damageIncrease = 2f;
    public float skillRange = 5f; // ระยะสกิล
    public int hitCount = 1; //  จำนวนครั้งที่ทำดาเมจ
    public float hitInterval = 1f; //  ระยะห่างระหว่างแต่ละฮิต
    public GameObject skillEffectPrefab;    
    public GameObject hitEffectPrefab;  
    public DamageType damageType = DamageType.Physical;
    public bool isStun = false;
    public float stunDuration = 0f;
    public override void Activate(Hero myHero)
    {
        PhotonView photonView = myHero.photonView; //  ดึง PhotonView ของ Hero
        if (photonView == null) return;

        myHero.StartCoroutine(PerformHits(myHero, photonView));
    }

    private IEnumerator PerformHits(Hero myHero, PhotonView photonView)
    {
        bool effectSpawned = false; //  เช็คว่าเอฟเฟคสกิลถูกสร้างไปแล้วหรือยัง
        float damage = basicDamage;
        if (damageType == DamageType.Physical || damageType == DamageType.TrueDamage )
        {
            damage += myHero.attackDamage * damageIncrease;
        }
        else{
            damage += myHero.magic * damageIncrease;
        }
        for (int i = 0; i < hitCount; i++)
        {
            if (!effectSpawned && skillEffectPrefab != null)
            {
                string effectName = skillEffectPrefab.name;
                photonView.RPC("RPC_CreateSkillEffect", RpcTarget.All, effectName, myHero.transform.position, 1.5f);
                effectSpawned = true; //  สร้างเอฟเฟคแค่ครั้งแรก
            }

            Collider[] hitColliders = Physics.OverlapSphere(myHero.transform.position, skillRange);
            foreach (Collider collider in hitColliders)
            {
                Hero enemyHero = collider.GetComponent<Hero>();
                NPC enemyNPC = collider.GetComponent<NPC>();
                Minion enemyMinion = collider.GetComponent<Minion>();

                if (enemyHero != null && !myHero.IsOnSameTeamAs(enemyHero)) 
                {
                    ApplyDamage(myHero, enemyHero, photonView, damage);
                }
                else if (enemyNPC != null)
                {
                    ApplyDamage(myHero, enemyNPC, photonView, damage);
                }
                else if (enemyMinion != null && enemyMinion.minionTeam != TeamManager.instance.GetTeam(myHero.photonView.Owner))
                {
                    ApplyDamage(myHero, enemyMinion, photonView, damage);
                }
            }

            if (i < hitCount - 1) //  รอระยะเวลาที่กำหนดก่อนทำดาเมจครั้งต่อไป
            {
                yield return new WaitForSeconds(hitInterval);
            }
        }
    }

    private void ApplyDamage(Hero myHero, IDamageable target, PhotonView photonView, float damage)
    {
        //Debug.Log($"{myHero.name} uses {skillName} on {target} dealing {damage} damage.");

        if (hitEffectPrefab != null)
        {
            string effectName = hitEffectPrefab.name; //  ส่งชื่อของ Prefab
            photonView.RPC("RPC_CreateHitEffect", RpcTarget.All, effectName, ((MonoBehaviour)target).transform.position, 1.5f);
        }
        if (isStun && target is Hero targetHero)
        {
            targetHero.photonView.RPC("RPC_Stun", RpcTarget.All, stunDuration);
        }
        target.TakeDamage(damage, myHero, damageType);
    }

}
