using System.Collections.Generic;
using Newtonsoft.Json;

namespace Project.Character
{
    /// <summary>
    /// character.json 对应的数据结构。
    /// </summary>
    public class CharacterDefinition
    {
        [JsonProperty("characterId")]
        public string CharacterId { get; set; }

        [JsonProperty("characterName")]
        public string CharacterName { get; set; }

        [JsonProperty("configVersion")]
        public string ConfigVersion { get; set; }

        [JsonProperty("model")]
        public CharacterModelConfig Model { get; set; }

        [JsonProperty("persona")]
        public CharacterPersonaConfig Persona { get; set; }

        [JsonProperty("mapping")]
        public CharacterMappingConfig Mapping { get; set; }

        [JsonProperty("voice")]
        public CharacterVoiceConfig Voice { get; set; }
    }

    /// <summary>
    /// Live2D 模型相关配置。
    /// </summary>
    public class CharacterModelConfig
    {
        [JsonProperty("model3JsonPath")]
        public string Model3JsonPath { get; set; }

        [JsonProperty("autoScanExpressions")]
        public bool AutoScanExpressions { get; set; } = true;

        [JsonProperty("autoScanMotions")]
        public bool AutoScanMotions { get; set; } = true;
    }

    /// <summary>
    /// 角色人设与 Prompt 模板配置。
    /// </summary>
    public class CharacterPersonaConfig
    {
        [JsonProperty("personaFile")]
        public string PersonaFile { get; set; }

        /// <summary>
        /// 这里是扩展字段。
        /// 之前 character.json 示例中没有写，但角色包里通常会有 prompt_template.txt。
        /// 如果 JSON 中没写，Loader 会默认尝试读取 prompt_template.txt。
        /// </summary>
        [JsonProperty("promptTemplateFile")]
        public string PromptTemplateFile { get; set; }
    }

    /// <summary>
    /// 表情映射表与动作映射表配置。
    /// </summary>
    public class CharacterMappingConfig
    {
        [JsonProperty("expressionMappingFile")]
        public string ExpressionMappingFile { get; set; }

        [JsonProperty("motionMappingFile")]
        public string MotionMappingFile { get; set; }
    }

    /// <summary>
    /// 语音配置。
    /// 当前 Unity 前端 MVP 只读取配置，不直接调用真实 TTS。
    /// </summary>
    public class CharacterVoiceConfig
    {
        [JsonProperty("voiceConfigFile")]
        public string VoiceConfigFile { get; set; }
    }

    /// <summary>
    /// expression_mapping.json 对应的数据结构。
    /// </summary>
    public class ExpressionMappingConfig
    {
        [JsonProperty("defaultEmotion")]
        public string DefaultEmotion { get; set; }

        [JsonProperty("emotionMappings")]
        public Dictionary<string, ExpressionMappingEntry> EmotionMappings { get; set; }
            = new Dictionary<string, ExpressionMappingEntry>();

        [JsonProperty("fallbackEmotionMap")]
        public Dictionary<string, string> FallbackEmotionMap { get; set; }
            = new Dictionary<string, string>();
    }

    /// <summary>
    /// 单个 emotion 到表情文件的映射。
    /// </summary>
    public class ExpressionMappingEntry
    {
        [JsonProperty("expression")]
        public string Expression { get; set; }

        [JsonProperty("priority")]
        public int Priority { get; set; }
    }

    /// <summary>
    /// motion_mapping.json 对应的数据结构。
    /// </summary>
    public class MotionMappingConfig
    {
        [JsonProperty("defaultMotion")]
        public string DefaultMotion { get; set; }

        [JsonProperty("actionMappings")]
        public Dictionary<string, MotionMappingEntry> ActionMappings { get; set; }
            = new Dictionary<string, MotionMappingEntry>();

        [JsonProperty("fallbackActionMap")]
        public Dictionary<string, string> FallbackActionMap { get; set; }
            = new Dictionary<string, string>();
    }

    /// <summary>
    /// 单个 action 到动作文件的映射。
    /// </summary>
    public class MotionMappingEntry
    {
        [JsonProperty("motion")]
        public string Motion { get; set; }

        [JsonProperty("loop")]
        public bool Loop { get; set; }

        [JsonProperty("priority")]
        public int Priority { get; set; }
    }
}