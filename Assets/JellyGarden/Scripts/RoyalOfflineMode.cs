namespace RoyalThrone
{
    /// <summary>
    /// Whether the kit's online services (Facebook, in-app purchases) may start. LevelManager asks. King Leo has one
    /// switch for this, StoryFreeGame: free (the default) keeps them off, and switching it off hands them back to the
    /// kit, as main did.
    /// </summary>
    public static class RoyalOfflineMode
    {
        public static bool ServicesEnabled => !JellyGarden.Scripts.Story.StoryFreeGame.Enabled;
    }
}
