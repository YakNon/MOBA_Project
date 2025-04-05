using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DamageType
{
    Physical,
    Magic,
    TrueDamage // ดาเมจจริงที่ทะลุค่าป้องกัน
}

public interface IDamageable
{
    void TakeDamage(float damage, IDamageable attacker, DamageType damageType); // ✅ รองรับประเภทดาเมจ
    Transform transform { get; } 
}
