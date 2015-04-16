
#define CONSOLE_CS

using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// A console that displays the contents of Unity's debug log.
/// </summary>
public class Console : MonoBehaviour
{
	private class Entry
	{
		public readonly string message;
		public readonly string stackTrace;
		public readonly int	type;
		public readonly DateTime time;
		
		public Entry (string p_message, string p_stackTrace, int p_type)
		{
			this.message = p_message;
			this.stackTrace	= p_stackTrace;
			this.type = p_type;
			this.time = DateTime.Now;
		}
	}

	private class CommandParser
	{
		private class Command
		{
			public readonly string command;
			public readonly Action<string> action;

			public Command(string p_command, Action<string> p_action)
			{
				this.command = p_command;
				this.action = p_action;
			}
		}

		#region public events
		public event Action<string> onCommandExecuted;
		public event Action<string> onError;
		#endregion

		#region private data
		private List<Command> _commandList = new List<Command>();
		#endregion

		public void ParseCommand(string p_command)
		{
			string[] __splitCache = p_command.Split(' ');
			Command __commandCache = _commandList.Find(x => x.command == __splitCache[0]);
			if(__commandCache == null)
			{
				if(onError != null)
					onError(__splitCache[0] + " is not a valid or registered command.");
				return;
			}

			string __parametersCache = "";
			for(int i = 1; i < __splitCache.Length; i++)
				__parametersCache += ((i != 1) ? " " : "") +__splitCache[i];

			__commandCache.action(__parametersCache);
			if(onCommandExecuted != null)
				onCommandExecuted(p_command);
		}

		public void RegisterCommand(string p_command, Action<string> p_action)
		{
			if(p_command.Contains(" "))
			{
				if(onError != null)
					onError(@"Commands can not have SPACE(' ') in them.");
				return;
			}

			if(_commandList.Find(x => x.command == p_command) != null)
			{
				if(onError != null)
					onError(p_command + " already exists and cannot be registered again.");

				return;
			}

			_commandList.Add(new Command(p_command, p_action));
		}
	}

	#region singleton
	private static Console _instance;
	#endregion
	
	#region private data
	private CommandParser _commandParser = new CommandParser();

	private List<Entry> _entryList = new List<Entry>();
	private Vector2 _scrollPos;
	private bool _show = false;
	private bool _collapse = false;

	private bool _enabled = true;
	private Rect _windowPosition;

	private string _commandCache = "";
	#endregion

	public static void Show()
	{
		_instance._show = true;
	}

	public static void Hide()
	{
		_instance._show = false;
	}

	public static void RegisterCommand(string p_command, Action p_action)
	{
		RegisterCommand(p_command, (p_null) => p_action());
	}

	public static void RegisterCommand(string p_command, Action<string> p_action)
	{
		_instance._commandParser.RegisterCommand(p_command, p_action);
	}

	void Awake()
	{
		if(_instance != null)
			GameObject.Destroy(_instance.gameObject);

		_instance = this;

		Application.logMessageReceived += (p_message, p_stackTrace, p_type) => 
		{
			if(_enabled == false)
				return;

			//force the console to show the new line
			_scrollPos = new Vector2(_scrollPos.x, Mathf.Infinity);

			Entry __entry = new Entry(p_message, p_stackTrace, (int)p_type);
			_entryList.Add(__entry);

			//limits the coutsize to a maximun number of entries
			if(_entryList.Count > 40)
				_entryList.RemoveAt(0);
		}; 

		_commandParser.onCommandExecuted += (p_command) => 
		{
			_scrollPos = new Vector2(_scrollPos.x, Mathf.Infinity);
			Entry __entry = new Entry(">> " + p_command + " executed.", "", -1);
			_entryList.Add(__entry);
		};

		_commandParser.onError += (p_error) => 
		{
			_scrollPos = new Vector2(_scrollPos.x, Mathf.Infinity);
			Entry __entry = new Entry(":: " + p_error, "", -1);
			_entryList.Add(__entry);
		};

		RegisterCommand("close", () => _show = false);
	}

	void OnEnable () 
	{ 
		_enabled = true;
	}

	void OnDisable () 
	{ 
		_enabled = false;
	}
	
	void Update ()
	{
		_windowPosition = new Rect(0, 0, Screen.width, Screen.height * 0.4f);

		if (Input.GetKeyDown("\\")) 
			_show = !_show;
	}
	
	void OnGUI ()
	{
		if (_show == false)
			return;

		if (Event.current != null && Event.current.isKey && Event.current.keyCode == KeyCode.Return)
		{
			GUI.FocusControl("COMMAND_TEXT_FIELD_CONSOLE");

			if(_commandCache != "")
				RunCommand();
		}

		GUILayout.Window(718931423, _windowPosition, Window, "Console");
	}

	private void Window(int p_id)
	{
		_scrollPos = GUILayout.BeginScrollView(_scrollPos);
		for (int i = 0; i < _entryList.Count; i++) 
		{
			Entry __entry = _entryList[i];

			if (_collapse && i > 0 && __entry.message == _entryList[i - 1].message) 
				continue;

			if(__entry.type != -1)
			{
				switch ((LogType)__entry.type) 
				{
				case LogType.Assert:
				case LogType.Error:
				case LogType.Exception:
					GUI.contentColor = Color.red;
					break;
					
				case LogType.Warning:
					GUI.contentColor = Color.yellow;
					break;
					
				default:
					GUI.contentColor = Color.white;
					break;
				}
			}
			else
			{
				GUI.contentColor = Color.gray;
			}
			GUILayout.Label("[" + __entry.time.Hour.ToString("00") + ":" + __entry.time.Minute.ToString("00") + ":" + __entry.time.Second.ToString("00") + "] " + __entry.message);
		}
		GUILayout.EndScrollView();

		GUILayout.BeginHorizontal();
		{
			GUI.contentColor = Color.white;
			DateTime __cacheTime = DateTime.Now;
			GUILayout.Label("[" + __cacheTime.Hour.ToString("00") + ":" + __cacheTime.Minute.ToString("00") + ":" + __cacheTime.Second.ToString("00") + "]", GUILayout.ExpandWidth(false));

			GUI.SetNextControlName("COMMAND_TEXT_FIELD_CONSOLE");
			_commandCache = GUILayout.TextField(_commandCache);

			if (GUILayout.Button(new GUIContent("Run", "Run the commands entered in the text field."), GUILayout.ExpandWidth(false)) && _commandCache != "")
				RunCommand();

			if (GUILayout.Button(new GUIContent("Clear entries", "Clear the contents of the console."), GUILayout.ExpandWidth(false)))
			{
				_entryList.Clear();
			}

			bool __cacheColapse = _collapse;
			_collapse = GUILayout.Toggle(_collapse, new GUIContent("Collapse", "Hide repeated messages."), GUILayout.ExpandWidth(false));
			if(__cacheColapse != _collapse)
				_scrollPos = new Vector2(_scrollPos.x, Mathf.Infinity);
		}
		GUILayout.EndHorizontal();
	}

	private void RunCommand()
	{
		string __cache = _commandCache;
		_commandCache = "";
		_commandParser.ParseCommand(__cache);
	}
}
