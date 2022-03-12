﻿using UnityEngine;
using System.Linq;

namespace LibSM64
{
    [RequireComponent(typeof(MeshFilter))]
    public class SM64DynamicTerrain : MonoBehaviour
    {
        [SerializeField] SM64TerrainType terrainType = SM64TerrainType.Grass;
        [SerializeField] SM64SurfaceType surfaceType = SM64SurfaceType.Default;

        public SM64TerrainType TerrainType { get { return terrainType; }}
        public SM64SurfaceType SurfaceType { get { return surfaceType; }}

        private Vector3 _position;
        private Vector3 _lastPosition;
        private Vector3 _nextPosition;
        private Quaternion _rotation;
        private Quaternion _lastRotation;
        private Quaternion _nextRotation;
        private float _lastScaleFactor;
        private uint _surfaceObjectId;

        public Vector3 position     { get { return _position;     }}
        public Vector3 lastPosition { get { return _lastPosition; }}
        public Quaternion rotation     { get { return _rotation;     }}
        public Quaternion lastRotation { get { return _lastRotation; }}

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            SM64Context.RegisterSurfaceObject(this);

            _position = transform.position;
            _rotation = transform.rotation;
            _lastPosition = _position;
            _lastRotation = _rotation;
            _nextPosition = _position;
            _nextRotation = _rotation;

            Mesh objMesh;
            Vector3 meshScale = Vector3.one;
            if (GetComponent<MeshCollider>() != null)
            {
                objMesh = GetComponent<MeshCollider>().sharedMesh;
            }
            else if (GetComponent<BoxCollider>() != null)
            {
                if (Utils.unityCubeMesh == null)
                    Utils.unityCubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                objMesh = Utils.unityCubeMesh;
                meshScale = GetComponent<BoxCollider>().size;
            }
            else
            {
                objMesh = GetComponent<MeshFilter>().sharedMesh;
            }
            if (objMesh != null)
            {
                var surfaces = Utils.GetSurfacesForMesh(Vector3.Scale(meshScale, transform.lossyScale), objMesh, surfaceType, terrainType);
                _surfaceObjectId = Interop.SurfaceObjectCreate(_position, _rotation, surfaces.ToArray());
            }
        }

        public void Terminate()
        {
            if( Interop.isGlobalInit )
            {
                SM64Context.UnregisterSurfaceObject( this );
                Interop.SurfaceObjectDelete( _surfaceObjectId );
            }
        }

        private void OnDestroy()
        {
            Terminate();
        }

        internal void contextFixedUpdate()
        {
            _nextPosition = transform.position;
            _nextRotation = transform.rotation;

            transform.position = _lastPosition;
            transform.rotation = _lastRotation;

            if ( _position != _nextPosition || _rotation != _nextRotation )
            {
                _position = _nextPosition;
                _rotation = _nextRotation;
                Interop.SurfaceObjectMove( _surfaceObjectId, _position, _rotation );
            }

            _lastPosition = _nextPosition;
            _lastRotation = _nextRotation;
        }

        internal void contextUpdate()
        {
            float t = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;

            //transform.position = Vector3.LerpUnclamped( _lastPosition, _position, t );
            //transform.rotation = Quaternion.SlerpUnclamped( _lastRotation, _rotation, t );
        }

        public void SetPosition( Vector3 position )
        {
            _nextPosition = position;
        }

        public void SetRotation( Quaternion rotation )
        {
            _nextRotation = rotation;
        }
    }
}