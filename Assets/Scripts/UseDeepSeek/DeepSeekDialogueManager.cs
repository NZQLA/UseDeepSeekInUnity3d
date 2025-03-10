using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using BaseToolsForUnity;
using Business;
using LitJson;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Deepseek对话管理器
/// </summary>
public class DeepSeekDialogueManager : MonoBehaviour
{
    [SerializeField, Header("聊天面板")]
    protected DeepSeekChatPanelTemp panelChat;

    [SerializeField, Header("使用流式获取回复？")]
    protected bool useStream = true;


    [SerializeField, Header("发送问题时 清理上一次的回复内容？")]
    protected bool clearRspOnSendQuestion = true;

    [SerializeField, Header("用于接收回复的内存")]
    protected long bufferSize = 1024 * 20;

    /// <summary>
    /// 当收到的回复有更新时
    /// </summary>
    public event Action<string> OnRspRefreshedHandler;

    /// <summary>
    /// 当回复完成时
    /// </summary>
    public event Action OnRspCompletedHandler;

    // API配置
    [Header("API Settings")]
    [SerializeField]
    //private string apiKey = "在此处填入你申请的API密钥";  // DeepSeek API密钥
    private string apiKey = "sk-38bd3a635ad2459e85601376800f3a69";  // DeepSeek API密钥

    [SerializeField]
    private string modelName = "deepseek-chat";  // 使用的模型名称

    [SerializeField]
    private string apiUrl = "https://api.deepseek.com/v1/chat/completions"; // API请求地址

    [SerializeField]
    protected bool convertRspData = false; // 是否转换响应数据


    [SerializeField]
    protected string rspRawContent = ""; // 响应数据


    [SerializeField]
    protected string rspContent = "";


    [SerializeField, Header("返回的内容")]
    protected DeepSeekRspData rspData = new DeepSeekRspData { };

    [SerializeField]
    protected byte[] rspBuffer = new byte[1024 * 20];

    [SerializeField]
    protected StreamedDownloadHandler streamedDownloadHandler;

    [SerializeField]
    protected JsonData rspJsonObj;


    // 对话参数
    [Header("Dialogue Settings")]
    [Range(0, 2)]
    public float temperature = 0.7f; // 控制生成文本的随机性（0-2，值越高越随机）   
    [Range(1, 1000)]
    public int maxTokens = 150;// 生成的最大令牌数（控制回复长度）
    // 角色设定
    [System.Serializable]
    public class NPCCharacter
    {
        public string name;
        [TextArea(3, 10)]
        public string personalityPrompt = "你是虚拟人物Unity-Chan，是个性格活泼，聪明可爱的女生。擅长Unity和C#编程知识。"; // 角色设定提示词
    }
    [SerializeField]
    public NPCCharacter npcCharacter;
    // 回调委托，用于异步处理API响应
    public delegate void DialogueCallback(string response, bool isSuccess);



    public void Start()
    {
        panelChat.OnSendQuestionHandler += SendQuestion;
    }

    public void Update()
    {
        //if (convertRspData)
        //{
        //    RefreshRspData();
        //}


    }

    /// <summary>
    /// 发送问题
    /// </summary>
    /// <param name="question"></param>
    public void SendQuestion(string question)
    {
        if (question.IsNullOrEmpty())
        {
            Debug.Log("输入内容为空！");
            return;
        }

        if (clearRspOnSendQuestion)
        {
            rspRawContent = "";
            panelChat.SetRspContent(rspRawContent);
        }


        if (useStream)
        {
            //  重新分配内存
            rspBuffer = new byte[bufferSize];
            rspContent = "";
            rspData.Clear();

            TestChatWithDeepSeekWithStream(question);
        }
        else
        {
            TestChatWithDeepSeek(question);
        }

    }

    public void OnRspCompleted()
    {
        OnRspCompletedHandler?.Invoke();
        panelChat.ReadyForNextChat();
    }

