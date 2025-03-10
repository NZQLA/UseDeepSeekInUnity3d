using System;
using UnityEngine;
using UnityEngine.UI;

namespace Business
{
    /// <summary>
    /// 临时的聊天面板(DeepSeek)
    /// </summary>
    public class DeepSeekChatPanelTemp : MonoBehaviour
    {
        /// <summary>
        /// 事件：发送问题
        /// </summary>
        public event Action<string> OnSendQuestionHandler;

        [SerializeField, Header("聊天状态")]
        protected ChatState chatState = ChatState.Free;

        [SerializeField, Header("回复内容滚动控制")]
        protected ScrollRect rspScroll;

        [SerializeField, Header("回复内容")]
        protected Text rsp;

        [SerializeField, Header("输入框")]
        protected InputField inputQuestion;


        public void Awake()
        {
            // 监听输入框的输入事件
            inputQuestion.onValueChanged.AddListener(OnInputting);

            //监听输入框的回车事件
            inputQuestion.onEndEdit.AddListener(TrySendQuestion);
        }


        /// <summary>
        /// 输入框输入中
        /// </summary>
        /// <param name="input"></param>
        public void OnInputting(string input)
        {
            if (chatState == ChatState.WaitingRsp)
            {
                return;
            }

            chatState = ChatState.InputtingQuestion;
        }

        /// <summary>
        /// 尝试发送问题
        /// </summary>
        /// <param name="question"></param>
        public void TrySendQuestion(string question)
        {
            if (chatState == ChatState.WaitingRsp)
            {
                return;
            }

            chatState = ChatState.WaitingRsp;
            OnSendQuestionHandler?.Invoke(question);
        }


        public void SetRspContent(string content)
        {
            rsp.text = content;
        }


        public void ClearRspContent()
        {
            rsp.text = "";
        }

        public void ClearInput()
        {
            inputQuestion.text = "";
        }

        public void ScrollToBottom()
        {
            //rspScroll.ScrollToBottom();
            rspScroll.verticalNormalizedPosition = 1;
        }

        public void ReadyForNextChat()
        {
            chatState = ChatState.Free;
            ClearInput();
            //rspScroll.ScrollToTop();
        }


    }


    /// <summary>
    /// 聊天状态
    /// </summary>
    public enum ChatState
    {
        /// <summary>
        /// 空闲状态
        /// </summary>
        Free,

        /// <summary>
        /// 输入问题中
        /// </summary>
        InputtingQuestion,

        /// <summary>
        /// 等待回复中
        /// </summary>
        WaitingRsp,
    }
}