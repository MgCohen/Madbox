using UnityEditor;
using UnityEngine;

namespace Madbox.Addressables.Editor
{
    [CustomPropertyDrawer(typeof(AddressablesPreloadConfigEntry))]
    public class AddressablesPreloadConfigEntryDrawer : PropertyDrawer
    {
        private const float spacing = 6f;
        private const float typeWidthRatio = 0.34f;
        private const float referenceTypeRatio = 0.18f;
        private const float modeWidthRatio = 0.16f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EntryFields fields = CreateFields(property);
            if (ShouldUseFallback(fields)) { DrawFallback(position, property, label); return; }
            Rect content = EditorGUI.PrefixLabel(position, label);
            DrawColumns(content, fields);
        }

        private void DrawFallback(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }

        private EntryFields CreateFields(SerializedProperty property)
        {
            SerializedProperty assetType = property.FindPropertyRelative("assetType");
            SerializedProperty referenceType = property.FindPropertyRelative("referenceType");
            SerializedProperty assetReference = property.FindPropertyRelative("assetReference");
            SerializedProperty labelReference = property.FindPropertyRelative("labelReference");
            SerializedProperty mode = property.FindPropertyRelative("mode");
            return new EntryFields(assetType, referenceType, assetReference, labelReference, mode);
        }

        private bool ShouldUseFallback(EntryFields fields)
        {
            if (fields.AssetType == null) { return true; }
            if (fields.ReferenceType == null) { return true; }
            if (fields.AssetReference == null) { return true; }
            if (fields.LabelReference == null) { return true; }
            return fields.Mode == null;
        }

        private void DrawColumns(Rect content, EntryFields fields)
        {
            ColumnRects rects = CreateColumnRects(content);
            DrawWithoutLabel(rects.TypeRect, fields.AssetType);
            DrawWithoutLabel(rects.ReferenceTypeRect, fields.ReferenceType);
            DrawReferenceValue(rects.ValueRect, fields);
            DrawWithoutLabel(rects.ModeRect, fields.Mode);
        }

        private ColumnRects CreateColumnRects(Rect content)
        {
            float valueWidth = CalculateValueWidth(content.width);
            float typeWidth = CalculateTypeWidth(content.width);
            float referenceTypeWidth = CalculateReferenceTypeWidth(content.width);
            float modeWidth = CalculateModeWidth(content.width);
            return CreateRects(content, typeWidth, referenceTypeWidth, valueWidth, modeWidth);
        }

        private ColumnRects CreateRects(Rect content, float typeWidth, float referenceTypeWidth, float valueWidth, float modeWidth)
        {
            Rect typeRect = new Rect(content.x, content.y, typeWidth, content.height);
            Rect referenceTypeRect = new Rect(typeRect.xMax + spacing, content.y, referenceTypeWidth, content.height);
            Rect valueRect = new Rect(referenceTypeRect.xMax + spacing, content.y, valueWidth, content.height);
            Rect modeRect = new Rect(valueRect.xMax + spacing, content.y, modeWidth, content.height);
            return new ColumnRects(typeRect, referenceTypeRect, valueRect, modeRect);
        }

        private float CalculateTypeWidth(float fullWidth)
        {
            float width = fullWidth - (spacing * 3f);
            return width * typeWidthRatio;
        }

        private float CalculateReferenceTypeWidth(float fullWidth)
        {
            float width = fullWidth - (spacing * 3f);
            return width * referenceTypeRatio;
        }

        private float CalculateModeWidth(float fullWidth)
        {
            float width = fullWidth - (spacing * 3f);
            return width * modeWidthRatio;
        }

        private float CalculateValueWidth(float fullWidth)
        {
            float width = fullWidth - (spacing * 3f);
            float typeWidth = width * typeWidthRatio;
            float referenceTypeWidth = width * referenceTypeRatio;
            float modeWidth = width * modeWidthRatio;
            return width - typeWidth - referenceTypeWidth - modeWidth;
        }

        private void DrawReferenceValue(Rect rect, EntryFields fields)
        {
            SerializedProperty selected = IsLabelReference(fields.ReferenceType) ? fields.LabelReference : fields.AssetReference;
            DrawWithoutLabel(rect, selected);
        }

        private bool IsLabelReference(SerializedProperty referenceType)
        {
            return referenceType.enumValueIndex == (int)PreloadReferenceType.LabelReference;
        }

        private void DrawWithoutLabel(Rect rect, SerializedProperty property)
        {
            EditorGUI.PropertyField(rect, property, GUIContent.none, true);
        }

        private readonly struct EntryFields
        {
            public EntryFields(SerializedProperty assetType, SerializedProperty referenceType, SerializedProperty assetReference, SerializedProperty labelReference, SerializedProperty mode)
            {
                AssetType = assetType;
                ReferenceType = referenceType;
                AssetReference = assetReference;
                LabelReference = labelReference;
                Mode = mode;
            }

            public SerializedProperty AssetType { get; }
            public SerializedProperty ReferenceType { get; }
            public SerializedProperty AssetReference { get; }
            public SerializedProperty LabelReference { get; }
            public SerializedProperty Mode { get; }
        }

        private readonly struct ColumnRects
        {
            public ColumnRects(Rect typeRect, Rect referenceTypeRect, Rect valueRect, Rect modeRect)
            {
                TypeRect = typeRect;
                ReferenceTypeRect = referenceTypeRect;
                ValueRect = valueRect;
                ModeRect = modeRect;
            }

            public Rect TypeRect { get; }
            public Rect ReferenceTypeRect { get; }
            public Rect ValueRect { get; }
            public Rect ModeRect { get; }
        }
    }
}
