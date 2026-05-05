using System.IO;

namespace Project.Character
{
    /// <summary>
    /// 角色包基础信息。
    ///
    /// 这个类只描述“某个角色包文件夹在哪里”，
    /// 不负责真正解析 character.json，也不负责校验内容。
    /// </summary>
    public class CharacterPackageInfo
    {
        /// <summary>
        /// 角色包根目录。
        /// 例如：
        /// persistentDataPath/UserData/Characters/Koharu
        /// </summary>
        public string PackageRootPath { get; private set; }

        /// <summary>
        /// character.json 的完整路径。
        /// </summary>
        public string CharacterJsonPath
        {
            get { return ResolvePath("character.json"); }
        }

        /// <summary>
        /// 角色包文件夹名称。
        /// 例如 Koharu。
        /// 注意：这不一定等于 characterId。
        /// </summary>
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
        public string Live2DRootPath
        {
            get { return ResolvePath("live2d"); }
        }

        /// <summary>
        /// Live2D 表情文件目录。
        /// </summary>
        public string ExpressionsRootPath
        {
            get { return ResolvePath("live2d/expressions"); }
        }

        /// <summary>
        /// Live2D 动作文件目录。
        /// </summary>
        public string MotionsRootPath
        {
            get { return ResolvePath("live2d/motions"); }
        }

        /// <summary>
        /// character.json 是否存在。
        /// </summary>
        public bool HasCharacterJson
        {
            get { return File.Exists(CharacterJsonPath); }
        }

        public CharacterPackageInfo(string packageRootPath)
        {
            PackageRootPath = packageRootPath;
        }

        /// <summary>
        /// 把角色包内的相对路径转换成绝对路径。
        ///
        /// 支持：
        /// live2d/Koharu.model3.json
        /// persona.txt
        /// expression_mapping.json
        /// </summary>
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
        /// 统一处理 Windows / macOS / Linux 路径分隔符。
        /// JSON 配置中建议统一写 /。
        /// </summary>
        private static string NormalizeRelativePath(string relativePath)
        {
            return relativePath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
        }
    }
}