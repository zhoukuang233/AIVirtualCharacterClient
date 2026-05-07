using Project.Character;
using Project.Presentation;
using UnityEngine;

namespace Project.Test
{
    /// <summary>
    /// 表现命令链 Debug 测试器。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 本脚本用于在暂未接入真实 Live2D 播放器之前，验证“文本 + 表情 + 动作 + 语音风格”
    /// 是否能够被统一转换成 PresentationCommandQueue 的 Debug 执行日志。
    /// </para>
    /// <para>
    /// 使用方式：
    /// 1. 确保场景中已经存在 AppBootstrapper，并且启动时成功加载角色与初始化 PresentationSystem。
    /// 2. 将本脚本挂载到测试物体上。
    /// 3. 运行场景后，在 Console 中查看 PresentationCommandQueue 输出。
    /// </para>
    /// <para>
    /// TODO: 后续接入 ChatController 后，本脚本可以移入 DeveloperConsole，作为手动测试按钮。
    /// </para>
    /// </remarks>
    public class PresentationMappingTestRunner : MonoBehaviour
    {
        [Header("Debug Reply")]
        [SerializeField] private string replyText = "别、别突然叫我呀……";

        [Header("Semantic Labels")]
        [SerializeField] private string emotion = "shy";
        [SerializeField] private string action = "deny";
        [SerializeField] private string voiceStyle = "soft";

        /// <summary>
        /// Unity Start 生命周期入口。
        /// </summary>
        private void Start()
        {
            EnsurePresentationSystemInitialized();
            PresentationSystem.Instance.EnqueueAndExecuteDebug(replyText, emotion, action, voiceStyle);
        }

        /// <summary>
        /// 确保表现系统已经完成初始化。
        /// </summary>
        /// <remarks>
        /// 正常情况下 AppBootstrapper 会先完成初始化。这里保留兜底逻辑，方便开发阶段单独测试本脚本。
        /// </remarks>
        private void EnsurePresentationSystemInitialized()
        {
            if (PresentationSystem.Instance.Initialized)
            {
                return;
            }

            if (!CharacterSystem.Instance.HasCurrentCharacter)
            {
                Debug.LogWarning("[PresentationMappingTestRunner] 当前没有已加载角色，无法测试表现命令链。");
                return;
            }

            PresentationSystem.Instance.Initialize(CharacterSystem.Instance.CurrentCharacter);
        }
    }
}
