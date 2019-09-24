#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		private static readonly string IGNORE_LIST_PATH =
			$"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\IgnoredFolders({Application.productName})";

		private static List<Folder> FoundedFolders = new List<Folder>();
		private static ParallelLoopResult ResultSearchEmptyFolders;
		private static Vector2 ScrollPosition = new Vector2();
		private static bool IsSearchingComplete = false;

		[MenuItem("HelpTools/Delete Empty Folder")]
		private static void OpenWindow()
		{
			IsSearchingComplete = false;
			EditorWindow Window = GetWindow(typeof(DeleteEmptyFolders), false, "KillEmpty", false);
			Window.maxSize = new Vector2(600, 300);
			Window.minSize = new Vector2(600, 300);
			Window.Focus();
		}

		private static void FindEmptyFoldersInAssets()
		{
			string AssetsFolder = Application.dataPath;
			string[] InternalFolders = Directory.GetDirectories(AssetsFolder);
			FoundedFolders.Clear();

			ResultSearchEmptyFolders = Parallel.ForEach<string>(InternalFolders, FindEmptyFolders);

			while (!ResultSearchEmptyFolders.IsCompleted) { }

			FilterFoundedFolders();
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

		private static void FilterFoundedFolders()
		{
			try
			{
				using (StreamReader str = new StreamReader(IGNORE_LIST_PATH, System.Text.Encoding.Default))
				{
					string line;
					while ((line = str.ReadLine()) != null)
					{
						foreach (var folder in FoundedFolders)
						{
							if (folder.Path == line)
							{
								FoundedFolders.Remove(folder);
								break;
							}
						}
					}
				}
			} 
			catch (Exception e) { }

			IsSearchingComplete = true;
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

		private static void IgnoreFolder(string path)
		{
			File.AppendAllText(IGNORE_LIST_PATH, $"{path}\n" );
			FindEmptyFoldersInAssets();
		}

		private void OnGUI()
		{
			EditorGUILayout.BeginHorizontal();

				if (GUILayout.Button("Search", GUILayout.Width(100)))
				{
					FindEmptyFoldersInAssets();
				}

				if (GUILayout.Button("Clear", GUILayout.Width(100)))
				{
					FoundedFolders.Clear();
					IsSearchingComplete = false;
				}

				EditorGUILayout.Space();

				if (GUILayout.Button("Show Ignore List", GUILayout.Width(120)))
				{
					if (File.Exists(IGNORE_LIST_PATH))
					{
						Process.Start(new ProcessStartInfo("explorer.exe", " /select, " + IGNORE_LIST_PATH));
					}
			}

				if (GUILayout.Button("Clear Ignore List", GUILayout.Width(120)))
				{
					File.Delete(IGNORE_LIST_PATH);
				}

			EditorGUILayout.EndHorizontal();

			if (FoundedFolders.Count > 0 && ResultSearchEmptyFolders.IsCompleted)
			{
				EditorGUILayout.BeginScrollView(ScrollPosition, false, false,
					GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar,
					GUI.skin.textArea);

					foreach (var folder in FoundedFolders)
					{
						EditorGUILayout.BeginHorizontal();

							folder.Toogle = EditorGUILayout.ToggleLeft(folder.Path, folder.Toogle, GUI.skin.label);
							if (GUILayout.Button("Ignore", GUI.skin.box, GUILayout.Width(60)))
							{
								IgnoreFolder(folder.Path);
								break;
							}

						EditorGUILayout.EndHorizontal();
					}

				EditorGUILayout.EndScrollView();
			}
			else
			{
				if (IsSearchingComplete && ResultSearchEmptyFolders.IsCompleted)
				{
					EditorGUILayout.LabelField("Пустых папок не найдено...", GUI.skin.textArea, GUILayout.Width(595), GUILayout.Height(255));
				}
				else
				{
					EditorGUILayout.LabelField(" ", GUI.skin.textArea, GUILayout.Width(595), GUILayout.Height(255));
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

#endif
