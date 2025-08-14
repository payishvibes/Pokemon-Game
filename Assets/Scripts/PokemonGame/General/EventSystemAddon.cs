using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PokemonGame.General
{
    [RequireComponent(typeof(EventSystem))]
    public class EventSystemAddon : MonoBehaviour
    {
        private EventSystem _system;
        private GameObject _lastSelected;

        private void Awake()
        {
            _system = GetComponent<EventSystem>();
        }

        private void Update()
        {
            if (!_system.currentSelectedGameObject && _lastSelected)
            {
                if (_lastSelected.activeInHierarchy)
                {
                    _system.SetSelectedGameObject(_lastSelected);
                }
                else
                {
                    Button button = FindAnyObjectByType<Button>(FindObjectsInactive.Exclude);
                    _system.SetSelectedGameObject(button.gameObject);
                }
            }

            _lastSelected = _system.currentSelectedGameObject;
        }
    }
}