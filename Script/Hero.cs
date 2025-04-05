using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
//[RequireComponent(typeof(PhotonTransformView))]
public class Hero : MonoBehaviourPun, IDamageable
{

    public Player ownerPlayer;
    private int originalLayer;
    private bool isHidden = false;
    public Team Team => TeamManager.instance.GetTeam(photonView.Owner);
    public Sprite heroImage;
    protected int heroId;
    public Transform transform => base.transform;
    public string name;

    public float maxHealth;
    [HideInInspector] public float currentHealth;
    [HideInInspector] public string heroName;
    public Slider healthSlider;
    public Image healthFill; // ช่องสีของเลือดใน Health Bar
    public float attackRange = 4;
    public float attackDamage = 100f;
    public float trueDamage = 1f;
    public float attackSpeed = 1.0f;
    public float lastAttackTime = 0f; // เก็บเวลาโจมตีล่าสุด
    public float magic = 1f;
    public float magicDef = 0f;

    public float mana = 0f;
    public float flatPhysicalPenetration = 0f; // เจาะเกราะ
    public float percentPhysicalPenetration = 0f; // เจาะเกราะ%

    public float flatMagicPenetration = 0f;
    public float percentMagicPenetration = 0f;

    public bool isAttackinPhysical = true;
    public bool isAttackinMagic = false;
    public bool isAttackinTrueDamage = false;
    public int currentGold = 200;
    public int currentExp = 0;
    private int gold = 100;
    public float defense = 5f;
    public float speed = 10.0f;
    public GameObject rangeCanvasPrefab;  // Prefab สำหรับ RangeCanvas
    [HideInInspector] public GameObject rangeCanvasInstance; // ตัวแปรเก็บ Instance ของ RangeCanvas
    protected float rangeDisplayDuration = 0.5f;
    protected GameObject warpEffectInstance;
    protected string hitEffect = "Hit Stones hit";
    protected bl_Joystick joystick;
    protected IDamageable lastAttacker;

    protected Animator animator;
    protected CharacterController characterController;
    public float rotateVelocity = 10.0f;

    [SerializeField]
    public List<Skill> skills;
    protected Vector3 movement;

    public int killCount = 0;
    public int deathCount = 0;
    public int assistCount = 0;
    public List<Item> items = new List<Item>();
    public Transform firePoint; //for herorange
    public bool isDead = false;
    public bool isStunned = false;
    protected float respawnTime = 30f;
    protected Vector3 initialSpawnPoint;
    protected Coroutine warpCoroutine;
    protected bool isWarping = false;
    protected bool isTakingDamage = false;
    public float cooldownReductionPercent = 0f;

    protected Dictionary<Hero, float> damageReceivedFrom = new Dictionary<Hero, float>(); // บันทึกดาเมจที่ได้รับจากฮีโร่แต่ละตัว

    public void Start()
    {
        ownerPlayer = photonView.Owner;
        Time.timeScale = 1;
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        currentHealth = maxHealth;
        heroName = name;
        joystick = FindObjectOfType<bl_Joystick>();

        // ตรวจสอบว่าผู้เล่นมีทีมถูกต้องก่อนเซ็ตทีมใหม่
        if (ownerPlayer.CustomProperties.ContainsKey("team"))
        {
            Team assignedTeam = (Team)(int)ownerPlayer.CustomProperties["team"];
            //Debug.Log($" Hero {name} belongs to Team: {assignedTeam}");

            // ป้องกันการเรียก SetTeam() ซ้ำ ถ้าทีมถูกต้องอยู่แล้ว
            if (TeamManager.instance.GetTeam(ownerPlayer) != assignedTeam)
            {
                //Debug.LogWarning($"Mismatch team detected! Fixing team to {assignedTeam}");
                SetTeam(assignedTeam);
            }
        }
        else
        {
           // Debug.LogWarning($"Player {ownerPlayer.NickName} ไม่มีทีม! กำหนดให้เป็นค่าเริ่มต้น Red");
            SetTeam(Team.Red);
        }
        originalLayer = gameObject.layer;

        SetHealthBarColor();
        UpdateHealthUI();
        if (rangeCanvasPrefab != null)
        {
            rangeCanvasInstance = Instantiate(rangeCanvasPrefab, transform);
            rangeCanvasInstance.transform.position += new Vector3(0, 0.5f, 0);

            Vector3 heroScale = transform.lossyScale; // ขนาดจริงของฮีโร่
            rangeCanvasInstance.transform.localScale = new Vector3(
                ((float)attackRange / heroScale.x) * 2f,
                ((float)attackRange / heroScale.y) * 2f,
                1f
            );
            rangeCanvasInstance.SetActive(false); // ซ่อนเริ่มต้น

        }
    }
    public void ShowRangeCanvas()
    {
        if (rangeCanvasInstance != null)
        {
            rangeCanvasInstance.SetActive(true);
            StartCoroutine(HideRangeCanvasAfterDelay());
        }
    }

