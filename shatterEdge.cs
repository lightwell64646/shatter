using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shatterEdge{
    public List<shatterVert> verts;
    public List<shatterTriangle> faces;
    public bool seam = false;

    public shatterEdge(shatterVert v1, shatterVert v2){
        verts = new List<shatterVert>();
        verts.Add(v1);
        verts.Add(v2);
        /*if (v1.generatorEdge != null && v2.generatorEdge != null){
            if (v1.generatorEdge.faces.Contains)
        }*/
    }
}