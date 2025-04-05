// using System.Collections;
// using UnityEngine;
// using Photon.Pun;

// [CreateAssetMenu(fileName = "NewMultiDamageBuff", menuName = "Skills/MultiDamageBuff")]
// public class MultiDamageBuff : Skill
// {
//     public float physicalDamageMultiplier = 1f; //  ทำดาเมจกายภาพ = พลังโจมตีของฮีโร่
//     public float magicDamageMultiplier = 1f; //  ทำดาเมจเวท = พลังเวทของฮีโร่
//     public float trueDamageMultiplier = 0.5f; //  ทำดาเมจจริง = พลังโจมตี * 0.5
//     public float attackSpeedIncrease = 2f; //  เพิ่มความเร็วโจมตี +2
//     public float moveSpeed  = 2f;
//     public float duration = 5f; //  ระยะเวลาสกิล 5 วิ

//     public GameObject skillEffectPrefab; //  เอฟเฟคแสดงผลขณะ Buff ทำงาน

//     public override void Activate(Hero myHero)
//     {
//         myHero.StartCoroutine(ApplyMultiDamageBuff(myHero));
//     }

// private IEnumerator ApplyMultiDamageBuff(Hero myHero)
// {
//     Debug.Log($" {myHero.name} ใช้ Multi-Damage Buff!");

//     //  บันทึกค่าเดิมของฮีโร่ก่อนเพิ่มบัฟ
//     float originalAttackSpeed = myHero.attackSpeed;
//     float originalMoveSpeed = myHero.speed;
//     System.Action originalAttackFunction = myHero.Attack; // บันทึกฟังก์ชันโจมตีเดิม

//     //  เพิ่มความเร็วโจมตีและเคลื่อนที่
//     myHero.attackSpeed += attackSpeedIncrease;
//     myHero.speed += moveSpeed;

//     //  เปลี่ยนให้ฮีโร่ใช้ BuffedAttack แทน Attack ปกติ
//     myHero.Attack = myHero.BuffedAttack;

//     //  เล่นเอฟเฟคสกิลถ้ามี
//     if (skillEffectPrefab != null)
//     {
//         myHero.photonView.RPC("RPC_CreateSkillEffectChild", RpcTarget.All, skillEffectPrefab.name, myHero.photonView.ViewID, duration);
//     }

//     Debug.Log($" Buff Active: {duration} วินาที!");

//     yield return new WaitForSeconds(duration);

//     //  รีเซ็ตค่าการโจมตีกลับเป็นแบบปกติ
//     myHero.attackSpeed = originalAttackSpeed;
//     myHero.speed = originalMoveSpeed;

//     //  คืนค่าโจมตีเป็นแบบเดิม
//     myHero.Attack = originalAttackFunction;

//     Debug.Log($" Buff Expired: กลับไปโจมตีแบบปกติ!");
// }

// }
