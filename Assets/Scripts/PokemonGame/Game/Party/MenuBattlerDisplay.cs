using PokemonGame.ScriptableObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuBattlerDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI battlerNameText;
    [SerializeField] private TextMeshProUGUI battlerHealthText;
    [SerializeField] private TextMeshProUGUI statusDisplay;
    [SerializeField] private RectMask2D expDisplay;
    [SerializeField] private Image battlerSpriteImage;
    [SerializeField] private Image background;
    [SerializeField] private Color aliveColour;
    [SerializeField] private Color defeatedColour;

    public void Init(string name, int health, int maxHealth, StatusEffect effect, int exp, int maxExp, Sprite sprite)
    {
        battlerNameText.text = name;
        battlerHealthText.text = $"{health}/{maxHealth}";
        battlerSpriteImage.sprite = sprite;
        statusDisplay.text = health != 0 ? effect.name : "Fainted";
        statusDisplay.color = health != 0 ? effect.colour : Color.white;
        expDisplay.padding = new Vector4(0, 0, 282 - (float)exp/(float)maxExp * 282f, 0);
        background.color = health == 0 ? defeatedColour : aliveColour;
    }
}
