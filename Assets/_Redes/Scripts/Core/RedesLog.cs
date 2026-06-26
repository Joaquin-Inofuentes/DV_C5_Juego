using UnityEngine;

namespace Redes.Core
{
    /// <summary>
    /// Centralized, flagged debug logger for the whole "Redes" project.
    ///
    /// WHY THIS EXISTS (SOLID - Single Responsibility):
    /// All gameplay/network code logs THROUGH this class so the console shows
    /// consistent, colored flags. The assignment requires seeing very clear
    /// debug logs, so every system uses one of the channels below.
    ///
    /// The actual log STRINGS required by the assignment live where the event
    /// happens (network, lobby, match, player). This class only formats them.
    /// </summary>
    public static class RedesLog
    {
        // Flags (channels). Each system uses its own so logs are easy to filter.
        public const string BOOT   = "[REDES][BOOT]";
        public const string NET    = "[REDES][NET]";
        public const string LOBBY  = "[REDES][LOBBY]";
        public const string MATCH  = "[REDES][MATCH]";
        public const string PLAYER = "[REDES][PLAYER]";
        public const string COMBAT = "[REDES][COMBAT]";
        public const string AMMO   = "[REDES][AMMO]";
        public const string VFX    = "[REDES][VFX]";

        public static void Info(string flag, string message)
        {
            if (IsSoundOrCursorLog(message))
            {
                Debug.Log($"{flag} {message}");
            }
        }

        public static void Warn(string flag, string message)
        {
            if (IsSoundOrCursorLog(message))
            {
                Debug.LogWarning($"{flag} {message}");
            }
        }

        public static void Error(string flag, string message)
        {
            // Siempre mostrar errores del sistema para desarrollo
            Debug.LogError($"{flag} {message}");
        }

        private static bool IsSoundOrCursorLog(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return false;
            string lower = msg.ToLower();
            return lower.Contains("sound") || 
                   lower.Contains("sfx") || 
                   lower.Contains("audio") || 
                   lower.Contains("cursor") || 
                   lower.Contains("teleport");
        }

        public static void Trace(string flag, string className, string methodName, string playerContext, string content, string type = "Info")
        {
            string contextStr = string.IsNullOrEmpty(playerContext) ? "System" : $"Player:{playerContext}";
            string formatted = $"[{className}::{methodName}] ({contextStr}) -> {content}";

            if (type == "Warn")
                Warn(flag, formatted);
            else if (type == "Error")
                Error(flag, formatted);
            else
                Info(flag, formatted);
        }
    }
}
