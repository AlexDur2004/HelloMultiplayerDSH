using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using System;

public class MovimientoEnemigo : NetworkBehaviour
{
    [SerializeField] private float danio = 1f;
    [SerializeField] private float distanciaAtaque = 1.5f;
    [SerializeField] private float TimeBetweenAttack = 0.5f;
    [SerializeField] private float tiempoEntreMovimientos = 3f;
    [SerializeField] private float radioMovimientoAleatorio = 10f;

    private LivingEntity livingEntity;
    private UnityEngine.AI.NavMeshAgent pathfinder;
    private readonly List<Transform> targets = new List<Transform>();
    private readonly List<LivingEntity> targetEntities = new List<LivingEntity>();
    private readonly List<float> targetCollisionRadii = new List<float>();
    private float myCollisionRadius;
    private float tiempoEsperaMovimiento;
    private float NextAttackTime;
    private bool finPartida;
    private bool atacando;

    public delegate void OnDeath();
    public static event OnDeath OnDeathAnother;
    
    [NonSerialized] public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();
    [NonSerialized] public NetworkVariable<bool> IsDead = new NetworkVariable<bool>(false);

    private void Awake()
    {
        pathfinder = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (pathfinder == null)
        {
            Debug.LogError("NavMeshAgent no encontrado en " + gameObject.name);
            enabled = false;
            return;
        }

        livingEntity = GetComponent<LivingEntity>();
        if (livingEntity == null)
        {
            livingEntity = gameObject.AddComponent<LivingEntity>();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ActualizarObjetivos();
            myCollisionRadius = GetComponent<CapsuleCollider>().radius;
        }
        Position.OnValueChanged += OnPositionChanged;
    }

    public override void OnNetworkDespawn()
    {
        Position.OnValueChanged -= OnPositionChanged;
    }

    private void OnPositionChanged(Vector3 oldValue, Vector3 newValue)
    {
        if (!IsServer)
        {
            pathfinder.Warp(newValue);
        }
    }

    private void Start()
    {
        myCollisionRadius = GetComponent<CapsuleCollider>().radius;
    }

    private void OnEnable()
    {
        JugadorController.OnPlayerDeath += FinalPartida;
    }

    private void OnDisable()
    {
        JugadorController.OnPlayerDeath -= FinalPartida;
    }

    private void FinalPartida()
    {
        finPartida = true;
        pathfinder.enabled = false;
        StopAllCoroutines();
        atacando = false;
    }

    private void Update()
    {
        if (!IsServer) return;

        LimpiarObjetivos();
        if (targets.Count == 0)
        {
            ActualizarObjetivos();
        }

        var objetivo = EncontrarObjetivoMasCercano();
        
        if (objetivo.target == null || objetivo.targetEntity == null || objetivo.targetIndex == -1)
        {
            if (Time.time >= tiempoEsperaMovimiento)
            {
                MoverAleatoriamente();
                tiempoEsperaMovimiento = Time.time + tiempoEntreMovimientos;
            }
            return;
        }

        if (!finPartida && !atacando && !objetivo.targetEntity.muerto.Value)
        {
            if (objetivo.minDistance <= radioMovimientoAleatorio)
            {
                PerseguirObjetivo(objetivo.target, objetivo.targetIndex);
                VerificarAtaque(objetivo.target, objetivo.targetEntity, objetivo.targetIndex);
            }
            else if (Time.time >= tiempoEsperaMovimiento)
            {
                MoverAleatoriamente();
                tiempoEsperaMovimiento = Time.time + tiempoEntreMovimientos;
            }
        }
    }

