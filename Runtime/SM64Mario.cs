using System;
using System.Linq;
using UnityEngine;

namespace LibSM64
{
    public class SM64Mario : MonoBehaviour
    {
        [SerializeField] Material material = null;

        SM64InputProvider inputProvider;

        Vector3[][] positionBuffers;
        Vector3[][] normalBuffers;
        Vector3[] lerpPositionBuffer;
        Vector3[] lerpNormalBuffer;
        Vector3[] colorBuffer;
        Color[] colorBufferColors;
        Vector2[] uvBuffer;
        ushort numTrianglesUsed;
        int buffIndex;
        Interop.SM64MarioState[] states;

        Mesh marioMesh;
        uint marioId;

        Vector3 previousVelocity;
        ushort previousNumTrianglesUsed = 0;

        public Action MarioStartedMoving;
        public Action MarioStoppedMoving;
        public GameObject marioRendererObject;

        public void Initialize()
        {
            SM64Context.RegisterMario( this );

            var initPos = transform.position;
            var initRot = transform.eulerAngles;
            marioId = Interop.MarioCreate( new Vector3( -initPos.x, initPos.y, initPos.z ) * Interop.SCALE_FACTOR, initRot );

            inputProvider = GetComponent<SM64InputProvider>();
            if( inputProvider == null )
                throw new System.Exception("Need to add an input provider component to Mario");

            marioRendererObject = new GameObject("MARIO");
            marioRendererObject.hideFlags |= HideFlags.HideInHierarchy;
            
            var renderer = marioRendererObject.AddComponent<MeshRenderer>();
            var meshFilter = marioRendererObject.AddComponent<MeshFilter>();

            states = new Interop.SM64MarioState[2] {
                new Interop.SM64MarioState(),
                new Interop.SM64MarioState()
            };

            renderer.material = material;
            renderer.sharedMaterial.SetTexture("_MainTex", Interop.marioTexture);

            marioRendererObject.transform.localScale = new Vector3( -1, 1, 1 ) / Interop.SCALE_FACTOR;
            marioRendererObject.transform.localPosition = Vector3.zero;

            lerpPositionBuffer = new Vector3[3 * Interop.SM64_GEO_MAX_TRIANGLES];
            lerpNormalBuffer = new Vector3[3 * Interop.SM64_GEO_MAX_TRIANGLES];
            positionBuffers = new Vector3[][] { new Vector3[3 * Interop.SM64_GEO_MAX_TRIANGLES], new Vector3[3 * Interop.SM64_GEO_MAX_TRIANGLES] };
            normalBuffers = new Vector3[][] { new Vector3[3 * Interop.SM64_GEO_MAX_TRIANGLES], new Vector3[3 * Interop.SM64_GEO_MAX_TRIANGLES] };
            colorBuffer = new Vector3[3 * Interop.SM64_GEO_MAX_TRIANGLES];
            colorBufferColors = new Color[3 * Interop.SM64_GEO_MAX_TRIANGLES];
            uvBuffer = new Vector2[3 * Interop.SM64_GEO_MAX_TRIANGLES];

            marioMesh = new Mesh();
            marioMesh.vertices = lerpPositionBuffer;
            marioMesh.triangles = Enumerable.Range(0, 3*Interop.SM64_GEO_MAX_TRIANGLES).ToArray();
            meshFilter.sharedMesh = marioMesh;
        }

        public void Terminate()
        {
            if( marioRendererObject != null )
            {
                Destroy( marioRendererObject );
                marioRendererObject = null;
            }

            if( Interop.isGlobalInit )
            {
                Interop.MarioDelete( marioId );
                SM64Context.UnregisterMario(this);
            }
        }

        public void SetAction(SM64MarioAction action)
        {
            Interop.MarioSetAction(action);
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
            Interop.MarioSetPosition(position);
        }

        public void SetRotation(Quaternion rotation)
        {
            Interop.MarioSetRotation(rotation);
        }

        public void SetVelocity(Vector3 velocity)
        {
            Interop.MarioSetVelocity(velocity);
        }

        public void SetFowardVelocity(float velocity)
        {
            Interop.MarioSetForwardVelocity(velocity);
        }

