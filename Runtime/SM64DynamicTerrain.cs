using UnityEngine;
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

        Vector3 _position;
        Vector3 _lastPosition;
        Vector3 _nextPosition;
        Quaternion _rotation;
        Quaternion _lastRotation;
        Quaternion _nextRotation;
        uint _surfaceObjectId;

        public Vector3 position     { get { return _position;     }}
        public Vector3 lastPosition { get { return _lastPosition; }}
        public Quaternion rotation     { get { return _rotation;     }}
        public Quaternion lastRotation { get { return _lastRotation; }}

        void OnEnable()
        {
            SM64Context.RegisterSurfaceObject( this );

            _position = transform.position;
            _rotation = transform.rotation;
            _lastPosition = _position;
            _lastRotation = _rotation;
            _nextPosition = _position;
            _nextRotation = _rotation;

            Mesh objMesh = null;
            if (GetComponent<MeshCollider>() != null)
            {
                objMesh = GetComponent<MeshCollider>().sharedMesh;
            }
            else if (GetComponent<BoxCollider>() != null)
            {
                if (Utils._unityCubeMesh == null)
                    Utils._unityCubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                objMesh = Utils._unityCubeMesh;
            }
            else
            {
                objMesh = GetComponent<MeshFilter>().sharedMesh;
            }
            if (objMesh != null) 
            {
                var surfaces = Utils.GetSurfacesForMesh(transform.lossyScale, objMesh, surfaceType, terrainType);
                _surfaceObjectId = Interop.SurfaceObjectCreate(_position, _rotation, surfaces.ToArray());
            }
        }

        void OnDisable()
        {
            if( Interop.isGlobalInit )
            {
                SM64Context.UnregisterSurfaceObject( this );
                Interop.SurfaceObjectDelete( _surfaceObjectId );
            }
        }

        internal void contextFixedUpdate()
        {
            _lastPosition = _position;
            _lastRotation = _rotation;

            if( _position != _nextPosition || _rotation != _nextRotation )
            {
                _position = _nextPosition;
                _rotation = _nextRotation;
                Interop.SurfaceObjectMove( _surfaceObjectId, _position, _rotation );
            }
        }

        internal void contextUpdate()
        {
            float t = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;

            transform.position = Vector3.LerpUnclamped( _lastPosition, _position, t );
            transform.rotation = Quaternion.SlerpUnclamped( _lastRotation, _rotation, t );
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