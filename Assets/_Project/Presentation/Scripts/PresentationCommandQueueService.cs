using System.Collections.Generic;
using Project.Infrastructure;

namespace Project.Presentation
{
    /// <summary>
    /// 表现命令队列全局服务。
    ///
    /// 功能：
    /// 1. 持有一个全局 PresentationCommandQueue。
    /// 2. 为 ChatSystem、PresentationSystem、DeveloperConsole 提供统一入队和执行入口。
    /// 3. 当前 MVP 阶段只执行 Debug 命令；后续可以替换为真实 Live2D / TTS 调度。
    ///
    /// 使用方式：
    /// PresentationCommandQueueService.Instance.EnqueueRange(resolveResult.ToCommands());
    /// PresentationCommandQueueService.Instance.ExecuteAllDebug();
    ///
    /// 对外暴露：
    /// - Count：当前队列命令数量。
    /// - Enqueue：加入单条表现命令。
    /// - EnqueueRange：批量加入表现命令。
    /// - Dequeue：取出一条表现命令。
    /// - Clear：清空队列。
    /// - ExecuteAllDebug：Debug 执行所有命令。
    ///
    /// TODO：
    /// - 后续接入 Live2DExpressionPlayer、Live2DMotionPlayer、TTS 播放器和 LipSyncController。
    /// - 后续支持优先级、动作打断、语音口型同步。
    /// - 如果未来支持多个角色同屏，应改为“每个角色一个队列”，不要继续使用全局唯一队列。
    /// </summary>
    public class PresentationCommandQueueService : SingletonMonoBehaviour<PresentationCommandQueueService>
    {
        private readonly PresentationCommandQueue _queue = new PresentationCommandQueue();

        /// <summary>
        /// 当前命令队列中的命令数量。
        /// </summary>
        public int Count
        {
            get { return _queue.Count; }
        }

        /// <summary>
        /// 添加一条表现命令。
        ///
        /// 参数：
        /// command：要加入队列的表现命令。
        ///
        /// 返回：
        /// 无。
        /// </summary>
        public void Enqueue(PresentationCommand command)
        {
            _queue.Enqueue(command);
        }

        /// <summary>
        /// 批量添加表现命令。
        ///
        /// 参数：
        /// commands：要加入队列的表现命令集合。
        ///
        /// 返回：
        /// 无。
        /// </summary>
        public void EnqueueRange(IEnumerable<PresentationCommand> commands)
        {
            _queue.EnqueueRange(commands);
        }

        /// <summary>
        /// 从队列中取出一条表现命令。
        ///
        /// 返回：
        /// 如果队列非空，返回队首命令；如果队列为空，返回 null。
        /// </summary>
        public PresentationCommand Dequeue()
        {
            return _queue.Dequeue();
        }

        /// <summary>
        /// 清空表现命令队列。
        ///
        /// 参数：
        /// 无。
        ///
        /// 返回：
        /// 无。
        /// </summary>
        public void Clear()
        {
            _queue.Clear();
        }

        /// <summary>
        /// Debug 执行当前队列中的所有表现命令。
        ///
        /// 参数：
        /// 无。
        ///
        /// 返回：
        /// 无。
        /// </summary>
        public void ExecuteAllDebug()
        {
            _queue.ExecuteAllDebug();
        }
    }
}