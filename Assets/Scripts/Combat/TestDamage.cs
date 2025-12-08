using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TestDamage : MonoBehaviour
{
    
    [SerializeField] private TextMeshProUGUI healthText;
    


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
