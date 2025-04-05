using System.Collections;
using UnityEngine;
using Photon.Pun;

[CreateAssetMenu(fileName = "New BuffAlly", menuName = "Skills/Buff Ally")]
public class BuffAlly : Skill
{
    public float attackSpeedBoostPercent = 0f;
    public float attackRangeBoostPercent =0f;
    public float attackPowerBoostPercent = 0f;
    public float magicPowerBoostPercent = 0f;
    public float trueDamagePowerBoostPercent = 0f;
    public float physicalDefenseBoostPercent = 0f;
    public float magicDefenseBoostPercent = 0f;
    public float movementSpeedBoostPercent = 0f;
    
    public float duration = 5f;
    public float buffRange = 8f;
    public string buffEffectPrefabName = "CA Buff"; //  ชื่อ Prefab ใน Resources/PhotonEffects/

    public override void Activate(Hero myHero)
    {
        PhotonView photonView = myHero.photonView;
        if (photonView == null) return;

        //  ปรับขนาด Range Canvas เป็น buffRange
        Vector3 heroScale = myHero.transform.lossyScale;
        if (myHero.rangeCanvasInstance != null)
        {
            myHero.rangeCanvasInstance.transform.localScale = new Vector3(
                ((float)buffRange / heroScale.x) * 2f,
                ((float)buffRange / heroScale.y) * 2f,
                1f
            );
        }

        //  แสดง Range Canvas ตาม buffRange ก่อน
        myHero.ShowRangeCanvas();

        //  ใช้ Coroutine เพื่อคืนค่าเป็น attackRange หลังจาก 0.5 วินาที (ต้องมากกว่าค่าที่ HideRangeCanvas ใช้)
        myHero.StartCoroutine(ResetRangeCanvasSize(myHero, heroScale));

        //  บัฟตัวเอง
        photonView.RPC("RPC_ApplyBuff", RpcTarget.All, 
            attackSpeedBoostPercent, attackRangeBoostPercent, attackPowerBoostPercent, 
            magicPowerBoostPercent, trueDamagePowerBoostPercent, physicalDefenseBoostPercent, 
            magicDefenseBoostPercent, movementSpeedBoostPercent, duration, buffEffectPrefabName);

        Collider[] hitColliders = Physics.OverlapSphere(myHero.transform.position, buffRange);
        foreach (Collider collider in hitColliders)
        {
            Hero allyHero = collider.GetComponent<Hero>();
            if (allyHero != null && myHero.IsOnSameTeamAs(allyHero))
            {
                allyHero.photonView.RPC("RPC_ApplyBuff", RpcTarget.All,
                    attackSpeedBoostPercent, attackRangeBoostPercent, attackPowerBoostPercent, 
                    magicPowerBoostPercent, trueDamagePowerBoostPercent, physicalDefenseBoostPercent, 
                    magicDefenseBoostPercent, movementSpeedBoostPercent, duration, buffEffectPrefabName);
            }
        }
    }

    //  ใช้ Coroutine เพื่อคืนค่าเป็น attackRange หลังจากที่ RangeCanvas หายไป
    private IEnumerator ResetRangeCanvasSize(Hero myHero, Vector3 heroScale)
    {
        yield return new WaitForSeconds(0.6f); // ให้มากกว่าเวลาที่ HideRangeCanvas ใช้ (0.5f)

        if (myHero.rangeCanvasInstance != null)
        {
            myHero.rangeCanvasInstance.transform.localScale = new Vector3(
                ((float)myHero.attackRange / heroScale.x) * 2f,
                ((float)myHero.attackRange / heroScale.y) * 2f,
                1f
            );
        }
    }


}
