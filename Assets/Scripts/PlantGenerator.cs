using UnityEngine;
using System.Text;

public class PlantGenerator : MonoBehaviour
{
    private string plant;

    [SerializeField] private int seed = -1;

    [SerializeField] private string axiom;

    [SerializeField] private int iterations;

    void Start()
    {
        plant = axiom;

        ExpandPlantString();
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
                    'B' => "[lFB][rFB]",
                    _ => predecessor
                });
            }

            plant = production.ToString();
            Debug.Log($"Plant at iteration: {plant}");
        }
    }
}
