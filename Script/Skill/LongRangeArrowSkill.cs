using System.Collections;
using UnityEngine;
using Photon.Pun;

[CreateAssetMenu(fileName = "LongRangeArrowSkill", menuName = "Skills/Long Range Arrow")]
public class LongRangeArrowSkill : AimingSkill
{
    public GameObject arrowPrefab; // Prefab ของลูกธนู
    public float arrowSpeed = 20f; // ความเร็วของลูกธนู
    public float skillDuration = 30f; // เวลาสูงสุดที่ธนูจะอยู่ได้
    public float damage = 500f; // ดาเมจที่ทำ

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
        FireArrow(myHero, indicatorController.directionPoint.position);
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
    private void FireArrow(Hero myHero, Vector3 targetPosition)
    {
        GameObject arrow = FireballPoolManager.instance.GetProjectile(arrowPrefab.name);
        if (arrow != null)
        {
            arrow.transform.position = myHero.firePoint.position;
            arrow.transform.rotation = Quaternion.identity;

            LongRangeArrowProjectile arrowScript = arrow.GetComponent<LongRangeArrowProjectile>();
            if (arrowScript != null)
            {
                arrowScript.Initialize(myHero, damage, skillDuration, targetPosition, arrowSpeed);
            }
        }
    }

}
