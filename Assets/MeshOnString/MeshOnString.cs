using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;


[ExecuteAlways]
public class MeshOnString : MonoBehaviour
{

    public bool dynamicAnchors;
    public bool debugRopePoints;
    public bool debugVertPoints;

    public bool showMesh;
    public bool parallelTransport;
    public Transform[] ropeAnchors;


    public int stiffnessIterations = 2;
    public float _RopeLength;
    public Vector3 _Gravity;

    public Vector3 _Forward;    
    public float _Dampening;

    
public float _AnchorNormalForce;
public float _MaxForce;
public float _MaxVel;
public float _ForceMultiplier;

public float _NoiseSize;
public float _NoisePower;

public float _Radius = 10;


    
    
    public ComputeBuffer _RopeBuffer;
    public ComputeBuffer _VertBuffer;
    public ComputeBuffer _AnchorBuffer;
    public ComputeBuffer _TriBuffer;
    public int _NumVertsPerRope;

    public Mesh mesh;
    public ComputeShader shader;

    public Transform meshNormalizationTransform;
    

    int numVerts;
    int _NumTris;

    int vertBufferStructSize;
    int ropeBufferStructSize;


    float[] values;
    int[] triValues;

    int _NumInstances;
    
    uint numThreads;

    int _NumVerts;
    int _TotalRopePoints;
    public int _TotalVerts;
    int _TotalTris;


    public Material meshMaterial;
    public Material debugRopeMaterial;
    public Material debugVertMaterial;
    

    Matrix4x4[] anchors;
    // Start is called before the first frame update
    void  OnEnable()
    {
    vertBufferStructSize = 3 + 3 + 3+ 3 + 3 + 3 + 2 + 4;
    ropeBufferStructSize = 3 + 3+ 3+ 3 +3+2 + 3;
        _NumInstances = ropeAnchors.Length;

        anchors = new Matrix4x4[_NumInstances];
        for( int i = 0; i < _NumInstances; i++ ){
            anchors[i] = ropeAnchors[i].localToWorldMatrix;
        }

        _TotalRopePoints = _NumInstances * _NumVertsPerRope;

        _NumVerts = mesh.vertices.Length;
        _NumTris = mesh.triangles.Length;

        _TotalVerts = _NumVerts * _NumInstances;
        _TotalTris = _NumTris * _NumInstances;
    
        _RopeBuffer = new ComputeBuffer( _NumVertsPerRope * _NumInstances , sizeof(float)* ropeBufferStructSize);
        _VertBuffer = new ComputeBuffer( _NumVerts *  _NumInstances , sizeof(float)* vertBufferStructSize);
        _TriBuffer  = new ComputeBuffer( _NumTris *  _NumInstances , sizeof(int));
        _AnchorBuffer =  new ComputeBuffer(  _NumInstances , sizeof(float)* 16);


        
        uint y; uint z;
        shader.GetKernelThreadGroupSizes(0, out numThreads , out y, out z);


        values = new float[_TotalVerts * vertBufferStructSize];
        triValues = new int[_TotalTris ];
        int index = 0;

        Vector3[] verts = mesh.vertices;
        Vector3[] nors = mesh.normals;
        Vector4[] tans = mesh.tangents;
        Vector2[] uvs = mesh.uv;
        Color[] colors = mesh.colors;
        int[] tris = mesh.triangles;



        print( _NumInstances );
        print( vertBufferStructSize );
        print( mesh.vertices.Length);

        for( int i = 0; i < _NumInstances; i++ ){
            for( int j = 0;  j < _NumVerts; j++ ){
                

                // To be calculated in compute shader
                values[index++] = 0; 
                values[index++] = 0; 
                values[index++] = 0;

                values[index++] = 0;
                values[index++] = 0;
                values[index++] = 0;

                values[index++] = 0;
                values[index++] = 0;
                values[index++] = 0;



                // Originals

                float3 fPos =meshNormalizationTransform.TransformPoint( verts[j] );


                values[index++] =fPos.x; 
                values[index++] =fPos.y; 
                values[index++] =fPos.z;



                float3 fNor =meshNormalizationTransform.TransformDirection( nors[j] );
                values[index++] = fNor.x;
                values[index++] = fNor.y;
                values[index++] = fNor.z;


                float4 t = tans[j];
                float3 fTan =meshNormalizationTransform.TransformDirection( t.xyz );
                values[index++] = fTan.x;
                values[index++] = fTan.y;
                values[index++] = fTan.z;


                values[index++] = uvs[j].x;
                values[index++] = uvs[j].y;


                values[index++] = colors[j].r;    
                values[index++] = colors[j].g;
                values[index++] = colors[j].b;
                values[index++] = colors[j].a;


            }  
        }

        _VertBuffer.SetData( values);


        index = 0;

        triValues = new int[ _TotalTris];

        for( int i = 0; i < _NumInstances; i++ ){

            int baseTri = i * tris.Length;
            int baseVert = i * verts.Length;
            for( int j = 0; j < tris.Length; j++ ){

                triValues[index++] = tris[j] + baseVert;

            }
        }

        _TriBuffer.SetData(triValues);



                              
        
    }


