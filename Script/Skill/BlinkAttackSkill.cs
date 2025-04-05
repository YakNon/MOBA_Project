using System.Collections;
using UnityEngine;
using Photon.Pun;

[CreateAssetMenu(fileName = "NewBlinkAttackSkill", menuName = "Skills/BlinkAttack")]
public class BlinkAttackSkill : AimingSkill
{
    public GameObject blinkEffectPrefab; // เอฟเฟควาร์ป
    public GameObject attackEffectPrefab; // เอฟเฟคโจมตี
    public float attackSkillRange = 3.0f; // ระยะโจมตีหลังวาร์ป
    public float damageIncrease = 1.5f; // ดาเมจของสกิล
    public DamageType damageType = DamageType.Magic;
    public LayerMask terrainLayer; 

    private Vector2 startTouchPosition;

    private bool isDragging = false;

    public override void Activate(Hero myHero)
    {
        base.Activate(myHero);
        myHero.StartCoroutine(AimingCoroutine(myHero));
    }

    private IEnumerator AimingCoroutine(Hero myHero)
    {
        isDragging = true;
        startTouchPosition = GetTouchOrMouseScreenPosition();

        while (isDragging)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Ended)
                {
                    isDragging = false;
                    break;
                }
            }
            else if (!Input.GetMouseButton(0))
            {
                isDragging = false;
                break;
            }

            Vector2 currentTouchPosition = GetTouchOrMouseScreenPosition();
            indicatorController.UpdateDirection(GetTouchOrMouseWorldPosition());

            yield return null;
        }

        OnRelease(myHero);
    }

    public override void OnDrag(Vector2 dragPosition)
    {
        Vector3 worldPos = GetTouchOrMouseWorldPosition();
        indicatorController.UpdateDirection(worldPos);
    }

    public override void OnRelease(Hero myHero)
    {
        if (indicatorController == null) return;

        Vector3 blinkDirection = (indicatorController.directionPoint.position - myHero.transform.position).normalized;
        Vector3 blinkTarget = myHero.transform.position + blinkDirection * (skillRange / 2.0f);

        Debug.Log($" Debug: heroPos = {myHero.transform.position}, blinkTarget = {blinkTarget}");

        bool canBlink = true;

        if (Physics.Raycast(blinkTarget + Vector3.up * 5, Vector3.down, out RaycastHit terrainHit, 10f, terrainLayer))
        {
            if (terrainHit.point.y > 2.5f)
            {
                Debug.Log(" BlinkAttack ถูกยกเลิก: จุดเป้าหมายสูงเกินไป!");
                canBlink = false;
            }
            else
            {
                blinkTarget = terrainHit.point;
            }
        }
        else
        {
            Debug.Log(" BlinkAttack ถูกยกเลิก: ไม่มีพื้นรองรับ!");
            canBlink = false;
        }

        if (!canBlink)
        {
            ///Debug.Log(" Blink ถูกยกเลิก!");
            indicatorInstance.SetActive(false);
            return;
        }
        float damage = 0f;
        if (damageType == DamageType.Physical || damageType == DamageType.TrueDamage)
        {
            damage = myHero.attackDamage * damageIncrease;
        }
        else
        {
            damage = myHero.magic * damageIncrease;
        }
        

        //  ส่งคำสั่งให้ทุกเครื่องเล่นเอฟเฟคและย้ายตำแหน่ง
        myHero.photonView.RPC("RPC_BlinkAttack", RpcTarget.All, blinkTarget, blinkEffectPrefab.name, attackEffectPrefab.name, attackSkillRange, damage, damageType);

        //  ปิด Indicator
        indicatorInstance.SetActive(false);
    }

    private Vector2 GetTouchOrMouseScreenPosition()
    {
        if (Input.touchCount > 0) return Input.GetTouch(0).position;
        return (Vector2)Input.mousePosition;
    }

    private Vector3 GetTouchOrMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(GetTouchOrMouseScreenPosition());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return new Vector3(hit.point.x, 0, hit.point.z);
        }
        return Vector3.zero;
    }
}
