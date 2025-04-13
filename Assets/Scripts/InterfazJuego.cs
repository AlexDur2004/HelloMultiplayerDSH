using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class InterfazJuego : NetworkBehaviour
{
    public AudioSource audioEfectos;
    public AudioClip sonidoHitJugador;
    public AudioClip sonidoHitEnemigo;
    private NetworkVariable<bool> debeCargarEscenaFin = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool cambioEscenaEnProceso = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        debeCargarEscenaFin.OnValueChanged += OnDebeCargarEscenaFinChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        debeCargarEscenaFin.OnValueChanged -= OnDebeCargarEscenaFinChanged;
    }

    private void OnDebeCargarEscenaFinChanged(bool oldValue, bool newValue)
    {
        if (newValue && !cambioEscenaEnProceso)
        {
            cambioEscenaEnProceso = true;
            StartCoroutine(CargarEscenaFinConDelay());
        }
    }

    public void ActivarCambioEscena()
    {
        if (IsServer && !debeCargarEscenaFin.Value)
        {
            debeCargarEscenaFin.Value = true;
        }
    }

    private IEnumerator CargarEscenaFinConDelay()
    {
        Debug.Log($"Preparando cambio a escena FIN en cliente {NetworkManager.Singleton.LocalClientId}");
        yield return new WaitForSeconds(1f);
        NetworkManager.Singleton.SceneManager.LoadScene("FIN", LoadSceneMode.Single);
    }

    public void ReproducirSonidoHit()
    {
        audioEfectos.PlayOneShot(sonidoHitJugador);
    }
}