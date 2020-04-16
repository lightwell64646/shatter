using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shatterTriangle{
    public List<shatterVert> verts;
    public List<shatterVert> storedIntersects;
    public List<shatterTriangle> adjacent;
    public bool isConsumed = false;
    public bool isSubstrate;
    public bool culled = false;
    public Vector3 norm;

    private Vector3 right, up;
    private Vector2 p02D;
    private float det;
    private float a,b,c,d;
    
    private float epsilon = 1E-4f;

    public shatterTriangle(shatterVert v1, shatterVert v2, shatterVert v3, bool isSubstrateTri){
        verts = new List<shatterVert>();
        verts.Add(v1);
        verts.Add(v2);
        verts.Add(v3);
        storedIntersects = new List<shatterVert>();
        adjacent = new List<shatterTriangle>();
        isSubstrate = isSubstrateTri;
        populateAdjacent();

        norm = Vector3.Cross(v2.pos - v1.pos, v3.pos - v1.pos);
        norm = Vector3.Normalize(norm);

        up = Vector3.Cross(norm, Vector3.right);
        if (Vector3.Magnitude(up) < epsilon){
            up = Vector3.Cross(norm, Vector3.up);
        }
        up = Vector3.Normalize(up);
        right = Vector3.Cross(up, norm);

        Vector2 ac = collapse(v2.pos - v1.pos);
        Vector2 bd = collapse(v3.pos - v1.pos);
        p02D = collapse(v1.pos);
        a = ac[0];
        b = bd[0];
        c = ac[1];
        d = bd[1];
        det = 1/(a*d-b*c);
    }

    public shatterVert intersect(shatterVert l1V, shatterVert l2V){
        Vector3 l1 = l1V.pos;
        Vector3 l2 = l2V.pos;
        Vector3 lv = l2 - l1;
        Vector3 lo = l1 - verts[0].pos;
        float loDist = Vector3.Dot(lo, norm);
        float lvAlign = Vector3.Dot(lv, norm);
        float travelT = -loDist / lvAlign;
        if (travelT > 1+epsilon || travelT < -epsilon || lvAlign == 0){
            return null;
        }
        
        Vector3 proj = l1 + lv * travelT;
        Vector2 proj2 = collapse(proj) - p02D;
        Vector2 bari = new Vector2(d*proj2[0] - b*proj2[1], a*proj2[1] - c*proj2[0]);
        bari = det*bari;
        float w = 1 - bari[0] - bari[1];
        if (bari[0] < -epsilon || bari[1] < -epsilon || w < -epsilon){
            return null;
        }

        Vector2 UV = verts[2].uv*bari[0] + verts[1].uv*bari[1] + verts[0].uv*w;
        shatterVert res = new shatterVert(proj, UV);
        res.isIntersection = true;
        return res;
    }

    public bool containsPlanar(Vector3 p){
        Vector2 proj2 = collapse(p) - p02D;
        Vector2 bari = new Vector2(d*proj2[0] - b*proj2[1], a*proj2[1] - c*proj2[0]);
        bari = det*bari;
        float w = 1 - bari[0] - bari[1];
        if (bari[0] < -epsilon || bari[1] < -epsilon || w < -epsilon){
            return false;
        }
        return true;
    }

    public shatterVert portIn(shatterVert v){
        Vector2 proj2 = collapse(v.pos);
        Vector2 bari = new Vector2(d*proj2[0] - b*proj2[1], a*proj2[1] - c*proj2[0]);
        bari = det*bari;
        float w = 1 - bari[0] - bari[1];
        Vector2 UV = verts[2].uv*bari[0] + verts[1].uv*bari[1] + verts[0].uv*w;
        shatterVert ported = new shatterVert(v.pos, UV);
        if (isSubstrate){
            ported.intersectionTunnel = v;
        }
        ported.isIntersection = true;
        return ported;
    }

    public void populateAdjacent(){
        foreach (shatterVert v in verts){
            foreach(shatterTriangle t in v.faces){
                if (!adjacent.Contains(t)){
                    adjacent.Add(t);
                    t.adjacent.Add(this);
                }
            }
            v.faces.Add(this);
        }
    }

    public void populateAdjacentTunneled(){
        populateAdjacentTunneled(true);
    }
    public void populateAdjacentTunneled(bool tunnelInternal){
        foreach (shatterVert v in verts){
            if (v.intersectionTunnel != null){
                foreach(shatterTriangle t in v.intersectionTunnel.faces){
                    float tDepth = characteriseTriangleSide(t);
                    if ((tDepth > epsilon && !tunnelInternal) || (tDepth < -epsilon && tunnelInternal)){
                        adjacent.Add(t);
                    }
                }
            }
        }
    }

    public void cull(){
        culled = true;
        foreach (shatterTriangle tri in adjacent){
            tri.adjacent.Remove(this);
        }
        foreach (shatterVert v in verts){
            v.faces.Remove(this);
        }
    }

    private float characteriseTriangleSide(shatterTriangle t){
        foreach (shatterVert v in t.verts){
            if (!v.isIntersection){
                return Vector3.Dot(v.pos - verts[0].pos, norm);
            }
        }
        return 0;
    }

    private Vector2 collapse(Vector3 v3){
        return new Vector2(Vector3.Dot(v3, right), (Vector3.Dot(v3, up)));
    }
}