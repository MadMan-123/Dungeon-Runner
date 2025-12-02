using System;
using Unity.Netcode;

public class PlayerMetaData : NetworkBehaviour
{
        
        public int playerId;
        private MultiplayerManager _manager;

        public override void OnNetworkSpawn()
        {
                base.OnNetworkSpawn();
                _manager = (MultiplayerManager)NetworkManager.Singleton;
                playerId = _manager.currentPlayers++;
                
        }
}