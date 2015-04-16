using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;

public class Interface : MonoBehaviour 
{

	#region inspector
	public GameObject selectionPlanePrefab;
	public int selectionArea = 1;
	public GUIStyle btnStyle;
	public GameObject tilePrefab;
	#endregion

	#region public data
	[HideInInspector]
	public bool lockHUD = false;
	#endregion

	#region static data
	private static string _pathToLoad = "";
	#endregion

	#region private data
	private Camera _camera;
	private List<Transform> _selectionTransforms = new List<Transform>();
	private int _lastSelectionArea = 0;

	private List<Tile> _loadedTiles = new List<Tile>();
	private Tile _currentSelectedTile;

	private List<Rect> _areasInScreenToIgnoreMouseClick = new List<Rect>();
	private List<TileObject> _tilesOnScene = new List<TileObject>();

	private int _tileIdCount = 0;

	private ScreenBugFixer _bugFixer = new ScreenBugFixer();

	private bool _useDialog
	{
		get
		{
			return (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer);
		}
	}

	private bool _lockUpdate = false;

	
	private Vector2 _tilesSelectionInterfaceScrollVector = Vector2.zero;
	private List<GameObject> _placedGameObjectsAtCurrentDrag = new List<GameObject>();

	private Texture2D _whiteTexture;

	private List<int> _selectedTiles = new List<int>();
	private Dictionary<string, List<int>> _groups = new Dictionary<string, List<int>>();
	private List<string> _expandedGroups = new List<string>();
	#endregion

	#region private events
	private Action onHUDLocked;
	#endregion

	//unity start callback
	void Start() 
	{
		_whiteTexture = new Texture2D(1,1);
		_whiteTexture.SetPixel(0,0, Color.white);
		_whiteTexture.Apply();

		_camera = this.gameObject.GetComponent<Camera>();

		
		if(_pathToLoad != "")
		{
			LoadMap(_pathToLoad);
			lockHUD = true;
		}

		_pathToLoad = "";
	}


	//unity update callback
	void Update()
	{
		_bugFixer.Update();

		if(lockHUD || _lockUpdate)
			return;

		UpdateSelectionArea();
		UpdateSelectionPosition();
		CheckShortKeyInputs();
		DetectMouseClickForPlacementOfTile();
	}
	//unity gui draw callback
	void OnGUI()
	{
		if(!lockHUD)
			DrawHUD();
		else if(onHUDLocked != null)
			onHUDLocked();
	}

	void OnApplicationFocus(bool p_focused) 
	{
		_lockUpdate = !p_focused;
	}

