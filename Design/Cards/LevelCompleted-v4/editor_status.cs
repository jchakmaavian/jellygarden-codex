return new {
    playing = UnityEngine.Application.isPlaying,
    compiling = UnityEditor.EditorApplication.isCompiling,
    updating = UnityEditor.EditorApplication.isUpdating,
    testing = RoyalThrone.Testing.RoyalSave.Testing,
    scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
    game = JellyGarden.Scripts.LevelManager.THIS ? JellyGarden.Scripts.LevelManager.THIS.gameStatus.ToString() : "none",
    haloField = typeof(RoyalThrone.Presentation.RoyalWinCard).GetField("haloSparkles") != null,
    dirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty
};
