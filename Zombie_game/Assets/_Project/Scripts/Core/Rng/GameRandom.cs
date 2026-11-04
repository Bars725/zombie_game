using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// Deterministic seeded RNG for run generation (xorshift64* seeded via SplitMix64).
    /// Same seed => same sequence on every machine and Unity version, unlike UnityEngine.Random
    /// (global state) or System.Random (implementation may differ between runtimes).
    ///
    /// Use Fork("waves"), Fork("loot")... to give each system its own stream:
    /// extra rolls in one system then never shift the results of another.
    /// Not thread-safe; not for cryptography.
    /// </summary>
    public sealed class GameRandom
    {
        private ulong _state;

        public ulong Seed { get; }

        public GameRandom(ulong seed)
        {
            Seed = seed;
            ulong sm = seed;
            _state = SplitMix64(ref sm);
            if (_state == 0) _state = 0x9E3779B97F4A7C15UL; // xorshift must not start at 0
        }

        /// <summary>Creates an independent stream derived from this RNG's seed (not its current state).</summary>
        public GameRandom Fork(string streamName)
        {
            if (streamName == null) throw new ArgumentNullException(nameof(streamName));
            ulong mixed = Seed ^ Fnv1a64(streamName);
            return new GameRandom(SplitMix64(ref mixed));
        }

        /// <summary>Full 64 random bits.</summary>
        public ulong NextULong()
        {
            ulong x = _state;
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            _state = x;
            return x * 0x2545F4914F6CDD1DUL;
        }

        /// <summary>Upper 32 bits of the next value (the stronger half of xorshift64*).</summary>
        public uint NextUInt() => (uint)(NextULong() >> 32);

        /// <summary>Integer in [minInclusive, maxExclusive). Modulo bias is below 2^-32 and ignored.</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive),
                    $"maxExclusive ({maxExclusive}) must be greater than minInclusive ({minInclusive}).");
            }
            ulong range = (ulong)((long)maxExclusive - minInclusive);
            return (int)(minInclusive + (long)(NextULong() % range));
        }

        /// <summary>Float in [0, 1).</summary>
        public float NextFloat() => (NextULong() >> 40) * (1f / (1 << 24));

        /// <summary>Float in [min, max).</summary>
        public float Range(float min, float max) => min + (max - min) * NextFloat();

        /// <summary>True with the given probability (0..1).</summary>
        public bool Chance(float probability) => NextFloat() < probability;

        public T Pick<T>(IReadOnlyList<T> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (items.Count == 0) throw new ArgumentException("Cannot pick from an empty list.", nameof(items));
            return items[NextInt(0, items.Count)];
        }

        /// <summary>In-place Fisher-Yates shuffle.</summary>
        public void Shuffle<T>(IList<T> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = NextInt(0, i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        private static ulong SplitMix64(ref ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            ulong z = x;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>Stable string hash (string.GetHashCode is not guaranteed stable across runtimes).</summary>
        private static ulong Fnv1a64(string text)
        {
            ulong hash = 0xCBF29CE484222325UL;
            for (int i = 0; i < text.Length; i++)
            {
                hash ^= text[i];
                hash *= 0x100000001B3UL;
            }
            return hash;
        }
    }
}
