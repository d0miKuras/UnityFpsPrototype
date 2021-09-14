using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(PlayerInputs))]
public class PlayerCharacterController : MonoBehaviour
{
    #region Variables

    [Header("References")]
    public Camera playerCamera;
    public Transform weaponParent; // keeps track of the weapon parent transform
    public GameObject CameraRoot;

    [Header("General")]
    [Tooltip("Force applied downward when in the air")]
    public float gravityDownForce = 20f;
    [Tooltip("Physic layers checked to consider the player grounded")]
    public LayerMask groundCheckLayers = -1;
    [Tooltip("distance from the bottom of the character controller capsule to test for grounded")]
    public float groundCheckDistance = 0.05f;
    public float defautFOV = 50f;
    public float sprintingFOV = 60f;
    public float sprintFOVChangeSpeed;

    [Space(10)]
    [Header("Movement")]
    [Tooltip("Max movement speed when grounded (when not sprinting)")]
    public float maxSpeedOnGround = 10f;
    [Tooltip("Sharpness for the movement when grounded, a low value will make the player accelerate and decelerate slowly, a high value will do the opposite")]
    public float movementSharpnessOnGround = 15;
    [Tooltip("Max movement speed when crouching")]
    [Range(0, 1)]
    public float maxSpeedCrouchedRatio = 0.5f;
    [Tooltip("Max movement speed when not grounded")]
    public float maxSpeedInAir = 10f;
    [Tooltip("Acceleration speed when in the air")]
    public float accelerationSpeedInAir = 25f;
    [Tooltip("Multiplicator for the sprint speed (based on grounded speed)")]
    public float sprintSpeedModifier = 2f;
    [Tooltip("Height at which the player dies instantly when falling off the map")]
    public float killHeight = -50f;

    [Space(10)]
    [Header("Rotation")]
    [Tooltip("Rotation speed for moving the camera")]
    public float rotationSpeed = 200f;
    [Range(0.1f, 1f)]
    [Tooltip("Rotation speed multiplier when aiming")]
    public float aimingRotationMultiplier = 0.4f;


    [Space(10)]
    [Header("Jump")]
    [Tooltip("Force applied upward when jumping")]
    public float jumpForce = 9f;

    [Header("Stance")]
    [Tooltip("Ratio (0-1) of the character height where the camera will be at")]
    public float cameraHeightRatio = 0.9f;
    [Tooltip("Height of character when standing")]
    public float capsuleHeightStanding = 1.8f;
    [Tooltip("Height of character when crouching")]
    public float capsuleHeightCrouching = 0.9f;
    [Tooltip("Speed of crouching transitions")]
    public float crouchingSharpness = 10f;
    #region Weapon Bob

    public float weaponBobIdleIntensityX = 0.01f;
    public float weaponBobIdleIntensityY = 0.01f;
    public float weaponBobMovingAngleMultiplier = 2f;

    public float weaponBobSprintingAngleModifierX = 2f;
    public float weaponBobSprintingAngleModifierY = 2f;
    public float weaponBobMovingSmoothingMultiplier = 4f;

    public float weaponBobSmoothing = 2f;
    float _idleCounter; // goes to infinity, keeps track of time for headbob
    float _movementCounter; // goes to infinity, keeps track of time for headbob

    Vector3 weaponParentOrigin; // for head bobbing
    Vector3 targetWeaponBobPosition;
    #endregion


    // private stuff
    public PlayerInputs _inputs;
    private CharacterController _controller;
    private MyWeaponManager _weaponManager;
    public float cameraVerticalAngleX = 0f;
    public float cameraVerticalAngleY = 0f;
    public float cameraVerticalAngleZ = 0f;
    Vector3 groundNormal;
    Vector3 _characterVelocity;
    Vector3 _latestImpactSpeed;
    float _lastTimeJumped = 0f;
    Vector3 _groundNormal;
    float _targetCharacterHeight;



