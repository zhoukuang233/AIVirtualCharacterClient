using UnityEngine;

namespace Project.Presentation
{
    /// <summary>
    /// 语音表现命令。
    ///
    /// 当前阶段不接 TTS，只保留 voiceStyle。
    /// 后续可以扩展为：
    /// 1. 播放 TTS 返回的音频文件。
    /// 2. 触发口型同步。
    /// 3. 记录语音风格和音频延迟。
    /// </summary>
    public class VoiceCommand : PresentationCommand
    {
        public string VoiceStyle { get; private set; }

        public VoiceCommand(string voiceStyle)
            : base("Voice")
        {
            VoiceStyle = string.IsNullOrWhiteSpace(voiceStyle) ? "normal" : voiceStyle;
        }

        public override void ExecuteDebug()
        {
            Debug.Log(
                "[Presentation][Voice]\n" +
                $"{GetBaseLogText()}\n" +
                $"VoiceStyle={VoiceStyle}\n" +
                "CurrentStage=DebugOnly"
            );
        }
    }
}