	#region OnGUI methods
	private void DrawHUD()
	{
		//SELECTION AREA BTNS
		Rect __selectionAreaBtnsRect = new Rect(Screen.width-80, 15, 80, 100);
		if(_areasInScreenToIgnoreMouseClick.Contains(__selectionAreaBtnsRect) == false)
			_areasInScreenToIgnoreMouseClick.Add(__selectionAreaBtnsRect);
		GUI.BeginGroup(__selectionAreaBtnsRect);
		{
			GUI.Label(new Rect(0,0, __selectionAreaBtnsRect.width, 20), "Selection:");
			selectionArea = GUI.SelectionGrid(new Rect(0, 25, __selectionAreaBtnsRect.width - 15, 60), selectionArea -1, new string[] {"1x1 [1]", "3x3 [2]", "5x5 [3]"}, 1, btnStyle) + 1;
		}
		GUI.EndGroup();
		
		//EDIT TEXTURE
		Rect __editTileBtnRect = new Rect(Screen.width - 170, Screen.height - 70, 160, 70);
		if(_areasInScreenToIgnoreMouseClick.Contains(__editTileBtnRect) == false)
			_areasInScreenToIgnoreMouseClick.Add(__editTileBtnRect);
		GUI.BeginGroup(__editTileBtnRect);
		{
			if(_currentSelectedTile != null)
				if(GUI.Button(new Rect(0,0,__editTileBtnRect.width, __editTileBtnRect.height - 15), "Edit tile [e]", btnStyle))
					EditTile(_currentSelectedTile);
		}
		GUI.EndGroup();
		
		//TILES SELECTION INTERFACE
		Rect __tilesSelectionInterfaceRect = new Rect(-10, 35, 140, Screen.height - 70);
		if(_areasInScreenToIgnoreMouseClick.Contains(__tilesSelectionInterfaceRect) == false)
			_areasInScreenToIgnoreMouseClick.Add(__tilesSelectionInterfaceRect);
		GUI.BeginGroup(__tilesSelectionInterfaceRect);
		{
			GUI.Box(new Rect(0, 0, __tilesSelectionInterfaceRect.width, __tilesSelectionInterfaceRect.height), "");
			int __tilesToDraw = _loadedTiles.Count;
			foreach(var group in _groups)
				if(_expandedGroups.Contains(group.Key) == false)
					__tilesToDraw -= group.Value.Count;

			float __currentHeightToDraw = 0;

			Rect __tilesSelectionInterfaceScrollView = new Rect(0,0, 75, (__tilesToDraw * 75) + ((__tilesToDraw + 1) * 10) + (_groups.Count * 25));
			_tilesSelectionInterfaceScrollVector = GUI.BeginScrollView(new Rect(20, 5, __tilesSelectionInterfaceRect.width - 20, __tilesSelectionInterfaceRect.height - 10),
			                                                            _tilesSelectionInterfaceScrollVector, 
			                                                            __tilesSelectionInterfaceScrollView, 
			                                                            false, 
			                                                            _tilesSelectionInterfaceScrollVector.y >= __tilesSelectionInterfaceScrollView.height);
			{
				Action<int, Rect> __drawBtnAction = (p_number, p_position) =>
				{
					if(_currentSelectedTile == _loadedTiles[p_number])
					{
						var __backupColor = GUI.color;
						GUI.color = new Color(1,1,1,0.25f);
						GUI.DrawTexture(new Rect(p_position.x-2, p_position.y-2, p_position.width+4, p_position.height+4), _whiteTexture);
						GUI.color = __backupColor;
						GUI.Box(p_position, _loadedTiles[p_number].texture);
					}
					else if(GUI.Button(p_position, _loadedTiles[p_number].texture))
					_currentSelectedTile = _loadedTiles[p_number];
					
					__currentHeightToDraw += p_position.height + 10;
				};


				//DRAW GROUPS
				List<string> __toRemove = new List<string>();
				foreach(var group in _groups)
				{
					bool __isExpanded = _expandedGroups.Contains(group.Key);
					float __btnWidth = 20;

					if(GUI.Button(new Rect(0,__currentHeightToDraw,__btnWidth,20), (__isExpanded) ? "-" : "+"))
					{
						if(__isExpanded)
							_expandedGroups.Remove(group.Key);
						else
							_expandedGroups.Add(group.Key);
					}

					GUI.Label(new Rect(__btnWidth,__currentHeightToDraw, 60,20), group.Key);

					if(GUI.Button(new Rect(__btnWidth+60, __currentHeightToDraw, __btnWidth, 20), "x"))
						__toRemove.Add(group.Key);

					__currentHeightToDraw += 25;

					if(__isExpanded == false)
						continue;

					foreach(var tileNumber in group.Value)
					{
						Rect __btnRect = new Rect(17, __currentHeightToDraw, 75, 75);
						__drawBtnAction(tileNumber, __btnRect);
					}
				}
				__toRemove.ForEach(x => _groups.Remove(x));


				//DRAW UNGROUPED TILES
				for(int i = 0; i < _loadedTiles.Count; i++)
				{
					bool __isInGroup = false;
					foreach(var group in _groups)
					{
						bool __found = false;
						foreach(var tileNumber in group.Value)
						{
							if(tileNumber == i)
							{
								__found = true;
								break;
							}
						}

						if(__found == true)
						{
							__isInGroup = true;
							break;
						}
					}

					if(__isInGroup)
						continue;

					Rect __btnRect = new Rect(2, __currentHeightToDraw, 75, 75);

					Rect __checkMarkRect = new Rect(__btnRect.x+__btnRect.width+4,__btnRect.y,15,15);
					bool __selected = _selectedTiles.Contains(i);
					if(GUI.Toggle(__checkMarkRect, __selected, "") != __selected)
					{
						if(__selected)
							_selectedTiles.Remove(i);
						else
							_selectedTiles.Add(i);
					}

					__drawBtnAction(i, __btnRect);
				}
			}
			GUI.EndScrollView();
		}
		GUI.EndGroup();
		
		// CREATE/DELETE BTNS
		Rect __tilesCreateDeleteRect = new Rect(5, 5, __tilesSelectionInterfaceRect.width - 15, __tilesSelectionInterfaceRect.y - 10);
		if(_areasInScreenToIgnoreMouseClick.Contains(__tilesCreateDeleteRect) == false)
			_areasInScreenToIgnoreMouseClick.Add(__tilesCreateDeleteRect);
		GUI.BeginGroup(__tilesCreateDeleteRect);
		{
			//create
			if(GUI.Button(new Rect(0,0,(__tilesCreateDeleteRect.width * 0.5f) - 10, __tilesCreateDeleteRect.height), "Create", btnStyle))
				OnCreateBtnClicked();

			//delete ##### desabilitei para apresentaçao de quinta (resolver 2 bugs - deletar em grupo - colocar novo tile depois de deletar antigo)
			/*if(_currentSelectedTile != null)
				if(GUI.Button(new Rect((__tilesCreateDeleteRect.width * 0.5f),0,(__tilesCreateDeleteRect.width * 0.5f) - 10, __tilesCreateDeleteRect.height), "Delete", btnStyle))
					OnDeleteBtnClicked();*/
		}
		GUI.EndGroup();
		
		// SAVE/LOAD BTNS
		Rect __tilesSaveLoadRect = new Rect(5, __tilesSelectionInterfaceRect.y + __tilesSelectionInterfaceRect.height + 5, 
		                                    __tilesSelectionInterfaceRect.width - 15, Screen.height - 10 - (__tilesSelectionInterfaceRect.y + __tilesSelectionInterfaceRect.height));
		if(_areasInScreenToIgnoreMouseClick.Contains(__tilesSaveLoadRect) == false)
			_areasInScreenToIgnoreMouseClick.Add(__tilesSaveLoadRect);
		GUI.BeginGroup(__tilesSaveLoadRect);
		{
			//save
			if(GUI.Button(new Rect(0,0,(__tilesSaveLoadRect.width * 0.5f) - 10, __tilesSaveLoadRect.height), "Save", btnStyle))
				OnSaveBtnClicked();

			//load
			if(GUI.Button(new Rect((__tilesSaveLoadRect.width * 0.5f),0,(__tilesSaveLoadRect.width * 0.5f) - 10, __tilesSaveLoadRect.height), "Load", btnStyle))
				OnLoadBtnClicked();
		}
		GUI.EndGroup();


		//GROUP BTNS
		Rect __groupBtnsRect = new Rect(__tilesSelectionInterfaceRect.x + __tilesSelectionInterfaceRect.width, (Screen.height*.5f) -25, 115, 50);
		if(_selectedTiles.Count > 0)
		{
			if(_areasInScreenToIgnoreMouseClick.Contains(__groupBtnsRect) == false)
				_areasInScreenToIgnoreMouseClick.Add(__groupBtnsRect);

			GUI.BeginGroup(__groupBtnsRect);
			{
				if(GUI.Button(new Rect(5,0,110,50), "Add to group:", btnStyle))
				{
					OnAddToGroupClicked();
				}
			}
			GUI.EndGroup();
		}
		else
		{
			if(_areasInScreenToIgnoreMouseClick.Contains(__groupBtnsRect) == true)
				_areasInScreenToIgnoreMouseClick.Remove(__groupBtnsRect);
		}
	}

