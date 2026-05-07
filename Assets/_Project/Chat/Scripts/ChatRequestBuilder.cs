using System;
using Project.Character;

namespace Project.Chat
{
    /// <summary>
    /// 聊天请求构造器。
    ///
    /// 功能：
    /// 1. 把用户输入和当前角色包数据组合成 ChatRequestDto。
    /// 2. 隔离 ChatController 对 CharacterPackageData 内部字段的直接依赖。
    /// 3. 为后续接入真实后端时的请求体构造提供统一入口。
    ///
    /// 使用方式：
    /// ChatRequestDto request = ChatRequestBuilder.Build("你好", CurrentCharacterContext.Instance.CurrentCharacter);
    ///
    /// 对外暴露方法：
    /// - Build：构造一次聊天请求。
    /// </summary>
    public static class ChatRequestBuilder
    {
        /// <summary>
        /// 构造一次聊天请求。
        /// </summary>
        /// <param name="userInput">用户输入文本。</param>
        /// <param name="characterData">当前角色包数据。</param>
        /// <returns>返回可发送给 Mock 服务或真实后端的请求对象。</returns>
        /// <exception cref="ArgumentNullException">characterData 为空时抛出。</exception>
        public static ChatRequestDto Build(string userInput, CharacterPackageData characterData)
        {
            if (characterData == null)
            {
                throw new ArgumentNullException(nameof(characterData));
            }

            return new ChatRequestDto
            {
                UserInput = string.IsNullOrWhiteSpace(userInput) ? string.Empty : userInput.Trim(),
                CharacterId = characterData.CharacterId,
                CharacterName = characterData.CharacterName,
                ConfigVersion = characterData.Definition != null ? characterData.Definition.ConfigVersion : string.Empty,
                PersonaText = characterData.PersonaText ?? string.Empty,
                PromptTemplateText = characterData.PromptTemplateText ?? string.Empty
            };
        }
    }
}
