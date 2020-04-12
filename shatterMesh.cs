public class shatterMesh{

    public List<shatterVert> verts;
    public List<shatterTriangle> triangles;

    private float epsilon = 1E-5;

    public shatterMesh(Mesh m){
        createShatterMesh(m, new Vector3(0,0,0));
    }

    public shatterMesh(Mesh m, Vector3 offset){
        createShatterMesh(m, offset);
    }

    public void intersect(shatterMesh other){
        int otherOrigTris = other.triangles.Count;
        int thisOrigTris = triangles.Count;
        cut(other, otherOrigTris);
        other.cut(this, thisOrigTris);
    }

    public void cut(shatterMesh other, int otherOrig){
        for (int i=0; i<otherOrigTris; i++){
            int startStepTris = triangles.Count;
            for (int j=0; j<thisStepTris; j++){
                if (!triangles[j].culled){
                    cutTri(triangles[j], other.triangles[i]);
                }
            }
        }
    }

    private void cutTri(shatterTriangle tri, shatterTriangle other){
        List<shatterVert> intersects = new List<shatterVert>();
        for (int i=0; i < 3; i++){
            shatterVert intersect = tri.intersect(other.verts[i].pos, other.verts[(i+1)%3].pos);
            if (intersect != null && getDuplicate(intersect.pos, intersects) == -1){
                intersects.Add(intersect);
            }
            shatterVert intersect = other.intersect(verts[i].pos, verts[(i+1)%3].pos);
            if (intersect != null && getDuplicate(intersect.pos, intersects) == -1){
                intersects.Add(tri.portIn(intersect));
            }
        }

        if (intersects.Count != 0)
            repairTri(tri, intersects);
    }

    private int getDuplicate(Vector3 newV, List<shatterVert> currentV){
        for (int i=0; i<currentV.Count; i++)
            if ((newV - currentV[i].pos).sqrMagnitude < epsilon)
                return i;
        return -1;
    }

    private void repairTri(shatterTriangle tri, List<shatterVert> intersects){
        int dup;
        if (intersects.Count == 2){
            if ((dup = getDuplicate(intersects[0], verts)) == -1){
                if ((dup = getDuplicate(intersects[1], verts)) == -1){
                    repairTri2(tri, intersects);
                }
                else{
                    verts[dup].isIntersection = true;
                    repairTri1(tri, intersects[0]);
                }
            }
            else{
                verts[dup].isIntersection = true;
                if ((dup = getDuplicate(intersects[1], verts)) == -1){
                    repairTri1(tri, intersects[1]);
                }
            }
        }
        else if (intersects.Count == 1){
            if ((dup = getDuplicate(intersects[0], verts)) == -1){
                repairTri1(tri, intersects[0]);
            }
            else{
                verts[dup].isIntersection = true;
            }
        }
        else{
            Debug.Log(intersects.Count);
        }
    }

    private void repairTri1(shatterTriangle triangle, shatterVert inter){
        
    }

    private void repairTri2(shatterTriangle triangle, List<shatterVert> intersects){
        
    }

    private createShatterMesh(Mesh m, Vector3 offset){
        verts = new List<shatterVert>();
        triangles =  new List<shatterTriangle>();
        for (int i=0; i<m.verticies.Count; i++){
            verts.Add(new shatterVert(m.verticies[i] + offset, m.uv[i]));
        }
        for (int i=0; i<m.triangles.Count; i+=3){
            triangles.Add(new shatterTriangle(verts[m.triangles[i]],
                                              verts[m.triangles[i+1]],
                                              verts[m.triangles[i+2]]));
        }
    }
}