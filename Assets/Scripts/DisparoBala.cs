using UnityEngine;
using Unity.Netcode;

public class DisparoBala : NetworkBehaviour
{
    [SerializeField] private Transform salidaBala;
    [SerializeField] private GameObject balaPrefab;
    [SerializeField] private float tiempoEntreDisparos = 0.5f;
    [SerializeField] private AudioClip sonidoDisparo;
    
    private float proximoDisparo;
    private InterfazJuego interfaz;
    private bool isInitialized;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) 
        {
            enabled = false;
            return;
        }
        
        interfaz = FindObjectOfType<InterfazJuego>();
        isInitialized = true;
    }

    private void Update()
    {
        if (!IsOwner || !isInitialized) return;

        if (Input.GetMouseButtonDown(0) && Time.time > proximoDisparo)
        {
            Disparar();
        }
    }

    public void Disparar()
    {
        if (!IsOwner || !isInitialized || Time.time <= proximoDisparo) return;

        if (balaPrefab == null)
        {
            Debug.LogError("Prefab de bala no asignado");
            return;
        }

        DispararLocalmente();
        RequestSpawnBalaServerRpc(NetworkManager.Singleton.LocalClientId, salidaBala.position, salidaBala.rotation);
        proximoDisparo = Time.time + tiempoEntreDisparos;
    }

    private void DispararLocalmente()
    {
        GameObject balaLocal = Instantiate(balaPrefab, salidaBala.position, salidaBala.rotation);
        Destroy(balaLocal, 2f);

        if (interfaz != null && sonidoDisparo != null)
        {
            interfaz.audioEfectos.PlayOneShot(sonidoDisparo);
        }
    }

    [ServerRpc]
    private void RequestSpawnBalaServerRpc(ulong clientId, Vector3 position, Quaternion rotation)
    {
        if (clientId != OwnerClientId) return;

        GameObject bala = Instantiate(balaPrefab, position, rotation);
        NetworkObject netObj = bala.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.SpawnWithOwnership(clientId);
        }
        else
        {
            Destroy(bala);
        }
    }
}