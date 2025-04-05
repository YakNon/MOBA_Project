using System.Collections.Generic;
using UnityEngine;

public class FireballPoolManager : MonoBehaviour
{
    public static FireballPoolManager instance;

    [System.Serializable]
    public class ProjectilePool
    {
        public string projectileName;
        public GameObject prefab;
        public int poolSize = 10;
    }

    public List<ProjectilePool> projectilePools;
    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();

    void Awake()
    {
        instance = this;

        foreach (var pool in projectilePools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.poolSize; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.projectileName, objectPool);
        }
    }

    public GameObject GetProjectile(string projectileName)
    {
        if (!poolDictionary.ContainsKey(projectileName))
        {
            Debug.LogWarning("Projectile pool with name " + projectileName + " doesn't exist!");
            return null;
        }

        var pool = poolDictionary[projectileName];

        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        else
        {
            // fallback if pool is empty
            GameObject prefab = projectilePools.Find(p => p.projectileName == projectileName)?.prefab;
            if (prefab != null)
            {
                return Instantiate(prefab);
            }
            return null;
        }
    }

    public void ReturnProjectile(string projectileName, GameObject projectile)
    {
        if (!poolDictionary.ContainsKey(projectileName))
        {
            Debug.LogWarning("Projectile pool with name " + projectileName + " doesn't exist!");
            Destroy(projectile);
            return;
        }

        projectile.SetActive(false);
        poolDictionary[projectileName].Enqueue(projectile);
    }
}
