using Game.Core.Events;

namespace Game.Gameplay.Waves
{
    public readonly struct WaveStartedEvent : IGameEvent
    {
        public readonly int WaveNumber;
        public readonly int Budget;
        public readonly int PlannedEnemyCount;

        public WaveStartedEvent(int waveNumber, int budget, int plannedEnemyCount)
        {
            WaveNumber = waveNumber;
            Budget = budget;
            PlannedEnemyCount = plannedEnemyCount;
        }
    }

    public readonly struct WaveClearedEvent : IGameEvent
    {
        public readonly int WaveNumber;

        public WaveClearedEvent(int waveNumber) => WaveNumber = waveNumber;
    }

    public readonly struct WavesCompletedEvent : IGameEvent
    {
        public readonly int TotalWaves;

        public WavesCompletedEvent(int totalWaves) => TotalWaves = totalWaves;
    }
}
