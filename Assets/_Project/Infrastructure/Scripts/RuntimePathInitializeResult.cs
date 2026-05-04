using System.Collections.Generic;

namespace Project.Infrastructure
{
    /// <summary>
    /// 运行时路径初始化结果。
    /// 
    /// 用这个对象返回初始化过程中的关键信息，
    /// 方便后续 DeveloperConsole 显示初始化状态。
    /// </summary>
    public class RuntimePathInitializeResult
    {
        /// <summary>
        /// 初始化是否成功。
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// UserData 根目录路径。
        /// </summary>
        public string UserDataRootPath { get; set; }

        /// <summary>
        /// 初始化失败时的错误信息。
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 本次启动新创建的目录。
        /// </summary>
        public List<string> CreatedDirectories { get; } = new();

        /// <summary>
        /// 本次启动从 DefaultUserData 复制的文件。
        /// </summary>
        public List<string> CopiedFiles { get; } = new();

        /// <summary>
        /// 初始化过程中的非致命警告。
        /// </summary>
        public List<string> Warnings { get; } = new();
    }
}