#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace KillEmpty
{
	[Serializable]
	public class Folder
	{
		public bool Toogle;
		public string Path;
		public List<Folder> InternalFolders;
	}

	public class DeleteEmptyFolders : EditorWindow
	{
		public static readonly string IGNORE_LIST_PATH =
			$"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\IgnoredFolders({Application.productName})";

		private static readonly string META_POSTFIX = ".meta";

		private static List<Folder> FoundedFolders = new List<Folder>();
		private static ParallelLoopResult ResultSearchEmptyFolders;
		private static Vector2 ScrollPositionMainList;
		private static Vector2 ScrollPositionIgnoreList;
		private static bool IsSearchingComplete = false;
		private static bool IsShowIgnoreList = false;

		[MenuItem("HelpTools/Delete Empty Folder")]
		private static void OpenWindow()
		{
			EditorWindow Window = GetWindow(typeof(DeleteEmptyFolders), false, "Kill Empty", false);

			if (IsShowIgnoreList)
			{
				Window.maxSize = new Vector2(1205, 305);
				Window.minSize = new Vector2(1205, 305);
			}
			else
			{
				Window.maxSize = new Vector2(600, 305);
				Window.minSize = new Vector2(600, 305);
			}

			Window.Focus();
		}

		private static void FindEmptyFoldersInAssets()
		{
			string AssetsFolder = Application.dataPath;
			string[] InternalFolders = Directory.GetDirectories(AssetsFolder);
			IsSearchingComplete = false;
			FoundedFolders.Clear();

			ResultSearchEmptyFolders = Parallel.ForEach<string>(InternalFolders, FindEmptyFolders);

			while (!ResultSearchEmptyFolders.IsCompleted) { }

			FilterFoundedFolders();
		}

		private static void FindEmptyFolders(string path)
		{
			CheckFolderIsEmpty(path);
		}

		private static bool CheckFolderIsEmpty(string path)
		{
			string[] InternalFolders = Directory.GetDirectories(path);
			bool flag = true;

			if (InternalFolders.Length > 0)
			{
				foreach (string folder in InternalFolders)
				{
					if (!CheckFolderIsEmpty(folder))
					{
						flag = false;
					}
				}
			}

			if (IsFilesExist(path) || !flag)
			{
				return false;
			}

			var newFolder = new Folder
			{
				Path = path,
				Toogle = false
			};
			FoundedFolders.Add(newFolder);

			return true;
		}

		private static void FilterFoundedFolders()
		{
			FoundedFolders = IgnoreList.FilterFoundedFolders(FoundedFolders);

			IsSearchingComplete = true;
		}

		private static bool IsFilesExist(string path)
		{
			string[] InternalFiles = Directory.GetFiles(path);
			
			return !InternalFiles.All(file => file.EndsWith(".meta")); 
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

		private static void IgnoreToogleFolders()
		{
			foreach (var folder in FoundedFolders)
			{
				if (folder.Toogle)
				{
					IgnoreList.AddIntoIgnoreList(folder.Path);
				}
			}

			IgnoreList.ReadIgnoreList();
			FindEmptyFoldersInAssets();
		}

		private void OnGUI()
		{
			EditorGUILayout.BeginHorizontal();

				EditorGUILayout.BeginVertical();

					EditorGUILayout.BeginHorizontal(GUILayout.Width(600));

						if (GUILayout.Button("Search", GUILayout.Width(100)))
						{
							FindEmptyFoldersInAssets();
						}

						if (GUILayout.Button("Clear", GUILayout.Width(100)))
						{
							FoundedFolders.Clear();
							IsSearchingComplete = false;
						}

						GUILayout.Space(260);

						if (!IsShowIgnoreList)
						{
							if (GUILayout.Button("Show Ignore List", GUILayout.Width(120)))
							{
								IsShowIgnoreList = true;
								IgnoreList.ReadIgnoreList();
								OpenWindow();
							}
						}
						else
						{
							if (GUILayout.Button("Hide Ignore List", GUILayout.Width(120)))
							{
								IsShowIgnoreList = false;
								OpenWindow();
							}
						}

					EditorGUILayout.EndHorizontal();

					if (FoundedFolders.Count > 0 && ResultSearchEmptyFolders.IsCompleted)
					{
						ScrollPositionMainList = EditorGUILayout.BeginScrollView(ScrollPositionMainList, true, true,
							GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar,
							GUI.skin.textArea, GUILayout.Width(595), GUILayout.Height(255));

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
							EditorGUILayout.LabelField("Пустых папок не найдено...", GUI.skin.textArea, GUILayout.Width(595), GUILayout.Height(255));
						}
						else
						{
							EditorGUILayout.LabelField(" ", GUI.skin.textArea, GUILayout.Width(595), GUILayout.Height(255));
						}
					}

					EditorGUILayout.BeginHorizontal();

						if (GUILayout.Button("All", GUILayout.Width(50)))
						{
							ToogleAllItems(true);
						}

						if (GUILayout.Button("None", GUILayout.Width(50)))
						{
							ToogleAllItems(false);
						}

						GUILayout.Space(20);

						if (GUILayout.Button("Delete", GUILayout.Width(100)))
						{
							DeleteFoundedFolders();
						}

						GUILayout.Space(20);

						if (GUILayout.Button("Ignore", GUILayout.Width(100)))
						{
							IgnoreToogleFolders();
						}

			EditorGUILayout.EndHorizontal();

				EditorGUILayout.EndVertical();

		if (!IsShowIgnoreList)
		{
			EditorGUILayout.EndHorizontal();
			return;
		}

				EditorGUILayout.BeginVertical();

					GUILayout.Space(26);

					if (IgnoreList.IgnoredFolders.Count < 1)
					{
						EditorGUILayout.LabelField(" ", GUI.skin.textArea, GUILayout.Width(595), GUILayout.Height(255));
					}
					else
					{

						ScrollPositionIgnoreList = EditorGUILayout.BeginScrollView(ScrollPositionIgnoreList, true, true,
							GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar,
							GUI.skin.textArea, GUILayout.Width(595), GUILayout.Height(255));

							foreach (var folder in IgnoreList.IgnoredFolders)
							{
								folder.Toogle = EditorGUILayout.ToggleLeft(folder.Path, folder.Toogle, GUI.skin.label);
							}

						EditorGUILayout.EndScrollView();
					}

					EditorGUILayout.BeginHorizontal();

					if (GUILayout.Button("All", GUILayout.Width(50)))
					{
						IgnoreList.ToogleAllItems(true);
					}

					if (GUILayout.Button("None", GUILayout.Width(50)))
					{
						IgnoreList.ToogleAllItems(false);
					}

					GUILayout.Space(20);

					if (GUILayout.Button("Drop", GUILayout.Width(100)))
					{
						IgnoreList.RemoveToogleItems();
						IgnoreList.WriteIgnoreList();
					}

					EditorGUILayout.EndHorizontal();
				
				EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();
		}
	}

	public class IgnoreList : EditorWindow
	{
		public static List<Folder> IgnoredFolders = new List<Folder>();

		public static void AddIntoIgnoreList(string path)
		{
			File.AppendAllText(DeleteEmptyFolders.IGNORE_LIST_PATH, $"{path}\n");
		}

		public static List<Folder> FilterFoundedFolders(List<Folder> folders)
		{
			ReadIgnoreList();

			foreach (var ignoredFolder in IgnoredFolders)
			{
				string line = ignoredFolder.Path;

				foreach (var folder in folders)
				{
					if (folder.Path == line)
					{
						folders.Remove(folder);
						break;
					}
				}
			}

			return folders;
		}

		public static void RemoveToogleItems()
		{
			foreach (var folder in IgnoredFolders)
			{
				if (folder.Toogle)
				{
					IgnoredFolders.Remove(folder);
					RemoveToogleItems();
					break;
				}
			}
		}

		public static void WriteIgnoreList()
		{
			File.WriteAllText(DeleteEmptyFolders.IGNORE_LIST_PATH, "");

			foreach (var folder in IgnoredFolders)
			{
				AddIntoIgnoreList(folder.Path);
			}

			ReadIgnoreList();
		}

		public static void ReadIgnoreList()
		{
			try
			{
				IgnoredFolders.Clear();

				using (StreamReader str = new StreamReader(DeleteEmptyFolders.IGNORE_LIST_PATH, System.Text.Encoding.Default))
				{
					string line;
					while ((line = str.ReadLine()) != null)
					{
						var folder = new Folder();
						folder.Toogle = false;
						folder.Path = line;
						IgnoredFolders.Add(folder);
					}
				}
			}
			catch (Exception e) { }
		}

		public static void ToogleAllItems(bool toogle)
		{
			foreach (var folder in IgnoredFolders)
			{
				folder.Toogle = toogle;
			}
		}
	}
}

#endif
