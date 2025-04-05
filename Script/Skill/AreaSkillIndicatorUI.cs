using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class AreaSkillIndicatorUI : MonoBehaviourPun
{
    [Header("UI Elements")]
    public GameObject rangeIndicator;  // วงกลมแสดงระยะการใช้สกิล
    public GameObject damageAreaIndicator;  // วงกลมแสดงพื้นที่สกิล

    private string skillEffectPrefab; 
    private string hitEffectPrefab; 

    private bool isPlacingSkill = false;
    private float range;
    private float damageRadius;
    private float damageAmount;
    private float effectDuration = 3f; // ระยะเวลาส่งผลของสกิล
    private float damageInterval = 0.5f; // ระยะห่างของการทำดาเมจ
    private bool isDamageOverTime = false; // true = ดาเมจต่อเนื่อง, false = ดาเมจครั้งเดียว
    private Hero myHero;
    private bool isDraggingMouse = false;

    public void Setup(Hero myHero, float range, float damageRadius, float damageAmount, bool isDamageOverTime, float effectDuration, string skillEffectPrefab, string hitEffectPrefab)
    {
        this.myHero = myHero;
        this.range = range;
        this.damageRadius = damageRadius;
        this.damageAmount = damageAmount;
        this.isDamageOverTime = isDamageOverTime;
        this.effectDuration = effectDuration;
        this.skillEffectPrefab = skillEffectPrefab;
        this.hitEffectPrefab = hitEffectPrefab;

        // ตั้งค่าตำแหน่งและขนาด UI
        rangeIndicator.transform.position = myHero.transform.position + new Vector3(0, 0.5f, 0);
        // Vector3 heroScale = myHero.transform.lossyScale; // ขนาดจริงของฮีโร่
        // indicatorInstance.transform.localScale = new Vector3(
        //         ((float)skillRange / heroScale.x) * 2f,
        //         ((float)skillRange / heroScale.y) * 2f,
        //         1f
        //     );

        rangeIndicator.transform.localScale = new Vector3(range * 2, range * 2, 1);
        damageAreaIndicator.transform.position = myHero.transform.position + new Vector3(0, 0.5f, 0);
        damageAreaIndicator.transform.localScale = new Vector3(damageRadius * 2, damageRadius * 2, 1);

        isPlacingSkill = true;
    }


    void Update()
    {
        if (!isPlacingSkill) return;

    #if UNITY_STANDALONE || UNITY_WEBGL
        HandleMouseInput();
    #elif UNITY_ANDROID || UNITY_IOS
        HandleTouchInput();
    #endif
    }

    private void HandleMouseInput()
    {
        // เริ่มลาก
        if (Input.GetMouseButtonDown(0))
        {
            isDraggingMouse = true;
        }

        if (Input.GetMouseButton(0) && isDraggingMouse)
        {
            Vector3 touchWorldPosition = GetMouseWorldPosition();
            UpdateIndicatorPosition(touchWorldPosition);
        }

        // ปล่อยเมาส์
        if (Input.GetMouseButtonUp(0) && isDraggingMouse)
        {
            isDraggingMouse = false;
            ConfirmSkillUse();
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        Vector3 touchWorldPosition = GetTouchWorldPosition();

        if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
        {
            UpdateIndicatorPosition(touchWorldPosition);
        }

        if (touch.phase == TouchPhase.Ended)
        {
            ConfirmSkillUse();
        }
    }
    private void UpdateIndicatorPosition(Vector3 position)
    {
        if (position == Vector3.zero) return;

        rangeIndicator.transform.position = myHero.transform.position + new Vector3(0, 0.5f, 0);

        float distance = Vector3.Distance(position, rangeIndicator.transform.position);
        position.y = rangeIndicator.transform.position.y;

        if (distance <= range)
        {
            damageAreaIndicator.transform.position = position;
        }
        else
        {
            Vector3 direction = (position - rangeIndicator.transform.position).normalized;
            Vector3 limitedPosition = rangeIndicator.transform.position + direction * range;
            limitedPosition.y = rangeIndicator.transform.position.y;
            damageAreaIndicator.transform.position = limitedPosition;
        }
    }
    public void ConfirmSkillUse()
    {
        isPlacingSkill = false;

        // เอฟเฟกต์สกิล
        if (myHero.photonView != null && skillEffectPrefab != "")
        {
            myHero.photonView.RPC("RPC_CreateSkillEffect", RpcTarget.All, skillEffectPrefab, damageAreaIndicator.transform.position, effectDuration, damageRadius);
        }

        if (isDamageOverTime)
        {
            StartCoroutine(ApplyDamageOverTime(effectDuration, damageInterval));
        }
        else
        {
            ApplyInstantDamage();
        }

        rangeIndicator.SetActive(false);
        damageAreaIndicator.SetActive(false);
    }
    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return new Vector3(hit.point.x, 0.5f, hit.point.z);
        }
        return Vector3.zero;
    }

    private Vector3 GetTouchWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return new Vector3(hit.point.x, 0.5f, hit.point.z);
        }
        return Vector3.zero;
    }
    public void AimAtScreenPosition(Vector2 screenPosition)
    {
        if (!isPlacingSkill) return;

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 position = new Vector3(hit.point.x, 0.5f, hit.point.z);
            UpdateIndicatorPosition(position);
        }
    }





    // void Update()
    // {
    //     if (!isPlacingSkill) return;  // รอให้กดใช้ก่อน

    //     //ให้ RangeIndicator ติดตามฮีโร่ตลอดเวลา
    //     rangeIndicator.transform.position = myHero.transform.position + new Vector3(0, 0.5f, 0);

    //     Vector3 touchWorldPosition = GetTouchOrMouseWorldPosition();
    //     if (touchWorldPosition != Vector3.zero)
    //     {
    //         float distance = Vector3.Distance(touchWorldPosition, rangeIndicator.transform.position);
            
    //         // ล็อคค่า y ของ damageAreaIndicator ให้เท่ากับ rangeIndicator
    //         touchWorldPosition.y = rangeIndicator.transform.position.y;

    //         if (distance <= range)
    //         {
    //             damageAreaIndicator.transform.position = touchWorldPosition;
    //         }
    //         else
    //         {
    //             Vector3 direction = (touchWorldPosition - rangeIndicator.transform.position).normalized;
    //             Vector3 limitedPosition = rangeIndicator.transform.position + direction * range;
                
    //             // ล็อคค่า y เช่นกัน
    //             limitedPosition.y = rangeIndicator.transform.position.y;

    //             damageAreaIndicator.transform.position = limitedPosition;
    //         }
    //         //Debug.Log($"Touch world pos: {touchWorldPosition}, releasing = {IsReleasedNow()}");

    //         //ต้องตรวจจับปล่อยนิ้วก่อนค่อยปิด UI
    //         if (IsTouchReleased())
    //         {
    //             isPlacingSkill = false;

    //             //สร้างเอฟเฟกต์สกิลให้ทุกคนเห็น
    //             if (myHero.photonView != null && skillEffectPrefab != "")
    //             {
    //                 myHero.photonView.RPC("RPC_CreateSkillEffect", RpcTarget.All, skillEffectPrefab, damageAreaIndicator.transform.position, effectDuration, damageRadius);
    //             }

    //             //ทำดาเมจหลังจากปล่อยนิ้วจริงๆ
    //             if (isDamageOverTime)
    //             {
    //                 StartCoroutine(ApplyDamageOverTime(effectDuration, damageInterval));
    //             }
    //             else
    //             {
    //                 ApplyInstantDamage();
    //             }
    //             //Debug.Log($"Touch world pos: {touchWorldPosition}, releasing = {IsReleasedNow()}");

    //             //ซ่อน UI แทนที่จะทำลาย
    //             rangeIndicator.SetActive(false);
    //             damageAreaIndicator.SetActive(false);
    //         }
    //     }
    // }




    //รองรับทั้งเมาส์และมือถือ
    // private Vector3 GetTouchOrMouseWorldPosition()
    // {
    //     Vector2 screenPosition;
    //     if (Input.touchCount > 0)
    //     {
    //         screenPosition = Input.GetTouch(0).position;
    //     }
    //     else if (Input.GetMouseButton(0))
    //     {
    //         screenPosition = Input.mousePosition;
    //     }
    //     else
    //     {
    //         return Vector3.zero;
    //     }

    //     Ray ray = Camera.main.ScreenPointToRay(screenPosition);
    //     if (Physics.Raycast(ray, out RaycastHit hit))
    //     {
    //         return new Vector3(hit.point.x, 0.5f, hit.point.z);
    //     }
    //     return Vector3.zero;
    // }

    //ตรวจสอบการปล่อยนิ้วหรือคลิกเมาส์

    // private bool IsTouchReleased()
    // {
    // #if UNITY_STANDALONE || UNITY_WEBGL
    //     return Input.GetMouseButtonUp(0);
    // #elif UNITY_ANDROID || UNITY_IOS
    //     if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
    //     {
    //         return true;
    //     }
    //     return false;
    // #else
    //     // fallback
    //     return Input.GetMouseButtonUp(0);
    // #endif
    // }



    private void ApplyInstantDamage()
    {
        Collider[] hitColliders = Physics.OverlapSphere(damageAreaIndicator.transform.position, damageRadius);
        foreach (Collider collider in hitColliders)
        {
            ApplyDamageToTarget(collider);
        }
        DestroyUI();
    }

    private IEnumerator ApplyDamageOverTime(float totalDuration, float interval)
    {
        float elapsedTime = 0f;
        while (elapsedTime < totalDuration)
        {
            Collider[] hitColliders = Physics.OverlapSphere(damageAreaIndicator.transform.position, damageRadius);
            foreach (Collider collider in hitColliders)
            {
                ApplyDamageToTarget(collider);
            }

            yield return new WaitForSeconds(interval);
            elapsedTime += interval;
        }
        DestroyUI();
    }

    private void ApplyDamageToTarget(Collider collider)
    {
        NPC enemy = collider.GetComponent<NPC>();
        Hero enemyHero = collider.GetComponent<Hero>();
        Minion enemyMinion = collider.GetComponent<Minion>();
        if (collider.transform != myHero.transform)
        {
            if (enemy != null)
            {
                enemy.TakeDamage(damageAmount, myHero ,DamageType.Magic);
                if (myHero.photonView != null && hitEffectPrefab != "")
                {
                    myHero.photonView.RPC("RPC_CreateHitEffect", RpcTarget.All, hitEffectPrefab, enemy.transform.position, 2f);
                }
            }
            else if (enemyHero != null && !myHero.IsOnSameTeamAs(enemyHero))
            {
                enemyHero.TakeDamage(damageAmount, myHero, DamageType.Magic);
                if (myHero.photonView != null && hitEffectPrefab != "")
                {
                    myHero.photonView.RPC("RPC_CreateHitEffect", RpcTarget.All, hitEffectPrefab, enemyHero.transform.position ,2f);
                }
            }
            else if (enemyMinion != null && enemyMinion.minionTeam != TeamManager.instance.GetTeam(myHero.photonView.Owner))
            {
                enemyMinion.TakeDamage(damageAmount, myHero, DamageType.Magic);
                if (myHero.photonView != null && hitEffectPrefab != "")
                {
                    myHero.photonView.RPC("RPC_CreateHitEffect", RpcTarget.All, hitEffectPrefab, enemyMinion.transform.position, 2f);
                }
            }
        }
    }

    private void DestroyUI()
    {
        Destroy(rangeIndicator);
        Destroy(damageAreaIndicator);
        Destroy(gameObject);
    }
}