    /// <summary>
    /// 刷新来自 stream 方式 收到的数据
    /// </summary>
    public void RefreshStreamRspData()
    {
        rspRawContent = Encoding.UTF8.GetString(rspBuffer);
        // 只要第一行  
        var indexStart = rspRawContent.IndexOf(":") + 1;
        var indexEnd = rspRawContent.IndexOf("\n");
        var length = indexEnd - indexStart;
        rspRawContent = rspRawContent.Substring(indexStart, length);

        //rspRawContent = $"{{{rspRawContent}}}";
        //rspJsonObj = JsonUtility.FromJson(rspContent, typeof(object));
        //Log.LogAtUnityEditor($"Receive stream data: {rspRawContent}");
        try
        {
            rspJsonObj = JsonMapper.ToObject(rspRawContent);
            var dataFirst = rspJsonObj["choices"][0]["delta"];
            var reasoning_content = dataFirst["reasoning_content"];
            var content = dataFirst["content"];

            if (reasoning_content != null)
            {
                rspData.AppendReason(reasoning_content.ToString());
            }

            if (content != null)
            {
                rspData.AppendContent(content.ToString());
            }

            rspContent = rspData.ToString();
            panelChat.SetRspContent(rspContent);
            panelChat.ScrollToBottom();
            OnRspRefreshedHandler?.Invoke(rspRawContent);
        }
        catch (Exception ex)
        {
            Log.LogAtUnityEditor($"{ex.Message} RawJson:{rspRawContent}", "red", BaseToolsForUnity.LogType.Error);
        }



    }

    /// <summary>
    /// 当接收到 以 stream 方式 收到的数据时
    /// </summary>
    /// <param name="data"></param>
    /// <param name="len"></param>
    public void ReceiveData(byte[] data, int len)
    {
        // 将数据存入 rspBuffer
        System.Buffer.BlockCopy(data, 0, rspBuffer, 0, len);
        RefreshStreamRspData();

    }

    public void TestChatWithDeepSeek(string yourWords)
    {
        if (yourWords.IsNullOrEmpty())
        {
            Debug.Log("输入内容为空！");
            return;
        }

        SendDialogueRequest(yourWords, (response, isSuccess) =>
        {
            if (isSuccess)
            {
                Log.LogAtUnityEditor($"{gameObject.name}.{this.GetType()} 请求成功!!!  ", "#AAFF00");
                Log.LogAtUnityEditor($"{gameObject.name}.{this.GetType()} 回复内容：{response}  ", "#EEEEEE");

            }
            else
            {
                Log.LogAtUnityEditor($"{gameObject.name}.{this.GetType()} 请求失败!!!  ", "#FF0000");
            }
            panelChat.SetRspContent(response);
            OnRspCompleted();
        });

    }


    public void TestChatWithDeepSeekWithStream(string yourWords)
    {
        if (yourWords.IsNullOrEmpty())
        {
            Debug.Log("输入内容为空！");
            return;
        }

        SendDialogueRequestWithStream(yourWords, (response, isSuccess) =>
        {
            if (isSuccess)
            {
                Log.LogAtUnityEditor($"{gameObject.name}.{this.GetType()} 请求成功!!!", "#AAFF00");
                Log.LogAtUnityEditor($"{gameObject.name}.{this.GetType()} 回复内容：{response}  ", "#EEEEEE");


            }
            else
            {
                Log.LogAtUnityEditor($"{gameObject.name}.{this.GetType()} 请求失败!!!  ", "#FF0000");
            }
            //panelChat.SetRspContent(response);
            OnRspCompleted();
        });

    }


    /// <summary>    
    /// 发送对话请求    
    /// </summary>    
    /// <param name="userMessage">玩家的输入内容</param> 
    /// <param name="callback">回调函数，用于处理API响应</param>  
    public void SendDialogueRequest(string userMessage, DialogueCallback callback)
    {
        StartCoroutine(ProcessDialogueRequest(userMessage, callback));
    }



    /// <summary>    
    /// 发送对话请求    
    /// </summary>    
    /// <param name="userMessage">玩家的输入内容</param> 
    /// <param name="callback">回调函数，用于处理API响应</param>  
    public void SendDialogueRequestWithStream(string userMessage, DialogueCallback callback)
    {
        StartCoroutine(ProcessDialogueRequestWithStream(userMessage, callback));
    }


