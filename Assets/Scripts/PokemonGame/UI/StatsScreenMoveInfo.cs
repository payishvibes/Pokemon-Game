using PokemonGame.ScriptableObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsScreenMoveInfo : MonoBehaviour
{
    [SerializeField] private GameObject toolTipObj;
    
    [Space]
    [SerializeField] private Image category;
    [SerializeField] private Sprite physicalSprite;
    [SerializeField] private Sprite specialSprite;
    [SerializeField] private Sprite statusSprite;
    
    [Space]
    [SerializeField] private Image typeColour;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI accuracyText;

    private bool _on;

    public void UpdateInfo(Move move, Transform location)
    {
        toolTipObj.SetActive(true);
        switch (move.category)
        {
            case MoveCategory.Physical:
                category.sprite = physicalSprite;
                break;
            case MoveCategory.Special:
                category.sprite = specialSprite;
                break;
            case MoveCategory.Status:
                category.sprite = statusSprite;
                break;
        }
        
        typeColour.color = move.type.color;
        nameText.text = move.name;
        typeText.text = move.type.name;
        damageText.text = move.category != MoveCategory.Status ? move.damage.ToString() : "-";
        accuracyText.text = move.accuracy != 0 ? (move.accuracy * 100) + "%" : "-";

        transform.position = location.position;
    }

    public void HideToolTip()
    {
        toolTipObj.SetActive(false);
    }
}
