using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Project.Editor
{
    /// <summary>
    /// Unity 新建 C# 脚本时自动添加 namespace 的编辑器工具。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该类在 Unity Editor 中运行，不会进入最终游戏包。它通过继承
    /// <see cref="AssetModificationProcessor"/> 监听脚本资源创建事件，并根据脚本所在路径生成命名空间。
    /// </para>
    /// <para>
    /// 示例：
    /// <c>Assets/_Project/Gameplay/Scripts/Player/PlayerController.cs</c>
    /// 会生成：<c>namespace Project.Gameplay.Player</c>。
    /// 其中 <c>Scripts</c> 目录会被忽略，避免生成 <c>Project.Gameplay.Scripts.Player</c>。
    /// </para>
    /// <para>
    /// 对外暴露成员：没有 public 字段或 public 方法。
    /// Unity Editor 会自动调用私有静态方法 <c>OnWillCreateAsset</c>。
    /// </para>
    /// <para>
    /// TODO: 后续可以把 ProjectRoot、NamespacePrefix、ScriptsFolderName 抽成 Editor 配置，
    /// 方便不同项目复用。
    /// </para>
    /// </remarks>
    public sealed class AutoNamespaceOnCreate : AssetModificationProcessor
    {
        /// <summary>
        /// 项目代码根目录。
        /// 只有位于该目录下的脚本才会自动添加 namespace。
        /// </summary>
        private const string ProjectRoot = "Assets/_Project";

        /// <summary>
        /// 脚本目录名称。
        /// 构建 namespace 时会忽略名为 Scripts 的目录。
        /// </summary>
        private const string ScriptsFolderName = "Scripts";

        /// <summary>
        /// namespace 的固定前缀。
        /// </summary>
        private const string NamespacePrefix = "Project";

        /// <summary>
        /// C# 关键字集合。
        /// </summary>
        /// <remarks>
        /// 当文件夹名刚好是 C# 关键字时，需要在前面加 @，否则不能作为合法的 namespace 片段。
        /// </remarks>
        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum",
            "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto",
            "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
            "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
            "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
            "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
            "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
        };

        /// <summary>
        /// Unity 在创建资源前调用的方法。
        /// </summary>
        /// <param name="assetName">
        /// Unity 传入的新建资源路径，例如
        /// <c>Assets/_Project/Gameplay/Scripts/Player/PlayerController.cs.meta</c>。
        /// </param>
        /// <remarks>
        /// Unity 创建脚本时，通常会先创建 .cs 文件，然后再创建对应的 .cs.meta 文件。
        /// 这里通过监听 .cs.meta 的创建，判断一个 C# 脚本已经被创建。
        /// </remarks>
        private static void OnWillCreateAsset(string assetName)
        {
            string assetPath = assetName.Replace("\\", "/");

            if (!assetPath.EndsWith(".cs.meta", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string scriptPath = assetPath.Substring(0, assetPath.Length - ".meta".Length);

            if (!scriptPath.StartsWith(ProjectRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                AddNamespaceIfNeeded(scriptPath);
            };
        }

        /// <summary>
        /// 如果脚本中还没有 namespace，则根据路径自动生成 namespace 并包裹脚本内容。
        /// </summary>
        /// <param name="assetPath">
        /// C# 脚本在 Unity 项目中的相对路径，例如
        /// <c>Assets/_Project/Gameplay/Scripts/Player/PlayerController.cs</c>。
        /// </param>
        private static void AddNamespaceIfNeeded(string assetPath)
        {
            string fullPath = Path.GetFullPath(assetPath);

            if (!File.Exists(fullPath))
            {
                return;
            }

            string text = File.ReadAllText(fullPath);

            if (HasNamespace(text))
            {
                return;
            }

            string namespaceName = BuildNamespaceFromPath(assetPath);
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return;
            }

            string newText = WrapWithNamespace(text, namespaceName);
            File.WriteAllText(fullPath, newText);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>
        /// 判断脚本内容中是否已经存在 namespace 声明。
        /// </summary>
        /// <param name="text">脚本文件文本内容。</param>
        /// <returns>存在 namespace 返回 true，否则返回 false。</returns>
        private static bool HasNamespace(string text)
        {
            return Regex.IsMatch(
                text,
                @"^\s*namespace\s+[\w\.@]+",
                RegexOptions.Multiline
            );
        }

        /// <summary>
        /// 根据脚本所在路径生成 namespace。
        /// </summary>
        /// <param name="assetPath">
        /// C# 脚本路径，例如 <c>Assets/_Project/Gameplay/Scripts/Player/PlayerController.cs</c>。
        /// </param>
        /// <returns>生成后的 namespace，例如 <c>Project.Gameplay.Player</c>。</returns>
        private static string BuildNamespaceFromPath(string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");

            if (string.IsNullOrEmpty(directory))
            {
                return string.Empty;
            }

            if (!directory.StartsWith(ProjectRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            string relativeDirectory = directory.Substring(ProjectRoot.Length).Trim('/');

            IEnumerable<string> namespaceParts = relativeDirectory
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(part => part != ScriptsFolderName)
                .Select(ToValidIdentifier)
                .Where(part => !string.IsNullOrWhiteSpace(part));

            string moduleNamespace = string.Join(".", namespaceParts);
            return string.IsNullOrWhiteSpace(moduleNamespace) ? NamespacePrefix : $"{NamespacePrefix}.{moduleNamespace}";
        }

        /// <summary>
        /// 将文件夹名称转换为合法的 C# 标识符。
        /// </summary>
        /// <param name="name">原始文件夹名称。</param>
        /// <returns>合法的 C# 标识符；无法转换时返回空字符串。</returns>
        private static string ToValidIdentifier(string name)
        {
            string identifier = Regex.Replace(name, @"[^a-zA-Z0-9_]", "");

            if (string.IsNullOrWhiteSpace(identifier))
            {
                return string.Empty;
            }

            if (!Regex.IsMatch(identifier, @"^[a-zA-Z_]"))
            {
                identifier = "_" + identifier;
            }

            if (CSharpKeywords.Contains(identifier))
            {
                identifier = "@" + identifier;
            }

            return identifier;
        }

        /// <summary>
        /// 将脚本内容包裹到指定 namespace 中。
        /// </summary>
        /// <param name="text">原始脚本文本。</param>
        /// <param name="namespaceName">要添加的 namespace 名称。</param>
        /// <returns>添加 namespace 后的新脚本文本。</returns>
        /// <remarks>
        /// 处理规则：保留文件顶部连续 using 语句在 namespace 外部，把 using 后面的主体内容放入 namespace 内部，
        /// 并给主体内容整体增加一级缩进。
        /// </remarks>
        private static string WrapWithNamespace(string text, string namespaceName)
        {
            Match match = Regex.Match(
                text,
                @"\A(?<usings>(?:\s*using\s+[^;]+;\s*)*)(?<body>.*)\z",
                RegexOptions.Singleline
            );

            string usings = match.Groups["usings"].Value.TrimEnd();
            string body = match.Groups["body"].Value.TrimStart('\r', '\n');
            string indentedBody = Regex.Replace(body, @"^(?!\s*$)", "    ", RegexOptions.Multiline);

            if (string.IsNullOrWhiteSpace(usings))
            {
                return $"namespace {namespaceName}\n" +
                       "{\n" +
                       $"{indentedBody}\n" +
                       "}\n";
            }

            return $"{usings}\n\n" +
                   $"namespace {namespaceName}\n" +
                   "{\n" +
                   $"{indentedBody}\n" +
                   "}\n";
        }
    }
}
