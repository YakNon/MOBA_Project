using System.Collections;
using UnityEngine;
using Photon.Pun;

public class ArrowProjectile : MonoBehaviourPun
{
    private Hero attacker;
    private float damage;
    private float stunDuration;
    private Vector3 targetPosition;
    private float speed;
    private Rigidbody rb;
    private bool reachedTarget = false;
    private Vector3 startPosition;
    private float skillRange;

    public void Initialize(Hero attacker, float damage, float stunDuration, Vector3 targetPosition, float speed)
    {
        this.attacker = attacker;
        this.damage = damage;
        this.stunDuration = stunDuration;
        this.targetPosition = targetPosition;
        this.speed = speed;
        this.skillRange = attacker.skills.Find(s => s is AimingSkill) is AimingSkill aimingSkill ? aimingSkill.skillRange : 10f;

        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        reachedTarget = false;

        if (rb != null)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            rb.velocity = direction * speed;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        StartCoroutine(DestroyAfterTime()); // เริ่มนับเวลา
    }

    private void Update()
    {
        if (rb != null && rb.velocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.velocity.normalized);
        }

        if (Vector3.Distance(startPosition, transform.position) >= skillRange)
        {
            reachedTarget = true;
            ReturnToPool();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"[Arrow] Trigger enter: {other.name}, Layer: {other.gameObject.layer}");
        if (reachedTarget ) return;

        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null && !attacker.IsOnSameTeamAs(target))
        {
            target.TakeDamage(damage, attacker, DamageType.Physical);

            if (target is Hero heroTarget)
            {
                heroTarget.photonView.RPC("RPC_Stun", RpcTarget.All, stunDuration);
            }

            reachedTarget = true;
            ReturnToPool();
        }
    }

    private IEnumerator DestroyAfterTime()
    {
        yield return new WaitForSeconds(5f);
        if (!reachedTarget)
        {
            ReturnToPool();
        }
    }
    private void ReturnToPool()
    {
        string poolName = gameObject.name.Replace("(Clone)", "").Trim();
        FireballPoolManager.instance.ReturnProjectile(poolName, gameObject);
    }

    private void OnEnable() //Reset ค่าทุกครั้งเมื่อถูกเรียกใช้ใหม่จาก Pool
    {
        reachedTarget = false;

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }
    }
    private void OnDisable()// เพื่อเคลียร์ค่าก่อนเข้าคิว
    {
        if (rb != null) rb.velocity = Vector3.zero;
    }


}
