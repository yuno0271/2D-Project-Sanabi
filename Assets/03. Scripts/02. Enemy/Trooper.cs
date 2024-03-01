using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Trooper : EnemyShooter
{
    private CapsuleCollider2D capsuleCol;
    public CapsuleCollider2D CapsuleCol { get { return capsuleCol; } }

    protected override void Awake()
    {
        base.Awake();

        capsuleCol = GetComponent<CapsuleCollider2D>(); 

        TrooperDetect detect = new TrooperDetect(this);
        detect.OnEnableDetect += () => detect.detectRoutine = StartCoroutine(detect.DetectRoutine());
        detect.OnDisableDetect += () => StopCoroutine(detect.detectRoutine);

        fsm.AddState("Detect", detect);
        fsm.AddState("Grabbed", new TrooperGrabbed(this));
        fsm.AddState("Die", new TrooperDie(this));

        fsm.Init("Detect");
        initState = "Detect";
    }

    public override void Detecting(out Vector3 targetPos)
    {
        targetPos = playerTr.transform.position;

        lrAnim.Play("DetectAim");

        lr.positionCount = 2;
        lr.SetPosition(0, muzzlePos.position);
        lr.SetPosition(1, (targetPos - muzzlePos.position).normalized * 100f);

        // Agent Rotation
        if (transform.position.x > targetPos.x)
            transform.rotation = Quaternion.Euler(0, -180f, 0);
        else
            transform.rotation = Quaternion.Euler(0, 0, 0);

        // Aim Rotation
        Vector3 dir = (targetPos - aimPos.position).normalized;
        aimPos.right = dir;
    }
    public override void Shooting()
    {
        anim.Play("Attack");
        lr.positionCount = 0;
        EnemyBulletObject bullet = Manager.Pool.GetPool(bulletPrefab, muzzlePos.position, aimPos.rotation) as EnemyBulletObject;
        bullet.transform.up = AimPos.right;
        bullet.Rigid.AddForce(aimPos.right * bulletPower, ForceMode2D.Impulse);
    }
    public override void Died()
    {
        base.Died();
    }
    public override void Grabbed(out float holdingYpoint)
    {
        lr.positionCount = 0;
        holdingYpoint = grabbedYPos;
        fsm.ChangeState("Grabbed");
    }
}