    /// <summary>  
    /// 处理对话请求的协程   
    /// </summary>  
    /// <param name="userInput">玩家的输入内容</param> 
    /// <param name="callback">回调函数，用于处理API响应</param>  
    public IEnumerator ProcessDialogueRequest(string userInput, DialogueCallback callback)
    {
        // 构建消息列表，包含系统提示和用户输入
        List<Message> messages = new List<Message>
        {
            //new Message { role = "system", content = npcCharacter.personalityPrompt },// 系统角色设定
            new Message { role = "user", content = userInput }// 用户输入
        };
        // 构建请求体
        ChatRequest requestBody = new ChatRequest
        {
            model = modelName,// 模型名称
            messages = messages,// 消息列表
            temperature = temperature,// 温度参数
            max_tokens = maxTokens// 最大令牌数
        };
        string jsonBody = JsonUtility.ToJson(requestBody);
        Debug.Log("Sending JSON: " + jsonBody); // 调试用，打印发送的JSON数据
        UnityWebRequest request = CreateWebRequest(jsonBody);
        yield return request.SendWebRequest();
        if (IsRequestError(request))
        {
            Debug.LogError($"API Error: {request.responseCode} --- {request.error}    \n{request.downloadHandler.text}");
            //callback?.Invoke(null, false);
            callback?.Invoke(request.error, false);
            yield break;
        }

        DeepSeekResponse response = ParseResponse(request.downloadHandler.text);
        if (response != null && response.choices.Length > 0)
        {
            string npcReply = response.choices[0].message.content;
            callback?.Invoke(npcReply, true);
        }
        else
        {
            callback?.Invoke(name + "（陷入沉默）", false);
        }
    }



    /// <summary>  
    /// 处理对话请求的协程   
    /// </summary>  
    /// <param name="userInput">玩家的输入内容</param> 
    /// <param name="callback">回调函数，用于处理API响应</param>  
    public IEnumerator ProcessDialogueRequestWithStream(string userInput, DialogueCallback callback)
    {

        // 构建消息列表，包含系统提示和用户输入
        List<Message> messages = new List<Message>
        {
            //new Message { role = "system", content = npcCharacter.personalityPrompt },// 系统角色设定
            new Message { role = "user", content = userInput }// 用户输入
        };
        // 构建请求体
        ChatRequest requestBody = new ChatRequest
        {
            model = modelName,// 模型名称
            messages = messages,// 消息列表
            temperature = temperature,// 温度参数
            max_tokens = maxTokens,// 最大令牌数
            stream = true
        };
        string jsonBody = JsonUtility.ToJson(requestBody);
        Debug.Log("Sending JSON: " + jsonBody); // 调试用，打印发送的JSON数据
        UnityWebRequest request = CreateWebRequest(jsonBody);


        //rspBuffer = new byte[1024 * 4];
        //rspBuffer = new byte[1024 * 20];
        streamedDownloadHandler = new StreamedDownloadHandler(rspBuffer);
        streamedDownloadHandler.OnReceiveDataHandler += ReceiveData;
        request.downloadHandler = streamedDownloadHandler;

        convertRspData = true;
        yield return request.SendWebRequest();

        //request.SendWebRequest();
        //yield break;


        yield return new WaitForEndOfFrame();
        convertRspData = false;


        if (IsRequestError(request))
        {
            Debug.LogError($"API Error: {request.responseCode} --- {request.error}    \n{request.downloadHandler.text}");
            callback?.Invoke(null, false);
            yield break;
        }

        RefreshStreamRspData();
        if (rspRawContent.IsNullOrEmpty())
        {
            callback?.Invoke(name + "（陷入沉默）", false);
        }
        else
        {
            //callback?.Invoke(rspRawContent, true);
            callback?.Invoke(rspContent, true);
        }

        //DeepSeekResponse response = ParseResponse(request.downloadHandler.text);
        //if (response != null && response.choices.Length > 0)
        //{
        //    string npcReply = response.choices[0].message.content;
        //    callback?.Invoke(npcReply, true);
        //}
        //else
        //{
        //    callback?.Invoke(name + "（陷入沉默）", false);
        //}
    }






