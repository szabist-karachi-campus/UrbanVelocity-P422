using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class UIInputFilter : MonoBehaviour
{
    private EventSystem eventSystem;
    private GameObject lastSelectedObject;

    void Start()
    {
        eventSystem = GetComponent<EventSystem>();
        if (eventSystem != null)
        {
            lastSelectedObject = eventSystem.firstSelectedGameObject;
        }
    }

    void Update()
    {
        if (Gamepad.current == null || eventSystem == null) return;

        // Check if ANY D-Pad direction is currently being pressed
        bool dPadPressed = Gamepad.current.dpad.up.isPressed ||
                          Gamepad.current.dpad.down.isPressed ||
                          Gamepad.current.dpad.left.isPressed ||
                          Gamepad.current.dpad.right.isPressed;

        if (dPadPressed)
        {
            // FORCE the EventSystem to lock onto the last valid button selected by the stick
            if (eventSystem.currentSelectedGameObject != lastSelectedObject)
            {
                eventSystem.SetSelectedGameObject(lastSelectedObject);
            }
        }
        else
        {
            // If the D-pad isn't pressed, allow normal selection updates (Left Stick/Mouse)
            if (eventSystem.currentSelectedGameObject != null)
            {
                lastSelectedObject = eventSystem.currentSelectedGameObject;
            }
        }
    }
}