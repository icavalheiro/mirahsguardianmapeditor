using UnityEngine;
using System.Collections;

public class NavigationMarker : MonoBehaviour 
{
	#region private data
	private Camera _camera;
	private GUIStyle _style;
	private Material _lineMaterial;
	private bool _showGrid = true;
	private Interface _interface;
	#endregion

	void Start()
	{
		//get the interface from the scene opbject that this script is in
		_interface = this.GetComponent<Interface>();

		//create material for drawing with opengl
		_lineMaterial = new Material("Shader \"Lines/Colored Blended\" {" + "SubShader { Pass { " + "    Blend SrcAlpha OneMinusSrcAlpha " + "    ZWrite Off Cull Off Fog { Mode Off } " + "    BindChannels {" + "      Bind \"vertex\", vertex Bind \"color\", color }" + "} } }");
		_lineMaterial.hideFlags = HideFlags.HideAndDontSave;
		_lineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
		_lineMaterial.color = new Color(33/255,76/255,64/255,0.3f);

		//setup class variables
		_camera = this.gameObject.GetComponent<Camera>();
		_style = new GUIStyle();
		_style.normal.textColor = Color.white;
		_style.alignment = TextAnchor.MiddleCenter;
		_style.fontSize = 9;
	}
	 
	void OnGUI()
	{
		if(_interface.lockHUD)
			return;

		//draw (0,0) mark
		Vector3 __screenPosition = _camera.WorldToScreenPoint(Vector3.zero + (0.4f * Vector3.up));
		Rect __drawRect = new Rect(__screenPosition.x - 30, (Screen.height - __screenPosition.y) - 10, 60, 20);

		/*
		//do a series of tests to assure that the mark is always visible
		if(__drawRect.x < 0)
			__drawRect = new Rect(0, __drawRect.y, __drawRect.width, __drawRect.height);

		if(__drawRect.y < 0)
			__drawRect = new Rect(__drawRect.x, 0, __drawRect.width, __drawRect.height);

		if(__drawRect.x + __drawRect.width > Screen.width)
			__drawRect = new Rect(Screen.width - __drawRect.width, __drawRect.y, __drawRect.width, __drawRect.height);

		if(__drawRect.y + __drawRect.height > Screen.height)
			__drawRect = new Rect(__drawRect.x, Screen.height - __drawRect.height, __drawRect.width, __drawRect.height);
		*/
		GUI.Label(__drawRect, "(0,0)", _style);

		//_showGrid = GUI.Toggle(new Rect(5,5,150,20), _showGrid, "Grid");
	}

	void OnPostRender()
	{
		//draw the grid lines

		if(_showGrid == false)
			return;

		_lineMaterial.SetPass(0);

		Ray __centerOfScreenRay = _camera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 1));
		Vector3 __centerOfScreen = __centerOfScreenRay.origin + (((-__centerOfScreenRay.origin.y)/__centerOfScreenRay.direction.y) * __centerOfScreenRay.direction);
		__centerOfScreen = new Vector3(Mathf.Round(__centerOfScreen.x), 0, Mathf.Round(__centerOfScreen.z));

		int __numberOfGridsToDraw = 50;
		int __numberOfGridsToDrawHeight = (int)(__numberOfGridsToDraw * 0.5f);
		for(int l = (int)(-__numberOfGridsToDrawHeight * 0.5f); l < (__numberOfGridsToDrawHeight * 0.5f); l++)
		{
			for(int c = (int)(-__numberOfGridsToDraw * 0.5f); c < (__numberOfGridsToDraw * 0.5f); c++)
			{
				DrawSquare(new Vector2(__centerOfScreen.x + c, __centerOfScreen.z + l), 1);
			}
		}

	}

	//desenha uma linha com OpenGL (ou directX)
	private void DrawLine(Vector2 p_start, Vector2 p_end)
	{
		GL.PushMatrix();
		GL.Begin(GL.LINES);
		{
			GL.Color(new Color(1,1,1,0.07f));
			GL.Vertex3(p_start.x, -0.1f, p_start.y);
			GL.Vertex3(p_end.x, -0.1f, p_end.y);
		}
		GL.End();
		GL.PopMatrix();
	}
	
	//desenha tiles no ponto informado (centro) de tamanho p_size
	private void DrawSquare(Vector2 p_point, float p_size)
	{
		float __halfSize = p_size * 0.5f;

		//top
		DrawLine(new Vector2(p_point.x - __halfSize, p_point.y - __halfSize), new Vector2(p_point.x + __halfSize, p_point.y - __halfSize));
		
		//bot
		DrawLine(new Vector2(p_point.x - __halfSize, p_point.y + __halfSize), new Vector2(p_point.x + __halfSize, p_point.y + __halfSize));
		
		//left
		DrawLine(new Vector2(p_point.x - __halfSize, p_point.y - __halfSize), new Vector2(p_point.x - __halfSize, p_point.y + __halfSize));
		
		//right
		DrawLine(new Vector2(p_point.x + __halfSize, p_point.y - __halfSize), new Vector2(p_point.x + __halfSize, p_point.y + __halfSize));
	}
}
