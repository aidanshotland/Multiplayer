using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerHealth : NetworkBehaviour
{
    public int maxHealth = 100; // Maximum health for the player
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public Slider healthBar; // Floating health bar above the player
    private static Dictionary<ulong, Slider> hudHealthBars = new Dictionary<ulong, Slider>(); // Stores HUD health bars for all players

    public AudioClip hitSound; // Sound effect when taking damage
    private AudioSource audioSource; // Audio source component

    void Start()
    {
        audioSource = GetComponent<AudioSource>(); // Get the AudioSource component

        if (IsServer)
        {
            currentHealth.Value = maxHealth; // Initialize health on the server
        }

        // Listen for health changes and update UI when health changes
        currentHealth.OnValueChanged += OnHealthChanged;

        // Register HUD health bars for both Host and Client
        if (IsOwner)
        {
            RegisterHUDHealthBar();
        }

        // Ensure UI is updated when game starts
        UpdateHealthUI();
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        UpdateHealthUI(); // Ensure UI updates whenever health changes
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer) return; // Only the server modifies health

        currentHealth.Value -= damage; // Decrease health by damage amount
        currentHealth.Value = Mathf.Clamp(currentHealth.Value, 0, maxHealth); // Prevent negative health

        PlayHitSoundClientRpc(); // Play hit sound on all clients
        UpdateHealthClientRpc(currentHealth.Value); // Sync health update to all clients

        if (currentHealth.Value <= 0)
        {
            Die(); // Handle player death
        }
    }

    [ClientRpc]
    void PlayHitSoundClientRpc(ClientRpcParams rpcParams = default)
    {
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound); // Play hit sound effect
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " has died!");
        gameObject.SetActive(false); // Disable the player object

        if (IsServer)
        {
            GameManager.instance.PlayerDiedServerRpc(OwnerClientId); // Notify GameManager that a player died
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetHealthServerRpc()
    {
        if (!IsServer) return;

        currentHealth.Value = maxHealth; // Reset health to max
        gameObject.SetActive(true); // Reactivate player
        UpdateHealthClientRpc(maxHealth); // Sync health update
        Debug.Log(gameObject.name + " health reset to " + maxHealth);
    }

    [ClientRpc]
    void UpdateHealthClientRpc(int newHealth, ClientRpcParams rpcParams = default)
    {
        currentHealth.Value = newHealth; // Sync health variable
        UpdateHealthUI(); // Update health bar UI
    }

    void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.value = (float)currentHealth.Value / maxHealth; // Update floating health bar
        }

        // Sync HUD Health Bars for all players
        if (hudHealthBars.ContainsKey(OwnerClientId))
        {
            hudHealthBars[OwnerClientId].value = (float)currentHealth.Value / maxHealth;
        }
    }

    void RegisterHUDHealthBar()
    {
        GameObject hostBarObj = GameObject.Find("HostHealthBar"); // Find Host Health Bar in scene
        GameObject clientBarObj = GameObject.Find("ClientHealthBar"); // Find Client Health Bar in scene

        if (NetworkManager.Singleton.LocalClientId == 0 && hostBarObj != null) // If Host, register HostHealthBar
        {
            hudHealthBars[OwnerClientId] = hostBarObj.GetComponent<Slider>();
        }
        else if (NetworkManager.Singleton.LocalClientId != 0 && clientBarObj != null) // If Client, register ClientHealthBar
        {
            hudHealthBars[OwnerClientId] = clientBarObj.GetComponent<Slider>();
        }
    }
}