    private void ActualizarObjetivos()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player == null) continue;

            Transform playerTransform = player.transform;
            if (!targets.Contains(playerTransform))
            {
                LivingEntity playerEntity = playerTransform.GetComponent<LivingEntity>();
                CapsuleCollider playerCollider = playerTransform.GetComponent<CapsuleCollider>();

                if (playerEntity != null && playerCollider != null && !playerEntity.muerto.Value)
                {
                    targets.Add(playerTransform);
                    targetEntities.Add(playerEntity);
                    targetCollisionRadii.Add(playerCollider.radius);
                }
            }
        }
    }

    private void LimpiarObjetivos()
    {
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            if (targets[i] == null || targetEntities[i] == null || targetEntities[i].muerto.Value)
            {
                targets.RemoveAt(i);
                targetEntities.RemoveAt(i);
                targetCollisionRadii.RemoveAt(i);
            }
        }
    }

    private (Transform target, LivingEntity targetEntity, int targetIndex, float minDistance) EncontrarObjetivoMasCercano()
    {
        Transform target = null;
        LivingEntity targetEntity = null;
        float minDistance = float.MaxValue;
        int targetIndex = -1;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null && targetEntities[i] != null && !targetEntities[i].muerto.Value)
            {
                float distance = Vector3.Distance(transform.position, targets[i].position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    target = targets[i];
                    targetEntity = targetEntities[i];
                    targetIndex = i;
                }
            }
        }

        return (target, targetEntity, targetIndex, minDistance);
    }

    private void PerseguirObjetivo(Transform target, int targetIndex)
    {
        if (!pathfinder.enabled)
        {
            pathfinder.enabled = true;
        }

        Vector3 dirToTarget = (target.position - transform.position).normalized;
        Vector3 targetPosition = target.position - dirToTarget * (myCollisionRadius + targetCollisionRadii[targetIndex] + distanciaAtaque);

        if (pathfinder.isOnNavMesh)
        {
            pathfinder.SetDestination(targetPosition);
            Position.Value = transform.position;
        }
        else
        {
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 1.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                pathfinder.Warp(hit.position);
            }
        }
    }

    private void VerificarAtaque(Transform target, LivingEntity targetEntity, int targetIndex)
    {
        if (Time.time > NextAttackTime)
        {
            NextAttackTime = Time.time + TimeBetweenAttack;
            float sqrDestToTarget = (target.position - transform.position).sqrMagnitude;
            if (sqrDestToTarget <= Mathf.Pow(myCollisionRadius + targetCollisionRadii[targetIndex] + distanciaAtaque, 2))
            {
                StartCoroutine(Attack(target, targetEntity, targetIndex));
            }
        }
    }

    private void MoverAleatoriamente()
    {
        if (!pathfinder.isOnNavMesh) return;

        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * radioMovimientoAleatorio;
        randomDirection += transform.position;
        
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(randomDirection, out hit, radioMovimientoAleatorio, UnityEngine.AI.NavMesh.AllAreas))
        {
            pathfinder.SetDestination(hit.position);
            Position.Value = transform.position;
        }
    }

    private IEnumerator Attack(Transform target, LivingEntity targetEntity, int targetIndex)
    {
        atacando = true;
        pathfinder.enabled = false;

        Vector3 originalPosition = transform.position;
        Vector3 dirToTarget = (target.position - transform.position).normalized;
        Vector3 attackPosition = target.position - dirToTarget * (myCollisionRadius + targetCollisionRadii[targetIndex]);

        float percent = 0;
        float attackSpeed = 3;
        bool hasAppliedDamage = false;

        while (percent <= 1)
        {
            if (percent >= .5f && !hasAppliedDamage)
            {
                hasAppliedDamage = true;
                targetEntity.takeHit(danio);
            }

            percent += Time.deltaTime * attackSpeed;
            float interpolation = (-Mathf.Pow(percent, 2) + percent) * 4;
            transform.position = Vector3.Lerp(originalPosition, attackPosition, interpolation);

            yield return null;
        }

        pathfinder.enabled = true;
        atacando = false;
    }

    public void takeHit(float damage)
    {
        if (!IsServer) return;

        livingEntity.takeHit(damage);
        if (livingEntity.muerto.Value)
        {
            Die();
        }
    }

    private void Die()
    {
        IsDead.Value = true;
        OnDeathAnother?.Invoke();
        NotifyDeathClientRpc();
        Destroy(gameObject);
    }

    [ClientRpc]
    private void NotifyDeathClientRpc()
    {
        if (!IsServer)
        {
            Destroy(gameObject);
        }
    }
}
