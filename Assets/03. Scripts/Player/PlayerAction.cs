using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static UnityEditor.PlayerSettings.SplashScreen;

public class PlayerAction : MonoBehaviour
{
    [Header("Components")]
    [Space(2)]
    [SerializeField]
    private Rigidbody2D rigid;
    public Rigidbody2D Rigid { get { return rigid; } }

    [SerializeField]
    private Animator anim;
    public Animator Anim { get { return anim; } }

    [SerializeField]
    private Camera mainCamera;

    [SerializeField]
    private GameObject cursorOb;

    [SerializeField]
    private HookAim hookAim;
      
    #region Specs
    [Space(3)]
    [Header("Specs")]
    [Space(2)]
    [SerializeField]
    private float movePower;
    public float MovePower { get { return movePower; } }

    [SerializeField]
    private float flyMovePower;
    public float FlyMovePower { get { return flyMovePower; } }

    [SerializeField]
    private float ropeMovePower;
    public float RopeMovePower { get { return ropeMovePower; } }

    [SerializeField]
    private float ropeAccelerationPower; 
    public float RopeAccelerationPower { get { return ropeAccelerationPower; } }

    [SerializeField]
    private float maxMoveSpeed;
    public float MaxMoveSpeed { get { return maxMoveSpeed; } }

    [SerializeField]
    private float jumpPower;
    public float JumpPower { get { return jumpPower; } }
    #endregion

    [ReadOnly(true)]
    public float MoveForce_Threshold = 0.1f;

    [ReadOnly(true)]
    public float JumpForce_Threshold = 0.05f;

    [Space(3)]
    [Header("FSM")]
    [Space(2)]
    [SerializeField]
    private StateMachine<PlayerAction> fsm; // Player finite state machine
    public StateMachine<PlayerAction> FSM { get { return fsm; } }

    #region Ballancing
    [Space(3)]
    [Header("Ballancing")]
    [Space(2)]
    [SerializeField]
    private bool isJointed = false; 
    public bool IsJointed { get { return isJointed; } set { isJointed = value; } }

    [SerializeField]
    private bool isGround; 
    public bool IsGround { get { return isGround; } }

    [SerializeField]
    private float hztBrakePower;    // horizontal movement brake force
    public float HztBrakePower { get { return hztBrakePower; } }

    [SerializeField]
    private float vtcBrakePower;    // vertical movement brake force
    public float VtcBrakePower { get { return vtcBrakePower; } }

    [SerializeField]
    private float moveHzt;  // Keyboard input - 'A', 'D' *Ground Movement
    public float MoveHzt { get { return moveHzt; } }

    [SerializeField]
    private float moveVtc;  // Keyboard input - 'W', 'S' *Wall Movement 
    public float MoveVtc { get { return moveVtc; } }

    [SerializeField]
    private float inputJumpPower;

    [SerializeField]
    private Hook jointedHook;
    public Hook JointedHook { get { return jointedHook; } set { jointedHook = value; } }

    [SerializeField]
    private RaycastHit2D hookHitInfo;

    [SerializeField]
    private float ropeLength;  // Raycast distance
    public float RopeLength { get { return ropeLength; } }

    [SerializeField]
    private Vector3 mousePos;

    #endregion

    private void Awake()
    {
        mainCamera = Camera.main;
        rigid = GetComponent<Rigidbody2D>();
        fsm = new StateMachine<PlayerAction>(this);

        fsm.AddState("Idle", new PlayerIdle(this));
        fsm.AddState("Run", new PlayerRun(this));
        fsm.AddState("RunStop", new PlayerRunStop(this));
        fsm.AddState("Fall", new PlayerFall(this));
        fsm.AddState("Jump", new PlayerJump(this));
        fsm.AddState("Roping", new PlayerRoping(this));

        fsm.AddAnyState("Jump", () =>
        {
            return !isGround && !isJointed && rigid.velocity.y > JumpForce_Threshold;
        });
        fsm.AddAnyState("Fall", () =>
        {
            return !isGround && !isJointed && rigid.velocity.y < -JumpForce_Threshold;
        });
        fsm.AddAnyState("Roping", () =>
        {
            return isJointed;
        });
        fsm.AddTransition("Fall", "Idle", 0f, () =>
        {
            return isGround;
        });

        fsm.AddTransition("Idle", "Run", 0f, () =>
        {
            // is input Keyboard "A"key or "D" key
            return Mathf.Abs(moveHzt) > MoveForce_Threshold;
        });
        fsm.AddTransition("Run", "RunStop", 0f, () =>
        {
            // isn't input Keyboard "A"key or "D" key
            return Mathf.Abs(moveHzt) == 0;
        });
        fsm.AddTransition("RunStop", "Run", 0f, () =>
        {
            // input swap "A"key -> "D"key or "D"key -> "A"key
            return Mathf.Abs(moveHzt) > MoveForce_Threshold;
        });
        fsm.AddTransition("RunStop", "Idle", 0.2f, () =>
        {
            return Mathf.Abs(moveHzt) <= MoveForce_Threshold;
        });

        fsm.Init("Idle");
    }

