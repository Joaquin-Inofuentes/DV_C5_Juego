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
        public const string BOOT   = "<color=#9E9E9E>[REDES][BOOT]</color>";
        public const string NET    = "<color=#2196F3>[REDES][NET]</color>";
        public const string LOBBY  = "<color=#00BCD4>[REDES][LOBBY]</color>";
        public const string MATCH  = "<color=#FF9800>[REDES][MATCH]</color>";
        public const string PLAYER = "<color=#4CAF50>[REDES][PLAYER]</color>";
        public const string COMBAT = "<color=#F44336>[REDES][COMBAT]</color>";
        public const string AMMO   = "<color=#9C27B0>[REDES][AMMO]</color>";

        public static void Info(string flag, string message)
        {
            Debug.Log($"{flag} {message}");
        }

        public static void Warn(string flag, string message)
        {
            Debug.LogWarning($"{flag} {message}");
        }

        public static void Error(string flag, string message)
        {
            Debug.LogError($"{flag} {message}");
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
