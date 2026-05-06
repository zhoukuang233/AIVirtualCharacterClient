using UnityEngine;

namespace Project.Presentation
{
    /// <summary>
    /// 表情表现命令。
    ///
    /// 当前只负责保存解析后的 exp3.json 信息。
    /// 后续接入 Live2D 时，再由 Live2DExpressionPlayer 执行。
    /// </summary>
    public class ExpressionCommand : PresentationCommand
    {
        public string InputEmotion { get; private set; }
        public string ResolvedEmotion { get; private set; }
        public string ExpressionFileName { get; private set; }
        public string ExpressionFilePath { get; private set; }
        public int Priority { get; private set; }
        public bool FallbackUsed { get; private set; }
        public bool FileExists { get; private set; }
        public string FallbackReason { get; private set; }

        public ExpressionCommand(
            string inputEmotion,
            string resolvedEmotion,
            string expressionFileName,
            string expressionFilePath,
            int priority,
            bool fallbackUsed,
            bool fileExists,
            string fallbackReason)
            : base("Expression")
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