	private void OnSaveBtnClicked()
	{
		lockHUD = true;
		string __savePath = "";
		Action __saveAction = () =>
		{
			//SAVE STREAM
			try
			{
				using(Stream __stream = File.Create(__savePath))
				{
					Map.SaveDummy __dummy = new Map.SaveDummy();
					__dummy.loadedTiles = new List<Tile>();
					__dummy.tileGroups = _groups;
					_loadedTiles.ForEach(x => __dummy.loadedTiles.Add(x.GetTileForSerialization()));
					
					__dummy.map = new Dictionary<Map.SaveDummy.DummyVector, int>();
					_tilesOnScene.ForEach(x => __dummy.map.Add(new Map.SaveDummy.DummyVector(x.transform.position), x.tile.id));
					
					new BinaryFormatter().Serialize(__stream, __dummy);
				}
				lockHUD = false;
				onHUDLocked = null;
				ShowMessage("Saved.");
			}
			catch(Exception e)
			{
				lockHUD = false;
				onHUDLocked = null;
				ShowMessage("Failed.\n" + e.Message);
			}
		};

		if(_useDialog)
		{
			System.Windows.Forms.SaveFileDialog __saveDialog = new System.Windows.Forms.SaveFileDialog();
			__saveDialog.Title = "Select where to save the map";
			__saveDialog.Filter = "bytes|*.bytes";
			if(__saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				__savePath = __saveDialog.FileName;
				__saveAction();
			}
		}
		else
		{
			onHUDLocked = null;
			onHUDLocked += () =>
			{
				//DRAW WINDOW
				Rect __saveWindowRect = new Rect((Screen.width * 0.5f) - 200, (Screen.height*0.5f)-50, 400, 115);
				GUI.BeginGroup(__saveWindowRect);
				{
					GUI.Box(new Rect(0,0,__saveWindowRect.width, __saveWindowRect.height), "");

					//SELECT PATH
					Rect __selectSavePathRect = new Rect(15,10,750,60);
					GUI.BeginGroup(__selectSavePathRect);
					{
						GUI.Label(new Rect(0,0, 400, 30), "Enter save file name: ");
						__savePath = GUI.TextField(new Rect(0, 30, 310, 25), __savePath);

						//SELECT BTN
						if(GUI.Button(new Rect(315, 30, 50, 25), "Select"))
						{
							if(__savePath.ToLower().EndsWith(".bytes") == false)
								__savePath = __savePath + ".bytes";

							__saveAction();

							onHUDLocked = null;
							lockHUD = false;
						}
					}
					GUI.EndGroup();

					//CANCEL BTN
					if(GUI.Button(new Rect((__saveWindowRect.width * 0.5f) - 40, (__saveWindowRect.height) - 35, 80, 25), "Cancel"))
					{
						onHUDLocked = null;
						lockHUD = false;
					}

				}
				GUI.EndGroup();
			};
		}
	}

