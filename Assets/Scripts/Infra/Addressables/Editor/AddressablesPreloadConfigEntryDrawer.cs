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
            if (BuildShouldUseFallback(fields))
{
    BuildDrawFallback(position, property, label); return;
}
            Rect content = EditorGUI.PrefixLabel(position, label);
            BuildDrawColumns(content, fields);
        }

        private static void BuildDrawFallback(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }

        private static EntryFields CreateFields(SerializedProperty property)
        {
            SerializedProperty assetType = property.FindPropertyRelative("assetType");
            SerializedProperty referenceType = property.FindPropertyRelative("referenceType");
            SerializedProperty assetReference = property.FindPropertyRelative("assetReference");
            SerializedProperty labelReference = property.FindPropertyRelative("labelReference");
            SerializedProperty mode = property.FindPropertyRelative("mode");
            return new EntryFields(assetType, referenceType, assetReference, labelReference, mode);
        }

        private static bool BuildShouldUseFallback(EntryFields fields)
        {
            return fields.AssetType == null
                || fields.ReferenceType == null
                || fields.AssetReference == null
                || fields.LabelReference == null
                || fields.Mode == null;
        }

        private static void BuildDrawColumns(Rect content, EntryFields fields)
        {
            ColumnRects rects = CreateColumnRects(content);
            BuildDrawWithoutLabel(rects.TypeRect, fields.AssetType);
            BuildDrawWithoutLabel(rects.ReferenceTypeRect, fields.ReferenceType);
            BuildDrawReferenceValue(rects.ValueRect, fields);
            BuildDrawWithoutLabel(rects.ModeRect, fields.Mode);
        }

        private static ColumnRects CreateColumnRects(Rect content)
        {
            float valueWidth = BuildCalculateValueWidth(content.width);
            float typeWidth = BuildCalculateTypeWidth(content.width);
            float referenceTypeWidth = BuildCalculateReferenceTypeWidth(content.width);
            float modeWidth = BuildCalculateModeWidth(content.width);
            return CreateRects(content, typeWidth, referenceTypeWidth, valueWidth, modeWidth);
        }

        private static ColumnRects CreateRects(Rect content, float typeWidth, float referenceTypeWidth, float valueWidth, float modeWidth)
        {
            Rect typeRect = new Rect(content.x, content.y, typeWidth, content.height);
            Rect referenceTypeRect = new Rect(typeRect.xMax + spacing, content.y, referenceTypeWidth, content.height);
            Rect valueRect = new Rect(referenceTypeRect.xMax + spacing, content.y, valueWidth, content.height);
            Rect modeRect = new Rect(valueRect.xMax + spacing, content.y, modeWidth, content.height);
            return new ColumnRects(typeRect, referenceTypeRect, valueRect, modeRect);
        }

        private static float BuildCalculateTypeWidth(float fullWidth)
        {
            float width = fullWidth - (spacing * 3f);
            return width * typeWidthRatio;
        }

        private static float BuildCalculateReferenceTypeWidth(float fullWidth)
        {
            float width = fullWidth - (spacing * 3f);
            return width * referenceTypeRatio;
        }

        private static float BuildCalculateModeWidth(float fullWidth)
        {
            float width = fullWidth - (spacing * 3f);
            return width * modeWidthRatio;
        }

        private static float BuildCalculateValueWidth(float fullWidth)
        {
            float width = fullWidth - (spacing * 3f);
            float typeWidth = width * typeWidthRatio;
            float referenceTypeWidth = width * referenceTypeRatio;
            float modeWidth = width * modeWidthRatio;
            return width - typeWidth - referenceTypeWidth - modeWidth;
        }

        private static void BuildDrawReferenceValue(Rect rect, EntryFields fields)
        {
            SerializedProperty selected = BuildIsLabelReference(fields.ReferenceType) ? fields.LabelReference : fields.AssetReference;
            BuildDrawWithoutLabel(rect, selected);
        }

        private static bool BuildIsLabelReference(SerializedProperty referenceType)
        {
            return referenceType.enumValueIndex == (int)PreloadReferenceType.LabelReference;
        }

        private static void BuildDrawWithoutLabel(Rect rect, SerializedProperty property)
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



