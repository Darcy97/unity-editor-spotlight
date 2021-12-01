/***
 * Created by Darcy
 * Github: https://github.com/Darcy97
 * Date: Wednesday, 01 December 2021
 * Time: 12:00:01
 ***/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EditorSpotlight
{
    public partial class EditorSpotlight
    {

        [Serializable]
        private class SearchHistory : ISerializationCallbackReceiver
        {
            public readonly Dictionary<string, int> clicks = new Dictionary<string, int> ();

            [SerializeField] List<string> clickKeys   = new List<string> ();
            [SerializeField] List<int>    clickValues = new List<int> ();

            public void OnBeforeSerialize ()
            {
                clickKeys.Clear ();
                clickValues.Clear ();

                var i = 0;
                foreach (var pair in clicks)
                {
                    clickKeys.Add (pair.Key);
                    clickValues.Add (pair.Value);
                    i++;
                }
            }

            public void OnAfterDeserialize ()
            {
                clicks.Clear ();
                for (var i = 0; i < clickKeys.Count; i++)
                    clicks.Add (clickKeys[i], clickValues[i]);
            }
        }
    }
}