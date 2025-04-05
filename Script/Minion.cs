using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI; 
using Photon.Pun;
using Photon.Realtime;

public class Minion : MonoBehaviourPun , IDamageable, IPunInstantiateMagicCallback
{
    public string name; 
    public Team minionTeam;
    public Transform[] waypoints; // จุดที่มินเนี่ยนจะเดินตามลำดับ
    private int currentWaypointIndex = 0;

    public Hero targetHero;
    public Tower targetTower;
    public Minion targetMinion;

    private NavMeshAgent agent;
    public float movementSpeed = 3.5f;
    public float visionRange = 4f; // ระยะการมองเห็นของมินเนี่ยน

    private bool isAttacking = false; // ใช้ตรวจสอบว่ากำลังโจมตีอยู่หรือไม่
    public Transform transform => base.transform;
    public Transform spawnPoint ;
    public float spawnTime;
    
    public float maxHealth;
    public float currentHealth; 
    public float defense = 5f;
    public float magicDef = 5f;
    public int gold = 10;
    public int exp = 10;
    public Slider healthSlider;   
    public float attackDamage = 10f;
    public float attackRange = 2f;
    protected Animator animator; 
    protected float attackCooldown = 2f; 
    protected float lastAttackTime; 
    public event System.Action OnDeath;

