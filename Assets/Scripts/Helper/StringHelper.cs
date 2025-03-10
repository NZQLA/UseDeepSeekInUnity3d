using System.Collections;
using UnityEngine;


namespace BaseToolsForUnity
{
    /// <summary>
    /// Some  helper  methods   for string .
    /// </summary>
    public static class StringHelper
    {

        public static bool IsNullOrEmpty(this string self)
        {
            return self == null || string.IsNullOrEmpty(self);
        }

    }
}