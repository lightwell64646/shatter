using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shatterMesh{

    public List<shatterVert> verts;
    public List<shatterTriangle> triangles;

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
    }

    public void intersect(shatterMesh other){
        int thisOrigTris = triangles.Count;
        cut(other);
        other.cut(this, thisOrigTris);
        foreach (shatterTriangle tri in triangles){
            tri.populateAdjacentTunneled();
        }
    }

    public void cut(shatterMesh other){
        cut(other, other.triangles.Count);
    }
    public void cut(shatterMesh other, int otherOrigTris){
        for (int i=0; i<otherOrigTris; i++){
            int startStepTris = triangles.Count;
            for (int j=0; j<startStepTris; j++){
                if (!triangles[j].culled){
                    cutTri(triangles[j], other.triangles[i]);
                }
            }
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
            }
            shatterVert intersectOther = other.intersect(tri.verts[i], tri.verts[(i+1)%3]);
            if (intersectOther != null && getDuplicate(intersectOther.pos, intersects) == -1){
                intersects.Add(tri.portIn(intersectOther));
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
        int dup;
        if (intersects.Count == 2){
            if ((dup = getDuplicate(intersects[0].pos, tri.verts)) == -1){
                if ((dup = getDuplicate(intersects[1].pos, tri.verts)) == -1){
                    repairTri2(tri, intersects);
                }
                else{
                    tri.verts[dup].isIntersection = true;
                    repairTri1(tri, intersects[0]);
                }
            }
            else{
                tri.verts[dup].isIntersection = true;
                if ((dup = getDuplicate(intersects[1].pos, tri.verts)) == -1){
                    repairTri1(tri, intersects[1]);
                }
                else{
                    tri.verts[dup].isIntersection = true;
                }
            }
        }
        else if (intersects.Count == 1){
            if ((dup = getDuplicate(intersects[0].pos, tri.verts)) == -1){
                repairTri1(tri, intersects[0]);
            }
            else{
                tri.verts[dup].isIntersection = true;
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
        for (int i=0; i<m.vertices.Length; i++){
            verts.Add(new shatterVert(m.vertices[i] + offset, m.uv[i]));
        }
        for (int i=0; i<m.triangles.Length; i+=3){
            triangles.Add(new shatterTriangle(verts[m.triangles[i]],
                                              verts[m.triangles[i+1]],
                                              verts[m.triangles[i+2]],
                                              true));
        }
    }
}