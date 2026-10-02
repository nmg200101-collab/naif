using System;

namespace RealDrivingAcademy.Persistence
{
    [Serializable]
    public sealed class RdaSaveData
    {
        public int schemaVersion = 1;
        public RdaUserSettings settings = new RdaUserSettings();
        public RdaProgressData progress = new RdaProgressData();

        public void Sanitize()
        {
            if (settings == null)
                settings = new RdaUserSettings();
            if (progress == null)
                progress = new RdaProgressData();

            settings.Sanitize();
            if (schemaVersion < 1)
                schemaVersion = 1;
        }
    }
}
