using System.Collections.Generic;
using System.Text;

namespace Project.Character
{
    /// <summary>
    /// 角色包校验结果。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该类用于承载 <see cref="CharacterPackageValidator.Validate"/> 的输出结果。
    /// 它不会主动校验任何文件，只负责记录错误、警告和基本定位信息。
    /// </para>
    /// <para>
    /// 使用方式：
    /// <code>
    /// CharacterValidationResult result = validator.Validate(packageInfo);
    /// if (!result.Valid)
    /// {
    ///     Debug.LogError(result.ToString());
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// 对外暴露成员：角色 ID、角色包路径、错误列表、警告列表、是否有效、添加错误/警告的方法和
    /// 用于 Debug.Log 的 <see cref="ToString"/>。
    /// </para>
    /// </remarks>
    public class CharacterValidationResult
    {
        /// <summary>
        /// 被校验角色的 ID。
        /// </summary>
        /// <remarks>
        /// 如果 character.json 可解析，则优先使用 characterId；否则使用角色包文件夹名作为临时标识。
        /// </remarks>
        public string CharacterId { get; set; }

        /// <summary>
        /// 角色包根目录。
        /// </summary>
        public string PackageRootPath { get; set; }

        /// <summary>
        /// 阻止角色包正常使用的错误列表。
        /// </summary>
        public List<string> Errors { get; private set; } = new List<string>();

        /// <summary>
        /// 不一定阻止角色包使用，但会影响扩展性、日志分析或后续功能的警告列表。
        /// </summary>
        public List<string> Warnings { get; private set; } = new List<string>();

        /// <summary>
        /// 角色包是否通过校验。
        /// </summary>
        /// <remarks>
        /// 当前规则：没有错误时认为角色包可用；警告不会让角色包失效。
        /// </remarks>
        public bool Valid => Errors.Count == 0;

        /// <summary>
        /// 添加一个错误信息。
        /// </summary>
        /// <param name="message">错误说明。</param>
        public void AddError(string message)
        {
            Errors.Add(message);
        }

        /// <summary>
        /// 添加一个警告信息。
        /// </summary>
        /// <param name="message">警告说明。</param>
        public void AddWarning(string message)
        {
            Warnings.Add(message);
        }

        /// <summary>
        /// 把校验结果转换成适合 Debug.Log 输出的多行文本。
        /// </summary>
        /// <returns>包含角色 ID、路径、Valid 状态、错误和警告详情的文本。</returns>
        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.AppendLine($"CharacterId: {CharacterId}");
            builder.AppendLine($"PackageRootPath: {PackageRootPath}");
            builder.AppendLine($"Valid: {Valid}");
            builder.AppendLine($"Errors: {Errors.Count}");
            builder.AppendLine($"Warnings: {Warnings.Count}");

            for (int i = 0; i < Errors.Count; i++)
            {
                builder.AppendLine($"[Error {i + 1}] {Errors[i]}");
            }

            for (int i = 0; i < Warnings.Count; i++)
            {
                builder.AppendLine($"[Warning {i + 1}] {Warnings[i]}");
            }

            return builder.ToString();
        }
    }
}