	private void OnAddToGroupClicked()
	{
		string __groupName = "";
		lockHUD = true;
		onHUDLocked = null;
		onHUDLocked += () =>
		{
			//DRAW WINDOW
			Rect __groupNameDialogRect = new Rect((Screen.width * 0.5f) - 200, (Screen.height*0.5f)-50, 400, 115);
			GUI.BeginGroup(__groupNameDialogRect);
			{
				GUI.Box(new Rect(0,0,__groupNameDialogRect.width, __groupNameDialogRect.height), "");
				
				//SELECT GROUP NAME
				Rect __selectGroupNameRect = new Rect(15,10,750,60);
				GUI.BeginGroup(__selectGroupNameRect);
				{
					GUI.Label(new Rect(0,0, 400, 30), "Enter group name: ");
					__groupName = GUI.TextField(new Rect(0, 30, 310, 25), __groupName);
					
					//SELECT BTN
					if(GUI.Button(new Rect(315, 30, 50, 25), "Select"))
					{
						//do magic
						if(_groups.ContainsKey(__groupName))
							_groups[__groupName].AddRange(_selectedTiles);
						else
							_groups.Add(__groupName, _selectedTiles);

						_selectedTiles = new List<int>();

						lockHUD = false;
						onHUDLocked = null;
					}
				}
				GUI.EndGroup();
				
				//CANCEL BTN
				if(GUI.Button(new Rect((__groupNameDialogRect.width * 0.5f) - 40, (__groupNameDialogRect.height) - 35, 80, 25), "Cancel"))
				{
					onHUDLocked = null;
					lockHUD = false;
				}
				
			}
			GUI.EndGroup();
		};
	}

	private void OnLoadBtnClicked()
	{

		string __loadPath = "";
		Action __loadAction = () =>
		{
			if(_tileIdCount == 0)
				LoadMap(__loadPath);
			else
			{
				_pathToLoad = __loadPath;
				Application.LoadLevel(Application.loadedLevel);
			}
		};

		if(_useDialog)
		{
			System.Windows.Forms.OpenFileDialog __openFile = new System.Windows.Forms.OpenFileDialog();
			__openFile.Filter = "bytes|*.bytes";
			__openFile.Multiselect = false;
			__openFile.Title = "Select map file to load";
			if(__openFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				__loadPath = __openFile.FileName;
				__loadAction();
			}
		}
		else
		{
			lockHUD = true;
			onHUDLocked = null;
			onHUDLocked += () =>
			{
				//DRAW WINDOW
				Rect __loadWindowRect = new Rect((Screen.width * 0.5f) - 200, (Screen.height*0.5f)-50, 400, 115);
				GUI.BeginGroup(__loadWindowRect);
				{
					GUI.Box(new Rect(0,0,__loadWindowRect.width, __loadWindowRect.height), "");
					
					//SELECT FILE
					Rect __selectLoadPathRect = new Rect(15,10,750,60);
					GUI.BeginGroup(__selectLoadPathRect);
					{
						GUI.Label(new Rect(0,0, 400, 30), "Enter file name: ");
						__loadPath = GUI.TextField(new Rect(0, 30, 310, 25), __loadPath);
						
						//SELECT BTN
						if(GUI.Button(new Rect(315, 30, 50, 25), "Select"))
						{
							if(__loadPath.ToLower().EndsWith(".bytes") == false)
								__loadPath = __loadPath + ".bytes";
							
							if(File.Exists(__loadPath) == false)
							{
								lockHUD = false;
								onHUDLocked = null;
								ShowMessage("Invalid file path.");
								return;
							}

							lockHUD = false;
							onHUDLocked = null;

							__loadAction();
						}
					}
					GUI.EndGroup();
					
					//CANCEL BTN
					if(GUI.Button(new Rect((__loadWindowRect.width * 0.5f) - 40, (__loadWindowRect.height) - 35, 80, 25), "Cancel"))
					{
						onHUDLocked = null;
						lockHUD = false;
					}
					
				}
				GUI.EndGroup();
			};
		}
	}

