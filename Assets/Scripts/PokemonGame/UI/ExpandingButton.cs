using System;
using PokemonGame.General;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PokemonGame.UI
{
    public class ExpandingButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        [SerializeField] private bool expandWhenHover = true;
        [SerializeField] private bool hasClickOverride = false;
        [SerializeField] private float scale = 1.1f;
        [SerializeField] private float speed = 4f;
        [SerializeField] private Button targetButton;
        [SerializeField] private bool hasTargetOverride;
        [ConditionalHide("hasTargetOverride", true)] public Transform targetGraphic;
        [SerializeField] private UnityEvent onHover;

        private bool _hovering = false;
        private bool _focused = false;
        private Vector3 _baseScale;

        protected void Awake()
        {
            if (!targetButton)
            {
                targetButton = GetComponent<Button>();
            }

            if (!targetGraphic)
            {
                targetGraphic = targetButton.targetGraphic.transform;
            }
            _baseScale = targetGraphic.localScale;
        }

        private void OnEnable()
        {
            targetButton.onClick.AddListener(ButtonClicked);
        }

        private void OnDisable()
        {
            targetButton.onClick.RemoveListener(ButtonClicked);
        }

        private void OnValidate()
        {
            if (targetButton == null)
            {
                targetButton = GetComponent<Button>();
            }
        }

        private void Update()
        {
            if (!EventSystem.current)
            {
                return;
            }

            if (!_focused && EventSystem.current.currentSelectedGameObject == gameObject)
            {
                onHover?.Invoke();
            }
            
            if (EventSystem.current.currentSelectedGameObject == gameObject)
            {
                _focused = true;
            }
            else
            {
                _focused = false;
            }
            
            if (_hovering || _focused)
            {
                targetGraphic.localScale = Vector3.Lerp(targetGraphic.localScale, _baseScale * scale, speed * Time.deltaTime);
            }
            else
            {
                targetGraphic.localScale = Vector3.Lerp(targetGraphic.localScale, _baseScale, speed * Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            _hovering = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (expandWhenHover)
            {
                _hovering = true;
            }
            
            onHover?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovering = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            ButtonClicked();
        }

        private void ButtonClicked()
        {
            if (!hasClickOverride)
            {
                InitiateClick();
            }
        }

        public void InitiateClick()
        {
            if (expandWhenHover && targetButton.interactable)
            {
                targetGraphic.localScale = _baseScale;
            }
        }
    }
}