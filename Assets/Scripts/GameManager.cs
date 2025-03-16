using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using TMPro; // Required for TextMeshPro UI

public class GameManager : NetworkBehaviour
{
    public static GameManager instance; // Singleton instance

    public int roundsToWin = 2; // Number of rounds needed to win the game
    public NetworkVariable<int> player1Wins = new NetworkVariable<int>(0); // Tracks Player 1's wins
    public NetworkVariable<int> player2Wins = new NetworkVariable<int>(0); // Tracks Player 2's wins
    private NetworkVariable<int> currentRound = new NetworkVariable<int>(1); // Tracks the current round
    private NetworkVariable<bool> gameEnded = new NetworkVariable<bool>(false); // Tracks if the game is over

    private TextMeshProUGUI roundText; // UI text for displaying the round number

    void Awake()
    {
        // Singleton pattern: Ensures only one instance of GameManager exists
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Server initializes the first round
        if (IsServer)
        {
            currentRound.Value = 1;
        }

        // Find the RoundText UI in the scene
        GameObject roundTextObj = GameObject.Find("RoundText");
        if (roundTextObj != null)
        {
            roundText = roundTextObj.GetComponent<TextMeshProUGUI>();
            UpdateRoundTextClientRpc(currentRound.Value); // Sync initial round number to all clients
        }
        else
        {
            Debug.LogError("RoundText UI not found in the scene!");
        }
    }

    /// Handles when a player dies, increments round count, and determines if the game is over.
    [ServerRpc(RequireOwnership = false)]
    public void PlayerDiedServerRpc(ulong loserId)
    {
        // If the game has already ended, do nothing
        if (gameEnded.Value) return;

        List<ulong> connectedClients = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);

        // Determine which player lost and update win counts
        if (loserId == connectedClients[0]) // If host lost
        {
            player2Wins.Value++;
            Debug.Log("Player 2 wins this round!");
        }
        else // If client lost
        {
            player1Wins.Value++;
            Debug.Log("Player 1 wins this round!");
        }

        Debug.Log($"Current Score - Player 1: {player1Wins.Value}, Player 2: {player2Wins.Value}");

        // Check if someone has won the match
        if (player1Wins.Value == roundsToWin)
        {
            Debug.Log("🏆 Player 1 Wins the Game!");
            EndGameClientRpc("Player 1 Wins!");
        }
        else if (player2Wins.Value == roundsToWin)
        {
            Debug.Log("🏆 Player 2 Wins the Game!");
            EndGameClientRpc("Player 2 Wins!");
        }
        else
        {
            // Increment the round number and start the next round
            currentRound.Value++;
            UpdateRoundTextClientRpc(currentRound.Value);
            StartCoroutine(RespawnPlayers());
        }
    }

    /// Respawns all players after a delay.
    private IEnumerator RespawnPlayers()
    {
        yield return new WaitForSeconds(3f); // Delay before respawning

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Value.PlayerObject != null)
            {
                // Reset player health
                PlayerHealth playerHealth = client.Value.PlayerObject.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.ResetHealthServerRpc();
                }

                Debug.Log($"Respawning Player {client.Key} at default position.");
            }
        }
    }

    /// Updates the round text UI across all clients.
    [ClientRpc]
    void UpdateRoundTextClientRpc(int roundNumber)
    {
        // Find the RoundText object if it wasn't found in Start()
        if (roundText == null)
        {
            GameObject roundTextObj = GameObject.Find("RoundText");
            if (roundTextObj != null)
            {
                roundText = roundTextObj.GetComponent<TextMeshProUGUI>();
            }
        }

        // Update the text on all clients
        if (roundText != null)
        {
            roundText.text = $"Round {roundNumber}";
            Debug.Log($"Updated Round Text to: Round {roundNumber}");
        }
    }

    /// Ends the game and updates the round text to display "Game Over."
    [ClientRpc]
    void EndGameClientRpc(string winner)
    {
        gameEnded.Value = true;
        Debug.Log(winner + " - Game Over!");

        // Find the RoundText object if necessary
        if (roundText == null)
        {
            GameObject roundTextObj = GameObject.Find("RoundText");
            if (roundTextObj != null)
            {
                roundText = roundTextObj.GetComponent<TextMeshProUGUI>();
            }
        }

        // Update the text to "Game Over"
        if (roundText != null)
        {
            roundText.text = "Game Over!";
        }
    }

    /// Resets the game state and restarts from round 1.
    [ServerRpc(RequireOwnership = false)]
    public void RestartGameServerRpc()
    {
        // Reset win counts and game state
        player1Wins.Value = 0;
        player2Wins.Value = 0;
        currentRound.Value = 1;
        gameEnded.Value = false;

        // Reset round text and respawn players
        UpdateRoundTextClientRpc(currentRound.Value);
        StartCoroutine(RespawnPlayers());
    }
}