	private void OnCreateBtnClicked()
	{
		//lock hud
		lockHUD = true;

		List<string> __texturePaths = new List<string>();
		string __texturePath = "";
		Action __textureAction = () =>
		{
			System.Collections.Generic.Dictionary<WWW, Tile> __loadingTiles = new System.Collections.Generic.Dictionary<WWW, Tile>();
			__texturePaths.ForEach(x =>
			{
				Tile __newTile = new Tile();
				__newTile.name = Path.GetFileName(x);
				_tileIdCount++;
				__newTile.id = _tileIdCount;

				WWW __www = new WWW("file://" + x);
				__loadingTiles.Add(__www, __newTile);
			});

			//LOAD THE TEXTURES

			onHUDLocked = null;
			onHUDLocked += () =>
			{
				PaintLoadingWindow();

				List<WWW> __toRemove = new List<WWW>();
				foreach(var loading in __loadingTiles)
				{
					if(loading.Key.isDone == false)
						continue;

					__toRemove.Add(loading.Key);
					loading.Value.texture = loading.Key.texture;
					loading.Value.encodedTexture = loading.Value.texture.EncodeToPNG();

					_loadedTiles.Add(loading.Value);
				}

				__toRemove.ForEach(x => __loadingTiles.Remove(x));

				if(__loadingTiles.Count != 0)
					return;

				onHUDLocked = null;
				lockHUD = false;
			};
		};

		if(_useDialog)
		{
			System.Windows.Forms.OpenFileDialog __dialog = new System.Windows.Forms.OpenFileDialog();
			__dialog.Title = "Select texture to load";
			__dialog.Multiselect = true;
			__dialog.Filter = "PNG|*.png|JPG|*.jpg";
			if(__dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				__texturePaths = new List<string>(__dialog.FileNames);
				__textureAction();
			}
			else
				lockHUD = false;
		}
		else
		{
			onHUDLocked += () =>
			{
				//DRAW WINDOW
				Rect __createTileWindowRect = new Rect((Screen.width * 0.5f) - 200, (Screen.height*0.5f)-50, 400, 115);
				GUI.BeginGroup(__createTileWindowRect);
				{
					GUI.Box(new Rect(0,0,__createTileWindowRect.width, __createTileWindowRect.height), "");

					//SELECT TEXTURE
					Rect __selectTextureRect = new Rect(15, 10, 750, 60);
					GUI.BeginGroup(__selectTextureRect);
					{
						GUI.Label(new Rect(0,0, 400, 30), "Upload a texture: ");
						__texturePath = GUI.TextField(new Rect(0, 30, 310, 25), __texturePath);

						//UPLOAD BTN
						if(GUI.Button(new Rect(315, 30, 50, 25), "Upload"))
						{
							if(File.Exists(__texturePath) == false || (__texturePath.ToLower().EndsWith("png") == false && __texturePath.ToLower().EndsWith("jpg") == false))
							{
								lockHUD = false;
								onHUDLocked = null;

								ShowMessage("Invalid path or invalid file format.");

								return;
							}

							__texturePaths = new List<string>();
							__texturePaths.Add(__texturePath);
							__textureAction();

							lockHUD = false;
							onHUDLocked = null;
						}
					}
					GUI.EndGroup();

					//CANCEL BTN
					if(GUI.Button(new Rect((__createTileWindowRect.width * 0.5f) - 40, (__createTileWindowRect.height) - 35, 80, 25), "Cancel"))
					{
						onHUDLocked = null;
						lockHUD = false;
					}
				}
				GUI.EndGroup();
			};
		}
	}

