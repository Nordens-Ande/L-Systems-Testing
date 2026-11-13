using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;

[Serializable]
public class Branch
{
    public int splineIndex;

    public int parentSplineIndex = -1;
    public int parentKnotIndex = -1;

    public float localLength = 0f;
    public float totalLength = 0f;
}

public class PlantGenerator : MonoBehaviour
{
    private string plant;
    private GameObject plantObject;

    [SerializeField] private int seed = -1;

    [SerializeField] private string axiom;

    [SerializeField] private int iterations;

    [SerializeField] private float stepLength;
    [SerializeField] private float stepAngle;

    [SerializeField] private float baseRadius = 0.3f;
    [SerializeField] private int radialSegments = 8;
    [SerializeField] private int splineResolution = 20;
    [SerializeField] private AnimationCurve radiusCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [SerializeField] Material plantMaterial;

    private List<Branch> branches = new List<Branch>(); 
    private Stack<GrammarNode> stack = new Stack<GrammarNode>();
    private Stack<int> splineIndexStack = new Stack<int>();

    private GrammarNode currentNode;
    private Spline currentSpline;
    private int currentSplineIndex;

    private List<List<Vector3>> branchList = new List<List<Vector3>>();


    private SplineContainer container;


    void Start()
    {   
        currentNode = new GrammarNode { position = transform.position, rotation = transform.rotation };

        plant = axiom;

        ExpandPlantString();

        GenerateObject();

        CreateBranches();

        plantObject.GetComponent<PlantExtruder>().GenerateMesh();

        //GenerateMesh();
    }

    void Update()
    {
        
    }

    private void ExpandPlantString()
    {
        for (int i = 0; i < iterations; i++)
        {
            // or newPlant since it will be an addition of the old plant and the production
            StringBuilder production = new StringBuilder();

            foreach (char predecessor in plant.ToString())
            {
                // creates the production by evaluating the predecessor and then adding the proper succesor
                production.Append(predecessor switch
                {
                    'F' => "FF",
                    //'B' => "[lFB][rFB]",
                    'B' => "[lFB][rFB]",
                    _ => predecessor
                });
            }

            plant = production.ToString();
            Debug.Log($"Plant at iteration: {plant}");
        }
    }


    private void GenerateObject()
    {
        // Replace SplineExtrude with custom
        plantObject = new GameObject("Plant", typeof(MeshFilter), typeof(MeshRenderer), typeof(SplineContainer), typeof(PlantExtruder)/*, typeof(SplineExtrude)*/);

        //plantObject.GetComponent<MeshFilter>().sharedMesh = new Mesh();
        plantObject.GetComponent<MeshRenderer>().material = plantMaterial;

        container = plantObject.GetComponent<SplineContainer>();
        container.RemoveSplineAt(0);

        //plantObject.GetComponent<SplineExtrude>().Container = container;
        //PlantExtruder extruder = plantObject.GetComponent<PlantExtruder>();
        plantObject.GetComponent<PlantExtruder>().container = container;
        plantObject.GetComponent<PlantExtruder>().branches = branches;


        currentSpline = container.AddSpline();
        currentSplineIndex = container.Splines.Count - 1;
        currentSpline.Add(new BezierKnot(currentNode.position), TangentMode.AutoSmooth);

        branches.Clear();

        branches.Add(new Branch
        {
            splineIndex = 0,
            parentSplineIndex = -1,
            parentKnotIndex = -1,
        });
    }

    private void CreateBranches()
    {
        Vector3 initialPosition;
        foreach (char c in plant)
        {
            Action productionRule = c switch
            {
                'F' => MoveForward,
                'r' => RotateRight,
                'l' => RotateLeft,
                '[' => PushNode,
                ']' => PopNode,
                _ => () => { }
            };

            productionRule();
        }
    }

    private void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Plant Mesh";

        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector2> uvs = new();

        // Iterate over all splines in the container.
        for (int splineIndex = 0; splineIndex < container.Splines.Count; splineIndex++)
        {
            Spline spline = container[splineIndex];

            // Sample spline and generate rings.
            // Connect adjacent rings with triangles.

            float t = splineIndex / container.Splines.Count;

            //float3 tangentFloat3 = spline.EvaluateTangent(t);
            Vector3 tangent = spline.EvaluateTangent(t);

            Vector3 reference = Vector3.up;

            if (Mathf.Abs(Vector3.Dot(tangent, reference)) > 0.99f)
                reference = Vector3.right;

            Vector3 normal = Vector3.Cross(tangent, reference).normalized;
            Vector3 binormal = Vector3.Cross(tangent, normal).normalized;

        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        plantObject.GetComponent<MeshFilter>().sharedMesh = mesh;
    }


    private void PushNode()
    {
        stack.Push(new GrammarNode
        {
            position = currentNode.position,
            rotation = currentNode.rotation
        });


        splineIndexStack.Push(currentSplineIndex);

        // Parent information, however, this is the same ad the prevIndices so we wont use the parent variables
        //int parentSplineIndex = currentSplineIndex;
        //int parentKnotIndex = container[parentSplineIndex].Count - 1;

        //int splineCount = currentSpline.Count;
        int prevSplineIndex = currentSplineIndex;
        int prevLastKnotIndex = container[prevSplineIndex].Count - 1;

        currentSpline = container.AddSpline();
        currentSplineIndex = container.Splines.Count - 1;

        Vector3 localPosition = container.transform.InverseTransformPoint(currentNode.position);

        currentSpline.Add(new BezierKnot(localPosition), TangentMode.AutoSmooth);
        container.LinkKnots(new SplineKnotIndex(prevSplineIndex, prevLastKnotIndex), new SplineKnotIndex(currentSplineIndex, 0));

        branches.Add(new Branch
        {
            splineIndex = currentSplineIndex,
            parentSplineIndex = prevSplineIndex,
            parentKnotIndex = prevLastKnotIndex,
        });
    }

    private void PopNode()
    {
        currentNode = stack.Pop();


        currentSplineIndex = splineIndexStack.Pop();
        currentSpline = container[currentSplineIndex];
    }

    private void MoveForward()
    {
        Vector3 newPosition = currentNode.position + currentNode.rotation * Vector3.up * stepLength;

        branchList.Add(new List<Vector3>() { currentNode.position, newPosition });
        //branchList[^1].Add(newPosition);

        Vector3 localPosition = container.transform.InverseTransformPoint(newPosition);
        currentSpline.Add(new BezierKnot(newPosition), TangentMode.AutoSmooth);

        currentNode.position = newPosition;
    }

    private void RotateRight()
    {
        currentNode.rotation *= Quaternion.Euler(0, 0, -stepAngle);
    }

    private void RotateLeft()
    {
        currentNode.rotation *= Quaternion.Euler(0, 0, stepAngle);
    }

    float GetRadius(float t)
    {
        return baseRadius * radiusCurve.Evaluate(t);
    }


    private void OnDrawGizmos()
    {
        foreach (List<Vector3> branch in branchList)
        {
            Gizmos.DrawLine(branch[0], branch[1]);
        }
    }
}

public static class PlantGeneratorExtension
{
    //public static int FindIndex(this IReadOnlyList<Spline> splines, Spline spline)
    //{
    //    for (int i = 0; i < splines.Count; i++)
    //    {
    //        if (splines[i] == spline)
    //            return i;
    //    }

    //    return -1;
    //}
}