    private void Update()
    {
        fsm.Update();
    }

    private void FixedUpdate()
    {
        fsm.FixedUpdate();
    }
    private void LateUpdate()
    {
        fsm.LateUpdate();
    }

    #region Input Action
    #region Normal Movement
    // Keyboard Acition
    // move horizontally with keyboard "a" key and "d" key
    // jump with keyboard "space" key
    // +rope jump 
    private void OnMove(InputValue value)
    {
        moveHzt = value.Get<Vector2>().x;
        moveVtc = value.Get<Vector2>().y;
    }
    private void OnJump(InputValue value)
    {
        if (isGround)
            Jump();
        else if (isJointed)
            RopeJump();
    }

    // normal jumpping
    private void Jump()
    {
        anim.Play("Jump");
        rigid.velocity = new Vector2(rigid.velocity.x, rigid.velocity.y + jumpPower);
    }

    // disjoint hook and rope jumpping
    private void RopeJump()
    {
        Destroy(jointedHook.gameObject);

        anim.Play("RopeJump");
        isJointed = false;
        rigid.AddForce(rigid.velocity.normalized * rigid.velocity.magnitude, ForceMode2D.Impulse);
    }
    #endregion
    #region Mouse / Rope Action
    // Raycast to mouse position
    private void OnMousePos(InputValue value)
    {
        // cursorPos is mousePos
        // +Linerendering
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // move cursor
        cursorOb.transform.position = new Vector3(mousePos.x, mousePos.y, 0);

        if (!IsJointed)
            RopeRayCast();
    }
    // if Raycast hit is not null, linerendering to hit.point
    private void RopeRayCast()
    {
        Vector2 rayDir = (mousePos - transform.position).normalized;
        hookHitInfo = Physics2D.Raycast(transform.position, rayDir, ropeLength, Manager.Layer.hookInteractableLM);

        if (hookHitInfo)
        {
            HookAimSet();

            // hit is Enemy
            if (Manager.Layer.enemyLM.Contain(hookHitInfo.collider.gameObject.layer))
                hookAim.LineOn(LineRenderType.Enemy, hookHitInfo.point);
            // hit is Ground
            else
                hookAim.LineOn(LineRenderType.Ground, hookHitInfo.point);
        }
        else
            hookAim.LineOff();

    }
    // hookshot to mouse position
    private void OnMouseClick(InputValue value)
    {
        if (value.isPressed)
        {
            if (!prAction.IsJointed && hookHitInfo)
                OnHookShot?.Invoke(hookHitInfo.point);
        }
        else
        {

        }
    }

    #endregion

    #region Skills
    private void OnRopeForce(InputValue value)
    {
        if (isJointed)
            RopeForce();
    }
    private void RopeForce()
    {
        // 강한 반동 적용
        // 잔상 등 이펙트 추가
        Debug.Log("RopeForce! : " + rigid.transform.forward);
        Vector2 forceDir = transform.rotation.y == 0 ? Vector2.right : Vector2.left;
        rigid.AddForce(ropeAccelerationPower * forceDir, ForceMode2D.Impulse);
    }
    #endregion
    #endregion

    #region Hook Action

    private void GrabGround(Vector3 pos)
    {

    }
    private void GrabEnemy(GameObject enemy)
    {

    }

    private void HookAimSet()
    {
        float zRot = transform.position.GetAngleToTarget2D(hookHitInfo.point);

        hookAim.transform.rotation = Quaternion.Euler(0, 0, zRot - 90f);
        hookAim.transform.position = transform.position + transform.position.GetDirectionToTarget2D(hookHitInfo.point) * 2f;
    }
    // if hook collide with enemy, Invoke OnGrabbedEnemy
    // else if hook collide with ground, Invoke OnGrabbedGround
    private void HookShot(RaycastHit2D hookHitInfo)
    {
        Vector3 dist = new Vector3(hookHitInfo.point.x - prAction.Rigid.transform.position.x, hookHitInfo.point.y - prAction.Rigid.transform.position.y, 0);
        float zRot = Mathf.Atan2(dist.y, dist.x) * Mathf.Rad2Deg;
        anim.Play("RopeShot");
        isHookShot = true;

        GameObject hookOb = Instantiate(hookPrefab, hookPosOb.transform.position, hookPosOb.transform.rotation);
        Hook hook = hookOb.GetComponent<Hook>();

        // CCD setting
        // time = distance / velocity
        hook.ccdRoutine = StartCoroutine(hook.CCD(dist.magnitude / hookShotPower, new Vector3(hookHitInfo.point.x, hookHitInfo.point.y, 0)));
        hook.Owner = prAction;

        // rope shot
        hook.Rigid?.AddForce(dist.normalized * hookShotPower, ForceMode2D.Impulse);
    }
    #endregion

    #region Collision Callback
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log(collision.gameObject.layer);

        if(Manager.Layer.groundLM.Contain(collision.gameObject.layer))
        {
            // ground check
            isGround = true;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (Manager.Layer.groundLM.Contain(collision.gameObject.layer))
        {
            // ground check
            isGround = false;
        }
    }
    #endregion
}
