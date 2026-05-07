using System.IO;

namespace Project.Character
{
    /// <summary>
    /// 角色包基础路径信息。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该类只描述“某个角色包文件夹在哪里”，不负责解析 <c>character.json</c>，
    /// 也不负责校验文件是否完整。
    /// </para>
    /// <para>
    /// 使用方式：
    /// <code>
    /// var info = new CharacterPackageInfo(characterDirectory);
    /// string characterJsonPath = info.CharacterJsonPath;
    /// bool exists = info.HasCharacterJson;
    /// </code>
    /// </para>
    /// <para>
    /// 对外暴露属性：角色包根目录、character.json 路径、角色包文件夹名称、Live2D 根目录、
    /// 表情目录、动作目录和 character.json 是否存在。
    /// 对外暴露方法：<see cref="ResolvePath"/>。
    /// </para>
    /// </remarks>
    public class CharacterPackageInfo
    {
        /// <summary>
        /// 角色包根目录。
        /// </summary>
        /// <example>persistentDataPath/UserData/Characters/Huohuo</example>
        public string PackageRootPath { get; private set; }

        /// <summary>
        /// character.json 的完整路径。
        /// </summary>
        public string CharacterJsonPath => ResolvePath("character.json");

        /// <summary>
        /// 角色包文件夹名称。
        /// </summary>
        /// <remarks>
        /// 注意：该值来自文件夹名，不一定等于 <c>character.json</c> 中的 <c>characterId</c>。
        /// </remarks>
        public string PackageFolderName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(PackageRootPath))
                {
                    return string.Empty;
                }

                return new DirectoryInfo(PackageRootPath).Name;
            }
        }

        /// <summary>
        /// Live2D 根目录。
        /// </summary>
        public string Live2DRootPath => ResolvePath("live2d");

        /// <summary>
        /// Live2D 表情文件目录。
        /// </summary>
        public string ExpressionsRootPath => ResolvePath("live2d/expressions");

        /// <summary>
        /// Live2D 动作文件目录。
        /// </summary>
        public string MotionsRootPath => ResolvePath("live2d/motions");

        /// <summary>
        /// character.json 是否存在。
        /// </summary>
        public bool HasCharacterJson => File.Exists(CharacterJsonPath);

        /// <summary>
        /// 创建角色包路径信息对象。
        /// </summary>
        /// <param name="packageRootPath">角色包根目录。</param>
        public CharacterPackageInfo(string packageRootPath)
        {
            PackageRootPath = packageRootPath;
        }

        /// <summary>
        /// 把角色包内相对路径转换成绝对路径。
        /// </summary>
        /// <param name="relativePath">角色包内相对路径，例如 <c>persona.txt</c> 或 <c>live2d/Huohuo.model3.json</c>。</param>
        /// <returns>
        /// 返回规范化后的绝对路径。
        /// 如果 <paramref name="relativePath"/> 已经是绝对路径，则返回该绝对路径的规范化结果。
        /// 如果 <paramref name="relativePath"/> 为空，则返回角色包根目录。
        /// </returns>
        public string ResolvePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(PackageRootPath))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return PackageRootPath;
            }

            if (Path.IsPathRooted(relativePath))
            {
                return Path.GetFullPath(relativePath);
            }

            string normalizedRelativePath = NormalizeRelativePath(relativePath);
            return Path.GetFullPath(Path.Combine(PackageRootPath, normalizedRelativePath));
        }

        /// <summary>
        /// 统一处理 Windows、macOS、Linux 的路径分隔符。
        /// </summary>
        /// <param name="relativePath">原始相对路径。</param>
        /// <returns>使用当前操作系统路径分隔符的相对路径。</returns>
        private static string NormalizeRelativePath(string relativePath)
        {
            return relativePath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
        }
    }
}
