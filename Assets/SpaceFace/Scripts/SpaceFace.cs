using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GPUProcBod{
[ExecuteAlways]
public class SpaceFace : MonoBehaviour
{

    public bool updateInEdit;
    private ComputeBuffer blendShapeVerts;
    private ComputeBuffer placedVerts;
    public SkinnedMeshRenderer faceMeshRenderer;
    public ComputeShader placeShader;


    public Texture2D sizeMap;


    // CANNOT equal 0
    public int kernel = 2;



    private ComputeBuffer weightBuffer;

    private ComputeBuffer[] particleBuffers;
    public int[] particleCounts;
    public float[] noiseSizes;
    public string[] noiseTypes;
    public Material[] materials;

    public float[] particleOscillateSize;
    public float[] particleOscillateSpeed;
    public float[] particleBlinkSize;
    public float[] particleBlinkSpeed;

    public float[] particleSize;
    public float[] particleHueStart;
    public float[] particleHueSize;

    MaterialPropertyBlock[] mpbs;
    

    int pStructSize;
    int vStructSize;

      private Mesh m;
  private Mesh baseMesh;
  private Vector3[] baseVerts;
  private Vector3[] baseNors;
  private Vector4[] baseTans;
  private Vector2[] baseUVs;

  



int numBlendShapes;
int numVertsPerBlendShape;

float[] values;


uint numThreads;
uint numGroups;

public void OnEnable(){
    uint y; uint z;
    placeShader.GetKernelThreadGroupSizes(0, out numThreads , out y, out z);
    MakeBuffers();
}

public void OnDisable(){
    ReleaseBuffers();
}

public void ReleaseBuffers(){
    for( int i = 0; i < particleCounts.Length; i++ ){
        if(particleBuffers[i] != null){ particleBuffers[i].Release(); }
    }

    if( blendShapeVerts != null ){ blendShapeVerts.Release(); }
}


    public void MakeBuffers(){

    particleBuffers = new ComputeBuffer[ particleCounts.Length ];

        /*

            Setting up the buffers

        */

        pStructSize = 24;
        for( int i = 0; i < particleCounts.Length; i++){
            particleBuffers[i] = new ComputeBuffer( particleCounts[i] , pStructSize * sizeof(float) );
        }

    m = faceMeshRenderer.sharedMesh;
    numVertsPerBlendShape = m.vertices.Length;
    numBlendShapes = m.blendShapeCount;

    int count = (numBlendShapes + 1)  * numVertsPerBlendShape;
    vStructSize = 16;
    blendShapeVerts = new ComputeBuffer( count , vStructSize * sizeof(float));
    placedVerts = new ComputeBuffer( numVertsPerBlendShape , vStructSize * sizeof(float));
    weightBuffer = new ComputeBuffer( numBlendShapes , sizeof(float));


        

        /*

            Populating our blend shape buffer


        */

     int[] triangles = m.triangles;
    Vector3[] verts;
    Vector2[] uvs;
    Vector4[] tans;
    Color[] cols;
    Vector3[] nors;


    int index = 0;


          Mesh m1 = new Mesh();
       for( int k = 0; k < numBlendShapes; k++ ){
        faceMeshRenderer.SetBlendShapeWeight( k , 0);
      } 
      
    faceMeshRenderer.BakeMesh(m1);
    baseMesh = m1; 
    baseVerts = m1.vertices;
    baseNors = m1.normals;
    baseTans = m1.tangents;
    baseUVs = m1.uv;
    


    values = new float[count*vStructSize];

    /*

        For every blendshape, *bake* that blend shape, then get the difference
        between IT and the Base Mesh. When we blend the shapes we will use the
        weights multiplied by the difference to get teh final position

    */
    for( int i = 0; i <= numBlendShapes; i ++ ){
    
      for( int k = 0; k < numBlendShapes; k++ ){
        faceMeshRenderer.SetBlendShapeWeight( k , 0);
      }

      if( i > 0 ){
        faceMeshRenderer.SetBlendShapeWeight( i-1, 100);
      }


      m1 = new Mesh();
      
      faceMeshRenderer.BakeMesh(m1);
   
      verts = m1.vertices;
      uvs = m1.uv;
      tans = m1.tangents;
      cols = m1.colors;
      nors = m1.normals;

      for( int j = 0; j < numVertsPerBlendShape; j ++ ){

        if( i == 0 ){
        values[ index ++ ] = verts[j].x;
        values[ index ++ ] = verts[j].y;
        values[ index ++ ] = verts[j].z;

        values[ index ++ ] = 0;
        values[ index ++ ] = 0;
        values[ index ++ ] = 0;

        values[ index ++ ] = nors[j].x;
        values[ index ++ ] = nors[j].y;
        values[ index ++ ] = nors[j].z;

        values[ index ++ ] = tans[j].x;
        values[ index ++ ] = tans[j].y;
        values[ index ++ ] = tans[j].z;

        values[ index ++ ] = uvs[j].x;
        values[ index ++ ] = uvs[j].y;

        values[ index ++ ] = (float)i/(float)count;
        values[ index ++ ] = (float)j/(float)numVertsPerBlendShape;
        }else{

        values[ index ++ ] = verts[j].x-baseVerts[j].x;
        values[ index ++ ] = verts[j].y-baseVerts[j].y;
        values[ index ++ ] = verts[j].z-baseVerts[j].z;

        values[ index ++ ] = 0;
        values[ index ++ ] = 0;
        values[ index ++ ] = 0;

        values[ index ++ ] = nors[j].x-baseNors[j].x;
        values[ index ++ ] = nors[j].y-baseNors[j].y;
        values[ index ++ ] = nors[j].z-baseNors[j].z;

        values[ index ++ ] = tans[j].x-baseTans[j].x;
        values[ index ++ ] = tans[j].y-baseTans[j].y;
        values[ index ++ ] = tans[j].z-baseTans[j].z;

        values[ index ++ ] = uvs[j].x;
        values[ index ++ ] = uvs[j].y;

        values[ index ++ ] = (float)i/(float)count;
        values[ index ++ ] = (float)j/(float)numVertsPerBlendShape;
        }
        
        }


      }

      
      blendShapeVerts.SetData( values );

      /*

        PLACE OUR PARTICLES ON THE FACE MESH

      */

      // use m1 as thats our base mesh
        for( int particleGroup = 0; particleGroup < particleCounts.Length; particleGroup++ ){

            float noiseSize = noiseSizes[particleGroup];
            string noiseType = noiseTypes[particleGroup];
            int particleCount = particleCounts[particleGroup];

    
            float[] triangleAreas = new float[triangles.Length / 3];
            float totalArea = 0;

            int tri0;
            int tri1;
            int tri2;

            /*

                First loop through every triangle and
                assign it a 'size' to randomly place our particles on

            */
            for (int i = 0; i < triangles.Length / 3; i++) {
            
                tri0 = i * 3;
                tri1 = tri0 + 1;
                tri2 = tri0 + 2;
                
                tri0 = triangles[tri0];
                tri1 = triangles[tri1];
                tri2 = triangles[tri2];
                
                float area = 1;

                if( noiseType =="even"){ 
                    area = HELP.AreaOfTriangle (baseVerts[tri0], baseVerts[tri1], baseVerts[tri2]);
                }else if( noiseType =="fractal" ){
                    area = HELP.NoiseTriangleArea(noiseSize, baseVerts[tri0],  baseVerts[tri1], baseVerts[tri2]);
                    area = Mathf.Pow( area, 3);
                }else if( noiseType == "band"){
                    float avePos = baseVerts[tri0].y + baseVerts[tri1].y + baseVerts[tri2].y;
                    area = HELP.AreaOfTriangle (baseVerts[tri0], baseVerts[tri1], baseVerts[tri2])/(.01f + Mathf.Abs(100 * avePos));
                }else if( noiseType == "map"){

                    float col1  = sizeMap.GetPixelBilinear( baseUVs[tri0].x , baseUVs[tri0].y ).r;
                          col1 += sizeMap.GetPixelBilinear( baseUVs[tri1].x , baseUVs[tri1].y ).r;
                          col1 += sizeMap.GetPixelBilinear( baseUVs[tri2].x , baseUVs[tri2].y ).r;
                    area = 1/(.2f+col1);

                }

                triangleAreas[i] = area;
                 totalArea += area;
            
            }

            for (int i = 0; i < triangleAreas.Length; i++) {
            triangleAreas[i] /= totalArea;
            }


            /*

                The using that data,
                go through, get the barycentric coordinates of randomly place particles,
                and populate teh buffer using those
            */

            values = new float[particleCount*pStructSize];

            index = 0;


            Vector3 pos;
            Vector2 uv;
            Vector3 tan;
            Vector3 nor;
            int baseTri;

            for( int i = 0; i < particleCount; i ++ ){

            baseTri = 3 * HELP.getTri (Random.value, triangleAreas);

            tri0 = baseTri + 0;
            tri1 = baseTri + 1;
            tri2 = baseTri + 2;

            tri0 = triangles[tri0];
            tri1 = triangles[tri1];
            tri2 = triangles[tri2];

            pos = HELP.GetRandomPointInTriangle(i, baseVerts[tri0], baseVerts[tri1], baseVerts[tri2]);

            float a0 = HELP.AreaOfTriangle(pos, baseVerts[tri1], baseVerts[tri2]);
            float a1 = HELP.AreaOfTriangle(pos, baseVerts[tri0], baseVerts[tri2]);
            float a2 = HELP.AreaOfTriangle(pos, baseVerts[tri0], baseVerts[tri1]);

            float aTotal = a0 + a1 + a2;

            float p0 = a0 / aTotal;
            float p1 = a1 / aTotal;
            float p2 = a2 / aTotal;

            nor = (baseNors[tri0] * p0 + baseNors[tri1] * p1 + baseNors[tri2] * p2).normalized;
            uv = baseUVs[tri0] * p0 + baseUVs[tri1] * p1 + baseUVs[tri2] * p2;
            //tan = (HELP.ToV3(tans[tri0]) * p0 + HELP.ToV3(tans[tri1]) * p1 + HELP.ToV3(tans[tri2]) * p2).normalized;


            //print(uv);
            values[ index ++ ] = pos.x;
            values[ index ++ ] = pos.y;
            values[ index ++ ] = pos.z;

            values[ index ++ ] = 0;
            values[ index ++ ] = 0;
            values[ index ++ ] = 0;

            values[ index ++ ] = nor.x;
            values[ index ++ ] = nor.y;
            values[ index ++ ] = nor.z;

            values[ index ++ ] = 0;
            values[ index ++ ] = 0;
            values[ index ++ ] = 0;

            values[ index ++ ] = uv.x;
            values[ index ++ ] = uv.y;

        
            values[index++ ] = (float)i/(float)count;

            values[ index ++ ] = tri0;
            values[ index ++ ] = tri1;
            values[ index ++ ] = tri2;

            values[ index ++ ] = p0;
            values[ index ++ ] = p1;
            values[ index ++ ] = p2;

            values[ index ++ ] = 1;
            values[ index ++ ] = 0;
            values[ index ++ ] = 0;

            }


            particleBuffers[particleGroup].SetData( values );



        }




    }


    public void Update(){

        /*

            Every Frame we've got to set teh 
            weights for the blend shapes

        */
        values = new float[ numBlendShapes ];
        for( int i = 0; i < numBlendShapes; i++ ){
            values[i]  = faceMeshRenderer.GetBlendShapeWeight( i ) /100;
        }

        weightBuffer.SetData( values );


        /*
            
            This first pass will properly place
            our verts along the new blend shapes

        */
        placeShader.SetBuffer(0,"_VertBuffer", placedVerts);
        placeShader.SetBuffer(0,"_BaseBuffer", blendShapeVerts);
        placeShader.SetBuffer(0,"_BlendWeightBuffer", weightBuffer );

        placeShader.SetInt("_VertBuffer_COUNT", numVertsPerBlendShape);
        placeShader.SetInt("_BaseBuffer_COUNT", (numBlendShapes + 1)* numVertsPerBlendShape);
        placeShader.SetInt("_BlendWeightBuffer_COUNT", numBlendShapes);

        placeShader.SetFloat("_Time", Time.time);

        numGroups = ((uint)numVertsPerBlendShape+(numThreads-1))/numThreads;
        placeShader.Dispatch( 0,(int)numGroups,1,1);


        /*

            Then place All our particles on top of this!

        */

        for(int i = 0; i< particleCounts.Length; i++ ){
            placeShader.SetBuffer(kernel,"_VertBuffer", placedVerts);
            placeShader.SetBuffer(kernel,"_ParticleBuffer",particleBuffers[i]);
            placeShader.SetFloat("_OscillateSize" , particleOscillateSize[i] );
            placeShader.SetFloat("_OscillateSpeed" , particleOscillateSpeed[i] );
            placeShader.SetFloat("_BlinkSize" , particleBlinkSize[i] );
            placeShader.SetFloat("_BlinkSpeed" , particleBlinkSpeed[i] );
            placeShader.SetMatrix("_World", transform.localToWorldMatrix);
            
            placeShader.SetInt("_ParticleBuffer_COUNT", particleCounts[i]);
            numGroups = ((uint)particleCounts[i]+(numThreads-1))/numThreads;
            placeShader.Dispatch( kernel,(int)numGroups,1,1);
        }

    }


    public void LateUpdate(){

        if( mpbs == null ){
            mpbs = new MaterialPropertyBlock[ particleCounts.Length ];
            for( int i = 0; i < mpbs.Length; i++){
                mpbs[i] = new MaterialPropertyBlock();
            }
        }

        for( int i = 0; i < mpbs.Length; i++){
            mpbs[i].SetBuffer("_VertBuffer", particleBuffers[i]);
            mpbs[i].SetInt("_Count",particleCounts[i]);
            mpbs[i].SetMatrix("_Transform",transform.localToWorldMatrix);
            mpbs[i].SetFloat("_Size", particleSize[i]);
            mpbs[i].SetFloat("_HueSize", particleHueSize[i]);
            mpbs[i].SetFloat("_HueStart", particleHueStart[i]);
            
            Graphics.DrawProcedural(
                materials[i],  
                new Bounds(transform.position, Vector3.one * 5000), 
                MeshTopology.Triangles, 
                particleCounts[i] * 3 * 2, 
                1, null, 
                mpbs[i], 
                ShadowCastingMode.Off, 
                true, 
                LayerMask.NameToLayer("Default")
            );
        }
    }

   // Updates in Edit Mode!
   void OnDrawGizmos()
   {
 
      #if UNITY_EDITOR
            // Ensure continuous Update calls.
            if (!Application.isPlaying && updateInEdit )
            {
        
               UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
               UnityEditor.SceneView.RepaintAll();
            }
      #endif

   }
}}
