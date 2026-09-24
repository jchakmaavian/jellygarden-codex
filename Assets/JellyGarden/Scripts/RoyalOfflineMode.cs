namespace RoyalThrone
{
    /// <summary>Current Royal Throne builds use local progress and have no verified online services.</summary>
    public static class RoyalOfflineMode
    {
        // Deliberately not a player preference: no inherited integration may opt itself in.
        public static bool ServicesEnabled => false;
        public const string KingName = "King Vanguard";
    }
}
