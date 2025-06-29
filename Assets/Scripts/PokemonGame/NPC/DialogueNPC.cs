using PokemonGame.Dialogue;

namespace PokemonGame.NPC
{
    using UnityEngine;
    public class DialogueNPC : NPC
    {
        [SerializeField] private TextAsset textAsset;

        protected override void OnPlayerInteracted()
        {
            QueDialogue(textAsset, DialogueBoxType.Dialogue);
            base.OnPlayerInteracted();
        }
    }   
}
