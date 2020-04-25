using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shatterVert{
    public Vector3 pos;
    public Vector2 uv;
    public shatterVert intersectTunnel = null;
    public shatterVert flatShadeWeld = null;
    public List<shatterTriangle> faces;
    public List<shatterVert> excludeEdges;
    public int meshingNumber = -1;
    public int consumptionNumber = -1;

    public shatterVert(Vector3 P, Vector2 U){
        pos = P;
        uv = U;
        faces = new List<shatterTriangle>();
        excludeEdges = new List<shatterVert>();
    }
    public shatterVert(shatterVert other){
        pos = other.pos;
        uv = other.uv;
        faces = new List<shatterTriangle>();
        excludeEdges = new List<shatterVert>();
    }
}