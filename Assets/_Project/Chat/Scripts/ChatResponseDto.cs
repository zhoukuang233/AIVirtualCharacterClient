using Newtonsoft.Json;

namespace Project.Chat
{
    /// <summary>
    /// 一次聊天回复的结构化数据对象。
    ///
    /// 功能：
    /// 1. 承载后端或 Mock 服务返回的回复文本。
    /// 2. 承载 emotion、action、voiceStyle 三类语义标签。
    /// 3. 作为 ChatSystem 与 PresentationSystem 之间的数据边界。
    ///
    /// 使用方式：
    /// ChatResponseDto response = ChatResponseParser.Parse(rawJson);
    /// string emotion = response.Emotion;
    ///
    /// 对外暴露属性：
    /// - ReplyText：要显示给用户的角色回复。
    /// - Emotion：语义情绪标签，例如 happy、sad、neutral。
    /// - Action：语义动作标签，例如 greet、idle、encourage。
    /// - VoiceStyle：语音风格标签，例如 normal、soft、cheerful。
    /// </summary>
    public class ChatResponseDto
    {
        /// <summary>
        /// 角色回复文本。
        /// </summary>
        [JsonProperty("replyText")]
        public string ReplyText { get; set; }

        /// <summary>
        /// 情绪标签。
        /// </summary>
        [JsonProperty("emotion")]
        public string Emotion { get; set; }

        /// <summary>
        /// 动作标签。
        /// </summary>
        [JsonProperty("action")]
        public string Action { get; set; }

        /// <summary>
        /// 语音风格标签。
        /// </summary>
        [JsonProperty("voiceStyle")]
        public string VoiceStyle { get; set; }
    }
}
