using Unity.Netcode;
using UnityEngine;

namespace HelloWorld
{
    public class HelloWorldManager : MonoBehaviour
    {
        private NetworkManager m_NetworkManager;
        private bool isInitialized = false;

        void Awake()
        {
            m_NetworkManager = GetComponent<NetworkManager>();
            if (m_NetworkManager == null)
            {
                Debug.LogError("NetworkManager no encontrado en el objeto");
                return;
            }

            m_NetworkManager.OnClientConnectedCallback += OnClientConnected;
            m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            isInitialized = true;
        }

        void OnDestroy()
        {
            if (m_NetworkManager != null)
            {
                m_NetworkManager.OnClientConnectedCallback -= OnClientConnected;
                m_NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"Cliente conectado: {clientId}");
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"Cliente desconectado: {clientId}");
        }

        void OnGUI()
        {
            if (!isInitialized) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            if (!m_NetworkManager.IsClient && !m_NetworkManager.IsServer)
            {
                StartButtons();
            }
            else
            {
                StatusLabels();
            }
            GUILayout.EndArea();
        }

        void StartButtons()
        {
            if (GUILayout.Button("Host")) 
            {
                m_NetworkManager.StartHost();
            }
            if (GUILayout.Button("Client")) 
            {
                m_NetworkManager.StartClient();
            }
            if (GUILayout.Button("Server")) 
            {
                m_NetworkManager.StartServer();
            }
        }

        void StatusLabels()
        {
            var mode = m_NetworkManager.IsHost ?
                "Host" : m_NetworkManager.IsServer ? "Server" : "Client";
            GUILayout.Label("Mode: " + mode);
            
            var playerObject = m_NetworkManager.SpawnManager.GetLocalPlayerObject();
            if (playerObject != null)
            {
                var jugador = playerObject.GetComponent<JugadorController>();
                if (jugador != null)
                {
                    GUILayout.Label("Salud: " + jugador.health);
                }
            }
        }
    }
}
