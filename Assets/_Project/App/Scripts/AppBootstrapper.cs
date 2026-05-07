using Project.Character;
using Project.Infrastructure;
using Project.Presentation;
using UnityEngine;

namespace Project.App
{
    /// <summary>
    /// 应用启动入口。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 本类负责把 Unity 前端从“空场景”推进到可测试状态。当前阶段的启动流程是：
    /// 初始化运行时 UserData 目录，扫描并加载第一个有效角色包，初始化表现映射系统，
    /// 最后进入 TestReady 状态。
    /// </para>
    /// <para>
    /// 重要边界：
    /// 1. AppBootstrapper 只编排启动流程，不直接读取角色包文件。
    /// 2. 角色扫描、校验、加载统一交给 <see cref="CharacterSystem"/>。
    /// 3. 表现映射初始化统一交给 <see cref="PresentationSystem"/>。
    /// </para>
    /// <para>
    /// 使用方式：
    /// 1. 在启动场景中创建一个空物体，例如 GameEntry。
    /// 2. 将 AppBootstrapper 挂载到该物体上。
    /// 3. 运行场景后，本类会自动完成路径初始化、角色加载和表现映射初始化。
    /// </para>
    /// <para>
    /// 对外暴露：
    /// - Instance：通过 SingletonMonoBehaviour 提供的全局单例实例。
    /// - CurrentStartupState：当前启动状态。
    /// </para>
    /// <para>
    /// TODO: 后续接入 SettingsSystem 后，应根据 activeCharacterId 加载默认角色，而不是加载第一个有效角色。
    /// TODO: 后续接入 ServiceSystem 后，应在启动阶段读取后端地址、超时时间和当前服务配置名。
    /// TODO: 后续接入 GameStateMachine 后，应把 StartupState 替换成正式状态机状态。
    /// TODO: 后续接入 ExperimentLogging 后，应记录启动耗时、角色包版本和初始化错误。
    /// </para>
    /// </remarks>
    public class AppBootstrapper : SingletonMonoBehaviour<AppBootstrapper>
    {
        /// <summary>
        /// 当前启动状态。
        /// </summary>
        public AppStartupState CurrentStartupState { get; private set; } = AppStartupState.NotStarted;

        /// <summary>
        /// AppBootstrapper 是全局启动入口，应该跨场景保留。
        /// </summary>
        protected override bool IsPersistent
        {
            get { return true; }
        }

        /// <summary>
        /// 单例初始化完成后的启动流程。
        /// </summary>
        /// <remarks>
        /// 当前启动阶段按固定顺序执行：
        /// RuntimePathInitializer -> CharacterSystem -> PresentationSystem -> TestReady。
        /// 任一步失败都会提前停止，并把 CurrentStartupState 设置为 Failed。
        /// </remarks>
        protected override void OnSingletonAwake()
        {
            StartFrontend();
        }

        /// <summary>
        /// 执行 AI 虚拟角色 Unity 前端启动流程。
        /// </summary>
        private void StartFrontend()
        {
            CurrentStartupState = AppStartupState.Starting;
            Debug.Log("[AppBootstrapper] AI 虚拟角色前端启动。");

            if (!InitializeRuntimePaths())
            {
                CurrentStartupState = AppStartupState.Failed;
                return;
            }

            if (!LoadInitialCharacter(out CharacterPackageData characterData))
            {
                CurrentStartupState = AppStartupState.Failed;
                return;
            }

            if (!InitializePresentationSystem(characterData))
            {
                CurrentStartupState = AppStartupState.Failed;
                return;
            }

            CurrentStartupState = AppStartupState.TestReady;
            Debug.Log("[AppBootstrapper] 进入 TestReady 状态。");
        }

        /// <summary>
        /// 初始化运行时目录和默认 UserData。
        /// </summary>
        /// <returns>初始化成功返回 true；失败返回 false。</returns>
        private bool InitializeRuntimePaths()
        {
            RuntimePathInitializeResult result = RuntimePathInitializer.Initialize();
            if (!result.Success)
            {
                Debug.LogError($"[AppBootstrapper] 运行时路径初始化失败：{result.ErrorMessage}");
                return false;
            }

            LogRuntimePathWarnings(result);
            return true;
        }

        /// <summary>
        /// 加载启动阶段使用的默认角色。
        /// </summary>
        /// <param name="characterData">加载成功时输出角色包数据。</param>
        /// <returns>加载成功返回 true；失败返回 false。</returns>
        private bool LoadInitialCharacter(out CharacterPackageData characterData)
        {
            bool loaded = CharacterSystem.Instance.TryLoadFirstValidCharacter(
                out characterData,
                out CharacterValidationResult validationResult,
                out string message
            );

            if (!loaded)
            {
                Debug.LogWarning($"[AppBootstrapper] 启动角色加载失败：{message}");
                return false;
            }

            if (validationResult != null && validationResult.Warnings.Count > 0)
            {
                foreach (string warning in validationResult.Warnings)
                {
                    Debug.LogWarning($"[AppBootstrapper] 当前角色校验警告：{warning}");
                }
            }

            return true;
        }

        /// <summary>
        /// 初始化表现映射系统。
        /// </summary>
        /// <param name="characterData">当前启动角色数据。</param>
        /// <returns>初始化成功返回 true；失败返回 false。</returns>
        private bool InitializePresentationSystem(CharacterPackageData characterData)
        {
            bool initialized = PresentationSystem.Instance.Initialize(characterData);
            if (!initialized)
            {
                Debug.LogError("[AppBootstrapper] 表现映射初始化失败。");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 输出运行时路径初始化阶段的警告信息。
        /// </summary>
        /// <param name="result">运行时路径初始化结果。</param>
        private void LogRuntimePathWarnings(RuntimePathInitializeResult result)
        {
            if (result.Warnings.Count == 0)
            {
                return;
            }

            foreach (string warning in result.Warnings)
            {
                Debug.LogWarning($"[AppBootstrapper] 初始化警告：{warning}");
            }
        }
    }

    /// <summary>
    /// AppBootstrapper 当前启动状态。
    /// </summary>
    /// <remarks>
    /// 当前只是轻量枚举，用于替代散落的 bool 标记。后续如果引入正式 GameStateMachine，
    /// 可以把这里的状态迁移到统一状态机中。
    /// </remarks>
    public enum AppStartupState
    {
        /// <summary>尚未开始启动。</summary>
        NotStarted,

        /// <summary>正在启动。</summary>
        Starting,

        /// <summary>测试准备完成，可以进行离线聊天闭环或 DeveloperConsole 测试。</summary>
        TestReady,

        /// <summary>启动失败。</summary>
        Failed
    }
}
