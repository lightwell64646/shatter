public class shatterTriangle{
    public List<shatterVert> verts;
    public bool isConsumed = false;

    private Vector3 norm, right, up;
    private float det;
    private float a,b,c,d;
    
    private float epsilon = 1E-4;

    public shatterTriangle(shatterVert v1, shatterVert v2, shatterVert v3){
        verts = new List<shatterVert>();
        verts.Add(v1);
        verts.Add(v2);
        verts.Add(v3);

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
        a = ac[0];
        c = ac[1];
        b = bd[0];
        d = bd[1];
        det = 1/(a*d-b*c);
    }

    public shatterVert intersect(Vector3 l1, Vector3 l2){
        Vector3 lv = l2 - l1;
        Vector3 lo = l1 - verts[0].pos;
        float travelT = -Vector3.Dot(lo, norm) / Vector3.Dot(lv, norm);
        if (travelT > 1 || travelT < 0)
            return null;
        
        Vector3 proj = l1 + lv * travelT;
        Vector2 proj2 = collapse(proj);
        Vector2 bari = new Vector2(d*proj2[0] - b*proj2[1], a*proj2[1] - c*proj2[0]);
        bari = det*bari;
        float w = 1 - bari[0] - bari[1];
        if (bari[0] < 0 || bari[1] < 0 || w < 0)
            return null;

        Vector2 UV = self.verts[2].uv*bari[0] + self.verts[1].uv*bari[1] + self.verts[0].uv*w;
        shatterVert res = new shatterVert(proj, UV);
        res.isIntersection = true;
        return res;
    }

    public shatterVert portIn(shatterVert v){
        Vector2 proj2 = collapse(v.pos);
        Vector2 bari = new Vector2(d*proj2[0] - b*proj2[1], a*proj2[1] - c*proj2[0]);
        bari = det*bari;
        float w = 1 - bari[0] - bari[1];
        Vector2 UV = self.verts[2].uv*bari[0] + self.verts[1].uv*bari[1] + self.verts[0].uv*w;
        return new shatterVert(v.pos, UV);
    }

    private collapse(Vector3 v3){
        return new Vector2(Vector3.Dot(v3, right), (Vector3.Dot(v3, up));
    }


    
}