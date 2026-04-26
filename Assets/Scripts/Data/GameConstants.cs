namespace ReactorBreach.Data
{
    public static class GameConstants
    {
        // Physics layers
        public const int LayerDefault      = 0;
        public const int LayerPlayer       = 6;
        public const int LayerEnemy        = 7;
        public const int LayerInteractable = 8;
        public const int LayerStatic       = 9;
        public const int LayerBreakable    = 10;
        public const int LayerFoamZone     = 11;
        public const int LayerWeldBridge   = 12;

        // Tags
        public const string TagPlayer        = "Player";
        public const string TagEnemy         = "Enemy";
        public const string TagInteractable  = "Interactable";
        public const string TagSurface       = "Surface";
        public const string TagBreachPoint   = "BreachPoint";
        public const string TagBreakable     = "Breakable";
        public const string TagPressurePlate = "PressurePlate";

        // Vibration radii
        public const float VibrationRadiusWalk   = 8f;
        public const float VibrationRadiusSprint = 15f;
        public const float VibrationRadiusWeld   = 10f;
        public const float VibrationRadiusGravity = 15f;
        public const float VibrationRadiusFoam   = 5f;
        public const float VibrationRadiusImpact = 12f;

        // Vibration step intervals (seconds)
        public const float StepVibrationIntervalWalk   = 0.5f;
        public const float StepVibrationIntervalSprint = 0.3f;

        // Thresholds
        public const float ImpactVibrationThreshold = 5f;

        // Scenes
        public const string SceneMainMenu = "MainMenu";

        // Level_01, зона A: пропасть по Z — совпадать с BuildFloor (Floor_A1..3.5, 9.5..A2)
        // Смерть от провала: только в PlayerController — низ капсулы < плоскости пола, без триггера-«столба».
        public const float Level1_ChasmZMin  = 3.5f;
        public const float Level1_ChasmZMax  = 9.5f;
        public const float Level1_SectionHalfWidthX = 12f;
        /// <summary>Мировой Y плоскости настила станции (верх плитки пола ~0; провал = нижняя граница капсулы &lt; этого).</summary>
        public const float StationFloorPlaneY = 0f;
    }
}