	private void PaintLoadingWindow()
	{
		//WINDOW
		Rect __loadingWindowRect = new Rect((Screen.width * 0.5f) - 200, (Screen.height*0.5f)-50, 400, 115);
		GUI.BeginGroup(__loadingWindowRect);
		{
			GUI.Box(new Rect(0,0,__loadingWindowRect.width, __loadingWindowRect.height), "");
			
			GUIStyle __loadingStyle = new GUIStyle();
			__loadingStyle.normal.textColor = Color.white;
			__loadingStyle.alignment = TextAnchor.MiddleCenter;
			
			GUI.Label(new Rect(0,0, __loadingWindowRect.width, __loadingWindowRect.height), "Loading...", __loadingStyle);
		}
		GUI.EndGroup();
	}

	private void OnDeleteBtnClicked()
	{
		_loadedTiles.Remove(_currentSelectedTile);

		List<TileObject> __toDelete = new List<TileObject>();
		_tilesOnScene.ForEach(x => {if(x.tile.id == _currentSelectedTile.id) __toDelete.Add(x);});
		_currentSelectedTile = null;
		while(__toDelete.Count > 0)
		{
			var __cache = __toDelete[0];
			__toDelete.Remove(__cache);
			Destroy(__cache.gameObject);
		}
	}

	private void EditTile(Tile p_tile)
	{
		lockHUD = true;
		onHUDLocked += () =>
		{
			//DRAW WINDOW
			Rect __editTileWindowRect = new Rect((Screen.width * 0.5f) - 225, (Screen.height * 0.5f) - 150, 450, 300);
			GUI.BeginGroup(__editTileWindowRect);
			{
				GUI.Box(new Rect(0,0, __editTileWindowRect.width, __editTileWindowRect.height), "Edit tile: " + p_tile.name);

				//TYPE
				GUI.Label(new Rect(10, 30, 80, 20), "Tile type:");
				p_tile.type = (Tile.Type)GUI.SelectionGrid(new Rect(10, 55, 80, 110), (int)p_tile.type, new string[] {"VERTICAL", "BILBOARD", "GROUND"}, 1);

				//SCALE
				GUI.Label(new Rect(100, 30, 80, 20), "Scale size:");
				p_tile.scale = GUI.HorizontalSlider(new Rect(100, 55, 110, 20), p_tile.scale, 0.1f, 3);
				GUI.Label(new Rect(140, 75, 80, 20), p_tile.scale.ToString("0.00"));

				//X DESLOC
				GUI.Label(new Rect(130, 140, 80,20), "X Desloc:");
				p_tile.xDesloc = GUI.HorizontalSlider(new Rect(130, 160, 110, 20), p_tile.xDesloc, -0.5f, 0.5f);
				GUI.Label(new Rect(170, 185,80,20), p_tile.xDesloc.ToString("0.00"));

				//Y DESLOC
				GUI.Label(new Rect(250, 140, 80, 20), "Y Desloc:");
				p_tile.yDesloc = GUI.HorizontalSlider(new Rect(250, 160, 110, 20), p_tile.yDesloc, -0.5f, 0.5f);
				GUI.Label(new Rect(290, 185, 80,20), p_tile.yDesloc.ToString("0.00"));

				//CHECK LIST
				p_tile.blockCharacter = GUI.Toggle(new Rect(220, 30, 200, 20), p_tile.blockCharacter, "Block character movement");
				p_tile.blockProjectile = GUI.Toggle(new Rect(220, 55, 200, 20), p_tile.blockProjectile, "Block projectile");
				p_tile.useTransparency = GUI.Toggle(new Rect(220, 80, 200, 20), p_tile.useTransparency, "Use transparency");
				p_tile.pathable = GUI.Toggle(new Rect(220,105,200,20), p_tile.pathable, "IA Pathable");


				//APPLY BTN
				if(Input.GetKeyDown(KeyCode.Space) || GUI.Button(new Rect(__editTileWindowRect.width - 255, __editTileWindowRect.height - 40, 105, 30), "Apply [space]", btnStyle))
					p_tile.OnChange();

				//SAVE&CLOSE BTN
				if(Input.GetKeyDown(KeyCode.Return) || GUI.Button(new Rect(__editTileWindowRect.width - 145, __editTileWindowRect.height - 40, 135, 30), "Save & Close [return]", btnStyle))
				{
					p_tile.OnChange();
					lockHUD = false;
					onHUDLocked = null;
				}
			}
			GUI.EndGroup();
		};
	}

