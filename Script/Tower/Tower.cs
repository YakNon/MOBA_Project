using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
public class Tower : MonoBehaviour, IDamageable
{
    private PhotonView photonView;
    public Transform firePoint;
    public float maxHealth = 1000f;
    public float currentHealth = 1000f;
    public float attackRange = 10f;
    public float damage = 200f;
    public float defense = 20f;
    public float magicDef = 20f;
    public float attackCooldown = 2f;
    private int gold = 100;
    private float lastAttackTime;
    public GameObject projectilePrefab;

    public IDamageable currentTarget;
    private LineRenderer lineRenderer;
    private GameObject activeProjectile;

    public Canvas attackRangeCanvas;
    public Slider healthSlider;

    public Team towerTeam; // ทีมของป้อมที่กำหนดจาก Inspector
    private Hero previousTargetHero;


    void Start()
    {
        photonView = GetComponent<PhotonView>(); 
        currentHealth = maxHealth;
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.positionCount = 0;

        if (attackRangeCanvas != null) attackRangeCanvas.gameObject.SetActive(false);
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
            healthSlider.gameObject.SetActive(false);
        }
    }
    void Update()
    {
        FindTarget();

        if (attackRangeCanvas != null)
            attackRangeCanvas.gameObject.SetActive(currentTarget != null);
        if (healthSlider != null)
            healthSlider.gameObject.SetActive(currentTarget != null);

        if (currentTarget != null && Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            ShootProjectile(currentTarget);
        }

        if (currentTarget == null || (currentTarget is Hero hero && hero.isDead))
        {
            if (previousTargetHero != null)
            {
                photonView.RPC("RPC_UpdateAttackRangeColor", previousTargetHero.ownerPlayer, false);
                previousTargetHero = null; // ล้างค่าเมื่อเลิกเล็ง
            }

            currentTarget = null;
            HideAttackLine();
        }


    }

    void FindTarget()
    {
        // ถ้ามีเป้าเดิม → เช็คว่ายังอยู่ไหม
        if (currentTarget != null)
        {
            // ถ้าเป็น Hero
            if (currentTarget is Hero currentHero)
            {
                if (currentHero != null && !currentHero.isDead &&
                    Vector3.Distance(transform.position, currentHero.transform.position) <= attackRange)
                {
                    return; // ยิงใส่เป้าเดิมต่อ
                }
            }
            // ถ้าเป็น Minion
            else if (currentTarget is Minion currentMinion)
            {
                if (currentMinion != null &&
                    Vector3.Distance(transform.position, currentMinion.transform.position) <= attackRange)
                {
                    return;
                }
            }
        }

        //  เป้าเดิมตายหรือออกนอกระยะ → รีเซ็ต
        currentTarget = null;

        // หาตัวใหม่
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);
        IDamageable newTarget = null;
        Hero newTargetHero = null;

        foreach (Collider collider in hitColliders)
        {
            if (collider == null) continue;

            IDamageable target = collider.GetComponent<IDamageable>();
            if (target == null) continue;
            if (target is Hero hero)
            {
                if (hero.isDead) continue;
                if (TeamManager.instance.GetTeam(hero.photonView.Owner) == towerTeam) continue;

                newTarget = hero;
                newTargetHero = hero;
                break;
            }
            else if (target is Minion minion && minion.minionTeam != towerTeam)
            {
                newTarget = minion;
                break;
            }
        }

        //  อัปเดตสี Attack Range UI เฉพาะเป้าใหม่
        if (previousTargetHero != newTargetHero)
        {
            if (previousTargetHero != null)
            {
                photonView.RPC("RPC_UpdateAttackRangeColor", previousTargetHero.ownerPlayer, false);
            }
            if (newTargetHero != null)
            {
                photonView.RPC("RPC_UpdateAttackRangeColor", newTargetHero.ownerPlayer, true);
            }
            previousTargetHero = newTargetHero;
        }

        currentTarget = newTarget;
    }



    bool IsOnSameTeam(IDamageable target)
    {
        if (target is Hero hero)
        {
            // ตรวจสอบว่าทีมของ hero นั้นตรงกับทีมของ tower หรือไม่
            return TeamManager.instance.AreOnSameTeam(hero.photonView.Owner, PhotonNetwork.LocalPlayer) && TeamManager.instance.GetTeam(hero.photonView.Owner) == towerTeam;
        }
        else if (target is Minion minion)
        {
            return minion.minionTeam == this.towerTeam;
        }
        return false;
    }

    void ShootProjectile(IDamageable target)
    {
        StartCoroutine(ShowAttackLine(target));

        activeProjectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        ProjectileTower projectileScript = activeProjectile.GetComponent<ProjectileTower>();

        if (projectileScript != null)
        {
            projectileScript.Initialize(target, damage, this);
        }
    }
    IEnumerator ShowAttackLine(IDamageable target)
    {
        if (lineRenderer == null) yield break; // 🛑 ถ้าไม่มี lineRenderer ให้หยุดฟังก์ชัน

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, firePoint.position);

        while (target != null && activeProjectile != null)
        {
            if (target.transform == null) break; // 🛑 ถ้า target หายไป ให้หยุด loop

            //  ตรวจสอบก่อนว่า positionCount มีขนาดพอ
            if (lineRenderer.positionCount >= 2)
            {
                lineRenderer.SetPosition(1, target.transform.position);
            }

            yield return null;
        }

        HideAttackLine();
    }



    public void OnProjectileHit()
    {
        activeProjectile = null;
        HideAttackLine();
    }

    void HideAttackLine()
    {
        if(lineRenderer == null){
            return;
        }
        lineRenderer.positionCount = 0;
    }
    public void TakeDamage(float damage, IDamageable attacker, DamageType damageType)
    {
        if (currentHealth <= 0) return;
        if(attacker is Minion minionattacker){
            //Debug.Log($"🛡️ Tower Took Damage: {damage} from {minionattacker.name}");
        }
        float defenseValue = damageType == DamageType.Physical ? defense :
                            (damageType == DamageType.Magic ? magicDef : 0);
        float damageTaken = Mathf.Max(damage - defenseValue, 0);

        currentHealth -= damageTaken;
        UpdateHealthUI();

        //  แสดงตัวเลขดาเมจ
        Vector3 randomness = new Vector3(Random.Range(0f, 0.25f), Random.Range(0f, 0.25f) + 10, Random.Range(0f, 0.25f));
        DamagePopup.current.CreatPopUp(transform.position + randomness, damageTaken.ToString(), damageType, attacker, this);

        if (currentHealth <= 0)
        {
            ////Debug.Log($"{gameObject.name} ถูกทำลาย!");

            photonView.RPC("RPC_DestroyTower", RpcTarget.MasterClient, photonView.ViewID);
        }
    }


    [PunRPC]
    void RPC_DestroyTower(int viewID)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogError($" [RPC_DestroyTower] ผู้เล่นนี้ไม่ใช่ MasterClient!");
            return;
        }

        PhotonView towerView = PhotonView.Find(viewID);
        if (towerView == null)
        {
            Debug.LogError($" [RPC_DestroyTower] ไม่พบป้อมที่ ViewID: {viewID}!");
            return;
        }

        Tower tower = towerView.GetComponent<Tower>();
        if (tower == null)
        {
            Debug.LogError($" [RPC_DestroyTower] ไม่พบสคริปต์ Tower บน GameObject!");
            return;
        }

        //  แจกเงินให้ฮีโร่ทีมตรงข้าม
        Hero[] allHeroes = FindObjectsOfType<Hero>();
        foreach (Hero hero in allHeroes)
        {
            if (hero == null || hero.isDead) continue;

            Team heroTeam = TeamManager.instance.GetTeam(hero.ownerPlayer);
            if (heroTeam != tower.towerTeam) // ถ้าคนละทีมกับป้อม
            {
                hero.ReceiveGold(tower.gold);
            }
        }

        //Debug.Log($" MasterClient ทำลายป้อมที่ ViewID: {viewID} และแจก {tower.gold} gold ให้ฮีโร่ฝ่ายตรงข้าม");

        PhotonNetwork.Destroy(towerView.gameObject);
    }


    [PunRPC]
    void RPC_UpdateAttackRangeColor(bool isTargeted)
    {
        if (attackRangeCanvas != null)
        {
            Image rangeImage = attackRangeCanvas.GetComponentInChildren<Image>();
            if (rangeImage == null) return;

            rangeImage.color = isTargeted
                ? new Color32(172, 26, 0, 100)  // แดงเมื่อโดนป้อมเล็ง
                : new Color32(172, 255, 0, 100); // เขียวปกติ
        }
    }



    void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
