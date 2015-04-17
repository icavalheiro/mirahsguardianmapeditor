using UnityEngine;
using System.Collections;

public class TileObject : MonoBehaviour 
{
	#region inspector
	public Collider characterCollider;
	public Collider projectileCollider;
	public Collider groundCollider;
	public MeshRenderer groundRenderer;
	public MeshRenderer bilboardRenderer;
	public MeshRenderer verticalRenderer;
	public BilboardTile bilboardTile;
	public Transform scaleManager;

	public Material opaqueMaterial;
	public Material transparentMaterial;
	#endregion

	public Tile tile;
	public Vector2 position 
	{
		get
		{
			return new Vector2(this.transform.position.x, this.transform.position.z);
		}
	}

	//called by the entity that will instaitate this object, after it being instantiated
	public void Initialize(Tile p_tile)
	{
		this.tile = p_tile;
		this.tile.onChange += SetUp;
		SetUp();
	}

	private void SetUp()
	{
		groundRenderer.material = (tile.useTransparency) ? transparentMaterial : opaqueMaterial;
		verticalRenderer.material = (tile.useTransparency) ? transparentMaterial : opaqueMaterial;
		bilboardRenderer.material = (tile.useTransparency) ? transparentMaterial : opaqueMaterial;

		groundCollider.enabled = tile.type == Tile.Type.GROUND;
		characterCollider.enabled = tile.blockCharacter;
		projectileCollider.enabled = tile.blockProjectile;
		
		groundRenderer.enabled = tile.type == Tile.Type.GROUND;
		groundRenderer.material.mainTexture = (groundRenderer.enabled) ? tile.texture : null;
		bilboardRenderer.enabled = tile.type == Tile.Type.BILBOARD;
		bilboardRenderer.material.mainTexture = (bilboardRenderer.enabled) ? tile.texture : null;
		verticalRenderer.enabled = tile.type == Tile.Type.VERTICAL;
		verticalRenderer.material.mainTexture = (verticalRenderer.enabled) ? tile.texture : null;
		
		scaleManager.localScale = new Vector3(tile.scaleX, tile.scaleY, 1);

		var __currentLocalPosition = verticalRenderer.transform.localPosition;
		verticalRenderer.transform.localPosition = new Vector3(tile.yDesloc, __currentLocalPosition.y,tile.xDesloc);

		__currentLocalPosition = bilboardRenderer.transform.localPosition;
		bilboardRenderer.transform.localPosition = new Vector3(tile.yDesloc, __currentLocalPosition.y, tile.xDesloc);

		__currentLocalPosition = groundRenderer.transform.localPosition;
		groundRenderer.transform.localPosition = new Vector3(tile.yDesloc, __currentLocalPosition.y, tile.xDesloc);
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(bilboardRenderer.enabled)
			bilboardTile.AUpdate();
	}

	void OnDestroy()
	{
		this.tile.onChange -= SetUp;
	}
}
