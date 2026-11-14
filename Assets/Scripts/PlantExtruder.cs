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

    public float baseRadius = 0.3f;
    public float tipRadius = 0.010f;


    public int radialSegments = 8;

    public int splineResolution = 20;

    public AnimationCurve radiusCurve = AnimationCurve.Linear(0, 1, 1, 0);
        
    void Start()
    {
        //GenerateMesh();
    }

    float GetRadius(float remainingDistance, float totalLength)
    {
        float normalizedRemaining = Mathf.Clamp01(remainingDistance / totalLength);

        // Convert remaining distance into a root-to-tip coordinate.
        float normalizedPosition = 1f - normalizedRemaining;

        float curveValue = Mathf.Clamp01(radiusCurve.Evaluate(normalizedPosition));

        return Mathf.Lerp(tipRadius, baseRadius, curveValue);
    }

    // Postorder traversal
    float CalculateMaxTipDistance(Branch branch)
    {
        float maxDistance = branch.length;

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
    float GetRemainingDistanceToTip(Branch branch, float distanceAlongBranch)
    {
        // Distance remaining to the end of this branch
        float maxRemainingDistance = Mathf.Max(0f, branch.length - distanceAlongBranch);

        foreach (Branch child in branch.childBranches)
        {
            float childOffset = child.distanceFromRoot - branch.distanceFromRoot;

            // Ignore children that attach behind our current position.
            if (childOffset < distanceAlongBranch)
                continue;

            // Distance from our current position to the child, plus the child's longest remaining path.
            float remainingDistance = childOffset - distanceAlongBranch + child.maxTipDistance;

            maxRemainingDistance = Mathf.Max(maxRemainingDistance, remainingDistance);
        }

        return maxRemainingDistance;
    }

    public void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Plant Mesh";

        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector2> uvs = new();

        float totalLength = CalculateMaxTipDistance(root);

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
                float distanceAlongBranch = t * branch.length;

                float remainingDistance = GetRemainingDistanceToTip(branch, distanceAlongBranch);

                float radius = GetRadius(remainingDistance, totalLength);

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
}
