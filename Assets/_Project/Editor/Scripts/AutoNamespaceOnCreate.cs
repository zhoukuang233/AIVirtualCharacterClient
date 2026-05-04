using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Project.Editor
{
    /// <summary>
    /// 在 Unity 中创建 C# 脚本时，自动根据脚本所在路径添加 namespace。
    /// 
    /// 示例：
    /// Assets/_Project/Gameplay/Scripts/Player/PlayerController.cs
    /// 会自动生成：
    /// namespace Project.Gameplay.Player
    /// </summary>
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
        /// 
        /// 例如：
        /// Assets/_Project/Gameplay/Scripts/Player
        /// 会生成：
        /// Project.Gameplay.Player
        /// 而不是：
        /// Project.Gameplay.Scripts.Player
        /// </summary>
        private const string ScriptsFolderName = "Scripts";

        /// <summary>
        /// namespace 的固定前缀。
        /// 最终生成的 namespace 会以 Project 开头。
        /// </summary>
        private const string NamespacePrefix = "Project";

        /// <summary>
        /// C# 关键字集合。
        /// 
        /// 当文件夹名刚好是 C# 关键字时，需要在前面加 @，
        /// 否则不能作为合法的 namespace 片段。
        /// 
        /// 例如：
        /// class -> @class
        /// namespace -> @namespace
        /// </summary>
        private static readonly HashSet<string> CSharpKeywords = new()
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch",
            "char", "checked", "class", "const", "continue", "decimal", "default",
            "delegate", "do", "double", "else", "enum", "event", "explicit",
            "extern", "false", "finally", "fixed", "float", "for", "foreach",
            "goto", "if", "implicit", "in", "int", "interface", "internal",
            "is", "lock", "long", "namespace", "new", "null", "object",
            "operator", "out", "override", "params", "private", "protected",
            "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct",
            "switch", "this", "throw", "true", "try", "typeof", "uint",
            "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
            "void", "volatile", "while"
        };

        /// <summary>
        /// Unity 在创建资源前会调用该方法。
        /// 
        /// 注意：
        /// Unity 创建脚本时，通常会先创建 .cs 文件，
        /// 然后再创建对应的 .cs.meta 文件。
        /// 这里通过监听 .cs.meta 的创建，来判断一个 C# 脚本已经被创建。
        /// </summary>
        /// <param name="assetName">
        /// Unity 传入的新建资源路径。
        /// 例如：
        /// Assets/_Project/Gameplay/Scripts/Player/PlayerController.cs.meta
        /// </param>
        private static void OnWillCreateAsset(string assetName)
        {
            // 统一路径分隔符，避免 Windows 下出现反斜杠导致路径判断失败。
            string assetPath = assetName.Replace("\\", "/");

            // 只处理 C# 脚本对应的 .meta 文件。
            // 其他资源或非 C# 脚本不处理。
            if (!assetPath.EndsWith(".cs.meta", StringComparison.OrdinalIgnoreCase))
                return;

            // 去掉 .meta 后缀，得到真正的 C# 脚本路径。
            // 例如：
            // xxx.cs.meta -> xxx.cs
            string scriptPath = assetPath.Substring(0, assetPath.Length - ".meta".Length);

            // 只处理项目指定根目录下的脚本。
            // 防止影响 Assets 目录下其他插件、第三方代码或 Unity 自动生成代码。
            if (!scriptPath.StartsWith(ProjectRoot + "/", StringComparison.OrdinalIgnoreCase))
                return;

            // 延迟一帧执行。
            // 因为 OnWillCreateAsset 被调用时，脚本文件可能还没有完全写入磁盘。
            // 使用 delayCall 可以确保文件已经创建完成后再读取和修改。
            EditorApplication.delayCall += () =>
            {
                AddNamespaceIfNeeded(scriptPath);
            };
        }

        /// <summary>
        /// 如果脚本中还没有 namespace，则根据路径自动生成 namespace 并包裹脚本内容。
        /// </summary>
        /// <param name="assetPath">
        /// C# 脚本在 Unity 项目中的相对路径。
        /// 例如：
        /// Assets/_Project/Gameplay/Scripts/Player/PlayerController.cs
        /// </param>
        private static void AddNamespaceIfNeeded(string assetPath)
        {
            // 将 Unity 相对路径转换为系统完整路径，方便使用 File API 读取文件。
            string fullPath = Path.GetFullPath(assetPath);

            // 文件不存在时直接返回，避免读取时报错。
            if (!File.Exists(fullPath))
                return;

            // 读取脚本文件的完整文本内容。
            string text = File.ReadAllText(fullPath);

            // 如果脚本已经声明了 namespace，则不重复添加。
            if (HasNamespace(text))
                return;

            // 根据脚本路径生成 namespace 名称。
            string namespaceName = BuildNamespaceFromPath(assetPath);

            // 如果路径无法生成有效 namespace，则不处理。
            if (string.IsNullOrWhiteSpace(namespaceName))
                return;

            // 将原脚本内容用 namespace 包裹起来。
            string newText = WrapWithNamespace(text, namespaceName);

            // 将修改后的内容写回脚本文件。
            File.WriteAllText(fullPath, newText);

            // 强制 Unity 重新导入该脚本，使修改立即生效。
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

                // 匹配形如：
                // namespace Project.Gameplay
                //
                // 说明：
                // ^\s*       匹配行首空白
                // namespace  匹配 namespace 关键字
                // [\w\.@]+   匹配 namespace 名称，允许字母、数字、下划线、点和 @
                @"^\s*namespace\s+[\w\.@]+",

                // 允许 ^ 匹配每一行的开头，而不是只匹配整个文本开头。
                RegexOptions.Multiline
            );
        }

        /// <summary>
        /// 根据脚本所在路径生成 namespace。
        /// </summary>
        /// <param name="assetPath">
        /// C# 脚本路径。
        /// 例如：
        /// Assets/_Project/Gameplay/Scripts/Player/PlayerController.cs
        /// </param>
        /// <returns>
        /// 生成后的 namespace。
        /// 例如：
        /// Project.Gameplay.Player
        /// </returns>
        private static string BuildNamespaceFromPath(string assetPath)
        {
            // 获取脚本所在目录，并统一路径分隔符。
            string directory = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");

            // 如果目录为空，说明路径无效，返回空字符串。
            if (string.IsNullOrEmpty(directory))
                return string.Empty;

            // 只允许处理 ProjectRoot 下的脚本。
            if (!directory.StartsWith(ProjectRoot + "/", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            // 获取相对于 ProjectRoot 的目录路径。
            //
            // 例如：
            // Assets/_Project/Gameplay/Scripts/Player
            // 会变成：
            // Gameplay/Scripts/Player
            string relativeDirectory = directory.Substring(ProjectRoot.Length).Trim('/');

            // 将相对路径拆分成 namespace 的各级片段。
            IEnumerable<string> namespaceParts = relativeDirectory

                // 按路径分隔符拆分目录。
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)

                // 忽略 Scripts 目录。
                .Where(part => part != ScriptsFolderName)

                // 将目录名转换成合法的 C# 标识符。
                .Select(ToValidIdentifier)

                // 过滤掉转换失败或为空的片段。
                .Where(part => !string.IsNullOrWhiteSpace(part));

            // 使用点号拼接 namespace。
            //
            // 例如：
            // Gameplay + Player
            // 会变成：
            // Gameplay.Player
            string moduleNamespace = string.Join(".", namespaceParts);

            // 如果没有任何有效路径片段，则只返回固定前缀 Project。
            // 否则返回 Project.xxx.xxx。
            return string.IsNullOrWhiteSpace(moduleNamespace)
                ? NamespacePrefix
                : $"{NamespacePrefix}.{moduleNamespace}";
        }

        /// <summary>
        /// 将文件夹名称转换为合法的 C# 标识符。
        /// 
        /// namespace 的每一段本质上都必须是合法标识符。
        /// 所以这里会处理特殊字符、数字开头和 C# 关键字等情况。
        /// </summary>
        /// <param name="name">原始文件夹名称。</param>
        /// <returns>合法的 C# 标识符。</returns>
        private static string ToValidIdentifier(string name)
        {
            // 删除所有非法字符。
            // 只保留字母、数字和下划线。
            //
            // 例如：
            // Player-Controller -> PlayerController
            // UI.Panel -> UIPanel
            string identifier = Regex.Replace(name, @"[^a-zA-Z0-9_]", "");

            // 如果删除非法字符后为空，则返回空字符串。
            if (string.IsNullOrWhiteSpace(identifier))
                return string.Empty;

            // C# 标识符不能以数字开头。
            // 如果第一个字符不是字母或下划线，则在前面补一个下划线。
            //
            // 例如：
            // 3D -> _3D
            if (!Regex.IsMatch(identifier, @"^[a-zA-Z_]"))
                identifier = "_" + identifier;

            // 如果文件夹名刚好是 C# 关键字，则加 @ 转义。
            //
            // 例如：
            // class -> @class
            if (CSharpKeywords.Contains(identifier))
                identifier = "@" + identifier;

            return identifier;
        }

        /// <summary>
        /// 将脚本内容包裹到指定 namespace 中。
        /// 
        /// 处理规则：
        /// 1. 保留文件顶部的 using 语句在 namespace 外面。
        /// 2. 将 using 后面的主体内容放入 namespace 内部。
        /// 3. 给主体内容整体增加一级缩进。
        /// </summary>
        /// <param name="text">原始脚本文本。</param>
        /// <param name="namespaceName">要添加的 namespace 名称。</param>
        /// <returns>添加 namespace 后的新脚本文本。</returns>
        private static string WrapWithNamespace(string text, string namespaceName)
        {
            // 将脚本拆分成两部分：
            // 1. 顶部连续的 using 语句
            // 2. using 后面的主体内容
            Match match = Regex.Match(
                text,

                // 正则说明：
                // \A                      匹配整个文本开头
                // (?<usings>...)          捕获顶部 using 语句区域
                // (?:\s*using\s+[^;]+;\s*)* 匹配零个或多个 using 语句
                // (?<body>.*)             捕获剩余主体内容
                // \z                      匹配整个文本结尾
                @"\A(?<usings>(?:\s*using\s+[^;]+;\s*)*)(?<body>.*)\z",

                // 让 . 可以匹配换行符，从而完整捕获整个脚本。
                RegexOptions.Singleline
            );

            // 去掉 using 区域末尾多余空白，方便后续重新格式化。
            string usings = match.Groups["usings"].Value.TrimEnd();

            // 去掉主体内容开头的空行，避免 namespace 内部一开始出现多余空行。
            string body = match.Groups["body"].Value.TrimStart('\r', '\n');

            // 给主体内容中每一个非空行增加 4 个空格缩进。
            // 空行保持为空，避免产生带空格的空白行。
            string indentedBody = Regex.Replace(
                body,
                @"^(?!\s*$)",
                "    ",
                RegexOptions.Multiline
            );

            // 如果脚本顶部没有 using 语句，则直接输出 namespace 包裹后的内容。
            if (string.IsNullOrWhiteSpace(usings))
            {
                return
                    $"namespace {namespaceName}\n" +
                    "{\n" +
                    $"{indentedBody}\n" +
                    "}\n";
            }

            // 如果脚本顶部有 using 语句，则保留 using 在 namespace 外部。
            return
                $"{usings}\n\n" +
                $"namespace {namespaceName}\n" +
                "{\n" +
                $"{indentedBody}\n" +
                "}\n";
        }
    }
}