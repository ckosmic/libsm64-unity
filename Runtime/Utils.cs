using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LibSM64
{
    static internal class Utils
    {
        internal static Mesh _unityCubeMesh = null;

        static void transformAndGetSurfaces( List<Interop.SM64Surface> outSurfaces, Mesh mesh, SM64SurfaceType surfaceType, SM64TerrainType terrainType, Func<Vector3,Vector3> transformFunc )
        {
            var tris = mesh.GetTriangles(0);
            var vertices = mesh.vertices.Select(transformFunc).ToArray();

            for( int i = 0; i < tris.Length; i += 3 )
            {
                outSurfaces.Add(new Interop.SM64Surface {
                    force = 0,
                    type = (short)surfaceType,
                    terrain = (ushort)terrainType,
                    v0x = (short)(Interop.SCALE_FACTOR * -vertices[tris[i  ]].x),
                    v0y = (short)(Interop.SCALE_FACTOR *  vertices[tris[i  ]].y),
                    v0z = (short)(Interop.SCALE_FACTOR *  vertices[tris[i  ]].z),
                    v1x = (short)(Interop.SCALE_FACTOR * -vertices[tris[i+2]].x),
                    v1y = (short)(Interop.SCALE_FACTOR *  vertices[tris[i+2]].y),
                    v1z = (short)(Interop.SCALE_FACTOR *  vertices[tris[i+2]].z),
                    v2x = (short)(Interop.SCALE_FACTOR * -vertices[tris[i+1]].x),
                    v2y = (short)(Interop.SCALE_FACTOR *  vertices[tris[i+1]].y),
                    v2z = (short)(Interop.SCALE_FACTOR *  vertices[tris[i+1]].z)
                });
            }
        }

        static public Interop.SM64Surface[] GetSurfacesForMesh( Vector3 scale, Mesh mesh, SM64SurfaceType surfaceType, SM64TerrainType terrainType )
        {
            var surfaces = new List<Interop.SM64Surface>();
            transformAndGetSurfaces( surfaces, mesh, surfaceType, terrainType, x => Vector3.Scale( scale, x ));
            return surfaces.ToArray();
        }

        static public Interop.SM64Surface[] GetAllStaticSurfaces()
        {
            var surfaces = new List<Interop.SM64Surface>();

            foreach( var obj in GameObject.FindObjectsOfType<SM64StaticTerrain>())
            {
                if (!obj.enabled) continue;

                Mesh objMesh;
                Vector3 meshScale = Vector3.one;
                if (obj.GetComponent<MeshCollider>() != null)
                {
                    objMesh = obj.GetComponent<MeshCollider>().sharedMesh;
                }
                else if (obj.GetComponent<BoxCollider>() != null)
                {
                    if (_unityCubeMesh == null)
                        _unityCubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                    objMesh = _unityCubeMesh;
                    meshScale = obj.GetComponent<BoxCollider>().size;
                }
                else
                {
                    objMesh = obj.GetComponent<MeshFilter>().sharedMesh;
                }
                if (objMesh != null)
                    transformAndGetSurfaces( surfaces, objMesh, obj.SurfaceType, obj.TerrainType, x => obj.transform.TransformPoint(Vector3.Scale(meshScale, x)));
            }

            return surfaces.ToArray();
        }
    }
}