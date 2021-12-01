/***
 * Edited by Darcy
 * Github: https://github.com/Darcy97
 * Date: Tuesday, 30 November 2021
 * Time: 14:32:06
 ***/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorSpotlight
{
    public partial class EditorSpotlight : EditorWindow
    {

        private const string PlaceholderInput    = "Search Asset...";
        private const string SearchHistoryKey    = "SearchHistoryKey";
        private const int    BaseHeight          = 100;
        private const int    VisibleCountPerView = 6;
        private const int    ShowPage            = 10;

        [MenuItem ("Window/Spotlight/Clear History")]
        private static void ResetHistory ()
        {
            EditorPrefs.SetString (SearchHistoryKey, JsonUtility.ToJson (new SearchHistory ()));
            Debug.Log ("Clear success");
        }

        [MenuItem ("Window/Spotlight/Output History")]
        private static void OutputHistory ()
        {
            var json = EditorPrefs.GetString (SearchHistoryKey, JsonUtility.ToJson (new SearchHistory ()));
            Debug.Log (json);
        }

        [MenuItem ("Window/Spotlight/Open %k")]
        private static void Init ()
        {
            var window = CreateInstance<EditorSpotlight> ();
            window.titleContent = new GUIContent ("Spotlight");
            var pos = window.position;
            pos.height      = BaseHeight;
            pos.width       = 500;
            window.position = pos;
            window.EnforceWindowSize ();
            window.ShowUtility ();

            window.Reset ();
        }

        private void OnBecameVisible ()
        {
            LoadConfig ();
            UnLockProjectWindow ();
        }

        private static void UnLockProjectWindow ()
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

        private List<string> hits = new List<string> ();
        private string       input;
        private int          selectedIndex = 0;

        private SearchHistory _history;

        private void Reset ()
        {
            input = "";
            hits.Clear ();
            var json = EditorPrefs.GetString (SearchHistoryKey, JsonUtility.ToJson (new SearchHistory ()));
            _history = JsonUtility.FromJson<SearchHistory> (json);
            Focus ();
        }

        private void OnLostFocus ()
        {
            Close ();
        }

        private bool _autoOpenFile      = true;
        private bool _autoHighlightFile = true;

        private int _count;

        private Vector2 _scrollPos;

        private void OnGUI ()
        {
            EnforceWindowSize ();
            HandleEvents ();

            // 可能由于调用时序关系，在这里重置几次位置才能保证居中
            if (_count < 5)
            {
                this.SetYPosPercentMonMainWin (0.3f);
                _count++;
            }

            GUILayout.BeginHorizontal ();
            GUILayout.Space (15);
            GUILayout.BeginVertical ();
            GUILayout.Space (15);

            GUI.SetNextControlName ("SpotlightInput");
            var prevInput = input;
            input = GUILayout.TextField (input, Styles.InputFieldStyle, GUILayout.Height (60));
            EditorGUI.FocusTextInControl ("SpotlightInput");

            if (input != prevInput)
                ProcessInput ();

            if (selectedIndex >= hits.Count)
                selectedIndex = hits.Count - 1;
            else if (selectedIndex <= 0)
                selectedIndex = 0;

            if (string.IsNullOrEmpty (input))
                GUI.Label (GUILayoutUtility.GetLastRect (), PlaceholderInput, Styles.PlaceholderStyle);

            var rect = GUILayoutUtility.GetLastRect ();
            rect.x     += 15;
            rect.width =  30;
            var icon = AssetDatabase.LoadAssetAtPath<Texture> ("Assets/EditorSpotlight/Editor/Icon.png");
            GUI.DrawTexture (rect, icon, ScaleMode.ScaleToFit);

            GUILayout.BeginHorizontal ();
            GUILayout.Space (6);
            _autoHighlightFile = GUILayout.Toggle (_autoHighlightFile, "Highlight", GUILayout.Width (70));
            GUILayout.Space (1);
            _autoOpenFile = GUILayout.Toggle (_autoOpenFile, "Open");
            GUILayout.EndHorizontal ();

            GUILayout.BeginHorizontal ();
            GUILayout.Space (6);

            if (!string.IsNullOrEmpty (input))
                VisualizeHits ();

            GUILayout.Space (6);
            GUILayout.EndHorizontal ();

            GUILayout.Space (15);
            GUILayout.EndVertical ();
            GUILayout.Space (15);
            GUILayout.EndHorizontal ();
        }

        private void ProcessInput ()
        {
            input = input.ToLower ();
            var assetHits = AssetDatabase.FindAssets (input) ?? new string[0];
            hits = assetHits.ToList ();

            // Sort the search hits
            hits.Sort ((x, y) =>
            {
                // Generally, use click history
                int xScore;
                _history.clicks.TryGetValue (x, out xScore);
                int yScore;
                _history.clicks.TryGetValue (y, out yScore);

                // Value files that actually begin with the search input higher
                if (xScore != 0 && yScore != 0)
                {
                    var xName = Path.GetFileName (AssetDatabase.GUIDToAssetPath (x)).ToLower ();
                    var yName = Path.GetFileName (AssetDatabase.GUIDToAssetPath (y)).ToLower ();
                    if (xName.StartsWith (input) && !yName.StartsWith (input))
                        return -1;
                    if (!xName.StartsWith (input) && yName.StartsWith (input))
                        return 1;
                }

                return yScore - xScore;
            });

            hits = hits.Take (VisibleCountPerView * ShowPage).ToList ();
        }

        private void HandleEvents ()
        {
            var current = Event.current;

            if (current.type != EventType.KeyDown)
                return;

            switch (current.keyCode)
            {
                case KeyCode.UpArrow:
                {
                    current.Use ();
                    selectedIndex--;

                    if (selectedIndex < 0)
                        selectedIndex = 0;

                    AutoScroll ();
                    break;
                }
                case KeyCode.DownArrow:
                {
                    current.Use ();
                    selectedIndex++;
                    if (selectedIndex >= hits.Count)
                        selectedIndex = hits.Count - 1;

                    AutoScroll ();
                    break;
                }
                case KeyCode.Return:
                    OpenSelectedAssetAndClose (current.shift);
                    current.Use ();
                    break;
                case KeyCode.Escape:
                    Close ();
                    break;
            }
        }

        private void AutoScroll ()
        {
            var cellHeight    = EditorGUIUtility.singleLineHeight * 2;
            var index         = selectedIndex;
            var posYMin       = index * cellHeight;
            var posYMax       = posYMin + cellHeight;
            var visibleYStart = _scrollPos.y;
            var visibleYEnd   = VisibleCountPerView * cellHeight + _scrollPos.y;

            if (posYMin < visibleYStart)
            {
                _scrollPos.y -= visibleYStart - posYMin;
                if (_scrollPos.y < 0)
                    _scrollPos.y = 0;
                return;
            }

            if (posYMax > visibleYEnd)
            {
                _scrollPos.y += posYMax - visibleYEnd;
                var maxY = (hits.Count - VisibleCountPerView) * cellHeight;
                if (_scrollPos.y > maxY)
                    _scrollPos.y = maxY;
            }
        }

        private void VisualizeHits ()
        {
            var current = Event.current;

            var windowRect = position;
            windowRect.height = BaseHeight;


            GUILayout.BeginVertical ();

            GUILayout.Space (2);

            _scrollPos = EditorGUILayout.BeginScrollView (_scrollPos,
                GUILayout.Height (EditorGUIUtility.singleLineHeight * 2 * VisibleCountPerView + 10));

            GUILayout.Space (5);

            if (hits.Count == 0)
            {
                windowRect.height += EditorGUIUtility.singleLineHeight;
                GUILayout.Label ("No hits");
            }

            for (int i = 0; i < hits.Count; i++)
            {
                var style = i % 2 == 0 ? Styles.EntryOdd : Styles.EntryEven;

                GUILayout.BeginHorizontal (GUILayout.Height (EditorGUIUtility.singleLineHeight * 2),
                    GUILayout.ExpandWidth (true));

                var elementRect =
                    GUILayoutUtility.GetRect (0, 0, GUILayout.ExpandWidth (true), GUILayout.ExpandHeight (true));

                GUILayout.EndHorizontal ();

                if (i < VisibleCountPerView)
                    windowRect.height += EditorGUIUtility.singleLineHeight * 2;

                if (current.type == EventType.Repaint)
                {
                    style.Draw (elementRect, false, false, i == selectedIndex, false);
                    var assetPath = AssetDatabase.GUIDToAssetPath (hits[i]);
                    var icon      = AssetDatabase.GetCachedIcon (assetPath);

                    var iconRect = elementRect;
                    iconRect.x     = 30;
                    iconRect.width = 25;
                    GUI.DrawTexture (iconRect, icon, ScaleMode.ScaleToFit);


                    var           assetName        = Path.GetFileName (assetPath);
                    StringBuilder coloredAssetName = new StringBuilder ();

                    int start = assetName.ToLower ().IndexOf (input);
                    int end   = start + input.Length;

                    var highlightColor = EditorGUIUtility.isProSkin
                        ? Styles.proSkinHighlightColor
                        : Styles.personalSkinHighlightColor;

                    var normalColor = EditorGUIUtility.isProSkin
                        ? Styles.proSkinNormalColor
                        : Styles.personalSkinNormalColor;

                    // Sometimes the AssetDatabase finds assets without the search input in it.
                    if (start == -1)
                        coloredAssetName.Append (string.Format ("<color=#{0}>{1}</color>", normalColor, assetName));
                    else
                    {
                        if (0 != start)
                            coloredAssetName.Append (string.Format ("<color=#{0}>{1}</color>",
                                normalColor, assetName.Substring (0, start)));

                        coloredAssetName.Append (
                            string.Format ("<color=#{0}><b>{1}</b></color>", highlightColor,
                                assetName.Substring (start, end - start)));

                        if (end != assetName.Length - end)
                            coloredAssetName.Append (string.Format ("<color=#{0}>{1}</color>",
                                normalColor, assetName.Substring (end, assetName.Length - end)));
                    }

                    var labelRect = elementRect;
                    labelRect.x = 60;
                    GUI.Label (labelRect, coloredAssetName.ToString (), Styles.ResultLabelStyle);
                }

                if (current.type == EventType.MouseDown && elementRect.Contains (current.mousePosition))
                {
                    selectedIndex = i;
                    if (current.clickCount == 2)
                        OpenSelectedAssetAndClose (false);
                    else
                    {
                        Selection.activeObject = GetSelectedAsset ();
                        EditorGUIUtility.PingObject (Selection.activeGameObject);
                    }

                    Repaint ();
                }
            }

            GUILayout.Space (5);

            EditorGUILayout.EndScrollView ();

            windowRect.height += 15;
            position          =  windowRect;


            GUILayout.EndVertical ();
        }

        private void OpenSelectedAssetAndClose (bool withShift)
        {
            Close ();
            if (hits.Count <= selectedIndex) return;

            var select = GetSelectedAsset ();
            if (select == null)
            {
                return;
            }

            if (_autoOpenFile && !withShift || !_autoOpenFile && withShift)
                AssetDatabase.OpenAsset (select);

            if (_autoHighlightFile)
            {
                EditorUtility.FocusProjectWindow ();
                Selection.activeObject = select;
                EditorGUIUtility.PingObject (select);
            }

            var guid = hits[selectedIndex];
            if (!_history.clicks.ContainsKey (guid))
                _history.clicks[guid] = 0;

            _history.clicks[guid]++;
            EditorPrefs.SetString (SearchHistoryKey, JsonUtility.ToJson (_history));
        }

        private Object GetSelectedAsset ()
        {
            if (hits.Count < 1 || selectedIndex < 0)
            {
                return null;
            }

            var assetPath = AssetDatabase.GUIDToAssetPath (hits[selectedIndex]);
            return (AssetDatabase.LoadMainAssetAtPath (assetPath));
        }

        private void EnforceWindowSize ()
        {
            var pos = position;
            pos.width  = 500;
            pos.height = BaseHeight;
            position   = pos;
        }
    }
}