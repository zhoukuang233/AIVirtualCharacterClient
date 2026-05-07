using System.Collections.Generic;
using System.IO;
using Project.Infrastructure;

namespace Project.Character
{
    /// <summary>
    /// 角色包扫描器。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 职责：扫描 <c>RuntimePathInitializer.CharactersPath</c> 下的一级子目录，并为每个角色文件夹创建
    /// <see cref="CharacterPackageInfo"/>。
    /// </para>
    /// <para>
    /// 本类只负责发现“有哪些角色包目录”，不负责解析 <c>character.json</c>，也不负责校验角色包完整性。
    /// 解析交给 <see cref="CharacterPackageLoader"/>，校验交给 <see cref="CharacterPackageValidator"/>。
    /// </para>
    /// <para>
    /// 使用方式：
    /// <code>
    /// var scanner = new CharacterPackageScanner();
    /// List&lt;CharacterPackageInfo&gt; packages = scanner.Scan();
    /// </code>
    /// </para>
    /// <para>
    /// 对外暴露方法：<see cref="Scan"/>。
    /// </para>
    /// <para>
    /// TODO: 后续可以增加扫描排序规则，例如按角色名、最近修改时间或配置文件中的 displayOrder 排序。
    /// </para>
    /// </remarks>
    public class CharacterPackageScanner
    {
        /// <summary>
        /// 扫描所有角色包目录。
        /// </summary>
        /// <returns>
        /// 返回角色包基础信息列表。即使没有角色包或 Characters 目录不存在，也会返回空列表而不是 null。
        /// </returns>
        public List<CharacterPackageInfo> Scan()
        {
            var result = new List<CharacterPackageInfo>();
            string charactersRoot = RuntimePathInitializer.CharactersPath;

            if (!Directory.Exists(charactersRoot))
            {
                return result;
            }

            string[] characterDirectories = Directory.GetDirectories(charactersRoot);
            foreach (string characterDirectory in characterDirectories)
            {
                var packageInfo = new CharacterPackageInfo(characterDirectory);
                result.Add(packageInfo);
            }

            return result;
        }
    }
}
