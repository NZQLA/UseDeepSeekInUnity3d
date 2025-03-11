using System;
using BaseToolsForUnity;
using UnityEngine;
using UnityEngine.UI;


namespace Business
{
    /// <summary>
    /// menu for DeepSeek chat
    /// </summary>
    public class DeepSeekChatMenuPanel : MonoBehaviour
    {
        /// <summary>
        /// on show mode changed , true: show like markdown, false: show like plain text
        /// </summary>
        public event Action<bool> OnShowModeChangedHandler;

        [SerializeField, Header("Show like markdown?")]
        protected Toggle tgShowAsMd;

        public void Awake()
        {
            tgShowAsMd.onValueChanged.AddListener(OnShowModeChanged);
        }

        protected void OnShowModeChanged(bool showLikeMd)
        {
            OnShowModeChangedHandler?.Invoke(showLikeMd);
            Log.LogAtUnityEditor($"{gameObject.name}.{this.GetType()} Show Like markdown:{showLikeMd}", "#FFFF00");
        }
    }
}