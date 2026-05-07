using Newtonsoft.Json;

namespace Project.ExperimentLogging
{
    /// <summary>
    /// 前端交互日志条目。
    ///
    /// 功能：
    /// 1. 记录一次用户输入到前端表现命令生成的完整信息。
    /// 2. 保存 LLM/Mock 输出的语义标签和映射后的具体资源文件。
    /// 3. 为后续论文实验统计提供 JSONL 原始数据。
    ///
    /// 使用方式：
    /// var entry = new FrontendInteractionLogEntry();
    /// JsonlExperimentLogger.Append(entry);
    ///
    /// 对外暴露字段：
    /// 本类只作为数据容器，字段会直接序列化为 JSON。
    /// </summary>
    public class FrontendInteractionLogEntry
    {
        [JsonProperty("sessionId")]
        public string SessionId;

        [JsonProperty("turnId")]
        public int TurnId;

        [JsonProperty("timestamp")]
        public string Timestamp;

        [JsonProperty("characterId")]
        public string CharacterId;

        [JsonProperty("characterName")]
        public string CharacterName;

        [JsonProperty("configVersion")]
        public string ConfigVersion;

        [JsonProperty("userInput")]
        public string UserInput;

        [JsonProperty("replyText")]
        public string ReplyText;

        [JsonProperty("emotion")]
        public string Emotion;

        [JsonProperty("action")]
        public string Action;

        [JsonProperty("voiceStyle")]
        public string VoiceStyle;

        [JsonProperty("selectedExpression")]
        public string SelectedExpression;

        [JsonProperty("selectedMotion")]
        public string SelectedMotion;

        [JsonProperty("resolvedEmotion")]
        public string ResolvedEmotion;

        [JsonProperty("resolvedAction")]
        public string ResolvedAction;

        [JsonProperty("expressionFallback")]
        public bool ExpressionFallback;

        [JsonProperty("motionFallback")]
        public bool MotionFallback;

        [JsonProperty("expressionFallbackReason")]
        public string ExpressionFallbackReason;

        [JsonProperty("motionFallbackReason")]
        public string MotionFallbackReason;

        [JsonProperty("expressionFileExists")]
        public bool ExpressionFileExists;

        [JsonProperty("motionFileExists")]
        public bool MotionFileExists;

        [JsonProperty("rawResponseJson")]
        public string RawResponseJson;

        [JsonProperty("parseSuccess")]
        public bool ParseSuccess;

        [JsonProperty("errorMessage")]
        public string ErrorMessage;
    }
}
