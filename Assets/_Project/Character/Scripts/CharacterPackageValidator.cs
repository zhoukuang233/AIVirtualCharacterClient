using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Project.Character
{
    /// <summary>
    /// 角色包校验器。
    ///
    /// 职责：
    /// 1. 检查 character.json 是否存在并可解析。
    /// 2. 检查 persona、model3.json、mapping 文件是否存在。
    /// 3. 检查 expression_mapping.json 中声明的表情文件是否真实存在。
    /// 4. 检查 motion_mapping.json 中声明的动作文件是否真实存在。
    /// 5. 检查 defaultEmotion / defaultMotion 是否合理。
    ///
    /// 注意：
    /// Validator 不负责加载角色进入游戏，只负责报告角色包是否完整。
    /// </summary>
    public class CharacterPackageValidator
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

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

        private static void ValidateBasicFields(
            CharacterDefinition definition,
            CharacterValidationResult result)
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

        private static void ValidatePromptTemplate(
            CharacterPackageInfo packageInfo,
            CharacterDefinition definition,
            CharacterValidationResult result)
        {
            string promptTemplateFile = "prompt_template.txt";

            if (definition.Persona != null &&
                !string.IsNullOrWhiteSpace(definition.Persona.PromptTemplateFile))
            {
                promptTemplateFile = definition.Persona.PromptTemplateFile;
            }

            string promptPath = packageInfo.ResolvePath(promptTemplateFile);

            if (!File.Exists(promptPath))
            {
                result.AddWarning($"未找到 prompt_template.txt：{promptPath}。如果 Prompt 完全由后端管理，可以暂时忽略。");
            }
        }

        private static void ValidateVoiceConfig(
            CharacterPackageInfo packageInfo,
            CharacterDefinition definition,
            CharacterValidationResult result)
        {
            if (definition.Voice == null ||
                string.IsNullOrWhiteSpace(definition.Voice.VoiceConfigFile))
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

            if (!string.IsNullOrWhiteSpace(config.DefaultEmotion) &&
                !config.EmotionMappings.ContainsKey(config.DefaultEmotion))
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
                    result.AddError($"emotion={emotion} 的 expression 为空。");
                    continue;
                }

                string resolvedExpressionPath;

                if (!TryResolveExpressionFile(packageInfo, entry.Expression, out resolvedExpressionPath))
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

            if (!string.IsNullOrWhiteSpace(config.DefaultMotion) &&
                !ContainsMotionFile(config.ActionMappings, config.DefaultMotion))
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

                string resolvedMotionPath;

                if (!TryResolveMotionFile(packageInfo, entry.Motion, out resolvedMotionPath))
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

        private static void ValidateFallbackMap<T>(
            Dictionary<string, string> fallbackMap,
            Dictionary<string, T> targetMappings,
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

        private static bool ContainsMotionFile(
            Dictionary<string, MotionMappingEntry> actionMappings,
            string motionFileName)
        {
            foreach (KeyValuePair<string, MotionMappingEntry> pair in actionMappings)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                if (pair.Value.Motion == motionFileName)
                {
                    return true;
                }
            }

            return false;
        }

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
        /// 解析资源文件路径。
        ///
        /// 支持两种写法：
        /// 1. "happy.exp3.json"
        ///    自动尝试 live2d/expressions/happy.exp3.json
        ///
        /// 2. "live2d/expressions/happy.exp3.json"
        ///    直接按角色包根目录下的相对路径解析
        /// </summary>
        private static bool TryResolveResourceFile(
            CharacterPackageInfo packageInfo,
            string pathOrFileName,
            string standardRelativeFolder,
            out string resolvedPath)
        {
            resolvedPath = string.Empty;

            if (string.IsNullOrWhiteSpace(pathOrFileName))
            {
                return false;
            }

            var candidates = new List<string>();

            if (Path.IsPathRooted(pathOrFileName))
            {
                candidates.Add(Path.GetFullPath(pathOrFileName));
            }
            else
            {
                candidates.Add(packageInfo.ResolvePath(pathOrFileName));
                candidates.Add(packageInfo.ResolvePath($"{standardRelativeFolder}/{pathOrFileName}"));
            }

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    resolvedPath = candidate;
                    return true;
                }
            }

            resolvedPath = candidates.Count > 0 ? candidates[0] : string.Empty;
            return false;
        }

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