    //TODO: RELEASE BUFFERS
    void  Disable()
    {
        
    }




    MaterialPropertyBlock  mpb;
    int numGroups;
    // Update is called once per frame
    void LateUpdate()
    {
        if( dynamicAnchors ){

            for( int i = 0; i < _NumInstances; i++ ){
                anchors[i] = ropeAnchors[i].localToWorldMatrix;
            }

            _AnchorBuffer.SetData(anchors);
        }



        shader.SetVector("_Forward", _Forward );
        shader.SetInt("_NumInstances", _NumInstances);
        shader.SetInt("_NumVertsPerRope", _NumVertsPerRope);
        shader.SetInt("_NumVerts", _NumVerts);
        shader.SetInt("_NumTris", _NumTris);
        shader.SetFloat("_RopeLength", _RopeLength);
        shader.SetVector("_Gravity",_Gravity);
        shader.SetVector("_Forward ",_Forward );
        shader.SetFloat("_Dampening",_Dampening);
        shader.SetFloat("_NoisePower",_NoisePower);
        shader.SetFloat("_NoiseSize",_NoiseSize);
        shader.SetFloat("_Dampening",_Dampening);

        shader.SetFloat("_AnchorNormalForce",_AnchorNormalForce);
        shader.SetFloat("_MaxForce",_MaxForce);
        shader.SetFloat("_MaxVel",_MaxVel);
        shader.SetFloat("_ForceMultiplier",_ForceMultiplier);


        shader.SetFloat("_Radius",_Radius);

        

        numGroups = ((_TotalRopePoints)+((int)numThreads-1))/(int)numThreads;

        shader.SetBuffer( 0 , "_AnchorBuffer" , _AnchorBuffer );
        shader.SetBuffer( 0 , "_RopeBuffer" , _RopeBuffer );
        
        shader.Dispatch( 0,numGroups ,1,1);


    for( int i = 0; i < stiffnessIterations; i++ ){

        shader.SetBuffer( 1 , "_AnchorBuffer" , _AnchorBuffer );
        shader.SetBuffer( 1 , "_RopeBuffer" , _RopeBuffer );
        shader.Dispatch( 1,numGroups ,1,1);


        shader.SetBuffer( 2 , "_AnchorBuffer" , _AnchorBuffer );
        shader.SetBuffer( 2 , "_RopeBuffer" , _RopeBuffer );
        shader.Dispatch( 2,numGroups ,1,1);

    }


    // TODO parallel transport
    if( parallelTransport ){
        shader.SetBuffer( 4 , "_AnchorBuffer" , _AnchorBuffer );
        shader.SetBuffer( 4 , "_RopeBuffer" , _RopeBuffer );
        shader.Dispatch( 4,numGroups ,1,1);
    }else{
        shader.SetBuffer( 3 , "_AnchorBuffer" , _AnchorBuffer );
        shader.SetBuffer( 3 , "_RopeBuffer" , _RopeBuffer );
        shader.Dispatch( 3  , numGroups ,1,1);
    }
        


        numGroups = ((_TotalVerts)+((int)numThreads-1))/(int)numThreads;

        shader.SetBuffer( 5 , "_VertBuffer" , _VertBuffer );
        shader.SetBuffer( 5 , "_RopeBuffer" , _RopeBuffer );
        shader.Dispatch( 5  , numGroups ,1,1);

        if(mpb == null ){
            mpb = new MaterialPropertyBlock();
        }

        mpb.SetBuffer("_RopeBuffer", _RopeBuffer);
        mpb.SetBuffer("_AnchorBuffer", _AnchorBuffer);
        mpb.SetBuffer("_VertBuffer", _VertBuffer);
        mpb.SetBuffer("_TriBuffer", _TriBuffer);

        if( debugRopePoints ){
    
        
        Graphics.DrawProcedural( 
            debugRopeMaterial,  
            new Bounds(transform.position, Vector3.one * 5000), 
            MeshTopology.Triangles,
            _TotalRopePoints * 3 * 2 * 4, 
            1, null, mpb, 
            ShadowCastingMode.Off, 
            true, 
            LayerMask.NameToLayer("Default")
        );

        }

        if( debugVertPoints ){
            Graphics.DrawProcedural( 
                debugVertMaterial,  
                new Bounds(transform.position, Vector3.one * 5000), 
                MeshTopology.Triangles,
                _TotalVerts * 3 * 2, 
                1, null, mpb, 
                ShadowCastingMode.Off, 
                true, 
                LayerMask.NameToLayer("Default")
            );
        }

        if( showMesh ){
            Graphics.DrawProcedural( 
                meshMaterial,  
                new Bounds(transform.position, Vector3.one * 5000), 
                MeshTopology.Triangles,
                _TotalTris, 
                1, null, mpb, 
                ShadowCastingMode.On, 
                true, 
                LayerMask.NameToLayer("Default")
            );
        }
    
    }
}
