using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Project.Presentation
{
    /// <summary>
    /// 行为映射解析器。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该类负责把 LLM 或测试工具输出的语义标签转换成当前角色包中的具体表现资源。
    /// 例如：<c>emotion=happy</c> 解析成 <c>happy.exp3.json</c>，
    /// <c>action=greet</c> 解析成 <c>wave.motion3.json</c>。
    /// </para>
    /// <para>
    /// 重要边界：
    /// 1. 本类不调用 Live2D。
    /// 2. 本类不调用 LLM。
    /// 3. 本类只做“语义标签 -> 角色资源文件”的转换。
    /// </para>
    /// <para>
    /// 使用方式：
    /// <code>
    /// var resolver = new BehaviorMappingResolver(characterRootPath);
    /// PresentationResolveResult result = resolver.Resolve("happy", "greet", "cheerful");
    /// List&lt;PresentationCommand&gt; commands = result.ToCommands();
    /// </code>
    /// </para>
    /// <para>
    /// 对外暴露方法：<see cref="Reload"/> 和 <see cref="Resolve"/>。
    /// </para>
    /// <para>
    /// TODO: 后续可以把内部 MappingConfig DTO 复用 Character 模块中的 DTO，避免两套结构重复维护。
    /// TODO: 后续可以增加映射表热重载，用于 DeveloperConsole 修改 JSON 后立即刷新表现结果。
    /// TODO: 后续可以把 Debug.Log 改为结构化日志事件，交给 ExperimentLogging 统一记录。
    /// </para>
    /// </remarks>
    public class BehaviorMappingResolver
    {
        private readonly string _characterRootPath;
        private readonly string _expressionMappingPath;
        private readonly string _motionMappingPath;

        private ExpressionMappingConfig _expressionConfig;
        private MotionMappingConfig _motionConfig;

        /// <summary>
        /// 创建行为映射解析器，并立即读取当前角色的表情和动作映射表。
        /// </summary>
        /// <param name="characterRootPath">角色包根目录。</param>
        /// <param name="expressionMappingFileName">表情映射表文件名，默认 expression_mapping.json。</param>
        /// <param name="motionMappingFileName">动作映射表文件名，默认 motion_mapping.json。</param>
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
        /// 重新读取当前角色的表情和动作映射表。
        /// </summary>
        /// <remarks>
        /// 切换角色后，可以重新创建 Resolver，也可以在角色包路径不变、映射表内容变化时调用该方法。
        /// </remarks>
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
        /// 解析一次表现标签。
        /// </summary>
        /// <param name="emotion">输入 emotion 标签，例如 happy、sad、neutral。</param>
        /// <param name="action">输入 action 标签，例如 greet、idle、deny。</param>
        /// <param name="voiceStyle">输入 voiceStyle 标签；为空时使用 normal。</param>
        /// <returns>
        /// 返回 <see cref="PresentationResolveResult"/>，其中包含输入标签、解析后标签、资源文件路径、fallback 信息和文件存在性。
        /// </returns>
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

        /// <summary>
        /// 解析表情标签。
        /// </summary>
        /// <param name="result">需要写入表情解析结果的对象。</param>
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

            if (TryGetExpressionItem(requestedEmotion, out actualEmotion, out item))
            {
                FillExpressionResult(result, actualEmotion, item);
                return;
            }

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

            string defaultEmotion = NormalizeTag(_expressionConfig.defaultEmotion);
            if (TryGetExpressionItem(defaultEmotion, out actualEmotion, out item))
            {
                result.ExpressionFallbackUsed = true;
                result.ExpressionFallbackReason = $"emotion={requestedEmotion} 未命中，使用 defaultEmotion={actualEmotion}。";
                FillExpressionResult(result, actualEmotion, item);
                return;
            }

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

            result.ExpressionFallbackUsed = true;
            result.ExpressionFallbackReason = $"emotion={requestedEmotion} 未命中，并且没有可用默认表情。";
            Debug.LogWarning($"[BehaviorMappingResolver] {result.ExpressionFallbackReason}");
        }

        /// <summary>
        /// 解析动作标签。
        /// </summary>
        /// <param name="result">需要写入动作解析结果的对象。</param>
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

            if (TryGetMotionItem(requestedAction, out actualAction, out item))
            {
                FillMotionResult(result, actualAction, item);
                return;
            }

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

            if (TryGetMotionItem("idle", out actualAction, out item))
            {
                result.MotionFallbackUsed = true;
                result.MotionFallbackReason = $"action={requestedAction} 未命中，使用 idle。";
                FillMotionResult(result, actualAction, item);
                return;
            }

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

            result.MotionFallbackUsed = true;
            result.MotionFallbackReason = $"action={requestedAction} 未命中，并且没有可用默认动作。";
            Debug.LogWarning($"[BehaviorMappingResolver] {result.MotionFallbackReason}");
        }

        /// <summary>
        /// 把表情映射项写入解析结果。
        /// </summary>
        /// <param name="result">解析结果对象。</param>
        /// <param name="resolvedEmotion">实际命中的 emotion 标签。</param>
        /// <param name="item">表情映射项。</param>
        private void FillExpressionResult(
            PresentationResolveResult result,
            string resolvedEmotion,
            ExpressionMappingItem item)
        {
            result.ResolvedEmotion = resolvedEmotion;
            result.ExpressionFileName = item.expression;
            result.ExpressionPriority = item.priority;

            if (string.IsNullOrWhiteSpace(item.expression))
            {
                result.ExpressionFilePath = string.Empty;
                result.ExpressionFileExists = true;
                return;
            }

            result.ExpressionFilePath = BuildExpressionPath(item.expression);
            result.ExpressionFileExists = File.Exists(result.ExpressionFilePath);

            if (!result.ExpressionFileExists)
            {
                Debug.LogWarning($"[BehaviorMappingResolver] 表情文件不存在：{result.ExpressionFilePath}");
            }
        }

        /// <summary>
        /// 把动作映射项写入解析结果。
        /// </summary>
        /// <param name="result">解析结果对象。</param>
        /// <param name="resolvedAction">实际命中的 action 标签。</param>
        /// <param name="item">动作映射项。</param>
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
        /// </summary>
        /// <param name="expressionFileName">表情文件名。</param>
        /// <returns>优先返回标准路径；若标准路径不存在但递归找到同名文件，则返回找到的路径。</returns>
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
        /// </summary>
        /// <param name="motionFileName">动作文件名。</param>
        /// <returns>优先返回标准路径；若标准路径不存在但递归找到同名文件，则返回找到的路径。</returns>
        /// <remarks>
        /// 标准路径优先按 <c>live2d/motions</c> 查找，同时也会递归搜索 live2d 目录，兼容不同模型导出的目录结构。
        /// </remarks>
        private string BuildMotionPath(string motionFileName)
        {
            string standardPath = Path.Combine(_characterRootPath, "live2d", "motions", motionFileName);

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
        /// </summary>
        /// <param name="rootPath">要搜索的根目录。</param>
        /// <param name="fileName">要查找的文件名，例如 qizi.motion3.json。</param>
        /// <returns>如果找到，返回完整路径；如果找不到，返回 null。</returns>
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

        /// <summary>
        /// 从表情映射表中查找指定 emotion。
        /// </summary>
        /// <param name="emotion">目标 emotion 标签。</param>
        /// <param name="actualEmotion">实际命中的 emotion key。</param>
        /// <param name="item">命中的映射项。</param>
        /// <returns>命中有效映射项返回 true，否则返回 false。</returns>
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
                    return item != null;
                }
            }

            return false;
        }

        /// <summary>
        /// 从动作映射表中查找指定 action。
        /// </summary>
        /// <param name="action">目标 action 标签。</param>
        /// <param name="actualAction">实际命中的 action key。</param>
        /// <param name="item">命中的映射项。</param>
        /// <returns>命中有效映射项返回 true，否则返回 false。</returns>
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

        /// <summary>
        /// 根据 motion 文件名反查 action 映射项。
        /// </summary>
        /// <param name="motionFileName">motion3.json 文件名。</param>
        /// <param name="actualAction">命中的 action key。</param>
        /// <param name="item">命中的映射项。</param>
        /// <returns>找到返回 true，否则返回 false。</returns>
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

        /// <summary>
        /// 从 fallback 映射表中查找目标标签。
        /// </summary>
        /// <param name="inputTag">输入标签。</param>
        /// <param name="fallbackMap">fallback 映射表。</param>
        /// <param name="fallbackTarget">查找到的目标标签。</param>
        /// <returns>找到可用目标标签返回 true，否则返回 false。</returns>
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

        /// <summary>
        /// 读取 JSON 配置文件。
        /// </summary>
        /// <typeparam name="T">目标配置类型。</typeparam>
        /// <param name="path">JSON 文件路径。</param>
        /// <returns>解析成功返回配置对象；失败返回 null。</returns>
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

        /// <summary>
        /// 标准化标签文本。
        /// </summary>
        /// <param name="tag">原始标签。</param>
        /// <returns>去掉首尾空白并转成小写后的标签；空值返回空字符串。</returns>
        private static string NormalizeTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return string.Empty;
            }

            return tag.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// 表情映射表配置。
        /// </summary>
        private class ExpressionMappingConfig
        {
            public string defaultEmotion = "neutral";

            /// <summary>
            /// 默认表情文件。
            /// </summary>
            /// <remarks>
            /// 空字符串表示恢复模型默认表情状态，而不是强制绑定 normal.exp3.json。
            /// </remarks>
            public string defaultExpression = string.Empty;

            public Dictionary<string, ExpressionMappingItem> emotionMappings = new Dictionary<string, ExpressionMappingItem>();
            public Dictionary<string, string> fallbackEmotionMap = new Dictionary<string, string>();
        }

        /// <summary>
        /// 单个 emotion 到表情文件的映射项。
        /// </summary>
        private class ExpressionMappingItem
        {
            /// <summary>
            /// 表情文件名。允许为空字符串，表示恢复 Live2D 默认表情状态。
            /// </summary>
            public string expression = string.Empty;

            public int priority;

            /// <summary>
            /// 映射说明字段，主要用于开发者控制台展示、调试和论文说明。
            /// </summary>
            public string description;
        }

        /// <summary>
        /// 动作映射表配置。
        /// </summary>
        private class MotionMappingConfig
        {
            /// <summary>
            /// 默认动作文件。
            /// </summary>
            /// <remarks>
            /// 不同 Live2D 模型的默认动作命名可能不同，因此不在代码中写死 idle.motion3.json。
            /// </remarks>
            public string defaultMotion = string.Empty;

            public Dictionary<string, MotionMappingItem> actionMappings = new Dictionary<string, MotionMappingItem>();
            public Dictionary<string, string> fallbackActionMap = new Dictionary<string, string>();
        }

        /// <summary>
        /// 单个 action 到动作文件的映射项。
        /// </summary>
        private class MotionMappingItem
        {
            public string motion;
            public bool loop;
            public int priority;

            /// <summary>
            /// 映射说明字段，主要用于开发者控制台展示、调试和论文说明。
            /// </summary>
            public string description;
        }
    }
}
