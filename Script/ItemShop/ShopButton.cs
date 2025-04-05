using UnityEngine;
using UnityEngine.UI;

public class ShopButton : MonoBehaviour
{
    public ShopManager shopManager;
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OpenShop);
    }

    void OpenShop()
    {
        shopManager.ToggleShop();
    }
}
