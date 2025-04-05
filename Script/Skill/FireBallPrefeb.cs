using System.Collections;
using UnityEngine;

public class FireBallPrefab : MonoBehaviour
{
    private Hero attacker;
    private float damage;
    private float range;
    private float speed;
    private float slowPercentage;
    private float slowDuration;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 moveDirection;
    private Rigidbody rb;
    private bool reachedTarget = false;

    private string projectilePoolName = "fireball";

    public void Initialize(Hero attacker, float damage, float range, Vector3 targetPosition, float speed, float slowPercentage, float slowDuration)
    {
        this.attacker = attacker;
        this.damage = damage;
        this.range = range;
        this.targetPosition = targetPosition;
        this.speed = speed;
        this.slowPercentage = slowPercentage;
        this.slowDuration = slowDuration;

        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;

        if (rb != null)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            rb.velocity = direction * speed;

            // ✅ ทำให้ลูกบอลไฟหันไปตามทิศที่พุ่ง
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // เริ่มนับเวลาเพื่อถอด projectile
        StartCoroutine(DestroyAfterTime(5f));
    }

    private void Update()
    {
        if (rb != null && rb.velocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.velocity.normalized);
        }

        if (Vector3.Distance(startPosition, transform.position) >= range)
        {
            reachedTarget = true;
            ReturnToPool();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (reachedTarget) return;

        IDamageable target = other.GetComponent<IDamageable>();

        if (target != null && !attacker.IsOnSameTeamAs(target))
        {
            target.TakeDamage(damage, attacker, DamageType.Magic);

            if (target is Hero heroTarget)
            {
                heroTarget.photonView.RPC("RPC_ApplySlow", Photon.Pun.RpcTarget.All, slowPercentage, slowDuration);
            }
            else if (target is NPC npcTarget)
            {
                npcTarget.photonView.RPC("RPC_ApplySlow", Photon.Pun.RpcTarget.All, slowPercentage, slowDuration);
            }
            else if (target is Minion minionTarget)
            {
                minionTarget.photonView.RPC("RPC_ApplySlow", Photon.Pun.RpcTarget.All, slowPercentage, slowDuration);
            }

            reachedTarget = true;
            ReturnToPool();
        }
    }

    private IEnumerator DestroyAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        if (!reachedTarget)
        {
            reachedTarget = true;
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        rb.velocity = Vector3.zero;
        FireballPoolManager.instance.ReturnProjectile(projectilePoolName, gameObject);
    }
}
