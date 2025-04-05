using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamagePopupAnimation : MonoBehaviour
{
    
    public AnimationCurve opacityCurve;
    public AnimationCurve scaleCurve;
    private TextMeshProUGUI tmp;
    private float time = 0;
    private void Awake()
    {
        tmp = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
    }
    private void Update()
    {
        tmp.color = new Color(1, 1, 1, opacityCurve.Evaluate(time));
        transform.localScale = Vector3.one * scaleCurve.Evaluate(time);
        time += Time.deltaTime;
    }
}
