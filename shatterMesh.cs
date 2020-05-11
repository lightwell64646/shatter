using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shatterMesh{

    public List<shatterVert> verts;
    public List<shatterTriangle> triangles;
    //public bool cutFlat = true;
    private List<shatterVert> activeCuts;
    public GameObject debugTemplate;

    private float epsilon = 1E-6f;
    private float areaEpsilon = 1E-3f;

    public shatterMesh(Mesh m, Vector3 offset, bool isSubstrate = true, GameObject template = null){
        createShatterMesh(m, offset, isSubstrate);
        debugTemplate = template;
    }

    public shatterMesh(){
        verts = new List<shatterVert>();
        triangles = new List<shatterTriangle>();
        activeCuts = new List<shatterVert>();
    }

    public void intersect(shatterMesh other){
        int thisOrigTris = triangles.Count;
        int otherOrigTris = other.triangles.Count;
        cut(other);
        populateExternal(other, other.triangles.Count);
        Debug.Log("other cuting");
        other.cut(this, thisOrigTris);
        other.cullExternal(this, thisOrigTris);
        for (int i=thisOrigTris; i<triangles.Count; i++){
            triangles[i].populateAdjacentTunneled();
        }
    }

    public void cut(shatterMesh other, int stopCheck = -1){
        activeCuts.Clear();
        if (stopCheck < 0)
            stopCheck = other.triangles.Count;
        for (int i=0; i<stopCheck; i++){
            int startStepTris = triangles.Count;
            for (int j=0; j<startStepTris; j++){
                if (!triangles[j].culled){
                    cutTri(triangles[j], other.triangles[i]);
                }
            }
        }
        for (int i=0; i<triangles.Count; i++){
            triangles[i].populateAdjacent();
        }
        foreach (shatterVert v in activeCuts){
            if (v.excludeEdges.Count != 2){
                Debug.Log(v.excludeEdges.Count);
                Debug.Log(v.pos);
            }
        }
    }

    private void cutTri(shatterTriangle tri, shatterTriangle other){
        float parallelMetric = Vector3.Dot(tri.norm, other.norm);
        if (parallelMetric > 1-epsilon || parallelMetric < epsilon - 1) return;

        List<shatterVert> intersects = new List<shatterVert>();
        for (int i=0; i < 3; i++){
            shatterVert intersect = tri.intersect(other.verts[i], other.verts[(i+1)%3]);
            if (intersect != null && getDuplicate(intersect.pos, intersects) == -1){
                int dup = getDuplicate(intersect.pos, tri.storedIntersects);
                if (dup != -1){
                    tri.storedIntersects[dup].absorb(intersect);
                    intersect = tri.storedIntersects[dup];
                }
                intersects.Add(intersect);
                other.storedIntersects.Add(other.portIn(intersect));
            }
            shatterVert intersectOther = other.intersect(tri.verts[i], tri.verts[(i+1)%3]);
            if (intersectOther != null && getDuplicate(intersectOther.pos, intersects) == -1){
                shatterVert intersectThis = null;
                int dup = getDuplicate(intersectOther.pos, tri.storedIntersects);
                if (dup != -1){
                    tri.storedIntersects[dup].absorb(intersectOther);
                    intersectThis = tri.storedIntersects[dup];
                }
                else{
                    intersectThis = tri.portIn(intersectOther);
                }
                intersects.Add(intersectThis);
                other.storedIntersects.Add(intersectOther); 
            }
        }

        if (intersects.Count != 0){
            repairTri(tri, intersects);
        }
    }

    private int getDuplicate(Vector3 newV, List<shatterVert> currentV){
        float minDist = epsilon;
        int res = -1;
        for (int i=0; i<currentV.Count; i++){
            float met = Vector3.Magnitude(newV - currentV[i].pos);
            if (met < minDist){
                res = i;
                minDist = met;
            }
        }
        return res;
    }

    private void repairTri(shatterTriangle tri, List<shatterVert> intersects){
        if (intersects.Count > 2){
            Debug.Log("too many intersections");
            foreach (shatterVert i in intersects){
                Debug.Log(i.pos);
            }
        }

        int dup, dupCount, origID;
        dupCount = 0;
        origID = 0;
        for (int i=0; i<intersects.Count; i++){
            dup = getDuplicate(intersects[i].pos, tri.verts);
            if (dup != -1){
                tri.verts[dup].absorb(intersects[i]);
                intersects[i]=tri.verts[dup];
                dupCount += 1;
                continue;
            }
            else{
                origID=i;
            }
            
            //this type of merge just combines verticies the triangles
            //still need to be updated so we don't touch dupCount
            dup = getDuplicate(intersects[i].pos, activeCuts);
            if (dup == -1){
                verts.Add(intersects[i]);
                activeCuts.Add(intersects[i]);
            }
            else if (intersects[0] != activeCuts[dup]){
                activeCuts[dup].absorb(intersects[i]);
                intersects[i]=activeCuts[dup];
            }
        }


        int debugCount = triangles.Count;   
        Debug.Log(intersects.Count);     
        if (intersects.Count == 2){
            //Debug.Log(new Vector3(dupCount, intersects[0].excludeEdges.Count, intersects[1].excludeEdges.Count));
            intersects[0].excludeEdges.Add(intersects[1]);
            intersects[1].excludeEdges.Add(intersects[0]);
            if (dupCount == 0){
                repairTri2(tri, intersects);
            }
            else if (dupCount == 1){
                repairTri1(tri, intersects[origID]);
            }
        }
        /*else if (intersects.Count == 1){
            repairTri1(tri, intersects[0]);
        }*/

        if (triangles.Count != debugCount){
            //Debug
            Mesh newSubMesh = new Mesh();
            List<Vector3> vertsDebug = new List<Vector3>();
            List<Vector3> normalsDebug = new List<Vector3>();
            List<Vector2> uvDebug = new List<Vector2>();
            List<int> trianglesDebug = new List<int>();
            int meshNum = 0;
            foreach (shatterTriangle t in triangles){
                if (!t.culled){
                    foreach (shatterVert v in t.verts){
                        vertsDebug.Add(v.pos);
                        normalsDebug.Add(t.norm);
                        trianglesDebug.Add(meshNum);
                        uvDebug.Add(v.uv);
                        meshNum += 1;
                    }
                }
            }
            newSubMesh.vertices = vertsDebug.ToArray();
            newSubMesh.triangles = trianglesDebug.ToArray();
            newSubMesh.normals = normalsDebug.ToArray();
            newSubMesh.uv = uvDebug.ToArray();

            if (debugTemplate != null && debugTemplate.GetComponent<MeshFilter>()!=null){
                debugTemplate.GetComponent<MeshFilter>().mesh = newSubMesh;
                GameObject newSub = GameObject.Instantiate(debugTemplate);
            }
        }
        
    }

    private void repairTri1(shatterTriangle triangle, shatterVert inter){
        for (int i=0; i<3; i++){
            if (nonLinear(triangle.verts[i].pos, triangle.verts[(i+1)%3].pos, inter.pos)){
                shatterTriangle newTri = new shatterTriangle(triangle.verts[i], triangle.verts[(i+1)%3], inter, triangle.isSubstrate);
                newTri.storedIntersects = triangle.storedIntersects;
                triangles.Add(newTri);
            }
        }
        triangle.cull();
    }

    private void repairTri2(shatterTriangle triangle, List<shatterVert> intersects){
        for (int i=0; i<3; i++){
            if (nonLinear(triangle.verts[i].pos, triangle.verts[(i+1)%3].pos, intersects[0].pos)){
                shatterTriangle newTri = new shatterTriangle(triangle.verts[i], triangle.verts[(i+1)%3], intersects[0], triangle.isSubstrate);
                if (newTri.containsPlanar(intersects[1].pos)){
                    repairTri1(newTri, intersects[1]);
                }
                else{
                    newTri.storedIntersects = triangle.storedIntersects;
                    triangles.Add(newTri);
                }
            }
        }
        triangle.cull();
    }

    private bool nonLinear(Vector3 p1, Vector3 p2, Vector3 p3){
        Vector3 v1 = p2-p1;
        Vector3 v2 = p3-p1;
        return Vector3.Magnitude(Vector3.Cross(v1, v2)) > areaEpsilon;
    }

    private void createShatterMesh(Mesh m, Vector3 offset, bool isSubstrate){
        verts = new List<shatterVert>();
        triangles =  new List<shatterTriangle>();
        activeCuts = new List<shatterVert>();
        List<int> vertsUnDupRef = new List<int>();
        for (int i=0; i<m.vertices.Length; i++){
            int dup = getDuplicate(m.vertices[i]+offset, verts);
            if (dup == -1){
                vertsUnDupRef.Add(verts.Count);
                verts.Add(new shatterVert(m.vertices[i] + offset, m.uv[i]));
            }
            else{
                vertsUnDupRef.Add(dup);
            }
        }
        for (int i=0; i<m.triangles.Length; i+=3){
            triangles.Add(new shatterTriangle(verts[vertsUnDupRef[m.triangles[i+0]]],
                                              verts[vertsUnDupRef[m.triangles[i+1]]],
                                              verts[vertsUnDupRef[m.triangles[i+2]]],
                                              isSubstrate));
        }
        
    }



    //externality checks
    public void cullExternal(shatterMesh other, int stopCheck){
        populateExternal(other, stopCheck);
        foreach (shatterTriangle t in triangles){
            if (t.external){
            //if (!t.culled && other.raycastExternalCheck(t,stopCheck)){
                t.cull();
            }
        }
    }

    private void populateExternal(shatterMesh other, int stopCheck){
        foreach (shatterTriangle t in triangles){
            if (!t.culled && !t.isConsumed){
                t.external = other.raycastExternalCheck(t,stopCheck);
                externalFlood(t);
                Debug.Log("tanavast");
            }
        }
        foreach (shatterTriangle t in triangles){
            t.isConsumed = false;
        }
    }
    
    private void externalFlood(shatterTriangle first){
        Queue<shatterTriangle> flood = new Queue<shatterTriangle>();
        flood.Enqueue(first);
        first.isConsumed = true;
        while (flood.Count != 0){
            shatterTriangle currentTri = flood.Dequeue();
            foreach (shatterTriangle a in currentTri.adjacent){
                if (!a.isConsumed && !a.culled){
                    flood.Enqueue(a);
                    a.isConsumed = true;
                    a.external = first.external;
                }
            }
        }
    }

    public bool raycastExternalCheck(shatterTriangle toMeasure, int stopCheck = -1){
        Vector3 tPos = (toMeasure.verts[0].pos + toMeasure.verts[1].pos + toMeasure.verts[2].pos)/3;
        int intersects = 0;
        List<shatterVert> ignores = new List<shatterVert>();
        shatterVert infinity = new shatterVert(new Vector3(99999,99999,99999), new Vector2(0,0));
        shatterVert toMeasureVert = new shatterVert(tPos, new Vector2(0,0));
        if (stopCheck < 0){
            stopCheck = triangles.Count;
        }
        for(int i=0; i<stopCheck; i++){
            shatterTriangle tri = triangles[i];
            shatterVert found = tri.intersect(toMeasureVert, infinity);
            if (found!=null && Vector3.Magnitude(found.pos-tPos) < epsilon){
                return false;
            }
            if (found!=null && Vector3.Dot(found.pos - tPos, new Vector3(1,1,1))>0 && getDuplicate(found.pos, ignores)==-1){
                intersects += 1;
                ignores.Add(found);
                //Debug.Log(found.pos);
            }
        }
        return (intersects%2 == 0);
    }
}