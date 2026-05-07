using UnityEngine;

namespace Project.Presentation
{
    /// <summary>
    /// 表情表现命令。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该命令保存一次 emotion 解析后的表情表现信息，包括原始 emotion、解析后的 emotion、
    /// exp3.json 文件名、文件路径、优先级、是否发生 fallback 等。
    /// </para>
    /// <para>
    /// 当前阶段只负责保存和 Debug 输出，不直接调用 Live2D。
    /// 后续应由 Live2DExpressionPlayer 或同类组件读取本命令并执行真实表情播放。
    /// </para>
    /// <para>
    /// 对外暴露属性：InputEmotion、ResolvedEmotion、ExpressionFileName、ExpressionFilePath、
    /// Priority、FallbackUsed、FileExists、FallbackReason。
    /// 对外暴露方法：继承自 <see cref="PresentationCommand"/> 的 <see cref="ExecuteDebug"/>。
    /// </para>
    /// <para>
    /// TODO: 后续可以增加 FadeIn、FadeOut、Duration 等字段，控制表情过渡。
    /// TODO: 后续需要约定 ExpressionFileName 为空时表示“清空表情/恢复默认脸”。
    /// </para>
    /// </remarks>
    public class ExpressionCommand : PresentationCommand
    {
        /// <summary>
        /// LLM 或测试工具输入的原始 emotion 标签。
        /// </summary>
        public string InputEmotion { get; private set; }

        /// <summary>
        /// 经过 fallback 和默认值处理后的 emotion 标签。
        /// </summary>
        public string ResolvedEmotion { get; private set; }

        /// <summary>
        /// 解析出的表情文件名。
        /// </summary>
        /// <remarks>
        /// 可能为空字符串。为空时可表示恢复 Live2D 模型默认表情状态。
        /// </remarks>
        public string ExpressionFileName { get; private set; }

        /// <summary>
        /// 解析出的表情文件绝对路径。
        /// </summary>
        public string ExpressionFilePath { get; private set; }

        /// <summary>
        /// 表情优先级。
        /// </summary>
        public int Priority { get; private set; }

        /// <summary>
        /// 是否使用了 fallback 规则或默认表情。
        /// </summary>
        public bool FallbackUsed { get; private set; }

        /// <summary>
        /// 表情文件是否存在。
        /// </summary>
        public bool FileExists { get; private set; }

        /// <summary>
        /// fallback 原因说明。
        /// </summary>
        public string FallbackReason { get; private set; }

        /// <summary>
        /// 创建表情表现命令。
        /// </summary>
        /// <param name="inputEmotion">原始 emotion 标签。</param>
        /// <param name="resolvedEmotion">解析后的 emotion 标签。</param>
        /// <param name="expressionFileName">表情文件名。</param>
        /// <param name="expressionFilePath">表情文件绝对路径。</param>
        /// <param name="priority">表情优先级。</param>
        /// <param name="fallbackUsed">是否使用 fallback。</param>
        /// <param name="fileExists">表情文件是否存在。</param>
        /// <param name="fallbackReason">fallback 原因。</param>
        public ExpressionCommand(
            string inputEmotion,
            string resolvedEmotion,
            string expressionFileName,
            string expressionFilePath,
            int priority,
            bool fallbackUsed,
            bool fileExists,
            string fallbackReason) : base("Expression")
        {
            InputEmotion = inputEmotion;
            ResolvedEmotion = resolvedEmotion;
            ExpressionFileName = expressionFileName;
            ExpressionFilePath = expressionFilePath;
            Priority = priority;
            FallbackUsed = fallbackUsed;
            FileExists = fileExists;
            FallbackReason = fallbackReason;
        }

        /// <summary>
        /// 以 Debug.Log 的形式输出表情命令详情。
        /// </summary>
        public override void ExecuteDebug()
        {
            Debug.Log(
                "[Presentation][Expression]\n" +
                $"{GetBaseLogText()}\n" +
                $"InputEmotion={InputEmotion}\n" +
                $"ResolvedEmotion={ResolvedEmotion}\n" +
                $"ExpressionFileName={ExpressionFileName}\n" +
                $"ExpressionFilePath={ExpressionFilePath}\n" +
                $"Priority={Priority}\n" +
                $"FallbackUsed={FallbackUsed}\n" +
                $"FallbackReason={FallbackReason}\n" +
                $"FileExists={FileExists}"
            );
        }
    }
}
