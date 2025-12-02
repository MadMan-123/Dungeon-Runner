using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChatMessage : MonoBehaviour
{
    public TextMeshProUGUI messageText;


    public void SetMessage(string text)
    {
        messageText.text = text;

    }
}
