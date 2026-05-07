using Project.Infrastructure;
using UnityEngine;

namespace Project.App
{
    /// <summary>
    /// 应用启动入口。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该组件应挂载在启动场景中的一个常驻 GameObject 上，例如 <c>GameEntry</c>。
    /// Unity 加载场景后会调用 <see cref="Awake"/>，由它触发前端系统的第一轮初始化。
    /// </para>
    /// <para>
    /// 当前 MVP 阶段，本类只负责初始化运行时目录，并把关键路径输出到 Unity Console，
    /// 方便确认 <c>StreamingAssets/DefaultUserData</c> 是否成功复制到
    /// <c>Application.persistentDataPath/UserData</c>。
    /// </para>
    /// <para>
    /// 对外暴露成员：当前没有 public 字段或 public 方法。
    /// 本类主要通过 Unity 生命周期方法运行。
    /// </para>
    /// <para>
    /// TODO: 后续正式进入主流程时，可以在这里接入 SettingsSystem、ServiceSystem、CharacterSystem、
    /// PresentationSystem 和 GameStateMachine 的初始化逻辑。
    /// </para>
    /// </remarks>
    public class AppBootstrapper : MonoBehaviour
    {
        /// <summary>
        /// Unity 生命周期方法：场景对象初始化时调用。
        /// </summary>
        /// <remarks>
        /// 功能：
        /// 1. 调用 <see cref="RuntimePathInitializer.Initialize"/> 创建运行时目录。
        /// 2. 输出 UserData、Characters、Services、Logs、Experiments 等关键目录。
        /// 3. 输出本次启动中新建目录、复制文件和非致命警告的统计信息。
        /// </remarks>
        private void Awake()
        {
            Debug.Log("[AppBootstrapper] AI 虚拟角色前端启动。");

            RuntimePathInitializeResult result = RuntimePathInitializer.Initialize();

            if (!result.Success)
            {
                Debug.LogError($"[AppBootstrapper] 运行时路径初始化失败：{result.ErrorMessage}");
                return;
            }

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

            // TODO: 初始化 SettingsSystem，读取当前启用角色、后端地址、实验模式等全局配置。
            // TODO: 初始化 CharacterSystem，扫描角色包并加载默认角色。
            // TODO: 初始化 PresentationSystem，为当前角色创建 BehaviorMappingResolver。
            // TODO: 初始化 DeveloperConsole，在开发场景中显示角色包、映射表、日志状态等调试信息。
        }
    }
}
