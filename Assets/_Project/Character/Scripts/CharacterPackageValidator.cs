using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Project.Character
{
    /// <summary>
    /// 角色包校验器。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 职责：检查角色包是否满足当前 Unity 前端 MVP 的最低运行要求，包括 character.json、persona、
    /// model3.json、expression_mapping.json、motion_mapping.json 以及映射表中声明的资源文件。
    /// </para>
    /// <para>
    /// 本类不负责把角色加载进游戏，也不直接控制 Live2D。校验通过后，再交给
    /// <see cref="CharacterPackageLoader"/> 加载。
    /// </para>
    /// <para>
    /// 使用方式：
    /// <code>
    /// var validator = new CharacterPackageValidator();
    /// CharacterValidationResult result = validator.Validate(packageInfo);
    /// Debug.Log(result.ToString());
    /// </code>
    /// </para>
    /// <para>
    /// 对外暴露方法：<see cref="Validate"/>。
    /// </para>
    /// <para>
    /// TODO: 后续可以增加角色包 schemaVersion，并根据版本执行不同的校验规则。
    /// TODO: 后续可以增加 warning/error 的错误码，方便 DeveloperConsole 分类展示和论文实验统计。
    /// </para>
    /// </remarks>
    public class CharacterPackageValidator
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        /// 校验一个角色包。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <returns>返回校验结果，包含错误和警告。</returns>
        public CharacterValidationResult Validate(CharacterPackageInfo packageInfo)
        {
            var result = new CharacterValidationResult();

            if (packageInfo == null)
            {
                result.AddError("CharacterPackageInfo 为空。");
                return result;
            }

            result.PackageRootPath = packageInfo.PackageRootPath;
            result.CharacterId = packageInfo.PackageFolderName;

            if (!Directory.Exists(packageInfo.PackageRootPath))
            {
                result.AddError($"角色包目录不存在：{packageInfo.PackageRootPath}");
                return result;
            }

            if (!File.Exists(packageInfo.CharacterJsonPath))
            {
                result.AddError($"缺少 character.json：{packageInfo.CharacterJsonPath}");
                return result;
            }

            CharacterDefinition definition = TryLoadJson<CharacterDefinition>(
                packageInfo.CharacterJsonPath,
                result,
                "character.json");

            if (definition == null)
            {
                return result;
            }

            if (!string.IsNullOrWhiteSpace(definition.CharacterId))
            {
                result.CharacterId = definition.CharacterId;
            }

            ValidateBasicFields(definition, result);
            ValidateModel(packageInfo, definition, result);
            ValidatePersona(packageInfo, definition, result);
            ValidatePromptTemplate(packageInfo, definition, result);
            ValidateVoiceConfig(packageInfo, definition, result);
            ValidateExpressionMapping(packageInfo, definition, result);
            ValidateMotionMapping(packageInfo, definition, result);

            return result;
        }

        /// <summary>
        /// 校验 character.json 中的基础字段。
        /// </summary>
        /// <param name="definition">character.json 解析结果。</param>
        /// <param name="result">校验结果对象。</param>
        private static void ValidateBasicFields(CharacterDefinition definition, CharacterValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(definition.CharacterId))
            {
                result.AddError("character.json 缺少 characterId。");
            }

            if (string.IsNullOrWhiteSpace(definition.CharacterName))
            {
                result.AddWarning("character.json 缺少 characterName，DeveloperConsole 中可能只能显示 characterId。");
            }

            if (string.IsNullOrWhiteSpace(definition.ConfigVersion))
            {
                result.AddWarning("character.json 缺少 configVersion，不利于后续实验日志记录配置版本。");
            }
        }

        /// <summary>
        /// 校验 Live2D model3.json 配置和文件是否存在。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <param name="definition">character.json 解析结果。</param>
        /// <param name="result">校验结果对象。</param>
        private static void ValidateModel(
            CharacterPackageInfo packageInfo,
            CharacterDefinition definition,
            CharacterValidationResult result)
        {
            if (definition.Model == null)
            {
                result.AddError("character.json 缺少 model 配置。");
                return;
            }

            if (string.IsNullOrWhiteSpace(definition.Model.Model3JsonPath))
            {
                result.AddError("model.model3JsonPath 为空。");
                return;
            }

            string modelPath = packageInfo.ResolvePath(definition.Model.Model3JsonPath);
            if (!File.Exists(modelPath))
            {
                result.AddError($"Live2D model3.json 文件不存在：{modelPath}");
            }
        }

        /// <summary>
        /// 校验 persona.txt 配置和文件是否存在。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <param name="definition">character.json 解析结果。</param>
        /// <param name="result">校验结果对象。</param>
        private static void ValidatePersona(
            CharacterPackageInfo packageInfo,
            CharacterDefinition definition,
            CharacterValidationResult result)
        {
            if (definition.Persona == null)
            {
                result.AddError("character.json 缺少 persona 配置。");
                return;
            }

            if (string.IsNullOrWhiteSpace(definition.Persona.PersonaFile))
            {
                result.AddError("persona.personaFile 为空。");
                return;
            }

            string personaPath = packageInfo.ResolvePath(definition.Persona.PersonaFile);
            if (!File.Exists(personaPath))
            {
                result.AddError($"persona 文件不存在：{personaPath}");
            }
        }

        /// <summary>
        /// 校验 Prompt 模板文件是否存在。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <param name="definition">character.json 解析结果。</param>
        /// <param name="result">校验结果对象。</param>
        /// <remarks>
        /// 当前阶段 Prompt 模板缺失只作为警告，因为后续也可能完全由后端 PromptManager 管理。
        /// </remarks>
        private static void ValidatePromptTemplate(
            CharacterPackageInfo packageInfo,
            CharacterDefinition definition,
            CharacterValidationResult result)
        {
            string promptTemplateFile = "prompt_template.txt";

            if (definition.Persona != null && !string.IsNullOrWhiteSpace(definition.Persona.PromptTemplateFile))
            {
                promptTemplateFile = definition.Persona.PromptTemplateFile;
            }

            string promptPath = packageInfo.ResolvePath(promptTemplateFile);
            if (!File.Exists(promptPath))
            {
                result.AddWarning($"未找到 prompt_template.txt：{promptPath}。如果 Prompt 完全由后端管理，可以暂时忽略。");
            }
        }

        /// <summary>
        /// 校验语音配置文件是否存在。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <param name="definition">character.json 解析结果。</param>
        /// <param name="result">校验结果对象。</param>
        /// <remarks>
        /// MVP 阶段语音配置缺失只作为警告，因为当前前端尚未真正调用 TTS。
        /// </remarks>
        private static void ValidateVoiceConfig(
            CharacterPackageInfo packageInfo,
            CharacterDefinition definition,
            CharacterValidationResult result)
        {
            if (definition.Voice == null || string.IsNullOrWhiteSpace(definition.Voice.VoiceConfigFile))
            {
                result.AddWarning("未配置 voice.voiceConfigFile。MVP 阶段可以暂时忽略。");
                return;
            }

            string voicePath = packageInfo.ResolvePath(definition.Voice.VoiceConfigFile);
            if (!File.Exists(voicePath))
            {
                result.AddWarning($"voice_config.json 文件不存在：{voicePath}。MVP 阶段可以暂时忽略。");
            }
        }

        /// <summary>
        /// 校验 expression_mapping.json 以及其中声明的表情文件。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <param name="definition">character.json 解析结果。</param>
        /// <param name="result">校验结果对象。</param>
        private static void ValidateExpressionMapping(
            CharacterPackageInfo packageInfo,
            CharacterDefinition definition,
            CharacterValidationResult result)
        {
            if (definition.Mapping == null)
            {
                result.AddError("character.json 缺少 mapping 配置。");
                return;
            }

            if (string.IsNullOrWhiteSpace(definition.Mapping.ExpressionMappingFile))
            {
                result.AddError("mapping.expressionMappingFile 为空。");
                return;
            }

            string mappingPath = packageInfo.ResolvePath(definition.Mapping.ExpressionMappingFile);
            if (!File.Exists(mappingPath))
            {
                result.AddError($"expression_mapping.json 不存在：{mappingPath}");
                return;
            }

            ExpressionMappingConfig config = TryLoadJson<ExpressionMappingConfig>(
                mappingPath,
                result,
                "expression_mapping.json");

            if (config == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(config.DefaultEmotion))
            {
                result.AddError("expression_mapping.json 缺少 defaultEmotion。");
            }

            if (config.EmotionMappings == null || config.EmotionMappings.Count == 0)
            {
                result.AddError("expression_mapping.json 中 emotionMappings 为空。");
                return;
            }

            if (!string.IsNullOrWhiteSpace(config.DefaultEmotion) && !config.EmotionMappings.ContainsKey(config.DefaultEmotion))
            {
                result.AddError($"defaultEmotion={config.DefaultEmotion} 未出现在 emotionMappings 中。");
            }

            foreach (KeyValuePair<string, ExpressionMappingEntry> pair in config.EmotionMappings)
            {
                string emotion = pair.Key;
                ExpressionMappingEntry entry = pair.Value;

                if (string.IsNullOrWhiteSpace(emotion))
                {
                    result.AddError("emotionMappings 中存在空 emotion key。");
                    continue;
                }

                if (entry == null)
                {
                    result.AddError($"emotion={emotion} 的映射对象为空。");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Expression))
                {
                    // neutral 允许为空字符串，表示“不绑定 exp3.json，恢复模型默认表情”。
                    // 这里记为 warning 而不是 error，避免合法的“无表情”配置被判定为角色包不可用。
                    result.AddWarning($"emotion={emotion} 的 expression 为空。若该 emotion 表示无表情/默认脸，可以忽略。");
                    continue;
                }

                if (!TryResolveExpressionFile(packageInfo, entry.Expression, out _))
                {
                    result.AddError($"emotion={emotion} 指向的表情文件不存在：{entry.Expression}");
                }
            }

            ValidateFallbackMap(
                config.FallbackEmotionMap,
                config.EmotionMappings,
                "fallbackEmotionMap",
                result);
        }

        /// <summary>
        /// 校验 motion_mapping.json 以及其中声明的动作文件。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <param name="definition">character.json 解析结果。</param>
        /// <param name="result">校验结果对象。</param>
        private static void ValidateMotionMapping(
            CharacterPackageInfo packageInfo,
            CharacterDefinition definition,
            CharacterValidationResult result)
        {
            if (definition.Mapping == null)
            {
                result.AddError("character.json 缺少 mapping 配置。");
                return;
            }

            if (string.IsNullOrWhiteSpace(definition.Mapping.MotionMappingFile))
            {
                result.AddError("mapping.motionMappingFile 为空。");
                return;
            }

            string mappingPath = packageInfo.ResolvePath(definition.Mapping.MotionMappingFile);
            if (!File.Exists(mappingPath))
            {
                result.AddError($"motion_mapping.json 不存在：{mappingPath}");
                return;
            }

            MotionMappingConfig config = TryLoadJson<MotionMappingConfig>(
                mappingPath,
                result,
                "motion_mapping.json");

            if (config == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(config.DefaultMotion))
            {
                result.AddError("motion_mapping.json 缺少 defaultMotion。");
            }

            if (config.ActionMappings == null || config.ActionMappings.Count == 0)
            {
                result.AddError("motion_mapping.json 中 actionMappings 为空。");
                return;
            }

            if (!string.IsNullOrWhiteSpace(config.DefaultMotion) && !ContainsMotionFile(config.ActionMappings, config.DefaultMotion))
            {
                result.AddWarning($"defaultMotion={config.DefaultMotion} 没有被任何 actionMappings 引用。");
            }

            foreach (KeyValuePair<string, MotionMappingEntry> pair in config.ActionMappings)
            {
                string action = pair.Key;
                MotionMappingEntry entry = pair.Value;

                if (string.IsNullOrWhiteSpace(action))
                {
                    result.AddError("actionMappings 中存在空 action key。");
                    continue;
                }

                if (entry == null)
                {
                    result.AddError($"action={action} 的映射对象为空。");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Motion))
                {
                    result.AddError($"action={action} 的 motion 为空。");
                    continue;
                }

                if (!TryResolveMotionFile(packageInfo, entry.Motion, out _))
                {
                    result.AddError($"action={action} 指向的动作文件不存在：{entry.Motion}");
                }
            }

            ValidateFallbackMap(
                config.FallbackActionMap,
                config.ActionMappings,
                "fallbackActionMap",
                result);
        }

        /// <summary>
        /// 校验 fallback 映射表的目标标签是否存在于主映射表中。
        /// </summary>
        /// <typeparam name="TEntry">主映射表的值类型。</typeparam>
        /// <param name="fallbackMap">fallback 映射表，例如 excited -&gt; happy。</param>
        /// <param name="targetMappings">主映射表。</param>
        /// <param name="mapName">用于日志输出的映射表名称。</param>
        /// <param name="result">校验结果对象。</param>
        private static void ValidateFallbackMap<TEntry>(
            Dictionary<string, string> fallbackMap,
            Dictionary<string, TEntry> targetMappings,
            string mapName,
            CharacterValidationResult result)
        {
            if (fallbackMap == null || fallbackMap.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<string, string> pair in fallbackMap)
            {
                string sourceTag = pair.Key;
                string targetTag = pair.Value;

                if (string.IsNullOrWhiteSpace(sourceTag))
                {
                    result.AddWarning($"{mapName} 中存在空的来源标签。");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(targetTag))
                {
                    result.AddWarning($"{mapName} 中 {sourceTag} 的目标标签为空。");
                    continue;
                }

                if (targetMappings == null || !targetMappings.ContainsKey(targetTag))
                {
                    result.AddWarning($"{mapName} 中 {sourceTag} -> {targetTag}，但目标标签不存在。");
                }
            }
        }

        /// <summary>
        /// 判断动作映射表中是否存在指定 motion 文件。
        /// </summary>
        /// <param name="actionMappings">动作映射表。</param>
        /// <param name="motionFileName">要查找的 motion 文件名。</param>
        /// <returns>存在返回 true，否则返回 false。</returns>
        private static bool ContainsMotionFile(Dictionary<string, MotionMappingEntry> actionMappings, string motionFileName)
        {
            foreach (KeyValuePair<string, MotionMappingEntry> pair in actionMappings)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                if (string.Equals(pair.Value.Motion, motionFileName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 解析表情文件路径。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <param name="expressionPathOrFileName">表情文件名或角色包内相对路径。</param>
        /// <param name="resolvedPath">解析后的候选路径。</param>
        /// <returns>文件存在返回 true，否则返回 false。</returns>
        private static bool TryResolveExpressionFile(
            CharacterPackageInfo packageInfo,
            string expressionPathOrFileName,
            out string resolvedPath)
        {
            return TryResolveResourceFile(
                packageInfo,
                expressionPathOrFileName,
                "live2d/expressions",
                out resolvedPath);
        }

        /// <summary>
        /// 解析动作文件路径。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <param name="motionPathOrFileName">动作文件名或角色包内相对路径。</param>
        /// <param name="resolvedPath">解析后的候选路径。</param>
        /// <returns>文件存在返回 true，否则返回 false。</returns>
        private static bool TryResolveMotionFile(
            CharacterPackageInfo packageInfo,
            string motionPathOrFileName,
            out string resolvedPath)
        {
            return TryResolveResourceFile(
                packageInfo,
                motionPathOrFileName,
                "live2d/motions",
                out resolvedPath);
        }

        /// <summary>
        /// 解析角色包内资源文件路径。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <param name="pathOrFileName">文件名、相对路径或绝对路径。</param>
        /// <param name="standardRelativeFolder">标准相对目录，例如 <c>live2d/expressions</c>。</param>
        /// <param name="resolvedPath">解析出的候选路径。即使文件不存在，也会返回第一个候选路径，便于错误提示。</param>
        /// <returns>找到真实文件返回 true，否则返回 false。</returns>
        /// <remarks>
        /// 支持两种常见写法：
        /// 1. <c>happy.exp3.json</c>：自动尝试标准目录。
        /// 2. <c>live2d/expressions/happy.exp3.json</c>：按角色包根目录下相对路径解析。
        /// </remarks>
        private static bool TryResolveResourceFile(
            CharacterPackageInfo packageInfo,
            string pathOrFileName,
            string standardRelativeFolder,
            out string resolvedPath)
        {
            if (packageInfo == null)
            {
                resolvedPath = string.Empty;
                return false;
            }

            return CharacterPackagePathResolver.TryResolveResourceFile(
                packageInfo.PackageRootPath,
                pathOrFileName,
                standardRelativeFolder,
                out resolvedPath);
        }

        /// <summary>
        /// 尝试读取并反序列化 JSON 文件，把错误写入校验结果。
        /// </summary>
        /// <typeparam name="T">目标 DTO 类型。</typeparam>
        /// <param name="path">JSON 文件路径。</param>
        /// <param name="result">校验结果对象。</param>
        /// <param name="displayName">用于错误提示的文件显示名。</param>
        /// <returns>解析成功返回对象；失败返回 null。</returns>
        private static T TryLoadJson<T>(
            string path,
            CharacterValidationResult result,
            string displayName) where T : class
        {
            try
            {
                string json = File.ReadAllText(path);
                T data = JsonConvert.DeserializeObject<T>(json, JsonSettings);

                if (data == null)
                {
                    result.AddError($"{displayName} 解析结果为空：{path}");
                    return null;
                }

                return data;
            }
            catch (Exception ex)
            {
                result.AddError($"{displayName} 解析失败：{path}，错误：{ex.Message}");
                return null;
            }
        }
    }
}
