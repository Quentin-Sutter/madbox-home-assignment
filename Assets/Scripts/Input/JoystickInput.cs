using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Madbox.InputSystem
{
    /// <summary>
    /// Interprets pointer input into movement intent and drives the joystick view.
    /// Keeps input logic separate from both UI rendering and hero movement.
    /// </summary>
    public sealed class JoystickInput : MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler,
        IPointerExitHandler,
        IMoveIntentSource
    {
        [Header("References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private JoystickView joystickView;
        [SerializeField] private Camera worldCamera;

        [Header("Tuning")]
        [SerializeField, Min(1f)] private float radius = 100f;
        [SerializeField, Range(0f, 1f)] private float deadzone = 0.1f;

        private int _activePointerId = -1;
        private bool _isPointerActive;
        private Vector2 _baseLocalPosition;
        private MoveIntent _currentIntent = MoveIntent.Idle;

        public MoveIntent CurrentIntent => _currentIntent;
        public event Action<MoveIntent> IntentChanged;

        private void Awake()
        {
            if (canvasRect == null && canvas != null)
            {
                canvasRect = canvas.GetComponent<RectTransform>();
            }

            ResetIntent();
            joystickView?.Hide();
        }

        private void OnDisable()
        {
            ReleasePointer();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isPointerActive)
            {
                return;
            }

            if (!TryGetCanvasLocalPoint(eventData, out var localPoint))
            {
                return;
            }

            _isPointerActive = true;
            _activePointerId = eventData.pointerId;
            _baseLocalPosition = localPoint;

            joystickView?.Show(_baseLocalPosition);
            SetIntent(Vector2.zero, 0f);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsMatchingPointer(eventData))
            {
                return;
            }

            if (!TryGetCanvasLocalPoint(eventData, out var localPoint))
            {
                return;
            }

            var delta = localPoint - _baseLocalPosition;
            var clampedDelta = Vector2.ClampMagnitude(delta, radius);
            var normalized = clampedDelta / radius;
            var rawMagnitude = normalized.magnitude;

            var strength = ApplyDeadzone(rawMagnitude);
            var direction2D = rawMagnitude > Mathf.Epsilon ? normalized / rawMagnitude : Vector2.zero;

            joystickView?.UpdateKnob(_baseLocalPosition, clampedDelta);
            SetIntent(direction2D, strength);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsMatchingPointer(eventData))
            {
                return;
            }

            ReleasePointer();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!IsMatchingPointer(eventData))
            {
                return;
            }

            // Safeguard for unexpected pointer lifecycle interruptions.
            ReleasePointer();
        }

        private bool IsMatchingPointer(PointerEventData eventData)
        {
            return _isPointerActive && eventData.pointerId == _activePointerId;
        }

        private void ReleasePointer()
        {
            _isPointerActive = false;
            _activePointerId = -1;
            joystickView?.Hide();
            ResetIntent();
        }

        private void ResetIntent()
        {
            _currentIntent = MoveIntent.Idle;
            IntentChanged?.Invoke(_currentIntent);
        }

        private void SetIntent(Vector2 direction2D, float strength)
        {
            var worldDirection = ToWorldDirection(direction2D);
            var isMoving = strength > 0f && worldDirection.sqrMagnitude > 0f;
            _currentIntent = new MoveIntent(worldDirection, strength, isMoving);
            IntentChanged?.Invoke(_currentIntent);
        }

        private Vector3 ToWorldDirection(Vector2 direction2D)
        {
            if (direction2D.sqrMagnitude <= Mathf.Epsilon)
            {
                return Vector3.zero;
            }

            var referenceCamera = worldCamera != null ? worldCamera : Camera.main;

            var forward = referenceCamera != null ? referenceCamera.transform.forward : Vector3.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                forward = Vector3.forward;
            }
            else
            {
                forward.Normalize();
            }

            var right = Vector3.Cross(Vector3.up, forward);
            if (right.sqrMagnitude <= Mathf.Epsilon)
            {
                right = Vector3.right;
            }
            else
            {
                right.Normalize();
            }

            var worldDirection = right * direction2D.x + forward * direction2D.y;
            return worldDirection.sqrMagnitude > Mathf.Epsilon ? worldDirection.normalized : Vector3.zero;
        }

        private bool TryGetCanvasLocalPoint(PointerEventData eventData, out Vector2 localPoint)
        {
            if (canvasRect == null)
            {
                localPoint = default;
                return false;
            }

            var eventCamera = GetEventCamera(eventData);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventCamera,
                out localPoint);
        }

        private Camera GetEventCamera(PointerEventData eventData)
        {
            if (canvas == null)
            {
                return eventData.pressEventCamera;
            }

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera != null ? canvas.worldCamera : eventData.pressEventCamera;
        }

        private float ApplyDeadzone(float value)
        {
            if (value <= deadzone)
            {
                return 0f;
            }

            return Mathf.Clamp01((value - deadzone) / (1f - deadzone));
        }
    }
}
