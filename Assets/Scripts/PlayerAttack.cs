using UnityEngine;
using Unity.Netcode;

public class PlayerAttack : NetworkBehaviour
{
    public int attackDamage = 10;  // Damage dealt when attacking
    public float attackRange = 2f; // Attack radius
    public KeyCode attackKey = KeyCode.F;  // Attack key

    private AudioSource audioSource;  // Audio component for playing attack sounds
    public AudioClip[] attackSounds;  // Array of attack sound effects

    void Start()
    {
        if (!IsOwner) return; // Only the local player should initialize
        audioSource = GetComponent<AudioSource>(); // Get the AudioSource component
    }

    void Update()
    {
        if (!IsOwner) return; // Only the owner should process input

        if (Input.GetKeyDown(attackKey)) // If the attack key is pressed
        {
            AttackServerRpc(); // Call the attack function on the server
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void AttackServerRpc()
    {
        PlayAttackSoundClientRpc(); // Play attack sound on all clients

        // Detect objects in attack range
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);
        foreach (Collider collider in hitColliders)
        {
            if (collider.CompareTag("Player") && collider.gameObject != gameObject) // Ensure it's a player and not self
            {
                PlayerHealth enemyHealth = collider.GetComponent<PlayerHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(attackDamage); // Apply damage to the enemy
                }
            }
        }
    }

    [ClientRpc]
    void PlayAttackSoundClientRpc(ClientRpcParams rpcParams = default)
    {
        if (audioSource != null && attackSounds.Length > 0) // Ensure audio source and sounds exist
        {
            int randomIndex = Random.Range(0, attackSounds.Length); // Pick a random sound from the array
            audioSource.PlayOneShot(attackSounds[randomIndex]); // Play the selected attack sound
        }
    }
}
