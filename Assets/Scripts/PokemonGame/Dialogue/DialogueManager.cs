using PokemonGame.General;
using PokemonGame.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace PokemonGame.Dialogue
{
    using System;
    using UnityEngine;
    using TMPro;
    using Ink.Runtime;
    using System.Collections;
    using System.Collections.Generic;
    using Game;
    using UnityEngine.SceneManagement;
    
    /// <summary>
    /// Manages all dialogue in the game, if dialogue is wanted in a scene, must have an instance inside of said scene
    /// </summary>
    public class DialogueManager : MonoBehaviour
    { 
        public static DialogueManager instance;
        
        [Header("Dialogue Panels")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private List<GameObject> dialoguePanels;
        
        [Space]
        [SerializeField] private GameObject[] choices;
        [SerializeField] private int currentChoicesAmount;
        [SerializeField] private int maxCharAmount;
        [SerializeField] private float textScrollSpeed = 1;
        private Story _currentStory;
        [SerializeField] private TextAsset globalsInkFile;
        private TextMeshProUGUI[] _choicesText;
        // TODO: replace this god awful global variable system, like jesus christ
        private DialogueVariables _dialogueVariables;
        private DialogueMethods _dialogueMethods;

        public bool dialogueIsPlaying { get; private set; }
        public DialogueTrigger currentTrigger;
        [SerializeField] private PlayerMovement movement;
        
        public event EventHandler<DialogueStartedEventArgs> DialogueStarted;
        public event EventHandler<DialogueEndedEventArgs> DialogueEnded;
        public event EventHandler<DialogueChoiceEventArgs> DialogueChoice;

        private Queue<QueuedDialogue> _queue = new Queue<QueuedDialogue>();

        private bool isInBattle => SceneManager.GetActiveScene().name == "Battle";

        private string[] tempNextLines;
        private int currentTempIndex;
        
        private DialogueBoxType currentDialogueBoxType;

        public int cancelChoiceId = -1;

        private QueuedDialogue currentQueuedDialogue;

        private bool _forceStopNextQueued;
        
        private void Awake()
        {
            instance = this;
            
            SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;
    
            _dialogueVariables = new DialogueVariables(globalsInkFile);
            _dialogueMethods = new DialogueMethods();
        }
        
        private void SceneManagerOnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            string name = SceneManager.GetActiveScene().name;
            if (name != "Battle" || name == "Boot")
            {
                movement = FindFirstObjectByType<PlayerMovement>();
            }
        }

        public List<Choice> CurrentChoices()
        {
            return _currentStory.currentChoices;
        }

        private bool HasChoices()
        {
            var hasChoices = false;
            if (currentQueuedDialogue.ink)
            {
                currentChoicesAmount = _currentStory.currentChoices.Count;
                hasChoices = currentChoicesAmount > 0;
            }

            if (tempNextLines != null)
            {
                if (currentTempIndex != tempNextLines.Length-1)
                {
                    hasChoices = false;
                }
            }

            return hasChoices;
        }

        public void PressedContinue()
        {
            if (_textAnimPlaying)
            {
                _textAnimPlaying = false;
            }
            else if (!HasChoices())
            {
                ContinueStory();
            }
        }

        private void BackPressed(InputAction.CallbackContext context)
        {
            if (cancelChoiceId != -1 && HasChoices() && dialogueIsPlaying)
            {
                MakeChoice(cancelChoiceId);
            }
        }

        private void OnEnable()
        {
            InputSystem.actions.FindAction("Escape").performed += BackPressed;
        }

        private void OnDisable()
        {
            InputSystem.actions.FindAction("Escape").performed -= BackPressed;
        }

        private void Start()
        {
            dialogueIsPlaying = false;
            dialoguePanel.SetActive(false);
            _choicesText = new TextMeshProUGUI[choices.Length];
            
            int index = 0;
            foreach (GameObject choice in choices)
            {
                _choicesText[index] = choice.GetComponentInChildren<TextMeshProUGUI>();
                index++;
            }
    
            for (int i = 0; i < choices.Length; i++)
            {
                _choicesText[i] = choices[i].GetComponentInChildren<TextMeshProUGUI>();
            }
        }
        
        /// <summary>
        /// Starts a new conversation
        /// </summary>
        /// <param name="inkJson">The TextAsset with the information about the conversation</param>
        /// <param name="trigger">The DialogueTrigger that triggered this conversation</param>
        /// <param name="id">The id that can be used to identify the dialogue</param>
        /// <param name="autostart">Automatically start the dialogue on load, on by default</param>
        /// <param name="variables">The variables that can be loaded into the ink file</param>
        public void QueDialogue(TextAsset inkJson, DialogueTrigger trigger, string id, bool autostart, DialogueBoxType boxType, Dictionary<string, string> variables = null)
        {
            QueuedDialogue dialogue = new QueuedDialogue(trigger, variables, id, inkJson, boxType, autostart && !_forceStopNextQueued);
            if (_queue.Count > 0 || dialogueIsPlaying) // if we can actually begin the dialogue or if we need to wait
            {
                _queue.Enqueue(dialogue);
            }
            else // load it now
            {
                LoadDialogueFromQueue(dialogue);
            }
            _forceStopNextQueued = false;
        }
        
        /// <summary>
        /// Starts a new conversation
        /// </summary>
        /// <param name="text">The text of the conversation</param>
        /// <param name="trigger">The DialogueTrigger that triggered this conversation</param>
        /// <param name="autostart">Automatically start the dialogue on load, on by default</param>
        public void QueDialogue(string text, DialogueTrigger trigger, string id, bool autostart, DialogueBoxType boxType)
        {
            QueuedDialogue dialogue = new QueuedDialogue(trigger, id, text, boxType, autostart && !_forceStopNextQueued);
            if (_queue.Count > 0 || dialogueIsPlaying) // if we can actually begin the dialogue or if we need to wait
            {
                _queue.Enqueue(dialogue);
            }
            else // load it now
            {
                LoadDialogueFromQueue(dialogue);
            }
            _forceStopNextQueued = false;
        }

        public void ForceStopNextQueued()
        {
            _forceStopNextQueued = true;
        }

        /// <summary>
        /// Load dialogue from the queue
        /// </summary>
        /// <param name="dialogueToLoad">The queued dialogue to load</param>
        private void LoadDialogueFromQueue(QueuedDialogue dialogueToLoad)
        {
            currentTrigger = dialogueToLoad.trigger;
            currentQueuedDialogue = dialogueToLoad;
            
            if(!isInBattle)
                movement.canMove = false;
            
            if (dialogueToLoad.ink)
            {
                _currentStory = new Story(dialogueToLoad.textAsset.text);
            
                _dialogueVariables.StartListening(_currentStory);
                if(dialogueToLoad.variables != null)
                {
                    SetDialogueVariables(dialogueToLoad.variables);
                }
            }
            
            if (dialogueToLoad.autoStart)
            {
                StartLoadedDialogue(dialogueToLoad);
            }
            else
            {
                dialogueIsPlaying = false;
                dialoguePanel.SetActive(false);
            }
        }

        /// <summary>
        /// Start dialogue that you queued
        /// </summary>
        /// <param name="trigger">Used to tell if you were the one that queued it</param>
        public void StartDialogue(DialogueTrigger trigger)
        {
            if (!currentQueuedDialogue.autoStart)
            {
                if(currentTrigger == trigger)
                {
                    StartLoadedDialogue(currentQueuedDialogue);
                    _forceStopNextQueued = false;
                }
                else
                {
                    Debug.LogWarning("The trigger to start the dialogue must be the one that queued it!");
                }
            }
            else
            {
                _forceStopNextQueued = false;
            }
        }

        private void StartLoadedDialogue(QueuedDialogue dialogueToLoad)
        {
            if (dialogueToLoad.autoStart)
            {
                DialogueStarted?.Invoke(this,
                    new DialogueStartedEventArgs(dialogueToLoad.trigger, currentQueuedDialogue.id,
                        currentQueuedDialogue.textAsset));
            }
            else
            {
                DialogueStarted?.Invoke(this,
                    new DialogueStartedEventArgs(dialogueToLoad.trigger, currentQueuedDialogue.id,
                        currentQueuedDialogue.text));
            }
            dialogueIsPlaying = true;
            dialoguePanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ContinueStory();
        }

        public void ClearQueue()
        {
            _queue.Clear();
        }

        /// <summary>
        /// Set dialogue variables for the current story
        /// </summary>
        /// <param name="variables">The dialogue variables to set</param>
        public void SetDialogueVariables(Dictionary<string, string> variables)
        {
            foreach (var variable in variables)
            {
                _currentStory.variablesState[variable.Key] = variable.Value;
            }
        }
        
        /// <summary>
        /// Exit current dialogue and either start the next or finish up
        /// </summary>
        /// <returns></returns>
        private IEnumerator ExitDialogueMode()
        {
            if (_queue.Count > 0)
            {
                string id = currentQueuedDialogue.id;
                LoadDialogueFromQueue(_queue.Dequeue());
                DialogueEnded?.Invoke(this, new DialogueEndedEventArgs(currentTrigger, currentQueuedDialogue, id, true));
            }
            else
            {
                yield return new WaitForSeconds(0.2f);
                string id = currentQueuedDialogue.id;
                if (currentQueuedDialogue.ink)
                {
                    _dialogueVariables.StopListening(_currentStory);
                }
                if(!isInBattle)
                {
                    movement.canMove = true;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                dialogueIsPlaying = false;
                dialoguePanel.SetActive(false);
                currentTrigger.EndDialogue();
                DialogueEnded?.Invoke(this, new DialogueEndedEventArgs(currentTrigger, currentQueuedDialogue, id, false));
            }
        }
        
        /// <summary>
        /// Continue the current story
        /// </summary>
        private void ContinueStory()
        {
            dialogueIsPlaying = true;
            StopAllCoroutines();
            ClearChoices();

            if (tempNextLines == null)
            {
                if (currentQueuedDialogue.ink)
                {
                    if (_currentStory.canContinue)
                    {
                        string next = _currentStory.Continue();

                        if (string.IsNullOrEmpty(next))
                        {
                            ContinueStory();
                        }
                        else
                        {
                            if (next.Length >= maxCharAmount)
                            {
                                string[] newNextLines = next.SplitIntoParts(maxCharAmount);

                                tempNextLines = newNextLines;
                                StartCoroutine(DisplayText(newNextLines[currentTempIndex], currentQueuedDialogue.boxType, false, true));
                            }
                            else
                            {
                                StartCoroutine(DisplayText(next, currentQueuedDialogue.boxType, true, true));
                            }
                        }
                    }
                    else
                    {
                        StartCoroutine(ExitDialogueMode());
                    }   
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentQueuedDialogue.text))
                    {
                        string next = currentQueuedDialogue.text;
                        
                        if (string.IsNullOrEmpty(next))
                        {
                            ContinueStory();
                        }
                        else
                        {
                            if (next.Length >= maxCharAmount)
                            {
                                string[] newNextLines = next.SplitIntoParts(maxCharAmount);

                                tempNextLines = newNextLines;
                                StartCoroutine(DisplayText(newNextLines[currentTempIndex], currentQueuedDialogue.boxType, false, false));
                            }
                            else
                            {
                                StartCoroutine(DisplayText(next, currentQueuedDialogue.boxType, false, false));
                            }

                            currentQueuedDialogue.text = string.Empty;
                        }
                    }
                    else
                    {
                        StartCoroutine(ExitDialogueMode());
                    } 
                }
            }
            else
            {
                if (currentQueuedDialogue.ink)
                {
                    if (currentTempIndex >= tempNextLines.Length - 1)
                    {
                        // exhausted our leftovers
                        tempNextLines = null;
                        currentTempIndex = 0;
                    
                        if(_currentStory.canContinue)
                        {
                            StartCoroutine(DisplayText(_currentStory.Continue(), currentQueuedDialogue.boxType, true, true));
                        }
                        else
                        {
                            StartCoroutine(ExitDialogueMode());
                        }
                    }
                    else
                    {
                        currentTempIndex++;
                        StartCoroutine(DisplayText(tempNextLines[currentTempIndex], currentQueuedDialogue.boxType,
                            currentTempIndex == tempNextLines.Length - 1, false));
                    }   
                }
                else
                {
                    if (currentTempIndex >= tempNextLines.Length - 1)
                    {
                        // exhausted our leftovers
                        tempNextLines = null;
                        currentTempIndex = 0;
                    
                        if(string.IsNullOrEmpty(currentQueuedDialogue.text))
                        {
                            StartCoroutine(DisplayText(_currentStory.Continue(), currentQueuedDialogue.boxType, false, false));
                        }
                        else
                        {
                            StartCoroutine(ExitDialogueMode());
                        }
                    }
                    else
                    {
                        currentTempIndex++;
                        StartCoroutine(DisplayText(tempNextLines[currentTempIndex], currentQueuedDialogue.boxType,
                            currentTempIndex == tempNextLines.Length - 1, false));
                    }  
                }
            }
        }

        private void ClearChoices()
        {
            foreach (var choiceButton in choices)
            {
                choiceButton.gameObject.SetActive(false);
            }
            currentChoicesAmount = 0;
        }

        private bool _textAnimPlaying = false;
        
        private IEnumerator DisplayText(string nextSentence, DialogueBoxType boxType, bool showChoices, bool handleTags)
        {
            _textAnimPlaying = true;
            
            EventSystem.current.SetSelectedGameObject(dialoguePanel.GetComponentsInChildren<Button>()[0].gameObject);

            currentDialogueBoxType = boxType;

            foreach (var panel in dialoguePanels)
            {
                panel.SetActive(false);
            }

            int boxTypeIndex = (int)boxType;

            dialoguePanels[boxTypeIndex].SetActive(true);
            EventSystem.current.SetSelectedGameObject(dialoguePanels[boxTypeIndex].gameObject);
            TextMeshProUGUI textToUse = dialoguePanels[boxTypeIndex].GetComponentInChildren<TextMeshProUGUI>();;
            ExpandingButton buttonToUse = dialoguePanels[boxTypeIndex].GetComponent<ExpandingButton>();
            GameObject indicatorToUse = dialoguePanels[boxTypeIndex].transform.GetChild(1).GetChild(0).gameObject;
            
            indicatorToUse.SetActive(false);

            bool interupted = false;
            
            // actually display text lol, everything else is just additional stuff
            textToUse.text = "";
            foreach (char letter in nextSentence)
            {
                if (!_textAnimPlaying)
                {
                    interupted = true;
                    break;
                }
                textToUse.text += letter;
                yield return new WaitForSeconds(0.025f * textScrollSpeed);
            }

            textToUse.text = nextSentence;
            if (interupted)
            {
                buttonToUse.SetPreviousScale();
            }
            _textAnimPlaying = false;

            if (showChoices)
            {
                StartCoroutine(DisplayChoices());
            }
            
            if (currentChoicesAmount == 0)
            {
                indicatorToUse.SetActive(true);
            }

            if (handleTags)
            {
                HandleTags(_currentStory.currentTags);
            }
        }
        
        private void HandleTags(List<string> currentTags)
        {
            foreach(string tag in currentTags)
            {
                string[] splitTag = tag.Split(':');
    
                string tagKey = "";
                string tagValue = "";
                string[] tagValues = null;

                if(splitTag.Length > 2)
                {
                    Debug.LogError("Tag could not be appropriately parsed: " + tag);
                }
                else if(splitTag.Length == 2)
                {
                    tagKey = splitTag[0].Trim();
                    tagValue = splitTag[1].Trim();

                    tagValues = tagValue.Split('.');
                }
                else
                {
                    // only the key is provided
                    tagKey = tag;
                }

                if (currentTrigger.allowsGlobalTags)
                {
                    _dialogueMethods.HandleGlobalTag(tagKey, tagValues);
                }
                
                currentTrigger.CallTag(tagKey, tagValues);
            }
        }
        
        private IEnumerator DisplayChoices()
        {
            List<Choice> currentChoices = _currentStory.currentChoices;
            currentChoicesAmount = currentChoices.Count;
            
            if (currentChoices.Count > choices.Length)
            {
                Debug.LogError("More choices were given than the UI can support either add more choice buttons are take away choices. Number of choices given: "
                    + currentChoices.Count);
            }

            // Change the navigation up from the dialogue panel to the lowest dialogue choice button
            if (currentChoices.Count > 0)
            {
                Button dialogueNavButton = dialoguePanels[0].GetComponent<Button>();
                
                switch (currentDialogueBoxType)
                {
                    case DialogueBoxType.Dialogue:
                        dialogueNavButton = dialoguePanels[0].GetComponent<Button>(); 
                        break;
                    case DialogueBoxType.Narration:
                        dialogueNavButton = dialoguePanels[1].GetComponent<Button>();
                        break;
                    case DialogueBoxType.Event:
                        dialogueNavButton = dialoguePanels[2].GetComponent<Button>();
                        break;
                }
                
                Navigation nav = dialogueNavButton.navigation;
                nav.selectOnUp = _choicesText[currentChoices.Count - 1].transform.parent.GetComponent<Button>();
                dialogueNavButton.navigation = nav;
            }
            
            int index = 0;
            
            foreach(Choice choice in currentChoices)
            {
                choices[index].gameObject.SetActive(true);
                _choicesText[index].text = choice.text;
                index++;
            }
            
            for(int i = index; i < choices.Length; i++)
            {
                choices[i].gameObject.SetActive(false);
                yield return null;
            }
        }
        
        /// <summary>
        /// Makes the choice that the player wants using the index of the choice
        /// </summary>
        /// <param name="choiceIndex">The choicer index of the player wants to make</param>
        public void MakeChoice(int choiceIndex)
        {
            _currentStory.ChooseChoiceIndex(choiceIndex);

            cancelChoiceId = -1;

            DialogueChoiceEventArgs args = new DialogueChoiceEventArgs(currentTrigger, choiceIndex);

            DialogueChoice?.Invoke(this, args);
            
            ContinueStory();
        }
    }

    /// <summary>
    /// Event arguments for dialogue starting
    /// </summary>
    public class DialogueStartedEventArgs : EventArgs
    {
        /// <summary>
        /// The dialogue trigger that started the dialogue
        /// </summary>
        public DialogueTrigger trigger;
        /// <summary>
        /// Id of the dialogue that started
        /// </summary>
        public string id;
        /// <summary>
        /// The text asset that was used for the dialogue
        /// </summary>
        public TextAsset textAsset;
        /// <summary>
        /// Weather the dialogue comes from an ink file or simply a single string
        /// </summary>
        public bool ink;
        /// <summary>
        /// Raw dialogue, only used when
        /// </summary>
        public string text;
    
        public DialogueStartedEventArgs(DialogueTrigger trigger, string id, TextAsset textAsset)
        {
            this.trigger = trigger;
            this.textAsset = textAsset;
            this.id = id;
            ink = true;
        }
        
        public DialogueStartedEventArgs(DialogueTrigger trigger, string id, string text)
        {
            this.trigger = trigger;
            this.text = text;
            this.id = id;
            ink = false;
        }
    }
    
    
    /// <summary>
    /// Event arguments for dialogue ending
    /// </summary>
    public class DialogueEndedEventArgs : EventArgs
    {
        /// <summary>
        /// The dialogue trigger that started the dialogue that just ended
        /// </summary>
        public DialogueTrigger trigger;
        /// <summary>
        /// The dialogue item that just ended
        /// </summary>
        public QueuedDialogue queuedDialogue;
        /// <summary>
        /// Weather there is more dialogue queued
        /// </summary>
        public bool moreToGo;
        /// <summary>
        /// Id of the dialogue that finished
        /// </summary>
        public string id;
    
        public DialogueEndedEventArgs(DialogueTrigger trigger, QueuedDialogue queuedDialogue, string id, bool moreToGo)
        {
            this.trigger = trigger;
            this.queuedDialogue = queuedDialogue;
            this.id = id;
            this.moreToGo = moreToGo;
        }
    }

    public class DialogueChoiceEventArgs : EventArgs
    {
        public DialogueTrigger trigger;
        public int choiceIndex;

        public DialogueChoiceEventArgs(DialogueTrigger trigger, int choiceIndex)
        {
            this.trigger = trigger;
            this.choiceIndex = choiceIndex;
        }
    }

    /// <summary>
    /// Container for queued dialogue
    /// </summary>
    public class QueuedDialogue
    {
        /// <summary>
        /// Trigger that queued it
        /// </summary>
        public DialogueTrigger trigger;
        /// <summary>
        /// Identifying string for the dialogue
        /// </summary>
        public string id;
        /// <summary>
        /// Variables to set
        /// </summary>
        public Dictionary<string, string> variables;
        /// <summary>
        /// Automatically start upon dequeing
        /// </summary>
        public bool autoStart;
        /// <summary>
        /// Text asset to construct the dialogue from
        /// </summary>
        public TextAsset textAsset;
        /// <summary>
        /// Weather the dialogue comes from an ink file or simply a string
        /// </summary>
        public bool ink;
        /// <summary>
        /// Raw dialogue, only used when not an ink file
        /// </summary>
        public string text;
        /// <summary>
        /// The type of box the dialogue will be displayed in
        /// </summary>
        public DialogueBoxType boxType;

        public QueuedDialogue(DialogueTrigger trigger, Dictionary<string, string> variables, string id, TextAsset textAsset, DialogueBoxType boxType, bool autoStart = true)
        {
            this.trigger = trigger;
            this.variables = variables;
            this.id = id;
            this.textAsset = textAsset;
            this.autoStart = autoStart;
            this.boxType = boxType;
            ink = true;
        }
        
        public QueuedDialogue(DialogueTrigger trigger, string id, string text, DialogueBoxType boxType, bool autoStart = true)
        {
            this.trigger = trigger;
            this.id = id;
            this.text = text;
            this.autoStart = autoStart;
            this.boxType = boxType;
            ink = false;
        }
    }

    public enum DialogueBoxType
    {
        Dialogue = 0,
        Narration,
        Event
    }
}