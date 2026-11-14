using UnityEngine;

//[CreateAssetMenu(fileName = "ProductionRule", menuName = "Scriptable Objects/ProductionRule")]
public abstract class ProductionRule : ScriptableObject
{
    public char Predecessor;

    public bool Matches(char input) => Predecessor == input;

    // Produces the Sucessor
    public abstract string Produce();
}
