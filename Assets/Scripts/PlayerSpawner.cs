using UnityEngine;
using Unity.Netcode;

public class PlayerSpawner : NetworkBehaviour
{
    public Transform hostSpawnPoint; // Assign in Inspector
    public Transform clientSpawnPoint; // Assign in Inspector

    public override void OnNetworkSpawn()
    {
        if (IsServer) // Only the Server/Host spawns players
        {
            ulong clientId = OwnerClientId; // Get the joining client's ID
            Vector3 spawnPosition = IsHost ? hostSpawnPoint.position : clientSpawnPoint.position;
            SpawnPlayer(spawnPosition, Quaternion.identity, clientId);
        }
    }

    private void SpawnPlayer(Vector3 position, Quaternion rotation, ulong ownerId)
    {
        GameObject playerPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;

        if (playerPrefab != null)
        {
            GameObject player = Instantiate(playerPrefab, position, rotation);
            NetworkObject networkObject = player.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Debug.LogError("Player prefab is missing a NetworkObject component!");
                return;
            }

            // Assign ownership to the correct client
            networkObject.SpawnAsPlayerObject(ownerId);
            Debug.Log($"Player Spawned at {position} with Owner: {networkObject.OwnerClientId}");
        }
    }
}
