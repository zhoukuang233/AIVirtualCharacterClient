using System;
using System.Collections.Generic;
using Project.Infrastructure;
using UnityEngine;

namespace Project.Character
{
    /// <summary>
    /// 角色系统门面。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 本类把底层的 <see cref="CharacterPackageScanner"/>、<see cref="CharacterPackageValidator"/>
    /// 和 <see cref="CharacterPackageLoader"/> 组合成一个统一入口。
    /// AppBootstrapper、DeveloperConsole、ChatSystem 等上层模块只应该依赖 CharacterSystem，
    /// 不应该直接依赖 Scanner / Validator / Loader 三个底层类。
    /// </para>
    /// <para>
    /// 使用方式：
    /// <code>
    /// CharacterSystem characterSystem = CharacterSystem.Instance;
    /// List&lt;CharacterPackageInfo&gt; packages = characterSystem.ScanCharacters();
    /// bool ok = characterSystem.TryLoadFirstValidCharacter(out CharacterPackageData data, out CharacterValidationResult validation, out string message);
    /// </code>
    /// </para>
    /// <para>
    /// 对外暴露：
    /// - CurrentCharacter：当前角色包数据。
    /// - LastValidationResult：最近一次校验结果。
    /// - LastErrorMessage：最近一次错误信息。
    /// - CurrentCharacterChanged：当前角色变化事件。
    /// - ScanCharacters：扫描角色包。
    /// - Validate：校验角色包。
    /// - TryLoadCharacter：加载指定角色包或指定角色 ID。
    /// - TryLoadFirstValidCharacter：加载第一个有效角色包。
    /// - ClearCurrentCharacter：清空当前角色。
    /// </para>
    /// <para>
    /// TODO: 后续接入 SettingsSystem 后，增加 TryLoadActiveCharacter，根据 activeCharacterId 加载默认角色。
    /// TODO: 后续接入 EventBus 后，在角色切换成功时发布 CharacterChangedEvent。
    /// TODO: 后续可以增加角色包缓存，避免 DeveloperConsole 频繁切换角色时重复读取磁盘。
    /// </para>
    /// </remarks>
    public class CharacterSystem : SingletonMonoBehaviour<CharacterSystem>
    {
        private readonly CharacterPackageScanner _scanner = new CharacterPackageScanner();
        private readonly CharacterPackageValidator _validator = new CharacterPackageValidator();
        private readonly CharacterPackageLoader _loader = new CharacterPackageLoader();

        /// <summary>
        /// 当前已加载的角色包数据。
        /// </summary>
        public CharacterPackageData CurrentCharacter
        {
            get { return CurrentCharacterContext.Instance.CurrentCharacter; }
        }

        /// <summary>
        /// 最近一次角色包校验结果。
        /// </summary>
        public CharacterValidationResult LastValidationResult { get; private set; }

        /// <summary>
        /// 最近一次扫描结果。
        /// </summary>
        public IReadOnlyList<CharacterPackageInfo> LastScannedPackages
        {
            get { return _lastScannedPackages; }
        }

        /// <summary>
        /// 最近一次角色系统错误信息。
        /// </summary>
        public string LastErrorMessage { get; private set; }

        /// <summary>
        /// 当前是否已经加载角色。
        /// </summary>
        public bool HasCurrentCharacter
        {
            get { return CurrentCharacterContext.Instance.HasCurrentCharacter; }
        }

        /// <summary>
        /// 当前角色变化事件。
        /// </summary>
        public event Action<CharacterPackageData> CurrentCharacterChanged;

        private List<CharacterPackageInfo> _lastScannedPackages = new List<CharacterPackageInfo>();

        /// <summary>
        /// CharacterSystem 是全局运行时服务，应该跨场景保留。
        /// </summary>
        protected override bool IsPersistent
        {
            get { return true; }
        }

        /// <summary>
        /// 单例初始化入口。
        /// </summary>
        protected override void OnSingletonAwake()
        {
            CurrentCharacterContext.Instance.CurrentCharacterChanged += HandleCurrentCharacterChanged;
        }

