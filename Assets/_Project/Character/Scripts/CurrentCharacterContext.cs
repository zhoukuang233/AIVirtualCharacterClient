using System;
using Project.Infrastructure;
using UnityEngine;

namespace Project.Character
{
    /// <summary>
    /// 当前角色上下文。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 本类只负责保存“当前正在使用的角色包数据”，并在角色变化时发出事件。
    /// 它不扫描、不校验、不加载角色包；这些流程统一由 <see cref="CharacterSystem"/> 负责。
    /// </para>
    /// <para>
    /// 使用方式：
    /// <code>
    /// CharacterPackageData current = CurrentCharacterContext.Instance.CurrentCharacter;
    /// bool hasCharacter = CurrentCharacterContext.Instance.HasCurrentCharacter;
    /// </code>
    /// </para>
    /// <para>
    /// 对外暴露：
    /// - CurrentCharacter：当前角色包数据。
    /// - HasCurrentCharacter：当前是否已有角色。
    /// - CurrentCharacterChanged：角色切换事件。
    /// - SetCurrentCharacter：设置当前角色。
    /// - Clear：清空当前角色。
    /// </para>
    /// </remarks>
    public class CurrentCharacterContext : SingletonMonoBehaviour<CurrentCharacterContext>
    {
        /// <summary>
        /// 当前已加载的角色包数据。
        /// </summary>
        public CharacterPackageData CurrentCharacter { get; private set; }

        /// <summary>
        /// 当前是否已经存在可用角色。
        /// </summary>
        public bool HasCurrentCharacter
        {
            get { return CurrentCharacter != null; }
        }

        /// <summary>
        /// 当前角色发生变化时触发。
        /// </summary>
        /// <remarks>
        /// 参数为新的当前角色数据；清空角色时参数为 null。
        /// </remarks>
        public event Action<CharacterPackageData> CurrentCharacterChanged;

        /// <summary>
        /// 当前角色上下文属于全局运行状态，应该跨场景保留。
        /// </summary>
        protected override bool IsPersistent
        {
            get { return true; }
        }

        /// <summary>
        /// 直接设置当前角色。
        /// </summary>
        /// <param name="characterData">已经通过校验并加载完成的角色包数据。</param>
        /// <exception cref="ArgumentNullException">characterData 为空时抛出。</exception>
        public void SetCurrentCharacter(CharacterPackageData characterData)
        {
            if (characterData == null)
            {
                throw new ArgumentNullException(nameof(characterData));
            }

            CurrentCharacter = characterData;

            Debug.Log(
                "[CurrentCharacterContext] 当前角色已切换：" +
                $"CharacterId={CurrentCharacter.CharacterId}, " +
                $"CharacterName={CurrentCharacter.CharacterName}"
            );

            CurrentCharacterChanged?.Invoke(CurrentCharacter);
        }

        /// <summary>
        /// 清空当前角色。
        /// </summary>
        /// <remarks>
        /// 通常用于开发者调试、角色包热重载或退出当前角色。
        /// </remarks>
        public void Clear()
        {
            CurrentCharacter = null;

            Debug.Log("[CurrentCharacterContext] 当前角色已清空。");
            CurrentCharacterChanged?.Invoke(null);
        }
    }
}
