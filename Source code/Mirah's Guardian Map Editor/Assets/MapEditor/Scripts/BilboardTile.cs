using UnityEngine;
using System.Collections;

public class BilboardTile : MonoBehaviour 
{
	#region private data
	private Transform _mainCameraTransform;
	private Transform _transform;
	#endregion

	// Use this for initialization
	void Start () 
	{
		//look at the camera =P
		_mainCameraTransform = Camera.main.transform;
		_transform = this.transform;
	}
	
	// Update is called once per frame by the TileObject class
	public void AUpdate () 
	{
		if(_mainCameraTransform == null)
			return;

		Vector3 __currentRotation = transform.eulerAngles;
		_transform.LookAt(_mainCameraTransform.transform.position + (Vector3.forward * (-5)));
		_transform.eulerAngles = new Vector3(__currentRotation.x, _transform.eulerAngles.y, __currentRotation.z);
	}
}
