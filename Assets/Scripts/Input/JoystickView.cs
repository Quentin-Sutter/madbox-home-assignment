using UnityEngine;

namespace Madbox.InputSystem
{
    /// <summary>
    /// UI-only component responsible for showing/hiding and positioning joystick visuals.
    /// Contains no gameplay logic.
    /// </summary>
    public sealed class JoystickView : MonoBehaviour
    {
        [SerializeField] private RectTransform baseRect;
        [SerializeField] private RectTransform knobRect;

        public void Show(Vector2 baseLocalPosition)
        {
            if (baseRect == null || knobRect == null)
            {
                return;
            }

            if (!baseRect.gameObject.activeSelf)
            {
                baseRect.gameObject.SetActive(true);
            }

            if (!knobRect.gameObject.activeSelf)
            {
                knobRect.gameObject.SetActive(true);
            }

            baseRect.anchoredPosition = baseLocalPosition;
            knobRect.anchoredPosition = baseLocalPosition;
        }

        public void UpdateKnob(Vector2 baseLocalPosition, Vector2 knobOffset)
        {
            if (knobRect == null)
            {
                return;
            }

            knobRect.anchoredPosition = baseLocalPosition + knobOffset;
        }

        public void Hide()
        {
            if (baseRect != null && baseRect.gameObject.activeSelf)
            {
                baseRect.gameObject.SetActive(false);
            }

            if (knobRect != null && knobRect.gameObject.activeSelf)
            {
                knobRect.gameObject.SetActive(false);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (baseRect == null || knobRect == null)
            {
                return;
            }

            if (baseRect.parent != knobRect.parent)
            {
                Debug.LogWarning("Joystick base and knob should share the same parent RectTransform for consistent positioning.", this);
            }
        }
#endif
    }
}
