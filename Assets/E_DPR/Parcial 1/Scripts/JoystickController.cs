using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class JoystickController : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    [Header("Configuración")]
    [SerializeField] private string actionName; // "Move" o "Shoot"
    [SerializeField] private RectTransform handle;
    [SerializeField] private RawImage handleImage;
    [SerializeField] private float maxRange = 100f;
    [SerializeField] private float returnSpeed = 20f;

    [Header("Colores")]
    [SerializeField] private Color idleColor = Color.black;
    [SerializeField] private Color pressedColor = Color.yellow;

    // EVENTO ESTÁTICO: Nombre de acción y dirección
    public static Action<string, Vector2> OnJoystickAction;

    private Vector2 _inputVector;
    private Vector3 _startPos;
    private bool _isDragging = false;

    private void Awake()
    {
        
        _startPos = handle.localPosition;
        if (handleImage != null) handleImage.color = idleColor;
    }

    private void Update()
    {
        // 1. Retorno suave al centro
        if (!_isDragging && handle.localPosition != _startPos)
        {
            handle.localPosition = Vector3.Lerp(handle.localPosition, _startPos, Time.deltaTime * returnSpeed);
            if (Vector3.Distance(handle.localPosition, _startPos) < 0.1f)
            {
                handle.localPosition = _startPos;
                SendInput(Vector2.zero);
            }
        }

        // 2. Lógica para WebGL: Ocultar si es Mouse, mostrar si es Tactil
        HandleVisibility();
    }

    private void HandleVisibility()
    {
        // Si detectamos mouse (y no es móvil), ocultamos los sticks
        if (Input.mousePresent && !Application.isMobilePlatform)
        {
            gameObject.SetActive(false);
        }

        // Si hay un toque en la pantalla, los mostramos
        if (Input.touchCount > 0)
            gameObject.SetActive(true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isDragging = true;
        if (handleImage != null) handleImage.color = pressedColor;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position, eventData.pressEventCamera, out pos))
        {
            pos = Vector2.ClampMagnitude(pos, maxRange);
            handle.localPosition = pos;
            _inputVector = pos / maxRange;
            SendInput(_inputVector);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isDragging = false;
        if (handleImage != null) handleImage.color = idleColor;
        SendInput(Vector2.zero);
    }

    private void SendInput(Vector2 dir)
    {
        OnJoystickAction?.Invoke(actionName, dir);
    }
}