    // properties
    public Vector3 characterVelocity { get; set; }
    public bool isGrounded { get; private set; }
    public bool hasJumpedThisFrame { get; private set; }
    public bool isCrouching { get; private set; }
    public bool isSprinting { get; private set; }


    private const float _jumpGroundingPreventionTime = 0.2f;
    private const float _groundCheckDistanceInAir = 0.07f;

    WallRun wallRunComponent;

    #endregion

    #region Monobehaviour Callbacks
    // Start is called before the first frame update
    void Start()
    {
        _inputs = GetComponent<PlayerInputs>();
        _controller = GetComponent<CharacterController>();
        _weaponManager = GetComponent<MyWeaponManager>();

        weaponParentOrigin = weaponParent.localPosition; // save the weapon parent origin to use in headbobbing

        wallRunComponent = GetComponent<WallRun>();
        SetFOV(defautFOV);
        // force crouch to false (false uncrouch)
        SetCrouchingState(false, true);
        UpdateCharacterHeight(true);
    }

    // Update is called once per frame
    void Update()
    {
        // TODO: add death on killheight
        hasJumpedThisFrame = false;
        // bool wasGrounded = isGrounded;
        GroundCheck();

        // TODO: add crouching
        UpdateCharacterHeight(false);

        HandleCharacterRotation();
        HandleCharacterMovement();



        // _movementCounter += Time.deltaTime * characterVelocity.magnitude;
        HandleWeaponBob();

    }

    #endregion

    #region Private Methods

