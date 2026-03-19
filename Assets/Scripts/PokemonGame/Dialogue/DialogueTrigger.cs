using System.Collections.Generic;

namespace PokemonGame.Dialogue
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Can be extended by a class to create something that triggers dialogue
    /// </summary>
    public class DialogueTrigger : MonoBehaviour
    {
        /// <summary>
        /// Allow this dialogue trigger to use global tags
        /// </summary>
        public bool allowsGlobalTags = true;

        /// <summary>
        /// Dialogue that the trigger queued was called
        /// </summary>
        public event EventHandler DialogueWasCalled;
        /// <summary>
        /// Dialogue that the trigger queued ended
        /// </summary>
        public event EventHandler DialogueFinished;

        /// <summary>
        /// Queue dialogue to play
        /// </summary>
        /// <param name="textAsset">The text asset that the dialogue sequence draws from</param>
        /// <param name="boxType">The type of dialogue box the dialogue will use</param>
        /// <param name="id">The id of the dialogue that can be used to identify it</param>
        /// <param name="autostart">Automatically start the dialogue, turn off to start dialogue yourself, on by default</param>
        /// <param name="variables">Variables to pass into the dialogue when it plays</param>
        public void QueDialogue(TextAsset textAsset, DialogueBoxType boxType, string id = "", bool autostart = true, Dictionary<string, string> variables = null)
        {
            if (autostart)
            {
                DialogueManager.instance.QueDialogue(textAsset, this, id, true, boxType, variables);
                DialogueWasCalled?.Invoke(gameObject, EventArgs.Empty);
            }
            else
            {
                DialogueManager.instance.QueDialogue(textAsset, this, id, false, boxType);
            }
        }
        
        /// <summary>
        /// Queue dialogue to play
        /// </summary>
        /// <param name="text">The text that is read out</param>
        /// <param name="boxType">The type of dialogue box the dialogue will use</param>
        /// <param name="id">The id of the dialogue that can be used to identify it</param>
        /// <param name="autostart">Automatically start the dialogue, turn off to start dialogue yourself, on by default</param>
        public void QueDialogue(string text, DialogueBoxType boxType, string id = "", bool autostart = true)
        {
            if (autostart)
            {
                DialogueManager.instance.QueDialogue(text, this, id, true, boxType);
                DialogueWasCalled?.Invoke(gameObject, EventArgs.Empty);
            }
            else
            {
                DialogueManager.instance.QueDialogue(text, this, id, false, boxType);
            }
        }

        /// <summary>
        /// Forcibly stops the next dialogue queued from autoplaying
        /// </summary>
        protected void ForceStopNextQueued()
        {
            DialogueManager.instance.ForceStopNextQueued();
        }
        
        /// <summary>
        /// Starts an INK Dialogue sequence only if we queued one and told it not to autostart
        /// </summary>
        protected void StartDialogue()
        {
            if(DialogueManager.instance.currentTrigger == this && !DialogueManager.instance.DialogueIsPlaying)
            {
                DialogueManager.instance.StartDialogue(this);
                DialogueWasCalled?.Invoke(gameObject, EventArgs.Empty);
            }
        }

        public void ClearQueue()
        {
            DialogueManager.instance.ClearQueue();
        }

        /// <summary>
        /// Set dialogue variables for the current story
        /// </summary>
        /// <param name="variables">Variables to set</param>
        public void SetDialogueVariables(Dictionary<string, string> variables)
        {
            DialogueManager.instance.SetDialogueVariables(variables);
        }

        /// <summary>
        /// Ends the dialogue and calls the Dialogue Finished event
        /// </summary>
        public void EndDialogue()
        {
            DialogueFinished?.Invoke(gameObject, EventArgs.Empty);
        }

        /// <summary>
        /// Inheritors override this to handle tags
        /// </summary>
        /// <param name="tagKey">The tag key</param>
        /// <param name="tagValues">The tag values</param>
        public virtual void CallTag(string tagKey, string[] tagValues)
        {
            
        }
    }   
}