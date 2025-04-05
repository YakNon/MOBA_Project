using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

public class ShopManager : MonoBehaviour
{
    //อัปเดทเงิน ไอเท็ม  UI ของตัวเอง ซื้อขายไอเท็ม
    public GameObject shopUI; // Canvas ของร้านค้า
    public Transform itemContainer; // ที่วางไอเท็ม
    public GameObject itemButtonPrefab; // Prefab ปุ่มไอเท็ม

    public TMP_Text itemDescriptionText; // แสดงรายละเอียดไอเท็ม
    public TMP_Text playerGoldText; // แสดงเงินในหน้าร้านค้า
    public TMP_Text playerGoldTextInButton;// แสดงเงินที่อยู่ข้างปุ่ม
    public Button buyButton; // ปุ่มซื้อไอเท็ม

    public List<Item> availableItems; // รายการไอเท็มทั้งหมดในร้านค้า

    private Item selectedItem;
    public Hero hero;

    public Transform myItemPanel; // ตัว Panel หลัก
    public List<GameObject> itemSlots; 
    public GameObject myItemButtonPrefab;
    private GameObject selectedButton;


    public Button sellButton; // ปุ่มขายไอเท็ม
    public TMP_Text sellPriceText;
    
    void Start()
    {
        shopUI.SetActive(false);
        buyButton.gameObject.SetActive(false);
        sellButton.gameObject.SetActive(false);

        //FindPlayerHero(); // ค้นหา Hero ของผู้เล่น
        StartCoroutine(WaitForHero()); // รอให้ Hero ถูกสร้างก่อน
        PopulateShop();
        UpdateGoldDisplay();
    }
    IEnumerator WaitForHero()
    {
        while (hero == null)
        {
            foreach (Hero h in FindObjectsOfType<Hero>())
            {
                if (h.photonView.IsMine)
                {
                    hero = h;
                    //Debug.Log("Hero assigned successfully!");
                    UpdateGoldDisplay(); // อัปเดต UI หลังจากเจอ Hero
                    yield break; // ออกจาก Coroutine เมื่อเจอ Hero
                }
            }
            yield return new WaitForSeconds(0.5f); // รอ 0.5 วินาทีแล้วลองใหม่
        }
    }

    void FindPlayerHero()
    {
        foreach (Hero h in FindObjectsOfType<Hero>())
        {
            if (h.photonView.IsMine) 
            {
                hero = h;
                break;
            }
        }
    }


    void PopulateShop()
    {
        foreach (Item item in availableItems)
        {
            GameObject itemObj = Instantiate(itemButtonPrefab, itemContainer);


            TMP_Text itemNameText = itemObj.transform.Find("ItemNameText").GetComponent<TMP_Text>();
            TMP_Text itemPriceText = itemObj.transform.Find("ItemPriceText").GetComponent<TMP_Text>();

            itemNameText.text = item.itemName;
            itemPriceText.text = $"$ {item.price}";

            Image itemImage = itemObj.transform.Find("ItemImage").GetComponent<Image>();
            if (item.itemIcon != null)
            {
                itemImage.sprite = item.itemIcon;
            }

       
            Button btn = itemObj.GetComponent<Button>();
            btn.onClick.AddListener(() => ShowItemDetails(item));;
            //btn.onClick.AddListener(() => sellButton.gameObject.SetActive(false));
        }
    }

    void ShowItemDetails(Item item)
    {
        selectedItem = item;
        itemDescriptionText.text = $"{item.itemName}\n{item.description}\nPrice: {item.price} Gold";
        sellPriceText.text = "";
        buyButton.gameObject.SetActive(true);
        sellButton.gameObject.SetActive(false);

        // รีเซ็ตสีปุ่มก่อนหน้า
        if (selectedButton != null)
        {
            ResetButtonAppearance(selectedButton);
        }

        // ค้นหาและเปลี่ยนสีปุ่มปัจจุบัน
        foreach (Transform child in itemContainer)
        {
            if (child.GetComponentInChildren<TMP_Text>().text == item.itemName)
            {
                selectedButton = child.gameObject;
                HighlightButton(selectedButton);
                break;
            }
        }
    }
    public void BuyItem()
    {
        if (hero == null)
        {
            Debug.LogWarning("Hero is not assigned! Trying to find again...");
            FindPlayerHero(); // ลองหาใหม่
            if (hero == null)
            {
                return; // ถ้ายังหาไม่เจอ ให้หยุดฟังก์ชัน
            }
        }

        if (hero.items.Count >= 6) // เช็คว่ามีไอเท็มครบ 6 ช่องหรือยัง
        {
            itemDescriptionText.text = "Inventory Full!";
            //Debug.Log("Cannot buy more items, inventory is full!");
            return;
        }

        if (selectedItem != null && hero.currentGold >= selectedItem.price) 
        {
            int newGold = hero.currentGold - selectedItem.price;
            
            
            // เรียกใช้ RPC เพื่อให้ทุกคนเห็นการอัปเดตเงิน
            hero.photonView.RPC("RPC_UpdateGold", RpcTarget.AllBuffered, newGold);

            // เรียกใช้ RPC เพื่อเพิ่มไอเท็มใน inventory และให้ทุกคนเห็น
            hero.photonView.RPC("RPC_AddItem", RpcTarget.AllBuffered, selectedItem.itemName);

            //Debug.Log($"Purchased: {selectedItem.itemName}");
            buyButton.gameObject.SetActive(false);
            itemDescriptionText.text = "Item Purchased!";
        }
        else
        {
            itemDescriptionText.text = "Not enough gold!";
        }
    }

