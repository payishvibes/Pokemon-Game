using PokemonGame.ScriptableObjects;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PokemonGame.UI
{
    public class StatsScreenMoveIcon : MonoBehaviour
    {
        [SerializeField] private Image typeIcon;
        [SerializeField] private Color hovered = Color.white;
        [SerializeField] private Color notHovered = Color.white;
        [SerializeField] private float fadeSpeed = 1;
        [SerializeField] StatsScreenMoveInfo moveInfo;
        [SerializeField] private Transform toolTipAnchor;
        
        [HideInInspector] public Move move;

        private bool _hovering;

        public void Clicked()
        {
            moveInfo.UpdateInfo(move, toolTipAnchor);
        }

        private void Update()
        {
            _hovering = EventSystem.current.currentSelectedGameObject == gameObject;
            
            if (_hovering)
            {
                typeIcon.color = Color.Lerp(typeIcon.color, hovered, fadeSpeed * Time.deltaTime);
            }
            else
            {
                typeIcon.color = Color.Lerp(typeIcon.color, notHovered, fadeSpeed * Time.deltaTime);
            }
        }
    }
}