using System.Collections;
using PokemonGame.Battle;
using PokemonGame.General;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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

    private BattlerStats oldStats;
    private BattlerStats newStats;

    private bool updated;

    public void Init(BattlerStats oldStats, BattlerStats newStats)
    {
        this.oldStats = oldStats;
        this.newStats = newStats;
        
        oldHealth.text = oldStats.maxHealth.ToString();
        newHealth.text = "";
        
        oldAttack.text = oldStats.attack.ToString();
        newAttack.text = "";
        
        oldDefense.text = oldStats.defense.ToString();
        newDefense.text = "";
        
        oldSpecialAttack.text = oldStats.specialAttack.ToString();
        newSpecialAttack.text = "";
        
        oldSpecialDefense.text = oldStats.specialDefense.ToString();
        newSpecialDefense.text = "";
        
        oldSpeed.text = oldStats.speed.ToString();
        newSpeed.text = "";
    }

    private void OnEnable()
    {
        InputSystem.actions.FindAction("Interact").performed += OnPerformed;
    }

    private void OnPerformed(InputAction.CallbackContext obj)
    {
        Continue();
    }

    private void OnDisable()
    {
        InputSystem.actions.FindAction("Interact").performed -= OnPerformed;
    }

    private void Continue()
    {
        if (!updated)
        {
            newHealth.text = newStats.maxHealth.ToString();
        
            newAttack.text = newStats.attack.ToString();
        
            newDefense.text = oldStats.defense.ToString();
        
            newSpecialAttack.text = newStats.specialAttack.ToString();
        
            newSpecialDefense.text = newStats.specialAttack.ToString();
        
            newSpeed.text = newStats.speed.ToString();

            updated = true;
        }
        else
        {
            StartCoroutine(DelayByFrame());
        }
    }

    private IEnumerator DelayByFrame()
    {
        yield return new WaitForEndOfFrame();
        PlayerBattleController.Instance.FinishedViewingLevelUpScreen();
    }
}
