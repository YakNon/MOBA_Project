using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CameraMovement : MonoBehaviourPunCallbacks
{
    public Transform target;
    public float followSpeed = 15f; 
    public float offsetDistance = 16f; 
    public float offsetHeight = 18f; 
    public float rotationSpeed = 5f; 

    public float currentRotationY = 0f;
    public float currentRotationX = 30f;
    public float minRotationX = -30f;
    public float maxRotationX = 30f;

    private void OnEnable()
    {
        if (PhotonNetwork.IsConnected && photonView.IsMine)
        {
            //StartCoroutine(FindAndSetHeroTarget());
        }
    }

    private IEnumerator FindAndSetHeroTarget()
    {
        while (target == null)
        {
            // ค้นหาวัตถุที่มี Tag เป็น "Hero"
            GameObject hero = GameObject.FindGameObjectWithTag("Hero");
            if (hero != null)
            {
                PhotonView pv = hero.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    target = hero.transform;
                    Debug.Log("Hero assigned as target: " + hero.name);
                }
                else
                {
                    Debug.Log("Found hero but it is not owned by the current player.");
                }
            }
            else
            {
                Debug.Log("No hero found with the tag 'Hero'.");
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    // void Update()
    // {
    //     if (Input.GetMouseButton(1))
    //     {
    //         currentRotationY -= Input.GetAxis("Mouse X") * rotationSpeed;
    //         currentRotationX -= Input.GetAxis("Mouse Y") * rotationSpeed;
    //         currentRotationX = Mathf.Clamp(currentRotationX, minRotationX, maxRotationX);
    //     }
    // }

void LateUpdate()
{
    // ถ้ายังไม่มีเป้าหมาย (target) ให้หยุดทำงาน
    if (target == null) return;

    // ดึงข้อมูลของฮีโร่ที่กล้องกำลังติดตาม
    Hero hero = target.GetComponent<Hero>();

    // ตรวจสอบว่าฮีโร่นี้เป็นทีมแดงหรือไม่
    bool isRedTeam = false; // ค่าเริ่มต้นคือไม่ใช่ทีมแดง
    if (hero != null) // ตรวจสอบว่ามีข้อมูลฮีโร่จริงหรือไม่
    {
        // ใช้ TeamManager เพื่อเช็คว่าทีมของฮีโร่นี้เป็นทีมแดงหรือไม่
        if (TeamManager.instance.GetTeam(hero.ownerPlayer) == Team.Red)
        {
            isRedTeam = true; // ถ้าใช่ กำหนดค่าเป็น true
        }
    }

    // ถ้าฮีโร่เป็นทีมแดง หมุนกล้องไป 180 องศาเพื่อให้เห็นในมุมเดียวกับทีมสีน้ำเงิน
    float adjustedRotationY;
    if (isRedTeam)
    {
        adjustedRotationY = currentRotationY + 180; // หมุน Y เพิ่ม 180 องศา
    }
    else
    {
        adjustedRotationY = currentRotationY; // ถ้าไม่ใช่ทีมแดง ใช้ค่าปกติ
    }

    // คำนวณค่าการหมุนของกล้อง (รวมค่า Y ที่ถูกปรับให้ทีมแดงหมุนไป 180 องศา)
    Quaternion rotation = Quaternion.Euler(currentRotationX, adjustedRotationY, 0);

    // คำนวณตำแหน่งที่กล้องควรจะอยู่ โดยใช้ offset ที่กำหนดไว้
    Vector3 offset = new Vector3(0, offsetHeight, -offsetDistance);
    Vector3 desiredPosition = target.position + rotation * offset;

    // ทำให้กล้องเคลื่อนที่อย่างนุ่มนวลไปยังตำแหน่งที่ต้องการ
    transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

    // ให้กล้องมองไปที่เป้าหมาย (ฮีโร่) โดยให้มองตรงกลางตัวฮีโร่ขึ้นไปครึ่งหนึ่งของ offsetHeight
    transform.LookAt(target.position + Vector3.up * offsetHeight / 2);
}




}