    IEnumerator HideRangeCanvasAfterDelay()
    {
        yield return new WaitForSeconds(rangeDisplayDuration);
        if (rangeCanvasInstance != null)
        {
            rangeCanvasInstance.SetActive(false);
        }
    }

    public void Awake()
    {
        if (TeamManager.instance == null)
        {
            Debug.LogError(" TeamManager ยังไม่ถูกสร้าง! ตรวจสอบลำดับการโหลดฉาก");
        }
    }
    public void Update()
    {
        if (isDead || !photonView.IsMine || isStunned)
        {
            return;
        }
        // ตรวจจับการกดปุ่มใช้สกิลหรือโจมตี
        if (Input.GetKeyDown(KeyCode.Alpha1)) { UseSkill(0); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { UseSkill(1); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { UseSkill(2); }
        if (Input.GetKeyDown(KeyCode.Space)) { Attack(); }
    }
    public void FixedUpdate()
    {
        if (isDead || !photonView.IsMine || isStunned)
        {
            return;
        }
        float x = joystick.Horizontal;
        float z = joystick.Vertical;
        if (TeamManager.instance.GetTeam(ownerPlayer) == Team.Red)
        {
            x = -x;
            z = -z;
        }
        if (Mathf.Abs(x) < 0.05f) x = 0;
        if (Mathf.Abs(z) < 0.05f) z = 0;

        // กำหนดทิศทางการเคลื่อนที่
        movement = new Vector3(x, 0, z).normalized;

        bool isWalking = x != 0 || z != 0;
        // photonView.RPC("RPC_SetBool", RpcTarget.All , "IsWalking", isWalking);
        animator.SetBool("IsWalking", isWalking);
        MovePlayer(movement);
        RotateCharacter();
    }



    public void SetTeam(Team team)
    {
        // อัปเดตค่าทีมใน TeamManager
        TeamManager.instance.AssignTeam(photonView.Owner, team);

        // บันทึกค่าทีมลงใน CustomProperties
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { "team", (int)team }
        };
        photonView.Owner.SetCustomProperties(props);

       // Debug.Log($"{name} set to team {team}");
    }



    public void SetHealthBarColor()
    {
        if (healthFill == null) return;

        // ตรวจสอบว่าฮีโร่เป็นของตัวเองหรือไม่
        if (photonView.IsMine)
        {
            healthFill.color = new Color(0f, 207f / 255f, 40f / 255f); // 00CF28 (สีเขียว)
        }
        else if (TeamManager.instance.AreOnSameTeam(PhotonNetwork.LocalPlayer, ownerPlayer))
        {
            healthFill.color = new Color(0f, 15f / 255f, 196f / 255f); // 000FC4 (สีน้ำเงิน)
        }
        else
        {
            healthFill.color = new Color(207f / 255f, 0f, 10f / 255f); // CF000A (สีแดง)
        }
    }

    public void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    public void MovePlayer(Vector3 direction)
    {
        //isWarping = false;
        if (characterController != null)
        {
            // เพิ่มแรงโน้มถ่วง
            if (!characterController.isGrounded)
            {
                direction.y += Physics.gravity.y * Time.deltaTime;
            }

            // เคลื่อนที่เพียงครั้งเดียว
            characterController.Move(direction * Time.deltaTime * speed);
        }
    }

    public void RotateCharacter()
    {
        if (movement != Vector3.zero)
        {
            // คำนวณทิศทางที่ต้องหมุน
            Quaternion targetRotation = Quaternion.LookRotation(movement);

            // บันทึกค่า rotation ก่อนหมุน
            float previousYRotation = transform.rotation.eulerAngles.y;

            // หมุนตัวละครแบบนุ่มนวล
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed);

        }
    }
    public void PlayAnimation(string animationTrigger)
    {
        if (photonView.IsMine)
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    return;
                }
            }
            animator.SetTrigger(animationTrigger);
            photonView.RPC("RPC_SetTrigger", RpcTarget.All, animationTrigger);
        }
    }
    [PunRPC]
    public void RPC_SetTrigger(string triggerName)
    {
        animator.SetTrigger(triggerName);
    }
    [PunRPC]
    public void RPC_SetBool(string paramName, bool value)
    {
        animator.SetBool(paramName, value);
    }

    public virtual void Attack()
    {
        if (isDead || (Time.time >= lastAttackTime + (1f / attackSpeed)) || isStunned)
        {
            isWarping = false;
            lastAttackTime = Time.time;
            PlayAnimation("Attack");
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);
            foreach (Collider collider in hitColliders)
            {
                IDamageable target = collider.GetComponent<IDamageable>();
                if (target != null && target != this && !IsOnSameTeamAs(target))
                {
                    if (isAttackinPhysical)
                    {
                        target.TakeDamage(attackDamage, this, DamageType.Physical);
                    }
                    if (isAttackinMagic) target.TakeDamage(magic, this, DamageType.Magic);
                    if (isAttackinTrueDamage) target.TakeDamage(trueDamage, this, DamageType.TrueDamage);

                }

            }
            ShowRangeCanvas();
        }
    }



    public bool IsOnSameTeamAs(IDamageable other)
    {
        if (other == null) return false;

        Team myTeam = TeamManager.instance.GetTeam(ownerPlayer);
        Team otherTeam = Team.None; // ค่าเริ่มต้น

        if (other is Hero otherHero)
        {
            otherTeam = TeamManager.instance.GetTeam(otherHero.ownerPlayer);
        }
        else if (other is Minion otherMinion)
        {
            otherTeam = otherMinion.minionTeam;
        }
        else if (other is Tower otherTower)
        {
            otherTeam = otherTower.towerTeam;
        }
        else if (other is NPC otherNPC)
        {
            otherTeam = otherNPC.NPCteam;
        }

        return myTeam == otherTeam;
    }




    public virtual void UseSkill(int skillNumber)
    {
        if (isDead || isStunned) return;
        isWarping = false;

        if (skills == null || skillNumber < 0 || skillNumber >= skills.Count)
        {
            Debug.LogError($" [Hero] Invalid skill index {skillNumber}!");
            return;
        }

        Skill skill = skills[skillNumber];
        if (skill.CanUse())
        {
           // Debug.Log($"🛠 [Hero] Using skill: {skill.skillName}");
            photonView.RPC("RPC_SetTrigger", RpcTarget.All, $"Skill{skillNumber + 1}");
            skill.Use(this); // ใช้สกิลโดยไม่ต้องส่งเป้าหมาย
        }
        // else
        // {
        //    // Debug.LogWarning($" [Hero] {skill.skillName} is on cooldown!");
        // }
    }
    public void TakeDamage(float damage, IDamageable attacker, DamageType damageType)
    {
        if (isDead) return;
        isWarping = false;

        float defenseValue = 0f;

        if (attacker is Hero attackerHero) // ถ้าโจมตีโดย Hero
        {
            if (damageType == DamageType.Physical)
            {
                float flatPen = attackerHero.flatPhysicalPenetration;
                float percentPen = attackerHero.percentPhysicalPenetration;

                float reducedDefense = Mathf.Max(defense - flatPen, 0);
                defenseValue = reducedDefense * (1f - percentPen / 100f);
            }
            else if (damageType == DamageType.Magic)
            {
                float flatPen = attackerHero.flatMagicPenetration;
                float percentPen = attackerHero.percentMagicPenetration;

                float reducedMagicDef = Mathf.Max(magicDef - flatPen, 0);
                defenseValue = reducedMagicDef * (1f - percentPen / 100f);
            }
        }
        else
        {
            // ถ้าไม่ใช่ Hero → โดนดาเมจเต็ม ไม่หักเกราะ
            defenseValue = 0f;
        }

        float damageTaken = Mathf.Max(damage - defenseValue, 0);
        currentHealth -= damageTaken;
        lastAttacker = attacker;

        if (attacker is Hero attackerHeroAgain && attackerHeroAgain != this)
        {
            if (!damageReceivedFrom.ContainsKey(attackerHeroAgain))
            {
                damageReceivedFrom[attackerHeroAgain] = 0;
            }
            damageReceivedFrom[attackerHeroAgain] += damageTaken;
        }

        Vector3 randomOffset = new Vector3(Random.Range(0f, 2.0f), Random.Range(0f, 1.5f) + 8, Random.Range(0f, 2.0f));
        DamagePopup.current.CreatPopUp(transform.position + randomOffset, damageTaken.ToString(), damageType, attacker, this);
        UpdateHealthUI();
        photonView.RPC("RPC_UpdateHealthSlider", RpcTarget.Others, currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }

        StartCoroutine(ResetTakingDamage());
    }



    public void Die()
    {
        //Debug.Log($" Hero.Die() called on: {photonView.Owner.NickName} | IsMine = {photonView.IsMine}");
       // Debug.Log($"{name} Die() called. IsMine = {photonView.IsMine}");
        int newDeathCount = deathCount + 1;
        photonView.RPC("RPC_OnDie", RpcTarget.All, true, newDeathCount);
        if (photonView.IsMine)
        {
            RespawnPanelUI.instance?.ShowRespawn(respawnTime);
            StartCoroutine(Respawn());
        }
        else
        {
            photonView.RPC("RPC_RequestRespawn", photonView.Owner);
        }

        Collider heroCollider = GetComponent<Collider>();
        if (heroCollider != null)
        {
            heroCollider.enabled = false;
        }

        photonView.RPC("RPC_RemoveAsTarget", RpcTarget.All);
        photonView.RPC("RPC_UpdateDeathCount", RpcTarget.All, newDeathCount);

        if (lastAttacker != null && lastAttacker != this)
        {
            Team attackerTeam;

            if (lastAttacker is Hero attackerHero)
            {
                KillCanvasPopup.instance.CreateKillPopup(attackerHero.heroImage.name, this.heroName);
                int newKillCount = attackerHero.killCount + 1;
                attackerHero.photonView.RPC("RPC_UpdateKillCount", RpcTarget.All, newKillCount);
                attackerTeam = TeamManager.instance.GetTeam(attackerHero.ownerPlayer);
                attackerHero.ReceiveGold(gold);
                // ตรวจสอบ Assist
                foreach (var entry in damageReceivedFrom)
                {
                    Hero assistingHero = entry.Key;
                    float totalDamageDealt = entry.Value;

                    // หากดาเมจที่ทำไปมากกว่า 30% ของ maxHealth ของเป้าหมาย ถือว่าเป็น Assist
                    if (totalDamageDealt >= maxHealth * 0.3f && assistingHero != attackerHero)
                    {
                        assistingHero.photonView.RPC("RPC_UpdateAssistCount", RpcTarget.All, assistingHero.assistCount + 1);
                    }
                }
            }
            else if (lastAttacker is Tower attackerTower)
            {
                attackerTeam = attackerTower.towerTeam;
                KillCanvasPopup.instance.CreateKillPopup("Tower", this.heroImage.name);
            }
            else if (lastAttacker is Minion attackerMinion)
            {
                attackerTeam = attackerMinion.minionTeam;
                KillCanvasPopup.instance.CreateKillPopup("Minion", this.heroImage.name);
            }
            else if (lastAttacker is NPC attackerNPC)
            {
                string[] parts = attackerNPC.name.Split('_');
                string npcName = parts[0];
                KillCanvasPopup.instance.CreateKillPopup(npcName, this.heroImage.name);
                attackerTeam = TeamManager.instance.GetTeam(ownerPlayer) == Team.Red ? Team.Blue : Team.Red;
            }
            else
            {
                return;
            }

            if (PhotonNetwork.IsMasterClient)
            {
                GameManager.instance.AddKillToTeam(attackerTeam);
            }
        }
    }
    [PunRPC]
    public void RPC_RequestRespawn()
    {
        if (photonView.IsMine)
        {
            RespawnPanelUI.instance?.ShowRespawn(respawnTime);
            StartCoroutine(Respawn());
        }
    }
    [PunRPC]
    public void RPC_OnDie(bool isDead, int deathCount)
    {
        //Debug.Log("Use RPC_OnDie");
        animator.SetBool("IsDie", isDead);
        this.isDead = isDead;
        this.deathCount = deathCount;
    }


    [PunRPC]
    public void RPC_Heal(float amount)
    {
        Heal(amount);
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        //Debug.Log($" [Hero] {name} healed {amount} HP. Current HP: {currentHealth}");
        Vector3 ramdomness = new Vector3(Random.Range(0f, 0.25f), Random.Range(0f, 0.25f) + 10, Random.Range(0f, 0.25f));
        DamagePopup.current.CreatPopUp(transform.position + ramdomness, $"+{amount.ToString()}", Color.green);
        //UpdateHealthUI();
        photonView.RPC("RPC_UpdateHealthSlider", RpcTarget.Others, currentHealth);
    }

    IEnumerator ResetTakingDamage()
    {
        yield return new WaitForSeconds(0.5f);
        isTakingDamage = false;
        isWarping = false;
    }


    public void StartWarpToSpawn()
    {
        if (isWarping) return;

        //Debug.Log($"[Hero] {name} starting warp in 4 seconds...");
        isWarping = true;

        // แสดงเอฟเฟคให้ทุกคนเห็น
        photonView.RPC("RPC_PlayWarpEffect", RpcTarget.All, transform.position);

        // แสดงเอฟเฟคเฉพาะ local และเก็บ reference
        if (photonView.IsMine)
        {
            EffectManager.instance.PlayEffectLocal("MCFreeze circle", transform.position, 4f, out warpEffectInstance);
            warpEffectInstance.transform.SetParent(transform, true);
        }

        warpCoroutine = StartCoroutine(WarpCountdown());
    }


    IEnumerator WarpCountdown()
    {
        float warpTime = 4f;
        float elapsedTime = 0f;

        while (elapsedTime < warpTime)
        {
            yield return new WaitForSeconds(0.1f);
            elapsedTime += 0.1f;

            // ถ้าถูกโจมตีระหว่างวาร์ป ให้ยกเลิกวาร์ป
            if (isTakingDamage || isWarping == false || movement != Vector3.zero)
            {
                //Debug.Log($" [Hero] {name} warp canceled due to damage!");

                // ลบเอฟเฟกต์ออกเมื่อยกเลิก
                if (warpEffectInstance != null)
                {
                    EffectManager.instance.ReleaseEffect(warpEffectInstance);
                    warpEffectInstance = null;
                }

                isWarping = false;
                warpCoroutine = null;
                yield break;
            }
        }

        // ปิด CharacterController ชั่วคราวก่อนเปลี่ยนตำแหน่ง
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        //Debug.Log($"[Hero] {name} warping to spawn point {initialSpawnPoint}!");
        transform.position = initialSpawnPoint;
        currentHealth = maxHealth;
        UpdateHealthUI();
        photonView.RPC("RPC_UpdateHealthSlider", RpcTarget.Others, currentHealth);
        

        // เปิด CharacterController กลับมา
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        isWarping = false;
        warpCoroutine = null;

        // ลบเอฟเฟกต์ออกเมื่อวาร์ปสำเร็จ
        if (warpEffectInstance != null)
        {
            EffectManager.instance.ReleaseEffect(warpEffectInstance);
            warpEffectInstance = null;
        }
    }
    [PunRPC]
    public void RPC_PlayWarpEffect(Vector3 position)
    {
        EffectManager.instance.PlayEffect("MCFreeze circle", position, 4f);
    }


    public void CancelWarp()
    {
        if (isWarping && warpCoroutine != null)
        {
           // Debug.Log($" [Hero] {name} warp canceled!");
            StopCoroutine(warpCoroutine);
            isWarping = false;
            warpCoroutine = null;
        }
    }

    public Vector3 GetSpawnPoint()
    {
        return initialSpawnPoint;
    }



    [PunRPC]
    public void RPC_SetIsDead(bool state)
    {
        isDead = state;
    }

    [PunRPC]
    public void RPC_RemoveAsTarget()
    {
        Tower[] towers = FindObjectsOfType<Tower>();
        foreach (Tower tower in towers)
        {
            if (tower.currentTarget == this)
            {
                tower.currentTarget = null; // ป้อมไม่โจมตีฮีโร่ที่ตายแล้ว
            }
        }
    }


    public void Assist()
    {
        int newAssistCount = assistCount + 1;
        photonView.RPC("RPC_UpdateAssistCount", RpcTarget.All, newAssistCount);
    }
