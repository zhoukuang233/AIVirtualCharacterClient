using Project.Character;
using Project.Infrastructure;
using UnityEngine;

namespace Project.App
{
    /// <summary>
    /// 应用启动入口。
    ///
    /// 功能：
    /// 1. 作为整个 Unity 前端的启动引导器，全局只允许存在一个实例。
    /// 2. 初始化运行时目录，包括 UserData、Characters、Services、Logs、Experiments。
    /// 3. 输出初始化结果，方便开发阶段检查 persistentDataPath 是否正确。
    /// 4. 尝试加载第一个可用角色，为后续 ChatSystem 和 PresentationSystem 提供当前角色上下文。
    ///
    /// 使用方式：
    /// 1. 在启动场景中创建一个空物体，例如 GameEntry。
    /// 2. 将 AppBootstrapper 挂载到该物体上。
    /// 3. 运行游戏后，本类会自动执行初始化逻辑。
    ///
    /// 对外暴露：
    /// - 通过 AppBootstrapper.Instance 获取全局启动器实例。
    ///
    /// TODO：
    /// - 接入 SettingsSystem，根据配置加载默认角色，而不是直接加载第一个可用角色。
    /// - 接入 ServiceSystem，读取后端服务配置。
    /// - 接入 GameStateMachine，管理启动、等待输入、请求中、表现播放等状态。
    /// - 接入 ExperimentLogging，记录启动阶段日志。
    /// </summary>
    public class AppBootstrapper : SingletonMonoBehaviour<AppBootstrapper>
    {
        /// <summary>
        /// AppBootstrapper 是全局启动入口，应该跨场景保留。
        /// </summary>
        protected override bool IsPersistent
        {
            get { return true; }
        }

        /// <summary>
        /// 单例初始化完成后的启动流程。
        ///
        /// 功能：
        /// 1. 初始化运行时路径。
        /// 2. 打印初始化结果。
        /// 3. 尝试加载第一个有效角色包。
        ///
        /// 参数：
        /// 无。
        ///
        /// 返回：
        /// 无。
        /// </summary>
        protected override void OnSingletonAwake()
        {
            Debug.Log("[AppBootstrapper] AI 虚拟角色前端启动。");

            RuntimePathInitializeResult result = RuntimePathInitializer.Initialize();

            if (!result.Success)
            {
                Debug.LogError($"[AppBootstrapper] 运行时路径初始化失败：{result.ErrorMessage}");
                return;
            }

            LogRuntimePathInitializeResult(result);
            TryLoadInitialCharacter();
        }

        /// <summary>
        /// 输出运行时目录初始化结果。
        ///
        /// 参数：
        /// result：RuntimePathInitializer.Initialize 返回的初始化结果。
        ///
        /// 返回：
        /// 无。
        /// </summary>
        private void LogRuntimePathInitializeResult(RuntimePathInitializeResult result)
        {
            Debug.Log($"[AppBootstrapper] UserData 根目录：{result.UserDataRootPath}");
            Debug.Log($"[AppBootstrapper] Characters 目录：{RuntimePathInitializer.CharactersPath}");
            Debug.Log($"[AppBootstrapper] Services 目录：{RuntimePathInitializer.ServicesPath}");
            Debug.Log($"[AppBootstrapper] Logs 目录：{RuntimePathInitializer.LogsPath}");
            Debug.Log($"[AppBootstrapper] Experiments 目录：{RuntimePathInitializer.ExperimentsPath}");

            if (result.CreatedDirectories.Count > 0)
            {
                Debug.Log($"[AppBootstrapper] 本次新建目录数量：{result.CreatedDirectories.Count}");
            }

            if (result.CopiedFiles.Count > 0)
            {
                Debug.Log($"[AppBootstrapper] 本次复制默认文件数量：{result.CopiedFiles.Count}");
            }

            if (result.Warnings.Count > 0)
            {
                foreach (string warning in result.Warnings)
                {
                    Debug.LogWarning($"[AppBootstrapper] 初始化警告：{warning}");
                }
            }
        }

        /// <summary>
        /// 尝试加载启动阶段的当前角色。
        ///
        /// 当前策略：
        /// 扫描 Characters 目录，加载第一个通过校验的角色包。
        ///
        /// 后续策略：
        /// 应该从 SettingsSystem 中读取 activeCharacterId，然后按 ID 加载。
        ///
        /// 参数：
        /// 无。
        ///
        /// 返回：
        /// 无。
        /// </summary>
        private void TryLoadInitialCharacter()
        {
            bool loaded = CurrentCharacterContext.Instance.TryLoadFirstValidCharacter(
                out CharacterPackageData characterData,
                out string message
            );

            if (!loaded)
            {
                Debug.LogWarning($"[AppBootstrapper] 启动角色加载失败：{message}");
                return;
            }

            Debug.Log(
                "[AppBootstrapper] 启动角色加载成功：" +
                $"CharacterId={characterData.CharacterId}, " +
                $"CharacterName={characterData.CharacterName}"
            );
        }
    }
}