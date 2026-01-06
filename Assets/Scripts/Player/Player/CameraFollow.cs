using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
	public float FollowSpeed = 2f;
	public Transform Target;
	public Vector2 DeadZone = new Vector2(1.5f, 0.75f);

	// Transform of the camera to shake. Grabs the gameObject's transform
	// if null.
	private Transform camTransform;

	// How long the object should shake for.
	public float shakeDuration = 0f;

	// Amplitude of the shake. A larger value shakes the camera harder.
	public float shakeAmount = 0.1f;
	public float decreaseFactor = 1.0f;

	Vector3 originalPos;

	void Awake()
	{
		Cursor.visible = false;
		if (camTransform == null)
		{
			camTransform = GetComponent(typeof(Transform)) as Transform;
		}
	}

	void OnEnable()
	{
		originalPos = camTransform.localPosition;
	}

	private void Update()
	{
		if (Target == null)
			return;

		Vector3 targetPosition = Target.position;
		targetPosition.z = -10f;

		Vector3 currentPosition = camTransform.position;
		Vector3 newPosition = currentPosition;
		Vector3 delta = targetPosition - currentPosition;

		if (Mathf.Abs(delta.x) > DeadZone.x)
			newPosition.x = targetPosition.x - Mathf.Sign(delta.x) * DeadZone.x;

		if (Mathf.Abs(delta.y) > DeadZone.y)
			newPosition.y = targetPosition.y - Mathf.Sign(delta.y) * DeadZone.y;

		newPosition.z = targetPosition.z;
		camTransform.position = Vector3.Slerp(currentPosition, newPosition, FollowSpeed * Time.deltaTime);

		if (shakeDuration > 0)
		{
			camTransform.localPosition = originalPos + Random.insideUnitSphere * shakeAmount;

			shakeDuration -= Time.deltaTime * decreaseFactor;
		}
	}

	public void ShakeCamera()
	{
		originalPos = camTransform.localPosition;
		shakeDuration = 0.2f;
	}
}
