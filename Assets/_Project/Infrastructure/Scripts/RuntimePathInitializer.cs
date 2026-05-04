using System;
using System.IO;
using UnityEngine;

namespace Project.Infrastructure
{
    /// <summary>
    /// 运行时路径初始化器。
    /// 
    /// 职责：
    /// 1. 统一管理 UserData 运行时根目录。
    /// 2. 在程序启动时创建 Characters、Services、Logs、Experiments 等目录。
    /// 3. 从 StreamingAssets/DefaultUserData 补充默认配置到 persistentDataPath/UserData。
    /// 
    /// 注意：
    /// - StreamingAssets 适合放随包发布的默认配置。
    /// - persistentDataPath 适合放运行时可修改的数据。
    /// </summary>
    public static class RuntimePathInitializer
    {
        private const string UserDataFolderName = "UserData";
        private const string DefaultUserDataFolderName = "DefaultUserData";

        private const string CharactersFolderName = "Characters";
        private const string ServicesFolderName = "Services";
        private const string LogsFolderName = "Logs";
        private const string ExperimentsFolderName = "Experiments";

        /// <summary>
        /// persistentDataPath 下的 UserData 根目录。
        /// </summary>
        public static string UserDataRootPath =>
            Path.Combine(Application.persistentDataPath, UserDataFolderName);

        /// <summary>
        /// 角色包目录。
        /// </summary>
        public static string CharactersPath =>
            Path.Combine(UserDataRootPath, CharactersFolderName);

        /// <summary>
        /// 服务配置目录。
        /// </summary>
        public static string ServicesPath =>
            Path.Combine(UserDataRootPath, ServicesFolderName);

        /// <summary>
        /// 前端日志目录。
        /// </summary>
        public static string LogsPath =>
            Path.Combine(UserDataRootPath, LogsFolderName);

        /// <summary>
        /// 实验数据目录。
        /// </summary>
        public static string ExperimentsPath =>
            Path.Combine(UserDataRootPath, ExperimentsFolderName);

        /// <summary>
        /// StreamingAssets 中默认 UserData 的路径。
        /// </summary>
        private static string DefaultUserDataSourcePath =>
            Path.Combine(Application.streamingAssetsPath, DefaultUserDataFolderName);

        /// <summary>
        /// 初始化运行时目录。
        /// 建议在 AppBootstrapper 的 Awake 或 Start 中调用。
        /// </summary>
        public static RuntimePathInitializeResult Initialize()
        {
            var result = new RuntimePathInitializeResult();

            try
            {
                // 1. 创建 UserData 根目录
                CreateDirectoryIfMissing(UserDataRootPath, result);

                // 2. 如果 StreamingAssets/DefaultUserData 存在，则复制默认配置。
                //    注意：这里默认不覆盖已有文件，避免用户运行后修改的配置被重置。
                CopyDefaultUserDataIfExists(result);

                // 3. 再次确保核心目录一定存在。
                //    即使没有 DefaultUserData，也要保证 MVP 可以正常启动。
                CreateDirectoryIfMissing(CharactersPath, result);
                CreateDirectoryIfMissing(ServicesPath, result);
                CreateDirectoryIfMissing(LogsPath, result);
                CreateDirectoryIfMissing(ExperimentsPath, result);

                result.Success = true;
                result.UserDataRootPath = UserDataRootPath;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;

                Debug.LogError($"[RuntimePathInitializer] 初始化失败：{ex}");
            }

            return result;
        }

        /// <summary>
        /// 如果目录不存在，则创建目录。
        /// </summary>
        private static void CreateDirectoryIfMissing(string path, RuntimePathInitializeResult result)
        {
            if (Directory.Exists(path))
            {
                return;
            }

            Directory.CreateDirectory(path);
            result.CreatedDirectories.Add(path);

            Debug.Log($"[RuntimePathInitializer] 创建目录：{path}");
        }

        /// <summary>
        /// 从 StreamingAssets/DefaultUserData 复制默认配置到 persistentDataPath/UserData。
        /// 
        /// 设计原则：
        /// - 只补充缺失文件。
        /// - 不覆盖用户已有配置。
        /// - 这样后续用户可以修改 persistentDataPath/UserData 中的角色包和服务配置。
        /// </summary>
        private static void CopyDefaultUserDataIfExists(RuntimePathInitializeResult result)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Android 平台下 StreamingAssets 位于压缩包中，不能直接用 Directory 读取。
            // MVP 阶段先不处理 Android，后续如需移动端支持，可改成 UnityWebRequest 读取清单文件。
            Debug.LogWarning("[RuntimePathInitializer] Android 平台暂不支持直接复制 StreamingAssets 目录。");
            result.Warnings.Add("Android 平台暂不支持直接复制 StreamingAssets 目录。");
            return;
#else
            if (!Directory.Exists(DefaultUserDataSourcePath))
            {
                Debug.LogWarning($"[RuntimePathInitializer] 未找到默认配置目录：{DefaultUserDataSourcePath}");
                result.Warnings.Add($"未找到默认配置目录：{DefaultUserDataSourcePath}");
                return;
            }

            CopyDirectoryMissingOnly(DefaultUserDataSourcePath, UserDataRootPath, result);
#endif
        }

        /// <summary>
        /// 递归复制目录。
        /// 只复制目标目录中不存在的文件，不覆盖已有文件。
        /// </summary>
        private static void CopyDirectoryMissingOnly(
            string sourceDirectory,
            string targetDirectory,
            RuntimePathInitializeResult result)
        {
            CreateDirectoryIfMissing(targetDirectory, result);

            // 复制当前目录下的文件
            string[] sourceFiles = Directory.GetFiles(sourceDirectory);
            foreach (string sourceFilePath in sourceFiles)
            {
                string fileName = Path.GetFileName(sourceFilePath);
                string targetFilePath = Path.Combine(targetDirectory, fileName);

                if (File.Exists(targetFilePath))
                {
                    continue;
                }

                File.Copy(sourceFilePath, targetFilePath);
                result.CopiedFiles.Add(targetFilePath);

                Debug.Log($"[RuntimePathInitializer] 复制默认文件：{targetFilePath}");
            }

            // 递归复制子目录
            string[] sourceSubDirectories = Directory.GetDirectories(sourceDirectory);
            foreach (string sourceSubDirectoryPath in sourceSubDirectories)
            {
                string directoryName = Path.GetFileName(sourceSubDirectoryPath);
                string targetSubDirectoryPath = Path.Combine(targetDirectory, directoryName);

                CopyDirectoryMissingOnly(sourceSubDirectoryPath, targetSubDirectoryPath, result);
            }
        }
    }
}