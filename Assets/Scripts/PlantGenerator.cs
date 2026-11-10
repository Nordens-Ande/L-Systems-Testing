using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System;

public class PlantGenerator : MonoBehaviour
{
    private string plant;

    [SerializeField] private int seed = -1;

    [SerializeField] private string axiom;

    [SerializeField] private int iterations;

    [SerializeField] private float stepLength;
    [SerializeField] private float stepAngle;

    private Stack<GrammarNode> stack = new Stack<GrammarNode>();
    private GrammarNode node;
    private List<List<Vector3>> branchList = new List<List<Vector3>>();

    void Start()
    {   
        plant = axiom;

        ExpandPlantString();
        CreateBranches();
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

    private void PushNode()
    {
        stack.Push(new GrammarNode
        {
            position = node.position,
            rotation = node.rotation
        });
    }

    private void PopNode()
    {
        node = stack.Pop();
    }

    private void MoveForward()
    {
        Vector3 newPosition = node.position + node.rotation * Vector3.up * stepLength;

        //branchList.Add(new List<Vector3>() { newPosition });
        branchList[^1].Add(newPosition);

        node.position = newPosition;
    }

    private void RotateRight()
    {
        node.rotation *= Quaternion.Euler(0, 0, -stepAngle);
    }

    private void RotateLeft()
    {
        node.rotation *= Quaternion.Euler(0, 0, stepAngle);
    }

    private void OnDrawGizmos()
    {
        foreach (List<Vector3> branch in branchList)
        {
            Gizmos.DrawLine(branch[0], branch[1]);
        }
    }
}
