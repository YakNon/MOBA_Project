using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HeroStatsDetailUIContainer : MonoBehaviour
{
    public TextMeshProUGUI heroName;
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI maxHealthText;
    public TextMeshProUGUI attackDamageText;
    public TextMeshProUGUI defenseText;
    public TextMeshProUGUI magicText;
    public TextMeshProUGUI magicDefText;
    public Image heroImage;

    public void UpdateStats(Hero hero)
    {
        heroName.text = hero.name;
        playerName.text = hero.ownerPlayer.NickName;
        maxHealthText.text = $"{hero.maxHealth}";
        attackDamageText.text = $"{hero.attackDamage}";
        defenseText.text = $"{hero.defense}";
        magicText.text = $"{hero.magic}";
        magicDefText.text = $"{hero.magicDef}";
        heroImage.sprite = hero.heroImage;
    }
}
