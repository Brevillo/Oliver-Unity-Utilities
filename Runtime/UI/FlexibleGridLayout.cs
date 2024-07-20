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
    public enum Constraint
    {
        [Tooltip("Same number of rows and columns")]
        Uniform,
        [Tooltip("Matches the width of the rect transform")]
        MatchWidth,
        [Tooltip("Matches the height of the rect transform")]
        MatchHeight,
        [Tooltip("Choose a fixed number of rows")]
        FixedRows,
        [Tooltip("Choose a fixed number of columns")]
        FixedColumns,
    }

    [Tooltip("Spacing between each grid cell")]
    public Vector2 spacing;
    [Tooltip("Constraint for grid layout")]
    public Constraint constraint = Constraint.Uniform;

    [Tooltip("Should the cells be stretched to match the width?")]
    public bool matchWidth;
    [Tooltip("Width of each grid cell")]
    public float cellWidth;
    [Tooltip("Should the cells be stretched to match the height?")]
    public bool matchHeight;
    [Tooltip("Height of each grid cell")]
    public float cellHeight;

    [Tooltip("Fixed number of rows")]
    public int rows;
    [Tooltip("Fixed number of columns")]
    public int columns;

    [SerializeField]
    private Constraint previousConstraint;

    public event Action LayoutUpdated;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();

        if (!(previousConstraint == Constraint.FixedRows || previousConstraint == Constraint.FixedColumns)
            && (constraint == Constraint.FixedRows || constraint == Constraint.FixedColumns))
        {
            matchWidth = true;
            matchHeight = true;
        }

        previousConstraint = constraint;

        if (rows <= 0)
        {
            rows = 1;
        }

        if (columns <= 0)
        {
            columns = 1;
        }

        if (constraint == Constraint.MatchWidth || constraint == Constraint.MatchHeight || constraint == Constraint.Uniform)
        {
            float squareRoot = Mathf.Sqrt(transform.childCount);
            rows = columns = Mathf.CeilToInt(squareRoot);

            (matchWidth, matchHeight) = constraint switch
            {
                Constraint.MatchWidth => (true, false),
                Constraint.MatchHeight => (false, true),
                Constraint.Uniform => (true, true),
                _ => throw new NotImplementedException(),
            };
        }

        if (constraint == Constraint.MatchWidth || constraint == Constraint.FixedColumns)
        {
            rows = Mathf.CeilToInt((float)transform.childCount / columns);
        }

        if (constraint == Constraint.MatchHeight || constraint == Constraint.FixedRows)
        {
            columns = Mathf.CeilToInt((float)transform.childCount / rows);
        }

        if (matchWidth)
        {
            float parentWidth = rectTransform.rect.width;
            cellWidth = parentWidth / columns - (spacing.x / columns * (columns - 1))
                - ((float)padding.left / columns) - ((float)padding.right / columns);
        }

        if (matchHeight)
        {
            float parentHeight = rectTransform.rect.height;
            cellHeight = parentHeight / rows - (spacing.y / rows * (rows - 1))
                - ((float)padding.top / rows) - ((float)padding.bottom / rows); ;
        }

        for (int i = 0; i < rectChildren.Count; i++)
        {
            int rowCount = i / columns;
            int columnCount = i % columns;

            var item = rectChildren[i];

            float xPos = (cellWidth * columnCount) + (spacing.x * columnCount) + padding.left;
            float yPos = (cellHeight * rowCount) + (spacing.y * rowCount) + padding.top;

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

            paddingProp     = Find(nameof(m_Padding));
            spacingProp     = Find(nameof(spacing));
            constraintProp  = Find(nameof(constraint));
            matchWidthProp  = Find(nameof(matchWidth));
            cellWidthProp   = Find(nameof(cellWidth));
            matchHeightProp = Find(nameof(matchHeight));
            cellHeightProp  = Find(nameof(cellHeight));
            rowsProp        = Find(nameof(rows));
            columnsProp     = Find(nameof(columns));
        }

        private static void Property(SerializedProperty property, bool enabled = true)
        {
            if (enabled) EditorGUILayout.PropertyField(property);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Property(paddingProp);
            Property(spacingProp);
            Property(constraintProp);

            var grid = target as FlexibleGridLayout;
            bool showMatchWidthHeight = grid.constraint == Constraint.FixedRows || grid.constraint == Constraint.FixedColumns;

            Property(matchWidthProp, showMatchWidthHeight);
            Property(cellWidthProp, !grid.matchWidth);
            Property(matchHeightProp, showMatchWidthHeight);
            Property(cellHeightProp, !grid.matchHeight);

            Property(rowsProp, grid.constraint == Constraint.FixedRows);
            Property(columnsProp, grid.constraint == Constraint.FixedColumns);

            serializedObject.ApplyModifiedProperties();
        }
    }

    #endif
    #endregion
}
