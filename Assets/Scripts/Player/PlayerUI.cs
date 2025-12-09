using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerUI : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI PlayerName;
    [SerializeField] private TextMeshProUGUI healthText;
    //set name interface
    public void SetPlayerName(string name)
    {
        UpdateDamageServerRPC(name);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void UpdateDamageServerRPC(string name)
    {
        UpdateDamageClientRPC(name);    
    }

    [ClientRpc]
    private void UpdateDamageClientRPC(string name)
    {
        PlayerName.text = name;
    }
    
    
    [ServerRpc(RequireOwnership = false)]
    public void UpdateDamageServerRPC(int oldVal, int newVal)
    {
        UpdateDamageClientRPC(oldVal, newVal);    
    }

    [ClientRpc]
    void UpdateDamageClientRPC(int oldVal, int newVal)
    {
        healthText.text = $"{newVal}%";
    } 
}
