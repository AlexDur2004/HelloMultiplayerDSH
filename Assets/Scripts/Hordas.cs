using UnityEngine;
using Unity.Netcode;

public class Hordas : NetworkBehaviour
{
    public ValoresEnemigos[] hordaEnemigos;
    private ValoresEnemigos hordaActual;
    private int numHordaActual = 0;
    private int enemigosPorCrear = 0;
    private int enemigosMuertos = 0;
    private float tiempoEspera;
    public InterfazJuego interfaz;
    public Collider suelo;
    private bool puedeSpawnear = false;
    private bool hordaIniciada = false;
    private NetworkVariable<int> hordaActualNetwork = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> enemigosMuertosNetwork = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Start()
    {
        MovimientoEnemigo.OnDeathAnother += MuereOtro;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            hordaActualNetwork.Value = 0;
            enemigosMuertosNetwork.Value = 0;
            tiempoEspera = Time.time;
            hordaIniciada = false;
            hordaActual = hordaEnemigos[0];
        }

        hordaActualNetwork.OnValueChanged += OnHordaChanged;
    }

    private void OnHordaChanged(int oldValue, int newValue)
    {
        hordaActual = hordaEnemigos[newValue];
    }

    void Update()
    {
        if (!IsServer) return;

        JugadorController[] jugadores = FindObjectsOfType<JugadorController>();
        puedeSpawnear = jugadores.Length >= 2;

        if (!puedeSpawnear) 
        {
            return;
        }

        if (!hordaIniciada)
        {
            hordaIniciada = true;
            SiguienteHorda();
        }

        float tiempoActual = Time.time;
        if (enemigosPorCrear > 0 && tiempoActual > tiempoEspera)
        {
            enemigosPorCrear--;
            tiempoEspera = tiempoActual + hordaActual.tiempoEnemigos;
            Vector3 spawnPos = ObtenerPuntoAleatorioEnSuelo();
            SpawnEnemigo(spawnPos);
        }
    }

    private void SpawnEnemigo(Vector3 position)
    {
        if (!IsServer) return;

        GameObject nuevoEnemigo = Instantiate(hordaActual.prefabEnemigo, position, Quaternion.identity);
        
        nuevoEnemigo.SetActive(true);
        
        NetworkObject netObj = nuevoEnemigo.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            ConfigurarEnemigoAntesDeSpawn(nuevoEnemigo);
            
            netObj.SpawnWithOwnership(NetworkManager.ServerClientId, true);
            
            Debug.Log($"Enemigo spawnado y activado en posición: {position}");
        }
        else
        {
            Destroy(nuevoEnemigo);
        }
    }

    private void ConfigurarEnemigoAntesDeSpawn(GameObject enemigo)
    {
        foreach (var comp in enemigo.GetComponents<Behaviour>())
        {
            comp.enabled = true;
        }
        
        enemigo.transform.position = enemigo.transform.position;
        
        var agent = enemigo.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = true;
            agent.Warp(enemigo.transform.position);
        }
    }

    void SiguienteHorda()
    {
        if (!IsServer) return;

        if (hordaActualNetwork.Value >= hordaEnemigos.Length)
        {
            Debug.Log("No hay más hordas disponibles");
            if (interfaz != null)
            {
                interfaz.ActivarCambioEscena();
            }
            return;
        }

        hordaActual = hordaEnemigos[hordaActualNetwork.Value];
        enemigosPorCrear = hordaActual.numEnemigos;
        enemigosMuertos = 0;
        enemigosMuertosNetwork.Value = 0;
        
        Debug.Log($"Iniciando horda {hordaActualNetwork.Value + 1} con {enemigosPorCrear} enemigos");
    }

    void MuereOtro()
    {
        if (!IsServer) return;

        enemigosMuertos++;
        enemigosMuertosNetwork.Value = enemigosMuertos;

        if (enemigosMuertos >= hordaActual.numEnemigos)
        {
            if (hordaActualNetwork.Value < hordaEnemigos.Length - 1)
            {
                hordaActualNetwork.Value++;
                SiguienteHorda();
            }
            else
            {
                Debug.Log("Todas las hordas completadas");
                if (interfaz != null)
                {
                    interfaz.ActivarCambioEscena();
                }
            }
        }
    }

    void OnDestroy()
    {
        MovimientoEnemigo.OnDeathAnother -= MuereOtro;
        if (hordaActualNetwork != null)
        {
            hordaActualNetwork.OnValueChanged -= OnHordaChanged;
        }
    }

    Vector3 ObtenerPuntoAleatorioEnSuelo()
    {
        Bounds bounds = suelo.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float z = Random.Range(bounds.min.z, bounds.max.z);
        float y = 0.025f;

        Vector3 spawnPos = new Vector3(x, y, z);

        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit hit, 1.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            return hit.position;
        }

        return spawnPos;
    }
}