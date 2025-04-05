using UnityEngine;
public class IndicatorController : MonoBehaviour
{
    public Transform heroPoint; // ตำแหน่งของฮีโร่
    public Transform directionPoint; // ตำแหน่งปลายทางที่ลูกศรจะพุ่งไป
    private Vector3 initialOffset; // Offset ตำแหน่งระหว่าง Indicator กับ Hero

    void Start()
    {
        if (heroPoint != null)
        {
            initialOffset = transform.localPosition; // ใช้ localPosition แทน
        }
    }


    public void UpdateDirection(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - heroPoint.position).normalized;
        direction.y = 1; // ป้องกันการหมุนขึ้นลง

        // Quaternion lookRotation = Quaternion.LookRotation(direction);
        // transform.localRotation = Quaternion.Euler(90, 0, lookRotation.eulerAngles.z);
            // ✅ คำนวณมุมหมุนแกน Z
        float angleZ = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        // ✅ หมุนเฉพาะแกน Z
        transform.localRotation = Quaternion.Euler(90, 0, -angleZ);

    }



    public void SetRotation(float angle)
    {
        //transform.localRotation = Quaternion.Euler(90, angle, 0);
        transform.localRotation = Quaternion.AngleAxis(angle, Vector3.up) * Quaternion.Euler(90, 0, 0);

    }
    public void ResetIndicator()
    {
        transform.localPosition = Vector3.zero; // รีเซ็ตตำแหน่งให้กลับมาเป็น 0,0,0
        transform.localRotation = Quaternion.Euler(90, 0, 0); // รีเซ็ตการหมุน
        //directionPoint.localPosition = new Vector3(0, 0, 5f); // รีเซ็ตตำแหน่ง directionPoint
    }


}