    /// <summary>    
    /// 创建UnityWebRequest对象   
    /// </summary>  
    /// <param name="jsonBody">请求体的JSON字符串</param>  
    /// <returns>配置好的UnityWebRequest对象</returns>   
    private UnityWebRequest CreateWebRequest(string jsonBody)
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        var request = new UnityWebRequest(apiUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);// 设置上传处理器
        request.downloadHandler = new DownloadHandlerBuffer();// 设置下载处理器
        request.SetRequestHeader("Content-Type", "application/json");// 设置请求头
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");// 设置认证头
        request.SetRequestHeader("Accept", "application/json");// 设置接受类型
        return request;
    }
    /// <summary>   
    /// 检查请求是否出错    
    /// </summary>   
    /// <param name="request">UnityWebRequest对象</param>   
    /// <returns>如果请求出错返回true，否则返回false</returns>   
    private bool IsRequestError(UnityWebRequest request)
    {
        return request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.DataProcessingError;
    }
    /// <summary>    
    /// 解析API响应   
    /// </summary> 
    /// <param name="jsonResponse">API响应的JSON字符串</param>  
    /// <returns>解析后的DeepSeekResponse对象</returns>   
    private DeepSeekResponse ParseResponse(string jsonResponse)
    {
        try
        {
            return JsonUtility.FromJson<DeepSeekResponse>(jsonResponse);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON解析失败: {e.Message}\n响应内容：{jsonResponse}");
            return null;
        }
    }
    // 可序列化数据结构
    [System.Serializable]
    private class ChatRequest
    {
        public string model;// 模型名称
        public List<Message> messages;// 消息列表
        public float temperature;// 温度参数
        public int max_tokens;// 最大令牌数
        public bool stream = false;// 是否流式下载

    }
    [System.Serializable]
    public class Message
    {
        public string role;// 角色（system/user/assistant）
        public string content;// 消息内容
    }
    [System.Serializable]
    private class DeepSeekResponse
    {
        public Choice[] choices;//
    }
    [System.Serializable]
    private class Choice
    {
        public Message message;// 生成的消息 
    }


    /// <summary>
    /// 用于处理流式下载的DownloadHandler
    /// </summary>
    public class StreamedDownloadHandler : DownloadHandlerScript
    {
        public event Action<byte[], int> OnReceiveDataHandler;
        public event Action OnCompleteHandler;
        public StreamedDownloadHandler(byte[] buffer) : base(buffer) { }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || data.Length < 1)
            {
                Debug.LogError("No data received");
                return false;
            }

            OnReceiveDataHandler?.Invoke(data, dataLength);
            //// 确保不会超出缓冲区大小
            //if (bufferOffset + dataLength > buffer.Length)
            //{
            //    Debug.LogError("Buffer overflow");
            //    return false;
            //}

            //// 将接收到的数据写入缓冲区
            //System.Buffer.BlockCopy(data, 0, buffer, bufferOffset, dataLength);
            //bufferOffset += dataLength;

            //// 处理接收到的数据
            //Debug.Log($"Received {dataLength} bytes");
            return true;
        }

        protected override void CompleteContent()
        {
            OnCompleteHandler?.Invoke();
            Debug.Log("Download complete");
        }

        protected override void ReceiveContentLength(int contentLength)
        {
            Debug.Log($"Content length: {contentLength}");
        }
    }


    /// <summary>
    /// DeepSeek返回的数据
    /// </summary>
    [Serializable]
    public class DeepSeekRspData
    {
        public string reasoning_content = "";
        public string content = "";

        public void AppendReason(string _reason)
        {
            reasoning_content += _reason;
        }

        public void AppendContent(string _content)
        {
            content += _content;
        }


        public void Clear()
        {
            reasoning_content = "";
            content = "";
        }

        public override string ToString()
        {
            return $"<color=#CCCCCC><b>deepSeek思路</b>\n<i>{reasoning_content}</i></color>\n <b>正式回复:</b>\n{content}";
        }
    }

}
