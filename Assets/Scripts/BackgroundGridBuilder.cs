using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class BackgroundGridBuilder : MonoBehaviour
{
    [Header("Board Size")]
    [SerializeField] private int columns = 10;
    [SerializeField] private int rows = 20;

    [Header("References")]
    [SerializeField] private Image cellPrefab;

    [Header("Layout")]
    [SerializeField] private float spacing = 2f;
    [SerializeField] private bool generateOnStart = true;

    private GridLayoutGroup grid;
    private RectTransform rectTransform;

    private void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    private IEnumerator Start()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        if (generateOnStart)
        {
            GenerateGrid();
        }
    }

    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        if (cellPrefab == null)
        {
            Debug.LogError("Chưa gán Cell Prefab.", this);
            return;
        }

        if (grid == null) grid = GetComponent<GridLayoutGroup>();
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        ClearChildren();
        Canvas.ForceUpdateCanvases();

        float areaWidth = rectTransform.rect.width;
        float areaHeight = rectTransform.rect.height;

        if (areaWidth <= 0 || areaHeight <= 0)
        {
            Debug.LogError("BackgroundGridRoot chưa có kích thước hợp lệ.", this);
            return;
        }

        float cellSize = Mathf.Floor(
            Mathf.Min(
                (areaWidth - spacing * (columns - 1)) / columns,
                (areaHeight - spacing * (rows - 1)) / rows
            )
        );

        if (cellSize <= 0)
        {
            Debug.LogError("Cell size không hợp lệ.", this);
            return;
        }

        float usedWidth = cellSize * columns + spacing * (columns - 1);
        float usedHeight = cellSize * rows + spacing * (rows - 1);

        int padLeft = Mathf.RoundToInt((areaWidth - usedWidth) * 0.5f);
        int padRight = Mathf.RoundToInt(areaWidth - usedWidth - padLeft);
        int padTop = Mathf.RoundToInt((areaHeight - usedHeight) * 0.5f);
        int padBottom = Mathf.RoundToInt(areaHeight - usedHeight - padTop);

        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.cellSize = new Vector2(cellSize, cellSize);
        grid.spacing = new Vector2(spacing, spacing);
        grid.padding = new RectOffset(padLeft, padRight, padTop, padBottom);

        int total = columns * rows;

        for (int i = 0; i < total; i++)
        {
            Image newCell = Instantiate(cellPrefab, transform, false);
            RectTransform cellRect = newCell.rectTransform;

            cellRect.anchorMin = new Vector2(0.5f, 0.5f);
            cellRect.anchorMax = new Vector2(0.5f, 0.5f);
            cellRect.pivot = new Vector2(0.5f, 0.5f);
            cellRect.localScale = Vector3.one;
            cellRect.localRotation = Quaternion.identity;

            newCell.raycastTarget = false;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private void OnValidate()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        spacing = Mathf.Max(0f, spacing);
    }
}