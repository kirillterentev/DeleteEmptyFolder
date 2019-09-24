using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace KillEmpty
{
	public class DeleteEmptyFolders : EditorWindow
	{
		[Serializable]
		private class Folder
		{
			public bool Toogle;
			public string Path;
		}

		private static readonly string META_POSTFIX = ".meta";

		private static List<Folder> FoundedFolders = new List<Folder>();
		private static ParallelLoopResult ResultSearchEmptyFolders;
		private static Vector2 ScrollPosition = new Vector2();
		private static bool IsSearchingComplete = false;

		[MenuItem("HelpTools/Delete Empty Folder")]
		private static void OpenWindow()
		{
			IsSearchingComplete = false;
			EditorWindow Window = GetWindow(typeof(DeleteEmptyFolders), false, "KillEmpty", false);
			Window.maxSize = new Vector2(500, 300);
			Window.minSize = new Vector2(500, 300);
			Window.Focus();
		}

		private static void FindEmptyFoldersInAssets()
		{
			string AssetsFolder = Application.dataPath;
			string[] InternalFolders = Directory.GetDirectories(AssetsFolder);
			FoundedFolders.Clear();

			ResultSearchEmptyFolders = Parallel.ForEach<string>(InternalFolders, FindEmptyFolders);
		}

		private static void FindEmptyFolders(string path)
		{
			if (IsEmpty(path))
			{
				var folder = new Folder();
				folder.Path = path;
				folder.Toogle = false;
				FoundedFolders.Add(folder);
			}
			else
			{
				string[] InternalFolders = Directory.GetDirectories(path);

				if (InternalFolders.Length < 1)
				{
					return;
				}

				foreach (string folder in InternalFolders)
				{
					FindEmptyFolders(folder);
				}
			}
		}

		private static bool IsEmpty(string path)
		{
			string[] InternalFolders = Directory.GetDirectories(path);
			string[] InternalFiles = Directory.GetFiles(path);
			return InternalFolders.Length == 0 && InternalFiles.Length == 0;
		}

		private static void DeleteFoundedFolders()
		{
			foreach (var folder in FoundedFolders)
			{
				if (folder.Toogle)
				{
					DeleteFolder(folder.Path);
				}
			}

			FindEmptyFoldersInAssets();
		}

		private static void DeleteFolder(string path)
		{
			string[] SplitNames = path.Split('\\', '/');
			string TargetFolderName = SplitNames[SplitNames.Length - 1];
			string ParentPath = Directory.GetParent(path).FullName;
			string MetaFilePath = $"{ParentPath}\\{TargetFolderName}{META_POSTFIX}";

			Directory.Delete(path);

			if (File.Exists(MetaFilePath))
			{
				File.Delete(MetaFilePath);
			}
		}

		private static void ToogleAllItems(bool toogle)
		{
			foreach (var folder in FoundedFolders)
			{
				folder.Toogle = toogle;
			}
		}

		private void OnGUI()
		{
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Search", GUILayout.Width(100)))
			{
				FindEmptyFoldersInAssets();
				IsSearchingComplete = true;
			}

			if (GUILayout.Button("Clear", GUILayout.Width(100)))
			{
				FoundedFolders.Clear();
				IsSearchingComplete = false;
			}

			EditorGUILayout.EndHorizontal();

			if (FoundedFolders.Count > 0 && ResultSearchEmptyFolders.IsCompleted)
			{
				EditorGUILayout.BeginScrollView(ScrollPosition, true, true,
					GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar,
					GUI.skin.textArea);

				foreach (var folder in FoundedFolders)
				{
					folder.Toogle = EditorGUILayout.ToggleLeft(folder.Path, folder.Toogle, GUI.skin.label);
				}

				EditorGUILayout.EndScrollView();
			}
			else
			{
				if (IsSearchingComplete && ResultSearchEmptyFolders.IsCompleted)
				{
					EditorGUILayout.TextArea("Пустых папок не найдено...", GUILayout.Width(490), GUILayout.Height(255));
				}
				else
				{
					EditorGUILayout.TextArea("", GUILayout.Width(490), GUILayout.Height(255));
				}
			}

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("All", GUILayout.Width(100)))
			{
				ToogleAllItems(true);
			}

			if (GUILayout.Button("None", GUILayout.Width(100)))
			{
				foreach (var folder in FoundedFolders)
				{
					ToogleAllItems(false);
				}
			}

			if (GUILayout.Button("Delete", GUILayout.Width(100)))
			{
				DeleteFoundedFolders();
			}

			EditorGUILayout.EndHorizontal();
		}
	}
}
