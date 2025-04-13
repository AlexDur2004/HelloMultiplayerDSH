using UnityEngine;
using Unity.Netcode;

public class MovimientoBala : NetworkBehaviour
{
    [SerializeField] private float velocidad = 5.0f;
    [SerializeField] private float danio = 1f;
    [SerializeField] private float tiempoVida = 3f;
    
    private float tiempoInicio;
    private readonly NetworkVariable<Vector3> position = new NetworkVariable<Vector3>();
    private readonly NetworkVariable<Quaternion> rotation = new NetworkVariable<Quaternion>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            position.Value = transform.position;
            rotation.Value = transform.rotation;
            tiempoInicio = Time.time;
        }
        else
        {
            transform.position = position.Value;
            transform.rotation = rotation.Value;
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            if (Time.time - tiempoInicio > tiempoVida)
            {
                DesactivarBala();
                return;
            }

            float moveDistance = velocidad * Time.deltaTime;
            transform.Translate(Vector3.forward * moveDistance);
            position.Value = transform.position;
            rotation.Value = transform.rotation;
            CheckCollisions(moveDistance);
        }
        else
        {
            transform.position = position.Value;
            transform.rotation = rotation.Value;
        }
    }

    private void CheckCollisions(float moveDistance)
    {
        if (!IsServer) return;

        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, moveDistance))
        {
            OnHitObject(hit);
        }
    }

    public void OnHitObject(RaycastHit hit)
    {
        if (!IsServer) return;

        DesactivarBala();
        
        MovimientoEnemigo enemigo = hit.collider.GetComponent<MovimientoEnemigo>();
        if (enemigo != null)
        {
            enemigo.takeHit(danio);
            return;
        }

        if (hit.collider.transform.parent != null)
        {
            enemigo = hit.collider.transform.parent.GetComponent<MovimientoEnemigo>();
            if (enemigo != null)
            {
                enemigo.takeHit(danio);
            }
        }
    }

    private void OnBecomeInvisible()
    {
        if (IsServer)
        {
            DesactivarBala();
        }
    }

    private void DesactivarBala()
    {
        if (!IsServer) return;

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn();
        }
        Destroy(gameObject);
    }
}
