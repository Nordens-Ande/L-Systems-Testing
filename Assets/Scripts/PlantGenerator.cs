using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;

[Serializable]
//public struct ProductionRule
//{
//    public char Predecessor;
//    public string Sucessor;
//}

public class PlantGenerator : MonoBehaviour
{
    private string plant;
    private GameObject plantObject;

    [SerializeField] private int seed = -1;
    [SerializeField] private bool drawGizmo = false;

    [Space]

    [SerializeField] private string axiom;

    [Space]

    [SerializeField] private Grammar grammarPreset;
    [SerializeField] private List<ProductionRule> productionRules = new List<ProductionRule>();

    [Space]

    [SerializeField, Range(1, 10)] private int iterations;

    [Space]

    [SerializeField] private float stepLength;
    [SerializeField] private float stepAngle;

    [Space]

    [SerializeField] private float baseRadius = 0.3f;
    [SerializeField] private float tipRadius = 0.03f;
    [SerializeField] private int radialSegments = 8;
    [SerializeField] private int splineResolution = 20;
    [SerializeField] private AnimationCurve radiusCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Space]

    [SerializeField] Material plantMaterial;

    //private List<Branch> branches = new List<Branch>(); 
    private Stack<Branch> branchStack = new Stack<Branch>(); 
    private Stack<GrammarNode> stack = new Stack<GrammarNode>();
    private Stack<int> splineIndexStack = new Stack<int>();

    private GrammarNode currentNode;
    private Branch currentBranch;
    private Spline currentSpline;
    private int currentSplineIndex;

    private List<Branch> branches = new List<Branch>();

    private List<List<Vector3>> branchList = new List<List<Vector3>>();


    private SplineContainer container;

    private void Awake()
    {
        if (seed == -1)
        {
            //seed = (int)System.DateTime.Now.Ticks;
            seed = UnityEngine.Random.Range(short.MinValue, short.MaxValue);
        }

        UnityEngine.Random.InitState(seed);
    }

    void Start()
    {   
        currentNode = new GrammarNode 
        { 
            position = transform.position, 
            rotation = transform.rotation 
        };

        currentBranch = new Branch() //root
        {
            splineIndex = 0
        };
        //branchStack.Push(currentBranch);
        branches.Add(currentBranch);

        plant = grammarPreset != null ? grammarPreset.Axiom : axiom;

        ExpandPlantString();

        GenerateObject();

        CreateBranches();

        //for (int i = 0; i < container.Splines.Count; i++)
        //{
        //    Spline spline = container[i];

        //    if (spline.Count < 2)
        //    {
        //        Debug.LogWarning(
        //            $"Spline {i} has only {spline.Count} knot(s)!"
        //        );
        //    }
        //}

        plantObject.GetComponent<PlantExtruder>().GenerateMesh();

        //GenerateMesh();
    }

    void Update()
    {
        
    }

    private void ExpandPlantString()
    {
        //Dictionary<char, string> rules = new Dictionary<char, string>();
        //foreach (ProductionRule rule in productionRules)
        //{
        //    //rules.Add(rule.Predecessor, rule.Sucessor);
        //}
        Dictionary<char, Func<string>> rules = new Dictionary<char, Func<string>>();
        List<ProductionRule> productionRules = grammarPreset != null ? grammarPreset.Rules : this.productionRules;

        Debug.Log(grammarPreset.Rules.Count);
        foreach (ProductionRule rule in productionRules)
        {
            Debug.Log($"{rule.Predecessor} -> {rule.Produce()}");
            rules.Add(rule.Predecessor, rule.Produce);
        }


        for (int i = 0; i < iterations; i++)
        {
            // or newPlant since it will be an addition of the old plant and the production
            StringBuilder production = new StringBuilder();

            foreach (char predecessor in plant.ToString())
            {
                string sucessor = predecessor.ToString();

                // creates the production by evaluating the predecessor and then adding the proper succesor
                if (rules.TryGetValue(predecessor, out Func<string> rule))
                {
                    sucessor = rule();

                }
                //else
                //{
                //    sucessor = predecessor switch
                //    {
                //        'F' => "FF",
                //        'B' => "[lFB][rFB]",
                //        _ => predecessor.ToString()
                //    };
                //}

                production.Append(sucessor);
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
        PlantExtruder extruder = plantObject.GetComponent<PlantExtruder>();
        extruder.container = container;
        extruder.root = currentBranch;
        extruder.branches = branches;

        extruder.baseRadius = baseRadius;
        extruder.tipRadius = tipRadius;

        extruder.splineResolution = splineResolution;
        extruder.radialSegments = radialSegments;
        
        extruder.radiusCurve = radiusCurve;


        currentSpline = container.AddSpline();
        currentSplineIndex = container.Splines.Count - 1;

        Vector3 localPosition = container.transform.InverseTransformPoint(currentNode.position);
        currentSpline.Add(new BezierKnot(localPosition), TangentMode.AutoSmooth);

        //branchStack.Clear();

        //branchesStack.Add(new Branch
        //{
        //    splineIndex = 0,
        //    parentSplineIndex = -1,
        //    parentKnotIndex = -1,
        //});
    }

    private void CreateBranches()
    {
        foreach (char c in plant)
        {
            Action productionRule = c switch
            {
                'F' => MoveForward,
                'r' or '+' => RotateRight,
                'l' or '-' => RotateLeft,
                'u' => RotateUp,
                'd' => RotateDown,
                '[' => PushNode,
                ']' => PopNode,
                _ => () => { }
            };

            productionRule();
        }
    }

    private void PushNode()
    {
        // GrammarNode
        stack.Push(new GrammarNode
        {
            position = currentNode.position,
            rotation = currentNode.rotation
        });

        // Spline and indices
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

        // Branches
        Branch child = new Branch()
        {
            splineIndex         = currentSplineIndex,
            parentSplineIndex   = prevSplineIndex,
            parentBranch        = currentBranch,
            distanceFromRoot    = currentBranch.distanceFromRoot + currentBranch.length
        };

        currentBranch.childrenSplineIndices.Add(child.splineIndex);
        currentBranch.childBranches.Add(child);

        branchStack.Push(currentBranch);
        branches.Add(child);
        currentBranch = child;
    }

    private void PopNode()
    {
        // GrammerNode
        currentNode = stack.Pop();

        // Spline and indices
        currentSplineIndex = splineIndexStack.Pop();
        currentSpline = container[currentSplineIndex];

        // Branch
        currentBranch = branchStack.Pop();
    }

    private void MoveForward()
    {
        Vector3 newPosition = currentNode.position + currentNode.rotation * Vector3.up * stepLength;

        branchList.Add(new List<Vector3>() { currentNode.position, newPosition });
        //branchList[^1].Add(newPosition);

        Vector3 localPosition = container.transform.InverseTransformPoint(newPosition);
        currentSpline.Add(new BezierKnot(localPosition), TangentMode.AutoSmooth);

        currentNode.position = newPosition;

        currentBranch.length += stepLength;
    }

    private void RotateRight()
    {
        currentNode.rotation *= Quaternion.Euler(0, 0, -stepAngle);
    }

    private void RotateLeft()
    {
        currentNode.rotation *= Quaternion.Euler(0, 0, stepAngle);
    }

    private void RotateUp()
    {
        currentNode.rotation *= Quaternion.Euler(stepAngle, 0, 0);
    }

    private void RotateDown()
    {
        currentNode.rotation *= Quaternion.Euler(-stepAngle, 0, 0);
    }

    float GetRadius(float t)
    {
        return baseRadius * radiusCurve.Evaluate(t);
    }


    private void OnDrawGizmos()
    {
        if (!drawGizmo)
            return;

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