    public void SellItem()
    {
        if (selectedItem != null && hero.items.Contains(selectedItem))
        {
            int newGold = hero.currentGold + selectedItem.sellPrice;
            hero.photonView.RPC("RPC_UpdateGold", RpcTarget.AllBuffered, newGold);

            hero.photonView.RPC("RPC_RemoveItem", RpcTarget.AllBuffered, selectedItem.itemName);

            //Debug.Log($"Sold: {selectedItem.itemName}");

            //  ลบเฉพาะปุ่มของไอเท็มที่ขายไป
            if (selectedButton != null)
            {
                itemSlots.Remove(selectedButton);
                Destroy(selectedButton);
                selectedButton = null;
            }

            //  รีเซ็ต UI
            selectedItem = null;
            sellButton.gameObject.SetActive(false);
            itemDescriptionText.text = "Item Sold!";
            sellPriceText.text = "";
        }
    }



    void ShowMyItemDetails(Item item, GameObject itemButton)
    {
        if (selectedButton != null)
        {
            ResetButtonAppearance(selectedButton);
        }
        selectedItem = item;
        selectedButton = itemButton;
        itemDescriptionText.text = $"{item.itemName}\n{item.description}\nSell Price: {item.sellPrice} ";

        buyButton.gameObject.SetActive(false);
        sellButton.gameObject.SetActive(true); // แสดงปุ่มขาย
        sellPriceText.text = $"{item.sellPrice} "; // แสดงราคาขาย
    }

    public void UpdateGoldDisplay()
    {
        if (hero != null)
        {
            playerGoldText.text = $": {hero.currentGold}";
            playerGoldTextInButton.text = $"{hero.currentGold}";
        }
    }




    public void AddItemToMyItems(Item item)
    {
        foreach (Transform slot in myItemPanel)
        {
            if (slot.childCount == 0) // หาช่องว่างที่ยังไม่มีไอเท็ม
            {
                GameObject newItem = Instantiate(myItemButtonPrefab, slot);
                newItem.GetComponent<Image>().sprite = item.itemIcon;
                newItem.GetComponent<Button>().onClick.AddListener(() => ShowMyItemDetails(item, newItem));

                itemSlots.Add(newItem);
                return;
            }
        }
    }
    public void RefreshInventoryUI()
    {
        // ลบทั้งหมดจาก myItemPanel แบบนับถอยหลัง
        for (int i = 0; i < myItemPanel.childCount; i++)
        {
            Transform slot = myItemPanel.GetChild(i);
            if (slot.childCount > 0)
            {
                Destroy(slot.GetChild(0).gameObject);
            }
        }

        // เติมไอเท็มที่เหลือกลับเข้า UI
        foreach (Item item in hero.items)
        {
            AddItemToMyItems(item);
        }
    }

    // public void RefreshInventoryUI()
    // {
    //     foreach (Transform slot in myItemPanel)
    //     {
    //         if (slot.childCount > 0)
    //         {
    //             Destroy(slot.GetChild(0).gameObject); // ลบไอเท็มทั้งหมดจาก UI
    //         }
    //     }

    //     foreach (Item item in hero.items)
    //     {
    //         AddItemToMyItems(item); // เพิ่มไอเท็มที่เหลือกลับเข้า UI
    //     }
    // }



    void HighlightButton(GameObject button)
    {
        Image buttonImage = button.GetComponent<Image>();
        buttonImage.color = new Color(0, 0, 0, 200f / 255f); // พื้นหลังสีดำ โปร่ง 200

        Transform itemImageTransform = button.transform.Find("ItemImage");
        if (itemImageTransform != null)
        {
            Image itemImage = itemImageTransform.GetComponent<Image>();
            itemImage.color = Color.white; // รูปในปุ่มเป็นพื้นหลังสีขาว
        }

        Transform borderTransform = button.transform.Find("Border");
        if (borderTransform != null)
        {
            Image borderImage = borderTransform.GetComponent<Image>();
            borderImage.color = new Color(1.0f, 0.5f, 0.0f); // เปลี่ยนขอบเป็นสีส้ม
        }
    }

    void ResetButtonAppearance(GameObject button)
    {
        Image buttonImage = button.GetComponent<Image>();
        buttonImage.color = Color.white; // พื้นหลังกลับเป็นปกติ

        Transform itemImageTransform = button.transform.Find("ItemImage");
        if (itemImageTransform != null)
        {
            Image itemImage = itemImageTransform.GetComponent<Image>();
            itemImage.color = Color.white; // รูปไอเท็มกลับเป็นปกติ
        }

        Transform borderTransform = button.transform.Find("Border");
        if (borderTransform != null)
        {
            Image borderImage = borderTransform.GetComponent<Image>();
            borderImage.color = Color.clear; // ขอบปุ่มหายไป
        }
    }


    public void ToggleShop()
    {
        shopUI.SetActive(!shopUI.activeSelf);
    }

    public void CloseShop()
    {
        if (selectedButton != null)
        {
            ResetButtonAppearance(selectedButton);
        }
        itemDescriptionText.text = "";
        sellPriceText.text = "";
        buyButton.gameObject.SetActive(false);
        sellButton.gameObject.SetActive(false);
        shopUI.SetActive(false);
    }
}
