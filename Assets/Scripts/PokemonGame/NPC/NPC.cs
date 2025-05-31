using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace PokemonGame.NPC
{
    using System;
    using Dialogue;
    using Game;
    using UnityEngine;

    /// <summary>
    /// Base class of all NPCs, contains functionality for detecting the player nearby and having an OnPlayerInteract() override method
    /// </summary>
    public class NPC : DialogueTrigger
    {
        [Header("Visual Cue")]
        [SerializeField] private GameObject visualCue;
        [Space]

        [Header("Interactable")]
        public bool interactable = true;
        public bool customInteractionStopHandling = false;
        public List<GameObject> interactionObjects;

        private bool _playerInteracting = false;

        private bool _playerInRange;
        protected Player Player;

        private void OnValidate()
        {
            if (visualCue == null)
            {
                Debug.Log("interact cue not there");
                visualCue = Instantiate(Resources.Load<GameObject>("Pokemon Game/NPC/Interact Cue"), transform);
            }
        }

        private void DialogueEnded(object sender, DialogueEndedEventArgs e)
        {
            foreach (var interactionObject in interactionObjects)
            {
                interactionObject.SetActive(true);
            }
        }

        private void OnDialogueStarted(object sender, DialogueStartedEventArgs e)
        {
            foreach (var interactionObject in interactionObjects)
            {
                interactionObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            InputSystem.actions.FindAction("Interact").performed += OnPerformed;
            DialogueManager.instance.DialogueStarted += OnDialogueStarted;
            DialogueManager.instance.DialogueEnded += DialogueEnded;
            OverrideOnEnable();
        }

        protected virtual void OverrideOnEnable()
        {
            
        }

        protected virtual void OverrideOnDisable()
        {
            
        }

        private void OnDisable()
        {
            DialogueManager.instance.DialogueStarted -= OnDialogueStarted;
            DialogueManager.instance.DialogueEnded -= DialogueEnded;
            InputSystem.actions.FindAction("Interact").performed -= OnPerformed;
            OverrideOnDisable();
        }

        private bool _canInteract;

        private void OnPerformed(InputAction.CallbackContext obj)
        {
            if (_canInteract)
            {
                OnPlayerInteracted();
            }
        }

        private void Awake()
        {
            _playerInRange = false;
            visualCue.SetActive(false);
            DialogueFinished += StopPlayerLooking;
        }

        private void StopPlayerLooking(object o, EventArgs e)
        {
            if (_playerInteracting && !customInteractionStopHandling)
            {
                Player.interacting = false;
                Player.StopLooking();
                _playerInteracting = false;
            }
        }

        private void Update()
        {
            visualCue.SetActive(false);
            _canInteract = false;
            if(interactable)
            {
                if (_playerInRange && !Player.interacting)
                {
                    if (!DialogueManager.instance.dialogueIsPlaying)
                    {
                        _canInteract = true;
                        visualCue.SetActive(true);
                    }
                }
            }
            OverrideUpdate();
        }

        protected virtual void OverrideUpdate()
        {
            
        }

        /// <summary>
        /// Called when the player interacts with the NPC
        /// </summary>
        protected virtual void OnPlayerInteracted()
        {
            Player.interacting = true;
            Player.LookAtTarget(transform.position);
            _playerInteracting = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if(interactable)
            {
                if (other.gameObject.CompareTag("Player"))
                {
                    _playerInRange = true;
                    Player = other.GetComponent<Player>();
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if(interactable)
            {
                if (other.gameObject.CompareTag("Player"))
                {
                    _playerInRange = false;
                    Player = null;
                }
            }
        }
    }
}