        /// <summary>
        /// 扫描当前 UserData/Characters 目录下的所有角色包。
        /// </summary>
        /// <returns>
        /// 返回角色包基础信息列表。没有角色包时返回空列表，不返回 null。
        /// </returns>
        public List<CharacterPackageInfo> ScanCharacters()
        {
            _lastScannedPackages = _scanner.Scan();
            Debug.Log($"[CharacterSystem] 扫描到角色包数量：{_lastScannedPackages.Count}");
            return new List<CharacterPackageInfo>(_lastScannedPackages);
        }

        /// <summary>
        /// 校验指定角色包。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <returns>返回角色包校验结果。</returns>
        public CharacterValidationResult Validate(CharacterPackageInfo packageInfo)
        {
            CharacterValidationResult validationResult = _validator.Validate(packageInfo);
            LastValidationResult = validationResult;

            if (validationResult.Valid)
            {
                Debug.Log($"[CharacterSystem] 校验通过，warnings={validationResult.Warnings.Count}");
            }
            else
            {
                Debug.LogWarning(
                    "[CharacterSystem] 校验未通过，" +
                    $"errors={validationResult.Errors.Count}, warnings={validationResult.Warnings.Count}\n" +
                    validationResult
                );
            }

            return validationResult;
        }

        /// <summary>
        /// 尝试加载指定角色包。
        /// </summary>
        /// <param name="packageInfo">角色包基础路径信息。</param>
        /// <param name="characterData">加载成功时输出角色包数据；失败时为 null。</param>
        /// <param name="validationResult">输出本次校验结果。</param>
        /// <param name="message">输出加载结果说明或失败原因。</param>
        /// <returns>加载并设置当前角色成功返回 true；否则返回 false。</returns>
        public bool TryLoadCharacter(
            CharacterPackageInfo packageInfo,
            out CharacterPackageData characterData,
            out CharacterValidationResult validationResult,
            out string message)
        {
            characterData = null;
            validationResult = null;
            message = string.Empty;

            if (packageInfo == null)
            {
                LastErrorMessage = "packageInfo 为空，无法加载角色。";
                message = LastErrorMessage;
                return false;
            }

            validationResult = Validate(packageInfo);
            if (!validationResult.Valid)
            {
                LastErrorMessage = $"角色包校验未通过：{packageInfo.PackageFolderName}";
                message = LastErrorMessage;
                return false;
            }

            if (!_loader.TryLoad(packageInfo, out CharacterPackageData loadedData, out string errorMessage))
            {
                LastErrorMessage = errorMessage;
                message = LastErrorMessage;
                Debug.LogWarning($"[CharacterSystem] 角色包加载失败：{errorMessage}");
                return false;
            }

            SetCurrentCharacter(loadedData, validationResult);
            characterData = loadedData;
            message = $"已加载角色：{loadedData.CharacterId}";
            return true;
        }

