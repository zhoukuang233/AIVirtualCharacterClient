using System;

namespace Project.Presentation
{
    /// <summary>
    /// 表现命令基类。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 设计目的：让 ChatSystem、LLM 响应解析器和 UI 不直接控制 Live2D。
    /// 统一由 PresentationSystem 把 emotion、action、voiceStyle 等语义标签转换成表现命令，
    /// 再交给表现命令队列或具体播放器执行。
    /// </para>
    /// <para>
    /// 当前 MVP 阶段命令只通过 <see cref="ExecuteDebug"/> 输出 Debug.Log；
    /// 后续接入 Live2D、TTS 和口型同步后，可以把该基类扩展为真正的异步命令接口。
    /// </para>
    /// <para>
    /// 对外暴露属性：命令 ID、创建时间、命令类型。
    /// 对外暴露方法：<see cref="ExecuteDebug"/>。
    /// </para>
    /// <para>
    /// TODO: 后续可以增加 ExecuteAsync、Cancel、Priority、Duration、CanInterrupt 等字段，
    /// 让表情、动作、语音能够被队列统一调度。
    /// </para>
    /// </remarks>
    public abstract class PresentationCommand
    {
        /// <summary>
        /// 命令唯一 ID。
        /// </summary>
        /// <remarks>
        /// 用于后续实验日志追踪一次表现命令从生成到执行的完整链路。
        /// </remarks>
        public string CommandId { get; private set; }

        /// <summary>
        /// 命令创建时间。
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// 命令类型，例如 Expression、Motion、Voice。
        /// </summary>
        public string CommandType { get; protected set; }

        /// <summary>
        /// 创建表现命令。
        /// </summary>
        /// <param name="commandType">命令类型名称。</param>
        protected PresentationCommand(string commandType)
        {
            CommandId = Guid.NewGuid().ToString("N");
            CreatedAt = DateTime.Now;
            CommandType = commandType;
        }

        /// <summary>
        /// 当前阶段的临时执行方式。
        /// </summary>
        /// <remarks>
        /// MVP 阶段只输出 Debug.Log。后续接入 Live2D 后，可以替换为真正的播放器调用。
        /// </remarks>
        public abstract void ExecuteDebug();

        /// <summary>
        /// 生成命令基础日志文本。
        /// </summary>
        /// <returns>包含命令 ID、类型和创建时间的文本。</returns>
        protected string GetBaseLogText()
        {
            return $"CommandId={CommandId}, Type={CommandType}, CreatedAt={CreatedAt:HH:mm:ss.fff}";
        }
    }
}
