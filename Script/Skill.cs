using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "Skills/Skill")]
public abstract class Skill : ScriptableObject
{
    public string skillName;
    public Sprite  skillIcon;
    public int cooldown; 
    private float lastUsedTime;
    protected Hero SkillOwner;

    void OnEnable()
    {
        lastUsedTime = Time.time - cooldown;
    }
    public bool CanUse()
    {
        float effectiveCooldown = cooldown;
        
        if (SkillOwner != null)
        {
            effectiveCooldown = cooldown * (1f - Mathf.Clamp01(SkillOwner.cooldownReductionPercent / 100f));
        }

        return Time.time >= lastUsedTime + effectiveCooldown;
    }

    public void Use(Hero myHero)
    {
        SkillOwner = myHero;
        lastUsedTime = Time.time;
        Activate(myHero);
    }

    public abstract void Activate(Hero myHero);
    public virtual void UpdateAiming(Hero myHero, Vector3 targetPosition)
    {
        Debug.Log($"🎯 [Skill] {skillName} aiming at {targetPosition}");
    }

}
