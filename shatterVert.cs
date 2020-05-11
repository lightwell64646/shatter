using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shatterVert{
    public Vector3 pos;
    public Vector2 uv;
    public shatterVert intersectTunnel = null;
    public List<shatterTriangle> faces;
    public List<shatterVert> excludeEdges;
    public List<shatterVert> projenitorEdges;
    public int meshingNumber = -1;
    public int consumptionNumber = -1;

    public shatterVert(Vector3 P, Vector2 U){
        pos = P;
        uv = U;
        faces = new List<shatterTriangle>();
        excludeEdges = new List<shatterVert>();
        projenitorEdges = new List<shatterVert>();
    }
    public shatterVert(shatterVert other){
        pos = other.pos;
        uv = other.uv;
        faces = new List<shatterTriangle>();
        excludeEdges = new List<shatterVert>();
        projenitorEdges = new List<shatterVert>();
    }

    public void absorb(shatterVert other){
        /*if (other.excludeEdges.Count != 0){
            Debug.Log(other.excludeEdges.Count);
            Debug.Log(other == this);
        }*/
        pos = (pos + other.pos) / 2.0f;
        if (other.intersectTunnel != null && intersectTunnel == null){
            intersectTunnel = other.intersectTunnel;
            intersectTunnel.intersectTunnel = this;
        }
    }

    public Vector3 getNorm(){
        Vector3 norm = Vector3.zero;
        float totalArea = 0;
        foreach (shatterTriangle f in faces){
            float area = f.area;
            norm += f.norm * area;
            totalArea += area;
        }
        return norm/totalArea;
    }
}