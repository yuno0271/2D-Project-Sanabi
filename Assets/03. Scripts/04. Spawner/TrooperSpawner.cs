using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrooperSpawner : Spawner
{
    [Header("Components")]
    [Space(2)]
    [SerializeField]
    private Trooper trooperPrefab;

    [SerializeField]
    private Animator anim;

    [Space(3)]
    [Header("Specs")]
    [Space(2)]
    [SerializeField]
    private int spawnCount;

    [Space(3)]
    [Header("Ballancing")]
    [Space(2)]
    private Trooper spawnedTrooper;

    public void OnEnable()
    {
        anim.SetBool("IsEnable", true);
        anim.SetTrigger("OnSpawn");
    }

    protected override void Spawn()
    {
        Debug.Log("Spawn!");
        spawnCount--;
        spawnedTrooper = Manager.Pool.GetPool(trooperPrefab, spawnPos.position, spawnPos.rotation) as Trooper;
        spawnedTrooper.OnDie += OnTrooperDied;

        if (spawnCount < 1)
            anim.SetBool("IsEnable", false);
    }

    public void OnTrooperDied()
    {
        spawnedTrooper.OnDie -= OnTrooperDied;

        if (spawnCount > 0)
            anim.SetTrigger("OnSpawn");
    }

    // Animation Bind
    public void OnAnimationSpawn()
    {
        Debug.Log("Spawn Bind!");
        Spawn();
    }
    public void OnAnimationDisable()
    {
        Destroy(gameObject);
    }

    private void OnDisable()
    {
    }
}
