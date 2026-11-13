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

    [SerializeField] private AnimationCurve radiusCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
    void Start()
    {
        //GenerateMesh();
    }

    void Update()
    {
        
    }

    float GetRadius(float t)
    {
        return baseRadius * radiusCurve.Evaluate(t);
    }

    float GetBranchThickness(Branch branch, float rootThickness, float minThickness, float totalTreeHeight)
    {
        float normalizedHeight = branch.distanceFromRoot / totalTreeHeight;

        return Mathf.Lerp(rootThickness, minThickness, normalizedHeight);
    }

    // Postorder traversal
    float CalculateMaxTipDistance(Branch branch)
    {
        if (branch.childBranches.Count == 0)
            return branch.length;

        float max = 0f;

        foreach (Branch child in branch.childBranches)
        {
            float distance = CalculateMaxTipDistance(child);

            max = Mathf.Max(max, distance);
        }

        branch.maxTipDistance = branch.length + max;

        return branch.maxTipDistance;
    }

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

        foreach (Branch branch in branches)
        {
            Debug.Log($"Branch: {branch.splineIndex}, Length: {branch.length}, Root distance: {branch.distanceFromRoot}, Max Tip distance: {branch.maxTipDistance}, Child Count: {branch.childBranches.Count}");

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

                float normalizedHeight = (branch.distanceFromRoot + t * branch.length) / totalLength;

                float radius = Mathf.Lerp(baseRadius, tipRadius, normalizedHeight);

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


        //// First we sample/calculate the length of all splines
        //for (int splineIndex = 0; splineIndex < container.Splines.Count; splineIndex++)
        //{
        //    Spline spline = container[splineIndex];
        //    Branch branch = branches[splineIndex];

        //    List<Vector3> positions = new List<Vector3>();
        //    List<float> distances = new List<float>();
        //    List<(Vector3 position, float distance)> branchLength = new List<(Vector3 position, float distance)>();

        //    float splineLength = 0f;

        //    for (int j = 0; j <= splineResolution; j++)
        //    {
        //        float t = j / (float)splineResolution;

        //        Vector3 position = spline.EvaluatePosition(t);

        //        if (j != 0)
        //            splineLength += Vector3.Distance(positions[j - 1], position);

        //        positions.Add(position);
        //        distances.Add(splineLength);

        //        branchLength.Add((position, splineLength));
        //    }

        //    sampledPositions.Add(positions);
        //    sampledDistances.Add(distances);

        //    sampledBranchLengths.Add(branchLength);

        //    branch.localLength = splineLength;
        //    //Debug.Log(splineIndex);
        //    //Debug.Log(branch.splineIndex);
        //    //Debug.Log(branch.parentSplineIndex);
        //    branch.totalLength = splineLength + (splineIndex != 0 ? branches[branch.parentSplineIndex].totalLength : 0f);

        //    totalLength = Mathf.Max(branch.totalLength, totalLength);
        //}

        ////float accumulatedLength = 0f;

        //// Iterate over all splines in the container.
        //for (int splineIndex = 0; splineIndex < container.Splines.Count; splineIndex++)
        //{
        //    Spline spline = container[splineIndex];
        //    Branch branch = branches[splineIndex];

        //    List<Vector3> positions = sampledPositions[splineIndex];
        //    List<float> distances = sampledDistances[splineIndex];
        //    List<(Vector3 position, float distance)> branchLengths = sampledBranchLengths[splineIndex];

        //    int startVertex = vertices.Count;


        //    for (int j = 0; j <= splineResolution; j++)
        //    {
        //        float t = j / (float)splineResolution;

        //        // Evaluate spline
        //        //Vector3 position = spline.EvaluatePosition(t);
        //        Vector3 position = positions[j];
        //        Vector3 tangent = ((Vector3)spline.EvaluateTangent(t)).normalized;

        //        // Calculate world distance (real distance from plants start)
        //        //float totalBranchDistance = accumulatedLength + distances[j];
        //        float totalBranchDistance = (splineIndex != 0 ? branches[branch.parentSplineIndex].totalLength : 0f) + distances[j];

        //        float branchT = totalLength > 0f ? totalBranchDistance / totalLength : 0f;

        //        // Calculate radius
        //        float radius = Mathf.Lerp(baseRadius, tipRadius, branchT); // CHANGE THIS LATER TO ANIMATION CURVE

        //        // Calculate perpendicular vectors
        //        Vector3 reference = Vector3.up;

        //        if (Mathf.Abs(Vector3.Dot(tangent, reference)) > 0.99f)
        //            reference = Vector3.right;

        //        // Normals
        //        Vector3 normal = Vector3.Cross(tangent, reference).normalized;
        //        Vector3 binormal = Vector3.Cross(tangent, normal).normalized;

        //        // Generate circular ring
        //        for (int i = 0; i < radialSegments; i++)
        //        {
        //            float angle = (i / (float)radialSegments) * Mathf.PI * 2f;

        //            Vector3 offset = normal * Mathf.Cos(angle) * radius + binormal * Mathf.Sin(angle) * radius;

        //            vertices.Add(position + offset);

        //            uvs.Add(new Vector2(i / (float)radialSegments, branchT));
        //        }

        //        // Connect this ring to the previous ring
        //        if (j != 0)
        //        {
        //            int currentRing = startVertex + j * radialSegments;
        //            int previousRing = currentRing - radialSegments;

        //            for (int i = 0; i < radialSegments; i++)
        //            {
        //                int next = (i + 1) % radialSegments;

        //                int a = previousRing + i;
        //                int b = previousRing + next;
        //                int c = currentRing + i;
        //                int d = currentRing + next;

        //                triangles.Add(a);
        //                triangles.Add(b);
        //                triangles.Add(c);

        //                triangles.Add(b);
        //                triangles.Add(d);
        //                triangles.Add(c);
        //            }
        //        }
        //    }

            //accumulatedLength += distances[^1];

            //Debug.Log($"{splineIndex}: Total Length: {totalLength}, Accumulated Length: {accumulatedLength}");
        //}

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
