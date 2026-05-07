namespace Project.Character
{
    /// <summary>
    /// 一个已经加载进内存的角色包。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="CharacterPackageInfo"/> 只表示“角色包在哪里”；
    /// <see cref="CharacterPackageData"/> 表示“角色包内容是什么”。
    /// </para>
    /// <para>
    /// 使用方式：
    /// <code>
    /// CharacterPackageData data = loader.Load(packageInfo);
    /// Debug.Log(data.CharacterName);
    /// Debug.Log(data.PersonaText);
    /// </code>
    /// </para>
    /// <para>
    /// 对外暴露属性：角色包路径信息、角色定义、表情映射、动作映射、人设文本、Prompt 模板文本、
    /// 语音配置 JSON、各关键资源的绝对路径，以及便捷的 CharacterId 和 CharacterName。
    /// </para>
    /// </remarks>
    public class CharacterPackageData
    {
        /// <summary>
        /// 角色包基础路径信息。
        /// </summary>
        public CharacterPackageInfo PackageInfo { get; set; }

        /// <summary>
        /// character.json 解析后的强类型数据。
        /// </summary>
        public CharacterDefinition Definition { get; set; }

        /// <summary>
        /// expression_mapping.json 解析后的强类型数据。
        /// </summary>
        public ExpressionMappingConfig ExpressionMapping { get; set; }

        /// <summary>
        /// motion_mapping.json 解析后的强类型数据。
        /// </summary>
        public MotionMappingConfig MotionMapping { get; set; }

        /// <summary>
        /// persona.txt 的完整文本内容。
        /// </summary>
        public string PersonaText { get; set; }

        /// <summary>
        /// prompt_template.txt 的完整文本内容。
        /// </summary>
        public string PromptTemplateText { get; set; }

        /// <summary>
        /// voice_config.json 的原始 JSON 文本。
        /// </summary>
        /// <remarks>
        /// 当前 MVP 阶段暂时不把 voice_config.json 拆成强类型对象。
        /// TODO: 后续接入 TTS 后，应增加 VoiceConfig DTO，便于校验音色、speakerId、语速等配置。
        /// </remarks>
        public string VoiceConfigJson { get; set; }

        /// <summary>
        /// model3.json 的绝对路径。
        /// </summary>
        public string Model3JsonAbsolutePath { get; set; }

        /// <summary>
        /// persona.txt 的绝对路径。
        /// </summary>
        public string PersonaAbsolutePath { get; set; }

        /// <summary>
        /// prompt_template.txt 的绝对路径。
        /// </summary>
        public string PromptTemplateAbsolutePath { get; set; }

        /// <summary>
        /// expression_mapping.json 的绝对路径。
        /// </summary>
        public string ExpressionMappingAbsolutePath { get; set; }

        /// <summary>
        /// motion_mapping.json 的绝对路径。
        /// </summary>
        public string MotionMappingAbsolutePath { get; set; }

        /// <summary>
        /// voice_config.json 的绝对路径。
        /// </summary>
        public string VoiceConfigAbsolutePath { get; set; }

        /// <summary>
        /// 角色 ID 的便捷访问属性。
        /// </summary>
        public string CharacterId
        {
            get
            {
                if (Definition == null)
                {
                    return string.Empty;
                }

                return Definition.CharacterId;
            }
        }

        /// <summary>
        /// 角色显示名的便捷访问属性。
        /// </summary>
        public string CharacterName
        {
            get
            {
                if (Definition == null)
                {
                    return string.Empty;
                }

                return Definition.CharacterName;
            }
        }
    }
}
