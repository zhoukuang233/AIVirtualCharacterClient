using Newtonsoft.Json;

namespace Project.Chat
{
    /// <summary>
    /// 一次聊天请求的数据对象。
    ///
    /// 功能：
    /// 1. 保存用户输入。
    /// 2. 保存当前角色的基础信息。
    /// 3. 为后续真实后端请求预留 persona 和 promptTemplate 字段。
    ///
    /// 使用方式：
    /// ChatRequestDto request = ChatRequestBuilder.Build(userInput, characterData);
    ///
    /// 当前阶段：
    /// 该对象主要交给 MockChatBackendClient 使用，不会真正访问网络。
    /// 后续接入 Python/FastAPI 或 Spring Boot 后端时，可以直接序列化为 JSON 请求体。
    /// </summary>
    public class ChatRequestDto
    {
        /// <summary>
        /// 用户输入文本。
        /// </summary>
        [JsonProperty("userInput")]
        public string UserInput { get; set; }

        /// <summary>
        /// 当前角色 ID。
        /// </summary>
        [JsonProperty("characterId")]
        public string CharacterId { get; set; }

        /// <summary>
        /// 当前角色显示名。
        /// </summary>
        [JsonProperty("characterName")]
        public string CharacterName { get; set; }

        /// <summary>
        /// 当前角色配置版本。
        /// </summary>
        [JsonProperty("configVersion")]
        public string ConfigVersion { get; set; }

        /// <summary>
        /// 当前角色人设文本。
        /// </summary>
        [JsonProperty("personaText")]
        public string PersonaText { get; set; }

        /// <summary>
        /// Prompt 模板文本。
        /// </summary>
        [JsonProperty("promptTemplateText")]
        public string PromptTemplateText { get; set; }
    }
}
