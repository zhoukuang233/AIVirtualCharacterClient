using System;
using System.IO;
using Newtonsoft.Json;
using Project.Infrastructure;
using UnityEngine;

namespace Project.ExperimentLogging
{
    /// <summary>
    /// JSONL 实验日志写入器。
    ///
    /// 功能：
    /// 1. 把每一轮前端交互写成一行 JSON。
    /// 2. 默认保存到 persistentDataPath/UserData/Logs/frontend_interaction_yyyyMMdd_HHmmss.jsonl。
    /// 3. 记录 sessionId 和 turnId，方便后续做论文实验统计与回放。
    ///
    /// 使用方式：
    /// FrontendInteractionLogEntry entry = new FrontendInteractionLogEntry();
    /// JsonlExperimentLogger.Append(entry);
    ///
    /// 对外暴露：
    /// - SessionId：当前运行会话 ID。
    /// - GetNextTurnId：获取递增轮次。
    /// - Append：追加一条日志。
    ///
    /// TODO：
    /// - 后续可以增加 CSV 导出器，便于直接导入 Excel/SPSS/R 统计。
    /// - 后续可以增加 ReplayLogReader，根据 JSONL 回放某一轮表现。
    /// </summary>
    public static class JsonlExperimentLogger
    {
        private static readonly string _sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        private static int _turnCounter;

        /// <summary>
        /// 当前运行会话 ID。
        /// </summary>
        public static string SessionId
        {
            get { return _sessionId; }
        }

        /// <summary>
        /// 获取下一轮对话的 turnId。
        /// </summary>
        /// <returns>从 1 开始递增的轮次 ID。</returns>
        public static int GetNextTurnId()
        {
            _turnCounter += 1;
            return _turnCounter;
        }

        /// <summary>
        /// 追加写入一条前端交互日志。
        /// </summary>
        /// <param name="entry">日志条目。</param>
        public static void Append(FrontendInteractionLogEntry entry)
        {
            if (entry == null)
            {
                Debug.LogWarning("[JsonlExperimentLogger] 日志条目为空，已跳过写入。 ");
                return;
            }

            try
            {
                Directory.CreateDirectory(RuntimePathInitializer.LogsPath);

                string fileName = $"frontend_interaction_{SessionId}.jsonl";
                string filePath = Path.Combine(RuntimePathInitializer.LogsPath, fileName);
                string jsonLine = JsonConvert.SerializeObject(entry, Formatting.None);

                File.AppendAllText(filePath, jsonLine + Environment.NewLine);
                Debug.Log($"[JsonlExperimentLogger] 已写入前端交互日志：{filePath}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[JsonlExperimentLogger] 写入日志失败：{exception}");
            }
        }
    }
}
