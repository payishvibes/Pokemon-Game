using PokemonGame.Game.Party;
using PokemonGame.General;
using UnityEngine;
using UnityEngine.UI;

public class PartyMenu : MonoBehaviour
{
    [SerializeField] private MenuBattlerDisplay displayPrefab;
    [SerializeField] private Transform[] partyDisplayPositions;
    [SerializeField] private GameObject mainScreen;
    [SerializeField] private GameObject detailsScreen;

    private void Start()
    {
        Party currentPlayerParty = PartyManager.GetParty();
        
        if(currentPlayerParty != null)
        {
            for (int i = 0; i < currentPlayerParty.Count; i++)
            {
                Battler currentBattler = currentPlayerParty[i];
                MenuBattlerDisplay display = Instantiate(displayPrefab, partyDisplayPositions[i]);
                display.Init(currentBattler.name, currentBattler.currentHealth, currentBattler.stats.maxHealth,
                    currentBattler.statusEffect, currentBattler.exp,
                    ExperienceCalculator.RequiredForNextLevel(currentBattler),
                    currentBattler.GetSpriteFront());
                int index = i;
                display.GetComponentInChildren<Button>().onClick.AddListener((() =>
                {
                    OpenDetails(index);
                }));
            }
        }
    }

    private void OpenDetails(int i)
    {
        mainScreen.SetActive(false);
        detailsScreen.SetActive(true);
    }
    
    public void BackToMainScreen()
    {
        mainScreen.SetActive(true);
        detailsScreen.SetActive(false);
    }
}
