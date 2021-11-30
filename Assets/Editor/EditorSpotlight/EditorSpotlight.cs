﻿/***
 * Edited by Darcy
 * Github: https://github.com/Darcy97
 * Date: Tuesday, 30 November 2021
 * Time: 14:32:06
 ***/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Editor.EditorSpotlight
{
    public class EditorSpotlight : EditorWindow, IHasCustomMenu
    {
        private static class Styles
        {
            public static readonly GUIStyle inputFieldStyle;
            public static readonly GUIStyle placeholderStyle;
            public static readonly GUIStyle resultLabelStyle;
            public static readonly GUIStyle entryEven;
            public static readonly GUIStyle entryOdd;

            public static readonly string proSkinHighlightColor = "eeeeee";
            public static readonly string proSkinNormalColor    = "cccccc";

            public static readonly string personalSkinHighlightColor = "eeeeee";
            public static readonly string personalSkinNormalColor    = "222222";

            static Styles()
            {
                inputFieldStyle = new GUIStyle(EditorStyles.textField)
                                  {
                                      contentOffset = new Vector2(10, 10),
                                      fontSize      = 32,
                                      focused       = new GUIStyleState()
                                  };

                placeholderStyle = new GUIStyle(inputFieldStyle) {normal =
                                                                 {
                                                                     textColor = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, .2f) : new Color(.2f, .2f, .2f, .4f)
                                                                 }};


                resultLabelStyle = new GUIStyle(EditorStyles.largeLabel)
                                   {
                                       alignment = TextAnchor.MiddleLeft,
                                       richText  = true
                                   };

                entryOdd  = new GUIStyle("CN EntryBackOdd");
                entryEven = new GUIStyle("CN EntryBackEven");
            }
        }

        [MenuItem("Window/Spotlight %k")]
        private static void Init()
        {
            var window = CreateInstance<EditorSpotlight>();
            window.titleContent = new GUIContent("Spotlight");
            var pos = window.position;
            pos.height = BaseHeight;
            pos.width  = 500;
            // pos.xMin = Screen.currentResolution.width / 2 - 500 / 2;
            // pos.yMin = Screen.currentResolution.height * .3f;
            window.position = pos;
            window.EnforceWindowSize();
            window.CenterOnMainWin ();
            window.ShowUtility();
            window.CenterOnMainWin ();

            window.Reset();
        }
    
        private void OnBecameVisible ()
        {
            LoadConfig ();
            UnLockProjectWindow ();
        }

        private void UnLockProjectWindow ()
        {
            var typeInspector = typeof (EditorWindow).Assembly.GetType ("UnityEditor.ProjectBrowser");
            var w             = focusedWindow;
            if (!typeInspector.IsInstanceOfType (w))
            {
                w = null;
                var objs               = Resources.FindObjectsOfTypeAll (typeInspector);
                if (objs.Length > 0) w = objs[0] as EditorWindow;
            }

            if (w == null)
                return;
            var propertyInfo = typeInspector.GetProperty ("isLocked", BindingFlags.Instance | BindingFlags.NonPublic);
            if (propertyInfo == null)
                return;
            propertyInfo.SetValue (w, false, null);
            w.Repaint ();
        }

        private void OnDestroy ()
        {
            EditorPrefs.SetBool ("EditorSpotLightAutoHighlight", _autoHighlightFile);
            EditorPrefs.SetBool ("EditorSpotLightAutoOpenFile",  _autoOpenFile);

            EditorPrefs.SetFloat ("EditorSpotLightPosX", position.x);
            EditorPrefs.SetFloat ("EditorSpotLightPosY", position.y);
        }

        private void LoadConfig ()
        {
            _autoHighlightFile = EditorPrefs.GetBool ("EditorSpotLightAutoHighlight", true);
            _autoOpenFile      = EditorPrefs.GetBool ("EditorSpotLightAutoOpenFile",  true);
        }


        [Serializable] private class SearchHistory : ISerializationCallbackReceiver
        {
            public readonly Dictionary<string, int> clicks = new Dictionary<string, int>();

            [SerializeField] List<string> clickKeys   = new List<string>();
            [SerializeField] List<int>    clickValues = new List<int>();

            public void OnBeforeSerialize()
            {
                clickKeys.Clear();
                clickValues.Clear();

                int i = 0;
                foreach (var pair in clicks)
                {
                    clickKeys.Add(pair.Key);
                    clickValues.Add(pair.Value);
                    i++;
                }
            }

            public void OnAfterDeserialize()
            {
                clicks.Clear();
                for (var i = 0; i < clickKeys.Count; i++)
                    clicks.Add(clickKeys[i], clickValues[i]);
            }
        }

        const        string PlaceholderInput = "Search Asset...";
        const        string SearchHistoryKey = "SearchHistoryKey";
        public const int    BaseHeight       = 100;

        List<string> hits = new List<string>();
        string       input;
        int          selectedIndex = 0;

        SearchHistory history;

        void Reset()
        {
            input = "";
            hits.Clear();
            var json = EditorPrefs.GetString(SearchHistoryKey, JsonUtility.ToJson(new SearchHistory()));
            history = JsonUtility.FromJson<SearchHistory>(json);
            Focus();
        }

        void OnLostFocus()
        {
            Close();
        }
    
        private bool _autoOpenFile      = true;
        private bool _autoHighlightFile = true;

        private int _count;

        void OnGUI()
        {
            EnforceWindowSize();
            HandleEvents();

            // 可能由于调用时序关系，在这里重置几次位置才能保证居中
            if (_count < 5)
            {
                this.CenterOnMainWin ();
                _count++;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(15);
            GUILayout.BeginVertical();
            GUILayout.Space(15);

            GUI.SetNextControlName("SpotlightInput");
            var prevInput = input;
            input = GUILayout.TextField(input, Styles.inputFieldStyle, GUILayout.Height(60));
            EditorGUI.FocusTextInControl("SpotlightInput");

            if (input != prevInput)
                ProcessInput();

            if (selectedIndex >= hits.Count)
                selectedIndex = hits.Count - 1;
            else if (selectedIndex <= 0)
                selectedIndex = 0;

            if (string.IsNullOrEmpty(input))
                GUI.Label(GUILayoutUtility.GetLastRect(), PlaceholderInput, Styles.placeholderStyle);
        
            GUILayout.BeginHorizontal ();
            _autoHighlightFile = GUILayout.Toggle (_autoHighlightFile, "Highlight", GUILayout.Width (70));
            GUILayout.Space (1);
            _autoOpenFile = GUILayout.Toggle (_autoOpenFile, "Open");
            GUILayout.EndHorizontal ();

            GUILayout.BeginHorizontal();
            GUILayout.Space(6);

            if (!string.IsNullOrEmpty(input))
                VisualizeHits();

            GUILayout.Space(6);
            GUILayout.EndHorizontal();

            GUILayout.Space(15);
            GUILayout.EndVertical();
            GUILayout.Space(15);
            GUILayout.EndHorizontal();
        }

        void ProcessInput()
        {
            input = input.ToLower();
            var assetHits = AssetDatabase.FindAssets(input) ?? new string[0];
            hits = assetHits.ToList();

            // Sort the search hits
            hits.Sort((x, y) =>
            {
                // Generally, use click history
                int xScore;
                history.clicks.TryGetValue(x, out xScore);
                int yScore;
                history.clicks.TryGetValue(y, out yScore);

                // Value files that actually begin with the search input higher
                if (xScore != 0 && yScore != 0)
                {
                    var xName = Path.GetFileName(AssetDatabase.GUIDToAssetPath(x)).ToLower();
                    var yName = Path.GetFileName(AssetDatabase.GUIDToAssetPath(y)).ToLower();
                    if (xName.StartsWith(input) && !yName.StartsWith(input))
                        return -1;
                    if (!xName.StartsWith(input) && yName.StartsWith(input))
                        return 1;
                }

                return yScore - xScore;
            });

            hits = hits.Take(10).ToList();
        }

        void HandleEvents()
        {
            var current = Event.current;

            if (current.type == EventType.KeyDown)
            {
                if (current.keyCode == KeyCode.UpArrow)
                {
                    current.Use();
                    selectedIndex--;
                }
                else if (current.keyCode == KeyCode.DownArrow)
                {
                    current.Use();
                    selectedIndex++;
                }
                else if (current.keyCode == KeyCode.Return)
                {
                    OpenSelectedAssetAndClose();
                    current.Use();
                }
                else if (Event.current.keyCode == KeyCode.Escape)
                    Close();
            }
        }

        void VisualizeHits()
        {
            var current = Event.current;

            var windowRect = this.position;
            windowRect.height = BaseHeight;

            GUILayout.BeginVertical();
            GUILayout.Space(5);

            if (hits.Count == 0)
            {
                windowRect.height += EditorGUIUtility.singleLineHeight;
                GUILayout.Label("No hits");
            }

            for (int i = 0; i < hits.Count; i++)
            {
                var style = i % 2 == 0 ? Styles.entryOdd : Styles.entryEven;

                GUILayout.BeginHorizontal(GUILayout.Height(EditorGUIUtility.singleLineHeight * 2),
                    GUILayout.ExpandWidth(true));

                var elementRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                GUILayout.EndHorizontal();

                windowRect.height += EditorGUIUtility.singleLineHeight * 2;

                if (current.type == EventType.Repaint)
                {
                    style.Draw(elementRect, false, false, i == selectedIndex, false);
                    var assetPath = AssetDatabase.GUIDToAssetPath(hits[i]);
                    var icon      = AssetDatabase.GetCachedIcon(assetPath);

                    var iconRect = elementRect;
                    iconRect.x     = 30;
                    iconRect.width = 25;
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);


                    var           assetName        = Path.GetFileName(assetPath);
                    StringBuilder coloredAssetName = new StringBuilder();

                    int start = assetName.ToLower().IndexOf(input);
                    int end   = start + input.Length;

                    var highlightColor = EditorGUIUtility.isProSkin
                        ? Styles.proSkinHighlightColor
                        : Styles.personalSkinHighlightColor;

                    var normalColor = EditorGUIUtility.isProSkin
                        ? Styles.proSkinNormalColor
                        : Styles.personalSkinNormalColor;

                    // Sometimes the AssetDatabase finds assets without the search input in it.
                    if (start == -1)
                        coloredAssetName.Append(string.Format("<color=#{0}>{1}</color>", normalColor, assetName));
                    else
                    {
                        if (0 != start)
                            coloredAssetName.Append(string.Format("<color=#{0}>{1}</color>",
                                normalColor, assetName.Substring(0, start)));

                        coloredAssetName.Append(
                            string.Format("<color=#{0}><b>{1}</b></color>", highlightColor, assetName.Substring(start, end - start)));

                        if (end != assetName.Length - end)
                            coloredAssetName.Append(string.Format("<color=#{0}>{1}</color>",
                                normalColor, assetName.Substring(end, assetName.Length - end)));
                    }

                    var labelRect = elementRect;
                    labelRect.x = 60;
                    GUI.Label(labelRect, coloredAssetName.ToString(), Styles.resultLabelStyle);
                }

                if (current.type == EventType.MouseDown && elementRect.Contains(current.mousePosition))
                {
                    selectedIndex = i;
                    if (current.clickCount == 2)
                        OpenSelectedAssetAndClose();
                    else
                    {
                        Selection.activeObject = GetSelectedAsset();
                        EditorGUIUtility.PingObject(Selection.activeGameObject);
                    }

                    Repaint();
                }
            }

            windowRect.height += 5;
            position          =  windowRect;

            GUILayout.EndVertical();
        }

        void OpenSelectedAssetAndClose()
        {
            Close();
            if (hits.Count <= selectedIndex) return;
        
            var select = GetSelectedAsset ();
            if (select == null)
            {
                return;
            }

            if (_autoOpenFile)
                AssetDatabase.OpenAsset (select);

            if (_autoHighlightFile)
            {
                EditorUtility.FocusProjectWindow ();
                Selection.activeObject = select;
                EditorGUIUtility.PingObject (select);
            }

            var guid = hits[selectedIndex];
            if (!history.clicks.ContainsKey(guid))
                history.clicks[guid] = 0;

            history.clicks[guid]++;
            EditorPrefs.SetString(SearchHistoryKey, JsonUtility.ToJson(history));
        }

        UnityEngine.Object GetSelectedAsset()
        {
            if (hits.Count < 1 || selectedIndex < 0)
            {
                return null;
            }
        
            var assetPath = AssetDatabase.GUIDToAssetPath(hits[selectedIndex]);
            return (AssetDatabase.LoadMainAssetAtPath(assetPath));
        }

        public void EnforceWindowSize()
        {
            var pos = position;
            pos.width  = 500;
            pos.height = BaseHeight;
            position   = pos;
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Reset history"), false, () =>
            {
                EditorPrefs.SetString(SearchHistoryKey, JsonUtility.ToJson(new SearchHistory()));
                Reset();
            });

            menu.AddItem(new GUIContent("Output history"), false, () =>
            {
                var json = EditorPrefs.GetString(SearchHistoryKey, JsonUtility.ToJson(new SearchHistory()));
                Debug.Log(json);
            });
        }
    }
}