public IEnumerator Respawn()
{
    yield return new WaitForSeconds(respawnTime);

    if (photonView.IsMine)
    {
        //Debug.Log($"{name} is respawning at {initialSpawnPoint}");

        currentHealth = maxHealth;
        transform.position = initialSpawnPoint;

        photonView.RPC("RPC_OnRespawn", RpcTarget.All, initialSpawnPoint, maxHealth);

        // เปิด Collider
        Collider heroCollider = GetComponent<Collider>();
        if (heroCollider != null) heroCollider.enabled = true;
    }
}
[PunRPC]
public void RPC_OnRespawn(Vector3 spawnPosition, float health)
{
    if (characterController != null)
    {
        characterController.enabled = false;
    }

    transform.position = spawnPosition;

    if (characterController != null)
    {
        characterController.enabled = true;
    }

    animator.SetBool("IsDie", false);
    isDead = false;
    currentHealth = health;
    UpdateHealthUI();
}



    public void ReceiveGold(int amount)
    {
        currentGold += amount;
        photonView.RPC("RPC_UpdateGold", RpcTarget.All, currentGold);
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager != null)
        {
            shopManager.UpdateGoldDisplay();
        }
    }

    public void ReceiveExp(int amount)
    {
        currentExp += amount;
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    [PunRPC]
    public void RPC_UpdateHealthSlider(float newHealth)
    {
        currentHealth = newHealth;
        UpdateHealthUI();
    }

    [PunRPC]
    public void RPC_UpdateGold(int newGold)
    {
        currentGold = newGold;
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager != null)
        {
            shopManager.UpdateGoldDisplay();
        }
        FindObjectOfType<HeroStatusUIManager>()?.UpdateHeroStatus();//
    }
    [PunRPC]
    public void RPC_AddItem(string itemName)
    {
        Item item = FindItemByName(itemName);
        if (item != null)
        {
            items.Add(item);
            item.ApplyStats(this);

            if (photonView.IsMine) // ให้ Local Player เท่านั้นที่อัปเดต UI
            {
                ShopManager shopManager = FindObjectOfType<ShopManager>();
                if (shopManager != null)
                {
                    shopManager.UpdateGoldDisplay();
                    shopManager.AddItemToMyItems(item);
                }
            }
            FindObjectOfType<HeroStatusUIManager>()?.UpdateHeroStatus();//
        }
    }
    [PunRPC]
    public void RPC_RemoveItem(string itemName)
    {
        // ค้นหาไอเท็มใน inventory
        Item itemToRemove = items.Find(item => item.itemName == itemName);
        if (itemToRemove != null)
        {
            items.Remove(itemToRemove); // ลบไอเท็มออกจาก inventory
            itemToRemove.RemoveStats(this); // ลดค่าค่าสเตตัสที่ไอเท็มเพิ่มให้
            // อัปเดต UI ทั้ง Shop และ Hero Status UI
            if (photonView.IsMine)
            {
                ShopManager shopManager = FindObjectOfType<ShopManager>();
                if (shopManager != null)
                {
                    shopManager.UpdateGoldDisplay();
                    //shopManager.RefreshInventoryUI();
                }
            }
            FindObjectOfType<HeroStatusUIManager>()?.UpdateHeroStatus();
        }
    }



    public Item FindItemByName(string itemName)
    {
        // ค้นหา ShopManager ในฉาก
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager != null)
        {
            // ค้นหาไอเท็มจาก availableItems ใน ShopManager
            foreach (Item item in shopManager.availableItems)
            {
                if (item.itemName == itemName)
                {
                    return item;
                }
            }
        }
        return null; // ถ้าไม่เจอไอเท็ม
    }

    [PunRPC]
    public void RPC_UpdateKillCount(int newKillCount)
    {
        killCount = newKillCount;
        UpdateStatusUI();

        HeroStatsUI statsUI = FindObjectOfType<HeroStatsUI>();
        if (statsUI != null)
        {
            statsUI.ForceUpdate();
        }
        //FindObjectOfType<HeroStatusUIManager>()?.UpdateHeroStatus();//
    }

    [PunRPC]
    public void RPC_UpdateDeathCount(int newDeathCount)
    {
        deathCount = newDeathCount;
        UpdateStatusUI();

        HeroStatsUI statsUI = FindObjectOfType<HeroStatsUI>();
        if (statsUI != null)
        {
            statsUI.ForceUpdate();
        }
        //FindObjectOfType<HeroStatusUIManager>()?.UpdateHeroStatus();//
    }

    [PunRPC]
    public void RPC_UpdateAssistCount(int newAssistCount)
    {
        assistCount = newAssistCount;
        UpdateStatusUI();

        HeroStatsUI statsUI = FindObjectOfType<HeroStatsUI>();
        if (statsUI != null)
        {
            statsUI.ForceUpdate();
        }
        //FindObjectOfType<HeroStatusUIManager>()?.UpdateHeroStatus();//
    }
    public float GetTotalDamageDealt()
    {
        float totalDamage = 0;

        foreach (var entry in damageReceivedFrom)
        {
            totalDamage += entry.Value;
        }

        return totalDamage;
    }
    [PunRPC]
    public void RPC_CreateSkillEffect(string effectName, Vector3 position, float duration)
    {
        EffectManager.instance.PlayEffect(effectName, position, duration);
    }
    [PunRPC]
    public void RPC_CreateSkillEffect(string effectName, Vector3 position, float duration, float range)
    {
        GameObject effectObj = EffectManager.instance.PlayEffect(effectName, position, duration);

        if (effectObj != null)
        {
            // ปรับขนาดของ effect ตาม range
            Vector3 originalScale = effectObj.transform.localScale;
            effectObj.transform.localScale = new Vector3(
                originalScale.x * range,
                originalScale.y * range,
                originalScale.z * range
            );
        }
        else
        {
            Debug.LogError($" ไม่สามารถสร้างเอฟเฟค {effectName} จาก Pool ได้");
        }
    }

    [PunRPC]
    public void RPC_CreateSkillEffectChild(string effectPrefabName, int heroViewID, float effectDuration, float skillRange)
    {
        PhotonView heroPhotonView = PhotonView.Find(heroViewID);
        if (heroPhotonView == null)
        {
            Debug.LogError($" ไม่พบฮีโร่ที่มี ViewID {heroViewID}");
            return;
        }

        Hero targetHero = heroPhotonView.GetComponent<Hero>();
        if (targetHero == null)
        {
            Debug.LogError($" GameObject ไม่ใช่ Hero");
            return;
        }

        Transform heroTransform = targetHero.transform;

        // ดึงเอฟเฟคจาก Object Pool
        GameObject pooledEffect = EffectManager.instance.PlayEffect(effectPrefabName, heroTransform.position, effectDuration, heroTransform);
        if (pooledEffect == null)
        {
            Debug.LogError($" ไม่พบเอฟเฟคใน Pool: {effectPrefabName}");
            return;
        }

        //  ปรับสเกลเอฟเฟคตาม skillRange
        Vector3 originalScale = pooledEffect.transform.localScale;
        Vector3 heroScale = heroTransform.lossyScale;

        pooledEffect.transform.localScale = new Vector3(
            originalScale.x * (skillRange / heroScale.x),
            originalScale.y * (skillRange / heroScale.y),
            originalScale.z * (skillRange / heroScale.z)
        );
    }
    [PunRPC]
    public void RPC_CreateSkillEffectChild(string effectPrefabName, int heroViewID, float effectDuration)
    {
        PhotonView heroPhotonView = PhotonView.Find(heroViewID);
        if (heroPhotonView == null)
        {
            Debug.LogError($" ไม่พบฮีโร่ที่มี ViewID {heroViewID}");
            return;
        }

        Hero targetHero = heroPhotonView.GetComponent<Hero>();
        if (targetHero == null)
        {
            Debug.LogError($" GameObject ไม่ใช่ Hero");
            return;
        }

        Transform heroTransform = targetHero.transform;

        // ดึงเอฟเฟคจาก Object Pool
        GameObject pooledEffect = EffectManager.instance.PlayEffect(effectPrefabName, heroTransform.position, effectDuration, heroTransform);
        if (pooledEffect == null)
        {
            Debug.LogError($" ไม่พบเอฟเฟคใน Pool: {effectPrefabName}");
            return;
        }
        pooledEffect.transform.localScale =  Vector3.one;
    }
    [PunRPC]
    public void RPC_CreateHitEffect(string effectName, Vector3 position, float duration)
    {        
        GameObject pooledEffect = EffectManager.instance.PlayEffect(effectName, position, duration);
        if (pooledEffect == null)
        {
            Debug.LogError($" ไม่พบเอฟเฟค {effectName} ใน EffectManager!");
            return;
        }

        pooledEffect.transform.localScale = Vector3.one; 
    }

    [PunRPC]
    public void RPC_BlinkToPosition(Vector3 newPosition, string blinkEffectPrefab)
    {
        //เล่นเอฟเฟคตอนเริ่มวาร์ป
        if (blinkEffectPrefab != null)
        {
            photonView.RPC("RPC_CreateSkillEffect", RpcTarget.All, blinkEffectPrefab, transform.position, 0.5f);
        }

        //  ปิด CharacterController ชั่วคราว
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        transform.position = newPosition; //  เปลี่ยนตำแหน่งจริงๆ
        photonView.RPC("RPC_SyncPosition", RpcTarget.Others, newPosition);

        //  เปิด CharacterController กลับมา
        if (controller != null)
        {
            controller.enabled = true;
        }

        //เล่นเอฟเฟคหลังวาร์ปเสร็จ
        if (blinkEffectPrefab != null)
        {
            photonView.RPC("RPC_CreateSkillEffect", RpcTarget.All, blinkEffectPrefab, newPosition, 0.5f);
        }
    }
    [PunRPC]
    public void RPC_SyncPosition(Vector3 syncedPosition)
    {
        transform.position = syncedPosition;
    }
    [PunRPC]
    public void RPC_BlinkAttack(Vector3 newPosition, string blinkEffectPrefab, string attackEffectPrefab, float attackSkillRange, float skillDamage, DamageType damageType)
    {
        //  เล่นเอฟเฟควาร์ปก่อนย้ายตำแหน่ง
        if (blinkEffectPrefab != null)
        {
            //photonView.RPC("RPC_CreateSkillEffect", RpcTarget.All, blinkEffectPrefab, transform.position, 0.5f);
        }

        //  ปิด CharacterController ก่อนย้ายตำแหน่ง
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        transform.position = newPosition;
        photonView.RPC("RPC_SyncPosition", RpcTarget.Others, newPosition);

        // เปิด CharacterController กลับมา
        if (controller != null)
        {
            controller.enabled = true;
        }

        // เล่นอนิเมชัน BlinkAttack
        PlayAnimation("BlinkAttack");

        // เล่นเอฟเฟคโจมตี
        if (attackEffectPrefab != null)
        {
            photonView.RPC("RPC_CreateSkillEffect", RpcTarget.All, attackEffectPrefab, transform.position, 1.0f);
        }

        // หาเป้าหมายทั้งหมดที่อยู่ในระยะ โดยไม่ใช้ enemyLayer
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackSkillRange);
        foreach (Collider collider in hitColliders)
        {
            IDamageable target = collider.GetComponent<IDamageable>();

            //  ตรวจสอบว่าเป้าหมายเป็นศัตรู ไม่ใช่ตัวเอง และเป็น IDamageable
            if (target != null && target != this && !IsOnSameTeamAs(target))
            {
                target.TakeDamage(skillDamage, this, damageType);
            }
        }
    }

    [PunRPC]
    public void RPC_ApplyBuff(float attackSpeedBoost, float attackRangeBoost, float attackPowerBoost,
                            float magicPowerBoost, float trueDamageBoost, float physicalDefenseBoost,
                            float magicDefenseBoost, float movementSpeedBoost, float duration, string buffEffectPrefabName)
    {
        StartCoroutine(ApplyBuff(attackSpeedBoost, attackRangeBoost, attackPowerBoost,
                                magicPowerBoost, trueDamageBoost, physicalDefenseBoost,
                                magicDefenseBoost, movementSpeedBoost, duration, buffEffectPrefabName));
    }

    private Dictionary<string, float> originalStats = new Dictionary<string, float>();

    private IEnumerator ApplyBuff(float attackSpeedBoost, float attackRangeBoost, float attackPowerBoost,
                                float magicPowerBoost, float trueDamageBoost, float physicalDefenseBoost,
                                float magicDefenseBoost, float movementSpeedBoost, float duration, string buffEffectPrefabName)
    {
        Debug.Log($" {name} ได้รับ Buff!");

        // เช็คว่ามีการบันทึกค่าตั้งต้นหรือยัง ถ้ายังให้บันทึกไว้
        if (!originalStats.ContainsKey("speed"))
        {
            originalStats["attackSpeed"] = attackSpeed;
            originalStats["attackRange"] = attackRange;
            originalStats["attackDamage"] = attackDamage;
            originalStats["magic"] = magic;
            originalStats["trueDamage"] = trueDamage;
            originalStats["defense"] = defense;
            originalStats["magicDef"] = magicDef;
            originalStats["speed"] = speed;
        }

        // ใช้ค่าตั้งต้นเป็นฐานในการคำนวณ
        attackSpeed = originalStats["attackSpeed"] * (1 + attackSpeedBoost);
        attackRange = originalStats["attackRange"] * (1 + attackRangeBoost);
        attackDamage = originalStats["attackDamage"] * (1 + attackPowerBoost);
        magic = originalStats["magic"] * (1 + magicPowerBoost);
        trueDamage = originalStats["trueDamage"] * (1 + trueDamageBoost);
        defense = originalStats["defense"] * (1 + physicalDefenseBoost);
        magicDef = originalStats["magicDef"] * (1 + magicDefenseBoost);
        speed = originalStats["speed"] * (1 + movementSpeedBoost); // ใช้ค่าดั้งเดิมแทนค่าที่ถูกบัฟแล้ว

        // แสดงเอฟเฟค Buff
        if (!string.IsNullOrEmpty(buffEffectPrefabName))
        {
            photonView.RPC("RPC_CreateSkillEffectChild", RpcTarget.All, buffEffectPrefabName, photonView.ViewID, duration);
        }

        yield return new WaitForSeconds(duration);

        // คืนค่าตั้งต้นกลับมา
        attackSpeed = originalStats["attackSpeed"];
        attackRange = originalStats["attackRange"];
        attackDamage = originalStats["attackDamage"];
        magic = originalStats["magic"];
        trueDamage = originalStats["trueDamage"];
        defense = originalStats["defense"];
        magicDef = originalStats["magicDef"];
        speed = originalStats["speed"];  // คืนค่าความเร็วกลับมาให้ถูกต้อง

        Debug.Log($" Buff ของ {name} หมดอายุแล้ว!");
    }








    [PunRPC]
    public void RPC_Stun(float duration)
    {
        StartCoroutine(StunCoroutine(duration));
    }

    private IEnumerator StunCoroutine(float duration)
    {
        Debug.Log($" {name} ถูกสตั๊นเป็นเวลา {duration} วินาที!");
        isStunned = true;

        yield return new WaitForSeconds(duration);

        isStunned = false;
        Debug.Log($"{name} หายจากสตั๊นแล้ว!");
    }
    [PunRPC]
    public void RPC_ApplySlow(float slowPercentage, float duration)
    {
        StartCoroutine(ApplySlowEffect(slowPercentage, duration));
    }

    private IEnumerator ApplySlowEffect(float slowPercentage, float duration)
    {
        float originalSpeed = speed;
        speed *= (1 - slowPercentage);

        yield return new WaitForSeconds(duration);

        speed = originalSpeed;
    }
    [PunRPC]
    public void RPC_ApplyRemoveDebuff(int heroID, float duration)
    {
        PhotonView targetHeroView = PhotonView.Find(heroID);
        if (targetHeroView == null) return;

        Hero targetHero = targetHeroView.GetComponent<Hero>();
        if (targetHero == null) return;

        targetHero.StartCoroutine(targetHero.RemoveAllDebuffs(duration));
    }

    public IEnumerator RemoveAllDebuffs(float duration)
    {
        Debug.Log($"{name} ใช้สกิลลบดีบัฟ! ป้องกันดีบัฟเป็นเวลา {duration} วิ!");

        // ยกเลิกสถานะผิดปกติทั้งหมด
        isStunned = false;
        isTakingDamage = false;

        // ให้เป็นอมตะต่อดีบัฟ
        bool isImmuneToDebuff = true;

        // แสดงเอฟเฟคป้องกันดีบัฟ
        if (!string.IsNullOrEmpty("CA Buff"))
        {
            photonView.RPC("RPC_CreateSkillEffectChild", RpcTarget.All, "CA Buff", photonView.ViewID, duration);
        }

        yield return new WaitForSeconds(duration);

        isImmuneToDebuff = false;
        Debug.Log($" {name} หมดเวลาป้องกันดีบัฟแล้ว!");
    }


    [PunRPC]
    public void RPC_EnterGrass()
    {
        if (!photonView.IsMine) return; // แค่เจ้าของตัวละครเท่านั้นที่ควบคุมการเปลี่ยน Layer

        Team myTeam = TeamManager.instance.GetTeam(photonView.Owner);
        Team localTeam = TeamManager.instance.GetTeam(PhotonNetwork.LocalPlayer);

        if (myTeam != localTeam)
        {
            gameObject.layer = LayerMask.NameToLayer("HiddenInGrass");
            isHidden = true;
        }
    }

    [PunRPC]
    public void RPC_ExitGrass()
    {
        if (!photonView.IsMine) return;

        gameObject.layer = originalLayer;
        isHidden = false;
    }








    // ใช้ฟังก์ชันนี้เพื่ออัปเดต UI ทันทีหลังจากได้รับค่าใหม่
    public void UpdateStatusUI()
    {
        HeroStatusUIManager statusUIManager = FindObjectOfType<HeroStatusUIManager>();
        if (statusUIManager != null)
        {
            statusUIManager.UpdateHeroStatus();
        }
    }
    public void SetInitialSpawnPoint(Vector3 spawnPoint)
    {
        initialSpawnPoint = spawnPoint;
    }
}
