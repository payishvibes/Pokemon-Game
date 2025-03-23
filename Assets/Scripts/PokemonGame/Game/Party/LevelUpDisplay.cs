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

    public void Init(int oldHealth, int newHealth, int oldAttack, int newAttack, int oldDefense, int newDefense,
        int oldSpAttack, int newSpAttack, int oldSpDefense, int newSpDefense, int oldSpeed, int newSpeed)
    {
        this.oldHealth.text = oldHealth.ToString();
        this.newHealth.text = newHealth.ToString();
        
        this.oldAttack.text = oldAttack.ToString();
        this.newAttack.text = newAttack.ToString();
        
        this.oldDefense.text = oldDefense.ToString();
        this.newDefense.text = newDefense.ToString();
        
        oldSpecialAttack.text = oldSpAttack.ToString();
        newSpecialAttack.text = newSpAttack.ToString();
        
        oldSpecialDefense.text = oldSpDefense.ToString();
        newSpecialDefense.text = newSpDefense.ToString();
        
        this.oldSpeed.text = oldSpeed.ToString();
        this.newSpeed.text = newSpeed.ToString();
    }
}
