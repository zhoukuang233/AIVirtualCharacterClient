using System;
using System.IO;
using UnityEngine;

namespace Project.Infrastructure
{
    /// <summary>
    /// 运行时路径初始化器。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该类统一管理 AI 虚拟角色前端在运行时需要读写的目录结构。
    /// 项目设计上将默认配置放在 <c>Assets/StreamingAssets/DefaultUserData</c>，
    /// 首次启动时复制到 <c>Application.persistentDataPath/UserData</c>。
    /// 运行时主要读取和修改 persistentDataPath 下的数据，避免打包后无法改动 StreamingAssets。
    /// </para>
    /// <para>
    /// 对外暴露成员：
    /// <list type="bullet">
    /// <item><description><see cref="UserDataRootPath"/>：运行时 UserData 根目录。</description></item>
    /// <item><description><see cref="CharactersPath"/>：角色包目录。</description></item>
    /// <item><description><see cref="ServicesPath"/>：服务配置目录。</description></item>
    /// <item><description><see cref="LogsPath"/>：前端日志目录。</description></item>
    /// <item><description><see cref="ExperimentsPath"/>：实验数据目录。</description></item>
    /// <item><description><see cref="Initialize"/>：创建目录并复制默认配置。</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// TODO: 如果未来支持 Android 或 WebGL，需要为 StreamingAssets 的读取提供平台专用实现，
    /// 例如使用 UnityWebRequest 读取清单文件再逐个复制。
    /// </para>
    /// </remarks>
    public static class RuntimePathInitializer
    {
        private const string UserDataFolderName = "UserData";
        private const string DefaultUserDataFolderName = "DefaultUserData";
        private const string CharactersFolderName = "Characters";
        private const string ServicesFolderName = "Services";
        private const string LogsFolderName = "Logs";
        private const string ExperimentsFolderName = "Experiments";

        /// <summary>
        /// 运行时 UserData 根目录。
        /// </summary>
        public static string UserDataRootPath => Path.Combine(Application.persistentDataPath, UserDataFolderName);

        /// <summary>
        /// 角色包根目录。
        /// 每个角色包应作为该目录下的一个子文件夹存在。
        /// </summary>
        public static string CharactersPath => Path.Combine(UserDataRootPath, CharactersFolderName);

        /// <summary>
        /// 服务配置目录。
        /// 后续可放置 service_config.json、LLM/TTS 配置名、后端地址等前端侧配置。
        /// </summary>
        public static string ServicesPath => Path.Combine(UserDataRootPath, ServicesFolderName);

        /// <summary>
        /// 前端日志目录。
        /// 后续可用于保存 JSONL、CSV 或普通文本日志。
        /// </summary>
        public static string LogsPath => Path.Combine(UserDataRootPath, LogsFolderName);

        /// <summary>
        /// 实验数据目录。
        /// 后续可用于保存实验配置、实验结果、回放数据等。
        /// </summary>
        public static string ExperimentsPath => Path.Combine(UserDataRootPath, ExperimentsFolderName);

        /// <summary>
        /// StreamingAssets 中默认 UserData 的源目录。
        /// </summary>
        private static string DefaultUserDataSourcePath => Path.Combine(Application.streamingAssetsPath, DefaultUserDataFolderName);

        /// <summary>
        /// 初始化运行时目录，并补充默认配置文件。
        /// </summary>
        /// <returns>
        /// 返回 <see cref="RuntimePathInitializeResult"/>，其中包含是否成功、新建目录、复制文件和警告信息。
        /// </returns>
        /// <remarks>
        /// 建议在 <c>AppBootstrapper.Awake</c> 或更早的启动阶段调用。
        /// 该方法不会覆盖已有文件，因此用户修改过的角色包或服务配置不会在下次启动时被重置。
        /// </remarks>
        public static RuntimePathInitializeResult Initialize()
        {
            var result = new RuntimePathInitializeResult();

            try
            {
                // 1. 创建 UserData 根目录。
                CreateDirectoryIfMissing(UserDataRootPath, result);

                // 2. 从 StreamingAssets/DefaultUserData 复制缺失的默认配置。
                CopyDefaultUserDataIfExists(result);

                // 3. 再次确保核心目录一定存在。
                // 即使没有 DefaultUserData，也要保证 MVP 可以正常启动。
                CreateDirectoryIfMissing(CharactersPath, result);
                CreateDirectoryIfMissing(ServicesPath, result);
                CreateDirectoryIfMissing(LogsPath, result);
                CreateDirectoryIfMissing(ExperimentsPath, result);

                result.Success = true;
                result.UserDataRootPath = UserDataRootPath;

                Debug.Log("[RuntimePathInitializer] UserData 初始化完成。");
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
        /// 如果目录不存在，则创建目录，并把新建目录记录到初始化结果中。
        /// </summary>
        /// <param name="path">要确保存在的目录路径。</param>
        /// <param name="result">用于记录新建目录的初始化结果对象。</param>
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
        /// 如果 StreamingAssets 中存在默认配置目录，则把缺失文件复制到运行时 UserData 目录。
        /// </summary>
        /// <param name="result">用于记录复制文件和警告信息的初始化结果对象。</param>
        /// <remarks>
        /// 复制策略：只补充目标目录中不存在的文件，不覆盖已有文件。
        /// </remarks>
        private static void CopyDefaultUserDataIfExists(RuntimePathInitializeResult result)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Android 平台下 StreamingAssets 位于压缩包中，不能直接通过 Directory API 枚举目录。
            // TODO: 如需移动端支持，应改为 UnityWebRequest + 文件清单的方式复制默认配置。
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
        /// 递归复制目录，只复制目标目录中不存在的文件。
        /// </summary>
        /// <param name="sourceDirectory">源目录。</param>
        /// <param name="targetDirectory">目标目录。</param>
        /// <param name="result">用于记录新建目录和复制文件的初始化结果对象。</param>
        private static void CopyDirectoryMissingOnly(
            string sourceDirectory,
            string targetDirectory,
            RuntimePathInitializeResult result)
        {
            CreateDirectoryIfMissing(targetDirectory, result);

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
