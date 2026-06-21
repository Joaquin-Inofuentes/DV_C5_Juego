using System;
using System.IO;
using UnityEngine;

namespace DebugSystem
{
    public static class LocalNetworkMock
    {
        private static string FilePath => Path.Combine(Application.temporaryCachePath, "room_mock.json");
        
        public static bool IsHost { get; private set; }
        public static int LocalActorID { get; private set; }
        public static string LocalPlayerName { get; private set; }
        
        public static bool RoomExists()
        {
            return File.Exists(FilePath);
        }
        
        public static void CreateRoom(string playerName)
        {
            IsHost = true;
            LocalActorID = 1;
            LocalPlayerName = playerName;
            
            RoomData data = new RoomData
            {
                HostName = playerName,
                Player1Ready = true,
                Player2Ready = false,
                GameStarted = false
            };
            
            File.WriteAllText(FilePath, JsonUtility.ToJson(data));
            Debug.Log($"[NETWORK MOCK] Sala creada en {FilePath}");
        }
        
        public static void JoinRoom(string playerName)
        {
            if (!RoomExists()) return;
            
            IsHost = false;
            LocalActorID = 2;
            LocalPlayerName = playerName;
            
            string json = File.ReadAllText(FilePath);
            RoomData data = JsonUtility.FromJson<RoomData>(json);
            
            data.Player2Name = playerName;
            data.Player2Ready = true;
            data.GameStarted = true; // Auto-start the game when Player 2 joins
            
            File.WriteAllText(FilePath, JsonUtility.ToJson(data));
            Debug.Log($"[NETWORK MOCK] Te uniste a la sala alojada por {data.HostName}. ¡Iniciando juego automáticamente!");
        }
        
        public static RoomData GetRoomData()
        {
            if (!RoomExists()) return null;
            try
            {
                // To avoid sharing violations if the other process is writing
                using (FileStream stream = File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    return JsonUtility.FromJson<RoomData>(json);
                }
            }
            catch
            {
                return null;
            }
        }
        
        public static void StartGame()
        {
            if (!IsHost) return;
            
            RoomData data = GetRoomData();
            if (data != null)
            {
                data.GameStarted = true;
                File.WriteAllText(FilePath, JsonUtility.ToJson(data));
            }
        }

        public static void ClearRoom()
        {
            if (File.Exists(FilePath))
            {
                try
                {
                    File.Delete(FilePath);
                }
                catch {}
            }
        }

        [Serializable]
        public class RoomData
        {
            public string HostName;
            public string Player2Name;
            public bool Player1Ready;
            public bool Player2Ready;
            public bool GameStarted;
        }
    }
}
