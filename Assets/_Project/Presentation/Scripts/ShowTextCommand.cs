using UnityEngine;

namespace Project.Presentation
{
    /// <summary>
    /// 文本显示表现命令。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该命令表示一次角色回复文本的展示请求。当前 MVP 阶段只通过 Debug.Log 输出，
    /// 后续接入正式 UI 后，应由 ChatPanel、DialogueView 或同类 UI 组件消费该命令并显示文本。
    /// </para>
    /// <para>
    /// 对外暴露属性：<see cref="Text"/>。
    /// 对外暴露方法：继承自 <see cref="PresentationCommand"/> 的 <see cref="PresentationCommand.ExecuteDebug"/>。
    /// </para>
    /// <para>
    /// TODO: 后续可以增加打字机速度、是否追加到历史记录、文本语言、字幕持续时间等字段。
    /// </para>
    /// </remarks>
    public class ShowTextCommand : PresentationCommand
    {
        /// <summary>
        /// 需要显示到对话 UI 的角色回复文本。
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// 创建文本显示命令。
        /// </summary>
        /// <param name="text">角色回复文本；为空时使用空字符串。</param>
        public ShowTextCommand(string text) : base("ShowText")
        {
            Text = string.IsNullOrWhiteSpace(text) ? string.Empty : text;
        }

        /// <summary>
        /// 以 Debug.Log 的形式输出文本显示命令。
        /// </summary>
        public override void ExecuteDebug()
        {
            Debug.Log($"[PresentationCommandQueue] ShowText: {Text}");
        }
    }
}
