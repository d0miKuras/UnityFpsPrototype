using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum WeaponShootType
{
    Manual,
    Automatic,
    Charged
}


public class WeaponController : MonoBehaviour
{
    #region Variables

    #region General

    [Header("Information")]
    [Tooltip("The name that will be displayed in the UI for this weapon")]
    public string weaponName;
    public WeaponShootType shootType;




    #endregion

    #region Internal References
    [Header("Internal References")]
    [Tooltip("The root object for the weapon, this is what will be deactivated when the weapon isn't active")]
    public GameObject weaponRoot;
    [Tooltip("Tip of the weapon, where the projectiles are shot")]
    public Transform weaponMuzzle;
    #endregion

    #region Shoot Parameters

    [Header("Shoot Parameters")]
    public float delayBetweenShots;
    public int maxAmmo = 10;
    [Tooltip("The projectile prefab")] public ProjectileBase projectilePrefab;

    [Range(0, 1)] public float recoilXAxis;
    [Range(0, 10)] public float recoilYAxis;
    public float maxXRecoil;
    public float maxYRecoil;
    [Tooltip("The total direction of the recoil currently")] [System.NonSerialized] public Vector3 recoilRemaining;
    [Tooltip("The rate at which the recoil is being reset (cooled)")] [Range(0, 1)] public float recoilCoolingRate = 0.1f;



    #endregion


    [Header("Camera Recoil Settings:")]
    public float rotationSpeed = 6f;
    public float returnSpeed = 25f;

    [Space()]
    public Vector3 hipfireRecoilRotation = new Vector3(2f, 2f, 2f);
    public Vector3 adsRecoilRotation = new Vector3(0.5f, 0.5f, 0.5f);

    public float projectileVelocity;
    // public float damage;
    public float aimSpeed;

    public GameObject owner { get; set; }
    public Vector3 muzzleWorldVelocity { get; private set; }

    public bool isAiming { get; set; }





    #region Private Variables
    private bool m_isShooting;
    private int m_CurrentAmmo;
    private float m_LastTimeShot = Mathf.NegativeInfinity;
    private bool m_wantsToShoot;
    public Vector3 m_RecoilRemaining;
    public Vector3 m_CameraRot;
    // private Quaternion m_OriginalCameraRotation;
    Vector3 m_LastMuzzlePosition;
    private PlayerCharacterController m_Controller;
    private Quaternion m_OriginalCameraRotation;
    private Quaternion m_CurrentCameraRotation;
    #endregion

    #endregion

    #region Unity Callbacks

    private void Awake()
    {

        m_LastMuzzlePosition = weaponMuzzle.position;
        m_CurrentAmmo = maxAmmo;
    }
    void Start()
    {
        // m_Controller = owner.transform.GetComponent<PlayerCharacterController>(); 
    }

    private void FixedUpdate()
    {
        HandleRecoilCamera();
    }

    private void LateUpdate()
    {

    }
    // Update is called once per frame
    void Update()
    {
        if (Time.deltaTime > 0)
        {
            muzzleWorldVelocity = (weaponMuzzle.position - m_LastMuzzlePosition) / Time.deltaTime;
            m_LastMuzzlePosition = weaponMuzzle.position;
        }
    }


    public void UseAmmo(int amount)
    {
        m_CurrentAmmo = (int)Mathf.Clamp(m_CurrentAmmo - amount, 0f, maxAmmo);
        m_LastTimeShot = Time.time;

    }

    public bool HandleShotInputs(bool inputDown, bool inputHeld, bool inputUp)
    {
        m_wantsToShoot = inputDown || inputDown;
        switch (shootType)
        {
            case WeaponShootType.Manual:
                if (inputHeld)
                {
                    return TryShoot();
                }
                return false;

            default:
                return false;
        }

    }

    bool TryShoot()
    {
        if (m_CurrentAmmo > 0 && m_LastTimeShot + delayBetweenShots < Time.time)
        {
            var playerController = owner.transform.GetComponent<PlayerCharacterController>();
            Shoot();
            UseAmmo(1);
            return true;
        }
        return false;
    }

    #endregion

    #region Public Methods

    public void Shoot()
    {

        ProjectileBase newProjectile = Instantiate(projectilePrefab, weaponMuzzle.position, Quaternion.LookRotation(weaponMuzzle.forward));
        // RECOIL
        if (isAiming)
        {
            m_RecoilRemaining += new Vector3(-adsRecoilRotation.x, Random.Range(-adsRecoilRotation.y, adsRecoilRotation.y), Random.Range(-adsRecoilRotation.z, adsRecoilRotation.z));
        }
        else
        {
            m_RecoilRemaining += new Vector3(-hipfireRecoilRotation.x, Random.Range(-hipfireRecoilRotation.y, hipfireRecoilRotation.y), Random.Range(-hipfireRecoilRotation.z, hipfireRecoilRotation.z));
        }

        newProjectile.Shoot(this);
    }

    public void ShowWeapon(bool show)
    {
        weaponRoot.SetActive(show);
    }

    void HandleRecoilCamera()
    {

        var playerController = owner.transform.GetComponent<PlayerCharacterController>(); // TODO: figure out if I gotta call it every frame or not
        m_RecoilRemaining = Vector3.Lerp(m_RecoilRemaining, Vector3.zero, returnSpeed * Time.deltaTime);
        m_CameraRot = Vector3.Slerp(m_CameraRot, m_RecoilRemaining, rotationSpeed * Time.deltaTime);
        playerController.CameraRoot.transform.localRotation = Quaternion.Euler(m_CameraRot);


    }


    #endregion
}
