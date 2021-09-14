using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class aimScript : MonoBehaviour
{
	Vector3 initialPosition;
	public Transform aimPosition;
	public float aimSpeed;
	Vector3 targetPosition;
    
    // Start is called before the first frame update
    void Start()
    {
		initialPosition = transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
		if (Input.GetButton("Fire2"))
		{
			targetPosition = aimPosition.localPosition;
		}
		else
		{
			targetPosition = initialPosition;
		}
		transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * aimSpeed);
    }
}
