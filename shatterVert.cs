using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shatterVert{
    public Vector3 pos;
    public Vector2 uv;
    public bool isIntersection = false;
    public shatterVert intersectionTunnel = null;
    public List<shatterTriangle> faces;
    public int meshingNumber = -1;
    public int consumptionNumber = -1;

    public shatterVert(Vector3 P, Vector2 U){
        pos = P;
        uv = U;
        faces = new List<shatterTriangle>();
    }
    public shatterVert(shatterVert other){
        pos = other.pos;
        uv = other.uv;
        isIntersection = other.isIntersection;
        faces = new List<shatterTriangle>();
    }
}