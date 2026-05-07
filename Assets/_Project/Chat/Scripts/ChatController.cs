using System;
using System.Collections.Generic;
using Project.Character;
using Project.ExperimentLogging;
using Project.Presentation;
using Project.Service;
using UnityEngine;

namespace Project.Chat
{
    /// <summary>
    /// 聊天流程控制器。
    ///
    /// 功能：
    /// 1. 接收用户输入。
    /// 2. 构造 ChatRequestDto。
    /// 3. 调用 MockChatBackendClient 生成结构化响应。
    /// 4. 解析 replyText、emotion、action、voiceStyle。
    /// 5. 调用 PresentationSystem 生成表现解析结果。
    /// 6. 把文本、表情、动作、语音命令交给 PresentationCommandQueueService 执行 Debug 输出。
    /// 7. 写入 JSONL 前端交互日志。
    ///
    /// 使用方式：
    /// 1. 在 Main 或 TestLab 场景中新建 GameObject，命名为 ChatController。
    /// 2. 把本脚本挂载上去。
    /// 3. 开发阶段可勾选 AutoRunDebugOnStart，运行后自动模拟一次输入。
    /// 4. 后续接入 UI 时，让按钮调用 SubmitUserInput(inputField.text)。
    ///
    /// 对外暴露：
    /// - AutoRunDebugOnStart：是否 Start 时自动跑一次测试。
    /// - DebugUserInput：自动测试用的输入文本。
    /// - SubmitUserInput：提交用户输入。
    ///
    /// TODO：
    /// - 后续接入真实 BackendApiClient，替换 MockChatBackendClient。
    /// - 后续接入 UI，把 ReplyText 显示到聊天窗口。
    /// - 后续接入 GameStateMachine，避免请求中重复提交。
    /// </summary>
    public class ChatController : MonoBehaviour
    {
        /// <summary>
        /// 是否在 Start 阶段自动执行一次调试输入。
        /// </summary>
        [Header("Debug")]
        public bool AutoRunDebugOnStart = true;

        /// <summary>
        /// 自动测试用的用户输入。
        /// </summary>
        [TextArea(2, 4)]
        public string DebugUserInput = "你好，藿藿。";

        private readonly MockChatBackendClient _mockClient = new MockChatBackendClient();

        /// <summary>
        /// Unity Start 生命周期入口。
        /// </summary>
        private void Start()
        {
            if (!AutoRunDebugOnStart)
            {
                return;
            }

            SubmitUserInput(DebugUserInput);
        }

        /// <summary>
        /// 提交一次用户输入并执行完整离线交互流程。
        /// </summary>
        /// <param name="userInput">用户输入文本。</param>
        public void SubmitUserInput(string userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput))
            {
                Debug.LogWarning("[ChatController] 用户输入为空，已忽略。 ");
                return;
            }

            CharacterPackageData currentCharacter = CharacterSystem.Instance.CurrentCharacter;
            if (currentCharacter == null)
            {
                Debug.LogWarning("[ChatController] 当前没有已加载角色，无法开始聊天。请确认 AppBootstrapper 已成功加载角色包。 ");
                return;
            }

            int turnId = JsonlExperimentLogger.GetNextTurnId();
            string rawResponseJson = string.Empty;
            ChatResponseDto response = null;
            PresentationResolveResult resolveResult = null;
            string errorMessage = string.Empty;
            bool parseSuccess = false;

