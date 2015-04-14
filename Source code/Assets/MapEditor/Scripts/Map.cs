using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class Map : MonoBehaviour 
{
	[Serializable]
	public class SaveDummy
	{
		[Serializable]
		public class DummyVector
		{
			public float x;
			public float y;
			public float z;
			
			public DummyVector(Vector3 p_vector)
			{
				x = p_vector.x;
				y = p_vector.y;
				z = p_vector.z;
			}
			
			public Vector3 GetVector3()
			{
				return new Vector3(x, y, z);
			}
		}
		public List<Tile> loadedTiles;
		public Dictionary<DummyVector, int> map;
		public Dictionary<string, List<int>> tileGroups;
	}

	#region inspector
	public TextAsset map;
	public GameObject tilePrefab;
	#endregion

	#region private data
	private List<Tile> _loadedTiles = new List<Tile>();
	private List<TileObject> _tilesInScene = new List<TileObject>();
	#endregion

	void Awake()
	{
		if(map == null)
			return;

		using(Stream __stream = new MemoryStream(map.bytes))
		{
			try
			{
				BinaryFormatter __formatter = new BinaryFormatter();
				SaveDummy __dummy = (SaveDummy)__formatter.Deserialize(__stream);

				foreach(var tile in __dummy.loadedTiles)
				{
					Texture2D __texture = new Texture2D(1,1);
					__texture.LoadImage(tile.encodedTexture);
					tile.texture = __texture;

					_loadedTiles.Add(tile);
				}

				foreach(var tileInScene in __dummy.map)
				{
					TileObject __tileObj = ((GameObject)GameObject.Instantiate(tilePrefab, tileInScene.Key.GetVector3(), tilePrefab.transform.rotation)).GetComponent<TileObject>();
					__tileObj.Initialize(_loadedTiles.Find(x => x.id == tileInScene.Value));
					_tilesInScene.Add(__tileObj);
				}
			}
			catch(Exception p_exception)
			{
				Debug.LogError("Failed to load map: " + p_exception.Message);
			}
		}
	}

	public Tile GetTileByID(int p_id)
	{
		return _loadedTiles.Find(x => x.id == p_id);
	}

	public TileObject GetTileObjectByPosition(Vector3 p_position)
	{
		return _tilesInScene.Find(x => x.transform.position == p_position);
	}

	public Vector3 TransformPositionToTile(Vector3 p_position)
	{
		return new Vector3(Mathf.Round(p_position.x), p_position.y, Mathf.Round(p_position.z));
	}
}
