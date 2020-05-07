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
    public float area;
    public bool external;

    private Vector3 right, up;
    private Vector2 p02D;
    private float det;
    private float a,b,c,d;
    
    private float epsilon = 1E-12f;
    private List<shatterTriangle> seenOnce;
    private List<shatterVert> seenOnceVerts;

    public shatterTriangle(shatterVert v1, shatterVert v2, shatterVert v3, bool isSubstrateTri){
        verts = new List<shatterVert>();
        verts.Add(v1);
        verts.Add(v2);
        verts.Add(v3);
        storedIntersects = new List<shatterVert>();
        adjacent = new List<shatterTriangle>();
        isSubstrate = isSubstrateTri;

        norm = Vector3.Cross(v2.pos - v1.pos, v3.pos - v1.pos);
        norm = Vector3.Normalize(norm);

        up = Vector3.Cross(norm, Vector3.right);
        if (Vector3.Magnitude(up) < 1E-4f){
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
        area = getArea();
    }
    public float getArea(){
        return Vector3.Magnitude(Vector3.Cross(verts[1].pos - verts[0].pos, verts[2].pos - verts[0].pos));
    }

    public shatterVert intersect(shatterVert l1V, shatterVert l2V){
        Vector3 l1 = l1V.pos;
        Vector3 l2 = l2V.pos;
        Vector3 lv = l2 - l1;
        Vector3 lo = l1 - verts[0].pos;
        float loDist = Vector3.Dot(lo, norm);
        float lvAlign = Vector3.Dot(lv, norm);
        float travelT = -loDist / lvAlign;
        float parallelMetric = Vector3.Dot(Vector3.Normalize(lo), norm);
        if (travelT > 1 || travelT < 0 || Mathf.Abs(lvAlign) < 1E-4f){
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
        res.projenitorEdges.Add(l1V);
        res.projenitorEdges.Add(l2V);
        return res;
    }

    public bool checkIntersect(Vector3 l1, Vector3 l2){
        Vector3 lv = l2 - l1;
        Vector3 lo = l1 - verts[0].pos;
        float loDist = Vector3.Dot(lo, norm);
        float lvAlign = Vector3.Dot(lv, norm);
        float travelT = -loDist / lvAlign;
        if (travelT > 1 || travelT < 0 || Mathf.Abs(lvAlign) < 1E-4f){
            return false;
        }
        
        Vector3 proj = l1 + lv * travelT;
        Vector2 proj2 = collapse(proj) - p02D;
        Vector2 bari = new Vector2(d*proj2[0] - b*proj2[1], a*proj2[1] - c*proj2[0]);
        bari = det*bari;
        float w = 1 - bari[0] - bari[1];
        if (bari[0] < -epsilon || bari[1] < -epsilon || w < -epsilon){
            return false;
        }
        return true;
    }

    public bool containsPlanar(Vector3 p){
        Vector2 proj2 = collapse(p) - p02D;
        Vector2 bari = new Vector2(d*proj2[0] - b*proj2[1], a*proj2[1] - c*proj2[0]);
        bari = det*bari;
        float w = 1 - bari[0] - bari[1];
        if (bari[0] < -1E-4f || bari[1] < -1E-4f || w < -1E-4f){
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
        ported.intersectTunnel = v;
        v.intersectTunnel = ported;
        ported.intersectTunnel = v;
        ported.projenitorEdges = v.projenitorEdges;
        return ported;
    }

    public void populateAdjacent(){
        seenOnce = new List<shatterTriangle>();
        seenOnceVerts = new List<shatterVert>();
        foreach (shatterVert v in verts){
            foreach(shatterTriangle t in v.faces){
                considerConnection(t, v);
            }
            v.faces.Add(this);
        }
    }

    private void considerConnection(shatterTriangle t, shatterVert v){
        if (!adjacent.Contains(t)){
            int seenIndex = seenOnce.IndexOf(t);
            if (seenIndex != -1){
                if (!(seenOnceVerts[seenIndex].excludeEdges.Contains(v) || v.excludeEdges.Contains(seenOnceVerts[seenIndex]))){
                    adjacent.Add(t);
                    t.adjacent.Add(this);
                }
            }
            else{
                seenOnce.Add(t);
                seenOnceVerts.Add(v);
            }
        }
    }

    public void populateAdjacentTunneled(bool intersectTunnelInternal = true){
        foreach (shatterVert v in verts){
            if (v.intersectTunnel != null){
                foreach(shatterTriangle t in v.intersectTunnel.faces){
                    considerConnectionTunnel(t);
                }
            }
        }
    }
    
    private void considerConnectionTunnel(shatterTriangle t){
        if (adjacent.Contains(t)) return;
        adjacent.Add(t);
        t.adjacent.Add(this);
    }

    public float distance(Vector3 p){
        return Vector3.Dot(p - verts[0].pos, norm);
    }

    public void cull(){
        culled = true;
        foreach (shatterTriangle tri in adjacent){
            tri.adjacent.Remove(this);
        }
        adjacent.Clear();
        foreach (shatterVert v in verts){
            v.faces.Remove(this);
        }
    }

    private Vector2 collapse(Vector3 v3){
        return new Vector2(Vector3.Dot(v3, right), (Vector3.Dot(v3, up)));
    }
}