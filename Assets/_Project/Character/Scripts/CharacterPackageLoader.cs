using System;
using System.IO;
using Newtonsoft.Json;

namespace Project.Character
{
    /// <summary>
    /// 角色包加载器。
    ///
    /// 职责：
    /// 1. 读取 character.json。
    /// 2. 根据 character.json 读取 persona、prompt_template、mapping、voice_config。
    /// 3. 返回 CharacterPackageData。
    ///
    /// 注意：
    /// Loader 只负责加载，不负责完整校验。
    /// 完整校验交给 CharacterPackageValidator。
    /// </summary>
    public class CharacterPackageLoader
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        /// 加载角色包。
        /// 如果加载失败，直接抛出异常。
        /// </summary>
        public CharacterPackageData Load(CharacterPackageInfo packageInfo)
        {
            if (packageInfo == null)
            {
                throw new ArgumentNullException(nameof(packageInfo));
            }

            if (!Directory.Exists(packageInfo.PackageRootPath))
            {
                throw new DirectoryNotFoundException($"角色包目录不存在：{packageInfo.PackageRootPath}");
            }

            if (!File.Exists(packageInfo.CharacterJsonPath))
            {
                throw new FileNotFoundException($"缺少 character.json：{packageInfo.CharacterJsonPath}");
            }

            CharacterDefinition definition =
                LoadJsonFile<CharacterDefinition>(packageInfo.CharacterJsonPath, "character.json");

            var data = new CharacterPackageData
            {
                PackageInfo = packageInfo,
                Definition = definition
            };

            LoadModelPath(packageInfo, definition, data);
            LoadPersona(packageInfo, definition, data);
            LoadPromptTemplate(packageInfo, definition, data);
            LoadExpressionMapping(packageInfo, definition, data);
            LoadMotionMapping(packageInfo, definition, data);
            LoadVoiceConfig(packageInfo, definition, data);

            return data;
        }

        /// <summary>
        /// 尝试加载角色包。
        /// 不抛异常，适合在 DeveloperConsole 中调用。
        /// </summary>
        public bool TryLoad(
            CharacterPackageInfo packageInfo,
            out CharacterPackageData data,
            out string errorMessage)
        {
            try
            {
                data = Load(packageInfo);
                errorMessage = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                data = null;
                errorMessage = ex.Message;
                return false;
            }
        }

        private static void LoadModelPath(
            CharacterPackageInfo packageInfo,
            CharacterDefinition definition,
            CharacterPackageData data)
        {
            if (definition.Model == null || string.IsNullOrWhiteSpace(definition.Model.Model3JsonPath))
            {
                data.Model3JsonAbsolutePath = string.Empty;
                return;
            }

            data.Model3JsonAbsolutePath = packageInfo.ResolvePath(definition.Model.Model3JsonPath);
        }

        private static void LoadPersona(
            CharacterPackageInfo packageInfo,
            CharacterDefinition definition,
            CharacterPackageData data)
        {
            if (definition.Persona == null || string.IsNullOrWhiteSpace(definition.Persona.PersonaFile))
            {
                data.PersonaText = string.Empty;
                data.PersonaAbsolutePath = string.Empty;
                return;
            }

            string path = packageInfo.ResolvePath(definition.Persona.PersonaFile);
            data.PersonaAbsolutePath = path;
            data.PersonaText = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }

        private static void LoadPromptTemplate(
            CharacterPackageInfo packageInfo,
            CharacterDefinition definition,
            CharacterPackageData data)
        {
            string promptTemplateFile = "prompt_template.txt";

            if (definition.Persona != null &&
                !string.IsNullOrWhiteSpace(definition.Persona.PromptTemplateFile))
            {
                promptTemplateFile = definition.Persona.PromptTemplateFile;
            }

            string path = packageInfo.ResolvePath(promptTemplateFile);
            data.PromptTemplateAbsolutePath = path;
            data.PromptTemplateText = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }

        private static void LoadExpressionMapping(
            CharacterPackageInfo packageInfo,
            CharacterDefinition definition,
            CharacterPackageData data)
        {
            if (definition.Mapping == null ||
                string.IsNullOrWhiteSpace(definition.Mapping.ExpressionMappingFile))
            {
                data.ExpressionMapping = null;
                data.ExpressionMappingAbsolutePath = string.Empty;
                return;
            }

            string path = packageInfo.ResolvePath(definition.Mapping.ExpressionMappingFile);
            data.ExpressionMappingAbsolutePath = path;

            if (!File.Exists(path))
            {
                data.ExpressionMapping = null;
                return;
            }

            data.ExpressionMapping =
                LoadJsonFile<ExpressionMappingConfig>(path, "expression_mapping.json");
        }

        private static void LoadMotionMapping(
            CharacterPackageInfo packageInfo,
            CharacterDefinition definition,
            CharacterPackageData data)
        {
            if (definition.Mapping == null ||
                string.IsNullOrWhiteSpace(definition.Mapping.MotionMappingFile))
            {
                data.MotionMapping = null;
                data.MotionMappingAbsolutePath = string.Empty;
                return;
            }

            string path = packageInfo.ResolvePath(definition.Mapping.MotionMappingFile);
            data.MotionMappingAbsolutePath = path;

            if (!File.Exists(path))
            {
                data.MotionMapping = null;
                return;
            }

            data.MotionMapping =
                LoadJsonFile<MotionMappingConfig>(path, "motion_mapping.json");
        }

        private static void LoadVoiceConfig(
            CharacterPackageInfo packageInfo,
            CharacterDefinition definition,
            CharacterPackageData data)
        {
            if (definition.Voice == null ||
                string.IsNullOrWhiteSpace(definition.Voice.VoiceConfigFile))
            {
                data.VoiceConfigJson = string.Empty;
                data.VoiceConfigAbsolutePath = string.Empty;
                return;
            }

            string path = packageInfo.ResolvePath(definition.Voice.VoiceConfigFile);
            data.VoiceConfigAbsolutePath = path;
            data.VoiceConfigJson = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }

        private static T LoadJsonFile<T>(string path, string displayName) where T : class
        {
            string json = File.ReadAllText(path);

            T result = JsonConvert.DeserializeObject<T>(json, JsonSettings);

            if (result == null)
            {
                throw new InvalidDataException($"{displayName} 解析结果为空：{path}");
            }

            return result;
        }
    }
}