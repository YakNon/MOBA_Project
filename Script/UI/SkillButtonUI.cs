using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class SkillButtonUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public Image cooldownFill;
    public TextMeshProUGUI cooldownText;

    private Skill skill;
    private System.Action<Vector2> onDrag;
    private System.Action onRelease;
    private System.Action onPress;

    private bool isDragging = false;
    private bool isHolding = false;
    private float holdThreshold = 0.2f; // กำหนดเวลากดค้างก่อนจะถือว่าเป็นลาก
    private float holdTimer = 0;

    public void Setup(Skill assignedSkill, System.Action<Vector2> dragAction, System.Action releaseAction, System.Action pressAction)
    {
        skill = assignedSkill;
        onDrag = dragAction;
        onRelease = releaseAction;
        onPress = pressAction;

        if (GetComponent<Button>() != null && skill.skillIcon != null)
        {
            GetComponent<Button>().image.sprite = skill.skillIcon;
        }

        cooldownText.text = "";
        cooldownFill.fillAmount = 0;
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        if (skill.CanUse())
        {
            isHolding = true;
            holdTimer = 0;
            StartCoroutine(HoldCheck());
        }
    }

    private IEnumerator HoldCheck()
    {
        while (isHolding && holdTimer < holdThreshold)
        {
            holdTimer += Time.deltaTime;
            yield return null;
        }

        if (isHolding)
        {
            isDragging = true;
            onPress?.Invoke(); // ✅ เรียกใช้งาน Indicator
        }
    }


    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            onDrag?.Invoke(eventData.position);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;

        if (isDragging)
        {
            isDragging = false;
            onRelease?.Invoke();
        }
        else if (holdTimer < holdThreshold)
        {
            onPress?.Invoke();
        }

        StartCoroutine(StartCooldown()); // เริ่มคูลดาวเมื่อใช้สกิล
    }


    public void UpdateCooldown()
    {
        if (skill.CanUse())
        {
            cooldownText.text = "";
            cooldownFill.fillAmount = 0;
        }
    }
    IEnumerator StartCooldown()
    {
        float cooldownTime = skill.cooldown;
        float timer = cooldownTime;
        cooldownText.text = string.Format("{0:0.0}", timer);
        cooldownFill.fillAmount = 1;

        while (timer > 0)
        {
            yield return new WaitForSeconds(0.1f);
            timer -= 0.1f;
            cooldownFill.fillAmount = Mathf.Clamp01(timer / cooldownTime);
            cooldownText.text = (timer > 0) ? string.Format("{0:0.0}", timer) : "";
        }

        cooldownFill.fillAmount = 0;
        cooldownText.text = "";
    }

}
