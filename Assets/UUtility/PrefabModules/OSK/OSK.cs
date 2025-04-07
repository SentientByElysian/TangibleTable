using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using static Keyboard;

public class OSK : MonoBehaviour
{
    public static OSK activeOSK;

    [SerializeField] public TMP_InputField inputField;
    [SerializeField][Disable] public TMP_InputField selectedInputField;
    [SpaceArea]
    [SerializeField] private Keyboard activeKeyboard;
    [SerializeField] private Keyboard swapKeyboard;
    [SerializeField] private Keyboard numberKeyboard;
    [SpaceArea]
    [SerializeField] private UnityEvent<string> OnSubmit = new UnityEvent<string>();

    private TMP_InputField GetField()
    {
        TMP_InputField field = inputField;

        if (!field)
        {        
            GameObject selectedGO = EventSystem.current.currentSelectedGameObject;

            Debug.Log(selectedGO.name);
            if (selectedGO)
            {
                if (selectedInputField && selectedGO == selectedInputField.gameObject)
                    field = selectedInputField;
                else
                    field = selectedInputField = selectedGO.GetComponent<TMP_InputField>();
            }
            else
                field = selectedInputField = null;
        }

        return field;
    }

    public void OnKeyPress(KeyType keyType, string keyValue)
    {
        //Debug.Log(keyType + " || " + keyValue);

        TMP_InputField field = GetField();

        if (!field)
            return;

        string text = field.text;

        switch (keyType)
        {
            case KeyType.Char:
                field.text += keyValue;
                break;

            case KeyType.Backspace:
                if (field.text.Length > 0)
                    field.text = text.Remove(text.Length - 1, 1);
                break;

            case KeyType.Submit:
                OnSubmit?.Invoke(text);
                break;

            case KeyType.NumberBoard:
                activeKeyboard.Toggle();
                numberKeyboard.Toggle();
                break;

            case KeyType.SwitchBoard:
                activeKeyboard.Toggle();
                swapKeyboard.Toggle();

                Keyboard swapK = swapKeyboard;
                swapKeyboard = activeKeyboard;
                activeKeyboard = swapK;
                break;

            case KeyType.None:
                break;
        }
    }
}
