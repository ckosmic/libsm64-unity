using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LibSM64
{
    public class SM64Context : MonoBehaviour
    {
        private static SM64Context s_instance = null;
        private static int _staticSurfaceCount = 0;

        private List<SM64Mario> _marios = new List<SM64Mario>();
        private List<SM64DynamicTerrain> _surfaceObjects = new List<SM64DynamicTerrain>();

        private void Update()
        {
            if (Interop.isGlobalInit && _staticSurfaceCount > 0)
            {
                foreach (SM64DynamicTerrain o in _surfaceObjects)
                    if (o != null && IsActiveInHierarchyAndEnabled(o))
                        o.contextUpdate();

                foreach (SM64Mario o in _marios)
                    if (o != null && IsActiveInHierarchyAndEnabled(o))
                        o.contextUpdate();
            }
        }

        private void FixedUpdate()
        {
            if (Interop.isGlobalInit && _staticSurfaceCount > 0)
            {
                foreach (SM64DynamicTerrain o in _surfaceObjects)
                    if (o != null && IsActiveInHierarchyAndEnabled(o))
                        o.contextFixedUpdate();

                foreach (SM64Mario o in _marios)
                    if (o != null && IsActiveInHierarchyAndEnabled(o))
                        o.contextFixedUpdate();
            }
        }

        private void OnApplicationQuit()
        {
            Terminate();
        }

        private static void ensureInstanceExists()
        {
            if( s_instance == null )
            {
                var contextGo = new GameObject( "SM64_CONTEXT" );
                contextGo.hideFlags |= HideFlags.HideInHierarchy;
                s_instance = contextGo.AddComponent<SM64Context>();
                DontDestroyOnLoad(contextGo);
            }
        }

        #region Public Methods
        public static void Initialize()
        {
            Interop.GlobalInit(File.ReadAllBytes(Application.dataPath + "/../baserom.us.z64"));
        }

        public static void Initialize(string romPath)
        {
            if(romPath != null)
                Interop.GlobalInit(File.ReadAllBytes(romPath));
        }

        public static void Terminate()
        {
            if (Interop.isGlobalInit)
            {
                for (int i = 0; i < s_instance._surfaceObjects.Count; i++)
                    if (s_instance._surfaceObjects[i] != null)
                        DestroyImmediate(s_instance._surfaceObjects[i]);
                Interop.GlobalTerminate();
                s_instance = null;
            }
        }

        public static void SetScaleFactor(float scale)
        {
            ensureInstanceExists();

            float oldScale = Interop.SCALE_FACTOR / 100.0f;

            Interop.SCALE_FACTOR = scale * 100.0f;
            RefreshStaticTerrain();
            foreach (var o in s_instance._marios)
            {
                o.resetScaleFactor(oldScale);
            }
        }

        public static float GetScaleFactor()
        {
            return Interop.SCALE_FACTOR / 100.0f;
        }

        public static void RefreshStaticTerrain()
        {
            Interop.SM64Surface[] staticSurfaces = Utils.GetAllStaticSurfaces();
            _staticSurfaceCount = staticSurfaces.Length;
            Interop.StaticSurfacesLoad(staticSurfaces);
        }

        public static void RegisterMario( SM64Mario mario )
        {
            ensureInstanceExists();

            if( !s_instance._marios.Contains( mario ))
                s_instance._marios.Add( mario );
        }

        public static void UnregisterMario( SM64Mario mario )
        {
            if( s_instance != null && s_instance._marios.Contains( mario ))
                s_instance._marios.Remove( mario );
        }

        public static void RegisterSurfaceObject( SM64DynamicTerrain surfaceObject )
        {
            ensureInstanceExists();

            if( !s_instance._surfaceObjects.Contains( surfaceObject ))
                s_instance._surfaceObjects.Add( surfaceObject );
        }

        public static void UnregisterSurfaceObject( SM64DynamicTerrain surfaceObject )
        {
            if( s_instance != null && s_instance._surfaceObjects.Contains( surfaceObject ))
                s_instance._surfaceObjects.Remove( surfaceObject );
        }
        #endregion

        private bool IsActiveInHierarchyAndEnabled(Behaviour o)
        {
            return o.gameObject && o.enabled && o.gameObject.activeInHierarchy;
        }
    }
}