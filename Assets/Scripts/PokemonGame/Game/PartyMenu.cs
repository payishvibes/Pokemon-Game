using PokemonGame.Game.Party;
using PokemonGame.General;
using UnityEngine;

public class PartyMenu : MonoBehaviour
{
    [SerializeField] private MenuBattlerDisplay displayPrefab;
    [SerializeField] private Transform[] partyDisplayPositions;

    private void Start()
    {
        Party currentPlayerParty = PartyManager.GetParty();
        
        if(currentPlayerParty != null)
        {
            for (int i = 0; i < currentPlayerParty.Count; i++)
            {
                Battler currentBattler = currentPlayerParty[i];
                MenuBattlerDisplay display = Instantiate(displayPrefab, partyDisplayPositions[i]);
                display.Init(currentBattler.name, currentBattler.currentHealth, currentBattler.maxHealth,
                    currentBattler.statusEffect, currentBattler.exp,
                    ExperienceCalculator.RequiredForNextLevel(currentBattler),
                    currentBattler.source.texture);
            }
        }
    }
}
