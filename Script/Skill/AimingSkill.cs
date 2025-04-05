using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Aiming Skill", menuName = "Skills/AimingSkill")]
public abstract class AimingSkill : Skill
{
    public GameObject indicatorCanvasPrefab; // Indicator สำหรับเล็ง
    protected GameObject indicatorInstance;
    protected IndicatorController indicatorController;
    public float skillRange = 12f;

    public abstract void OnDrag(Vector2 dragPosition);
    public abstract void OnRelease(Hero myHero);

    public override void Activate(Hero myHero)
    {
        if (indicatorCanvasPrefab == null)
        {
            Debug.LogError($"❌ {skillName}: indicatorCanvasPrefab ยังไม่ได้ตั้งค่า!");
            return;
        }

        if (indicatorInstance == null)
        {
            indicatorInstance = Instantiate(indicatorCanvasPrefab, myHero.transform);
            indicatorInstance.transform.localPosition = new Vector3(0, 0.5f, 0);

            Vector3 heroScale = myHero.transform.lossyScale; // ขนาดจริงของฮีโร่
            indicatorInstance.transform.localScale = new Vector3(
                ((float)skillRange / heroScale.x) * 2f,
                ((float)skillRange / heroScale.y) * 2f,
                1f
            );
            indicatorInstance.transform.rotation = Quaternion.Euler(90, 0, 0);
            indicatorController = indicatorInstance.GetComponent<IndicatorController>();
        }
        indicatorInstance.SetActive(true);
    }
}
