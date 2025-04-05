using UnityEngine;
using Photon.Pun;
using System.Collections;

public class PlayerSetup : MonoBehaviourPun
{
    public GameObject cameraPrefab;
    private GameObject playerCamera;

    void Start()
    {
        if (photonView.IsMine)
        {
            if (cameraPrefab != null)
            {
                // เปลี่ยนจาก PhotonNetwork.Instantiate เป็น Instantiate ปกติ
                playerCamera = Instantiate(cameraPrefab, transform.position, Quaternion.identity);
                StartCoroutine(SetHeroAsTarget());
            }
            else
            {
                Debug.LogError("cameraPrefab is not assigned in PlayerSetup.");
            }
        }
    }

    private IEnumerator SetHeroAsTarget()
    {
        GameObject hero = null;
        while (hero == null)
        {
            // ใช้ PhotonView ID เพื่อให้แน่ใจว่าเป็นฮีโร่ของผู้เล่นนี้
            PhotonView[] views = FindObjectsOfType<PhotonView>();
            foreach (PhotonView view in views)
            {
                if (view.IsMine && view.CompareTag("Hero"))
                {
                    hero = view.gameObject;
                    break;
                }
            }

            if (hero != null)
            {
                CameraMovement cameraMovement = playerCamera.GetComponent<CameraMovement>();
                if (cameraMovement != null)
                {
                    cameraMovement.target = hero.transform;
                    Debug.Log("Hero assigned as camera target: " + hero.name);
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}
