using System;
using System.Collections.Generic;
using Madbox.Levels.Behaviors;
using Madbox.Enemies.Authoring.Definitions;
using UnityEditor;
using UnityEngine;
#pragma warning disable SCA0003
#pragma warning disable SCA0006

namespace Madbox.Enemies.Editor
{
    [CustomEditor(typeof(EnemyDefinitionSO))]
    public sealed class EnemyDefinitionSOEditor : UnityEditor.Editor
    {
        private const string behaviorRulesFieldName = "behaviorRules";

        private SerializedProperty behaviorRules;

        private void OnEnable()
        {
            behaviorRules = serializedObject.FindProperty(behaviorRulesFieldName);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", behaviorRulesFieldName);
            DrawBehaviorRules();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBehaviorRules()
        {
            if (behaviorRules == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Behavior Rules", EditorStyles.boldLabel);
            for (int i = 0; i < behaviorRules.arraySize; i++)
            {
                DrawBehaviorRuleElement(i);
            }

            DrawRuleButtons();
        }

        private void DrawBehaviorRuleElement(int index)
        {
            SerializedProperty element = behaviorRules.GetArrayElementAtIndex(index);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(element, GUIContent.none, true);
            if (GUILayout.Button("Remove Rule"))
            {
                behaviorRules.DeleteArrayElementAtIndex(index);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRuleButtons()
        {
            if (GUILayout.Button("Add Rule"))
            {
                ShowAddMenu();
            }

            if (behaviorRules.arraySize > 0 && GUILayout.Button("Clear Rules"))
            {
                behaviorRules.ClearArray();
            }
        }

        private void ShowAddMenu()
        {
            GenericMenu menu = new GenericMenu();
            IReadOnlyList<Type> candidateTypes = CollectRuleTypes();
            for (int i = 0; i < candidateTypes.Count; i++)
            {
                Type type = candidateTypes[i];
                menu.AddItem(new GUIContent(type.Name), false, () => AddRule(type));
            }

            menu.ShowAsContext();
        }

        private IReadOnlyList<Type> CollectRuleTypes()
        {
            List<Type> result = new List<Type>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<EnemyBehaviorDefinition>())
            {
                if (type.IsAbstract || type.ContainsGenericParameters || type.IsGenericTypeDefinition)
                {
                    continue;
                }

                result.Add(type);
            }

            return result;
        }

        private void AddRule(Type ruleType)
        {
            serializedObject.Update();
            behaviorRules.arraySize++;
            SerializedProperty element = behaviorRules.GetArrayElementAtIndex(behaviorRules.arraySize - 1);
            element.managedReferenceValue = Activator.CreateInstance(ruleType);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
