using System.Collections;
using UnityEngine;
using Photon.Pun;

public class Boss : NPC
{
    public string skill1EffectName = "SE Charge slash red";
    public string skill2EffectName = "AOE Red energy explosion";
    public int bonusGoldOnDeath = 200;
    public float skillRange = 5f;
    public float magic = 200;

    private bool usedSkill1 = false;
    private bool usedSkill2 = false;

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient || isDead) return;

        // เงื่อนไขใช้สกิล 1 เมื่อ HP <= 50%
        if (!usedSkill1 && currentHealth <= maxHealth * 0.5f)
        {
            usedSkill1 = true;
            animator.SetTrigger("Skill1");
            UseSkill1();
        }

        // เงื่อนไขใช้สกิล 2 เมื่อ HP <= 15%
        if (!usedSkill2 && currentHealth <= maxHealth * 0.15f)
        {
            usedSkill2 = true;
            animator.SetTrigger("Skill2");
            StartCoroutine(UseSkill2());
        }
    }

    void UseSkill1()
    {
        //Debug.Log("Boss uses Skill 1!");
        photonView.RPC("RPC_PlayBossSkillEffect", RpcTarget.All, skill1EffectName);
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, skillRange);
        foreach (Collider col in hitColliders)
        {
            Hero target = col.GetComponent<Hero>();
            if (target != null && !target.isDead)
            {
                target.photonView.RPC("RPC_Stun", RpcTarget.All, 0.5f);
                target.TakeDamage(attackDamage * 2f, this, DamageType.Physical);
            }
        }
    }

    IEnumerator UseSkill2()
    {
        //Debug.Log("Boss uses Skill 2!");
        photonView.RPC("RPC_PlayBossSkillEffect2", RpcTarget.All, skill2EffectName);

        int rounds = 5;
        float interval = 0.3f;

        for (int i = 0; i < rounds; i++)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, skillRange);
            foreach (Collider col in hitColliders)
            {
                Hero target = col.GetComponent<Hero>();
                if (target != null && !target.isDead)
                {
                    target.TakeDamage(magic, this, DamageType.Magic);
                }
            }
            yield return new WaitForSeconds(interval);
        }
    }

    [PunRPC]
    public void RPC_PlayBossSkillEffect(string effectName)
    {
        //EffectManager.instance.PlayEffect(effectName, transform.position, 3f);
        GameObject effectObj = EffectManager.instance.PlayEffect(effectName, transform.position, 2f);

        if (effectObj != null)
        {
            // ปรับขนาดของ effect ตาม skillRange
            Vector3 originalScale = effectObj.transform.localScale;
            effectObj.transform.localScale = new Vector3(
                originalScale.x * skillRange,
                originalScale.y * skillRange,
                originalScale.z * skillRange
            );
        }
        else
        {
            Debug.LogError($"ไม่สามารถสร้างเอฟเฟค {effectName} จาก Pool ได้");
        }
    }
    [PunRPC]
    public void RPC_PlayBossSkillEffect2(string effectName)
    {
        EffectManager.instance.PlayEffect(effectName, transform.position, 3f);
    }

    protected override IEnumerator HandleDeath()
    {
        if (lastAttacker != null)
        {
            Team killerTeam = TeamManager.instance.GetTeam(lastAttacker.ownerPlayer);

            foreach (Hero hero in FindObjectsOfType<Hero>())
            {
                if (TeamManager.instance.GetTeam(hero.ownerPlayer) == killerTeam)
                {
                    // ส่ง RPC ให้ Hero ที่แต่ละเครื่องควบคุมเอง
                    photonView.RPC("RPC_GiveBonusGold", hero.ownerPlayer, hero.photonView.ViewID, bonusGoldOnDeath);
                }
            }
        }

        yield return base.HandleDeath();
    }

    [PunRPC]
    public void RPC_GiveBonusGold(int viewID, int amount)
    {
        PhotonView heroView = PhotonView.Find(viewID);
        if (heroView != null && heroView.IsMine)
        {
            Hero hero = heroView.GetComponent<Hero>();
            hero.ReceiveGold(amount);
        }
    }

} 
