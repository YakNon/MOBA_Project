using System.Collections;
using UnityEngine;
using Photon.Pun;

[CreateAssetMenu(fileName = "MagicExplosion", menuName = "Skills/Magic Explosion")]
public class Explosion : Skill
{
    public float skillRange = 5f; // ระยะสกิล
    public GameObject skillEffectPrefab; // เอฟเฟคของสกิล
    public GameObject hitEffectPrefab; 
    public float baseDamage = 0f;
    public float increaseDamage = 1.2f;
    public int hitCount = 1; // จำนวนครั้งที่โจมตี (1 = โจมตีครั้งเดียว)
    public float hitInterval = 0.5f; // เวลาห่างระหว่างแต่ละ Hit
    public Skill afterExplosionSkill; 
    public DamageType damageType = DamageType.Magic;

    public override void Activate(Hero myHero)
    {
        PhotonView photonView = myHero.photonView; 
        if (photonView == null) return;
        float skillDamage = baseDamage;
        if(damageType == DamageType.Magic) skillDamage += (myHero.magic*increaseDamage);
        else skillDamage += (myHero.attackDamage*increaseDamage);
        

        //  เล่นเอฟเฟคสกิล
        if (skillEffectPrefab != null)
        {
            string effectName = skillEffectPrefab.name;

            if(hitCount == 1){
                photonView.RPC("RPC_CreateSkillEffectChild", RpcTarget.All, effectName, myHero.photonView.ViewID, 2f, skillRange);
            }else{
                 photonView.RPC("RPC_CreateSkillEffectChild", RpcTarget.All, effectName, myHero.photonView.ViewID, hitInterval*hitCount, skillRange);
            }
           
        }

        //  ทำดาเมจ
        myHero.StartCoroutine(ApplyDamageRoutine(myHero, skillDamage, photonView));

        if (afterExplosionSkill != null)
        {
            myHero.StartCoroutine(DelayedSkillActivation(myHero, 0.1f)); // หน่วงเวลาเล็กน้อยเพื่อความสมูท
        }
    }

    private IEnumerator ApplyDamageRoutine(Hero myHero, float damage, PhotonView photonView)
    {
        for (int i = 0; i < hitCount; i++)
        {
            ApplyDamageToAllTargets(myHero, damage, photonView);

            if (i < hitCount - 1) // ถ้าไม่ใช่ hit สุดท้าย ให้รอเวลา hitInterval ก่อนทำดาเมจครั้งต่อไป
            {
                yield return new WaitForSeconds(hitInterval);
            }
        }
    }

    private void ApplyDamageToAllTargets(Hero myHero, float damage, PhotonView photonView)
    {
        Collider[] hitColliders = Physics.OverlapSphere(myHero.transform.position, skillRange);
        foreach (Collider collider in hitColliders)
        {
            IDamageable target = collider.GetComponent<IDamageable>();
            if (target != null && !myHero.IsOnSameTeamAs(target))
            {
                ApplyDamage(myHero, target, damage, photonView);
            }
        }
    }

    private void ApplyDamage(Hero myHero, IDamageable target, float damage, PhotonView photonView)
    {
        //Debug.Log($"{myHero.name} casts Magic Explosion on {target} dealing {damage} magic damage.");

        target.TakeDamage(damage, myHero, DamageType.Magic);

        Vector3 heroScale = myHero.transform.lossyScale;
        if (myHero.rangeCanvasInstance != null)
        {
            myHero.rangeCanvasInstance.transform.localScale = new Vector3(
                ((float)skillRange / heroScale.x) * 2f,
                ((float)skillRange / heroScale.y) * 2f,
                1f
            );
        }
        myHero.ShowRangeCanvas();
        if (myHero.rangeCanvasInstance != null)
        {
            myHero.rangeCanvasInstance.transform.localScale = new Vector3(
                ((float)myHero.attackRange / heroScale.x) * 2f,
                ((float)myHero.attackRange / heroScale.y) * 2f,
                1f
            );
        }
    }
    private IEnumerator DelayedSkillActivation(Hero myHero, float delay)
    {
        yield return new WaitForSeconds(delay);
        afterExplosionSkill?.Activate(myHero);
    }
}
