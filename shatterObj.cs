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
        foreach (shatterTriangle ct in newCutTris){
            ct.isConsumed = false;
        }

        generateFragment(newSubstrateTris, newCutTris);
    }

    private void generateFragment(List<shatterTriangle> subTris, List<shatterTriangle> cutTris){
        List<Vector3> Vertices = new List<Vector3>();
        List<Vector2> UV = new List<Vector2>();
        List<Vector3> Normals = new List<Vector3>();
        List<int> Triangles = new List<int>();
        shatterMesh newShatter = new shatterMesh();
        int meshingNumber = 0;

        foreach (shatterTriangle tri in subTris){
            foreach (shatterVert v in tri.verts){
                if (v.consumptionNumber != consumptionNumber){
                    v.consumptionNumber = consumptionNumber;
                    v.meshingNumber = meshingNumber++;
                    Vertices.Add(v.pos);
                    UV.Add(v.uv);
                    Normals.Add(v.getNorm());
                    newShatter.verts.Add(v);
                }
                Triangles.Add(v.meshingNumber);
            }
            newShatter.triangles.Add(tri);
        }
        
        foreach (shatterTriangle tri in cutTris){
            foreach (shatterVert v in tri.verts){
                if (v.consumptionNumber != consumptionNumber){
                    v.consumptionNumber = consumptionNumber;
                    v.meshingNumber = meshingNumber++;
                    Vertices.Add(v.pos);
                    UV.Add(v.uv);
                    Normals.Add(v.getNorm());
                    newShatter.verts.Add(v);
                }
                Triangles.Add(v.meshingNumber);
            }

            //add the reverse triangles for good measure
            Triangles.Add(tri.verts[0].meshingNumber);
            Triangles.Add(tri.verts[2].meshingNumber);
            Triangles.Add(tri.verts[1].meshingNumber);
            newShatter.triangles.Add(new shatterTriangle(tri.verts[0], tri.verts[1], tri.verts[2], true));
            newShatter.triangles.Add(new shatterTriangle(tri.verts[0], tri.verts[2], tri.verts[1], true));
        }
        consumptionNumber++;

        Mesh newSubMesh = new Mesh();
        newSubMesh.vertices = Vertices.ToArray();
        newSubMesh.uv = UV.ToArray();
        newSubMesh.triangles = Triangles.ToArray();
        newSubMesh.normals = Normals.ToArray();

        GameObject newSub = Instantiate(substrateTemplate, transform.position, transform.rotation);
        newSub.GetComponent<MeshFilter>().mesh = newSubMesh;
        newSub.GetComponent<shatterObj>().substrate = newShatter;
    }
}
