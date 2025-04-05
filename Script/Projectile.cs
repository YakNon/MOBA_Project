using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Projectile : MonoBehaviourPun
{
    public float speed = 5.0f;
    private float damage;
    private float magicP;
    private float truedamageP;
    private IDamageable target;
    private Hero attacker;

    public void SetDamage(float damageAmount)
    {
        damage = damageAmount;
    }
    public void SetMagic(float damageAmount)
    {
        magicP = damageAmount;
    }
    public void SetTrueDamage(float damageAmount)
    {
        truedamageP = damageAmount;
    }

    public void SetTarget(IDamageable Target)
    {
        target = Target;
    }

    public void SetAttacker(Hero attacker)
    {
        this.attacker = attacker;
    }

    void Update()
    {
        if (!photonView.IsMine) return; // ให้เฉพาะเจ้าของ Object ควบคุม

        // ถ้าเป้าหมายถูกทำลายก่อนที่กระสุนจะถึง ➝ ทำลายตัวเอง
        if (!IsTargetValid(target))
        {
            PhotonNetwork.Destroy(gameObject);
            return;
        }

        // ตรวจสอบว่าเป้าหมายเป็นศัตรูหรือไม่
        if (target is Hero targetHero && attacker.IsOnSameTeamAs(targetHero))
        {
            PhotonNetwork.Destroy(gameObject);
            return;
        }
        else if (target is Minion targetMinion && attacker.IsOnSameTeamAs(targetMinion))
        {
            PhotonNetwork.Destroy(gameObject);
            return;
        }
        else if (target is Tower targetTower && attacker.IsOnSameTeamAs(targetTower))
        {
            PhotonNetwork.Destroy(gameObject);
            return;
        }

        // เคลื่อนที่ไปหาเป้าหมาย
        Vector3 direction = (target.transform.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.transform.position) < 0.5f)
        {
            HitTarget();
        }
    }

    void HitTarget()
    {
        if (IsTargetValid(target))
        {
            if(attacker.isAttackinPhysical) target.TakeDamage(damage, attacker, DamageType.Physical);
            if(attacker.isAttackinMagic) target.TakeDamage(magicP, attacker, DamageType.Magic);
            if(attacker.isAttackinTrueDamage) target.TakeDamage(truedamageP, attacker, DamageType.TrueDamage);
            
        }
        PhotonNetwork.Destroy(gameObject);
    }

    private bool IsTargetValid(IDamageable checkTarget)
    {
        if (checkTarget == null) return false; // ป้องกัน NullReferenceException

        if (checkTarget is Hero heroTarget) return heroTarget != null && heroTarget.currentHealth > 0;
        if (checkTarget is Minion minionTarget) return minionTarget != null && minionTarget.currentHealth > 0;
        if (checkTarget is Tower towerTarget) return towerTarget != null && towerTarget.currentHealth > 0;
        if (checkTarget is NPC npcTarget) return npcTarget != null && npcTarget.currentHealth > 0;

        return false;
    }
}
