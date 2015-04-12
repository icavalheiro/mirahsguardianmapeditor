using UnityEngine;
using System.Collections;


//Unity bugada do caraleo...
//tem um bg que quando vc redimensiona a tela
//o espaço a mais adicionado nao funciona corretamente,
//a posiçao do mouse nao atualiza nele, e os botoes
//nao funcionam tbm...
public class ScreenBugFixer
{
	private int prevWidth;
	private int prevHeight;
	
	private bool isInited = false;
	
	
	private void Init()
	{
		prevWidth  = Screen.width;
		prevHeight = Screen.height;
		
		isInited = true;
	}
	
	private void SetResolution()
	{
		prevWidth  = Screen.width;
		prevHeight = Screen.height;
		
		Screen.SetResolution(Screen.width, Screen.height, Screen.fullScreen);
	}
	
	public void Update()
	{
		if (!isInited)
		{
			Init();
		}
		
		if ((Screen.width != prevWidth) || (Screen.height != prevHeight))
		{
			SetResolution();
		}
	}
}
