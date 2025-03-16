using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    public float speed = 5f; // Player movement speed
    public float jumpForce = 8f; // Jump force applied when jumping
    public float gravity = -9.81f; // Gravity applied to the player
    private CharacterController controller; // Reference to the CharacterController component
    private Vector3 velocity; // Stores player's movement velocity
    private bool isGrounded; // Checks if the player is on the ground

    public AudioClip jumpSound; // Sound effect for jumping
    public AudioClip walkSound; // Sound effect for walking
    private AudioSource audioSource; // Reference to the AudioSource component
    private bool isWalking; // Tracks if the player is walking

    void Start()
    {
        controller = GetComponent<CharacterController>(); // Get the CharacterController component
        audioSource = GetComponent<AudioSource>(); // Get the AudioSource component
        audioSource.loop = true; // Enable looping for footsteps

        if (IsOwner) // Ensure only the owner sets their rotation
        {
            RequestRotationServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    [ServerRpc]
    void RequestRotationServerRpc(ulong clientId)
    {
        Quaternion rotation = clientId == 0 ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180, 0); // Set rotation based on client ID
        ApplyRotationClientRpc(clientId, rotation);
    }

    [ClientRpc]
    void ApplyRotationClientRpc(ulong clientId, Quaternion rotation)
    {
        if (OwnerClientId == clientId)
        {
            transform.rotation = rotation; // Apply the rotation to the player
            Debug.Log($"Player {clientId} spawned facing {rotation.eulerAngles.y}°");
        }
    }

    void Update()
    {
        if (!IsOwner) return; // Only allow movement for the owner

        isGrounded = controller.isGrounded; // Check if the player is grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Reset gravity when grounded
        }

        float moveX = Input.GetAxis("Horizontal"); // Get horizontal movement input
        float moveZ = Input.GetAxis("Vertical"); // Get vertical movement input
        Vector3 moveDirection = new Vector3(moveX, 0, moveZ).normalized; // Calculate movement direction

        if (moveDirection != Vector3.zero) // If player is moving
        {
            if (!isWalking) // Start walking sound if not already playing
            {
                isWalking = true;
                PlayWalkingSoundServerRpc();
            }
            MoveServerRpc(moveDirection); // Send movement to the server
        }
        else
        {
            if (isWalking) // Stop walking sound when player stops moving
            {
                isWalking = false;
                StopWalkingSoundServerRpc();
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded) // If space is pressed and player is grounded
        {
            JumpServerRpc(); // Send jump request to the server
        }

        velocity.y += gravity * Time.deltaTime; // Apply gravity over time
        controller.Move(velocity * Time.deltaTime); // Move the player
    }

    [ServerRpc(RequireOwnership = false)]
    void MoveServerRpc(Vector3 moveDirection, ServerRpcParams rpcParams = default)
    {
        if (controller != null)
        {
            controller.Move(moveDirection * speed * Time.deltaTime); // Move player on the server
        }
        SyncPositionClientRpc(transform.position, velocity); // Sync position across clients
    }

    [ServerRpc(RequireOwnership = false)]
    void JumpServerRpc(ServerRpcParams rpcParams = default)
    {
        if (controller.isGrounded)
        {
            velocity.y = jumpForce; // Apply jump force
            PlayJumpSoundClientRpc(); // Play jump sound for all clients
            SyncPositionClientRpc(transform.position, velocity); // Sync position across clients
        }
    }

    [ClientRpc]
    void PlayJumpSoundClientRpc(ClientRpcParams rpcParams = default)
    {
        if (audioSource != null && jumpSound != null)
        {
            audioSource.PlayOneShot(jumpSound); // Play jump sound
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void PlayWalkingSoundServerRpc()
    {
        PlayWalkingSoundClientRpc(); // Play walking sound for all clients
    }

    [ServerRpc(RequireOwnership = false)]
    void StopWalkingSoundServerRpc()
    {
        StopWalkingSoundClientRpc(); // Stop walking sound for all clients
    }

    [ClientRpc]
    void PlayWalkingSoundClientRpc(ClientRpcParams rpcParams = default)
    {
        if (audioSource != null && walkSound != null && !audioSource.isPlaying)
        {
            audioSource.clip = walkSound; // Set walking sound clip
            audioSource.Play(); // Play walking sound
        }
    }

    [ClientRpc]
    void StopWalkingSoundClientRpc(ClientRpcParams rpcParams = default)
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop(); // Stop walking sound
        }
    }

    [ClientRpc]
    void SyncPositionClientRpc(Vector3 newPosition, Vector3 newVelocity, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner)
        {
            controller.enabled = false; // Temporarily disable CharacterController
            transform.position = newPosition; // Sync position
            velocity = newVelocity; // Sync velocity
            controller.enabled = true; // Re-enable CharacterController
        }
    }
}
