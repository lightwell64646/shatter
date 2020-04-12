public class shatterVert{
    public Vector3 pos;
    public Vector2 uv;
    public bool isIntersection = false;
    public int meshingNumber = -1;

    public shatterVert(Vector3 P, Vector2 U){
        pos = P;
        uv = U;
    }
    public shatterVert(shatterVert other){
        pos = other.pos;
        uv = other.uv;
        isIntersection = other.isIntersection;
    }
}