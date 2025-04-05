using System.Collections;
using UnityEngine;
using Photon.Pun;

[CreateAssetMenu(fileName = "NewBuffSkill", menuName = "Skills/Buff")]
public class BuffSkill : Skill
{
    public float attackSpeedBoostPercent = 0f;
    public float attackRangeBoostPercent = 0f;
    public float attackPowerBoostPercent = 0f;
    public float magicPowerBoostPercent = 0f;
    public float trueDamagePowerBoostPercent = 0f;
    public float physicalDefenseBoostPercent = 0f;
    public float magicDefenseBoostPercent = 0f;
    public float movementSpeedBoostPercent = 0f;
    public bool isAttackinPhysical = true;
    public bool isAttackinMagic = false;
    public bool isAttackinTrueDamage = false;
    
    public float duration = 5f; // ระยะเวลาของ Buff
    public bool isRemoveDebuf = false;
    public GameObject buffEffectPrefab; // เอฟเฟคแสดงผลขณะ Buff ทำงาน

    public override void Activate(Hero myHero)
    {
        myHero.StartCoroutine(ApplyBuff(myHero));
    }

    private IEnumerator ApplyBuff(Hero myHero)
    {
        Debug.Log($"✨ {myHero.name} ใช้ BuffSkill!");
        if(isRemoveDebuf)
        {
            myHero.photonView.RPC("RPC_ApplyRemoveDebuff", RpcTarget.All, myHero.photonView.ViewID, duration);
        }

        //  คำนวณค่าที่จะเพิ่มให้ฮีโร่ (คิดเป็น %)
        float originalAttackSpeed = myHero.attackSpeed;
        float originalAttackRange = myHero.attackRange;
        float originalAttackPower = myHero.attackDamage;
        float originalMagicPower = myHero.magic;
        float originalPhysicalDefense = myHero.defense;
        float originalMagicDefense = myHero.magicDef;
        float originalTrueDamage = myHero.trueDamage;
        float originalMovementSpeed = myHero.speed;
        bool originalIsAttackinPhysical = myHero.isAttackinPhysical;
        bool originalIsAttackinMagic = myHero.isAttackinMagic;
        bool originalIsAttackinTrueDamage = myHero.isAttackinTrueDamage;

        myHero.attackSpeed += originalAttackSpeed * (attackSpeedBoostPercent);
        myHero.attackRange += originalAttackRange * (attackRangeBoostPercent);
        myHero.attackDamage += originalAttackPower * (attackPowerBoostPercent);
        myHero.magic += originalMagicPower * (magicPowerBoostPercent);
        myHero.trueDamage += originalTrueDamage *(trueDamagePowerBoostPercent);
        myHero.defense += originalPhysicalDefense * (physicalDefenseBoostPercent );
        myHero.magicDef += originalMagicDefense * (magicDefenseBoostPercent );
        myHero.speed += originalMovementSpeed * (movementSpeedBoostPercent);
        myHero.isAttackinPhysical = isAttackinPhysical;
        myHero.isAttackinMagic = isAttackinMagic;
        myHero.isAttackinTrueDamage = isAttackinTrueDamage;

        //  เล่นเอฟเฟคบัฟ (ถ้ามี)
        if (buffEffectPrefab != null)
        {
            myHero.photonView.RPC("RPC_CreateSkillEffectChild", RpcTarget.All, buffEffectPrefab.name, myHero.photonView.ViewID, duration);
        }
        else
        {
            myHero.photonView.RPC("RPC_CreateSkillEffectChild", RpcTarget.All, "CA Buff", myHero.photonView.ViewID, duration);
        }

        Debug.Log($" Buff Applied: {duration} วินาที!");
        Vector3 heroScale = myHero.transform.lossyScale; // ขนาดจริงของฮีโร่ ไว้หารเพื่อให้ indicator ตรงกับ attackRange
        // แสดง RangeCanvas ขนาดใหม่
        if (myHero.rangeCanvasInstance != null)
        {
            
            myHero.rangeCanvasInstance.transform.localScale = new Vector3(
                ((float)myHero.attackRange / heroScale.x) * 2f,
                ((float)myHero.attackRange / heroScale.y) * 2f,
                1f
            );
            //hero.rangeCanvasInstance.transform.localScale = new Vector3(hero.attackRange * 2, hero.attackRange * 2, 1);
        }

        yield return new WaitForSeconds(duration);

        //  คืนค่ากลับหลังจากหมดเวลา
        myHero.attackSpeed = originalAttackSpeed;
        myHero.attackRange = originalAttackRange;
        myHero.attackDamage = originalAttackPower;
        myHero.magic = originalMagicPower;
        myHero.trueDamage = originalTrueDamage;
        myHero.defense = originalPhysicalDefense;
        myHero.magicDef = originalMagicDefense;
        myHero.speed = originalMovementSpeed;
        myHero.isAttackinPhysical = originalIsAttackinPhysical;
        myHero.isAttackinMagic = originalIsAttackinMagic;
        myHero.isAttackinTrueDamage = originalIsAttackinTrueDamage;
        // รีเซ็ต RangeCanvas ขนาดเดิม
        if (myHero.rangeCanvasInstance != null)
        {
            myHero.rangeCanvasInstance.transform.localScale = new Vector3(
                ((float)myHero.attackRange / heroScale.x) * 2f,
                ((float)myHero.attackRange / heroScale.y) * 2f,
                1f
            );
            //hero.rangeCanvasInstance.transform.localScale = new Vector3(hero.attackRange * 2, hero.attackRange * 2, 1);
        }

        Debug.Log($"Buff Expired!");
    }
}
