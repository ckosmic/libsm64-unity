using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LibSM64
{
    public class SM64Context : MonoBehaviour
    {
        static SM64Context s_instance = null;

        List<SM64Mario> _marios = new List<SM64Mario>();
        List<SM64DynamicTerrain> _surfaceObjects = new List<SM64DynamicTerrain>();

        void Awake()
        {
            Initialize();
            RefreshStaticTerrain();
        }

        void Update()
        {
            foreach( var o in _surfaceObjects )
                o.contextUpdate();

            foreach( var o in _marios )
                o.contextUpdate();
        }

        void FixedUpdate()
        {
            foreach( var o in _surfaceObjects )
                o.contextFixedUpdate();

            foreach( var o in _marios )
                o.contextFixedUpdate();
        }

        void OnApplicationQuit()
        {
            Terminate();
        }

        static public void Initialize()
        {
            Interop.GlobalInit(File.ReadAllBytes(Application.dataPath + "/../baserom.us.z64"));
        }

        static public void Initialize(string romPath)
        {
            Interop.GlobalInit(File.ReadAllBytes(romPath));
        }

        static public void Terminate() {
            Interop.GlobalTerminate();
            s_instance = null;
        }

        static public void SetScaleFactor(float scale) {
            Interop.SCALE_FACTOR = scale * 100.0f;
        }

        static void ensureInstanceExists()
        {
            if( s_instance == null )
            {
                var contextGo = new GameObject( "SM64_CONTEXT" );
                contextGo.hideFlags |= HideFlags.HideInHierarchy;
                s_instance = contextGo.AddComponent<SM64Context>();
            }
        }

        static public void RefreshStaticTerrain()
        {
            Interop.StaticSurfacesLoad( Utils.GetAllStaticSurfaces() );
        }

        static public void RegisterMario( SM64Mario mario )
        {
            ensureInstanceExists();

            if( !s_instance._marios.Contains( mario ))
                s_instance._marios.Add( mario );
        }

        static public void UnregisterMario( SM64Mario mario )
        {
            if( s_instance != null && s_instance._marios.Contains( mario ))
                s_instance._marios.Remove( mario );
        }

        static public void RegisterSurfaceObject( SM64DynamicTerrain surfaceObject )
        {
            ensureInstanceExists();

            if( !s_instance._surfaceObjects.Contains( surfaceObject ))
                s_instance._surfaceObjects.Add( surfaceObject );
        }

        static public void UnregisterSurfaceObject( SM64DynamicTerrain surfaceObject )
        {
            if( s_instance != null && s_instance._surfaceObjects.Contains( surfaceObject ))
                s_instance._surfaceObjects.Remove( surfaceObject );
        }
    }
}