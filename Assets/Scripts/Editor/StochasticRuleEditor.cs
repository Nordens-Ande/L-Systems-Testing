
using UnityEngine;
using UnityEditor;

// This editor script has been generated as it does not concern the user, it is only a visual "tool" to help designing

[CustomEditor(typeof(StochasticRule), true)]
public class StochasticRuleEditor : Editor
{
    private const float BarHeight = 18f;
    private const float BarSpacing = 2f;

    private SerializedProperty successors;

    private void OnEnable()
    {
        successors = serializedObject.FindProperty(
            "stochasticSuccesors"
        );
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw all other ProductionRule properties normally.
        DrawPropertiesExcluding(serializedObject, "stochasticSuccesors");

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Stochastic Successors", EditorStyles.boldLabel);

        DrawProbabilityBar();

        EditorGUILayout.Space(5);

        DrawSuccessors();

        EditorGUILayout.Space(5);

        DrawProbabilityStatus();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawProbabilityBar()
    {
        int count = successors.arraySize;

        if (count == 0)
            return;

        float total = GetTotalOdds();

        Rect rect = GUILayoutUtility.GetRect(1, BarHeight, GUILayout.ExpandWidth(true));

        float x = rect.x;
        float availableWidth = rect.width - BarSpacing * (count - 1);

        for (int i = 0; i < count; i++)
        {
            float odds = successors.GetArrayElementAtIndex(i).FindPropertyRelative("Odds").floatValue;

            // Display normalized proportions, even if
            // existing serialized data is invalid.
            float percentage = total > 0f ? odds / total : 1f / count;

            float width = availableWidth * percentage;

            Rect segment = new Rect(x, rect.y, width, rect.height);

            EditorGUI.DrawRect(segment, GetColor(i));

            x += width + BarSpacing;
        }
    }

    private void DrawSuccessors()
    {
        for (int i = 0; i < successors.arraySize; i++)
        {
            SerializedProperty element = successors.GetArrayElementAtIndex(i);

            SerializedProperty successor = element.FindPropertyRelative("Sucessor");

            SerializedProperty odds = element.FindPropertyRelative("Odds");

            EditorGUILayout.BeginHorizontal();

            // Matching color indicator.
            Rect colorRect = GUILayoutUtility.GetRect(12, 18, GUILayout.Width(12));

            EditorGUI.DrawRect(colorRect, GetColor(i));

            // Successor string.
            successor.stringValue = EditorGUILayout.TextField(successor.stringValue, GUILayout.MinWidth(80));

            // Probability slider.
            EditorGUI.BeginChangeCheck();

            float newOdds = EditorGUILayout.Slider(odds.floatValue, 0f, 1f, GUILayout.MinWidth(100));

            if (EditorGUI.EndChangeCheck())
            {
                SetOdds(i, newOdds);
            }

            // Display exact probability.
            EditorGUILayout.LabelField(odds.floatValue.ToString("P1"), GUILayout.Width(48));

            // Remove button.
            if (GUILayout.Button("×", GUILayout.Width(22)))
            {
                RemoveSuccessor(i);
                EditorGUILayout.EndHorizontal();
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(3);

        if (GUILayout.Button("+ Add Successor"))
        {
            AddSuccessor();
        }
    }

    private void DrawProbabilityStatus()
    {
        float total = GetTotalOdds();

        bool valid = Mathf.Abs(total - 1f) < 0.0001f;

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField($"Total Probability: {total:P2}", EditorStyles.boldLabel);

        if (!valid && successors.arraySize > 0)
        {
            if (GUILayout.Button("Normalize", GUILayout.Width(90)))
            {
                NormalizeOdds();
            }
        }

        EditorGUILayout.EndHorizontal();

        if (!valid && successors.arraySize > 0)
        {
            EditorGUILayout.HelpBox("Probability distribution is invalid.", MessageType.Warning);
        }
    }

    // Changes one probability and distributes the
    // remaining probability among all other entries.
    private void SetOdds(int index, float newOdds)
    {
        newOdds = Mathf.Clamp01(newOdds);

        int count = successors.arraySize;

        if (count == 1)
        {
            successors.GetArrayElementAtIndex(0).FindPropertyRelative("Odds").floatValue = 1f;

            return;
        }

        float otherTotal = 0f;

        for (int i = 0; i < count; i++)
        {
            if (i == index)
                continue;

            otherTotal += successors.GetArrayElementAtIndex(i).FindPropertyRelative("Odds").floatValue;
        }

        float remaining = 1f - newOdds;

        for (int i = 0; i < count; i++)
        {
            SerializedProperty odds = successors.GetArrayElementAtIndex(i).FindPropertyRelative("Odds");

            if (i == index)
            {
                odds.floatValue = newOdds;
            }
            else if (otherTotal > 0.0001f)
            {
                float proportion = odds.floatValue / otherTotal;
                odds.floatValue = remaining * proportion;
            }
            else
            {
                odds.floatValue = remaining / (count - 1);
            }
        }
    }

    private void AddSuccessor()
    {
        int count = successors.arraySize;

        NormalizeOdds();

        successors.arraySize++;

        SerializedProperty newElement = successors.GetArrayElementAtIndex(count);

        newElement.FindPropertyRelative("Sucessor").stringValue = "";

        if (count == 0)
        {
            newElement.FindPropertyRelative("Odds").floatValue = 1f;
            return;
        }

        // Give the new entry 50% probability.
        // Existing entries share the other 50%.
        for (int i = 0; i < count; i++)
        {
            SerializedProperty odds = successors.GetArrayElementAtIndex(i).FindPropertyRelative("Odds");

            odds.floatValue *= 0.5f;
        }

        newElement.FindPropertyRelative("Odds").floatValue = 0.5f;
    }

    private void RemoveSuccessor(int index)
    {
        successors.DeleteArrayElementAtIndex(index);

        NormalizeOdds();
    }

    private void NormalizeOdds()
    {
        int count = successors.arraySize;

        if (count == 0)
            return;

        float total = GetTotalOdds();

        for (int i = 0; i < count; i++)
        {
            SerializedProperty odds = successors.GetArrayElementAtIndex(i).FindPropertyRelative("Odds");

            odds.floatValue = total > 0.0001f ? odds.floatValue / total : 1f / count;
        }
    }

    private float GetTotalOdds()
    {
        float total = 0f;

        for (int i = 0; i < successors.arraySize; i++)
        {
            total += successors.GetArrayElementAtIndex(i).FindPropertyRelative("Odds").floatValue;
        }

        return total;
    }

    // Deterministic color based on array index.
    private Color GetColor(int index)
    {
        float hue = Mathf.Repeat(index * 0.61803398875f, 1f);

        return Color.HSVToRGB(hue, 0.65f, 0.85f);
    }
}
