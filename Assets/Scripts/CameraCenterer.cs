using System.Collections;
using UnityEngine;

public class CameraCenterer : MonoBehaviour
{
    [SerializeField] private Camera camera;
    [SerializeField] private GameObject plant;
    
    void Start()
    {
        //camera = GetComponent<Camera>();
        //plant = GameObject.Find("Plant");

        //FitCamera();

        StartCoroutine(LateStart());
    }

    public IEnumerator LateStart()
    {
        yield return null;

        camera = GetComponent<Camera>();
        plant = GameObject.Find("Plant");

        FitCamera();
    }

    //[SerializeField] private Camera targetCamera;
    //[SerializeField] private Renderer targetObject;
    [SerializeField] private float padding = 1.2f;
    [SerializeField] private float degreeSpeed = 30f;

    private Vector3 center = Vector3.zero;

    public void FitCamera()
    {
        if (camera == null || plant == null)
            return;

        Bounds bounds = plant.GetComponent<MeshFilter>().sharedMesh.bounds;

        center = bounds.center;
        Vector3 size = bounds.size;

        // Calculate required camera distance
        float verticalFov = camera.fieldOfView * Mathf.Deg2Rad;
        float horizontalFov = 2f * Mathf.Atan(Mathf.Tan(verticalFov / 2f) * camera.aspect);

        float distanceHeight = (size.y / 2f) / Mathf.Tan(verticalFov / 2f);

        float distanceWidth = (size.x / 2f) / Mathf.Tan(horizontalFov / 2f);

        float distance = Mathf.Max(distanceHeight, distanceWidth);
        distance *= padding;

        // Position camera along its forward direction
        Vector3 direction = camera.transform.forward;

        camera.transform.position = center - direction * distance;

        // Look at object center
        camera.transform.LookAt(center);
    }

    private void Update()
    {
        if (center == Vector3.zero) 
            return;

        transform.RotateAround(center, Vector3.up, degreeSpeed * Time.deltaTime);
    }
}
