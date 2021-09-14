using System.Collections.Generic;
using UnityEngine;

public class MyWeaponManager : MonoBehaviour
{



    #region Variables
    public List<WeaponController> startingWeapons = new List<WeaponController>();
    public Transform weaponParent;
    // public float switchTime = 1f;
    public LayerMask canBeShot;

    private PlayerInputs _input;
    private GameObject currentWeaponGameObject;

    private WeaponController[] m_WeaponSlots = new WeaponController[1]; // 1 weapon slots
    private int _currentWeaponIndex;
    private float _lastWeaponSwitch;
    public bool isAiming { get; set; }
    public bool isShooting { get; set; }
    public int activeWeaponIndex { get; private set; }

    // private Vector3 newWeaponRotation;
    // private Vector3 newWeaponRotationVelocity;
    // private Vector3 targetWeaponRotation;
    // private Vector3 targetWeaponRotationVelocity;

    #endregion

    #region Monobehaviour Callbacks
    // Start is called before the first frame update
    void Start()
    {
        _input = GetComponent<PlayerInputs>();
        activeWeaponIndex = -1;
        foreach (var w in startingWeapons)
        {
            AddWeapon(w);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_input.GetNextWeaponHold())
            m_WeaponSlots[0].ShowWeapon(true);

        // if (_input.GetFireInputHeld())
        // {
        //     isShooting = true;
        //     m_WeaponSlots[0].Shoot();
        // }
        // else
        //     isShooting = false;
        m_WeaponSlots[0].HandleShotInputs(_input.GetFireInputDown(), _input.GetFireInputHeld(), _input.GetFireInputReleased());
        m_WeaponSlots[0].isAiming = isAiming;
    }

    #endregion

    #region Private Methods

    public bool AddWeapon(WeaponController weaponPrefab)
    {
        for (int i = 0; i < m_WeaponSlots.Length; i++)
        {
            if (m_WeaponSlots[i] == null)
            {
                // spawn weapon prefab as child of weapon parent
                WeaponController weaponInstance = Instantiate(weaponPrefab, weaponParent);
                weaponInstance.transform.localPosition = Vector3.zero;
                weaponInstance.transform.localRotation = Quaternion.identity;

                // TODO: set the owner of this gameObject so the weapon can alter projectile logic accordingly
                weaponInstance.owner = gameObject;
                weaponInstance.ShowWeapon(false);

                m_WeaponSlots[i] = weaponInstance;

                return true;
            }
        }
        return false;

    }

    public WeaponController GetActiveWeapon()
    {
        return GetWeaponAtSlot(0);
    }

    public WeaponController GetWeaponAtSlot(int index)
    {
        // find active weapon in the weapon slots
        if (index >= 0 && index < m_WeaponSlots.Length)
        {
            return m_WeaponSlots[index];
        }

        // if we didn't find a valid active weapon, return null;
        return null;
    }

    // void Equip(int p_ind)
    // {
    //     if (currentWeaponGameObject != null)
    //         Destroy(currentWeaponGameObject);

    //     _currentWeaponIndex = p_ind;
    //     GameObject t_newWeapon = Instantiate(loadout[p_ind].gameObject, weaponParent.position, weaponParent.rotation, weaponParent) as GameObject;

    //     t_newWeapon.transform.localPosition = Vector3.zero;
    //     t_newWeapon.transform.localEulerAngles = Vector3.zero;
    //     currentWeaponGameObject = t_newWeapon;

    // }

    // void Shoot()
    // {
    //     Transform t_spawn = transform.Find("Main Camera");
    //     Transform muzzle = loadout[_currentWeaponIndex].prefab.transform.Find("Anchor/Resources/Muzzle");
    //     var test = loadout[_currentWeaponIndex].prefab.transform;
    //     Debug.Log(test.position);
    //     GameObject t_newProjectile = Instantiate(loadout[_currentWeaponIndex].projectile.projectilePrefab, muzzle.position, Quaternion.identity, muzzle) as GameObject;
    //     // Destroy(t_newProjectile, 5f);
    //     if (Physics.Raycast(t_spawn.position, t_spawn.forward, out RaycastHit t_hit, 1000f, canBeShot))
    //     {
    //         // GameObject t_newBulletHole = Instantiate(loadout[_currentWeaponIndex].pro.bulletHolePrefab, t_hit.point + t_hit.normal * 0.001f, Quaternion.identity) as GameObject;
    //         // t_newBulletHole.transform.LookAt(t_hit.point + t_hit.normal);
    //         // Destroy(t_newBulletHole, 5f);

    //     }
    // }

    void Aim(bool p_isAiming)
    {


        Transform t_anchor = currentWeaponGameObject.transform.Find("Anchor");
        Transform t_state_ads = currentWeaponGameObject.transform.Find("States/ADS");
        Transform t_state_hip = currentWeaponGameObject.transform.Find("States/Hip");

        if (p_isAiming)
        {
            // aim
            t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_ads.position, m_WeaponSlots[_currentWeaponIndex].aimSpeed * Time.deltaTime);
            isAiming = true;
        }
        else
        {
            // hipfire
            t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_hip.position, m_WeaponSlots[_currentWeaponIndex].aimSpeed * Time.deltaTime);
            isAiming = false;
        }
    }

    #endregion





}
