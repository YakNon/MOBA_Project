using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GrassArea : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Hero hero = other.GetComponent<Hero>();
        if (hero != null)
        {
            hero.photonView.RPC("RPC_EnterGrass", RpcTarget.AllBuffered);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Hero hero = other.GetComponent<Hero>();
        if (hero != null)
        {
            hero.photonView.RPC("RPC_ExitGrass", RpcTarget.AllBuffered);
        }
    }
}
