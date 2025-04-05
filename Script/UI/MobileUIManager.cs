using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class MobileUIManager : MonoBehaviour
{
    public GameObject attackButton; // ปุ่มโจมตี
    public GameObject skillButtonPrefab; // Prefab ของปุ่มสกิล
    public GameObject warpButton;
    public Transform[] skillPositions; // ตำแหน่งของปุ่มสกิล

    public GameObject healButton; // ปุ่มฮีล
    public Image healCooldownFill; // ภาพแสดงคูลดาวน์
    public TextMeshProUGUI healCooldownText; // ข้อความแสดงคูลดาวน์
    private bool isHealOnCooldown = false;
    private float healCooldown = 12f;

    private Hero playerHero;
    private List<SkillButtonUI> skillButtons = new List<SkillButtonUI>();

    void Start()
    {
        if (PhotonNetwork.LocalPlayer == null) return;

        StartCoroutine(FindPlayerHero());
    }
    IEnumerator FindPlayerHero()
    {
        while (playerHero == null) 
        {
            foreach (Hero hero in FindObjectsOfType<Hero>())
            {
                if (hero.photonView.IsMine)
                {
                    playerHero = hero;
                    break;
                }
            }
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log($" Found playerHero {playerHero.name}");
        SetupSkillButtons();

        if (attackButton != null)
        {
            attackButton.GetComponent<Button>().onClick.AddListener(() => playerHero.Attack());
        }

        if (warpButton != null)
        {
            warpButton.GetComponent<Button>().onClick.AddListener(() => playerHero.StartWarpToSpawn());
        }
        if (healButton != null)
        {
            healButton.GetComponent<Button>().onClick.AddListener(() => UseHeal());
        }
    }
    void UseHeal()
    {
        if (isHealOnCooldown) return;

        Debug.Log($" [MobileUIManager] Healing {playerHero.name}");
        playerHero.Heal(500); // ฮีล 500 HP ทันที

        if (DamagePopup.current == null)
        {
            Debug.LogError("[MobileUIManager] DamagePopup.current is NULL!");
        }
        else
        {
            // Vector3 randomness = new Vector3(Random.Range(0f, 0.25f), Random.Range(0f, 0.25f) + 10, Random.Range(0f, 0.25f));
            // Vector3 popupPosition = playerHero.transform.position + new Vector3(0, 2, 0);
            // DamagePopup.current.CreatPopUp(popupPosition+randomness, "+500", Color.green);
        }

        StartCoroutine(DelayedHeal(0.5f, 150)); // รอ 0.1 วิ แล้วฮีล 150 HP

        StartCoroutine(StartHealCooldown());
    }

    IEnumerator DelayedHeal(float delay, float amount)
    {
        yield return new WaitForSeconds(delay);
        if (playerHero != null)
        {
            Debug.Log($" [MobileUIManager] Additional Heal {amount} HP");
            playerHero.Heal(amount);
            // Vector3 ramdomness = new Vector3(Random.Range(0f, 0.25f), Random.Range(0f, 0.25f)+10, Random.Range(0f, 0.25f));
            // DamagePopup.current.CreatPopUp(transform.position + ramdomness, $"+{amount.ToString()}", Color.green);
        }
    }

    IEnumerator StartHealCooldown()
    {
        isHealOnCooldown = true;
        float cooldownTime = healCooldown;
        healCooldownFill.fillAmount = 1;
        healCooldownText.text = ((int)cooldownTime).ToString();

        while (cooldownTime > 0)
        {
            yield return new WaitForSeconds(0.1f);
            cooldownTime -= 0.1f;
            healCooldownFill.fillAmount = cooldownTime / healCooldown;
            healCooldownText.text = (cooldownTime > 0) ? ((int)cooldownTime).ToString() : "";
        }

        healCooldownFill.fillAmount = 0;
        healCooldownText.text = "";
        isHealOnCooldown = false;
    }
    void SetupSkillButtons()
    {
        if (playerHero == null || playerHero.skills == null)
        {
            Debug.LogError(" [MobileUIManager] playerHero or skills is NULL!");
            return;
        }

        for (int i = 0; i < playerHero.skills.Count; i++)
        {
            Skill skill = playerHero.skills[i];
            GameObject skillBtnObj = Instantiate(skillButtonPrefab, skillPositions[i]);
            SkillButtonUI skillBtn = skillBtnObj.GetComponent<SkillButtonUI>();
            int skillNum = i;

            if (skill is AimingSkill aimingSkill)
            {
                skillBtn.Setup(
                    skill,
                    dragPosition => aimingSkill.OnDrag(dragPosition),
                    () => aimingSkill.OnRelease(playerHero),
                    () => aimingSkill.Activate(playerHero)
                );
            }
            else
            {
                skillBtn.Setup(
                    skill,
                    null,
                    null,
                    () => playerHero.UseSkill(skillNum)
                );
            }
        }
    }


    void UseSkill(int skillIndex)
    {
        if (playerHero == null)
        {
            Debug.LogError(" [MobileUIManager] playerHero is NULL!");
            return;
        }

        Debug.Log($"🛠 [MobileUIManager] Calling useSkill({skillIndex}) on {playerHero.name}");
        playerHero.UseSkill(skillIndex);
    }



    void Update()
    {
        foreach (SkillButtonUI skillBtn in skillButtons)
        {
            skillBtn.UpdateCooldown();
        }
    }
}
