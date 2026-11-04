using System;
using System.Collections.Generic;
using Game.Core;
using Game.Gameplay.Enemies;

namespace Game.Gameplay.Waves
{
    /// <summary>Builds a deterministic enemy queue that spends a wave's spawn-point budget.</summary>
    public static class WaveComposition
    {
        public static IReadOnlyList<EnemyDefinition> Build(
            IReadOnlyList<EnemyDefinition> definitions,
            int budget,
            GameRandom random)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (budget < 0) throw new ArgumentOutOfRangeException(nameof(budget));

            var result = new List<EnemyDefinition>();
            int remaining = budget;
            while (remaining > 0)
            {
                int fittingCount = 0;
                for (int i = 0; i < definitions.Count; i++)
                {
                    EnemyDefinition definition = definitions[i];
                    if (definition != null && definition.Prefab != null && definition.SpawnCost > 0 && definition.SpawnCost <= remaining)
                        fittingCount++;
                }

                if (fittingCount == 0) break;

                int choice = random.NextInt(0, fittingCount);
                EnemyDefinition selected = null;
                for (int i = 0; i < definitions.Count; i++)
                {
                    EnemyDefinition definition = definitions[i];
                    if (definition == null || definition.Prefab == null || definition.SpawnCost <= 0 || definition.SpawnCost > remaining) continue;
                    if (choice-- == 0)
                    {
                        selected = definition;
                        break;
                    }
                }

                result.Add(selected);
                remaining -= selected.SpawnCost;
            }

            return result;
        }
    }
}
