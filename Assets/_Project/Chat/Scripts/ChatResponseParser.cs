using System;
using Newtonsoft.Json;

namespace Project.Chat
{
    /// <summary>
    /// 聊天响应解析器。
    ///
    /// 功能：
    /// 1. 把后端或 Mock 服务返回的 JSON 字符串解析为 ChatResponseDto。
    /// 2. 对缺失字段做安全默认值处理。
    /// 3. 避免 JSON 解析异常直接打断 Unity 前端流程。
    ///
    /// 使用方式：
    /// bool ok = ChatResponseParser.TryParse(rawJson, out ChatResponseDto response, out string error);
    ///
    /// 对外暴露方法：
    /// - TryParse：安全解析 JSON。
    /// </summary>
    public static class ChatResponseParser
    {
        /// <summary>
        /// 尝试解析聊天响应 JSON。
        /// </summary>
        /// <param name="rawJson">原始 JSON 字符串。</param>
        /// <param name="response">解析成功时输出响应对象。</param>
        /// <param name="errorMessage">解析失败时输出错误信息。</param>
        /// <returns>解析成功返回 true；失败返回 false。</returns>
        public static bool TryParse(string rawJson, out ChatResponseDto response, out string errorMessage)
        {
            response = null;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                errorMessage = "聊天响应 JSON 为空。";
                return false;
            }

            try
            {
                response = JsonConvert.DeserializeObject<ChatResponseDto>(rawJson);

                if (response == null)
                {
                    errorMessage = "聊天响应 JSON 解析结果为空。";
                    return false;
                }

                ApplySafeDefaults(response);
                return true;
            }
            catch (Exception exception)
            {
                response = null;
                errorMessage = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// 给缺失字段补安全默认值。
        /// </summary>
        /// <param name="response">需要补默认值的响应对象。</param>
        private static void ApplySafeDefaults(ChatResponseDto response)
        {
            if (string.IsNullOrWhiteSpace(response.ReplyText))
            {
                response.ReplyText = "我、我刚才好像不知道该怎么回答……";
            }

            if (string.IsNullOrWhiteSpace(response.Emotion))
            {
                response.Emotion = "neutral";
            }

            if (string.IsNullOrWhiteSpace(response.Action))
            {
                response.Action = "idle";
            }

            if (string.IsNullOrWhiteSpace(response.VoiceStyle))
            {
                response.VoiceStyle = "normal";
            }
        }
    }
}
