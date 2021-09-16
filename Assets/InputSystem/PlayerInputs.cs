using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class PlayerInputs : MonoBehaviour
{
    #region Variables

    private PlayerInput playerInput;
    private InputAction fireAction;
    private InputAction nextWeaponAction;
    [Header("Character inputs:")]
    [SerializeField]
    private Vector2 move;

    [SerializeField]
    private Vector2 look;

    [SerializeField]
    private bool jump;

    [SerializeField]
    private bool sprint;

    [SerializeField]
    private bool aim;

    [SerializeField]
    private bool dash;

    [SerializeField]
    private bool nextWeaponDown;
    [SerializeField]
    private bool nextWeaponHold;
    [SerializeField]
    private bool nextWeaponUp;

    [SerializeField]
    private bool fireDown;
    [SerializeField]
    private bool fireHeld;

    [SerializeField]
    private bool fireReleased;

    [Space(5)]
    [Header("Trigger settings")]
    [SerializeField]
    private bool triggerSprint;
    [SerializeField]
    private bool triggerAim;


    [Header("Movement Settings")]
    [SerializeField]
    private bool analogMovement;

    #endregion

    #region Monobehaviour Callbacks

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        fireAction = playerInput.actions["Fire"];
        nextWeaponAction = playerInput.actions["NextWeapon"];
    }
    private void OnEnable()
    {
        fireAction.Enable();
        nextWeaponAction.Enable();
    }

    private void OnDisable()
    {
        fireAction.Disable();
        nextWeaponAction.Disable();
    }

    private void Update()
    {

        SetFireDown(fireAction.WasPerformedThisFrame());
        SetFireHeld(fireAction.IsPressed());
        SetFireReleased(fireAction.WasReleasedThisFrame());

        SetNextWeapon(nextWeaponAction.WasPressedThisFrame(), nextWeaponAction.WasPerformedThisFrame(),
        nextWeaponAction.WasReleasedThisFrame());
    }

    #endregion

    #region Input System Callbacks
    // public void OnNextWeapon(InputAction.CallbackContext value)
    // {
    //     // if (value.started)
    //     // {
    //     //     Debug.Log("Next weapon");
    //     //     SetNextWeapon(true);
    //     // }

    //     // else
    //     //     SetNextWeapon(false);

    //     SetNextWeapon(value.started, value.performed, value.canceled);
    // }

    // public void OnFire(InputAction.CallbackContext context)
    // {
    //     SetFireDown(context.action.WasPerformedThisFrame());
    //     SetFireHeld(context.performed);
    //     SetFireReleased(context.canceled);
    // }

    public void OnDash(InputAction.CallbackContext value)
    {
        SetDash(value.performed);
    }

    public void OnMove(InputAction.CallbackContext value)
    {
        SetMove(value.ReadValue<Vector2>());
    }

    public void OnLook(InputAction.CallbackContext value)
    {
        SetLook(value.ReadValue<Vector2>());
    }

    public void OnJump(InputAction.CallbackContext value)
    {
        SetJump(value.performed);
    }

    public void OnSprint(InputAction.CallbackContext value)
    {
        SetSprint(value.performed);
    }

    public void OnAim(InputAction.CallbackContext value)
    {
        SetAim(value.performed);
    }

    #endregion

    #region Getters

    public bool GetFireInputReleased()
    {
        return fireReleased;
    }

    public bool GetFireInputDown()
    {
        return fireDown;
    }

    public bool GetFireInputHeld()
    {
        return fireHeld;
    }

    public bool GetNextWeaponDown()
    {
        return nextWeaponDown;
    }
    public bool GetNextWeaponHold()
    {
        return nextWeaponHold;
    }
    public bool GetNextWeaponUp()
    {
        return nextWeaponUp;
    }
    public bool GetDash()
    {
        return dash;
    }
    public Vector2 GetMove()
    {
        return move.normalized;
    }
    public Vector2 GetLook()
    {
        return look;
    }

    public bool GetJump()
    {
        return jump;
    }

    public bool GetSprint()
    {
        return sprint;
    }

    public bool GetAim()
    {
        return aim;
    }

    public bool GetAnalogMovement()
    {
        return analogMovement;
    }
    #endregion

    #region Setters

    private void SetFireReleased(bool canceled)
    {
        fireReleased = canceled;
    }

    private void SetFireHeld(bool performed)
    {
        fireHeld = performed;
    }

    private void SetFireDown(bool started)
    {
        fireDown = started;
    }

    private void SetNextWeapon(bool started, bool performed, bool canceled)
    {
        nextWeaponDown = started;
        nextWeaponHold = performed;
        nextWeaponUp = canceled;
    }

    private void SetDash(bool performed)
    {
        dash = performed;
    }
    private void SetMove(Vector2 value)
    {
        move = value;
    }
    private void SetLook(Vector2 value)
    {
        look = value;
    }
    private void SetJump(bool performed)
    {
        jump = performed;
    }

    private void SetSprint(bool performed)
    {
        if (triggerSprint)
            sprint = !sprint;
        else
            sprint = performed;
    }

    private void SetAim(bool performed)
    {
        if (triggerAim)
            aim = !aim;
        else
            aim = performed;
    }
    #endregion









}