using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Branch
{
    public int splineIndex;

    public int parentSplineIndex = -1;
    //public int parentKnotIndex = -1;
    public Branch parentBranch;

    public List<int> childrenSplineIndices = new List<int>();
    public List<Branch> childBranches = new List<Branch>();

    //public float localLength = 0f;
    //public float totalLength = 0f;

    public float length = 0f;
    public float distanceFromRoot = 0f;
    public float maxTipDistance = 0f;

    public float startRadius;
    public float endRadius = 0f;
}