	private void ShowMessage(string p_message)
	{
		lockHUD = true;
		//lastFrame and counter will help us know when more or less 3 sec have pessed
		//so we can hide the message
		int __lastFrame = Time.frameCount -1;
		float __counter = 0;
		onHUDLocked += () =>
		{
			//WINDOW
			Rect __messageWindowRect = new Rect((Screen.width * 0.5f) - 200, (Screen.height*0.5f)-50, 400, 115);
			GUI.BeginGroup(__messageWindowRect);
			{
				GUI.Box(new Rect(0,0,__messageWindowRect.width, __messageWindowRect.height), "");

				GUIStyle __messageStyle = new GUIStyle();
				__messageStyle.normal.textColor = Color.yellow;
				__messageStyle.alignment = TextAnchor.MiddleCenter;

				GUI.Label(new Rect(0,0, __messageWindowRect.width, __messageWindowRect.height), p_message, __messageStyle);

				//we need to test if the frame has changed becouse OnGUI function is usually called 3 to 4 times per frame
				if(__lastFrame != Time.frameCount)
				{
					__lastFrame = Time.frameCount;
					__counter += Time.deltaTime;

					if(__counter >= 2.3f)
					{
						onHUDLocked = null;
						lockHUD = false;
					}
				}
			}
			GUI.EndGroup();
		};
	}
	#endregion

	#region update methods
	private void LoadMap(string p_path)
	{
		lockHUD = true;
		string __loadPath = p_path;
		Action __loadAction = () =>
		{
			//LOAD STREAM
			try
			{
				using(Stream __stream = File.Open(__loadPath, FileMode.Open))
				{
					Map.SaveDummy __dummy = (Map.SaveDummy)(new BinaryFormatter().Deserialize(__stream));
					_loadedTiles = __dummy.loadedTiles;
					_loadedTiles.ForEach(x=>
					                     {
						Texture2D __texture = new Texture2D(1,1);
						__texture.LoadImage(x.encodedTexture);
						
						x.texture = __texture;
					});
					
					foreach(var tile in __dummy.map)
					{
						TileObject __tileObj = ((GameObject)GameObject.Instantiate(tilePrefab, tile.Key.GetVector3(), tilePrefab.transform.rotation)).GetComponent<TileObject>();
						__tileObj.Initialize(_loadedTiles.Find(x => x.id == tile.Value));
						_tilesOnScene.Add(__tileObj);
						
						if(_tileIdCount < tile.Value)
							_tileIdCount = tile.Value;
					}

					if(__dummy.tileGroups != null)
						_groups = __dummy.tileGroups;
				}
				lockHUD = false;
				onHUDLocked = null;
				ShowMessage("Loaded.");
			}
			catch(Exception e)
			{
				lockHUD = false;
				onHUDLocked = null;
				ShowMessage("Failed.\n" + e.Message);
			}
		};

		__loadAction();
	}

	private void DetectMouseClickForPlacementOfTile()
	{
		if(Input.GetMouseButtonUp(0))
		{
			_placedGameObjectsAtCurrentDrag = new List<GameObject>();
			return;
		}

		Vector2 __mousePositon = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

		bool __ignoreMouseClick = false;
		_areasInScreenToIgnoreMouseClick.ForEach(x =>
		{
			if(x.Contains(__mousePositon))
				__ignoreMouseClick = true;
		});

		if(__ignoreMouseClick)
			return;

		if(Input.GetMouseButton(0))
		{
			List<TileObject> __toRemove = new List<TileObject>();
			_selectionTransforms.ForEach(x =>
			{
				Vector3 __currentSelectorPosition = x.position;
				GameObject __alreadyPlaceObjWithCurrentDrag = _placedGameObjectsAtCurrentDrag.Find(cache => 
				{
					Vector2 __cachePosition = new Vector2(cache.transform.position.x, cache.transform.position.z);
					Vector2 __selectorPosition = new Vector2(__currentSelectorPosition.x, __currentSelectorPosition.z);

					if(__cachePosition == __selectorPosition)
						return true;
					return false;
				});

				//avoid placing more of the same tile in the current drag section
				if(__alreadyPlaceObjWithCurrentDrag != null)
					return;

				List<TileObject> __tilesInThisPosition = new List<TileObject>();
				_tilesOnScene.ForEach(y =>
				{
					if(new Vector2(y.position.x, y.position.y) == new Vector2(__currentSelectorPosition.x, __currentSelectorPosition.z))
					{
						if(_currentSelectedTile == null)
							__toRemove.Add(y);
						else
							__tilesInThisPosition.Add(y);
					}
				});

				if(_currentSelectedTile != null)
				{
					GameObject __instantiated = (GameObject)GameObject.Instantiate(tilePrefab, 
					                                                               new Vector3(__currentSelectorPosition.x, __tilesInThisPosition.Count * 0.0001f, __currentSelectorPosition.z),
					                                                               tilePrefab.transform.rotation);

					_placedGameObjectsAtCurrentDrag.Add(__instantiated);
					TileObject __instatiatedTileObject = __instantiated.GetComponent<TileObject>();
					__instatiatedTileObject.Initialize(_currentSelectedTile);
					_tilesOnScene.Add(__instatiatedTileObject);
				}
			});

			__toRemove.ForEach(x =>
			{
				_tilesOnScene.Remove(x);
				Destroy(x.gameObject);
			});
		}
	}

