using System;
using System.Collections.Generic;
using Project.Infrastructure;
using UnityEngine;

namespace Project.Character
{
    /// <summary>
    /// 当前角色上下文。
    ///
    /// 功能：
    /// 1. 维护当前正在使用的角色包数据。
    /// 2. 统一提供角色切换、默认角色加载、当前角色读取能力。
    /// 3. 让 ChatSystem、PresentationSystem、DeveloperConsole 不需要各自重复扫描和加载角色包。
    ///
    /// 使用方式：
    /// 1. 启动时由 AppBootstrapper 调用 TryLoadFirstValidCharacter。
    /// 2. DeveloperConsole 后续可以调用 TryLoadCharacterById 切换角色。
    /// 3. 其他模块通过 CurrentCharacterContext.Instance.CurrentCharacter 读取当前角色数据。
    ///
    /// 对外暴露：
    /// - CurrentCharacter：当前角色包数据。
    /// - LastValidationResult：最近一次角色包校验结果。
    /// - LastErrorMessage：最近一次错误信息。
    /// - HasCurrentCharacter：是否已有当前角色。
    /// - CurrentCharacterChanged：当前角色切换事件。
    /// - TryLoadFirstValidCharacter：加载第一个可用角色。
    /// - TryLoadCharacterById：按角色 ID 或角色包文件夹名加载角色。
    /// - SetCurrentCharacter：直接设置当前角色。
    /// - Clear：清空当前角色。
    ///
    /// TODO：
    /// - 后续接入 SettingsSystem，根据 activeCharacterId 加载默认角色。
    /// - 后续接入 EventBus，角色切换时发布 CharacterChangedEvent。
    /// - 后续接入 ExperimentLogging，记录角色切换和配置版本。
    /// </summary>
    public class CurrentCharacterContext : SingletonMonoBehaviour<CurrentCharacterContext>
    {
        private readonly CharacterPackageScanner _scanner = new CharacterPackageScanner();
        private readonly CharacterPackageValidator _validator = new CharacterPackageValidator();
        private readonly CharacterPackageLoader _loader = new CharacterPackageLoader();

        /// <summary>
        /// 当前已加载的角色包数据。
        /// </summary>
        public CharacterPackageData CurrentCharacter { get; private set; }

        /// <summary>
        /// 最近一次角色包校验结果。
        /// </summary>
        public CharacterValidationResult LastValidationResult { get; private set; }

        /// <summary>
        /// 最近一次加载或切换角色时产生的错误信息。
        /// </summary>
        public string LastErrorMessage { get; private set; }

        /// <summary>
        /// 当前是否已经存在可用角色。
        /// </summary>
        public bool HasCurrentCharacter
        {
            get { return CurrentCharacter != null; }
        }

        /// <summary>
        /// 当前角色发生变化时触发。
        ///
        /// 参数：
        /// CharacterPackageData：新的当前角色数据。
        /// </summary>
        public event Action<CharacterPackageData> CurrentCharacterChanged;

        /// <summary>
        /// 当前角色上下文属于全局运行状态，应该跨场景保留。
        /// </summary>
        protected override bool IsPersistent
        {
            get { return true; }
        }

        /// <summary>
        /// 加载第一个通过校验的角色包。
        ///
        /// 功能：
        /// 1. 扫描 Characters 目录下所有角色包。
        /// 2. 逐个校验角色包完整性。
        /// 3. 加载第一个校验通过且可解析的角色包。
        /// 4. 设置为当前角色。
        ///
        /// 参数：
        /// characterData：输出成功加载的角色包数据。
        /// message：输出加载结果说明或错误原因。
        ///
        /// 返回：
        /// true 表示加载成功；false 表示没有找到可用角色或加载失败。
        /// </summary>
        public bool TryLoadFirstValidCharacter(out CharacterPackageData characterData, out string message)
        {
            characterData = null;
            message = string.Empty;

            List<CharacterPackageInfo> packages = _scanner.Scan();

            if (packages.Count == 0)
            {
                LastErrorMessage = "Characters 目录下没有发现任何角色包。";
                message = LastErrorMessage;
                return false;
            }

            foreach (CharacterPackageInfo packageInfo in packages)
            {
                CharacterValidationResult validationResult = _validator.Validate(packageInfo);
                LastValidationResult = validationResult;

                if (!validationResult.Valid)
                {
                    Debug.LogWarning(
                        "[CurrentCharacterContext] 跳过无效角色包：\n" +
                        validationResult
                    );
                    continue;
                }

                if (_loader.TryLoad(packageInfo, out CharacterPackageData loadedData, out string errorMessage))
                {
                    SetCurrentCharacter(loadedData);
                    characterData = loadedData;
                    message = $"已加载角色：{loadedData.CharacterId}";
                    return true;
                }

                Debug.LogWarning($"[CurrentCharacterContext] 角色包加载失败：{errorMessage}");
            }

            LastErrorMessage = "没有找到可用角色包。请检查 character.json、persona.txt、Live2D 模型和映射表配置。";
            message = LastErrorMessage;
            return false;
        }

