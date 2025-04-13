using UnityEngine;
using System;
using Unity.Netcode;

public class LivingEntity : NetworkBehaviour
{
    [SerializeField] public float vidaInicial;
    
    public NetworkVariable<bool> muerto = new NetworkVariable<bool>(false);
    protected readonly NetworkVariable<float> vidaActual = new NetworkVariable<float>();

    protected virtual void Start()
    {
        if (IsServer)
        {
            vidaActual.Value = vidaInicial;
            muerto.Value = false;
        }
    }

    public virtual void takeHit(float damage)
    {
        if (!IsServer) return;

        vidaActual.Value -= damage;
        if (vidaActual.Value <= 0f && !muerto.Value)
        {
            muerto.Value = true;
            gameObject.SetActive(false);
        }
    }
}
