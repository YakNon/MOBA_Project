using System.Collections;
using UnityEngine;
using Photon.Pun;
using UnityEngine.EventSystems;

[CreateAssetMenu(fileName = "FireBallSkill", menuName = "Skills/FireBallSkill")]
public class FireBall : AimingSkill
{
    public GameObject fireballPrefab;
    //public GameObject arrowPrefab;
    public float baseDamage = 0f;
    public float slowDuration = 0.5f;
    public float slowPercentage = 0.5f;
    public float magicPersen = 1.5f;
    public float fireballSpeed = 15f;

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
        FireFireBall(myHero, indicatorController.directionPoint.position);
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

    private void FireFireBall(Hero myHero, Vector3 targetPosition)
    {
        GameObject fireball = FireballPoolManager.instance.GetProjectile("fireball");
        if (fireball != null)
        {
            fireball.transform.position = myHero.firePoint.position;
            fireball.transform.rotation = Quaternion.identity;

            FireBallPrefab fireballScript = fireball.GetComponent<FireBallPrefab>();
            if (fireballScript != null)
            {
                float damage = baseDamage + (myHero.magic * magicPersen);
                fireballScript.Initialize(myHero, damage, skillRange, targetPosition, fireballSpeed, slowPercentage, slowDuration);
            }
        }

        // GameObject fireball = PhotonNetwork.Instantiate(fireballPrefab.name, myHero.firePoint.position, Quaternion.identity);
        // FireBallPrefab fireballScript = fireball.GetComponent<FireBallPrefab>();
        // if (fireballScript != null)
        // {
        //     float damage = baseDamage + (myHero.magic*magicPersen);
        //     fireballScript.Initialize(myHero, damage, skillRange, targetPosition , fireballSpeed, slowPercentage, slowDuration);
        //     //Initialize(Hero attacker, damage,      range,     targetPosition, fireballSpeed, slowPercentage, slowDuration)
        // }
    }
}
