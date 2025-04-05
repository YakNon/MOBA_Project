using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealthSlider : MonoBehaviour
{
    void LateUpdate()
    {

        if (Camera.main == null) return;

        // ให้บาร์หันเข้าหากล้องตลอดเวลา
        //transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        // transform.rotation = Quaternion.LookRotation(-Camera.main.transform.forward);
        transform.LookAt(transform.position + Camera.main.transform.forward);
    }
}
