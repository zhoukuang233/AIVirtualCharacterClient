using Project.Infrastructure;
using UnityEngine;

namespace Project.App
{
    /// <summary>
    /// 应用启动入口。
    /// 
    /// 当前 MVP 阶段职责：
    /// 1. 初始化运行时路径。
    /// 2. 打印关键目录，方便开发阶段检查。
    /// 
    /// 后续会继续扩展：
    /// - 初始化 SettingsSystem
    /// - 初始化 ServiceSystem
    /// - 初始化 CharacterSystem
    /// - 加载默认角色
    /// - 进入主界面状态
    /// </summary>
    public class AppBootstrapper : MonoBehaviour
    {
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
        }
    }
}