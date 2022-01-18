using UnityEngine;

namespace LibSM64
{
    [RequireComponent(typeof(MeshFilter))]
    public class SM64StaticTerrain : MonoBehaviour
    {
        [SerializeField] SM64TerrainType terrainType = SM64TerrainType.Grass;
        [SerializeField] SM64SurfaceType surfaceType = SM64SurfaceType.Default;

        public SM64TerrainType TerrainType { get { return terrainType; }}
        public SM64SurfaceType SurfaceType { get { return surfaceType; }}
    }
}