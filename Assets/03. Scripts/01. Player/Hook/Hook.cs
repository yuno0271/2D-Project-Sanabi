using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

public class Hook : MonoBehaviour
{
    [Header("Components")]
    [SerializeField]
    private Rigidbody2D rigid;
    public Rigidbody2D Rigid { get { return rigid; } }
    [SerializeField]
    private DistanceJoint2D distJoint;
    [SerializeField]
    private Animator anim;
    [SerializeField]
    private LineRenderer lr;

    // should be set by HookPooler
    [Space(3)]
    [Header("Pooler Setting")]
    [Space(2)]
    [SerializeField]
    public UnityAction<IGrabable> OnHookHitObject;
    [SerializeField]
    public UnityAction OnHookHitGround;
    [SerializeField]
    public UnityAction OnDestroyHook;

    [SerializeField]
    private Rigidbody2D ownerRigid;
    public Rigidbody2D OwnerRigid { set { ownerRigid = value; } }

    [SerializeField]
    private float trailSpeed;
    public float TrailSpeed { get { return trailSpeed; } set { trailSpeed = value; } }

    [SerializeField]
    private float maxDistance;
    public float MaxDistance { get { return maxDistance; } set { maxDistance = value; } }

    [Header("Specs")]
    [SerializeField]
    private float knockBackPower;
    public float KnockBackPower { get { return knockBackPower; }  }

    [Header("Ballancing")]
    public Vector3 muzzlePos;
    public RaycastHit2D hitInfo;

    [SerializeField]
    private bool isConnected = false;
    private Rigidbody2D connectedRigid;

    public float destroyTime;
    private Coroutine trailRoutine;

    private void OnEnable()
    {
        lr.positionCount = 0;
        anim.Play("HookStart");
        trailRoutine = StartCoroutine(TrailRoutine());
    }

    private void Update()
    {
        if (isConnected)
            LineRendering();
    }

    private void LineRendering()
    {
        lr.positionCount = 2;
        lr.SetPosition(0, transform.position);
        lr.SetPosition(1, ownerRigid.position);
    }
    private void Grab(IGrabable grabed)
    {
        OnHookHitObject?.Invoke(grabed);

        DisConnecting();
    }
    private void Connecting()
    {
        isConnected = true;
        OnHookHitGround?.Invoke();

        anim.Play("Grabbing");

        rigid.isKinematic = true;
        rigid.freezeRotation = true;

        distJoint.enabled = true;
        distJoint.connectedBody = ownerRigid;

        float distance = (ownerRigid.transform.position - transform.position).magnitude;
        distJoint.distance = distance > maxDistance ? maxDistance : distance;
    }
    public void DisConnecting()
    {
        OnDestroyHook?.Invoke();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Hook Knockback
        if (Manager.Layer.enemyLM.Contain(collision.gameObject.layer)
            || Manager.Layer.hookingPlatformLM.Contain(collision.gameObject.layer))
        {
            IKnockbackable knockbacked = collision.gameObject.GetComponent<IKnockbackable>();
            knockbacked?.KnockBack((collision.transform.position - muzzlePos).normalized * knockBackPower);
        }
    }

    IEnumerator TrailRoutine()
    {
        float time = Vector3.Distance(muzzlePos, hitInfo.point) / trailSpeed;
        float rate = 0f;

        while(rate < 1f)
        {
            rate += Time.deltaTime / time;
            transform.position = Vector3.Lerp(muzzlePos, hitInfo.point, rate);
            yield return null;
        }

        transform.position = hitInfo.point;

        if (Manager.Layer.wallLM.Contain(hitInfo.collider.gameObject.layer))
        {
            transform.parent = hitInfo.collider.gameObject.transform;
            Connecting();
        }
        else
            Grab(hitInfo.collider.gameObject.GetComponent<IGrabable>());
        yield return null;
    }

    private void OnDisable()
    {
        if (trailRoutine != null)
            StopCoroutine(trailRoutine);
    }
    private void OnDestroy()
    {
        Debug.Log("Hook Destroyed");
    }
}
