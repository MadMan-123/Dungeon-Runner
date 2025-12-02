using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ClassSelector : MonoBehaviour
{
    public GameObject selector;
    public TextMeshProUGUI nameText;
    public TMP_InputField nameInput;
    public enum ClassType
    {
        NoOne = -1,
        Wizard,
        Knight,
        Ranger,
        MaxClass
    }

    private bool isHosting = false;
    private ClassType currentType = ClassType.NoOne;
    private string currentName = "John";

    private void Start()
    {
        nameInput.onSubmit.AddListener(GetName); 
        UpdateNameText();
    }

    public void LoadClassSelector(bool host)
    {
        isHosting = host;
        
        selector.SetActive(true);
        
    }

    private void GetName(string input)
    {
        currentName = input;
        UpdateNameText();
    }
    
    public void SetType(int index)
    {
        if(index is > (int)ClassType.MaxClass or < 0)
            return;
        
        currentType = (ClassType)index;
        
        UpdateNameText();
    }

    private void UpdateNameText()
    {
        nameText.text = $"You are: {currentName} a {currentType.ToString()}";
    }

    public void ReadyUp()
    {
        var manager = (FPSManager)NetworkManager.Singleton;
        if (isHosting)
        {
            manager.StartGame();
        }
        else
        {
            manager.JoinGame();
        }
    }
}
