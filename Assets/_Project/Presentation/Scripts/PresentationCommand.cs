using System;
using UnityEngine;

namespace Project.Presentation
{
    /// <summary>
    /// 表现命令基类。
    ///
    /// 设计目的：
    /// 1. ChatSystem / LLM 不直接控制 Live2D。
    /// 2. PresentationSystem 统一把 emotion/action/voiceStyle 转成命令。
    /// 3. 当前阶段只 Debug.Log，后续再替换成真实 Live2D 播放逻辑。
    /// </summary>
    public abstract class PresentationCommand
    {
        /// <summary>
        /// 命令唯一 ID，方便后续日志追踪。
        /// </summary>
        public string CommandId { get; private set; }

        /// <summary>
        /// 命令创建时间。
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// 命令类型，例如 Expression / Motion / Voice。
        /// </summary>
        public string CommandType { get; protected set; }

        protected PresentationCommand(string commandType)
        {
            CommandId = Guid.NewGuid().ToString("N");
            CreatedAt = DateTime.Now;
            CommandType = commandType;
        }

        /// <summary>
        /// 当前阶段的临时执行方式。
        /// 后续接入 Live2D 后，可以替换为真正的播放器调用。
        /// </summary>
        public abstract void ExecuteDebug();

        /// <summary>
        /// 输出命令的基础信息。
        /// </summary>
        protected string GetBaseLogText()
        {
            return $"CommandId={CommandId}, Type={CommandType}, CreatedAt={CreatedAt:HH:mm:ss.fff}";
        }
    }
}