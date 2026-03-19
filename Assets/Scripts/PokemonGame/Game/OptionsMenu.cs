using PokemonGame.Dialogue;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PokemonGame.Game
{
    using UnityEngine;

    public class OptionsMenu : MonoBehaviour
    {
        public static OptionsMenu instance;

        [SerializeField] private PlayerMovement movement;

        public bool on => state;
        
        [SerializeField] private bool state;
        [SerializeField] private GameObject firstSelectedButton;
        [SerializeField] private GameObject menuObject;
        [SerializeField] private GameObject defaultMenuObject;

        [SerializeField] private GameObject[] menuObjects;
        [SerializeField] private GameObject currentMenu;

        private void Awake()
        {
            SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;
            instance = this;
        }

        public void OpenSeparateMenu(int menuIndex)
        {
            defaultMenuObject.SetActive(false);
            currentMenu = Instantiate(menuObjects[menuIndex], transform);
        }

        public void CloseCurrentMenu()
        {
            defaultMenuObject.SetActive(true);
            Destroy(currentMenu);
            currentMenu = null;
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }

        private void Back()
        {
            if (currentMenu == null && on)
            {
                ToggleMenu();
            }
        }

        private void SceneManagerOnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (SceneManager.GetActiveScene().name != "Battle")
            {
                movement = FindFirstObjectByType<PlayerMovement>();
            }
        }

        private void Start()
        {
            DialogueManager.instance.OnDialogueStarted += InstanceOnDialogueStarted;
        }

        private void InstanceOnDialogueStarted(object sender, DialogueStartedEventArgs e)
        {
            if (state)
            {
                ToggleMenu();
            }
        }

        private void OnEnable()
        {
            InputSystem.actions.FindAction("Bag").performed += OnOptionsOpened;
            InputSystem.actions.FindAction("Escape").performed += OnEscapePressed;
        }

        private void OnDisable()
        {
            InputSystem.actions.FindAction("Bag").performed -= OnOptionsOpened;
            InputSystem.actions.FindAction("Escape").performed -= OnEscapePressed;
            DialogueManager.instance.OnDialogueStarted -= InstanceOnDialogueStarted;
        }

        private void OnOptionsOpened(InputAction.CallbackContext obj)
        {
            if (!DialogueManager.instance.DialogueIsPlaying && SceneManager.GetActiveScene().name != "Battle")
            {
                ToggleMenu();
            }
        }

        private void OnEscapePressed(InputAction.CallbackContext obj)
        {
            Back();
        }
        
        private void ToggleMenu()
        {
            state = !state;

            if (!state)
            {
                defaultMenuObject.SetActive(true);
            
                Destroy(currentMenu);
                currentMenu = null;
            }
            
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
            
            movement.canMove = !state;
            menuObject.SetActive(state);
            Cursor.visible = state;
            Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;
        }

        public void CloseMenu()
        {
            if (state)
            {
                ToggleMenu();
            }
        }
    }   
}
