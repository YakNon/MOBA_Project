using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HeroStatusUIContainer : MonoBehaviour
{
    public TextMeshProUGUI heroName;
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI killText;
    public TextMeshProUGUI deathText;
    public TextMeshProUGUI assistText;
    public TextMeshProUGUI goldText;
    public Image heroImage;
    public Image[] itemIcons; // ไอคอนสำหรับแสดงไอเท็มของฮีโร่

    public void UpdateStatus(string hero, string player, int kills, int deaths, int assists, int gold, Sprite image)
    {
        heroName.text = hero;
        playerName.text = player;
        killText.text = $"{kills}";
        deathText.text = $"{deaths}";
        assistText.text = $"{assists}";
        goldText.text = $"{gold}";
        heroImage.sprite = image;
    }

    public void UpdateItems(Sprite[] itemSprites)
    {
        for (int i = 0; i < itemIcons.Length; i++)
        {
            if (i < itemSprites.Length && itemSprites[i] != null)
            {
                itemIcons[i].sprite = itemSprites[i];
                itemIcons[i].gameObject.SetActive(true);
            }
            else
            {
                itemIcons[i].gameObject.SetActive(false);
            }
        }
    }
}
