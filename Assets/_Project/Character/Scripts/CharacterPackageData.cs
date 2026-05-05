namespace Project.Character
{
    /// <summary>
    /// 一个已经被加载进内存的角色包。
    ///
    /// 注意：
    /// CharacterPackageInfo 只表示“角色包在哪里”；
    /// CharacterPackageData 表示“角色包内容是什么”。
    /// </summary>
    public class CharacterPackageData
    {
        public CharacterPackageInfo PackageInfo { get; set; }

        public CharacterDefinition Definition { get; set; }

        public ExpressionMappingConfig ExpressionMapping { get; set; }

        public MotionMappingConfig MotionMapping { get; set; }

        public string PersonaText { get; set; }

        public string PromptTemplateText { get; set; }

        public string VoiceConfigJson { get; set; }

        public string Model3JsonAbsolutePath { get; set; }

        public string PersonaAbsolutePath { get; set; }

        public string PromptTemplateAbsolutePath { get; set; }

        public string ExpressionMappingAbsolutePath { get; set; }

        public string MotionMappingAbsolutePath { get; set; }

        public string VoiceConfigAbsolutePath { get; set; }

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