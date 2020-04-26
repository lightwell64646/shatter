using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shatterMesh{

    public List<shatterVert> verts;
    public List<shatterTriangle> triangles;
    //public bool cutFlat = true;
    private List<shatterVert> newIntersects;

    private float epsilon = 1E-5f;

    public shatterMesh(Mesh m){
        createShatterMesh(m, new Vector3(0,0,0));
    }

    public shatterMesh(Mesh m, Vector3 offset){
        createShatterMesh(m, offset);
    }

    public shatterMesh(){
        verts = new List<shatterVert>();
        triangles = new List<shatterTriangle>();
        newIntersects = new List<shatterVert>();
    }

    public void intersect(shatterMesh other){
        int thisOrigTris = triangles.Count;
        cut(other);
        other.cut(this, thisOrigTris);
        for (int i=thisOrigTris; i<triangles.Count; i++){
            triangles[i].populateAdjacentTunneled(true);
        }
    }

    public void cut(shatterMesh other){
        cut(other, other.triangles.Count);
    }
    public void cut(shatterMesh other, int otherOrigTris){
        newIntersects.Clear();
        int thisOrigTris = triangles.Count;
        for (int i=0; i<otherOrigTris; i++){
            int startStepTris = triangles.Count;
            for (int j=0; j<startStepTris; j++){
                if (!triangles[j].culled){
                    cutTri(triangles[j], other.triangles[i]);
                }
            }
        }
        for (int i=thisOrigTris; i<triangles.Count; i++){
            triangles[i].populateAdjacent();
        }
    }

    private void cutTri(shatterTriangle tri, shatterTriangle other){
        float parallelMetric = Vector3.Dot(tri.norm, other.norm);
        if (parallelMetric > 1-epsilon || parallelMetric < epsilon - 1){
            return; //define parallel triangles as non intersecting
        }

        List<shatterVert> intersects = new List<shatterVert>();
        for (int i=0; i < 3; i++){
            shatterVert intersect = tri.intersect(other.verts[i], other.verts[(i+1)%3]);
            if (intersect != null && getDuplicate(intersect.pos, intersects) == -1){
                if (tri.storedIntersects.Count != 0){
                    int dup = getDuplicate(intersect.pos, tri.storedIntersects);
                    if (dup != -1){
                        intersect = tri.storedIntersects[dup];
                    }
                }
                intersects.Add(intersect);
                other.storedIntersects.Add(other.portIn(intersect));
            }
            shatterVert intersectOther = other.intersect(tri.verts[i], tri.verts[(i+1)%3]);
            if (intersectOther != null && getDuplicate(intersectOther.pos, intersects) == -1){
                shatterVert intersectThis = null;
                if (tri.storedIntersects.Count != 0){
                    int dup = getDuplicate(intersectOther.pos, tri.storedIntersects);
                    if (dup != -1){
                        intersectThis = tri.storedIntersects[dup];
                    }
                }
                if (intersectThis == null){
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
        for (int i=0; i<currentV.Count; i++){
            if ((newV - currentV[i].pos).sqrMagnitude < epsilon){
                return i;
            }
        }
        return -1;
    }

    private void repairTri(shatterTriangle tri, List<shatterVert> intersects){
        int dup, dupCount, origID;
        dupCount = 0;
        origID = 0;
        for (int i=0; i<intersects.Count; i++){
            dup = getDuplicate(intersects[i].pos, tri.verts);
            if (dup != -1){
                intersects[i] = tri.verts[dup];
                dupCount += 1;
            }
            else{
                origID=i;
            }
            
            //this type of merge just combines verticies the triangles
            //still need to be updated so we don't touch dupCount
            dup = getDuplicate(intersects[i].pos, newIntersects);
            if (dup == -1){
                verts.Add(intersects[i]);
                newIntersects.Add(intersects[i]);
            }
            else{
                intersects[i]=newIntersects[dup];
            }
        }
        if (intersects.Count == 2){
            if (!intersects[0].excludeEdges.Contains(intersects[1]))
                intersects[0].excludeEdges.Add(intersects[1]);
            if (!intersects[1].excludeEdges.Contains(intersects[0]))
                intersects[1].excludeEdges.Add(intersects[0]);
            if (dupCount == 0){
                repairTri2(tri, intersects);
            }
            else if (dupCount == 1){
                repairTri1(tri, intersects[origID]);
            }
        }
        else if (intersects.Count == 1){
            if (dupCount == 0){
                repairTri1(tri, intersects[0]);
            }
        }
        else{
            Debug.Log("too many intersections");
            foreach (shatterVert i in intersects){
                Debug.Log(i.pos);
            }
        }
    }

    private void repairTri1(shatterTriangle triangle, shatterVert inter){
        int dup = getDuplicate(inter.pos,verts);
        if (dup == -1){
            verts.Add(inter);
        }
        else{
            inter = verts[dup];
        }
        for (int i=0; i<3; i++){
            if (nonLinear(triangle.verts[i].pos, triangle.verts[(i+1)%3].pos, inter.pos)){
                triangles.Add(new shatterTriangle(triangle.verts[i], triangle.verts[(i+1)%3], inter, triangle.isSubstrate));
            }
        }
        triangle.cull();
    }

    private void repairTri2(shatterTriangle triangle, List<shatterVert> intersects){
        int dup = getDuplicate(intersects[0].pos,verts);
        if (dup == -1){
            verts.Add(intersects[0]);
        }
        else{
            intersects[0] = verts[dup];
        }
        for (int i=0; i<3; i++){
            if (nonLinear(triangle.verts[i].pos, triangle.verts[(i+1)%3].pos, intersects[0].pos)){
                shatterTriangle newTri = new shatterTriangle(triangle.verts[i], triangle.verts[(i+1)%3], intersects[0], triangle.isSubstrate);
                if (newTri.containsPlanar(intersects[1].pos)){
                    repairTri1(newTri, intersects[1]);
                }
                else{
                    triangles.Add(newTri);
                }
            }
        }
        triangle.cull();
    }

    private bool nonLinear(Vector3 p1, Vector3 p2, Vector3 p3){
        float metric = Vector3.Dot(Vector3.Normalize(p2-p1), Vector3.Normalize(p3-p1));
        return metric < 1-epsilon && metric > epsilon - 1;
    }

    private void createShatterMesh(Mesh m, Vector3 offset){
        verts = new List<shatterVert>();
        triangles =  new List<shatterTriangle>();
        newIntersects = new List<shatterVert>();
        for (int i=0; i<m.vertices.Length; i++){
            int dup = getDuplicate(m.vertices[i], verts);
            verts.Add(new shatterVert(m.vertices[i] + offset, m.uv[i]));
            if (dup != -1){
                verts[i].flatShadeWeld = verts[dup];
                verts[dup].flatShadeWeld = verts[i];
            }
        }
        for (int i=0; i<m.triangles.Length; i+=3){
            triangles.Add(new shatterTriangle(verts[m.triangles[i]],
                                              verts[m.triangles[i+1]],
                                              verts[m.triangles[i+2]],
                                              true));
        }
        
        foreach (shatterTriangle t in triangles){
            t.populateAdjacent();
        }
    }
}