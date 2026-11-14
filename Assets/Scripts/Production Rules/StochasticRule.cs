using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New StochasticRule", menuName = "L-System/Production Rules/StochasticRule")]
public class StochasticRule : ProductionRule
{
    [Serializable]
    public class StochasticParameter
    {
        public string Sucessor;
        [Range(0f, 1f)] public float Odds;
    }

    public List<StochasticParameter> stochasticSuccesors = new List<StochasticParameter>();

    public override string Produce()
    {
        float roll = UnityEngine.Random.value;
        float currentOdd = 0f;

        for (int i = 0; i < stochasticSuccesors.Count; ++i)
        {
            StochasticParameter parameter = stochasticSuccesors[i];
            currentOdd += parameter.Odds;

            if (roll < currentOdd)
            {
                return parameter.Sucessor;
            }
        }

        throw new Exception();
    }
}
