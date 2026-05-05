using System.Collections.Generic;
using System.IO;
using Project.Infrastructure;

namespace Project.Character
{
    /// <summary>
    /// 角色包扫描器。
    ///
    /// 职责：
    /// 1. 扫描 persistentDataPath/UserData/Characters 下的角色文件夹。
    /// 2. 为每个角色文件夹创建 CharacterPackageInfo。
    /// 3. 不负责解析 character.json。
    /// 4. 不负责校验角色包完整性。
    ///
    /// 注意：
    /// CharacterPackageInfo 内部会根据 PackageRootPath 自动推导：
    /// - CharacterJsonPath
    /// - HasCharacterJson
    /// - Live2DRootPath
    /// - ExpressionsRootPath
    /// - MotionsRootPath
    /// </summary>
    public class CharacterPackageScanner
    {
        /// <summary>
        /// 扫描所有角色包目录。
        /// </summary>
        public List<CharacterPackageInfo> Scan()
        {
            var result = new List<CharacterPackageInfo>();

            string charactersRoot = RuntimePathInitializer.CharactersPath;

            // 如果 Characters 根目录不存在，直接返回空列表。
            // RuntimePathInitializer 正常执行后，这个目录应该已经存在。
            if (!Directory.Exists(charactersRoot))
            {
                return result;
            }

            string[] characterDirectories = Directory.GetDirectories(charactersRoot);

            foreach (string characterDirectory in characterDirectories)
            {
                // 这里只需要传入角色包根目录。
                // CharacterPackageInfo 会自动计算 character.json 路径和其他路径。
                var packageInfo = new CharacterPackageInfo(characterDirectory);

                result.Add(packageInfo);
            }

            return result;
        }
    }
}