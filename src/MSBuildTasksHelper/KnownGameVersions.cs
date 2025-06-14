using System;
using System.Collections.Generic;

namespace SanchozzONIMods
{
    public class KnownGameVersions
    {
        public List<GameVersionInfo> KnownVersions { get; set; }
        public string PreserveVersion { get; set; } = GetKleiAssemblyInfo.INVALID;
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
