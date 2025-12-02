using Unity.Netcode;
using UnityEngine;

public class ScoreManager : NetworkBehaviour
{
    public int[] currentScores;
    public MultiplayerManager network;

    public override void OnNetworkSpawn()
    {
        //allocate array for max players
        currentScores = new int[network.maxPlayers]; 
        //clear array to 0 using Array.Clear
        System.Array.Clear(currentScores,0,currentScores.Length);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void AddScoreServerRPC(ulong id,int scoreToAdd)
    {
        
        ScoreAddedClientRPC(id,scoreToAdd); 
    }

    [ClientRpc]
    public void ScoreAddedClientRPC(ulong id, int score)
    {
        currentScores[id] += score;
        Debug.Log($"Score {id} Added now:{currentScores[id]}");
    }
}