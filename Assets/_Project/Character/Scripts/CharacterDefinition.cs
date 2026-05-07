using System.Collections.Generic;
using Newtonsoft.Json;

namespace Project.Character
{
    /// <summary>
    /// <c>character.json</c> 对应的数据结构。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该类只负责承载角色元数据，不负责文件读取、路径解析、资源扫描或 Live2D 播放。
    /// 文件读取由 <see cref="CharacterPackageLoader"/> 完成，完整性检查由
    /// <see cref="CharacterPackageValidator"/> 完成。
    /// </para>
    /// <para>
    /// 使用方式：
    /// <code>
    /// CharacterDefinition definition = JsonConvert.DeserializeObject&lt;CharacterDefinition&gt;(json);
    /// string id = definition.CharacterId;
    /// </code>
    /// </para>
    /// <para>
    /// 对外暴露属性：角色 ID、角色名、配置版本、Live2D 模型配置、人设配置、表现映射配置和语音配置。
    /// </para>
    /// </remarks>
    public class CharacterDefinition
    {
        /// <summary>
        /// 角色唯一标识。
        /// </summary>
        /// <remarks>
        /// 建议使用稳定的小写英文或拼音，例如 <c>huohuo</c>。
        /// 后续实验日志、角色切换和配置版本追踪都应优先使用该字段。
        /// </remarks>
        [JsonProperty("characterId")]
        public string CharacterId { get; set; }

        /// <summary>
        /// 角色显示名。
        /// </summary>
        /// <remarks>
        /// 可用于 UI、DeveloperConsole 和日志展示，例如“藿藿”。
        /// </remarks>
        [JsonProperty("characterName")]
        public string CharacterName { get; set; }

        /// <summary>
        /// 角色包配置版本。
        /// </summary>
        /// <remarks>
        /// 建议使用语义化版本号，例如 <c>1.0.0</c>。
        /// 后续做实验对比时，可以把它写入日志，保证实验数据可追溯。
        /// </remarks>
        [JsonProperty("configVersion")]
        public string ConfigVersion { get; set; }

        /// <summary>
        /// Live2D 模型相关配置。
        /// </summary>
        [JsonProperty("model")]
        public CharacterModelConfig Model { get; set; }

        /// <summary>
        /// 人设文件和 Prompt 模板文件配置。
        /// </summary>
        [JsonProperty("persona")]
        public CharacterPersonaConfig Persona { get; set; }

        /// <summary>
        /// emotion/action 到表现资源的映射表配置。
        /// </summary>
        [JsonProperty("mapping")]
        public CharacterMappingConfig Mapping { get; set; }

        /// <summary>
        /// 语音配置。
        /// </summary>
        [JsonProperty("voice")]
        public CharacterVoiceConfig Voice { get; set; }
    }

    /// <summary>
    /// Live2D 模型相关配置。
    /// </summary>
    /// <remarks>
    /// 该类对应 <c>character.json</c> 中的 <c>model</c> 字段。
    /// 当前阶段只记录 model3.json 路径和是否自动扫描表情/动作文件。
    /// TODO: 后续可以扩展模型缩放、初始位置、默认 idle motion、默认显示参数等前端表现配置。
    /// </remarks>
    public class CharacterModelConfig
    {
        /// <summary>
        /// 角色包内 model3.json 的相对路径。
        /// </summary>
        /// <example>live2d/Huohuo.model3.json</example>
        [JsonProperty("model3JsonPath")]
        public string Model3JsonPath { get; set; }

        /// <summary>
        /// 是否自动扫描 Live2D 表情文件。
        /// </summary>
        [JsonProperty("autoScanExpressions")]
        public bool AutoScanExpressions { get; set; } = true;

        /// <summary>
        /// 是否自动扫描 Live2D 动作文件。
        /// </summary>
        [JsonProperty("autoScanMotions")]
        public bool AutoScanMotions { get; set; } = true;
    }

    /// <summary>
    /// 角色人设与 Prompt 模板配置。
    /// </summary>
    public class CharacterPersonaConfig
    {
        /// <summary>
        /// persona 文本文件名或相对路径。
        /// </summary>
        /// <remarks>
        /// 该文件用于描述角色身份、性格、说话风格、禁忌内容等长期稳定设定。
        /// </remarks>
        [JsonProperty("personaFile")]
        public string PersonaFile { get; set; }

        /// <summary>
        /// Prompt 模板文件名或相对路径。
        /// </summary>
        /// <remarks>
        /// 这是扩展字段。如果 JSON 中没有写，<see cref="CharacterPackageLoader"/>
        /// 会默认尝试读取 <c>prompt_template.txt</c>。
        /// TODO: 后续接入后端后，需要明确 Prompt 模板由 Unity 端管理还是后端 PromptManager 管理。
        /// </remarks>
        [JsonProperty("promptTemplateFile")]
        public string PromptTemplateFile { get; set; }
    }

    /// <summary>
    /// 表情映射表与动作映射表配置。
    /// </summary>
    public class CharacterMappingConfig
    {
        /// <summary>
        /// expression_mapping.json 的文件名或相对路径。
        /// </summary>
        [JsonProperty("expressionMappingFile")]
        public string ExpressionMappingFile { get; set; }