        public void SetColors(Color32[] unityColors)
        {
            Interop.MarioSetColors(unityColors);
        }

        public void contextFixedUpdate()
        {
            var inputs = new Interop.SM64MarioInputs();
            var look = inputProvider.GetCameraLookDirection();
            look.y = 0;
            look = look.normalized;

            var joystick = inputProvider.GetJoystickAxes();

            inputs.camLookX = -look.x;
            inputs.camLookZ = look.z;
            inputs.stickX = joystick.x;
            inputs.stickY = -joystick.y;
            inputs.buttonA = inputProvider.GetButtonHeld( SM64InputProvider.Button.Jump  ) ? (byte)1 : (byte)0;
            inputs.buttonB = inputProvider.GetButtonHeld( SM64InputProvider.Button.Kick  ) ? (byte)1 : (byte)0;
            inputs.buttonZ = inputProvider.GetButtonHeld( SM64InputProvider.Button.Stomp ) ? (byte)1 : (byte)0;

            states[buffIndex] = Interop.MarioTick( marioId, inputs, positionBuffers[buffIndex], normalBuffers[buffIndex], colorBuffer, uvBuffer, out numTrianglesUsed );

            if (previousNumTrianglesUsed != numTrianglesUsed)
            {
                for (int i = numTrianglesUsed * 3; i < positionBuffers[buffIndex].Length; i++)
                {
                    positionBuffers[buffIndex][i] = Vector3.zero;
                    normalBuffers[buffIndex][i] = Vector3.zero;
                }
                positionBuffers[buffIndex].CopyTo(positionBuffers[1 - buffIndex], 0);
                normalBuffers[buffIndex].CopyTo(normalBuffers[1 - buffIndex], 0);
                positionBuffers[buffIndex].CopyTo(lerpPositionBuffer, 0);
                normalBuffers[buffIndex].CopyTo(lerpNormalBuffer, 0);

                previousNumTrianglesUsed = numTrianglesUsed;
            }

            for ( int i = 0; i < colorBuffer.Length; ++i )
                colorBufferColors[i] = new Color( colorBuffer[i].x, colorBuffer[i].y, colorBuffer[i].z, 1 );

            marioMesh.colors = colorBufferColors;
            marioMesh.uv = uvBuffer;

            buffIndex = 1 - buffIndex;
        }

        public void contextUpdate()
        {
            float t = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
            int j = 1 - buffIndex;

            for( int i = 0; i < numTrianglesUsed * 3; ++i )
            {
                lerpPositionBuffer[i] = Vector3.LerpUnclamped(positionBuffers[buffIndex][i], positionBuffers[j][i], t);
                lerpNormalBuffer[i] = Vector3.LerpUnclamped(normalBuffers[buffIndex][i], normalBuffers[j][i], t);
            }

            transform.position = Vector3.LerpUnclamped( states[buffIndex].unityPosition, states[j].unityPosition, t );
            transform.rotation = Quaternion.Euler(0f, 360f - states[buffIndex].faceAngle * Mathf.Rad2Deg, 0f);

            marioMesh.vertices = lerpPositionBuffer;
            marioMesh.normals = lerpNormalBuffer;

            marioMesh.RecalculateBounds();
            marioMesh.RecalculateTangents();

            Vector3 velocity = SM64Vec3ToVector3(states[buffIndex].velocity);
            if (MarioStartedMoving != null && previousVelocity.magnitude == 0 && velocity.magnitude > 0)
                MarioStartedMoving.Invoke();
            else if (MarioStoppedMoving != null && velocity.magnitude == 0 && previousVelocity.magnitude > 0)
                MarioStoppedMoving.Invoke();
            previousVelocity = velocity;
        }

        private Vector3 SM64Vec3ToVector3(float[] sm64Vec)
        {
            if(sm64Vec != null && sm64Vec.Length >= 3)
                return new Vector3(sm64Vec[0], sm64Vec[1], sm64Vec[2]);
            return Vector3.zero;
        }

        void OnDrawGizmos()
        {
            if( !Application.isPlaying )
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere( transform.position, 0.5f );
            }
        }
    }
}