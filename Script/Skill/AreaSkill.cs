using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AreaSkill", menuName = "Skills/AreaSkill")]
public class AreaSkill : AimingSkill
{
    public GameObject mageSkillIndicatorPrefab;
    public float damageRadius = 6.0f;
    public float damageIncrease = 1.5f;
    public float baseDamage = 100f;
    public float effectDuration = 3f;

    public GameObject skillEffectPrefab;
    public GameObject hitEffectPrefab;

    public bool isDamageOverTime = false;

    private AreaSkillIndicatorUI skillIndicator;

    public override void Activate(Hero myHero)
    {
        if (mageSkillIndicatorPrefab != null)
        {
            GameObject skillIndicatorObj = Instantiate(mageSkillIndicatorPrefab);
            skillIndicator = skillIndicatorObj.GetComponent<AreaSkillIndicatorUI>();
            float damageAmount = baseDamage + (myHero.magic * damageIncrease);

            if (skillIndicator != null)
            {
                skillIndicator.Setup(
                    myHero, 
                    skillRange, 
                    damageRadius, 
                    damageAmount, 
                    isDamageOverTime, 
                    effectDuration,
                    skillEffectPrefab != null ? skillEffectPrefab.name : "",
                    hitEffectPrefab != null ? hitEffectPrefab.name : ""
                );
            }
            else
            {
                Debug.LogError("AreaSkillIndicatorUI not found on prefab.");
            }
        }
        else
        {
            Debug.LogError("mageSkillIndicatorPrefab not assigned.");
        }
    }

    public override void OnDrag(Vector2 dragPosition)
    {
        skillIndicator?.AimAtScreenPosition(dragPosition);
    }

    public override void OnRelease(Hero myHero)
    {
        skillIndicator?.ConfirmSkillUse();
    }
}

