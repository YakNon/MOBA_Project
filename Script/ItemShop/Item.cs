using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Attack,
    Defense,
    Magic,
    Support,
    Speed
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Item/Item")]
public class Item : ScriptableObject
{
    public string itemName;
    public string description;
    public int price;
    public int sellPrice;
    public Sprite itemIcon;

    // เปลี่ยนจากประเภทเดียวเป็นหลายประเภท
    public List<ItemType> itemTypes = new List<ItemType>();

    // ค่าสเตตัสที่เพิ่ม
    public float attackDamageBonus;
    public float attackSpeedBonus;
    public float trueDamageBonus;
    public float defenseBonus;
    public float maxHealthBonus;
    public float speedBonus;
    public float manaBonus;
    public float magicBonus;
    public float megicdefenseBonus;
    [Header("Penetration Bonus")]
    public float flatPhysicalPenetration = 0f;        // เจาะเกราะกายภาพแบบค่าคงที่
    public float percentPhysicalPenetration = 0f;     // เจาะเกราะกายภาพแบบเปอร์เซ็น
    public float flatMagicPenetration = 0f;           // เจาะเกราะเวทแบบค่าคงที่
    public float percentMagicPenetration = 0f;        // เจาะเกราะเวทแบบเปอร์เซ็น

    [Header("Percentage Bonus Stats")]
    public float attackDamagePercentBonus;
    public float attackSpeedPercentBonus;
    public float defensePercentBonus;
    public float maxHealthPercentBonus;
    public float speedPercentBonus;
    public float manaPercentBonus;
    public float magicPercentBonus;
    public float magicDefPercentBonus;
    // ค่าที่เพิ่มจากเปอร์เซ็นต์ (ใช้สำหรับ Remove)
    private float percentBonusAttackDamage;
    private float percentBonusAttackSpeed;
    private float percentBonusDefense;
    private float percentBonusMaxHealth;
    private float percentBonusSpeed;
    private float percentBonusMana;
    private float percentBonusMagic;
    private float percentBonusMagicDef;



    [Range(0f, 40f)]
    public float cooldownReductionPercent;

    public bool isAttackinPhysical = true;
    public bool isAttackinMagic = false;
    public bool isAttackinTrueDamage = false;


    private bool? previousIsPhysical;
    private bool? previousIsMagic;
    private bool? previousIsTrueDamage;


    public void ApplyStats(Hero hero)
    {
        previousIsPhysical = hero.isAttackinPhysical;
        previousIsMagic = hero.isAttackinMagic;
        previousIsTrueDamage = hero.isAttackinTrueDamage;

        // เพิ่มแบบ flat
        hero.attackDamage += attackDamageBonus;
        hero.attackSpeed += attackSpeedBonus;
        hero.trueDamage += trueDamageBonus;
        hero.defense += defenseBonus;
        hero.maxHealth += maxHealthBonus;
        hero.speed += speedBonus;
        hero.mana += manaBonus;
        hero.magic += magicBonus;
        hero.magicDef += megicdefenseBonus;

        // เจาะเกราะ
        hero.flatPhysicalPenetration += flatPhysicalPenetration;
        hero.percentPhysicalPenetration += percentPhysicalPenetration;
        hero.flatMagicPenetration += flatMagicPenetration;
        hero.percentMagicPenetration += percentMagicPenetration;

        // เก็บค่าที่เพิ่มจากเปอร์เซ็นต์ แล้วเพิ่มเข้าไป
        percentBonusAttackDamage = hero.attackDamage * (attackDamagePercentBonus / 100f);
        percentBonusAttackSpeed = hero.attackSpeed * (attackSpeedPercentBonus / 100f);
        percentBonusDefense = hero.defense * (defensePercentBonus / 100f);
        percentBonusMaxHealth = hero.maxHealth * (maxHealthPercentBonus / 100f);
        percentBonusSpeed = hero.speed * (speedPercentBonus / 100f);
        percentBonusMana = hero.mana * (manaPercentBonus / 100f);
        percentBonusMagic = hero.magic * (magicPercentBonus / 100f);
        percentBonusMagicDef = hero.magicDef * (magicDefPercentBonus / 100f);

        hero.attackDamage += percentBonusAttackDamage;
        hero.attackSpeed += percentBonusAttackSpeed;
        hero.defense += percentBonusDefense;
        hero.maxHealth += percentBonusMaxHealth;
        hero.speed += percentBonusSpeed;
        hero.mana += percentBonusMana;
        hero.magic += percentBonusMagic;
        hero.magicDef += percentBonusMagicDef;

        hero.cooldownReductionPercent += cooldownReductionPercent;
        hero.cooldownReductionPercent = Mathf.Min(hero.cooldownReductionPercent, 40f);

        hero.isAttackinPhysical = isAttackinPhysical;
        hero.isAttackinMagic = isAttackinMagic;
        hero.isAttackinTrueDamage = isAttackinTrueDamage;

        hero.UpdateHealthUI();
    }



    public void RemoveStats(Hero hero)
    {
        // ลบแบบ flat
        hero.attackDamage -= attackDamageBonus;
        hero.attackSpeed -= attackSpeedBonus;
        hero.trueDamage -= trueDamageBonus;
        hero.defense -= defenseBonus;
        hero.maxHealth -= maxHealthBonus;
        hero.speed -= speedBonus;
        hero.mana -= manaBonus;
        hero.magic -= magicBonus;
        hero.magicDef -= megicdefenseBonus;

        // ลบเจาะเกราะ
        hero.flatPhysicalPenetration -= flatPhysicalPenetration;
        hero.percentPhysicalPenetration -= percentPhysicalPenetration;
        hero.flatMagicPenetration -= flatMagicPenetration;
        hero.percentMagicPenetration -= percentMagicPenetration;

        // ลบค่าที่เพิ่มจากเปอร์เซ็นต์
        hero.attackDamage -= percentBonusAttackDamage;
        hero.attackSpeed -= percentBonusAttackSpeed;
        hero.defense -= percentBonusDefense;
        hero.maxHealth -= percentBonusMaxHealth;
        hero.speed -= percentBonusSpeed;
        hero.mana -= percentBonusMana;
        hero.magic -= percentBonusMagic;
        hero.magicDef -= percentBonusMagicDef;

        // ลบ cooldown reduction
        hero.cooldownReductionPercent -= cooldownReductionPercent;
        hero.cooldownReductionPercent = Mathf.Clamp(hero.cooldownReductionPercent, 0f, 40f);

        // คืนค่าดาเมจแบบเดิม
        if (previousIsPhysical.HasValue) hero.isAttackinPhysical = previousIsPhysical.Value;
        if (previousIsMagic.HasValue) hero.isAttackinMagic = previousIsMagic.Value;
        if (previousIsTrueDamage.HasValue) hero.isAttackinTrueDamage = previousIsTrueDamage.Value;

        hero.UpdateHealthUI();
    }


    // เช็คว่าเป็นประเภทใด
    public bool HasType(ItemType type)
    {
        return itemTypes.Contains(type);
    }
}
