using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shatterObj : MonoBehaviour{
    shatterMesh substrate = null;

    void Start(){
        if (substrate == null)
            substrate = new shatterMesh(GetComponent<MeshFilter>.mesh);
    }

    public void shatter(List<shatterMesh> cells){
        foreach (shatterMesh c in cells){
            substrate.intersect(c);
        }

        foreach (shatterMesh c in cells){
            foreach (shatterTriangle t in c.triangles){
                t.populateAdjacent();
            }
        }
        foreach (shatterTriangle t in substrate.triangles){
            t.populateAdjacent();
        }

        foreach (shatterTriangle t in substrate.triangles){
            if (!t.isConsumed){
                floodGenerateFragment(t);
            }
        }
        destroy(gameObject);
    }

    private void floodGenerateFragment(shatterTriangle first){
        shatterMesh newMesh = new shatterMesh();
        Queue<shatterTriangle> flood = new Queue<shatterTriangle>();
        flood.Enqueue(first);
        while (flood.Count != 0){
            shatterTriangle currentTri = flood.Dequeue();
            foreach (shatterTriangle a in currentTri.adjacent){
                if (!a.isConsumed){
                    flood.Enqueue(a);
                }
            }

            currentTri.isConsumed = true;
        }
        generateFragment(newMesh);
    }

    private void generateFragment(shatterMesh m){
        List<Vector3> meshVertices;
        List<Vector2> meshUV;
        List<int> meshTriangles;
        
        Mesh newMesh = new Mesh();
        newMesh.vertices = meshVertices.ToArray();
        newMesh.uv = meshUV.ToArray();
        newMesh.triangles = meshTriangles.ToArray();
    }

}





