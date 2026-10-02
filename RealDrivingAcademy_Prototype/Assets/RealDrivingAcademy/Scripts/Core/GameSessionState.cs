using System;

namespace RealDrivingAcademy.Core
{
    public enum PlayerSessionMode
    {
        None,
        Guest,
        Registered
    }

    [Serializable]
    public sealed class GameSessionState
    {
        public PlayerSessionMode sessionMode = PlayerSessionMode.None;
        public string profileId = string.Empty;
        public string displayName = string.Empty;

        public bool HasSession => sessionMode != PlayerSessionMode.None;

        public void BeginGuest()
        {
            sessionMode = PlayerSessionMode.Guest;
            profileId = "guest";
            displayName = "Guest";
        }

        public void BeginRegistered(string id, string name)
        {
            sessionMode = PlayerSessionMode.Registered;
            profileId = id ?? string.Empty;
            displayName = name ?? string.Empty;
        }

        public void Clear()
        {
            sessionMode = PlayerSessionMode.None;
            profileId = string.Empty;
            displayName = string.Empty;
        }
    }
}
