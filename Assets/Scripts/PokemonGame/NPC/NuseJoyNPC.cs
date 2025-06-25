using PokemonGame.Dialogue;
using UnityEngine;

namespace PokemonGame.NPC
{
    public class NuseJoyNPC : DialogueNPC
    {
        [SerializeField] private TextAsset forcedHealDialogue;

        public void ForceHealDialogue()
        {
            QueDialogue(forcedHealDialogue, DialogueBoxType.Dialogue);
        }
    }
}