        /// <summary>
        /// 按角色 ID 或角色包文件夹名加载角色。
        /// </summary>
        /// <param name="characterIdOrFolderName">character.json 中的 characterId，或角色包文件夹名。</param>
        /// <param name="characterData">加载成功时输出角色包数据；失败时为 null。</param>
        /// <param name="validationResult">输出匹配角色包的校验结果；没有匹配时为 null。</param>
        /// <param name="message">输出加载结果说明或失败原因。</param>
        /// <returns>加载并设置当前角色成功返回 true；否则返回 false。</returns>
        public bool TryLoadCharacter(
            string characterIdOrFolderName,
            out CharacterPackageData characterData,
            out CharacterValidationResult validationResult,
            out string message)
        {
            characterData = null;
            validationResult = null;
            message = string.Empty;

            if (string.IsNullOrWhiteSpace(characterIdOrFolderName))
            {
                LastErrorMessage = "characterIdOrFolderName 为空，无法加载角色。";
                message = LastErrorMessage;
                return false;
            }

            List<CharacterPackageInfo> packages = ScanCharacters();
            foreach (CharacterPackageInfo packageInfo in packages)
            {
                CharacterValidationResult currentValidation = Validate(packageInfo);
                bool matchCharacterId = string.Equals(
                    currentValidation.CharacterId,
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

                if (!currentValidation.Valid)
                {
                    validationResult = currentValidation;
                    LastErrorMessage = $"目标角色包存在但校验未通过：{characterIdOrFolderName}";
                    message = LastErrorMessage;
                    return false;
                }

                return TryLoadCharacter(packageInfo, out characterData, out validationResult, out message);
            }

            LastErrorMessage = $"没有找到角色：{characterIdOrFolderName}";
            message = LastErrorMessage;
            return false;
        }

        /// <summary>
        /// 扫描并加载第一个校验通过的角色包。
        /// </summary>
        /// <param name="characterData">加载成功时输出角色包数据；失败时为 null。</param>
        /// <param name="validationResult">输出成功角色包的校验结果；失败时为最近一次校验结果或 null。</param>
        /// <param name="message">输出加载结果说明或失败原因。</param>
        /// <returns>加载成功返回 true；没有可用角色包时返回 false。</returns>
        public bool TryLoadFirstValidCharacter(
            out CharacterPackageData characterData,
            out CharacterValidationResult validationResult,
            out string message)
        {
            characterData = null;
            validationResult = null;
            message = string.Empty;

            List<CharacterPackageInfo> packages = ScanCharacters();
            if (packages.Count == 0)
            {
                LastErrorMessage = "Characters 目录下没有发现任何角色包。";
                message = LastErrorMessage;
                return false;
            }

            foreach (CharacterPackageInfo packageInfo in packages)
            {
                CharacterValidationResult currentValidation = Validate(packageInfo);
                validationResult = currentValidation;

                if (!currentValidation.Valid)
                {
                    continue;
                }

                if (_loader.TryLoad(packageInfo, out CharacterPackageData loadedData, out string errorMessage))
                {
                    SetCurrentCharacter(loadedData, currentValidation);
                    characterData = loadedData;
                    message = $"已加载角色：{loadedData.CharacterId}";
                    return true;
                }

                Debug.LogWarning($"[CharacterSystem] 角色包加载失败：{errorMessage}");
            }

            LastErrorMessage = "没有找到可用角色包。请检查 character.json、persona.txt、Live2D 模型和映射表配置。";
            message = LastErrorMessage;
            return false;
        }

        /// <summary>
        /// 清空当前角色。
        /// </summary>
        public void ClearCurrentCharacter()
        {
            LastValidationResult = null;
            LastErrorMessage = string.Empty;
            CurrentCharacterContext.Instance.Clear();
        }

        /// <summary>
        /// 设置当前角色，并同步 CharacterSystem 与 CurrentCharacterContext 的状态。
        /// </summary>
        /// <param name="characterData">已经加载成功的角色包数据。</param>
        /// <param name="validationResult">该角色包对应的校验结果。</param>
        private void SetCurrentCharacter(CharacterPackageData characterData, CharacterValidationResult validationResult)
        {
            CurrentCharacterContext.Instance.SetCurrentCharacter(characterData);
            LastValidationResult = validationResult;
            LastErrorMessage = string.Empty;

            Debug.Log($"[CharacterSystem] 当前角色：{characterData.CharacterId}");
        }

        /// <summary>
        /// 转发 CurrentCharacterContext 的角色变化事件。
        /// </summary>
        /// <param name="characterData">新的当前角色数据。</param>
        private void HandleCurrentCharacterChanged(CharacterPackageData characterData)
        {
            CurrentCharacterChanged?.Invoke(characterData);
        }

        /// <summary>
        /// 释放事件订阅，避免热重载或销毁时残留回调。
        /// </summary>
        protected override void OnDestroy()
        {
            if (CurrentCharacterContext.HasInstance)
            {
                CurrentCharacterContext.Instance.CurrentCharacterChanged -= HandleCurrentCharacterChanged;
            }

            base.OnDestroy();
        }
    }
}
