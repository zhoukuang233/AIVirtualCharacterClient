using System;
using System.IO;
using Newtonsoft.Json;

namespace Project.Character
{
    /// <summary>
    /// 角色包加载器。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 职责：读取一个角色包中的 <c>character.json</c>，并根据其中的配置继续读取 persona、
    /// prompt_template、expression_mapping、motion_mapping 和 voice_config 等文件，最终返回
    /// <see cref="CharacterPackageData"/>。
    /// </para>
    /// <para>
    /// 本类只负责加载，不负责完整性校验。完整性校验请使用 <see cref="CharacterPackageValidator"/>。
    /// 这样可以让 DeveloperConsole 先扫描和展示角色包，即使某些文件缺失也能给出可读错误。
    /// </para>
    /// <para>
    /// 使用方式：
    /// <code>
    /// var loader = new CharacterPackageLoader();
    /// CharacterPackageData data = loader.Load(packageInfo);
    /// </code>
    /// 或者：
    /// <code>
    /// bool ok = loader.TryLoad(packageInfo, out CharacterPackageData data, out string error);
    /// </code>
    /// </para>
    /// <para>
    /// 对外暴露方法：<see cref="Load"/> 和 <see cref="TryLoad"/>。
    /// </para>
    /// <para>
    /// TODO: 后续可以加入缓存机制，避免频繁切换同一角色时反复读取磁盘。
    /// TODO: 后续可以增加异步加载版本，避免大角色包在主线程中读取造成卡顿。
    /// </para>
    /// </remarks>
    public class CharacterPackageLoader
    {
        /// <summary>
        /// JSON 反序列化设置。
        /// </summary>
        /// <remarks>
        /// 当前允许 JSON 中存在代码未声明的字段，便于角色包配置向后兼容。
        /// </remarks>
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        /// 加载角色包。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <returns>返回已经加载进内存的角色包数据。</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="packageInfo"/> 为 null 时抛出。</exception>
        /// <exception cref="DirectoryNotFoundException">当角色包目录不存在时抛出。</exception>
        /// <exception cref="FileNotFoundException">当 character.json 不存在时抛出。</exception>
        /// <exception cref="InvalidDataException">当 JSON 解析结果为空时抛出。</exception>
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

            CharacterDefinition definition = LoadJsonFile<CharacterDefinition>(packageInfo.CharacterJsonPath, "character.json");

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
        /// 尝试加载角色包，不向外抛出异常。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <param name="data">加载成功时返回角色包数据；失败时为 null。</param>
        /// <param name="errorMessage">加载失败时返回错误信息；成功时为空字符串。</param>
        /// <returns>加载成功返回 true，加载失败返回 false。</returns>
        /// <remarks>
        /// 适合 DeveloperConsole、角色包列表扫描等不希望异常中断流程的场景。
        /// </remarks>
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

        /// <summary>
        /// 解析 model3.json 的绝对路径。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <param name="definition">character.json 解析结果。</param>
        /// <param name="data">需要写入路径信息的角色包数据对象。</param>
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

        /// <summary>
        /// 读取 persona 文本文件。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <param name="definition">character.json 解析结果。</param>
        /// <param name="data">需要写入文本和路径信息的角色包数据对象。</param>
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

        /// <summary>
        /// 读取 Prompt 模板文本文件。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <param name="definition">character.json 解析结果。</param>
        /// <param name="data">需要写入文本和路径信息的角色包数据对象。</param>
        private static void LoadPromptTemplate(
            CharacterPackageInfo packageInfo,
            CharacterDefinition definition,
            CharacterPackageData data)
        {
            string promptTemplateFile = "prompt_template.txt";

            if (definition.Persona != null && !string.IsNullOrWhiteSpace(definition.Persona.PromptTemplateFile))
            {
                promptTemplateFile = definition.Persona.PromptTemplateFile;
            }

            string path = packageInfo.ResolvePath(promptTemplateFile);
            data.PromptTemplateAbsolutePath = path;
            data.PromptTemplateText = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }

        /// <summary>
        /// 读取 expression_mapping.json。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <param name="definition">character.json 解析结果。</param>
        /// <param name="data">需要写入映射表和路径信息的角色包数据对象。</param>
        private static void LoadExpressionMapping(
            CharacterPackageInfo packageInfo,
            CharacterDefinition definition,
            CharacterPackageData data)
        {
            if (definition.Mapping == null || string.IsNullOrWhiteSpace(definition.Mapping.ExpressionMappingFile))
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

            data.ExpressionMapping = LoadJsonFile<ExpressionMappingConfig>(path, "expression_mapping.json");
        }

        /// <summary>
        /// 读取 motion_mapping.json。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <param name="definition">character.json 解析结果。</param>
        /// <param name="data">需要写入映射表和路径信息的角色包数据对象。</param>
        private static void LoadMotionMapping(
            CharacterPackageInfo packageInfo,
            CharacterDefinition definition,
            CharacterPackageData data)
        {
            if (definition.Mapping == null || string.IsNullOrWhiteSpace(definition.Mapping.MotionMappingFile))
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

            data.MotionMapping = LoadJsonFile<MotionMappingConfig>(path, "motion_mapping.json");
        }

        /// <summary>
        /// 读取 voice_config.json 的原始 JSON 文本。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <param name="definition">character.json 解析结果。</param>
        /// <param name="data">需要写入语音配置文本和路径信息的角色包数据对象。</param>
        private static void LoadVoiceConfig(
            CharacterPackageInfo packageInfo,
            CharacterDefinition definition,
            CharacterPackageData data)
        {
            if (definition.Voice == null || string.IsNullOrWhiteSpace(definition.Voice.VoiceConfigFile))
            {
                data.VoiceConfigJson = string.Empty;
                data.VoiceConfigAbsolutePath = string.Empty;
                return;
            }

            string path = packageInfo.ResolvePath(definition.Voice.VoiceConfigFile);
            data.VoiceConfigAbsolutePath = path;
            data.VoiceConfigJson = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }

        /// <summary>
        /// 读取并反序列化 JSON 文件。
        /// </summary>
        /// <typeparam name="T">目标 DTO 类型。</typeparam>
        /// <param name="path">JSON 文件路径。</param>
        /// <param name="displayName">用于错误提示的文件显示名。</param>
        /// <returns>反序列化后的对象。</returns>
        /// <exception cref="InvalidDataException">当 JSON 解析结果为空时抛出。</exception>
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
