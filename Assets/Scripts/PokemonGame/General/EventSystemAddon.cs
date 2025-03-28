using UnityEngine;
using UnityEngine.EventSystems;

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
            if (_system.currentSelectedGameObject == null)
            {
                _system.SetSelectedGameObject(_lastSelected);
            }

            _lastSelected = _system.currentSelectedGameObject;
        }
    }
}