using UnityEngine;


namespace BaseToolsForUnity
{
    /// <summary>
    /// 
    /// </summary>
    public class Log : MonoBehaviour
    {
        /// <summary>
        /// Log something ,  Only work in editor !
        /// </summary>
        /// <param name="logContent"></param>
        /// <param name="color"></param>
        /// <param name="logType"></param>
        public static void LogAtUnityEditor(string logContent, string color = "white", LogType logType = LogType.Normal)
        {
            if (!Application.isEditor)
            {
                return;
            }

            var colorfulContent = AddColorForLogWithRichText(logContent, color);
            switch (logType)
            {
                case LogType.Normal:
                    Debug.Log(colorfulContent);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(colorfulContent);
                    break;
                case LogType.Error:
                    Debug.LogError(colorfulContent);
                    break;
                default:
                    break;
            }


        }

        /// <summary>
        /// Add color for log<br/>
        /// </summary>
        /// <param name="content">log content</param>
        /// <param name="colorCode">color code , like "bule" / "#AAFF00" / ... </param>
        /// <returns></returns>
        public static string AddColorForLogWithRichText(string content, string colorCode)
        {
            return $"<color={colorCode}>{content}</color>";
        }




    }

    public enum LogType
    {
        Normal,
        Warning,
        Error,
    }
}