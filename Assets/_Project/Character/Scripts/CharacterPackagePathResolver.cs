using System.IO;

namespace Project.Character
{
    /// <summary>
    /// 角色包路径解析工具。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 本工具统一处理角色包内部资源路径，避免 Validator、Loader、Presentation 等模块各自实现一套
    /// “文件名 / 相对路径 / 绝对路径”解析逻辑。
    /// </para>
    /// <para>
    /// 使用方式：
    /// <code>
    /// string path = CharacterPackagePathResolver.ResolveResourcePath(
    ///     characterRootPath,
    ///     "happy.exp3.json",
    ///     "live2d/expressions"
    /// );
    /// </code>
    /// </para>
    /// <para>
    /// 对外暴露方法：
    /// - NormalizeRelativePath：统一路径分隔符。
    /// - ResolvePath：把角色包内相对路径解析成绝对路径。
    /// - ResolveResourcePath：按标准资源目录解析表情、动作等资源文件。
    /// - TryResolveResourceFile：解析资源路径并检查文件是否存在。
    /// </para>
    /// </remarks>
    public static class CharacterPackagePathResolver
    {
        private const string Live2DFolderName = "live2d";

        /// <summary>
        /// 把相对路径中的 Windows / Unix 分隔符统一成当前系统分隔符。
        /// </summary>
        /// <param name="relativePath">角色包内相对路径。</param>
        /// <returns>标准化后的相对路径；输入为空时返回空字符串。</returns>
        public static string NormalizeRelativePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return string.Empty;
            }

            return relativePath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// 把角色包内相对路径转换成绝对路径。
        /// </summary>
        /// <param name="characterRootPath">角色包根目录。</param>
        /// <param name="pathOrFileName">角色包内相对路径、文件名或绝对路径。</param>
        /// <returns>解析后的绝对路径；输入非法时返回空字符串。</returns>
        public static string ResolvePath(string characterRootPath, string pathOrFileName)
        {
            if (string.IsNullOrWhiteSpace(characterRootPath))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(pathOrFileName))
            {
                return Path.GetFullPath(characterRootPath);
            }

            if (Path.IsPathRooted(pathOrFileName))
            {
                return Path.GetFullPath(pathOrFileName);
            }

            string normalizedRelativePath = NormalizeRelativePath(pathOrFileName);
            return Path.GetFullPath(Path.Combine(characterRootPath, normalizedRelativePath));
        }

        /// <summary>
        /// 解析角色资源文件路径。
        /// </summary>
        /// <param name="characterRootPath">角色包根目录。</param>
        /// <param name="pathOrFileName">文件名、角色包内相对路径或绝对路径。</param>
        /// <param name="standardRelativeFolder">标准资源目录，例如 live2d/expressions 或 live2d/motions。</param>
        /// <returns>
        /// 优先返回真实存在的文件路径；如果文件不存在，返回最合理的候选路径，便于日志定位。
        /// </returns>
        /// <remarks>
        /// 支持三种写法：
        /// 1. happy.exp3.json：按标准目录查找。
        /// 2. live2d/expressions/happy.exp3.json：按角色包相对路径查找。
        /// 3. D:/xxx/happy.exp3.json：按绝对路径查找。
        /// </remarks>
        public static string ResolveResourcePath(
            string characterRootPath,
            string pathOrFileName,
            string standardRelativeFolder)
        {
            if (string.IsNullOrWhiteSpace(characterRootPath) || string.IsNullOrWhiteSpace(pathOrFileName))
            {
                return string.Empty;
            }

            if (Path.IsPathRooted(pathOrFileName))
            {
                return Path.GetFullPath(pathOrFileName);
            }

            string normalizedPath = NormalizeRelativePath(pathOrFileName);
            string fileName = Path.GetFileName(normalizedPath);
            string relativeCandidate = ResolvePath(characterRootPath, normalizedPath);
            string standardCandidate = ResolvePath(characterRootPath, Path.Combine(standardRelativeFolder, fileName));

            if (File.Exists(relativeCandidate))
            {
                return relativeCandidate;
            }

            if (File.Exists(standardCandidate))
            {
                return standardCandidate;
            }

            string foundPath = FindFileRecursively(Path.Combine(characterRootPath, Live2DFolderName), fileName);
            if (!string.IsNullOrWhiteSpace(foundPath))
            {
                return foundPath;
            }

            return ContainsDirectorySeparator(pathOrFileName) ? relativeCandidate : standardCandidate;
        }

        /// <summary>
        /// 解析角色资源路径并检查文件是否真实存在。
        /// </summary>
        /// <param name="characterRootPath">角色包根目录。</param>
        /// <param name="pathOrFileName">文件名、角色包内相对路径或绝对路径。</param>
        /// <param name="standardRelativeFolder">标准资源目录。</param>
        /// <param name="resolvedPath">解析出的路径。</param>
        /// <returns>文件存在返回 true；否则返回 false。</returns>
        public static bool TryResolveResourceFile(
            string characterRootPath,
            string pathOrFileName,
            string standardRelativeFolder,
            out string resolvedPath)
        {
            resolvedPath = ResolveResourcePath(characterRootPath, pathOrFileName, standardRelativeFolder);
            return !string.IsNullOrWhiteSpace(resolvedPath) && File.Exists(resolvedPath);
        }

        /// <summary>
        /// 判断路径中是否包含目录分隔符。
        /// </summary>
        /// <param name="path">待检查路径。</param>
        /// <returns>包含目录层级返回 true；仅文件名返回 false。</returns>
        private static bool ContainsDirectorySeparator(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            return path.Contains("/") || path.Contains("\\");
        }

        /// <summary>
        /// 在指定目录下按文件名递归查找资源文件。
        /// </summary>
        /// <param name="rootPath">搜索根目录。</param>
        /// <param name="fileName">文件名。</param>
        /// <returns>找到时返回完整路径；未找到返回空字符串。</returns>
        private static string FindFileRecursively(string rootPath, string fileName)
        {
            if (string.IsNullOrWhiteSpace(rootPath) || string.IsNullOrWhiteSpace(fileName))
            {
                return string.Empty;
            }

            if (!Directory.Exists(rootPath))
            {
                return string.Empty;
            }

            string[] files = Directory.GetFiles(rootPath, fileName, SearchOption.AllDirectories);
            if (files == null || files.Length == 0)
            {
                return string.Empty;
            }

            return files[0];
        }
    }
}