        /// <summary>
        /// 按角色 ID 或角色包文件夹名加载角色。
        ///
        /// 功能：
        /// 1. 扫描所有角色包。
        /// 2. 先用 CharacterPackageValidator 获取角色 ID。
        /// 3. 如果 characterId 或角色包文件夹名匹配，则尝试加载并设置为当前角色。
        ///
        /// 参数：
        /// characterIdOrFolderName：角色 ID 或角色包文件夹名。
        /// characterData：输出成功加载的角色包数据。
        /// message：输出加载结果说明或错误原因。
        ///
        /// 返回：
        /// true 表示加载成功；false 表示没有找到或加载失败。
        /// </summary>
        public bool TryLoadCharacterById(
            string characterIdOrFolderName,
            out CharacterPackageData characterData,
            out string message)
        {
            characterData = null;
            message = string.Empty;

            if (string.IsNullOrWhiteSpace(characterIdOrFolderName))
            {
                LastErrorMessage = "characterIdOrFolderName 为空，无法加载角色。";
                message = LastErrorMessage;
                return false;
            }

            List<CharacterPackageInfo> packages = _scanner.Scan();

            foreach (CharacterPackageInfo packageInfo in packages)
            {
                CharacterValidationResult validationResult = _validator.Validate(packageInfo);
                LastValidationResult = validationResult;

                bool matchCharacterId = string.Equals(
                    validationResult.CharacterId,
                    characterIdOrFolderName,
                    StringComparison.OrdinalIgnoreCase
                );

                bool matchFolderName = string.Equals(
                    packageInfo.PackageFolderName,
                    characterIdOrFolderName,
                    StringComparison.OrdinalIgnoreCase
                );

                if (!matchCharacterId && !matchFolderName)
                {
                    continue;
                }

                if (!validationResult.Valid)
                {
                    LastErrorMessage = $"目标角色包存在但校验未通过：{characterIdOrFolderName}";
                    message = LastErrorMessage;
                    return false;
                }

                if (_loader.TryLoad(packageInfo, out CharacterPackageData loadedData, out string errorMessage))
                {
                    SetCurrentCharacter(loadedData);
                    characterData = loadedData;
                    message = $"已加载角色：{loadedData.CharacterId}";
                    return true;
                }

                LastErrorMessage = errorMessage;
                message = LastErrorMessage;
                return false;
            }

            LastErrorMessage = $"没有找到角色：{characterIdOrFolderName}";
            message = LastErrorMessage;
            return false;
        }

        /// <summary>
        /// 直接设置当前角色。
        ///
        /// 参数：
        /// characterData：已经加载完成的角色包数据。
        ///
        /// 返回：
        /// 无。
        /// </summary>
        public void SetCurrentCharacter(CharacterPackageData characterData)
        {
            if (characterData == null)
            {
                throw new ArgumentNullException(nameof(characterData));
            }

            CurrentCharacter = characterData;
            LastErrorMessage = string.Empty;

            Debug.Log(
                "[CurrentCharacterContext] 当前角色已切换：" +
                $"CharacterId={CurrentCharacter.CharacterId}, " +
                $"CharacterName={CurrentCharacter.CharacterName}"
            );

            CurrentCharacterChanged?.Invoke(CurrentCharacter);
        }

        /// <summary>
        /// 清空当前角色。
        ///
        /// 功能：
        /// 通常用于开发者调试、角色包热重载或退出当前角色。
        ///
        /// 参数：
        /// 无。
        ///
        /// 返回：
        /// 无。
        /// </summary>
        public void Clear()
        {
            CurrentCharacter = null;
            LastErrorMessage = string.Empty;
            LastValidationResult = null;

            Debug.Log("[CurrentCharacterContext] 当前角色已清空。");

            CurrentCharacterChanged?.Invoke(null);
        }
    }
}