	private void UpdateSelectionArea()
	{
		//we organize the selection are as the following:
		//we spawn one central selection, and then we check if we need to spawn more than 1
		//if we do, we spawn the other around the central one, and then we set them as chield of the central one
		//that way we have to worry only about the central one, that we know for sure will always exist
		//and then we move only that one...
		//we also save all the intances to a list, so once we detect a mouseDown in thescene (placing an object) we can place that object
		//at the same posicion of all the selection objhects in the scene =P

		if(Input.GetKeyDown("1")) selectionArea = 1;
		if(Input.GetKeyDown("2")) selectionArea = 2;
		if(Input.GetKeyDown("3")) selectionArea = 3;
		if(_lastSelectionArea != selectionArea)
		{
			//destroy all selection prefabs in the scene
			if(_selectionTransforms.Count > 0)
				Destroy(_selectionTransforms[0].gameObject);
			
			_selectionTransforms = new List<Transform>();
			
			//instantiate the central selection prefab
			GameObject __instanciated = (GameObject)GameObject.Instantiate(selectionPlanePrefab, new Vector3(0,0,0), selectionPlanePrefab.transform.rotation);
			_selectionTransforms.Add(__instanciated.transform);
			int __extrasToSpawn = selectionArea -1;
			
			//spawn all the selection prefabs that are going to follow the central one (only for 2 or +)
			for(int c = -__extrasToSpawn; c <= __extrasToSpawn; c ++)
			{
				for(int l = -__extrasToSpawn; l <= __extrasToSpawn; l++)
				{
					if(c == 0 && l ==0)
						continue;
					
					GameObject __instanciatedExtra = (GameObject)GameObject.Instantiate(selectionPlanePrefab, new Vector3(c,0,l), selectionPlanePrefab.transform.rotation);
					
					//set as chield of the central one, so we need to move only that one and now all of the them
					__instanciatedExtra.transform.parent = __instanciated.transform;
					_selectionTransforms.Add(__instanciatedExtra.transform);
				}
			}
			_lastSelectionArea = selectionArea;
		}
	}

	private void UpdateSelectionPosition()
	{
		Ray __ray = _camera.ScreenPointToRay(Input.mousePosition);
		
		//calculo para descobrir onde a linha passa pelo plano y=0, (no chao)
		Vector3 __updatedPosition = __ray.origin + (((-__ray.origin.y)/__ray.direction.y) * __ray.direction);
		
		//fixa na grade 1x1 (1m x 1m), como a grade eh 1x1 nao precisa multiplicar por nada, soh arredondar pro int mais proximo
		//como agnt espera que o plano y seja 0, nos vamos colocar um 0.001f ali (para que ele fica ligeiramente acima dos tiles ja colocados)
		__updatedPosition = new Vector3(Mathf.Round(__updatedPosition.x), 0.001f, Mathf.Round(__updatedPosition.z));
		
		_selectionTransforms[0].position = __updatedPosition;
	}

	private void CheckShortKeyInputs()
	{
		if(Input.GetKeyDown(KeyCode.Escape))
		{
			_currentSelectedTile = null;
			_selectedTiles = new List<int>();
		}

		if(Input.GetKeyDown(KeyCode.E))
			if(_currentSelectedTile != null)
				EditTile(_currentSelectedTile);
	}
#endregion
}
