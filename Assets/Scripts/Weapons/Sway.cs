using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sway : MonoBehaviour
{
    #region Variables

    #region Weapon Sway

    [Header("Weapon Sway")]
    public float weaponSwayIntensity;
    public float weaponSwaySmoothing;

    public float swayClampX;
    public float swayClampY;


    private PlayerInputs _input;
    private MyWeaponManager _weaponManager;
    private Quaternion originRotation;

    #endregion

    #endregion

    #region Monobehaviour Callbacks

    private void Start()
    {
        _input = transform.parent.parent.parent.GetComponent<PlayerInputs>();
        _weaponManager = transform.parent.parent.parent.GetComponent<MyWeaponManager>();
        // var parent = transform.parent;
        // Debug.Log("First parent - " + parent.gameObject.name);
        // var parentParent = parent.parent;
        // Debug.Log("Second parent - " + parentParent.gameObject.name);
        // var parentParentParent = parentParent.parent;
        // Debug.Log("Third parent - " + parentParentParent.gameObject.name);
        // var inputVar = parentParentParent.GetComponent<PlayerInputs>();
        // if (_input != null)
        //     Debug.Log("input component found");

        // set origin rotation
        originRotation = transform.localRotation;
    }
    private void Update()
    {
        // Debug.Log(_input.GetLook());
        if (!_weaponManager.isAiming)
            UpdateSway();
    }

    #endregion

    #region Private Methods

    private void UpdateSway()
    {
        Vector2 look = _input.GetLook();

        // clamping
        look.x = Mathf.Clamp(look.x, -swayClampX, swayClampX);
        look.y = Mathf.Clamp(look.y, -swayClampY, swayClampY);
        // calculate target rotation
        Quaternion t_adj_x = Quaternion.AngleAxis(weaponSwayIntensity * -look.x, Vector3.up);
        Quaternion t_adj_y = Quaternion.AngleAxis(weaponSwayIntensity * look.y, Vector3.right);
        Quaternion targerRotation = t_adj_x * t_adj_y * originRotation;

        // rotate towards target rotation
        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targerRotation, weaponSwaySmoothing * Time.deltaTime);
    }

    #endregion
}
