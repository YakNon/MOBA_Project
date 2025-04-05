using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.UI;

public class HeroStatusUIManager : MonoBehaviour
{
    [Header("All Hero Status")]
    public Transform leftTeamPanel;  // พาเรนต์ของ UI ทีมเดียวกัน
    public Transform rightTeamPanel; // พาเรนต์ของ UI ฝั่งตรงข้าม
    public GameObject statusPanelPrefab; // Prefab ของ UI แสดงค่า Kill/Die/Assist
    public GameObject detailStatusPanelPrefab; // Prefab ของ UI แสดงค่า  maxHealthText attackDamageText defenseText magicText magicDefText;

    public GameObject statusUICanvas; // Canvas ที่จะแสดง/ซ่อน UI
    [Header("Toggle Buttons")]
    public Button itemButton;
    public Button detailButton;
    public Color activeColor = new Color(0.6f, 0.6f, 0.6f);  // เทาเข้ม (150/255)
    public Color inactiveColor = new Color(0.6f, 0.6f, 0f);   //ไม่มีสี 0/255
    public GameObject iconPanel;
    public GameObject iconDetailPanel;

    private Dictionary<Hero, HeroStatsDetailUIContainer> detailPanels = new Dictionary<Hero, HeroStatsDetailUIContainer>();
    private bool showingBasicStats = true;
    public static HeroStatusUIManager instance;

    private Dictionary<Hero, HeroStatusUIContainer> heroStatusPanels = new Dictionary<Hero, HeroStatusUIContainer>();

