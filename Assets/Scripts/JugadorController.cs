using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Netcode;
// using UnityEditor.Callbacks;
// using static JugadorController;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DisparoBala))]

public class JugadorController : NetworkBehaviour
{
    CharacterController characterController;
    Rigidbody rb;
    Vector3 moveInput, moveVelocity, lastHitPoint;
    public float velocidad = 2.0f;
    public Camera mainCamera;
    DisparoBala disparoBala;
    public InterfazJuego interfaz;
    public AudioClip sonidoMuerte;
    public static event System.Action OnPlayerDeath;
    private static int totalJugadores = 0;
    public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();
    public NetworkVariable<Quaternion> Rotation = new NetworkVariable<Quaternion>();
    private float alturaFija = 0.0f;
    public float health = 100f;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
        disparoBala = GetComponent<DisparoBala>();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (interfaz == null)
        {
            interfaz = FindObjectOfType<InterfazJuego>();
        }

        totalJugadores++;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            InicializarPosicion();
        }
        else
        {
            // Para jugadores no propietarios, usar los valores de red
            transform.position = Position.Value;
            transform.rotation = Rotation.Value;
        }
    }

    private void InicializarPosicion()
    {
        if (IsServer)
        {
            SubmitPositionRequestServerRpc();
        }
        else
        {
            SubmitPositionRequestOwnerRpc();
        }
    }

    [Rpc(SendTo.Server)]
    void SubmitPositionRequestServerRpc(RpcParams rpcParams = default)
    {
        var randomPosition = GetRandomPositionOnPlane();
        transform.position = randomPosition;
        Position.Value = randomPosition;
        Rotation.Value = transform.rotation;
    }

    [Rpc(SendTo.Owner)]
    void SubmitPositionRequestOwnerRpc(RpcParams rpcParams = default)
    {
        var randomPosition = GetRandomPositionOnPlane();
        transform.position = randomPosition;
    }

    static Vector3 GetRandomPositionOnPlane()
    {
        return new Vector3(Random.Range(-3f, 3f), 1.0f, Random.Range(-3f, 3f));
    }

    void Update()
    {
        if (disparoBala == null) return;

        if (IsOwner)
        {
            // Movimiento
            moveInput = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));

            // Rotación basada en el mouse solo cuando está dentro de la pantalla
            if (Input.mousePosition.x >= 0 && Input.mousePosition.x <= Screen.width &&
                Input.mousePosition.y >= 0 && Input.mousePosition.y <= Screen.height)
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    // Solo rotar en el eje Y
                    Vector3 targetPoint = new Vector3(hit.point.x, transform.position.y, hit.point.z);
                    transform.LookAt(targetPoint);
                    // Mantener la rotación solo en el eje Y
                    transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
                    Rotation.Value = transform.rotation;
                }
            }

            // Disparo
            if (Input.GetMouseButtonDown(0))
            {
                disparoBala.Disparar();
            }

            // Mantener la posición Y fija
            Vector3 posicionActual = transform.position;
            posicionActual.y = 1.0f;
            transform.position = posicionActual;
            Position.Value = posicionActual;
        }
        else
        {
            // Para otros jugadores, usar los valores de red
            transform.position = Position.Value;
            transform.rotation = Rotation.Value;
        }
    }

    void ReiniciarJuego()
    {
        OnPlayerDeath?.Invoke();
        SceneTransition.Instance.LoadScene(SceneManager.GetActiveScene().name);
        if (interfaz != null && sonidoMuerte != null)
        {
            interfaz.audioEfectos.PlayOneShot(sonidoMuerte);
        }
    }

    void FixedUpdate()
    {
        if (IsOwner)
        {
            moveVelocity = moveInput * velocidad;
            characterController.Move(moveVelocity * Time.fixedDeltaTime);
        }
    }

    public void takeHit(float damage)
    {
        health -= damage;
        if (health <= 0)
        {
            OnDisable();
        }
        if (interfaz != null)
        {
            interfaz.ReproducirSonidoHit();
        }
    }

    void OnDisable()
    {
        totalJugadores--;
        if (totalJugadores == 0)
        {
            ReiniciarJuego();
        }
    }
}