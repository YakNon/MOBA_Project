using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class HeroRange : Hero
{
    public GameObject projectilePrefab; 

    void Start()
    {
        ownerPlayer = photonView.Owner;
        Time.timeScale = 1;
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        currentHealth = maxHealth;
        joystick = FindObjectOfType<bl_Joystick>();

        // ตรวจสอบว่าผู้เล่นมีทีมถูกต้องก่อนเซ็ตทีมใหม่
        if (ownerPlayer.CustomProperties.ContainsKey("team"))
        {
            Team assignedTeam = (Team)(int)ownerPlayer.CustomProperties["team"];
            Debug.Log($" Hero {name} belongs to Team: {assignedTeam}");
            
            // ป้องกันการเรียก SetTeam() ซ้ำ ถ้าทีมถูกต้องอยู่แล้ว
            if (TeamManager.instance.GetTeam(ownerPlayer) != assignedTeam)
            {
                Debug.LogWarning($"Mismatch team detected! Fixing team to {assignedTeam}");
                SetTeam(assignedTeam);
            }
        }
        else
        {
            Debug.LogWarning($"Player {ownerPlayer.NickName} ไม่มีทีม! กำหนดให้เป็นค่าเริ่มต้น Red");
            SetTeam(Team.Red);
        }

        SetHealthBarColor();
        UpdateHealthUI();

        // สร้าง RangeCanvas เมื่อเริ่มเกม
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
    public override void Attack()
    {
        if (Time.time >= lastAttackTime + (1f / attackSpeed) || isStunned || isDead)
        {
            isWarping = false;
            lastAttackTime = Time.time;
            PlayAnimation("Attack");

            MonoBehaviour target = GetNearestTarget();

            if (target == null)
            {
                //Debug.LogWarning($" {name} ไม่มีเป้าหมายโจมตี!");
                return;
            }

            Debug.Log($"{name} โจมตี {target.name}!");

            if (firePoint == null || projectilePrefab == null)
            {
                Debug.LogError("FirePoint หรือ ProjectilePrefab หายไป!");
                return;
            }

            Vector3 directionToTarget = target.transform.position - transform.position;
            directionToTarget.y = 0;
            transform.rotation = Quaternion.LookRotation(directionToTarget);

            GameObject projectile = PhotonNetwork.Instantiate(projectilePrefab.name, firePoint.position, firePoint.rotation);
            Projectile proj = projectile.GetComponent<Projectile>();

            if (proj != null)
            {
                proj.SetDamage(attackDamage);
                proj.SetMagic(magic);
                proj.SetTrueDamage(trueDamage);
                proj.SetAttacker(this);
                proj.SetTarget(target as IDamageable);
            }
        }

        ShowRangeCanvas();
    }


    MonoBehaviour GetNearestTarget()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);
        List<(MonoBehaviour target, float distance)> enemiesInRange = new List<(MonoBehaviour, float)>();

        Team heroTeam = TeamManager.instance.GetTeam(ownerPlayer);

        foreach (Collider collider in hitColliders)
        {
            IDamageable target = collider.GetComponent<IDamageable>();
            if (target == null) continue;

            bool isEnemy = false;
            float distance = Vector3.Distance(transform.position, collider.transform.position);
            Team enemyTeam = Team.None;

            if (target is Hero hero && !IsOnSameTeamAs(hero))
            {
                enemyTeam = hero.Team;
                isEnemy = true;
            }
            if (target is NPC)
            {
                enemyTeam = Team.None;
                isEnemy = true;
            }
            if (target is Minion minion && !IsOnSameTeamAs(minion))
            {
                enemyTeam = minion.minionTeam;
                isEnemy = true;
            }
            if (target is Tower tower && tower.towerTeam != heroTeam)
            {
                enemyTeam = tower.towerTeam;
                isEnemy = true;
            }


            if (isEnemy)
            {
                enemiesInRange.Add((target as MonoBehaviour, distance));
                //Debug.Log($" พบศัตรู: {(target as MonoBehaviour)?.name ?? "Unknown"} (ระยะ: {distance}) Team: {enemyTeam}");
            }
        }

        if (enemiesInRange.Count == 0)
        {
            return null;
        }

        // เรียงลำดับศัตรูตามระยะที่ใกล้ที่สุด
        enemiesInRange.Sort((a, b) => a.distance.CompareTo(b.distance));
        MonoBehaviour nearestEnemy = enemiesInRange[0].target;
        
        return nearestEnemy;
    }

}
