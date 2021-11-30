/***
 * Created by Darcy
 * Github: https://github.com/Darcy97
 * Date: Tuesday, 30 November 2021
 * Time: 14:32:06
 * Ref: https://answers.unity.com/questions/960413/editor-window-how-to-center-a-window.html
 ***/

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EditorSpotlight
{
    public static class Extensions
    {
        private static Type GetType (this AppDomain aAppDomain, Type aType, string name)
        {
            var        assemblies   = aAppDomain.GetAssemblies ();
            return assemblies.SelectMany (assembly => assembly.GetTypes ()).FirstOrDefault (type => type.IsSubclassOf (aType) && type.Name == name);
        }

        private static Rect GetEditorMainWindowPos ()
        {
            var containerWinType =
                AppDomain.CurrentDomain.GetType (typeof (ScriptableObject), "ContainerWindow");
            if (containerWinType == null)
                throw new MissingMemberException (
                    "Can't find internal type ContainerWindow. Maybe something has changed inside Unity");
            var showModeField = containerWinType.GetField ("m_ShowMode",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var positionProperty = containerWinType.GetProperty ("position",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (showModeField == null || positionProperty == null)
                throw new MissingFieldException (
                    "Can't find internal fields 'm_ShowMode' or 'position'. Maybe something has changed inside Unity");
            var windows = Resources.FindObjectsOfTypeAll (containerWinType);
            foreach (var win in windows)
            {
                var showMode = (int) showModeField.GetValue (win);
                if (showMode != 4)
                    continue;
                var pos = (Rect) positionProperty.GetValue (win, null);
                return pos;
            }

            throw new NotSupportedException (
                "Can't find internal main window. Maybe something has changed inside Unity");
        }

        public static void CenterOnMainWin (this EditorWindow window)
        {
            var main = GetEditorMainWindowPos ();
            var pos  = window.position;
            var w    = (main.width  - pos.width)  * 0.5f;
            var h    = (main.height - pos.height) * 0.5f;
            pos.x         = main.x + w;
            pos.y         = main.y + h;
            window.position = pos;
        }

        public static void SetYPosPercentMonMainWin (this EditorWindow window, float percent)
        {
            var main = GetEditorMainWindowPos ();
            var pos  = window.position;
            var w    = (main.width  - pos.width)  * 0.5f;
            var h    = (main.height - pos.height) * percent;
            pos.x        = main.x + w;
            pos.y        = main.y + h;
            window.position = pos;
        }
    }
}