using System.Collections.Generic;
using UnityEngine;

namespace Project.Presentation
{
    /// <summary>
    /// 表现命令队列。
    ///
    /// 当前阶段：
    /// 1. 接收 ExpressionCommand / MotionCommand / VoiceCommand。
    /// 2. 按入队顺序执行 Debug.Log。
    ///
    /// 后续阶段：
    /// 1. 可以加入优先级调度。
    /// 2. 可以处理动作打断。
    /// 3. 可以让语音、口型、表情同步执行。
    /// </summary>
    public class PresentationCommandQueue
    {
        private readonly Queue<PresentationCommand> _commands = new Queue<PresentationCommand>();

        public int Count
        {
            get { return _commands.Count; }
        }

        public void Enqueue(PresentationCommand command)
        {
            if (command == null)
            {
                Debug.LogWarning("[PresentationCommandQueue] 尝试加入空命令，已忽略。");
                return;
            }

            _commands.Enqueue(command);
        }

        public void EnqueueRange(IEnumerable<PresentationCommand> commands)
        {
            if (commands == null)
            {
                return;
            }

            foreach (PresentationCommand command in commands)
            {
                Enqueue(command);
            }
        }

        public PresentationCommand Dequeue()
        {
            if (_commands.Count == 0)
            {
                return null;
            }

            return _commands.Dequeue();
        }

        public void Clear()
        {
            _commands.Clear();
        }

        /// <summary>
        /// 当前阶段直接按顺序 Debug 执行所有表现命令。
        /// </summary>
        public void ExecuteAllDebug()
        {
            Debug.Log($"[PresentationCommandQueue] 开始执行表现命令，Count={_commands.Count}");

            while (_commands.Count > 0)
            {
                PresentationCommand command = _commands.Dequeue();
                command.ExecuteDebug();
            }

            Debug.Log("[PresentationCommandQueue] 表现命令执行结束。");
        }
    }
}