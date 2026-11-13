using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;

public class PlantExtruder : MonoBehaviour
{
    public SplineContainer container;
    public Branch root;
    public List<Branch> branches;

    [SerializeField] private float baseRadius = 0.3f;
    [SerializeField] private float tipRadius = 0.010f;


    [SerializeField] private int radialSegments = 8;

    [SerializeField] private int splineResolution = 20;

    [SerializeField] private AnimationCurve radiusCurve = AnimationCurve.Linear(0, 1, 1, 0);
        
    void Start()
    {
        //GenerateMesh();
    }

    void Update()
    {
        
    }

    //float GetRadius(float t)
    //{
    //    return baseRadius * radiusCurve.Evaluate(t);
    //}

    //float GetBranchThickness(Branch branch, float rootThickness, float minThickness, float totalTreeHeight)
    //{
    //    float normalizedHeight = branch.distanceFromRoot / totalTreeHeight;

    //    return Mathf.Lerp(rootThickness, minThickness, normalizedHeight);
    //}
    float GetBranchRadius(Branch branch, float t)
    {
        float curveValue = radiusCurve.Evaluate(t);

        return Mathf.Lerp(branch.endRadius, branch.startRadius, curveValue);
    }

    float GetPlantRadius(float distanceFromRoot, float totalLength)
    {
        float normalizedLength = Mathf.Clamp01(distanceFromRoot / totalLength);

        float curveValue = Mathf.Clamp01(radiusCurve.Evaluate(normalizedLength));

        return Mathf.Lerp(tipRadius, baseRadius, curveValue);
    }


    // Postorder traversal
    float CalculateMaxTipDistance(Branch branch)
    {
        // If a child is connected the end of the branch, we should treat the endpoint as a junction instead of a terminal/leaf tip (i.e tipRadius)
        bool hasChildAtEnd = HasChildAtEnd(branch);

        // Only count the branch's own endpoint if it is a tip/leaf
        float maxDistance = hasChildAtEnd ? 0f : branch.length;
        //float maxDistance = branch.length;

        foreach (Branch child in branch.childBranches)
        {
            // Distance from the beginning of the parent to the child attachment point
            float childOffset = child.distanceFromRoot - branch.distanceFromRoot;

            float distance = childOffset + CalculateMaxTipDistance(child);

            maxDistance = Mathf.Max(maxDistance, distance);
        }

        branch.maxTipDistance = maxDistance;

        return branch.maxTipDistance;
    }
    void AssignBranchRadii(Branch branch, float totalLength)
    {
        float branchEndDistance = branch.distanceFromRoot + branch.length;

        // Determine whether this branch ends at a junction or a tip/leaf
        bool hasChildAtEnd = HasChildAtEnd(branch);

        if (hasChildAtEnd)
        {
            // Get the real endpoint radius by using its global distance from the root
            float plantRadius = GetPlantRadius(branchEndDistance, totalLength);

            // We need to assign this so that junctions at endpoints don't use the tipRadius as it did in previous solutions
            branch.endRadius = Mathf.Min(branch.startRadius, plantRadius);
        }
        else
        {
            // This endpoint is an actual terminal tip/lead, i.e it should ineherent the tipRadius
            branch.endRadius = tipRadius;
        }

        foreach (Branch child in branch.childBranches)
        {
            // Distance from the beginning of the parent to the child attachment point
            float childOffset = child.distanceFromRoot - branch.distanceFromRoot;

            // Normalize the attachment position
            float t = branch.length > Mathf.Epsilon ? Mathf.Clamp01(childOffset / branch.length) : 0f;

            // Inherents the parents exact radius at the attatchment point of the child
            child.startRadius = GetBranchRadius(branch, t);

            // Continue recursively through the tree
            AssignBranchRadii(child, totalLength);
        }
    }
    bool HasChildAtEnd(Branch branch)
    {
        float branchEndDistance = branch.distanceFromRoot + branch.length;

        float tolerance = 0.0001f * Mathf.Max(1f, Mathf.Abs(branchEndDistance));

        foreach (Branch child in branch.childBranches)
        {
            if (Mathf.Abs(child.distanceFromRoot - branchEndDistance) <= tolerance)
                return true;
        }

        return false;
    }
    //float CalculateMaxTipDistance(Branch branch)
    //{
    //    if (branch.childBranches.Count == 0)
    //        return branch.length;

    //    float max = 0f;

    //    foreach (Branch child in branch.childBranches)
    //    {
    //        float distance = CalculateMaxTipDistance(child);

    //        max = Mathf.Max(max, distance);
    //    }

