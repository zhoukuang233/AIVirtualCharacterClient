using Newtonsoft.Json;
using Project.Chat;

namespace Project.Service
{
    /// <summary>
    /// Mock 聊天后端客户端。
    ///
    /// 功能：
    /// 1. 在不接入真实 LLM/TTS 后端的情况下，模拟返回结构化聊天响应。
    /// 2. 让 Unity 前端先跑通“用户输入 -> 结构化响应 -> 表现映射 -> 命令队列 -> 日志”的闭环。
    /// 3. 后续接入真实后端时，可用 BackendApiClient 替换本类。
    ///
    /// 使用方式：
    /// var client = new MockChatBackendClient();
    /// string rawJson = client.GenerateResponseJson(request);
    ///
    /// 对外暴露方法：
    /// - GenerateResponseJson：根据用户输入生成模拟 JSON 响应。
    /// </summary>
    public class MockChatBackendClient
    {
        /// <summary>
        /// 根据请求生成模拟响应 JSON。
        /// </summary>
        /// <param name="request">聊天请求。</param>
        /// <returns>符合 ChatResponseDto 格式的 JSON 字符串。</returns>
        public string GenerateResponseJson(ChatRequestDto request)
        {
            ChatResponseDto response = GenerateResponse(request);
            return JsonConvert.SerializeObject(response, Formatting.None);
        }

        /// <summary>
        /// 根据用户输入简单匹配一个回复模板。
        /// </summary>
        /// <param name="request">聊天请求。</param>
        /// <returns>模拟的结构化回复。</returns>
        private ChatResponseDto GenerateResponse(ChatRequestDto request)
        {
            string input = request != null && request.UserInput != null
                ? request.UserInput.Trim()
                : string.Empty;

            if (ContainsAny(input, "你好", "您好", "hello", "hi"))
            {
                return new ChatResponseDto
                {
                    ReplyText = "你、你好……我是藿藿。附近应该没有什么奇怪的动静吧？如果没有的话，我们可以慢慢聊……",
                    Emotion = "happy",
                    Action = "greet",
                    VoiceStyle = "cheerful"
                };
            }

            if (ContainsAny(input, "鬼", "邪祟", "闹鬼", "害怕", "吓"))
            {
                return new ChatResponseDto
                {
                    ReplyText = "呜……这、这个听起来真的很可怕。要不我们先贴一张符，再离远一点观察？我不是想逃跑，只是想先确认安全路线……",
                    Emotion = "scared",
                    Action = "panic",
                    VoiceStyle = "serious"
                };
            }

            if (ContainsAny(input, "谢谢", "厉害", "可爱", "夸"))
            {
                return new ChatResponseDto
                {
                    ReplyText = "诶？我、我没有那么厉害啦……不过你愿意这么说，我会稍微有一点勇气的。谢谢你……",
                    Emotion = "shy",
                    Action = "agree",
                    VoiceStyle = "soft"
                };
            }

            if (ContainsAny(input, "加油", "鼓励", "困难", "难受", "帮我"))
            {
                return new ChatResponseDto
                {
                    ReplyText = "虽、虽然我也会紧张……但我会陪你一起想办法的。我们一步一步来，先处理最容易做到的部分，好吗？",
                    Emotion = "encouraging",
                    Action = "encourage",
                    VoiceStyle = "cheerful"
                };
            }

            if (ContainsAny(input, "再见", "拜拜", "下次", "晚安"))
            {
                return new ChatResponseDto
                {
                    ReplyText = "那、那今天就先到这里吧……路上小心。如果听到奇怪的声音，也不要一个人逞强哦。",
                    Emotion = "shy",
                    Action = "farewell",
                    VoiceStyle = "soft"
                };
            }

            return new ChatResponseDto
            {
                ReplyText = "嗯……我、我会认真听的。你可以再多告诉我一点吗？这样我应该能更好地帮你判断。",
                Emotion = "neutral",
                Action = "think",
                VoiceStyle = "normal"
            };
        }

        /// <summary>
        /// 判断文本中是否包含任意关键词。
        /// </summary>
        /// <param name="text">待检查文本。</param>
        /// <param name="keywords">关键词列表。</param>
        /// <returns>包含任意关键词返回 true，否则返回 false。</returns>
        private static bool ContainsAny(string text, params string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(text) || keywords == null)
            {
                return false;
            }

            string lowerText = text.ToLowerInvariant();

            foreach (string keyword in keywords)
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    continue;
                }

                if (lowerText.Contains(keyword.ToLowerInvariant()))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
