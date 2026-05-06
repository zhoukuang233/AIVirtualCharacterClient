using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Project.Presentation
{
    /// <summary>
    /// 行为映射解析器。
    ///
    /// 负责把 LLM 输出的语义标签：
    /// emotion = happy
    /// action = greet
    ///
    /// 解析成当前角色包中的具体表现资源：
    /// happy.exp3.json
    /// wave.motion3.json
    ///
    /// 注意：
    /// 1. 本类不调用 Live2D。
    /// 2. 本类不关心 LLM。
    /// 3. 本类只做“语义标签 -> 角色资源文件”的转换。
    /// </summary>
    public class BehaviorMappingResolver
    {
        private readonly string _characterRootPath;
        private readonly string _expressionMappingPath;
        private readonly string _motionMappingPath;

        private ExpressionMappingConfig _expressionConfig;
        private MotionMappingConfig _motionConfig;

        public BehaviorMappingResolver(
            string characterRootPath,
            string expressionMappingFileName = "expression_mapping.json",
            string motionMappingFileName = "motion_mapping.json")
        {
            _characterRootPath = characterRootPath;
            _expressionMappingPath = Path.Combine(characterRootPath, expressionMappingFileName);
            _motionMappingPath = Path.Combine(characterRootPath, motionMappingFileName);

            Reload();
        }

        /// <summary>
        /// 重新读取当前角色的映射表。
        /// 切换角色后，应重新创建 Resolver 或调用 Reload。
        /// </summary>
        public void Reload()
        {
            _expressionConfig = LoadJsonConfig<ExpressionMappingConfig>(_expressionMappingPath);
            _motionConfig = LoadJsonConfig<MotionMappingConfig>(_motionMappingPath);

            if (_expressionConfig == null)
            {
                _expressionConfig = new ExpressionMappingConfig();
                Debug.LogWarning($"[BehaviorMappingResolver] 表情映射表读取失败，将使用空配置：{_expressionMappingPath}");
            }

            if (_motionConfig == null)
            {
                _motionConfig = new MotionMappingConfig();
                Debug.LogWarning($"[BehaviorMappingResolver] 动作映射表读取失败，将使用空配置：{_motionMappingPath}");
            }
        }

        /// <summary>
        /// 核心入口：
        /// 输入 emotion/action/voiceStyle，输出 PresentationResolveResult。
        /// </summary>
        public PresentationResolveResult Resolve(string emotion, string action, string voiceStyle = "normal")
        {
            var result = new PresentationResolveResult
            {
                InputEmotion = NormalizeTag(emotion),
                InputAction = NormalizeTag(action),
                InputVoiceStyle = NormalizeTag(voiceStyle),
                ResolvedVoiceStyle = string.IsNullOrWhiteSpace(voiceStyle) ? "normal" : NormalizeTag(voiceStyle)
            };

            ResolveExpression(result);
            ResolveMotion(result);

            Debug.Log(result.ToDebugText());

            return result;
        }

        private void ResolveExpression(PresentationResolveResult result)
        {
            string requestedEmotion = result.InputEmotion;

            if (string.IsNullOrWhiteSpace(requestedEmotion))
            {
                requestedEmotion = NormalizeTag(_expressionConfig.defaultEmotion);
                result.ExpressionFallbackUsed = true;
                result.ExpressionFallbackReason = "输入 emotion 为空，使用 defaultEmotion。";
            }

            ExpressionMappingItem item;
            string actualEmotion;

            // 1. 直接命中 emotionMappings。
            if (TryGetExpressionItem(requestedEmotion, out actualEmotion, out item))
            {
                FillExpressionResult(result, actualEmotion, item);
                return;
            }

            // 2. 查 fallbackEmotionMap，例如 excited -> happy。
            string fallbackEmotion;
            if (TryGetFallbackTarget(requestedEmotion, _expressionConfig.fallbackEmotionMap, out fallbackEmotion))
            {
                if (TryGetExpressionItem(fallbackEmotion, out actualEmotion, out item))
                {
                    result.ExpressionFallbackUsed = true;
                    result.ExpressionFallbackReason = $"emotion={requestedEmotion} 未命中，使用 fallbackEmotionMap 映射到 {actualEmotion}。";
                    FillExpressionResult(result, actualEmotion, item);
                    return;
                }
            }

            // 3. 使用 defaultEmotion，例如 neutral。
            string defaultEmotion = NormalizeTag(_expressionConfig.defaultEmotion);
            if (TryGetExpressionItem(defaultEmotion, out actualEmotion, out item))
            {
                result.ExpressionFallbackUsed = true;
                result.ExpressionFallbackReason = $"emotion={requestedEmotion} 未命中，使用 defaultEmotion={actualEmotion}。";
                FillExpressionResult(result, actualEmotion, item);
                return;
            }

            // 4. 如果配置了 defaultExpression，则直接使用文件名。
            if (!string.IsNullOrWhiteSpace(_expressionConfig.defaultExpression))
            {
                result.ResolvedEmotion = defaultEmotion;
                result.ExpressionFileName = _expressionConfig.defaultExpression;
                result.ExpressionFilePath = BuildExpressionPath(_expressionConfig.defaultExpression);
                result.ExpressionPriority = 0;
                result.ExpressionFallbackUsed = true;
                result.ExpressionFallbackReason = $"emotion={requestedEmotion} 未命中，且 defaultEmotion 无映射，直接使用 defaultExpression。";
                result.ExpressionFileExists = File.Exists(result.ExpressionFilePath);
                return;
            }

            // 5. 完全解析失败。
            result.ExpressionFallbackUsed = true;
            result.ExpressionFallbackReason = $"emotion={requestedEmotion} 未命中，并且没有可用默认表情。";
            Debug.LogWarning($"[BehaviorMappingResolver] {result.ExpressionFallbackReason}");
        }

        private void ResolveMotion(PresentationResolveResult result)
        {
            string requestedAction = result.InputAction;

            if (string.IsNullOrWhiteSpace(requestedAction))
            {
                requestedAction = "idle";
                result.MotionFallbackUsed = true;
                result.MotionFallbackReason = "输入 action 为空，尝试使用 idle。";
            }

            MotionMappingItem item;
            string actualAction;

            // 1. 直接命中 actionMappings。
            if (TryGetMotionItem(requestedAction, out actualAction, out item))
            {
                FillMotionResult(result, actualAction, item);
                return;
            }

            // 2. 查 fallbackActionMap，例如 hello -> greet。
            string fallbackAction;
            if (TryGetFallbackTarget(requestedAction, _motionConfig.fallbackActionMap, out fallbackAction))
            {
                if (TryGetMotionItem(fallbackAction, out actualAction, out item))
                {
                    result.MotionFallbackUsed = true;
                    result.MotionFallbackReason = $"action={requestedAction} 未命中，使用 fallbackActionMap 映射到 {actualAction}。";
                    FillMotionResult(result, actualAction, item);
                    return;
                }
            }

            // 3. 优先尝试 idle。
            if (TryGetMotionItem("idle", out actualAction, out item))
            {
                result.MotionFallbackUsed = true;
                result.MotionFallbackReason = $"action={requestedAction} 未命中，使用 idle。";
                FillMotionResult(result, actualAction, item);
                return;
            }

            // 4. 使用 defaultMotion 文件名。
            if (!string.IsNullOrWhiteSpace(_motionConfig.defaultMotion))
            {
                if (TryFindMotionItemByFileName(_motionConfig.defaultMotion, out actualAction, out item))
                {
                    result.MotionFallbackUsed = true;
                    result.MotionFallbackReason = $"action={requestedAction} 未命中，使用 defaultMotion={_motionConfig.defaultMotion}。";
                    FillMotionResult(result, actualAction, item);
                    return;
                }

                result.ResolvedAction = "default";
                result.MotionFileName = _motionConfig.defaultMotion;
                result.MotionFilePath = BuildMotionPath(_motionConfig.defaultMotion);
                result.MotionLoop = true;
                result.MotionPriority = 0;
                result.MotionFallbackUsed = true;
                result.MotionFallbackReason = $"action={requestedAction} 未命中，直接使用 defaultMotion 文件名。";
                result.MotionFileExists = File.Exists(result.MotionFilePath);
                return;
            }

            // 5. 完全解析失败。
            result.MotionFallbackUsed = true;
            result.MotionFallbackReason = $"action={requestedAction} 未命中，并且没有可用默认动作。";
            Debug.LogWarning($"[BehaviorMappingResolver] {result.MotionFallbackReason}");
        }

        private void FillExpressionResult(
            PresentationResolveResult result,
            string resolvedEmotion,
            ExpressionMappingItem item)
        {
            result.ResolvedEmotion = resolvedEmotion;
            result.ExpressionFileName = item.expression;
            result.ExpressionFilePath = BuildExpressionPath(item.expression);
            result.ExpressionPriority = item.priority;
            result.ExpressionFileExists = File.Exists(result.ExpressionFilePath);

            if (!result.ExpressionFileExists)
            {
                Debug.LogWarning($"[BehaviorMappingResolver] 表情文件不存在：{result.ExpressionFilePath}");
            }
        }

        private void FillMotionResult(
            PresentationResolveResult result,
            string resolvedAction,
            MotionMappingItem item)
        {
            result.ResolvedAction = resolvedAction;
            result.MotionFileName = item.motion;
            result.MotionFilePath = BuildMotionPath(item.motion);
            result.MotionLoop = item.loop;
            result.MotionPriority = item.priority;
            result.MotionFileExists = File.Exists(result.MotionFilePath);

            if (!result.MotionFileExists)
            {
                Debug.LogWarning($"[BehaviorMappingResolver] 动作文件不存在：{result.MotionFilePath}");
            }
        }

        /// <summary>
        /// 构建表情文件路径。
        ///
        /// 优先使用标准路径：
        /// characterRoot/live2d/expressions/xxx.exp3.json
        ///
        /// 如果标准路径不存在，则在 live2d 目录下递归搜索同名文件。
        /// 这样可以兼容不同 Live2D 模型导出的目录结构。
        /// </summary>
        private string BuildExpressionPath(string expressionFileName)
        {
            string standardPath = Path.Combine(_characterRootPath, "live2d", "expressions", expressionFileName);

            if (File.Exists(standardPath))
            {
                return standardPath;
            }

            string live2dRootPath = Path.Combine(_characterRootPath, "live2d");
            string foundPath = FindFileRecursively(live2dRootPath, expressionFileName);

            return string.IsNullOrWhiteSpace(foundPath) ? standardPath : foundPath;
        }

        /// <summary>
        /// 构建动作文件路径。
        ///
        /// 优先使用标准路径：
        /// characterRoot/live2d/motions/xxx.motion3.json
        ///
        /// 如果标准路径不存在，则在 live2d 目录下递归搜索同名文件。
        /// 当前阶段只用于 Debug 和文件存在性检查，不负责真实播放 Live2D。
        /// </summary>
        private string BuildMotionPath(string motionFileName)
        {
            string standardPath = Path.Combine(_characterRootPath, "live2d", "animations", motionFileName);

            if (File.Exists(standardPath))
            {
                return standardPath;
            }

            string live2dRootPath = Path.Combine(_characterRootPath, "live2d");
            string foundPath = FindFileRecursively(live2dRootPath, motionFileName);

            return string.IsNullOrWhiteSpace(foundPath) ? standardPath : foundPath;
        }

        /// <summary>
        /// 在指定根目录下递归查找文件。
        ///
        /// 参数：
        /// rootPath：要搜索的根目录。
        /// fileName：要查找的文件名，例如 qizi.motion3.json。
        ///
        /// 返回：
        /// 如果找到，返回完整路径；如果找不到，返回 null。
        /// </summary>
        private string FindFileRecursively(string rootPath, string fileName)
        {
            if (string.IsNullOrWhiteSpace(rootPath) || string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            if (!Directory.Exists(rootPath))
            {
                return null;
            }

            string[] files = Directory.GetFiles(rootPath, fileName, SearchOption.AllDirectories);

            if (files == null || files.Length == 0)
            {
                return null;
            }

            return files[0];
        }

        private bool TryGetExpressionItem(
            string emotion,
            out string actualEmotion,
            out ExpressionMappingItem item)
        {
            actualEmotion = null;
            item = null;

            if (_expressionConfig == null || _expressionConfig.emotionMappings == null)
            {
                return false;
            }

            foreach (KeyValuePair<string, ExpressionMappingItem> pair in _expressionConfig.emotionMappings)
            {
                if (string.Equals(pair.Key, emotion, StringComparison.OrdinalIgnoreCase))
                {
                    actualEmotion = pair.Key;
                    item = pair.Value;
                    return item != null && !string.IsNullOrWhiteSpace(item.expression);
                }
            }

            return false;
        }

        private bool TryGetMotionItem(
            string action,
            out string actualAction,
            out MotionMappingItem item)
        {
            actualAction = null;
            item = null;

            if (_motionConfig == null || _motionConfig.actionMappings == null)
            {
                return false;
            }

            foreach (KeyValuePair<string, MotionMappingItem> pair in _motionConfig.actionMappings)
            {
                if (string.Equals(pair.Key, action, StringComparison.OrdinalIgnoreCase))
                {
                    actualAction = pair.Key;
                    item = pair.Value;
                    return item != null && !string.IsNullOrWhiteSpace(item.motion);
                }
            }

            return false;
        }

        private bool TryFindMotionItemByFileName(
            string motionFileName,
            out string actualAction,
            out MotionMappingItem item)
        {
            actualAction = null;
            item = null;

            if (_motionConfig == null || _motionConfig.actionMappings == null)
            {
                return false;
            }

            foreach (KeyValuePair<string, MotionMappingItem> pair in _motionConfig.actionMappings)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                if (string.Equals(pair.Value.motion, motionFileName, StringComparison.OrdinalIgnoreCase))
                {
                    actualAction = pair.Key;
                    item = pair.Value;
                    return true;
                }
            }

            return false;
        }

        private bool TryGetFallbackTarget(
            string inputTag,
            Dictionary<string, string> fallbackMap,
            out string fallbackTarget)
        {
            fallbackTarget = null;

            if (fallbackMap == null)
            {
                return false;
            }

            foreach (KeyValuePair<string, string> pair in fallbackMap)
            {
                if (string.Equals(pair.Key, inputTag, StringComparison.OrdinalIgnoreCase))
                {
                    fallbackTarget = NormalizeTag(pair.Value);
                    return !string.IsNullOrWhiteSpace(fallbackTarget);
                }
            }

            return false;
        }

        private static T LoadJsonConfig<T>(string path) where T : class
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[BehaviorMappingResolver] JSON 文件不存在：{path}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[BehaviorMappingResolver] JSON 解析失败：{path}\n{exception}");
                return null;
            }
        }

        private static string NormalizeTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return string.Empty;
            }

            return tag.Trim().ToLowerInvariant();
        }

        private class ExpressionMappingConfig
        {
            public string defaultEmotion = "neutral";

            // 默认表情文件。
            //
            // 注意：
            // 对于 Cubism Live2D 来说，无表情状态可以不绑定任何 exp3.json。
            // 因此这里不能默认写死 normal.exp3.json。
            // 空字符串表示：恢复模型默认表情状态。
            public string defaultExpression = string.Empty;

            public Dictionary<string, ExpressionMappingItem> emotionMappings =
                new Dictionary<string, ExpressionMappingItem>();

            // 可选字段：例如 excited -> happy。
            public Dictionary<string, string> fallbackEmotionMap =
                new Dictionary<string, string>();
        }

        private class ExpressionMappingItem
        {
            // 表情文件名。
            //
            // 允许为空字符串。
            // 空字符串表示：不使用任何 exp3.json，恢复 Live2D 模型默认表情状态。
            public string expression = string.Empty;

            public int priority;

            // 映射说明字段。
            // 主要用于开发者控制台展示、调试和论文说明，不参与核心解析逻辑。
            public string description;
        }

        private class MotionMappingConfig
        {
            // 默认动作文件。
            //
            // 不建议在代码中写死 idle.motion3.json。
            // 不同 Live2D 模型的默认动作命名可能不同，例如你的模型是 Scene1.motion3.json。
            public string defaultMotion = string.Empty;

            public Dictionary<string, MotionMappingItem> actionMappings =
                new Dictionary<string, MotionMappingItem>();

            // 可选字段：例如 hello -> greet。
            public Dictionary<string, string> fallbackActionMap =
                new Dictionary<string, string>();
        }

        private class MotionMappingItem
        {
            public string motion;
            public bool loop;
            public int priority;

            // 映射说明字段。
            // 主要用于开发者控制台展示、调试和论文说明，不参与核心解析逻辑。
            public string description;
        }
    }
}