using UnityEngine;

[CreateAssetMenu(fileName = "New DeterministicRule", menuName = "L-System/Production Rules/DeterministicRule")]
public class DeterministicRule : ProductionRule
{
    public string Successor;

    public override string Produce()
    {
        return Successor;
    }
}
