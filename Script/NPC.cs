using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using Photon.Pun;
using Photon.Realtime;

public class NPC : MonoBehaviourPun , IDamageable
{
    public Transform transform => base.transform;
    public Team  NPCteam = Team.None;
    [HideInInspector] public Vector3 spawnPoint;
    public float spawnTime;
    public string name; 
    public float maxHealth;
    [HideInInspector] public float currentHealth;
    public float defense = 5f;
    public float magicDef = 5f;
    public int gold = 10;
    public int exp = 10;
    public Slider healthSlider;    
    private float speed = 10f;

    public float aggroRange = 8f;
    public float leashRange = 10f;
    public float resetDelay = 2f;
    public float regenRate = 50f;
    public float attackRange = 2.5f;
    public float attackDamage = 50f;
    public float attackCooldown = 2f;

    protected Hero currentTarget;
    protected float lastAttackTime;
    protected bool isResetting = false;
    protected Animator animator;
    public event System.Action OnDeath;

    protected Hero lastAttacker;  
    protected bool isDead = false;

    void Start()
    {
        spawnPoint = transform.position;
        currentHealth = maxHealth;
        UpdateHealthUI();
        animator = GetComponent<Animator>();
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(AI_Behavior());
        }
    }

    IEnumerator AI_Behavior()
    {
        while (true)
        {
            // ✅ ให้เฉพาะ MasterClient เท่านั้นที่ควบคุม NPC
            if (!PhotonNetwork.IsMasterClient)
            {
                yield return null;
                continue;
            }

            if (isDead)
            {
                yield return null;
                continue;
            }

            if (isResetting)
            {
                RegenerateHealth();
                yield return null;
                continue;
            }

            float distanceFromSpawn = Vector3.Distance(transform.position, spawnPoint);

            if (currentTarget == null || currentTarget.isDead)
                currentTarget = FindClosestEnemy();

            if (currentTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);

                if (distanceFromSpawn > leashRange)
                {
                    StartCoroutine(ResetNPC());
                    continue;
                }

                if (distanceToTarget <= attackRange && Time.time > lastAttackTime + attackCooldown)
                {
                    lastAttackTime = Time.time;
                    AttackTarget();
                }
                else
                {
                    MoveTowards(currentTarget.transform.position);
                }
            }
            else
            {
                ReturnToSpawn();
            }

            yield return new WaitForSeconds(0.2f);
        }
    }


    void AttackTarget()
    {
        animator?.SetTrigger("Attack");
        currentTarget?.TakeDamage(attackDamage, this, DamageType.Physical);
    }
    [PunRPC]
    public void RPC_UpdateHealth(float newHealth)
    {
        currentHealth = newHealth;
        UpdateHealthUI();
    }


    void MoveTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        if (direction.magnitude > 0.1f)
        {
            transform.position += direction * speed * Time.deltaTime;
            animator?.SetBool("IsWalk", true);
        }
        else
        {
            animator?.SetBool("IsWalk", false);
        }

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    Hero FindClosestEnemy()
    {
        Hero closest = null;
        float minDistance = aggroRange;

        foreach (Hero hero in FindObjectsOfType<Hero>())
        {
            if (hero.isDead || !IsEnemy(hero)) continue;

            float dist = Vector3.Distance(transform.position, hero.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = hero;
            }
        }

        return closest;
    }

    bool IsEnemy(Hero hero)
    {
        return TeamManager.instance.GetTeam(hero.ownerPlayer) != this.NPCteam;
    }

    void ReturnToSpawn()
    {
        if (Vector3.Distance(transform.position, spawnPoint) > 0.5f)
        {
            MoveTowards(spawnPoint);
        }
        else
        {
            animator?.SetBool("IsWalk", false);
        }
    }

    IEnumerator ResetNPC()
    {
        isResetting = true;
        currentTarget = null;
        animator?.SetBool("HeroInRange", false);
        yield return new WaitForSeconds(resetDelay);
        //Debug.Log($"{name} resetting to spawn...");
    }

    void RegenerateHealth()
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + regenRate * Time.deltaTime);
        if (Vector3.Distance(transform.position, spawnPoint) > 0.5f)
            MoveTowards(spawnPoint);
        else if (currentHealth >= maxHealth)
        {
            isResetting = false;
        }
    }

    public void TakeDamage(float damage, IDamageable attacker, DamageType damageType)
    {
        if (currentHealth <= 0 || isDead) return;

        float defenseValue = damageType == DamageType.Physical ? defense :
                            (damageType == DamageType.Magic ? magicDef : 0);
        float damageTaken = Mathf.Max(damage - defenseValue, 0);

        currentHealth -= damageTaken;

        // ซิงก์ไปให้ทุกคนเห็นเลือดใหม่
        photonView.RPC("RPC_UpdateHealth", RpcTarget.AllBuffered, currentHealth);

        lastAttacker = attacker as Hero;

        Vector3 randomness = new Vector3(Random.Range(0f, 2.0f), Random.Range(0f, 1.5f) + 8, Random.Range(0f, 2.0f));
        DamagePopup.current.CreatPopUp(transform.position + randomness, damageTaken.ToString(), damageType, attacker, this);

        if (currentHealth <= 0)
        StartCoroutine(HandleDeath());
    }

    protected virtual IEnumerator HandleDeath()
    {
        // แจ้งทุก client ว่า NPC ตายแล้ว
        photonView.RPC("RPC_SetDead", RpcTarget.AllBuffered, true);

        lastAttacker.ReceiveGold(gold);
        lastAttacker.ReceiveExp(exp);

        animator?.SetBool("IsDie", true);
        animator?.SetBool("IsWalk", false);
        OnDeath?.Invoke();

        yield return new WaitForSeconds(2f);

        transform.position = new Vector3(spawnPoint.x, -100f, spawnPoint.z);

        yield return new WaitForSeconds(spawnTime);

        // Respawn
        photonView.RPC("RPC_SetDead", RpcTarget.AllBuffered, false);
        transform.position = spawnPoint;
        currentHealth = maxHealth;
        UpdateHealthUI();
        animator?.SetBool("IsDie", false);
    }

    [PunRPC]
    public void RPC_SetDead(bool state)
    {
        isDead = state;
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

    [PunRPC]
    public void RPC_ApplySlow(float slowPercentage, float duration)
    {
        StartCoroutine(ApplySlowEffect(slowPercentage, duration));
    }

    protected IEnumerator ApplySlowEffect(float slowPercentage, float duration)
    {
        Debug.Log($"NPC {name} ถูกลดความเร็วลง {slowPercentage * 100}% เป็นเวลา {duration} วินาที!");
        float originalSpeed = speed; 
        float slowedSpeed = originalSpeed * (1 - slowPercentage);

        yield return new WaitForSeconds(duration);

        Debug.Log($" NPC {name} ฟื้นคืนความเร็ว!");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // วาด Gizmos สีเขียว
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, leashRange);
    }
}
