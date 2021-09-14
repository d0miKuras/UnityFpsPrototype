using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class weaponSway : MonoBehaviour
{
	public float amount = 0.055f;
	public float maxAmount = 0.09f;
	float smooth = 3;
	float _smooth;
	Vector3 def;
	Vector2 dafAth;
	Vector3 euler;
	bool aiming;

    // Start is called before the first frame update
    void Start()
    {
		def = transform.localPosition;
		euler = transform.localEulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
		if (Input.GetButton("Fire2"))
		{
			aiming = true;
		}
		else
		{
			aiming = false;
		}

		_smooth = smooth;

		float factorX = -Input.GetAxis("Mouse X") * amount;
		float factorY = -Input.GetAxis("Mouse Y") * amount;

        if(factorX > maxAmount)
		{
			factorX = maxAmount;
		}
        if(factorX < -maxAmount)
		{
			factorX = -maxAmount;
		}

		if (factorY > maxAmount)
		{
			factorY = maxAmount;
		}
		if (factorY < -maxAmount)
		{
			factorY = -maxAmount;
		}
        if (!aiming)
		{
			Vector3 final = new Vector3(def.x + factorX, def.y + factorY, def.z);
			transform.localPosition = Vector3.Lerp(transform.localPosition, final, Time.deltaTime * _smooth);
		}
		else
		{
			Vector3 final = new Vector3(def.x + factorX/2, def.y + factorY/2, def.z);
			transform.localPosition = Vector3.Lerp(transform.localPosition, final, Time.deltaTime * _smooth);
		}

	}
}
