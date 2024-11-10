using System;

namespace SanchozzONIMods
{
    public class KnownGameVersions
    {
        public GameVersionInfo[] KnownVersions { get; set; }
    }

    public struct GameVersionInfo : IComparable<GameVersionInfo>
    {
        public string GameVersion { get; set; }
        public int MinimumBuildNumber { get; set; }

        public int CompareTo(GameVersionInfo other)
        {
            return MinimumBuildNumber.CompareTo(other.MinimumBuildNumber);
        }
    }
}
