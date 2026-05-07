using System.Collections.Generic;
using Project.Character;
using Project.Infrastructure;
using UnityEngine;

namespace Project.Presentation
{
    /// <summary>
    /// 表现系统门面。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 本类把表现映射解析器 <see cref="BehaviorMappingResolver"/> 和表现命令队列
    /// <see cref="PresentationCommandQueueService"/> 包装成上层统一入口。
    /// AppBootstrapper、ChatSystem、DeveloperConsole 后续可以只依赖 PresentationSystem，
    /// 不直接依赖具体 Resolver 或 Queue。
    /// </para>
    /// <para>
    /// 使用方式：
    /// <code>
    /// PresentationSystem.Instance.Initialize(characterData);
    /// PresentationSystem.Instance.EnqueueAndExecuteDebug("你好呀", "shy", "deny", "soft");
    /// </code>
    /// </para>
    /// <para>
    /// 对外暴露：
    /// - Initialized：表现映射是否已经初始化。
    /// - CurrentCharacter：当前表现系统绑定的角色。
    /// - Initialize：根据角色包初始化表现映射。
    /// - Resolve：解析 emotion/action/voiceStyle。
    /// - BuildCommands：生成 ShowText / Expression / Motion / Voice 命令链。
    /// - EnqueueAndExecuteDebug：入队并 Debug 执行命令链。
    /// </para>
    /// <para>
    /// TODO: 后续接入 Live2D 后，在本类中进一步组合 Live2DExpressionPlayer、Live2DMotionPlayer、TTS 播放器和 LipSyncController。
    /// TODO: 后续可以增加 ReinitializeForCurrentCharacter，用于角色切换后自动刷新表现映射。
    /// </para>
    /// </remarks>
    public class PresentationSystem : SingletonMonoBehaviour<PresentationSystem>
    {
        private BehaviorMappingResolver _resolver;

        /// <summary>
        /// 当前表现系统绑定的角色包数据。
        /// </summary>
        public CharacterPackageData CurrentCharacter { get; private set; }

        /// <summary>
        /// 表现映射是否已经初始化。
        /// </summary>
        public bool Initialized
        {
            get { return _resolver != null && CurrentCharacter != null; }
        }

        /// <summary>
        /// PresentationSystem 是全局运行时服务，应该跨场景保留。
        /// </summary>
        protected override bool IsPersistent
        {
            get { return true; }
        }

        /// <summary>
        /// 根据当前角色包初始化表现映射解析器。
        /// </summary>
        /// <param name="characterData">已经通过校验并加载成功的角色包数据。</param>
        /// <returns>初始化成功返回 true；失败返回 false。</returns>
        public bool Initialize(CharacterPackageData characterData)
        {
            if (characterData == null || characterData.PackageInfo == null)
            {
                Debug.LogWarning("[PresentationSystem] 初始化失败：角色数据为空。");
                return false;
            }

            string expressionMappingFile = "expression_mapping.json";
            string motionMappingFile = "motion_mapping.json";

            if (characterData.Definition != null && characterData.Definition.Mapping != null)
            {
                if (!string.IsNullOrWhiteSpace(characterData.Definition.Mapping.ExpressionMappingFile))
                {
                    expressionMappingFile = characterData.Definition.Mapping.ExpressionMappingFile;
                }

                if (!string.IsNullOrWhiteSpace(characterData.Definition.Mapping.MotionMappingFile))
                {
                    motionMappingFile = characterData.Definition.Mapping.MotionMappingFile;
                }
            }

            CurrentCharacter = characterData;
            _resolver = new BehaviorMappingResolver(
                characterData.PackageInfo.PackageRootPath,
                expressionMappingFile,
                motionMappingFile
            );

            Debug.Log("[PresentationSystem] 表现映射初始化完成。");
            return true;
        }

        /// <summary>
        /// 解析一次表现标签。
        /// </summary>
        /// <param name="emotion">LLM 或测试工具输出的 emotion 标签。</param>
        /// <param name="action">LLM 或测试工具输出的 action 标签。</param>
        /// <param name="voiceStyle">LLM 或测试工具输出的 voiceStyle 标签。</param>
        /// <returns>返回表现映射解析结果；未初始化时返回 null。</returns>
        public PresentationResolveResult Resolve(string emotion, string action, string voiceStyle)
        {
            if (!Initialized)
            {
                Debug.LogWarning("[PresentationSystem] Resolve 失败：表现系统尚未初始化。");
                return null;
            }

            return _resolver.Resolve(emotion, action, voiceStyle);
        }

        /// <summary>
        /// 根据回复文本和表现标签生成完整命令链。
        /// </summary>
        /// <param name="replyText">角色回复文本。</param>
        /// <param name="emotion">emotion 标签。</param>
        /// <param name="action">action 标签。</param>
        /// <param name="voiceStyle">voiceStyle 标签。</param>
        /// <returns>
        /// 返回命令列表，顺序为 ShowText、PlayExpression、PlayMotion、PlayVoice。
        /// 未初始化或解析失败时返回空列表。
        /// </returns>
        public List<PresentationCommand> BuildCommands(
            string replyText,
            string emotion,
            string action,
            string voiceStyle)
        {
            var commands = new List<PresentationCommand>();

            if (!string.IsNullOrWhiteSpace(replyText))
            {
                commands.Add(new ShowTextCommand(replyText));
            }

            PresentationResolveResult resolveResult = Resolve(emotion, action, voiceStyle);
            if (resolveResult == null)
            {
                return commands;
            }

            commands.AddRange(resolveResult.ToCommands());
            return commands;
        }

        /// <summary>
        /// 生成表现命令链，加入全局表现命令队列，并立刻以 Debug 方式执行。
        /// </summary>
        /// <param name="replyText">角色回复文本。</param>
        /// <param name="emotion">emotion 标签。</param>
        /// <param name="action">action 标签。</param>
        /// <param name="voiceStyle">voiceStyle 标签。</param>
        public void EnqueueAndExecuteDebug(
            string replyText,
            string emotion,
            string action,
            string voiceStyle)
        {
            List<PresentationCommand> commands = BuildCommands(replyText, emotion, action, voiceStyle);
            PresentationCommandQueueService.Instance.EnqueueRange(commands);
            PresentationCommandQueueService.Instance.ExecuteAllDebug();
        }
    }
}
