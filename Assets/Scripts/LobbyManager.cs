using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : NetworkBehaviour 
{
    
    public static LobbyManager instance; 
   
    public string currentName = "John";
    public TextMeshProUGUI nameText;
    public TMP_InputField nameInput;
    public NetworkVariable<int> readyCount;

    private bool isHosting = false;
    
    private void Start()
    {
        nameInput.onSubmit.AddListener(GetName); 
        UpdateNameText();
    }
    
     private void Awake()
     {
         if (!instance)
         {
             instance = this;
         }
         else
         {
             Destroy(instance);
         }
     }
    public void UpdateNameText()
    {
        nameText.text = $"You are: {currentName} a {ClassSelector.instance.currentType.ToString()}";
        
        
    }
    
    private void GetName(string input)
    {
        currentName = input;
        UpdateNameText();
    }
    
    public void ReadyUp()
    {
        var manager = (FPSManager)(NetworkManager.Singleton);
        
        manager.LoadWorld();
    }
}