            try
            {
                ChatRequestDto request = ChatRequestBuilder.Build(userInput, currentCharacter);

                rawResponseJson = _mockClient.GenerateResponseJson(request);
                Debug.Log("[ChatController] Mock 后端响应：" + rawResponseJson);

                parseSuccess = ChatResponseParser.TryParse(rawResponseJson, out response, out errorMessage);
                if (!parseSuccess)
                {
                    Debug.LogError($"[ChatController] 响应解析失败：{errorMessage}");
                    return;
                }

                Debug.Log("[ChatController] 角色回复：" + response.ReplyText);

                EnsurePresentationSystemReady(currentCharacter);

                resolveResult = PresentationSystem.Instance.Resolve(
                    response.Emotion,
                    response.Action,
                    response.VoiceStyle
                );

                var commands = new List<PresentationCommand>
                {
                    new ShowTextCommand(response.ReplyText)
                };

                if (resolveResult != null)
                {
                    commands.AddRange(resolveResult.ToCommands());
                }

                PresentationCommandQueueService.Instance.EnqueueRange(commands);
                PresentationCommandQueueService.Instance.ExecuteAllDebug();
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                Debug.LogError($"[ChatController] 聊天流程异常：{exception}");
            }
            finally
            {
                WriteInteractionLog(
                    turnId,
                    currentCharacter,
                    userInput,
                    rawResponseJson,
                    response,
                    resolveResult,
                    parseSuccess,
                    errorMessage
                );
            }
        }

        /// <summary>
        /// 确保表现系统已绑定当前角色。
        /// </summary>
        /// <param name="currentCharacter">当前聊天使用的角色包数据。</param>
        /// <remarks>
        /// 正常启动流程中 AppBootstrapper 会先初始化 PresentationSystem。这里保留兜底逻辑，
        /// 方便单独运行 ChatController 测试场景时仍能完成表现映射。
        /// </remarks>
        private static void EnsurePresentationSystemReady(CharacterPackageData currentCharacter)
        {
            if (PresentationSystem.Instance.Initialized && PresentationSystem.Instance.CurrentCharacter == currentCharacter)
            {
                return;
            }

            PresentationSystem.Instance.Initialize(currentCharacter);
        }

        /// <summary>
        /// 写入一次前端交互日志。
        /// </summary>
        /// <param name="turnId">当前轮次 ID。</param>
        /// <param name="characterData">当前角色包数据。</param>
        /// <param name="userInput">用户输入。</param>
        /// <param name="rawResponseJson">原始响应 JSON。</param>
        /// <param name="response">解析后的响应对象。</param>
        /// <param name="resolveResult">表现映射解析结果。</param>
        /// <param name="parseSuccess">响应解析是否成功。</param>
        /// <param name="errorMessage">错误信息。</param>
        private static void WriteInteractionLog(
            int turnId,
            CharacterPackageData characterData,
            string userInput,
            string rawResponseJson,
            ChatResponseDto response,
            PresentationResolveResult resolveResult,
            bool parseSuccess,
            string errorMessage)
        {
            string configVersion = characterData.Definition != null
                ? characterData.Definition.ConfigVersion
                : string.Empty;

            var entry = new FrontendInteractionLogEntry
            {
                SessionId = JsonlExperimentLogger.SessionId,
                TurnId = turnId,
                Timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
                CharacterId = characterData.CharacterId,
                CharacterName = characterData.CharacterName,
                ConfigVersion = configVersion,
                UserInput = userInput,
                ReplyText = response != null ? response.ReplyText : string.Empty,
                Emotion = response != null ? response.Emotion : string.Empty,
                Action = response != null ? response.Action : string.Empty,
                VoiceStyle = response != null ? response.VoiceStyle : string.Empty,
                SelectedExpression = resolveResult != null ? resolveResult.ExpressionFileName : string.Empty,
                SelectedMotion = resolveResult != null ? resolveResult.MotionFileName : string.Empty,
                ResolvedEmotion = resolveResult != null ? resolveResult.ResolvedEmotion : string.Empty,
                ResolvedAction = resolveResult != null ? resolveResult.ResolvedAction : string.Empty,
                ExpressionFallback = resolveResult != null && resolveResult.ExpressionFallbackUsed,
                MotionFallback = resolveResult != null && resolveResult.MotionFallbackUsed,
                ExpressionFallbackReason = resolveResult != null ? resolveResult.ExpressionFallbackReason : string.Empty,
                MotionFallbackReason = resolveResult != null ? resolveResult.MotionFallbackReason : string.Empty,
                ExpressionFileExists = resolveResult != null && resolveResult.ExpressionFileExists,
                MotionFileExists = resolveResult != null && resolveResult.MotionFileExists,
                RawResponseJson = rawResponseJson,
                ParseSuccess = parseSuccess,
                ErrorMessage = errorMessage
            };

            JsonlExperimentLogger.Append(entry);
        }
    }
}
