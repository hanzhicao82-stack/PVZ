using UnityEngine;

namespace PVZ.DOTS.Utils
{
    /// <summary>
    /// 游戏日志工具类 - 只在Editor模式下输出日志
    /// </summary>
    public static class GameLogger
    {
        /// <summary>
        /// 普通日志
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(object message)
        {
            UnityEngine.Debug.Log(message);
        }

        /// <summary>
        /// 带标签的日志
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(string tag, object message)
        {
            UnityEngine.Debug.Log($"[{tag}] {message}");
        }

        /// <summary>
        /// 警告日志
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogWarning(object message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        /// <summary>
        /// 带标签的警告日志
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogWarning(string tag, object message)
        {
            UnityEngine.Debug.LogWarning($"[{tag}] {message}");
        }

        /// <summary>
        /// 错误日志
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogError(object message)
        {
            UnityEngine.Debug.LogError(message);
        }

        /// <summary>
        /// 带标签的错误日志
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogError(string tag, object message)
        {
            UnityEngine.Debug.LogError($"[{tag}] {message}");
        }
    }
}
