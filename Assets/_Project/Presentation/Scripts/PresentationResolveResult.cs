using System.Collections.Generic;

namespace Project.Presentation
{
    /// <summary>
    /// 表现映射解析结果。
    ///
    /// 它不是最终播放器，而是一次 emotion/action/voiceStyle 解析后的中间结果。
    /// 后续 ExperimentLogging 可以直接记录这个对象里的字段。
    /// </summary>
    public class PresentationResolveResult
    {
        public string InputEmotion;
        public string InputAction;
        public string InputVoiceStyle;

        public string ResolvedEmotion;
        public string ResolvedAction;
        public string ResolvedVoiceStyle;

        public string ExpressionFileName;
        public string ExpressionFilePath;
        public int ExpressionPriority;
        public bool ExpressionFallbackUsed;
        public bool ExpressionFileExists;
        public string ExpressionFallbackReason;

        public string MotionFileName;
        public string MotionFilePath;
        public bool MotionLoop;
        public int MotionPriority;
        public bool MotionFallbackUsed;
        public bool MotionFileExists;
        public string MotionFallbackReason;

        public bool HasExpression
        {
            get { return !string.IsNullOrWhiteSpace(ExpressionFileName); }
        }

        public bool HasMotion
        {
            get { return !string.IsNullOrWhiteSpace(MotionFileName); }
        }

        public bool HasVoice
        {
            get { return !string.IsNullOrWhiteSpace(ResolvedVoiceStyle); }
        }

        /// <summary>
        /// 把解析结果转换成表现命令。
        /// 当前阶段命令只 Debug.Log，后续再由 Live2D/TTS 子模块执行。
        /// </summary>
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
        /// 方便一次性输出完整解析结果。
        /// </summary>
        public string ToDebugText()
        {
            return
                "[Presentation][ResolveResult]\n" +
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