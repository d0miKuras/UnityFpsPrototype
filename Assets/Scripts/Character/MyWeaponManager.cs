using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MyWeaponManager : MonoBehaviour
{

    public enum WeaponSwitchState
    {
        Up,
        Down,
        PutDownPrevious,
        PutUpNew,
    }

    #region Variables
    public List<WeaponController> startingWeapons = new List<WeaponController>();
    public Transform weaponParent;
    public Transform downWeaponPosition;
    public float weaponSwitchDelay = 1f;


    private int _currentWeaponIndex;
    private float _lastWeaponSwitch;
    public bool isAiming { get; set; }
    public bool isShooting { get; set; }
    public int activeWeaponIndex { get; private set; }

    PlayerInputs _input;

    WeaponController[] m_WeaponSlots = new WeaponController[1]; // 1 weapon slots
    int m_WeaponSwitchNewWeaponIndex;
    float m_TimeStartedWeaponSwitch;
    Vector3 m_WeaponMainLocalPosition;
    WeaponSwitchState m_WeaponSwitchState;

    UnityAction<WeaponController> onSwitchedToWeapon;
    #endregion

    #region Monobehaviour Callbacks
    // Start is called before the first frame update
    void Start()
    {
        _input = GetComponent<PlayerInputs>();
        m_WeaponSwitchState = WeaponSwitchState.Down;
        activeWeaponIndex = -1;
        onSwitchedToWeapon += OnWeaponSwitched;
        foreach (var w in startingWeapons)
        {
            AddWeapon(w);
        }
        SwitchWeapon(true);
    }

    // Update is called once per frame
    void Update()
    {
        WeaponController activeWeapon = GetActiveWeapon();
        if (activeWeapon && m_WeaponSwitchState == WeaponSwitchState.Up)
        {
            // Handle aiming down sights
            isAiming = _input.GetAim();
            Aim(isAiming);
            activeWeapon.isAiming = isAiming;

            // Handle shooting
            activeWeapon.HandleShotInputs(_input.GetFireInputDown(), _input.GetFireInputHeld(), _input.GetFireInputReleased());
        }

        // Handle weapon switching
        if (!isAiming &&
            (m_WeaponSwitchState == WeaponSwitchState.Up || m_WeaponSwitchState == WeaponSwitchState.Down))
        {
            SwitchWeapon(_input.GetNextWeaponHold());
        }

        // if (_input.GetNextWeaponHold())
        //     m_WeaponSlots[0].ShowWeapon(true);

        // // if (_input.GetFireInputHeld())
        // // {
        // //     isShooting = true;
        // //     m_WeaponSlots[0].Shoot();
        // // }
        // // else
        // //     isShooting = false;
        // m_WeaponSlots[0].HandleShotInputs(_input.GetFireInputDown(), _input.GetFireInputHeld(), _input.GetFireInputReleased());
        // m_WeaponSlots[0].isAiming = isAiming;
    }

    private void LateUpdate()
    {
        UpdateWeaponSwitching();
    }
    #endregion

    #region Private Methods

    public bool AddWeapon(WeaponController weaponPrefab)
    {
        if (HasWeapon(weaponPrefab))
        {
            return false;
        }

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
                weaponInstance.sourcePrefab = weaponPrefab.gameObject;
                weaponInstance.ShowWeapon(false);

                m_WeaponSlots[i] = weaponInstance;

                return true;
            }
        }
        if (GetActiveWeapon() == null)
        {
            SwitchWeapon(true);
        }
        return false;

    }

    // Iterate through weapon slots to find the next valid weapon
    private void SwitchWeapon(bool ascendingOrder)
    {
        int newWeaponIndex = -1;
        int closestSlotDistance = m_WeaponSlots.Length;

        for (int i = 0; i < m_WeaponSlots.Length; i++)
        {
            // if the weapon at this slot is valid, calculate its distance from the active slot and select it if its the closest distance yet
            if (i != activeWeaponIndex && GetWeaponAtSlot(i) != null)
            {
                int distanceToActiveIndex = GetDistanceBetweenSlots(activeWeaponIndex, i, ascendingOrder);

                if (distanceToActiveIndex < closestSlotDistance)
                {
                    closestSlotDistance = distanceToActiveIndex;
                    newWeaponIndex = i;
                }
            }
        }
        SwitchToWeaponIndex(newWeaponIndex);
    }

    // Switches to the given index in weapon slots if the new index is a valid weapon
    private void SwitchToWeaponIndex(int newWeaponIndex, bool force = false)
    {
        if (force || (newWeaponIndex != activeWeaponIndex && newWeaponIndex >= 0))
        {
            // Store data related to weapon switching animation
            m_WeaponSwitchNewWeaponIndex = newWeaponIndex;
            m_TimeStartedWeaponSwitch = Time.time;

            // handle the case of switching to a valid weapon for the first time
            if (GetActiveWeapon() == null)
            {
                m_WeaponMainLocalPosition = downWeaponPosition.localPosition;
                m_WeaponSwitchState = WeaponSwitchState.PutUpNew;
                activeWeaponIndex = m_WeaponSwitchNewWeaponIndex;

                // TODO: Add onSwitchedToWeapon invocation
                WeaponController newWeapon = GetWeaponAtSlot(m_WeaponSwitchNewWeaponIndex);
                if (onSwitchedToWeapon != null)
                {
                    onSwitchedToWeapon.Invoke(newWeapon);
                }
            }
            else // put down the weapon
            {
                m_WeaponSwitchState = WeaponSwitchState.PutDownPrevious;
            }

        }
    }

    private void UpdateWeaponSwitching()
    {
        // Calculate the time ratio (0 to 1) since the weapon switch was triggered
        float switchingTimeFactor = 0f;
        if (weaponSwitchDelay == 0f)
        {
            switchingTimeFactor = 1f;
        }
        else
        {
            switchingTimeFactor = Mathf.Clamp01((Time.time - m_TimeStartedWeaponSwitch) / weaponSwitchDelay);
        }

        // Handle transitioning to a new state
        if (switchingTimeFactor >= 1f)
        {
            if (m_WeaponSwitchState == WeaponSwitchState.PutDownPrevious)
            {
                // Deactivate old weapon
                WeaponController oldWeapon = GetWeaponAtSlot(activeWeaponIndex);
                if (oldWeapon != null)
                {
                    oldWeapon.ShowWeapon(false);
                }

                activeWeaponIndex = m_WeaponSwitchNewWeaponIndex;
                switchingTimeFactor = 0f;

                WeaponController newWeapon = GetWeaponAtSlot(activeWeaponIndex);
                if (onSwitchedToWeapon != null)
                {
                    onSwitchedToWeapon.Invoke(newWeapon);
                }

                // Activate new weapon
                if (newWeapon)
                {
                    m_TimeStartedWeaponSwitch = Time.time;
                    m_WeaponSwitchState = WeaponSwitchState.PutUpNew;
                }
                else // If new weapon is null, dont follow through with putting it back up
                {
                    m_WeaponSwitchState = WeaponSwitchState.Down;
                }
            }
            else if (m_WeaponSwitchState == WeaponSwitchState.PutUpNew)
            {
                m_WeaponSwitchState = WeaponSwitchState.Up;
            }
        }

        // TODO: Handle moving the weapon socket position for the animated weapon switching
        // if(m_WeaponSwitchState == WeaponSwitchState.PutDownPrevious)
        // {
        //     m_WeaponMainLocalPosition = Vector3.Lerp()
        // }

    }

    private bool HasWeapon(WeaponController weaponPrefab)
    {
        foreach (var w in m_WeaponSlots)
        {
            if (w != null && w.sourcePrefab == weaponPrefab.gameObject)
            {
                return true;
            }
        }
        return false;
    }

    // returns the currently active weaopn
    public WeaponController GetActiveWeapon()
    {
        return GetWeaponAtSlot(activeWeaponIndex);
    }

    // returns a weapon at a given slot indee
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

    // calculates the distance between two given weapon slot indexes
    int GetDistanceBetweenSlots(int fromSlotIndex, int toSlotIndex, bool ascendingOrder)
    {
        int distanceBetweenSlots = 0;

        if (ascendingOrder)
        {
            distanceBetweenSlots = toSlotIndex - fromSlotIndex;
        }
        else
        {
            distanceBetweenSlots = -1 * (toSlotIndex - fromSlotIndex);
        }

        if (distanceBetweenSlots < 0)
        {
            distanceBetweenSlots = m_WeaponSlots.Length + distanceBetweenSlots;
        }

        return distanceBetweenSlots;
    }

    void OnWeaponSwitched(WeaponController newWeapon)
    {
        if (newWeapon != null)
        {
            newWeapon.ShowWeapon(true);
        }
    }

    void Aim(bool p_isAiming)
    {


        // Transform t_anchor = currentWeaponGameObject.transform.Find("Anchor");
        // Transform t_state_ads = currentWeaponGameObject.transform.Find("States/ADS");
        // Transform t_state_hip = currentWeaponGameObject.transform.Find("States/Hip");

        Transform t_anchor = GetActiveWeapon().transform.Find("Anchor");
        Debug.Log(t_anchor.position);
        Transform t_state_ads = GetActiveWeapon().transform.Find("States/ADS");
        Transform t_state_hip = GetActiveWeapon().transform.Find("States/Hip");

        if (p_isAiming)
        {
            // aim
            t_anchor.localPosition = Vector3.Lerp(t_anchor.localPosition, t_state_ads.localPosition, GetActiveWeapon().aimSpeed * Time.deltaTime);
            // isAiming = true;
        }
        else
        {
            // hipfire
            t_anchor.localPosition = Vector3.Lerp(t_anchor.localPosition, t_state_hip.localPosition, GetActiveWeapon().aimSpeed * Time.deltaTime);
            // isAiming = false;
        }
    }

    #endregion





}
