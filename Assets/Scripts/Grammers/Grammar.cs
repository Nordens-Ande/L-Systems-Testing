using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Grammar", menuName = "L-System/Grammar")]
public class Grammar : ScriptableObject
{
    public string Axiom;
    public List<ProductionRule> Rules;
}
