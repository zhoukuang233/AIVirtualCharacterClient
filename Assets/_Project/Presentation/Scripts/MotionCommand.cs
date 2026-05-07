using UnityEngine;

namespace Project.Presentation
{
    /// <summary>
    /// 动作表现命令。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该命令保存一次 action 解析后的动作表现信息，包括原始 action、解析后的 action、
    /// motion3.json 文件名、文件路径、循环标记、优先级、是否发生 fallback 等。
    /// </para>
    /// <para>
    /// 当前阶段只保存解析结果并输出 Debug.Log。后续接入 Live2D 时，应由 Live2DMotionPlayer
    /// 或同类组件执行真实动作播放。
    /// </para>
    /// <para>
    /// 对外暴露属性：InputAction、ResolvedAction、MotionFileName、MotionFilePath、Loop、
    /// Priority、FallbackUsed、FileExists、FallbackReason。
    /// </para>
    /// <para>
    /// TODO: 后续可以增加 MotionGroup、FadeIn、FadeOut、InterruptPolicy 等字段，适配 Cubism 动作播放。
    /// </para>
    /// </remarks>
    public class MotionCommand : PresentationCommand
    {
        /// <summary>
        /// LLM 或测试工具输入的原始 action 标签。
        /// </summary>
        public string InputAction { get; private set; }

        /// <summary>
        /// 经过 fallback 和默认值处理后的 action 标签。
        /// </summary>
        public string ResolvedAction { get; private set; }

        /// <summary>
        /// 解析出的动作文件名。
        /// </summary>
        public string MotionFileName { get; private set; }

        /// <summary>
        /// 解析出的动作文件绝对路径。
        /// </summary>
        public string MotionFilePath { get; private set; }

        /// <summary>
        /// 该动作是否循环播放。
        /// </summary>
        public bool Loop { get; private set; }

        /// <summary>
        /// 动作优先级。
        /// </summary>
        public int Priority { get; private set; }

        /// <summary>
        /// 是否使用了 fallback 规则或默认动作。
        /// </summary>
        public bool FallbackUsed { get; private set; }

        /// <summary>
        /// 动作文件是否存在。
        /// </summary>
        public bool FileExists { get; private set; }

        /// <summary>
        /// fallback 原因说明。
        /// </summary>
        public string FallbackReason { get; private set; }

        /// <summary>
        /// 创建动作表现命令。
        /// </summary>
        /// <param name="inputAction">原始 action 标签。</param>
        /// <param name="resolvedAction">解析后的 action 标签。</param>
        /// <param name="motionFileName">动作文件名。</param>
        /// <param name="motionFilePath">动作文件绝对路径。</param>
        /// <param name="loop">是否循环播放。</param>
        /// <param name="priority">动作优先级。</param>
        /// <param name="fallbackUsed">是否使用 fallback。</param>
        /// <param name="fileExists">动作文件是否存在。</param>
        /// <param name="fallbackReason">fallback 原因。</param>
        public MotionCommand(
            string inputAction,
            string resolvedAction,
            string motionFileName,
            string motionFilePath,
            bool loop,
            int priority,
            bool fallbackUsed,
            bool fileExists,
            string fallbackReason) : base("Motion")
        {
            InputAction = inputAction;
            ResolvedAction = resolvedAction;
            MotionFileName = motionFileName;
            MotionFilePath = motionFilePath;
            Loop = loop;
            Priority = priority;
            FallbackUsed = fallbackUsed;
            FileExists = fileExists;
            FallbackReason = fallbackReason;
        }

        /// <summary>
        /// 以 Debug.Log 的形式输出动作命令详情。
        /// </summary>
        public override void ExecuteDebug()
        {
            Debug.Log(
                "[Presentation][Motion]\n" +
                $"{GetBaseLogText()}\n" +
                $"InputAction={InputAction}\n" +
                $"ResolvedAction={ResolvedAction}\n" +
                $"MotionFileName={MotionFileName}\n" +
                $"MotionFilePath={MotionFilePath}\n" +
                $"Loop={Loop}\n" +
                $"Priority={Priority}\n" +
                $"FallbackUsed={FallbackUsed}\n" +
                $"FallbackReason={FallbackReason}\n" +
                $"FileExists={FileExists}"
            );
        }
    }
}