    protected Hero lastAttacker;  
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        if (info.photonView.InstantiationData != null && info.photonView.InstantiationData.Length > 0)
        {
            minionTeam = (Team)(int)info.photonView.InstantiationData[0];
            SetHealthBarColor(); // ตั้งสี UI ตามทีม
        }
    }

    [PunRPC]
    public void RPC_PlayAnimation(string animationTrigger)
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
    }
    [PunRPC]
    public void RPC_SetWalkingAnimation(bool isWalking)
    {
        animator.SetBool("IsWalking", isWalking);
    }
    public void SetHealthBarColor()
    {
        if (healthSlider == null) return;

        //  หา GameObject "Fill" ที่อยู่ภายใต้ "Fill Area"
        Transform fillArea = healthSlider.transform.Find("Fill Area");
        if (fillArea == null) return; // ถ้าไม่พบ "Fill Area" ให้ออกจากฟังก์ชัน

        Transform fillTransform = fillArea.Find("Fill");
        if (fillTransform == null) return; // ถ้าไม่พบ "Fill" ให้ออกจากฟังก์ชัน

        Image healthFill = fillTransform.GetComponent<Image>();
        if (healthFill == null) return; // ถ้าไม่พบ `Image` ให้ออกจากฟังก์ชัน

        //  ตรวจสอบทีมของผู้เล่น
        Team playerTeam = TeamManager.instance.GetTeam(PhotonNetwork.LocalPlayer);
        
        if (minionTeam == playerTeam)
        {
            healthFill.color = new Color(0f, 15f / 255f, 196f / 255f); //  สีน้ำเงิน (000FC4)
        }
        else
        {
            healthFill.color = new Color(207f / 255f, 0f, 10f / 255f); //  สีแดง (CF000A)
        }
    }


    void Start()
    {

        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
        agent.speed = movementSpeed;
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError($" Minion {name} ไม่มี Animator! ตรวจสอบว่า GameObject มี Animator Component อยู่หรือไม่.");
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        //SetHealthBarColor();
        UpdateHealthUI();

        // เพิ่ม SphereCollider เพื่อใช้เป็นระยะโจมตี
        SphereCollider attackCollider = gameObject.AddComponent<SphereCollider>();
        attackCollider.radius = attackRange; // ใช้ attackRange ที่กำหนดไว้
        attackCollider.isTrigger = true; // ต้องเป็น trigger เพื่อให้ตรวจจับวัตถุอื่นได้

        if (waypoints.Length > 0)
        {
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
        if (photonView.IsMine) 
        {
            StartCoroutine(AI_Behavior());
        }
    }
    IEnumerator AI_Behavior()
    {
        while (true)
        {
            if (!photonView.IsMine) yield break; // ให้มินเนี่ยนที่ตัวเองเป็นเจ้าของเท่านั้นที่ทำงาน

            FindTarget();

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                if (targetHero != null)
                {
                    // เช็คว่าเป้าหมายยังอยู่และไม่ตาย
                    if (targetHero.isDead || Vector3.Distance(transform.position, targetHero.transform.position) > visionRange)
                    {
                        ResetTarget();
                    }
                    else
                    {
                        agent.SetDestination(targetHero.transform.position);

                        if (Vector3.Distance(transform.position, targetHero.transform.position) <= attackRange)
                        {
                            Attack(targetHero);
                        }
                    }
                }

                else if (targetTower != null)
                {
                    agent.SetDestination(targetTower.transform.position);
                    if (Vector3.Distance(transform.position, targetTower.transform.position) <= attackRange)
                    {
                        Attack(targetTower);
                    }
                    else ResetTarget();
                }
                else if (targetMinion != null)
                {
                    agent.SetDestination(targetMinion.transform.position);
                    if (Vector3.Distance(transform.position, targetMinion.transform.position) <= attackRange)
                    {
                        Attack(targetMinion);
                    }
                    else ResetTarget();
                }
                else
                {
                    FollowWaypoints();
                }
            }

            yield return new WaitForSeconds(0.5f); // ตรวจสอบเป้าหมายทุก 0.5 วินาที
        }
    }
    void FindTarget()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, visionRange);
        foreach (Collider collider in hitColliders)
        {
            Hero hero = collider.GetComponent<Hero>();
            Tower tower = collider.GetComponent<Tower>();
            Minion enemyMinion = collider.GetComponent<Minion>();

            if (hero != null && TeamManager.instance.GetTeam(hero.photonView.Owner) != minionTeam)
            {
                targetHero = hero;
                return;
            }
            else if (tower != null && tower.towerTeam != minionTeam)
            {
                targetTower = tower;
                return;
            }
            else if (enemyMinion != null && enemyMinion.minionTeam != minionTeam)
            {
                targetMinion = enemyMinion;
                return;
            }
        }
    }

    void StartAttack(IDamageable target)
    {
        isAttacking = true;
        agent.isStopped = true; // หยุดการเคลื่อนที่ชั่วคราวขณะโจมตี
        Attack(target);
    }

    void ResetTarget()
    {
        targetHero = null;
        targetTower = null;
        targetMinion = null;
        isAttacking = false;
        agent.isStopped = false;
    }

    void FollowWaypoints()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            if (currentWaypointIndex < waypoints.Length - 1) //  หยุดที่ waypoint สุดท้าย
            {
                currentWaypointIndex++;
                agent.SetDestination(waypoints[currentWaypointIndex].position);
            }
            else
            {
                agent.isStopped = true; //  หยุดการเคลื่อนที่
            }

        }
    }

    void Attack(IDamageable target)
    {
        target.TakeDamage(attackDamage, this, DamageType.Physical);
        lastAttackTime = Time.time; // อัปเดตเวลาที่โจมตีล่าสุด

        if (animator != null)
        {
            animator.SetTrigger("Attack");
            photonView.RPC("RPC_PlayAnimation", RpcTarget.All, "Attack");
        }
    }

    public void TakeDamage(float damage, IDamageable attacker, DamageType damageType)
    {
        if (currentHealth <= 0) return;

        float defenseValue = damageType == DamageType.Physical ? defense :
                            (damageType == DamageType.Magic ? magicDef : 0);
        float damageTaken = Mathf.Max(damage - defenseValue, 0);

        currentHealth -= damageTaken;
        lastAttacker = attacker as Hero;

        UpdateHealthUI();

        //  แสดงตัวเลขดาเมจ
        Vector3 randomness = new Vector3(Random.Range(0f, 2.0f), Random.Range(0f, 1.5f) + 8, Random.Range(0f, 2.0f));
        DamagePopup.current.CreatPopUp(transform.position + randomness, damageTaken.ToString(), damageType, attacker, this);

        if (currentHealth <= 0) Die();
    }

    public  void Die()
    {

        OnDeath?.Invoke();

        if (lastAttacker != null)
        {
            lastAttacker.ReceiveGold(gold);
            lastAttacker.ReceiveExp(exp);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            photonView.RPC("RPC_RequestDestroy", RpcTarget.MasterClient, photonView.ViewID);
        }
    }

    [PunRPC]
    public  void  RPC_RequestDestroy(int viewID)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        PhotonView target = PhotonView.Find(viewID);
        if (target != null)
        {
            PhotonNetwork.Destroy(target.gameObject);
        }
    }
    [PunRPC]
    public void RPC_SetMinionTeam(int teamInt)
    {

        minionTeam = (Team)teamInt;
    }
    [PunRPC]
    public void RPC_ApplySlow(float slowPercentage, float duration)
    {
        StartCoroutine(ApplySlowEffect(slowPercentage, duration));
    }

    private IEnumerator ApplySlowEffect(float slowPercentage, float duration)
    {
        Debug.Log($"Minion {name} ถูกลดความเร็วลง {slowPercentage * 100}% เป็นเวลา {duration} วินาที!");
        float originalSpeed = movementSpeed;
        movementSpeed *= (1 - slowPercentage);

        yield return new WaitForSeconds(duration);

        movementSpeed = originalSpeed;
        Debug.Log($"Minion {name} ฟื้นคืนความเร็ว!");
    }


    void OnDrawGizmos()
    {
        // วาด Gizmos สีเขียว
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        // วาด Gizmos สีแดง
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

     void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth; 
        }
    }

}