    void Start()
    {
        ToggleToItem();
        //  ถ้าอยู่ใน Summary Scene ให้เปิด UI ทันที
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "summary")
        {
            statusUICanvas.SetActive(true);
        }
        else
        {
            statusUICanvas.SetActive(false); 
        }
    }
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            //DontDestroyOnLoad(gameObject); // ✅ ทำให้ UI คงอยู่ข้ามฉาก
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // void Update()
    // {
    //     if (statusUICanvas.activeSelf)
    //     {
    //         UpdateHeroStatus();
    //     }
    // }

    void InitializeHeroStatusPanels()
    {
        foreach (Hero hero in FindObjectsOfType<Hero>())
        {
            CreateHeroStatusPanel(hero);
        }
    }
    public void SetHeroStatusCanvas(GameObject newCanvas)
    {
        if (newCanvas != null)
        {
            statusUICanvas = newCanvas;
            statusUICanvas.SetActive(true); // เปิดใช้งาน UI
            Debug.Log("statusUICanvas has been updated to the new StatusCanvas.");
        }
        else
        {
            Debug.LogWarning("The new StatusCanvas is null. Cannot update statusUICanvas.");
        }
    }

    public void SetHeroStatusCanvasAndPanels(GameObject newCanvas)
    {
        if (newCanvas != null)
        {
            statusUICanvas = newCanvas;
            statusUICanvas.SetActive(true); // เปิดใช้งาน UI

            // ค้นหา Content ที่เป็นลูกของ StatusCanvas
            Transform contentTransform = statusUICanvas.transform.Find("content");
            if (contentTransform != null)
            {
                // ค้นหา MyTeamPanel และ EnemyTeamPanel ภายใน content
                Transform myTeamPanel = contentTransform.Find("MyTeamPanel");
                Transform enemyTeamPanel = contentTransform.Find("EnemyTeamPanel");

                if (myTeamPanel != null)
                {
                    leftTeamPanel = myTeamPanel;
                    Debug.Log("✅ leftTeamPanel (MyTeamPanel) ถูกตั้งค่าเรียบร้อย");
                }
                else
                {
                    Debug.LogWarning("⚠️ ไม่พบ MyTeamPanel ใน Content");
                }

                if (enemyTeamPanel != null)
                {
                    rightTeamPanel = enemyTeamPanel;
                    Debug.Log("✅ rightTeamPanel (EnemyTeamPanel) ถูกตั้งค่าเรียบร้อย");
                }
                else
                {
                    Debug.LogWarning("⚠️ ไม่พบ EnemyTeamPanel ใน Content");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ ไม่พบ content ภายใน StatusCanvas");
            }

            Debug.Log("✅ statusUICanvas and panels have been updated.");
        }
        else
        {
            Debug.LogWarning("⚠️ newCanvas is null. Cannot update statusUICanvas.");
        }
    }

    void CreateHeroStatusPanel(Hero hero)
    {
        Transform parentPanel = (hero.Team == TeamManager.instance.GetTeam(PhotonNetwork.LocalPlayer)) 
            ? leftTeamPanel : rightTeamPanel;

        GameObject basicPanel = Instantiate(statusPanelPrefab, parentPanel);
        HeroStatusUIContainer basicUI = basicPanel.GetComponent<HeroStatusUIContainer>();
        heroStatusPanels[hero] = basicUI;

        GameObject detailPanel = Instantiate(detailStatusPanelPrefab, parentPanel);
        HeroStatsDetailUIContainer detailUI = detailPanel.GetComponent<HeroStatsDetailUIContainer>();
        detailPanels[hero] = detailUI;

        detailPanel.SetActive(false); // ซ่อนรายละเอียดไว้ก่อน

        basicUI.UpdateStatus(
            hero.name,
            hero.ownerPlayer.NickName,
            hero.killCount,
            hero.deathCount,
            hero.assistCount,
            hero.currentGold,
            hero.heroImage
        );

        detailUI.UpdateStats(hero);
        UpdateHeroItems(basicUI, hero);
    }
    public void UpdateHeroStatus()
    {
        if (IsSummaryScene())
        {
            for (int i = 0; i < summaryBasicPanels.Count; i++)
            {
                summaryBasicPanels[i].SetActive(showingBasicStats);
            }

            for (int i = 0; i < summaryDetailPanels.Count; i++)
            {
                summaryDetailPanels[i].SetActive(!showingBasicStats);
            }

            return;
        }

        foreach (var entry in heroStatusPanels)
        {
            Hero hero = entry.Key;

            if (hero != null)
            {
                if (showingBasicStats)
                {
                    heroStatusPanels[hero].gameObject.SetActive(true);
                    detailPanels[hero].gameObject.SetActive(false);

                    heroStatusPanels[hero].UpdateStatus(
                        hero.name,
                        hero.ownerPlayer.NickName,
                        hero.killCount,
                        hero.deathCount,
                        hero.assistCount,
                        hero.currentGold,
                        hero.heroImage
                    );

                    UpdateHeroItems(heroStatusPanels[hero], hero);
                }
                else
                {
                    heroStatusPanels[hero].gameObject.SetActive(false);
                    detailPanels[hero].gameObject.SetActive(true);
                    detailPanels[hero].UpdateStats(hero);
                }
            }
        }
    }

    private bool IsSummaryScene()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "summary";
    }

    void UpdateHeroItems(HeroStatusUIContainer panel, Hero hero)
    {
        Sprite[] itemSprites = new Sprite[panel.itemIcons.Length];
        for (int i = 0; i < itemSprites.Length; i++)
        {
            if (i < hero.items.Count && hero.items[i] != null)
            {
                itemSprites[i] = hero.items[i].itemIcon;
            }
            else
            {
                itemSprites[i] = null;
            }
        }
        panel.UpdateItems(itemSprites);
    }    
    void UpdateButtonColors()
    {
        if (itemButton != null && detailButton != null)
        {
            itemButton.GetComponent<Image>().color = showingBasicStats ? activeColor : inactiveColor;
            detailButton.GetComponent<Image>().color = showingBasicStats ? inactiveColor : activeColor;
        }
    }
    public void ToggleToItem()
    {
        showingBasicStats = true;
        iconDetailPanel.SetActive(false);
        iconPanel.SetActive(true);

        UpdateHeroStatus();
        UpdateButtonColors();
    }

    public void ToggleToDetail()
    {
        showingBasicStats = false;
        iconDetailPanel.SetActive(true);
        iconPanel.SetActive(false);
        UpdateHeroStatus();
        UpdateButtonColors();
    }

    public void ToggleHeroStatusUI()
    {
        bool isActive = statusUICanvas.activeSelf;
        statusUICanvas.SetActive(!isActive);
        ToggleToItem();

        if (!isActive)
        {
            InitializeHeroStatusPanels();
            UpdateHeroStatus();
        }
    }

    public void CloseHeroStatusUI()
    {
        foreach (var entry in heroStatusPanels)
        {
            if (entry.Value != null)
            {
                Destroy(entry.Value.gameObject);
            }
        }
        heroStatusPanels.Clear();
        foreach (var entry in detailPanels)
        {
            if (entry.Value != null)
            {
                Destroy(entry.Value.gameObject);
            }
        }
        detailPanels.Clear();
        statusUICanvas.SetActive(false);
        
    }

    //for summary scene
    private List<GameObject> summaryBasicPanels = new List<GameObject>();
    private List<GameObject> summaryDetailPanels = new List<GameObject>();

    public void CreateSummaryPanel(GameManager.HeroSummaryData data, bool isMyTeam)
    {
        Transform parentPanel = isMyTeam ? leftTeamPanel : rightTeamPanel;

        GameObject basicPanel = Instantiate(statusPanelPrefab, parentPanel);
        HeroStatusUIContainer basicUI = basicPanel.GetComponent<HeroStatusUIContainer>();

        GameObject detailPanel = Instantiate(detailStatusPanelPrefab, parentPanel);
        HeroStatsDetailUIContainer detailUI = detailPanel.GetComponent<HeroStatsDetailUIContainer>();

        detailPanel.SetActive(false);

        // โหลดรูปฮีโร่จาก Resources โดยใช้ heroName
        Sprite heroSprite = Resources.Load<Sprite>($"HeroImage/{data.heroIcon}");
        if (heroSprite == null)
        {
            heroSprite = Resources.Load<Sprite>("HeroImage/22");
        }

        basicUI.UpdateStatus(
            data.heroName,
            data.playerName,
            data.kill,
            data.death,
            data.assist,
            data.gold,
            heroSprite
        );

        // โหลดไอเท็มจากชื่อ
        Sprite[] itemSprites = new Sprite[basicUI.itemIcons.Length];

        for (int i = 0; i < itemSprites.Length; i++)
        {
            if (i < data.itemNames.Count)
            {
                string itemName = data.itemNames[i];
                Sprite icon = Resources.Load<Sprite>($"Item/Item_Icon/{itemName}");
                itemSprites[i] = icon;
            }
            else
            {
                itemSprites[i] = null;
            }
        }

        basicUI.UpdateItems(itemSprites);

        detailUI.heroName.text = data.heroName;
        detailUI.playerName.text = data.playerName;
        detailUI.maxHealthText.text = data.maxHealth.ToString();
        detailUI.attackDamageText.text = data.attackDamage.ToString();
        detailUI.defenseText.text = data.defense.ToString();
        detailUI.magicText.text = data.magic.ToString();
        detailUI.magicDefText.text = data.magicDef.ToString();
        detailUI.heroImage.sprite = heroSprite;

        //  เก็บลง dictionary เพื่อให้ toggle ทำงานได้
        summaryBasicPanels.Add(basicUI.gameObject);
        summaryDetailPanels.Add(detailUI.gameObject);
    }
    public void ClearSummaryPanels()
    {
        foreach (var panel in summaryBasicPanels)
        {
            if (panel != null) Destroy(panel);
        }
        summaryBasicPanels.Clear();

        foreach (var panel in summaryDetailPanels)
        {
            if (panel != null) Destroy(panel);
        }
        summaryDetailPanels.Clear();
    }



}
