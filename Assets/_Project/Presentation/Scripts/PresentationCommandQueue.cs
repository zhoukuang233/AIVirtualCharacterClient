using System.Collections.Generic;
using UnityEngine;

namespace Project.Presentation
{
    /// <summary>
    /// 表现命令队列。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该队列用于接收 ExpressionCommand、MotionCommand、VoiceCommand 等表现命令，
    /// 并按入队顺序统一执行。这样可以避免 UI、Live2D、TTS 各自直接执行导致流程混乱。
    /// </para>
    /// <para>
    /// 当前 MVP 阶段只支持同步 Debug 执行；后续可以扩展为真正的表现调度器，处理优先级、
    /// 动作打断、语音和口型同步、文本显示节奏等问题。
    /// </para>
    /// <para>
    /// 对外暴露属性：<see cref="Count"/>。
    /// 对外暴露方法：<see cref="Enqueue"/>、<see cref="EnqueueRange"/>、<see cref="Dequeue"/>、
    /// <see cref="Clear"/>、<see cref="ExecuteAllDebug"/>。
    /// </para>
    /// <para>
    /// TODO: 后续可以引入命令优先级和状态机，让高优先级动作打断低优先级 idle motion。
    /// TODO: 后续可以加入异步执行，等待语音播放结束后再进入下一轮对话状态。
    /// </para>
    /// </remarks>
    public class PresentationCommandQueue
    {
        private readonly Queue<PresentationCommand> _commands = new Queue<PresentationCommand>();

        /// <summary>
        /// 当前队列中的命令数量。
        /// </summary>
        public int Count => _commands.Count;

        /// <summary>
        /// 入队一个表现命令。
        /// </summary>
        /// <param name="command">要加入队列的表现命令；为 null 时会被忽略。</param>
        public void Enqueue(PresentationCommand command)
        {
            if (command == null)
            {
                Debug.LogWarning("[PresentationCommandQueue] 尝试加入空命令，已忽略。");
                return;
            }

            _commands.Enqueue(command);
        }

        /// <summary>
        /// 批量入队表现命令。
        /// </summary>
        /// <param name="commands">命令集合；为 null 时不执行任何操作。</param>
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

        /// <summary>
        /// 从队列头部取出一个表现命令。
        /// </summary>
        /// <returns>如果队列不为空，返回队列头部命令；否则返回 null。</returns>
        public PresentationCommand Dequeue()
        {
            if (_commands.Count == 0)
            {
                return null;
            }

            return _commands.Dequeue();
        }

        /// <summary>
        /// 清空所有待执行命令。
        /// </summary>
        public void Clear()
        {
            _commands.Clear();
        }

        /// <summary>
        /// 当前阶段按顺序 Debug 执行所有表现命令。
        /// </summary>
        /// <remarks>
        /// 调用后队列会被清空。
        /// </remarks>
        public void ExecuteAllDebug()
        {
            while (_commands.Count > 0)
            {
                PresentationCommand command = _commands.Dequeue();
                ExecuteSingleDebug(command);
            }
        }

        /// <summary>
        /// 当前阶段以统一、简洁的 Debug 日志执行一条表现命令。
        /// </summary>
        /// <param name="command">待执行命令。</param>
        private void ExecuteSingleDebug(PresentationCommand command)
        {
            if (command == null)
            {
                return;
            }

            ShowTextCommand showTextCommand = command as ShowTextCommand;
            if (showTextCommand != null)
            {
                Debug.Log($"[PresentationCommandQueue] ShowText: {showTextCommand.Text}");
                return;
            }

            ExpressionCommand expressionCommand = command as ExpressionCommand;
            if (expressionCommand != null)
            {
                Debug.Log($"[PresentationCommandQueue] PlayExpression: {expressionCommand.ExpressionFileName}");
                return;
            }

            MotionCommand motionCommand = command as MotionCommand;
            if (motionCommand != null)
            {
                Debug.Log($"[PresentationCommandQueue] PlayMotion: {motionCommand.MotionFileName}");
                return;
            }

            VoiceCommand voiceCommand = command as VoiceCommand;
            if (voiceCommand != null)
            {
                Debug.Log($"[PresentationCommandQueue] PlayVoice: {voiceCommand.VoiceStyle}");
                return;
            }

            command.ExecuteDebug();
        }
    }
}