    private void HandleWeaponBob()
    {
        //Headbob
        if (characterVelocity == Vector3.zero) // idle headbob
        {
            HeadBob(_idleCounter, weaponBobIdleIntensityX, weaponBobIdleIntensityY);
            _idleCounter += Time.deltaTime;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * weaponBobSmoothing);
        }
        else if (characterVelocity.magnitude > 0)
        {
            float sprintMultiplierX = isSprinting ? weaponBobSprintingAngleModifierX : 1f;
            float sprintMultiplierY = isSprinting ? weaponBobSprintingAngleModifierY : 1f;
            HeadBob(_movementCounter, weaponBobIdleIntensityX * weaponBobMovingAngleMultiplier * sprintMultiplierX, weaponBobIdleIntensityY * weaponBobMovingAngleMultiplier * sprintMultiplierY);
            _movementCounter += Time.deltaTime * characterVelocity.magnitude;
            weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * weaponBobSmoothing * weaponBobMovingSmoothingMultiplier);
        }
    }
    private void HandleCharacterRotation()
    {
        float t_rotationSpeed = _weaponManager.isAiming ? rotationSpeed * aimingRotationMultiplier : rotationSpeed;
        // horizontal character rotation
        {

            transform.Rotate(new Vector3(0f, (_inputs.GetLook().x * t_rotationSpeed), 0f), Space.Self);
        }

        // vertical camera rotation
        {
            // add vertical inputs to the camera's vertical angle
            cameraVerticalAngleX -= _inputs.GetLook().y * t_rotationSpeed;


            // limit the camera's vertical angle
            cameraVerticalAngleX = Mathf.Clamp(cameraVerticalAngleX, -89f, 89f);

            // make the camera pivot up and down
            if (wallRunComponent != null)
            {
                // if (playerCamera.transform.eulerAngles.x < 90f && playerCamera.transform.eulerAngles.x < 90f)
                //     playerCamera.transform.Rotate(new Vector3(-_inputs.GetLook().y * t_rotationSpeed, 0, wallRunComponent.GetCameraRoll()), Space.World);


                playerCamera.transform.localEulerAngles = new Vector3(cameraVerticalAngleX, 0, wallRunComponent.GetCameraRoll());


            }
            else
            {
                playerCamera.transform.localEulerAngles = new Vector3(cameraVerticalAngleX, 0, 0);
            }
        }
    }


    private void HandleCharacterMovement()
    {
        isSprinting = _inputs.GetSprint();
        if (isSprinting)
        {
            SetFOV(Mathf.Lerp(playerCamera.fieldOfView, sprintingFOV, sprintFOVChangeSpeed * Time.deltaTime));
            isSprinting = SetCrouchingState(false, false);
        }
        else
            SetFOV(Mathf.Lerp(playerCamera.fieldOfView, defautFOV, sprintFOVChangeSpeed * Time.deltaTime));

        float speedModifier = isSprinting ? sprintSpeedModifier : 1f;

        Vector3 worldSpaceMoveInput = transform.TransformVector(new Vector3(_inputs.GetMove().x, 0f, _inputs.GetMove().y));
        // Debug.Log(worldSpaceMoveInput);

        if (isGrounded || (wallRunComponent != null && wallRunComponent.IsWallRunning()))
        {
            // calculate the desired velocity from inputs, max speed, and current slope
            Vector3 targetVelocity = worldSpaceMoveInput * maxSpeedOnGround * speedModifier;
            // Debug.Log(targetVelocity);
            // reduce speed if crouching by crouch speed ratio
            if (isCrouching)
                targetVelocity *= maxSpeedCrouchedRatio;
            // slope adjustments
            targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, _groundNormal) * targetVelocity.magnitude;


            // smoothly interpolate between our current velocity and the target velocity based on acceleration speed
            characterVelocity = Vector3.Lerp(characterVelocity, targetVelocity, movementSharpnessOnGround * Time.deltaTime);
        }
        // jumping
        if ((isGrounded || (wallRunComponent != null && wallRunComponent.IsWallRunning())) && _inputs.GetJump())
        {
            // force the crouch state to false
            if (SetCrouchingState(false, false))
            {
                if (isGrounded)
                {
                    // start by canceling out the vertical component of our velocity
                    characterVelocity = new Vector3(characterVelocity.x, 0f, characterVelocity.z);
                    // then, add the jumpSpeed value upwards
                    characterVelocity += Vector3.up * jumpForce;
                }
                else
                {
                    characterVelocity = new Vector3(characterVelocity.x, 0f, characterVelocity.z);
                    // tadd the jumpspeed upwards from the wall
                    characterVelocity += wallRunComponent.GetWallJumpDirection() * jumpForce;
                }


                // remember last time we jumped because we need to prevent snapping to ground for a short time
                _lastTimeJumped = Time.time;
                hasJumpedThisFrame = true;

                // force grounding to false;
                isGrounded = false;
                _groundNormal = Vector3.up;
            }

        }
        // handle air movement
        else
        {
            if (wallRunComponent == null || (wallRunComponent != null && !wallRunComponent.IsWallRunning()))
                // add air acceleration
                characterVelocity += worldSpaceMoveInput * accelerationSpeedInAir * Time.deltaTime;

            // limit air speed to maximum horizontal
            float verticalVelocity = characterVelocity.y;
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(characterVelocity, Vector3.up);
            horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxSpeedInAir * speedModifier);
            characterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

            // apply the gravity to the velocity
            characterVelocity += Vector3.down * gravityDownForce * Time.deltaTime;
        }

        // apply the final calculated velocity value as a character movement
        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
        Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(_controller.height);
        _controller.Move(characterVelocity * Time.deltaTime);

        _latestImpactSpeed = Vector3.zero;
        if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, _controller.radius, characterVelocity.normalized, out RaycastHit hit, characterVelocity.magnitude * Time.deltaTime, groundCheckLayers, QueryTriggerInteraction.Ignore))
        {
            // remember the last impact speed because fall damage logic might need it
            _latestImpactSpeed = characterVelocity;
            characterVelocity = Vector3.ProjectOnPlane(characterVelocity, hit.normal);
        }

    }

    void GroundCheck()
    {
        // Make sure that the ground check distance while already in air is very small, to prevent suddenly snapping to ground
        float chosenGroundCheckDistance = isGrounded ? (_controller.skinWidth + groundCheckDistance) : _groundCheckDistanceInAir;

        // reset values before the check
        isGrounded = false;
        _groundNormal = Vector3.up;

        // only ground check if it's been a short time since the jump, otherwise might instantly snap to the ground
        if (Time.time >= _lastTimeJumped + _jumpGroundingPreventionTime)
        {
            // if we're grounded, get the ground normal
            if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(_controller.height), _controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, groundCheckLayers, QueryTriggerInteraction.Ignore))
            {
                // storing upward direction of the surface
                _groundNormal = hit.normal;

                // its only a valid ground hit if the ground normal goes in the same direction as controller's up AND if the slope angle is lower than the character controller's limit
                if (Vector3.Dot(hit.normal, transform.up) > 0f && IsNormalUnderSlopeLimit(_groundNormal))
                {
                    isGrounded = true;

                    // handle ground snapping
                    if (hit.distance > _controller.skinWidth)
                    {
                        _controller.Move(Vector3.down * hit.distance);
                    }
                }
            }
        }

    }

    // Gets the center point of the bottom hemisphere of the character controller capsule
    private Vector3 GetCapsuleBottomHemisphere()
    {
        return transform.position + (transform.up * _controller.radius);
    }

    // Gets the center point of the top hemisphere of the character controller capsule    
    private Vector3 GetCapsuleTopHemisphere(float atHeight)
    {
        return transform.position + (transform.up * (atHeight - _controller.radius));
    }

    // Gets a reoriented direction that is tangent to a given slope
    private Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
    {
        Vector3 directionRight = Vector3.Cross(direction, transform.up);
        return Vector3.Cross(slopeNormal, directionRight).normalized;
    }

    // returns true if the slope angle in the given noemal is under the slope angle limit of the character controller
    private bool IsNormalUnderSlopeLimit(Vector3 normal)
    {
        return Vector3.Angle(transform.up, normal) <= _controller.slopeLimit;
    }


    // returns false if there was an obstruction
    bool SetCrouchingState(bool crouched, bool ignoreObstructions)
    {
        if (crouched)
        {
            _targetCharacterHeight = capsuleHeightCrouching;
        }
        else
        {
            if (!ignoreObstructions)
            {
                Collider[] standingOverlaps = Physics.OverlapCapsule(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(capsuleHeightStanding), _controller.radius, groundCheckLayers, QueryTriggerInteraction.Ignore);
                foreach (Collider collider in standingOverlaps)
                {
                    if (collider != _controller)
                    {
                        return false;
                    }
                }
            }
            _targetCharacterHeight = capsuleHeightStanding;
        }

        isCrouching = crouched;
        return true;
    }

    void UpdateCharacterHeight(bool forced)
    {
        // instantly updates height
        if (forced)
        {
            _controller.height = _targetCharacterHeight;
            _controller.center = Vector3.up * _controller.height * 0.5f;
            CameraRoot.transform.localPosition = Vector3.up * _targetCharacterHeight * cameraHeightRatio;
        }
        // smoothly update height
        else if (_controller.height != _targetCharacterHeight)
        {
            _controller.height = Mathf.Lerp(_controller.height, _targetCharacterHeight, crouchingSharpness * Time.deltaTime);
            _controller.center = Vector3.up * _controller.height * 0.5f;
            CameraRoot.transform.localPosition = Vector3.Lerp(CameraRoot.transform.localPosition, Vector3.up * _targetCharacterHeight * cameraHeightRatio, crouchingSharpness * Time.deltaTime);

        }
    }

    void HeadBob(float p_z, float p_x_intensity, float p_y_intensity)
    {
        targetWeaponBobPosition = weaponParentOrigin + new Vector3(Mathf.Cos(p_z) * p_x_intensity, Mathf.Sin(p_z * 2) * p_y_intensity, 0f);
    }


    #endregion

    #region Public Methods
    public void SetFOV(float fov)
    {
        playerCamera.fieldOfView = fov;
    }

    #endregion
}
