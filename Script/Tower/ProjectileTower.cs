using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileTower : MonoBehaviour
{
    public float speed = 10f;
    public IDamageable target;
    private float damage;
    private Rigidbody rb;
    private Tower originatingTower; // เพิ่มตัวแปรสำหรับ Tower

    public void Initialize(IDamageable target, float damage, Tower tower)
    {
        this.target = target;
        this.damage = damage;
        this.originatingTower = tower; // บันทึก Tower ที่สร้างโปรเจคไทล์นี้
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (target == null || (target is MonoBehaviour && ((MonoBehaviour)target) == null))
        {
            Destroy(gameObject);
            return;
        }
        if (target == null || (target is Hero hero && hero.isDead)) 
        {
            Destroy(gameObject); // ถ้าเป้าหมายตายแล้ว ทำลายกระสุน
            return;
        }

        Vector3 direction = (target.transform.position - transform.position).normalized;
        rb.velocity = direction * speed;
    }
    void FixedUpdate()
    {
        if (target == null || (target is MonoBehaviour && ((MonoBehaviour)target) == null))
        {
            Destroy(gameObject);
        }
    }


    void OnTriggerEnter(Collider other)
    {
        //Debug.LogError($"Projectile hit: {other.gameObject.name}");
        if (target == null || (target is MonoBehaviour && ((MonoBehaviour)target) == null))
        {
            Destroy(gameObject);
            return;
        }

        IDamageable hitTarget = other.GetComponent<IDamageable>();
        // ถ้าโปรเจคไทล์ชน Tower ของตัวเอง ให้ข้ามไป
        if (hitTarget == originatingTower)
        {
           // Debug.LogError("Cant Damaged Your Mom!!!!!!!!");
            return; // ไม่ทำอะไรเลย และให้โปรเจคไทล์เคลื่อนที่ต่อ
            
        }
        if (hitTarget != null && hitTarget == target && hitTarget != originatingTower)
        {
            hitTarget.TakeDamage(damage, originatingTower, DamageType.Physical);
            originatingTower?.OnProjectileHit();
            Destroy(gameObject);
        }
    }

}
