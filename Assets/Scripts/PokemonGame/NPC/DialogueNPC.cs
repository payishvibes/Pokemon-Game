using PokemonGame.Dialogue;

namespace PokemonGame.NPC
{
    using UnityEngine;
    public class DialogueNPC : NPC
    {
        [SerializeField] private TextAsset textAsset;

        protected override void OnPlayerInteracted()
        {
            Debug.Log("queued dialogue");
            QueDialogue(textAsset, DialogueBoxType.Dialogue);
            base.OnPlayerInteracted();
        }
    }   
}
