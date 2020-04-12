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
            shatterVert intersect = tri.intersect(other.verts[i], other.verts[(i+1)%3]);
            if (intersect != null && getDuplicate(intersect.pos, intersects) == -1){
                intersect.isIntersection = true;
                intersects.Add(intersect);
            }
            shatterVert intersect = other.intersect(verts[i], verts[(i+1)%3]);
            if (intersect != null && getDuplicate(intersect.pos, intersects) == -1){
                intersect.isIntersection = true;
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
        int dup, dup2;
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
                if ((dup2 = getDuplicate(intersects[1], verts)) == -1){
                    repairTri1(tri, intersects[1]);
                }
                else{
                    verts[dup2].isIntersection = true;
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
        for (int i=0; i<3; i++){
            if (nonLinear(triangle.verts[i], triangle.verts[(i+1)%3], inter)){
                triangles.Add(new shatterTriangle(triangle.verts[i], triangle.verts[(i+1)%3], inter));
            }
        }
        triangle.culled = true;
    }

    private void repairTri2(shatterTriangle triangle, List<shatterVert> intersects){
        for (int i=0; i<3; i++){
            if (nonLinear(triangle.verts[i].pos, triangle.verts[(i+1)%3].pos, intersects[0].pos)){
                shatterTriangle newTri = new shatterTriangle(triangle.verts[i], triangle.verts[(i+1)%3], intersects[0]);
                if (newTri.containsPlanar(intersects[1].pos)){
                    repairTri1(newTri);
                }
                else{
                    triangles.Add(newTri);
                }
            }
        }
        triangle.culled = true;
    }

    private bool nonLinear(Vector3 p1, Vector3 p2, Vector3 p3){
        float metric = Vector3.Dot(Vector3.Normalize(p2-p1), Vector3.Normalize(p3-p1));
        return metric < 1-epsilon && metric > epsilon - 1;
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