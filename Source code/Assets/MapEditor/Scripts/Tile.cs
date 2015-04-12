using UnityEngine;
using System;

//Classe eh basicamente uma Struct
[System.Serializable]
public class Tile
{
	public enum Type
	{
		NONE = 4,
		VERTICAL = 0,
		BILBOARD = 1,
		GROUND = 2
	}

	public event Action onChange;

	[NonSerialized]
	[System.Xml.Serialization.XmlIgnore]
	public Texture2D texture;

	[System.Xml.Serialization.XmlIgnore]
	public float scaleX
	{
		get 
		{
			return ((texture.width > texture.height) ? (texture.width/(float)texture.height) : (1)) * scale; 
		}
	}

	[System.Xml.Serialization.XmlIgnore]
	public float scaleY
	{
		get 
		{
			return ((texture.height > texture.width) ? (texture.height/(float)texture.width) : (1)) * scale; 
		}
	}

	#region Properties
	public string name = "";
	public float scale = 1;
	public Type type = Type.GROUND;
	public bool blockCharacter = false;
	public bool blockProjectile = false;
	public bool useTransparency = false;
	public byte[] encodedTexture;
	public float xDesloc = 0;
	public float yDesloc = 0;
	public bool pathable = false;

	public int id;
	#endregion

	public void OnChange()
	{
		if(onChange != null)
			onChange();
	}

	public Tile GetTileForSerialization()
	{
		Tile __toReturn = new Tile();
		__toReturn.name = name;
		__toReturn.scale = scale;
		__toReturn.type = type;
		__toReturn.blockCharacter = blockCharacter;
		__toReturn.blockProjectile = blockProjectile;
		__toReturn.useTransparency = useTransparency;
		__toReturn.id = id;
		__toReturn.encodedTexture = encodedTexture;
		__toReturn.xDesloc = xDesloc;
		__toReturn.yDesloc = yDesloc;
		__toReturn.pathable = pathable;

		return __toReturn;
	}
}
