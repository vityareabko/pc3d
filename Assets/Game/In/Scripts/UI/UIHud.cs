using System;
using UnityEngine;

public enum CursorType
{
    None,
    Default,
    Interact,
}

public class UIHud : MonoBehaviour
{
    [SerializeField] private Transform cursorDefault;
    [SerializeField] private Transform cursorInteract;
    
    
    public void SetCursor(CursorType type, string txt = "")
    {
        cursorDefault.gameObject.SetActive(false);
        cursorInteract.gameObject.SetActive(false);

        
        switch (type)
        {
            case CursorType.None:
                break;
            case CursorType.Default:
                cursorDefault.gameObject.SetActive(true);
                break;
            case CursorType.Interact:
                cursorDefault.gameObject.SetActive(true);
                cursorInteract.gameObject.SetActive(true);
                break;
                defeult:
                throw new ArgumentException();
        }
        
    }
}