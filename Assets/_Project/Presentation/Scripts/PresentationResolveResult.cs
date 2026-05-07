using System.Collections.Generic;

namespace Project.Presentation
{
    /// <summary>
    /// 表现映射解析结果。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该类表示一次 emotion/action/voiceStyle 解析后的中间结果。
    /// 它不是最终播放器，也不直接执行 Live2D 动作。
    /// </para>
    /// <para>
    /// 典型流程：
    /// <code>
    /// PresentationResolveResult result = resolver.Resolve("happy", "greet", "cheerful");
    /// List&lt;PresentationCommand&gt; commands = result.ToCommands();
    /// queue.EnqueueRange(commands);
    /// </code>
    /// </para>
    /// <para>
    /// 对外暴露字段：输入标签、解析后标签、表情文件、动作文件、优先级、fallback 信息和文件存在性。
    /// 对外暴露属性：HasExpression、HasMotion、HasVoice。
    /// 对外暴露方法：<see cref="ToCommands"/> 和 <see cref="ToDebugText"/>。
    /// </para>
    /// <para>
    /// TODO: 后续 ExperimentLogging 可以直接记录本对象中的字段，用于统计 fallback 次数、映射成功率和表现延迟。
    /// </para>
    /// </remarks>
    public class PresentationResolveResult
    {
        /// <summary>输入的 emotion 标签。</summary>
        public string InputEmotion;

        /// <summary>输入的 action 标签。</summary>
        public string InputAction;

        /// <summary>输入的 voiceStyle 标签。</summary>
        public string InputVoiceStyle;

        /// <summary>解析后的 emotion 标签。</summary>
        public string ResolvedEmotion;

        /// <summary>解析后的 action 标签。</summary>
        public string ResolvedAction;

        /// <summary>解析后的 voiceStyle 标签。</summary>
        public string ResolvedVoiceStyle;

        /// <summary>解析出的表情文件名。</summary>
        public string ExpressionFileName;

        /// <summary>解析出的表情文件绝对路径。</summary>
        public string ExpressionFilePath;

        /// <summary>表情优先级。</summary>
        public int ExpressionPriority;

        /// <summary>表情解析是否发生 fallback。</summary>
        public bool ExpressionFallbackUsed;

        /// <summary>表情文件是否存在。</summary>
        public bool ExpressionFileExists;

        /// <summary>表情 fallback 原因。</summary>
        public string ExpressionFallbackReason;

        /// <summary>解析出的动作文件名。</summary>
        public string MotionFileName;

        /// <summary>解析出的动作文件绝对路径。</summary>
        public string MotionFilePath;

        /// <summary>动作是否循环播放。</summary>
        public bool MotionLoop;

        /// <summary>动作优先级。</summary>
        public int MotionPriority;

        /// <summary>动作解析是否发生 fallback。</summary>
        public bool MotionFallbackUsed;

        /// <summary>动作文件是否存在。</summary>
        public bool MotionFileExists;

        /// <summary>动作 fallback 原因。</summary>
        public string MotionFallbackReason;

        /// <summary>
        /// 是否有需要执行的表情命令。
        /// </summary>
        /// <remarks>
        /// ExpressionFileName 为空时表示当前解析结果不需要播放 exp3.json。
        /// </remarks>
        public bool HasExpression => !string.IsNullOrWhiteSpace(ExpressionFileName);

        /// <summary>
        /// 是否有需要执行的动作命令。
        /// </summary>
        public bool HasMotion => !string.IsNullOrWhiteSpace(MotionFileName);

        /// <summary>
        /// 是否有语音风格信息。
        /// </summary>
        public bool HasVoice => !string.IsNullOrWhiteSpace(ResolvedVoiceStyle);

        /// <summary>
        /// 把解析结果转换成表现命令列表。
        /// </summary>
        /// <returns>
        /// 返回由 ExpressionCommand、MotionCommand、VoiceCommand 组成的命令列表。
        /// 当前阶段命令只 Debug.Log，后续可由 Live2D/TTS 子模块执行。
        /// </returns>
        public List<PresentationCommand> ToCommands()
        {
            var commands = new List<PresentationCommand>();

            if (HasExpression)
            {
                commands.Add(new ExpressionCommand(
                    InputEmotion,
                    ResolvedEmotion,
                    ExpressionFileName,
                    ExpressionFilePath,
                    ExpressionPriority,
                    ExpressionFallbackUsed,
                    ExpressionFileExists,
                    ExpressionFallbackReason
                ));
            }

            if (HasMotion)
            {
                commands.Add(new MotionCommand(
                    InputAction,
                    ResolvedAction,
                    MotionFileName,
                    MotionFilePath,
                    MotionLoop,
                    MotionPriority,
                    MotionFallbackUsed,
                    MotionFileExists,
                    MotionFallbackReason
                ));
            }

            if (HasVoice)
            {
                commands.Add(new VoiceCommand(ResolvedVoiceStyle));
            }

            return commands;
        }

        /// <summary>
        /// 把完整解析结果转换成适合 Debug.Log 输出的多行文本。
        /// </summary>
        /// <returns>包含输入标签、解析后标签、文件路径、fallback 和文件存在性的信息。</returns>
        public string ToDebugText()
        {
            return "[Presentation][ResolveResult]\n" +
                   $"InputEmotion={InputEmotion}\n" +
                   $"InputAction={InputAction}\n" +
                   $"InputVoiceStyle={InputVoiceStyle}\n" +
                   $"ResolvedEmotion={ResolvedEmotion}\n" +
                   $"ResolvedAction={ResolvedAction}\n" +
                   $"ResolvedVoiceStyle={ResolvedVoiceStyle}\n" +
                   $"ExpressionFileName={ExpressionFileName}\n" +
                   $"ExpressionFilePath={ExpressionFilePath}\n" +
                   $"ExpressionFallbackUsed={ExpressionFallbackUsed}\n" +
                   $"ExpressionFallbackReason={ExpressionFallbackReason}\n" +
                   $"ExpressionFileExists={ExpressionFileExists}\n" +
                   $"MotionFileName={MotionFileName}\n" +
                   $"MotionFilePath={MotionFilePath}\n" +
                   $"MotionLoop={MotionLoop}\n" +
                   $"MotionFallbackUsed={MotionFallbackUsed}\n" +
                   $"MotionFallbackReason={MotionFallbackReason}\n" +
                   $"MotionFileExists={MotionFileExists}";
        }
    }
}
