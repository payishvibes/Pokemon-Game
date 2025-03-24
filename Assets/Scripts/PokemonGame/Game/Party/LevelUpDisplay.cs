using PokemonGame.General;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI oldHealth;
    [SerializeField] private TextMeshProUGUI newHealth;
    
    [SerializeField] private TextMeshProUGUI oldAttack;
    [SerializeField] private TextMeshProUGUI newAttack;
    
    [SerializeField] private TextMeshProUGUI oldDefense;
    [SerializeField] private TextMeshProUGUI newDefense;
    
    [SerializeField] private TextMeshProUGUI oldSpecialAttack;
    [SerializeField] private TextMeshProUGUI newSpecialAttack;
    
    [SerializeField] private TextMeshProUGUI oldSpecialDefense;
    [SerializeField] private TextMeshProUGUI newSpecialDefense;
    
    [SerializeField] private TextMeshProUGUI oldSpeed;
    [SerializeField] private TextMeshProUGUI newSpeed;

    public void Init(BattlerStats oldStats, BattlerStats newStats)
    {
        oldHealth.text = oldStats.maxHealth.ToString();
        newHealth.text = newStats.maxHealth.ToString();
        
        oldAttack.text = oldStats.attack.ToString();
        newAttack.text = newStats.attack.ToString();
        
        oldDefense.text = oldStats.defense.ToString();
        newDefense.text = oldStats.defense.ToString();
        
        oldSpecialAttack.text = oldStats.specialAttack.ToString();
        newSpecialAttack.text = newStats.specialAttack.ToString();
        
        oldSpecialDefense.text = oldStats.specialDefense.ToString();
        newSpecialDefense.text = newStats.specialAttack.ToString();
        
        oldSpeed.text = oldStats.speed.ToString();
        newSpeed.text = newStats.speed.ToString();
    }
}
