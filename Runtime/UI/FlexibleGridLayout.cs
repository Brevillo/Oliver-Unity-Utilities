/* FlexibleGridLayout.cs
 * From: Game Dev Guide - Fixing Grid Layouts in Unity With a Flexible Grid Component
 * Created: June 2020, NowWeWake
 * Modified: Oliver Beebe, tweaked and added custom editor
 */

using UnityEngine;
using UnityEngine.UI;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FlexibleGridLayout : LayoutGroup
{
    public enum Corner
    {
        UpperLeft = 0,
        UpperRight = 1,
        LowerLeft = 2,
        LowerRight = 3,
    }

    public enum Axis
    {
        Horizontal = 0,
        Vertical = 1,
    }

    public enum Constraint
    {
        [Tooltip("Calculates rows and columns to make smallest grid size.")]
        Tight = 0,
        [Tooltip("Same number of rows and columns")]
        Uniform = 1,
        [Tooltip("Choose a fixed number of rows")]
        FixedRows = 2,
        [Tooltip("Choose a fixed number of columns")]
        FixedColumns = 3,
    }

    [Tooltip("Spacing between each grid cell")]
    public Vector2 spacing = Vector2.zero;
    [Tooltip("Corner to start laying out children from")]
    public Corner startCorner = Corner.UpperLeft;
    [Tooltip("Axis to start laying out children along")]
    public Axis startAxis = Axis.Horizontal;
    [Tooltip("Constraint for grid layout")]
    public Constraint constraint = Constraint.Uniform;

    [Tooltip("Should the cells be stretched to match the width?")]
    public bool matchWidth = true;
    [Tooltip("Width of each grid cell")]
    public float cellWidth;
    [Tooltip("Should the cells be stretched to match the height?")]
    public bool matchHeight = true;
    [Tooltip("Height of each grid cell")]
    public float cellHeight;

    [Tooltip("Number of rows")]
    public int rows;
    [Tooltip("Number of columns")]
    public int columns;

    public event Action LayoutUpdated;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();

        if (rows <= 0)
        {
            rows = 1;
        }

        if (columns <= 0)
        {
            columns = 1;
        }

        if (constraint == Constraint.Uniform)
        {
            float count = transform.childCount;
            float squareRoot = Mathf.Sqrt(count);
            rows = columns = Mathf.CeilToInt(squareRoot);
        }
        else if (constraint == Constraint.Tight)
        {
            float count = transform.childCount;
            float squareRoot = Mathf.Sqrt(count);
            int greatestFactor = Mathf.CeilToInt(squareRoot);
            int secondGreatestFactor = Mathf.CeilToInt(count / greatestFactor);

            (columns, rows) = startAxis == Axis.Horizontal
                ? (greatestFactor, secondGreatestFactor)
                : (secondGreatestFactor, greatestFactor);
        }

        if (constraint == Constraint.FixedColumns)
        {
            rows = Mathf.CeilToInt((float)transform.childCount / columns);
        }

        if (constraint == Constraint.FixedRows)
        {
            columns = Mathf.CeilToInt((float)transform.childCount / rows);
        }

        if (matchWidth)
        {
            float parentWidth = rectTransform.rect.width;
            float totalSpacing = spacing.x * (columns - 1);
            float gridWidthForCells = parentWidth - totalSpacing - padding.horizontal;

            cellWidth = gridWidthForCells / columns;
        }

        if (matchHeight)
        {
            float parentHeight = rectTransform.rect.height;
            float totalSpacing = spacing.y * (rows - 1);
            float gridHeightForCells = parentHeight - totalSpacing - padding.vertical;

            cellHeight = gridHeightForCells / rows;
        }

        float startOffsetX = GetStartOffset(0, columns * cellWidth + (columns - 1) * spacing.x);
        float startOffsetY = GetStartOffset(1, rows * cellHeight + (rows - 1) * spacing.y);

        int cornerX = (int)startCorner % 2;
        int cornerY = (int)startCorner / 2;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            (int xIndex, int yIndex) = startAxis == Axis.Horizontal
                ? (i % columns, i / columns)
                : (i / rows, i % rows);

            if (cornerX == 1)
            {
                xIndex = columns - 1 - xIndex;
            }

            if (cornerY == 1)
            {
                yIndex = rows - 1 - yIndex;
            }

            float xPos = startOffsetX + (cellWidth * xIndex) + (spacing.x * xIndex);
            float yPos = startOffsetY + (cellHeight * yIndex) + (spacing.y * yIndex);

            var item = rectChildren[i];

            SetChildAlongAxis(item, 0, xPos, cellWidth);
            SetChildAlongAxis(item, 1, yPos, cellHeight);
        }

        LayoutUpdated?.Invoke();
    }

    public override void CalculateLayoutInputVertical() { }
    public override void SetLayoutHorizontal() { }
    public override void SetLayoutVertical() { }

    #region Editor
    #if UNITY_EDITOR

    [CustomEditor(typeof(FlexibleGridLayout))]
    private class FlexibleGridLayoutEditor : Editor
    {
        private SerializedProperty
            paddingProp,
            spacingProp,
            childAlignmentProp,
            startCornerProp,
            startAxisProp,
            constraintProp,
            matchWidthProp,
            cellWidthProp,
            matchHeightProp,
            cellHeightProp,
            rowsProp,
            columnsProp;

        private void OnEnable()
        {
            SerializedProperty Find(string name) => serializedObject.FindProperty(name);

            paddingProp         = Find(nameof(m_Padding));
            spacingProp         = Find(nameof(spacing));
            childAlignmentProp  = Find(nameof(m_ChildAlignment));
            startCornerProp     = Find(nameof(startCorner));
            startAxisProp       = Find(nameof(startAxis));
            constraintProp      = Find(nameof(constraint));
            matchWidthProp      = Find(nameof(matchWidth));
            cellWidthProp       = Find(nameof(cellWidth));
            matchHeightProp     = Find(nameof(matchHeight));
            cellHeightProp      = Find(nameof(cellHeight));
            rowsProp            = Find(nameof(rows));
            columnsProp         = Find(nameof(columns));
        }

        private static void Property(SerializedProperty property, bool enabled = true)
        {
            if (enabled) EditorGUILayout.PropertyField(property);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            Property(paddingProp);
            Property(spacingProp);
            Property(startCornerProp);
            Property(startAxisProp);
            Property(childAlignmentProp);
            Property(constraintProp);

            var grid = target as FlexibleGridLayout;

            Property(rowsProp, grid.constraint == Constraint.FixedRows);
            Property(columnsProp, grid.constraint == Constraint.FixedColumns);

            Property(matchWidthProp);
            Property(cellWidthProp, !grid.matchWidth);
            Property(matchHeightProp);
            Property(cellHeightProp, !grid.matchHeight);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }

    #endif
    #endregion
}
