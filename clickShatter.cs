using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class clickShatter : MonoBehaviour
{
    public GameObject shatterPrefab;
    private List<shatterMesh> cells;
    public bool addVisual = true;
    void Start(){
        cells = new List<shatterMesh>();
    }

    void Update(){
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                hit.collider.GetComponent<Renderer>().material.color = Color.red;
                shatter oShatter = hit.collider.GetComponent<shatterObj>();
                if (oShatter != null){
                    preparePatern(hit.collider.transform.InverseTransformPoint(hit.point));
                    oShatter.shatter(cells);
                    if (addVisual)
                        Instantiate(shatterPrefab, hit.point, Quaternion.identity);
                }
            }
        }
    }
    
    private void preparePatern(Vector3 offset){
        foreach (Transform child in shatterPrefab.transform)
        {
            cells.Add(new shatterMesh(child.GetComponent<MeshFilter>(), offset + child.transform.position));
        }
    }
}