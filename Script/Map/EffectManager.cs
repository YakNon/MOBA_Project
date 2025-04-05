using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class EffectManager : MonoBehaviour
{
    public static EffectManager instance;

    [System.Serializable]
    public class EffectPool
    {
        public GameObject prefab;
        public int poolSize = 10;
        [HideInInspector] public string effectName;
        [HideInInspector] public Queue<GameObject> pool = new Queue<GameObject>();

        public void InitializeEffectName()
        {
            if (prefab != null)
            {
                effectName = prefab.name;
            }
        }
    }

    public List<EffectPool> effectsToPool = new List<EffectPool>();
    private Dictionary<string, EffectPool> effectLookup = new Dictionary<string, EffectPool>();

    // 🔥 เก็บ original scale ของแต่ละเอฟเฟค
    private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();

    private void OnValidate()
    {
        foreach (var pool in effectsToPool)
        {
            pool.InitializeEffectName();
        }
    }

    void Awake()
    {
        if (instance == null) instance = this;

        foreach (var effect in effectsToPool)
        {
            for (int i = 0; i < effect.poolSize; i++)
            {
                GameObject obj = Instantiate(effect.prefab);
                obj.SetActive(false);
                effect.pool.Enqueue(obj);

                //  เก็บ original scale
                originalScales[obj] = obj.transform.localScale;
            }
            effectLookup[effect.effectName] = effect;
        }
    }

    public GameObject PlayEffect(string effectName, Vector3 position, float duration, Transform parent = null)
    {
        if (!effectLookup.ContainsKey(effectName))
        {
            Debug.LogError($" Effect {effectName} not found in EffectManager!");
            return null;
        }

        EffectPool pool = effectLookup[effectName];
        GameObject effectObj = null;

        if (pool.pool.Count > 0)
        {
            effectObj = pool.pool.Dequeue();
        }
        else
        {
            effectObj = Instantiate(pool.prefab);

            //  เก็บ original scale สำหรับอันใหม่ที่เพิ่งสร้าง
            originalScales[effectObj] = effectObj.transform.localScale;
        }

        effectObj.transform.position = position;
        effectObj.transform.rotation = Quaternion.identity;
        if (parent != null) effectObj.transform.SetParent(parent);
        effectObj.SetActive(true);
        StartCoroutine(DisableAfterTime(effectObj, duration, pool));

        return effectObj;
    }

    public void PlayEffectLocal(string effectName, Vector3 position, float duration, out GameObject instance, Transform parent = null)
    {
        instance = PlayEffect(effectName, position, duration, parent);
    }

    public void ReleaseEffect(GameObject effectObj)
    {
        if (effectObj == null) return;

        foreach (var kvp in effectLookup)
        {
            EffectPool pool = kvp.Value;
            if (effectObj.name.StartsWith(pool.prefab.name))
            {
                ResetEffectTransform(effectObj);
                effectObj.SetActive(false);
                effectObj.transform.SetParent(null);
                pool.pool.Enqueue(effectObj);
                return;
            }
        }

        Destroy(effectObj); // fallback ถ้าไม่เจอ pool
    }

    private IEnumerator DisableAfterTime(GameObject obj, float time, EffectPool pool)
    {
        yield return new WaitForSeconds(time);

        ResetEffectTransform(obj);
        obj.SetActive(false);
        obj.transform.SetParent(null);
        pool.pool.Enqueue(obj);
    }

    private void ResetEffectTransform(GameObject effectObj)
    {
        if (originalScales.ContainsKey(effectObj))
        {
            effectObj.transform.localScale = originalScales[effectObj]; //  รีเซ็ตสเกล
        }
        else
        {
            effectObj.transform.localScale = Vector3.one;
        }
    }
}