        /// <summary>
        /// motion_mapping.json 的文件名或相对路径。
        /// </summary>
        [JsonProperty("motionMappingFile")]
        public string MotionMappingFile { get; set; }
    }

    /// <summary>
    /// 语音配置。
    /// </summary>
    /// <remarks>
    /// 当前 Unity 前端 MVP 只读取配置，不直接调用真实 TTS。
    /// TODO: 后续接入 TTS 后，可以把 voiceStyle、speakerId、音色、语速、音量等字段拆成强类型 DTO。
    /// </remarks>
    public class CharacterVoiceConfig
    {
        /// <summary>
        /// voice_config.json 的文件名或相对路径。
        /// </summary>
        [JsonProperty("voiceConfigFile")]
        public string VoiceConfigFile { get; set; }
    }

    /// <summary>
    /// <c>expression_mapping.json</c> 对应的数据结构。
    /// </summary>
    /// <remarks>
    /// 表情映射表负责把 LLM 输出的 emotion 语义标签转换为当前角色包中真实存在的 exp3.json 表情资源。
    /// 该结构用于角色包加载和校验；运行时解析逻辑在 <c>Project.Presentation.BehaviorMappingResolver</c> 中。
    /// </remarks>
    public class ExpressionMappingConfig
    {
        /// <summary>
        /// 默认 emotion 标签。
        /// </summary>
        /// <remarks>
        /// 一般写 <c>neutral</c>。当输入 emotion 为空或非法时，可以回退到该标签。
        /// </remarks>
        [JsonProperty("defaultEmotion")]
        public string DefaultEmotion { get; set; }

        /// <summary>
        /// emotion 标签到表情文件的映射表。
        /// </summary>
        [JsonProperty("emotionMappings")]
        public Dictionary<string, ExpressionMappingEntry> EmotionMappings { get; set; } =
            new Dictionary<string, ExpressionMappingEntry>();

        /// <summary>
        /// emotion 兜底映射表。
        /// </summary>
        /// <remarks>
        /// 例如 <c>excited -&gt; happy</c>、<c>upset -&gt; sad</c>。
        /// </remarks>
        [JsonProperty("fallbackEmotionMap")]
        public Dictionary<string, string> FallbackEmotionMap { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 单个 emotion 到表情文件的映射项。
    /// </summary>
    public class ExpressionMappingEntry
    {
        /// <summary>
        /// 表情文件名或角色包内相对路径。
        /// </summary>
        /// <remarks>
        /// 允许为空字符串。对于 neutral 这种“恢复 Live2D 默认脸”的状态，
        /// 可以不绑定任何 exp3.json，而是让后续 Live2DExpressionPlayer 清空当前表情。
        /// </remarks>
        [JsonProperty("expression")]
        public string Expression { get; set; }

        /// <summary>
        /// 表情优先级。
        /// </summary>
        /// <remarks>
        /// 当前 MVP 阶段主要用于调试输出；后续可用于表情抢占、过渡和表现命令队列排序。
        /// </remarks>
        [JsonProperty("priority")]
        public int Priority { get; set; }

        /// <summary>
        /// 映射说明。
        /// </summary>
        /// <remarks>
        /// 该字段主要用于 DeveloperConsole 展示和论文说明，不参与核心解析逻辑。
        /// </remarks>
        [JsonProperty("description")]
        public string Description { get; set; }
    }

    /// <summary>
    /// <c>motion_mapping.json</c> 对应的数据结构。
    /// </summary>
    public class MotionMappingConfig
    {
        /// <summary>
        /// 默认动作文件名。
        /// </summary>
        [JsonProperty("defaultMotion")]
        public string DefaultMotion { get; set; }

        /// <summary>
        /// action 标签到 motion3.json 动作文件的映射表。
        /// </summary>
        [JsonProperty("actionMappings")]
        public Dictionary<string, MotionMappingEntry> ActionMappings { get; set; } =
            new Dictionary<string, MotionMappingEntry>();

        /// <summary>
        /// action 兜底映射表。
        /// </summary>
        /// <remarks>
        /// 例如 <c>hello -&gt; greet</c>、<c>no -&gt; deny</c>。
        /// </remarks>
        [JsonProperty("fallbackActionMap")]
        public Dictionary<string, string> FallbackActionMap { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 单个 action 到动作文件的映射项。
    /// </summary>
    public class MotionMappingEntry
    {
        /// <summary>
        /// motion3.json 文件名或角色包内相对路径。
        /// </summary>
        [JsonProperty("motion")]
        public string Motion { get; set; }

        /// <summary>
        /// 该动作是否循环播放。
        /// </summary>
        [JsonProperty("loop")]
        public bool Loop { get; set; }

        /// <summary>
        /// 动作优先级。
        /// </summary>
        /// <remarks>
        /// 当前 MVP 阶段主要用于调试输出；后续可用于动作打断、排队和表现命令调度。
        /// </remarks>
        [JsonProperty("priority")]
        public int Priority { get; set; }

        /// <summary>
        /// 映射说明。
        /// </summary>
        /// <remarks>
        /// 该字段主要用于 DeveloperConsole 展示和论文说明，不参与核心解析逻辑。
        /// </remarks>
        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
