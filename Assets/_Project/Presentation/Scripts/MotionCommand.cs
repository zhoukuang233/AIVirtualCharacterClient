using UnityEngine;

namespace Project.Presentation
{
    /// <summary>
    /// 动作表现命令。
    ///
    /// 当前只保存解析后的 motion3.json 信息。
    /// 后续接入 Live2D 时，再由 Live2DMotionPlayer 执行。
    /// </summary>
    public class MotionCommand : PresentationCommand
    {
        public string InputAction { get; private set; }
        public string ResolvedAction { get; private set; }
        public string MotionFileName { get; private set; }
        public string MotionFilePath { get; private set; }
        public bool Loop { get; private set; }
        public int Priority { get; private set; }
        public bool FallbackUsed { get; private set; }
        public bool FileExists { get; private set; }
        public string FallbackReason { get; private set; }

        public MotionCommand(
            string inputAction,
            string resolvedAction,
            string motionFileName,
            string motionFilePath,
            bool loop,
            int priority,
            bool fallbackUsed,
            bool fileExists,
            string fallbackReason)
            : base("Motion")
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