using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

[RequireComponent(typeof(FlexibleGridLayout))]
public class FlexibleGridLayoutAutoNavigationSetup : MonoBehaviour
{
    public WrapType horizontalWrapping;
    public WrapType verticalWrapping;

    public FlexibleGridLayout FlexibleGridLayout
    {
        get
        {
            if (flexibleGridLayout == null)
            {
                flexibleGridLayout = GetComponent<FlexibleGridLayout>();
            }

            return flexibleGridLayout;
        }
    }

    public enum WrapType
    {
        None,
        SameLine,
        NextLine,
        PreviousLine,
    }

    private FlexibleGridLayout flexibleGridLayout;

    private Selectable[] right, left, top, bottom;

    public Selectable[] Right => right;
    public Selectable[] Left => left;
    public Selectable[] Top => top;
    public Selectable[] Bottom => bottom;

    private void Awake()
    {
        FlexibleGridLayout.LayoutUpdated += SetupNavigation;
    }

    private void OnValidate()
    {
        if (Application.IsPlaying(this))
        {
            SetupNavigation();
        }
    }

    public void SetupNavigation()
    {
        var selectables = transform
            .OfType<Transform>()
            .Select(child => child.GetComponentInChildren<Selectable>())
            .ToArray();

        var grid = FlexibleGridLayout;
        int width = grid.columns;
        int height = grid.rows;
        int count = selectables.Length;

        List<Selectable>
            right = new(),
            left = new(),
            top = new(),
            bottom = new();

        int GetIndex(int x, int y) => (x + width) % width + (y + height) % height * width;

        for (int i = 0; i < count; i++)
        {
            int x = i % width;
            int y = i / width;

            int index = GetIndex(x, y);
            var selectable = selectables[i];

            if (selectable == null)
            {
                continue;
            }

            Selectable GetNavigationTarget(int x, int y, WrapType wrapType)
            {
                switch (wrapType)
                {
                    case WrapType.None:
                        x = Mathf.Clamp(x, 0, width - 1);
                        y = Mathf.Clamp(y, 0, height - 1);
                        break;

                    case WrapType.NextLine:
                    case WrapType.PreviousLine:

                        int delta = wrapType == WrapType.NextLine ? 1 : -1;

                        if (y == -1) x -= delta;
                        else if (y == height) x += delta;

                        if (x == -1) y -= delta;
                        else if (x == width) y += delta;

                        break;
                }

                int newIndex = GetIndex(x, y);
                int clampedIndex = Mathf.Min(newIndex, count - 1);

                if (clampedIndex == index)
                {
                    switch (wrapType)
                    {
                        case WrapType.None:
                            newIndex = clampedIndex;
                            break;

                        case WrapType.SameLine:
                            newIndex = count - index % width - 1;
                            break;

                        case WrapType.NextLine:
                            newIndex = 0;
                            break;

                        case WrapType.PreviousLine:
                            newIndex = count - index % width - width - 1;
                            break;
                    }
                }
                else
                {
                    newIndex = clampedIndex;
                }

                return selectables[newIndex];
            }

            selectable.navigation = new()
            {
                mode = Navigation.Mode.Explicit,
                selectOnRight = GetNavigationTarget(x + 1, y, horizontalWrapping),
                selectOnLeft  = GetNavigationTarget(x - 1, y, horizontalWrapping),
                selectOnDown  = GetNavigationTarget(x, y + 1, verticalWrapping),
                selectOnUp    = GetNavigationTarget(x, y - 1, verticalWrapping),
            };

            if (x == width - 1) right.Add(selectable);
            if (x == 0) left.Add(selectable);
            if (y == 0) top.Add(selectable);
            if (y == height - 1) bottom.Add(selectable);
        }


        this.right = right.ToArray();
        this.left = left.ToArray();
        this.top = top.ToArray();
        this.bottom = bottom.ToArray();
    }
}
