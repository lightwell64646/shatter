using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shatterObj : MonoBehaviour{
    public shatterMesh substrate = null;
    public GameObject substrateTemplate;

    private int consumptionNumber = 0;

    void Start(){
        if (substrate == null){
            substrate = new shatterMesh(GetComponent<MeshFilter>().mesh, new Vector3(0,0,0), true, substrateTemplate);
        }
    }

    public void shatter(List<shatterMesh> cells){
        shatterMesh newSub = null;
        foreach (shatterMesh c in cells){
            int otherOrigTris = c.triangles.Count;
            substrate.intersect(c);
            foreach (shatterTriangle t in substrate.triangles){
                if (!t.isConsumed && !t.culled){
                    shatterMesh fgf_res = floodGenerateFragment(t, c, otherOrigTris);
                    if (fgf_res != null){
                        newSub = fgf_res;
                    }
                }
            }

            //FOR DEBUG ONLY
            /*foreach (shatterTriangle t in c.triangles){
                if (!t.isConsumed && !t.culled){
                    DEBUGcut(t);
                }
            }*/
        }

        Destroy(gameObject);
    }

    private shatterMesh floodGenerateFragment(shatterTriangle first, shatterMesh other, int otherOrigTris){
        List<shatterTriangle> newSubstrateTris = new List<shatterTriangle>();
        List<shatterTriangle> newCutTris = new List<shatterTriangle>();
        Queue<shatterTriangle> flood = new Queue<shatterTriangle>();
        flood.Enqueue(first);
        first.isConsumed = true;
        bool substrateExternType = first.external;
        while (flood.Count != 0){
            shatterTriangle currentTri = flood.Dequeue();
            foreach (shatterTriangle a in currentTri.adjacent){
                if (!a.isConsumed && !a.culled){
                    if (!a.isSubstrate || (a.external == substrateExternType)){
                        //when leaving cut mesh go into the substrate type you started at (otherwise shouldn't matter)
                        flood.Enqueue(a);
                        a.isConsumed = true;
                    }
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

        shatterMesh newshatter = generateFragment(newSubstrateTris, newCutTris, substrateExternType);
        Debug.Log(new Vector2(newSubstrateTris.Count, newCutTris.Count));
        Debug.Log(substrateExternType);
        if (substrateExternType)
            return null;
        return newshatter;
    }

    private shatterMesh generateFragment(List<shatterTriangle> subTris, List<shatterTriangle> cutTris, bool isFragment){
        List<Vector3> Vertices = new List<Vector3>();
        List<Vector2> UV = new List<Vector2>();
        List<Vector3> Normals = new List<Vector3>();
        List<int> Triangles = new List<int>();
        int meshingNumber = 0;

        foreach (shatterTriangle tri in subTris){
            foreach (shatterVert v in tri.verts){
                v.meshingNumber = meshingNumber++;
                Vertices.Add(v.pos);
                UV.Add(v.uv);
                Normals.Add(tri.norm);
                Triangles.Add(v.meshingNumber);
            }
        }

        foreach (shatterTriangle tri in cutTris){
            foreach (shatterVert v in tri.verts){
                v.meshingNumber = meshingNumber++;
                Vertices.Add(v.pos);
                UV.Add(v.uv);
                Normals.Add(tri.norm);
                Triangles.Add(v.meshingNumber);
            }
            if (!isFragment){
                int tv2 = Triangles[Triangles.Count - 1];
                Triangles[Triangles.Count - 1] = Triangles[Triangles.Count - 2];
                Triangles[Triangles.Count - 2] = tv2;
            }
        }
        consumptionNumber++;

        Mesh newSubMesh = new Mesh();
        newSubMesh.vertices = Vertices.ToArray();
        newSubMesh.uv = UV.ToArray();
        newSubMesh.triangles = Triangles.ToArray();
        newSubMesh.normals = Normals.ToArray();

        substrateTemplate.GetComponent<MeshFilter>().mesh = newSubMesh;
        GameObject newSub = Instantiate(substrateTemplate, transform.position, transform.rotation);
        return newSub.GetComponent<shatterObj>().substrate;
    }
    private void DEBUGcut(shatterTriangle first){
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
            newCutTris.Add(currentTri);
        }

        generateFragment(new List<shatterTriangle>(), newCutTris, false);
    }
}
