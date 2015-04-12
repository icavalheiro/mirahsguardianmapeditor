using UnityEngine;
using System.Collections.Generic;

public class CameraMoviment : MonoBehaviour 
{
	#region Inspector
	public float speed = 5;
	#endregion

	#region private data
	private Transform _transform;
	private bool _lockUpdate = false;
	#endregion

	void Start()
	{
		_transform = this.transform;
	}

	void OnApplicationFocus(bool p_focused) 
	{
		_lockUpdate = !p_focused;
	}

	// Update is called once per frame
	void Update () 
	{
		if(_lockUpdate)
			return;

		var __horizontal = Input.GetAxis("Horizontal");
		var __vertical = Input.GetAxis("Vertical");

		/*float __cornersSize = 20;

		Dictionary<Rect, Vector2> __borderRects = new Dictionary<Rect, Vector2>();

		//topLeft
		//__borderRects.Add(new Rect(0,0,__cornersSize,__cornersSize), new Vector2(-1,1));
		//topRight
		__borderRects.Add(new Rect(Screen.width-__cornersSize,0,__cornersSize,__cornersSize), new Vector2(1,1));
		//bottomLeft
		//__borderRects.Add(new Rect(0, Screen.height-__cornersSize,__cornersSize,__cornersSize), new Vector2(-1,-1));
		//bottomRight
		__borderRects.Add(new Rect(Screen.width-__cornersSize, Screen.height-__cornersSize,__cornersSize,__cornersSize), new Vector2(1,-1));

		//top
		__borderRects.Add(new Rect(__cornersSize+100,0,Screen.width-__cornersSize-__cornersSize-100,__cornersSize), new Vector2(0,1));
		//left
		__borderRects.Add(new Rect(0,__cornersSize,__cornersSize,Screen.height-__cornersSize-__cornersSize), new Vector2(-1,0));
		//right
		__borderRects.Add(new Rect(Screen.width-__cornersSize,__cornersSize,__cornersSize,Screen.height-__cornersSize-__cornersSize), new Vector2(1,0));
		//bottom
		__borderRects.Add(new Rect(__cornersSize+100,Screen.height-__cornersSize,Screen.width-__cornersSize-__cornersSize-100,__cornersSize), new Vector2(0,-1));

		Vector2 __mousePosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

		foreach(var rect in __borderRects)
		{
			if(rect.Key.Contains(__mousePosition))
			{
				__horizontal = rect.Value.x;
				__vertical = rect.Value.y;
			}
		}*/

		Vector3 __newPosition = new Vector3(_transform.position.x + (speed * __horizontal * Time.deltaTime), 
		                                    _transform.position.y,
		                                    _transform.position.z + (speed * __vertical * Time.deltaTime));
		_transform.position = __newPosition;

		//missing
		//do moviment by mouse approaching screen borders
	}
}
