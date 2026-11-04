using Unity.AI.Navigation;
using UnityEngine;

namespace Game.Gameplay.Enemies
{
    /// <summary>
    /// Bakes the attached NavMeshSurface when the scene starts. Good for the generated sandbox
    /// (no baked asset to keep in sync). Handcrafted arenas should bake in the Editor instead
    /// (NavMeshSurface > Bake) and leave this component off.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(NavMeshSurface))]
    public sealed class RuntimeNavMeshBaker : MonoBehaviour
    {
        private void Awake()
        {
            var surface = GetComponent<NavMeshSurface>();
            float start = Time.realtimeSinceStartup;
            surface.BuildNavMesh();
            Debug.Log($"[RuntimeNavMeshBaker] NavMesh built in {(Time.realtimeSinceStartup - start) * 1000f:0} ms");
        }
    }
}
