using UnityEngine;

namespace Project.Presentation
{
    /// <summary>
    /// 语音表现命令。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该命令保存一次回复中解析出的 voiceStyle 信息。
    /// 当前 MVP 阶段还没有接入 TTS，因此只用于 Debug.Log 和后续日志结构预留。
    /// </para>
    /// <para>
    /// 对外暴露属性：<see cref="VoiceStyle"/>。
    /// 对外暴露方法：继承自 <see cref="PresentationCommand"/> 的 <see cref="ExecuteDebug"/>。
    /// </para>
    /// <para>
    /// TODO: 后续接入 TTS 后，可扩展音频文件路径、音频时长、TTS 延迟、speakerId、
    /// 音量、语速和口型同步参数。
    /// </para>
    /// </remarks>
    public class VoiceCommand : PresentationCommand
    {
        /// <summary>
        /// 语音风格标签。
        /// </summary>
        /// <remarks>
        /// 例如 normal、cheerful、soft、serious。当前为空时自动回退到 normal。
        /// </remarks>
        public string VoiceStyle { get; private set; }

        /// <summary>
        /// 创建语音表现命令。
        /// </summary>
        /// <param name="voiceStyle">语音风格标签；为空时使用 normal。</param>
        public VoiceCommand(string voiceStyle) : base("Voice")
        {
            VoiceStyle = string.IsNullOrWhiteSpace(voiceStyle) ? "normal" : voiceStyle;
        }

        /// <summary>
        /// 以 Debug.Log 的形式输出语音命令详情。
        /// </summary>
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
