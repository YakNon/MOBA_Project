using System.Collections;
using UnityEngine;
using Photon.Pun;

public class LongRangeArrowProjectile : MonoBehaviourPun
{
    private Hero attacker;
    private float damage;
    private float skillDuration;
    private float arrowSpeed;
    private Rigidbody rb;
    private Vector3 moveDirection; // ทิศทางการเคลื่อนที่

    public void Initialize(Hero attacker, float damage, float skillDuration, Vector3 targetPosition, float arrowSpeed)
    {
        this.attacker = attacker;
        this.damage = damage;
        this.skillDuration = skillDuration;
        this.arrowSpeed = arrowSpeed;

        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false; //  เปิดให้ Rigidbody ใช้ฟิสิกส์
            rb.useGravity = false;  //  ปิดแรงโน้มถ่วง
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; //  ป้องกันการทะลุ

            moveDirection = (targetPosition - transform.position).normalized; //  คำนวณทิศทาง
            rb.velocity = moveDirection * arrowSpeed; //  ใช้ velocity ตามทิศทางที่ถูกต้อง
        }

        //  หมุนลูกธนูให้ตรงกับทิศทาง
        transform.rotation = Quaternion.LookRotation(moveDirection);

        StartCoroutine(DestroyAfterTime());
    }
    private void OnTriggerEnter(Collider other)
    {
        //if (!photonView.IsMine) return;

        Hero targetHero = other.GetComponent<Hero>();
        if (targetHero != null)
        {
            if (!attacker.IsOnSameTeamAs(targetHero))
            {
                targetHero.TakeDamage(damage, attacker, DamageType.Physical);
                FireballPoolManager.instance.ReturnProjectile(gameObject.name.Replace("(Clone)", "").Trim(), gameObject);
            }
        }
    }

    private IEnumerator DestroyAfterTime()
    {
        yield return new WaitForSeconds(skillDuration);
        FireballPoolManager.instance.ReturnProjectile(gameObject.name.Replace("(Clone)", "").Trim(), gameObject);
    }


}
