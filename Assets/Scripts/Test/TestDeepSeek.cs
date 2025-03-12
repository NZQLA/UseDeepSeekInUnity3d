using System.Collections;
using UnityEngine;


namespace Template
{
    /// <summary>
    /// 
    /// </summary>
    public class TestDeepSeek : MonoBehaviour
    {
        public DeepSeekDialogueManager deepSeekDialogueManager;


        [ContextMenuItem("TestOnGetStreamContent", "OnGetStreamContent")]
        public string streamContent = "[DONE]";

        [ContextMenu("OnGetStreamContent")]
        public void OnGetStreamContent()
        {
            deepSeekDialogueManager.OnGetStreamContent(streamContent);
        }
    }
}