using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MoveIcon : MonoBehaviour
{
    [SerializeField] private Image typeIcon;
    [SerializeField] private Color hovered = Color.white;
    [SerializeField] private Color notHovered = Color.white;
    [SerializeField] private float fadeSpeed = 5;
    
    private bool _hovering;
    
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