    //    branch.maxTipDistance = branch.length + max;

    //    return branch.maxTipDistance;
    //}

    public void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Plant Mesh";

        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector2> uvs = new();


        // Store positions and cumulative distances
        //List<List<Vector3>> sampledPositions = new List<List<Vector3>>();
        //List<List<float>> sampledDistances = new List<List<float>>();

        //List<List<(Vector3, float)>> sampledBranchLengths = new List<List<(Vector3, float)>>();

        float totalLength = CalculateMaxTipDistance(root);

        // Initialize root and then propagate through the branch tree
        root.startRadius = baseRadius;
        AssignBranchRadii(root, totalLength);

        foreach (Branch branch in branches)
        {
            Debug.Log(
                $"Branch: {branch.splineIndex}, " +
                $"Length: {branch.length}, " +
                $"Root distance: {branch.distanceFromRoot}, " +
                $"Max Tip distance: {branch.maxTipDistance}, " +
                $"Start Radius: {branch.startRadius}, " +
                $"Child Count: {branch.childBranches.Count}"
                );

            Spline spline = container[branch.splineIndex];
            Debug.Log($"Spline positions, T:0 = {spline.EvaluatePosition(0)}, T:1 = {spline.EvaluatePosition(1)}");

            int startVertex = vertices.Count;


            for (int j = 0; j <= splineResolution; j++)
            {
                float t = j / (float)splineResolution;

                // Evaluate spline
                Vector3 position = spline.EvaluatePosition(t);
                Vector3 tangent = ((Vector3)spline.EvaluateTangent(t)).normalized;

                // branchT, ratio?, used for UV
                float branchT = branch.distanceFromRoot / totalLength;

                // Calculate radius
                //float radius = GetBranchThickness(branch, baseRadius, tipRadius, totalLength); // CHANGE THIS LATER TO ANIMATION CURVE

                //float normalizedHeight = (branch.distanceFromRoot + t * branch.length) / totalLength;
                //float radius = Mathf.Lerp(baseRadius, tipRadius, normalizedHeight);
                
                float radius = GetBranchRadius(branch, t);

                // Calculate perpendicular vectors
                Vector3 reference = Vector3.up;

                if (Mathf.Abs(Vector3.Dot(tangent, reference)) > 0.99f)
                    reference = Vector3.right;

                // Normals
                Vector3 normal = Vector3.Cross(tangent, reference).normalized;
                Vector3 binormal = Vector3.Cross(tangent, normal).normalized;

                // Generate circular ring
                for (int i = 0; i < radialSegments; i++)
                {
                    float angle = (i / (float)radialSegments) * Mathf.PI * 2f;

                    Vector3 offset = normal * Mathf.Cos(angle) * radius + binormal * Mathf.Sin(angle) * radius;

                    vertices.Add(position + offset);

                    uvs.Add(new Vector2(i / (float)radialSegments, branchT));
                }

                // Connect this ring to the previous ring
                if (j != 0)
                {
                    int currentRing = startVertex + j * radialSegments;
                    int previousRing = currentRing - radialSegments;

                    for (int i = 0; i < radialSegments; i++)
                    {
                        int next = (i + 1) % radialSegments;

                        int a = previousRing + i;
                        int b = previousRing + next;
                        int c = currentRing + i;
                        int d = currentRing + next;

                        triangles.Add(a);
                        triangles.Add(b);
                        triangles.Add(c);

                        triangles.Add(b);
                        triangles.Add(d);
                        triangles.Add(c);
                    }
                }
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    //public void GenerateMesh()
    //{
    //    Mesh mesh = new Mesh();
    //    mesh.name = "Plant Mesh";

    //    List<Vector3> vertices = new();
    //    List<int> triangles = new();
    //    List<Vector2> uvs = new();


    //    // Store positions and cumulative distances
    //    List<List<Vector3>> sampledPositions = new List<List<Vector3>>();
    //    List<List<float>> sampledDistances = new List<List<float>>();

    //    List<List<(Vector3, float)>> sampledBranchLengths = new List<List<(Vector3, float)>>();

    //    float totalLength = 0f;

    //    // First we sample/calculate the length of all splines
    //    for (int splineIndex = 0; splineIndex < container.Splines.Count; splineIndex++)
    //    {
    //        Spline spline = container[splineIndex];
    //        Branch branch = branches[splineIndex];

    //        List<Vector3> positions = new List<Vector3>();
    //        List<float> distances = new List<float>();
    //        List<(Vector3 position, float distance)> branchLength = new List<(Vector3 position, float distance)>();

    //        float splineLength = 0f;

    //        for (int j = 0; j <= splineResolution; j++)
    //        {
    //            float t = j / (float)splineResolution;

    //            Vector3 position = spline.EvaluatePosition(t);

    //            if (j != 0)
    //                splineLength += Vector3.Distance(positions[j - 1], position);

    //            positions.Add(position);
    //            distances.Add(splineLength);

    //            branchLength.Add((position, splineLength));
    //        }

    //        sampledPositions.Add(positions);
    //        sampledDistances.Add(distances);

    //        sampledBranchLengths.Add(branchLength);

    //        branch.localLength = splineLength;
    //        //Debug.Log(splineIndex);
    //        //Debug.Log(branch.splineIndex);
    //        //Debug.Log(branch.parentSplineIndex);
    //        branch.totalLength = splineLength + (splineIndex != 0 ? branches[branch.parentSplineIndex].totalLength : 0f);

    //        totalLength = Mathf.Max(branch.totalLength, totalLength);
    //    }

    //    //float accumulatedLength = 0f;

    //    // Iterate over all splines in the container.
    //    for (int splineIndex = 0; splineIndex < container.Splines.Count; splineIndex++)
    //    {
    //        Spline spline = container[splineIndex];
    //        Branch branch = branches[splineIndex];

    //        List<Vector3> positions = sampledPositions[splineIndex];
    //        List<float> distances = sampledDistances[splineIndex];
    //        List<(Vector3 position, float distance)> branchLengths = sampledBranchLengths[splineIndex];

    //        int startVertex = vertices.Count;


    //        for (int j = 0; j <= splineResolution; j++)
    //        {
    //            float t = j / (float)splineResolution;

    //            // Evaluate spline
    //            //Vector3 position = spline.EvaluatePosition(t);
    //            Vector3 position = positions[j];
    //            Vector3 tangent = ((Vector3)spline.EvaluateTangent(t)).normalized;

    //            // Calculate world distance (real distance from plants start)
    //            //float totalBranchDistance = accumulatedLength + distances[j];
    //            float totalBranchDistance = (splineIndex != 0 ? branches[branch.parentSplineIndex].totalLength : 0f) + distances[j];

    //            float branchT = totalLength > 0f ? totalBranchDistance / totalLength : 0f;

    //            // Calculate radius
    //            float radius = Mathf.Lerp(baseRadius, tipRadius, branchT); // CHANGE THIS LATER TO ANIMATION CURVE

    //            // Calculate perpendicular vectors
    //            Vector3 reference = Vector3.up;

    //            if (Mathf.Abs(Vector3.Dot(tangent, reference)) > 0.99f)
    //                reference = Vector3.right;

    //            // Normals
    //            Vector3 normal = Vector3.Cross(tangent, reference).normalized;
    //            Vector3 binormal = Vector3.Cross(tangent, normal).normalized;

    //            // Generate circular ring
    //            for (int i = 0; i < radialSegments; i++)
    //            {
    //                float angle = (i / (float)radialSegments) * Mathf.PI * 2f;

    //                Vector3 offset = normal * Mathf.Cos(angle) * radius + binormal * Mathf.Sin(angle) * radius;

    //                vertices.Add(position + offset);

    //                uvs.Add(new Vector2(i / (float)radialSegments, branchT));
    //            }

    //            // Connect this ring to the previous ring
    //            if (j != 0)
    //            {
    //                int currentRing = startVertex + j * radialSegments;
    //                int previousRing = currentRing - radialSegments;

    //                for (int i = 0; i < radialSegments; i++)
    //                {
    //                    int next = (i + 1) % radialSegments;

    //                    int a = previousRing + i;
    //                    int b = previousRing + next;
    //                    int c = currentRing + i;
    //                    int d = currentRing + next;

    //                    triangles.Add(a);
    //                    triangles.Add(b);
    //                    triangles.Add(c);

    //                    triangles.Add(b);
    //                    triangles.Add(d);
    //                    triangles.Add(c);
    //                }
    //            }
    //        }

    //        //accumulatedLength += distances[^1];

    //        //Debug.Log($"{splineIndex}: Total Length: {totalLength}, Accumulated Length: {accumulatedLength}");
    //    }

    //    mesh.SetVertices(vertices);
    //    mesh.SetTriangles(triangles, 0);
    //    mesh.SetUVs(0, uvs);
    //    mesh.RecalculateNormals();
    //    mesh.RecalculateBounds();

    //    GetComponent<MeshFilter>().sharedMesh = mesh;
    //}
}
