using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shatterObj : MonoBehaviour{
    public shatterMesh substrate = null;
    public GameObject cutTemplate;
    public GameObject substrateTemplate;

    private int consumptionNumber = 0;

    void Start(){
        if (substrate == null){
            substrate = new shatterMesh(GetComponent<MeshFilter>().mesh);
        }
    }

    public void shatter(List<shatterMesh> cells){
        int i = 0;
        foreach (shatterMesh c in cells){
            substrate.intersect(c);
            i+=1;
            for (int j=i; j<cells.Count; j++){
                c.intersect(cells[j]);
            }
        }
        /*foreach(shatterTriangle t in substrate.triangles){
            if (!t.culled){
                Debug.Log(t.verts[0].pos);
                Debug.Log(t.verts[1].pos);
                Debug.Log(t.verts[2].pos);
                Debug.Log("done with tri");
            }
        }*/

        foreach (shatterTriangle t in substrate.triangles){
            if (!t.isConsumed && !t.culled){
                floodGenerateFragment(t);
            }
        }
        Destroy(gameObject);
    }

    private void floodGenerateFragment(shatterTriangle first){
        List<shatterTriangle> newSubstrateTris = new List<shatterTriangle>();
        List<shatterTriangle> newCutTris = new List<shatterTriangle>();
        Queue<shatterTriangle> flood = new Queue<shatterTriangle>();
        flood.Enqueue(first);
        first.isConsumed = true;
        while (flood.Count != 0){
            shatterTriangle currentTri = flood.Dequeue();
            foreach (shatterTriangle a in currentTri.adjacent){
                if (!a.isConsumed && !a.culled){
                    flood.Enqueue(a);
                    a.isConsumed = true;
                }
            }
            if (currentTri.isSubstrate){
                newSubstrateTris.Add(currentTri);
            }
            else{
                newCutTris.Add(currentTri);
            }
        }  

        generateFragment(newSubstrateTris, newCutTris);
    }

    private void generateFragment(List<shatterTriangle> subTris, List<shatterTriangle> cutTris){
        List<Vector3> subVertices = new List<Vector3>();
        List<Vector3> cutVertices = new List<Vector3>();
        List<Vector2> subUV = new List<Vector2>();
        List<Vector2> cutUV = new List<Vector2>();;
        List<int> subTriangles = new List<int>();
        List<int> cutTriangles = new List<int>();
        shatterMesh newShatter = new shatterMesh();
        int meshingNumber = 0;

        foreach (shatterTriangle tri in subTris){
            foreach (shatterVert v in tri.verts){
                if (v.consumptionNumber != consumptionNumber){
                    v.consumptionNumber = consumptionNumber;
                    v.meshingNumber = meshingNumber++;
                    subVertices.Add(v.pos);
                    subUV.Add(v.uv);
                    newShatter.verts.Add(v);
                }
                subTriangles.Add(v.meshingNumber);
            }
            newShatter.triangles.Add(tri);
        }
        
        consumptionNumber++;
        meshingNumber = 0;
        foreach (shatterTriangle tri in cutTris){
            foreach (shatterVert v in tri.verts){
                if (v.consumptionNumber != consumptionNumber){
                    v.consumptionNumber = consumptionNumber;
                    v.meshingNumber = meshingNumber++;
                    cutVertices.Add(v.pos);
                    cutUV.Add(v.uv);
                    newShatter.verts.Add(v);
                }
                cutTriangles.Add(v.meshingNumber);
            }
            newShatter.triangles.Add(tri);
        }
        consumptionNumber++;

        Mesh newSubMesh = new Mesh();
        newSubMesh.vertices = subVertices.ToArray();
        newSubMesh.uv = subUV.ToArray();
        newSubMesh.triangles = subTriangles.ToArray();
        Mesh newCutMesh = new Mesh();
        newCutMesh.vertices = cutVertices.ToArray();
        newCutMesh.uv = cutUV.ToArray();
        newCutMesh.triangles = cutTriangles.ToArray();

        GameObject newSub = Instantiate(substrateTemplate, transform.position, transform.rotation);
        GameObject newCut = Instantiate(cutTemplate, transform.position, transform.rotation);
        newCut.GetComponent<MeshFilter>().mesh = newCutMesh;
        newSub.GetComponent<MeshFilter>().mesh = newSubMesh;
        newSub.GetComponent<shatterObj>().substrate = newShatter;
        newCut.transform.parent = newSub.transform;
    }
}
