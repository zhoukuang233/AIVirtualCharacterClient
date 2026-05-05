using System.Collections.Generic;
using System.Text;

namespace Project.Character
{
    /// <summary>
    /// 角色包校验结果。
    /// </summary>
    public class CharacterValidationResult
    {
        public string CharacterId { get; set; }

        public string PackageRootPath { get; set; }

        public List<string> Errors { get; private set; } = new List<string>();

        public List<string> Warnings { get; private set; } = new List<string>();

        /// <summary>
        /// 没有错误时，认为角色包可用。
        /// </summary>
        public bool Valid
        {
            get { return Errors.Count == 0; }
        }

        public void AddError(string message)
        {
            Errors.Add(message);
        }

        public void AddWarning(string message)
        {
            Warnings.Add(message);
        }

        /// <summary>
        /// 方便 Debug.Log 直接输出。
        /// </summary>
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