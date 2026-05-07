using System.Collections.Generic;

namespace Project.Infrastructure
{
    /// <summary>
    /// 运行时路径初始化结果。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该类是 <see cref="RuntimePathInitializer.Initialize"/> 的返回值，用于把初始化过程中的状态、
    /// 新建目录、复制文件和警告信息统一返回给启动器或开发者控制台。
    /// </para>
    /// <para>
    /// 使用方式：
    /// <code>
    /// RuntimePathInitializeResult result = RuntimePathInitializer.Initialize();
    /// if (!result.Success)
    /// {
    ///     Debug.LogError(result.ErrorMessage);
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// 对外暴露成员：
    /// <list type="bullet">
    /// <item><description><see cref="Success"/>：初始化是否成功。</description></item>
    /// <item><description><see cref="UserDataRootPath"/>：运行时 UserData 根目录。</description></item>
    /// <item><description><see cref="ErrorMessage"/>：失败时的错误信息。</description></item>
    /// <item><description><see cref="CreatedDirectories"/>：本次启动创建的目录列表。</description></item>
    /// <item><description><see cref="CopiedFiles"/>：本次启动复制的默认配置文件列表。</description></item>
    /// <item><description><see cref="Warnings"/>：初始化过程中的非致命警告。</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class RuntimePathInitializeResult
    {
        /// <summary>
        /// 初始化是否成功。
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// <c>Application.persistentDataPath/UserData</c> 的完整路径。
        /// </summary>
        public string UserDataRootPath { get; set; }

        /// <summary>
        /// 初始化失败时的错误信息。
        /// 成功时通常为空字符串或 null。
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 本次启动新创建的目录。
        /// </summary>
        public List<string> CreatedDirectories { get; } = new List<string>();

        /// <summary>
        /// 本次启动从 <c>StreamingAssets/DefaultUserData</c> 复制到运行时目录的文件。
        /// </summary>
        public List<string> CopiedFiles { get; } = new List<string>();

        /// <summary>
        /// 初始化过程中的非致命警告。
        /// </summary>
        /// <remarks>
        /// 例如：没有找到 DefaultUserData 目录、Android 平台暂不支持直接复制 StreamingAssets 目录等。
        /// </remarks>
        public List<string> Warnings { get; } = new List<string>();
    }
}
