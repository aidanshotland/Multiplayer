using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// Handles command-line arguments for starting the game in different network modes (server, host, client).
/// </summary>
public class NetworkCommandLine : MonoBehaviour
{
    private NetworkManager netManager;

    void Start()
    {
        // Get the NetworkManager component from the parent object
        netManager = GetComponentInParent<NetworkManager>();

        // If running in the Unity Editor, ignore command-line parsing
        if (Application.isEditor) return;

        // Parse command-line arguments
        var args = GetCommandlineArgs();

        // Check if the "-mode" argument was provided
        if (args.TryGetValue("-mode", out string mode))
        {
            string ipAddress = "127.0.0.1"; // Default to localhost
            ushort port = 7777; // Default port

            // Get the Unity Transport component and set the connection details
            var transport = netManager.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(ipAddress, port);
            }

            // Determine the network mode and start accordingly
            switch (mode)
            {
                case "server":
                    netManager.StartServer();
                    Debug.Log("Started as Server");
                    break;
                case "host":
                    netManager.StartHost();
                    Debug.Log("Started as Host");
                    break;
                case "client":
                    netManager.StartClient();
                    Debug.Log("Started as Client");
                    break;
                default:
                    Debug.LogError($"Unknown mode: {mode}");
                    break;
            }
        }
    }

    /// Parses command-line arguments into a dictionary for easy lookup.
    /// Dictionary containing command-line arguments.
    private Dictionary<string, string> GetCommandlineArgs()
    {
        Dictionary<string, string> argDictionary = new Dictionary<string, string>();
        var args = System.Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; ++i)
        {
            var arg = args[i].ToLower();
            if (arg.StartsWith("-")) // If the argument starts with "-", it's a flag
            {
                var value = i < args.Length - 1 ? args[i + 1].ToLower() : null; // Get the next argument as value
                value = (value?.StartsWith("-") ?? false) ? null : value; // Ensure it's not another flag
                argDictionary.Add(arg, value);
            }
        }

        return argDictionary;
    }
}
