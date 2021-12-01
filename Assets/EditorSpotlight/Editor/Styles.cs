/***
 * Created by Darcy
 * Github: https://github.com/Darcy97
 * Date: Wednesday, 01 December 2021
 * Time: 11:57:53
 ***/

using UnityEditor;
using UnityEngine;

namespace EditorSpotlight
{
    public partial class EditorSpotlight
    {
        private static class Styles
        {
            public static readonly GUIStyle InputFieldStyle;
            public static readonly GUIStyle PlaceholderStyle;
            public static readonly GUIStyle ResultLabelStyle;
            public static readonly GUIStyle EntryEven;
            public static readonly GUIStyle EntryOdd;

            public static readonly string proSkinHighlightColor = "eeeeee";
            public static readonly string proSkinNormalColor    = "cccccc";

            public static readonly string personalSkinHighlightColor = "eeeeee";
            public static readonly string personalSkinNormalColor    = "222222";

            static Styles()
            {
                InputFieldStyle = new GUIStyle(EditorStyles.textField)
                                  {
                                      contentOffset = new Vector2(54, 10),
                                      fontSize      = 32,
                                      focused       = new GUIStyleState()
                                  };

                PlaceholderStyle = new GUIStyle(InputFieldStyle) {normal =
                                                                 {
                                                                     textColor = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, .2f) : new Color(.2f, .2f, .2f, .4f)
                                                                 }};


                ResultLabelStyle = new GUIStyle(EditorStyles.largeLabel)
                                   {
                                       alignment = TextAnchor.MiddleLeft,
                                       richText  = true
                                   };

                EntryOdd  = new GUIStyle("CN EntryBackOdd");
                EntryEven = new GUIStyle("CN EntryBackEven");
            }
        }
    }
}