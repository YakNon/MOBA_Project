using System.Collections;
using UnityEngine;
using Photon.Pun;

[CreateAssetMenu(fileName = "NewBlinkSkill", menuName = "Skills/Blink")]
public class BlinkSkill : AimingSkill
{
    //public float blinkDistance = 8.0f; // ระยะทางที่สามารถวาร์ปได้
    public GameObject blinkEffectPrefab; // เอฟเฟคตอนใช้สกิล
    public Skill afterBlinkSkill; 
    public LayerMask terrainLayer; // กำหนด Layer ที่เป็น Terrain

    private Vector2 startTouchPosition;
    private bool isDragging = false; // เพิ่มตัวแปรเช็คสถานะการเล็ง

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
                if (touch.phase == TouchPhase.Ended) // ตรวจจับการปล่อยนิ้ว
                {
                    isDragging = false;
                    break;
                }
            }
            else if (!Input.GetMouseButton(0)) // ตรวจจับการปล่อยคลิกเมาส์
            {
                isDragging = false;
                break;
            }

            Vector2 currentTouchPosition = GetTouchOrMouseScreenPosition();
            Vector2 delta = currentTouchPosition - startTouchPosition;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            indicatorController.SetRotation(angle);
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

        bool canBlink = true;

        //  เช็คว่าตำแหน่ง blinkTarget มีพื้นให้ยืนหรือไม่
        if (Physics.Raycast(blinkTarget + Vector3.up * 5, Vector3.down, out RaycastHit terrainHit, 10f, terrainLayer))
        {
            //Debug.Log($" Debug: blinkTarget = {blinkTarget}, terrain.y = {terrainHit.point.y}");

            //  ถ้าความสูงของพื้น > 2.5 → ไม่ให้ Blink
            if (terrainHit.point.y > 2.5f)
            {
                Debug.Log(" Blink ถูกยกเลิก: จุดเป้าหมายสูงเกินไป!");
                canBlink = false;
            }
            else
            {
                //  ปรับให้ blinkTarget เป็นพื้นดินที่ Raycast ตรวจจับได้
                blinkTarget = terrainHit.point;
            }
        }
        else
        {
            Debug.Log("Blink ถูกยกเลิก: ไม่มีพื้นรองรับที่ blinkTarget!");
            canBlink = false; // ไม่มีพื้นรองรับ → ไม่ให้ Blink
        }

        //  ตรวจสอบว่ามีสิ่งกีดขวางที่ blinkTarget หรือไม่
        // if (Physics.Raycast(myHero.transform.position + Vector3.up * 0.5f, blinkDirection, out RaycastHit hit, blinkDistance, terrainLayer))
        // {
        //     Debug.Log("Blink ถูกยกเลิก: มีสิ่งกีดขวางขวางอยู่!");
        //     canBlink = false;
        // }

        if (!canBlink)
        {
            Debug.Log("🚫 Blink ถูกยกเลิก: ตำแหน่ง blinkTarget ไม่สามารถไปได้!");
            indicatorInstance.SetActive(false);
            return;
        }

        //  ส่งคำสั่งให้ทุกเครื่องเล่นเอฟเฟคและย้ายตำแหน่ง
        if(blinkEffectPrefab != null){
            myHero.photonView.RPC("RPC_BlinkToPosition", RpcTarget.All, blinkTarget, blinkEffectPrefab.name);
        }else{
            myHero.photonView.RPC("RPC_BlinkToPosition", RpcTarget.All, blinkTarget, "FireEmbers");
        }
        

        //  ปิด Indicator
        indicatorInstance.SetActive(false);
        if (afterBlinkSkill != null)
        {
            myHero.StartCoroutine(DelayedSkillActivation(myHero, 0.1f)); // หน่วงเวลาเล็กน้อยเพื่อความสมูท
        }

        //  ปิด Indicator
        indicatorInstance.SetActive(false);
    }
    private IEnumerator DelayedSkillActivation(Hero myHero, float delay)
    {
        yield return new WaitForSeconds(delay);
        afterBlinkSkill?.Activate(myHero);
        //Debug.Log($" {myHero.name} ใช้ {afterBlinkSkill.skillName} หลังจาก Blink!");
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
