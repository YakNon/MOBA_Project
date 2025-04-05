using System.Collections;
using UnityEngine;
using Photon.Pun;

[CreateAssetMenu(fileName = "HeroRangeBuffSkill", menuName = "Skills/Hero Range Buff")]
public class HeroRangeBuffSkill : Skill
{
    public GameObject skillEffectPrefab; // เอฟเฟคของสกิล
    public float rangeMultiplier = 1.2f; // เพิ่มระยะโจมตี 20%
    public float damageMultiplier = 1.5f; // เพิ่มพลังโจมตี 50%
    public float attackSpeedMultiplier = 2.0f; // เพิ่มความเร็วโจมตี 2 เท่า
    public float duration = 7.0f; // ระยะเวลาของบัฟ

    public override void Activate(Hero myHero)
    {
        if (myHero is HeroRange heroRange) // ตรวจสอบว่าเป็น HeroRange หรือไม่
        {
            Debug.Log($"⚡ {myHero.name} ใช้สกิล {skillName}! เพิ่มพลังโจมตีและระยะโจมตีชั่วคราว!");
            if (skillEffectPrefab != null)
            {
                myHero.photonView.RPC("RPC_CreateSkillEffectChild", RpcTarget.All, skillEffectPrefab.name, myHero.photonView.ViewID, duration);
            }
            // ใช้ Coroutine เพื่อจัดการบัฟและรีเซ็ตค่าหลังหมดเวลา
            heroRange.StartCoroutine(ApplyBuff(heroRange));
        }
        else
        {
            Debug.LogError("❌ สกิลนี้ใช้ได้เฉพาะ HeroRange เท่านั้น!");
        }
    }

    private IEnumerator ApplyBuff(HeroRange hero)
    {
        float originalAttackRange = hero.attackRange;
        float originalAttackDamage = hero.attackDamage;
        float originalAttackSpeed = hero.attackSpeed;

        // เพิ่มค่าบัฟ
        hero.attackRange *= rangeMultiplier;
        hero.attackDamage *= damageMultiplier;
        hero.attackSpeed *= attackSpeedMultiplier;

        Debug.Log($"บัฟเริ่มต้น: ระยะโจมตี {hero.attackRange}, พลังโจมตี {hero.attackDamage}, ความเร็วโจมตี {hero.attackSpeed}");
        Vector3 heroScale = hero.transform.lossyScale; // ขนาดจริงของฮีโร่ ไว้หารเพื่อให้ indicator ตรงกับ attackRange
        // แสดง RangeCanvas ขนาดใหม่
        if (hero.rangeCanvasInstance != null)
        {
            
            hero.rangeCanvasInstance.transform.localScale = new Vector3(
                ((float)hero.attackRange / heroScale.x) * 2f,
                ((float)hero.attackRange / heroScale.y) * 2f,
                1f
            );
            //hero.rangeCanvasInstance.transform.localScale = new Vector3(hero.attackRange * 2, hero.attackRange * 2, 1);
        }

        yield return new WaitForSeconds(duration);

        // รีเซ็ตค่ากลับเป็นเหมือนเดิม
        hero.attackRange = originalAttackRange;
        hero.attackDamage = originalAttackDamage;
        hero.attackSpeed = originalAttackSpeed;

        Debug.Log($"บัฟหมดเวลา: ระยะโจมตี {hero.attackRange}, พลังโจมตี {hero.attackDamage}, ความเร็วโจมตี {hero.attackSpeed}");

        // รีเซ็ต RangeCanvas ขนาดเดิม
        if (hero.rangeCanvasInstance != null)
        {
            hero.rangeCanvasInstance.transform.localScale = new Vector3(
                ((float)hero.attackRange / heroScale.x) * 2f,
                ((float)hero.attackRange / heroScale.y) * 2f,
                1f
            );
            //hero.rangeCanvasInstance.transform.localScale = new Vector3(hero.attackRange * 2, hero.attackRange * 2, 1);
        }
    }
}
