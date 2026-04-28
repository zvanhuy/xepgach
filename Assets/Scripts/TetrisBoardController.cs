using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class TetrisBoardController : MonoBehaviour
{
    private enum TetrominoType
    {
        O, I, T, L, J, S, Z
    }

    [Header("Board Size")]
    [SerializeField] private int columns = 10;
    [SerializeField] private int rows = 20;

    [Header("References")]
    [SerializeField] private RectTransform backgroundGridRoot;
    [SerializeField] private RectTransform cellsRoot;
    [SerializeField] private RectTransform activePieceRoot;
    [SerializeField] private Image cellPrefab;

    [Header("Top UI")]
    [SerializeField] private TextMeshProUGUI scoreValueText;
    [SerializeField] private TextMeshProUGUI linesValueText;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreValueText;
    [SerializeField] private TextMeshProUGUI finalLinesValueText;

    [Header("Falling")]
    [SerializeField] private float fallInterval = 0.7f;

    private GridLayoutGroup backgroundGridLayout;

    private bool[,] occupied;
    private Image[,] lockedCellImages;

    private readonly List<Image> activeCellImages = new List<Image>();

    private TetrominoType activeType;
    private Vector2Int activeOrigin;
    private Vector2Int[] activeShape;
    private Color activeColor;

    private float fallTimer;
    private bool isGameOver;

    private int score;
    private int lines;

    private void Awake()
    {
        backgroundGridLayout = backgroundGridRoot.GetComponent<GridLayoutGroup>();

        occupied = new bool[columns, rows];
        lockedCellImages = new Image[columns, rows];
    }

    private void Start()
    {
        if (backgroundGridLayout == null)
        {
            Debug.LogError("BackgroundGridRoot chưa có GridLayoutGroup.", this);
            enabled = false;
            return;
        }

        HideGameOverUI();
        UpdateUI();
        SpawnRandomPiece();
    }

    private void Update()
    {
        HandleInput();

        if (isGameOver)
            return;

        fallTimer += Time.deltaTime;
        if (fallTimer >= fallInterval)
        {
            fallTimer = 0f;
            StepDown();
        }
    }

    private void HandleInput()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartBoard();
            return;
        }

        if (isGameOver)
            return;

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
        {
            TryMove(new Vector2Int(-1, 0));
        }

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
        {
            TryMove(new Vector2Int(1, 0));
        }

        if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
        {
            StepDown();
            fallTimer = 0f;
        }

        if (Keyboard.current.upArrowKey.wasPressedThisFrame ||
            Keyboard.current.wKey.wasPressedThisFrame ||
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TryRotateClockwise();
        }
    }

    private void StepDown()
    {
        bool moved = TryMove(new Vector2Int(0, -1));

        if (!moved)
        {
            LockActivePiece();

            int clearedLines = ClearCompletedLines();
            if (clearedLines > 0)
            {
                AddScoreAndLines(clearedLines);
            }

            SpawnRandomPiece();
        }
    }

    private bool TryMove(Vector2Int delta)
    {
        Vector2Int nextOrigin = activeOrigin + delta;

        if (!IsValidPosition(nextOrigin, activeShape))
            return false;

        activeOrigin = nextOrigin;
        RefreshActivePieceVisual();
        return true;
    }

    private void TryRotateClockwise()
    {
        if (activeType == TetrominoType.O)
            return;

        Vector2Int[] rotatedShape = RotateClockwise(activeShape);

        Vector2Int[] kickTests = new Vector2Int[]
        {
            Vector2Int.zero,
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(-2, 0),
            new Vector2Int(2, 0),
            new Vector2Int(0, 1)
        };

        for (int i = 0; i < kickTests.Length; i++)
        {
            Vector2Int testOrigin = activeOrigin + kickTests[i];

            if (IsValidPosition(testOrigin, rotatedShape))
            {
                activeOrigin = testOrigin;
                activeShape = rotatedShape;
                RefreshActivePieceVisual();
                return;
            }
        }
    }

    private Vector2Int[] RotateClockwise(Vector2Int[] shape)
    {
        Vector2Int[] rotated = new Vector2Int[shape.Length];

        for (int i = 0; i < shape.Length; i++)
        {
            Vector2Int block = shape[i];
            rotated[i] = new Vector2Int(block.y, -block.x);
        }

        return rotated;
    }

    private void SpawnRandomPiece()
    {
        ClearActivePieceVisual();

        activeType = (TetrominoType)Random.Range(0, 7);
        activeShape = GetSpawnShape(activeType);
        activeColor = GetPieceColor(activeType);
        activeOrigin = GetSpawnOrigin(activeType);

        if (!IsValidPosition(activeOrigin, activeShape))
        {
            TriggerGameOver();
            return;
        }

        for (int i = 0; i < activeShape.Length; i++)
        {
            Image newCell = Instantiate(cellPrefab, activePieceRoot);
            newCell.color = activeColor;
            newCell.raycastTarget = false;
            activeCellImages.Add(newCell);
        }

        RefreshActivePieceVisual();
    }

    private void TriggerGameOver()
    {
        isGameOver = true;
        ShowGameOverUI();
        Debug.Log("Game Over");
    }

    private Vector2Int GetSpawnOrigin(TetrominoType type)
    {
        return new Vector2Int(4, 18);
    }

    private Vector2Int[] GetSpawnShape(TetrominoType type)
    {
        switch (type)
        {
            case TetrominoType.O:
                return new Vector2Int[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1)
                };

            case TetrominoType.I:
                return new Vector2Int[]
                {
                    new Vector2Int(-1, 0),
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(2, 0)
                };

            case TetrominoType.T:
                return new Vector2Int[]
                {
                    new Vector2Int(-1, 0),
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, 1)
                };

            case TetrominoType.L:
                return new Vector2Int[]
                {
                    new Vector2Int(-1, 0),
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(1, 1)
                };

            case TetrominoType.J:
                return new Vector2Int[]
                {
                    new Vector2Int(-1, 0),
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(-1, 1)
                };

            case TetrominoType.S:
                return new Vector2Int[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(-1, 1),
                    new Vector2Int(0, 1)
                };

            case TetrominoType.Z:
                return new Vector2Int[]
                {
                    new Vector2Int(-1, 0),
                    new Vector2Int(0, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1)
                };
        }

        return new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1)
        };
    }

    private Color GetPieceColor(TetrominoType type)
    {
        switch (type)
        {
            case TetrominoType.O: return new Color32(255, 215, 70, 255);
            case TetrominoType.I: return new Color32(70, 220, 255, 255);
            case TetrominoType.T: return new Color32(180, 90, 255, 255);
            case TetrominoType.L: return new Color32(255, 150, 60, 255);
            case TetrominoType.J: return new Color32(70, 120, 255, 255);
            case TetrominoType.S: return new Color32(90, 220, 120, 255);
            case TetrominoType.Z: return new Color32(255, 90, 90, 255);
        }

        return Color.white;
    }

    private bool IsValidPosition(Vector2Int origin, Vector2Int[] shape)
    {
        for (int i = 0; i < shape.Length; i++)
        {
            Vector2Int boardPos = origin + shape[i];

            if (boardPos.x < 0 || boardPos.x >= columns)
                return false;

            if (boardPos.y < 0 || boardPos.y >= rows)
                return false;

            if (occupied[boardPos.x, boardPos.y])
                return false;
        }

        return true;
    }

    private void RefreshActivePieceVisual()
    {
        for (int i = 0; i < activeShape.Length; i++)
        {
            Vector2Int boardPos = activeOrigin + activeShape[i];
            PositionCell(activeCellImages[i].rectTransform, boardPos.x, boardPos.y);
            activeCellImages[i].color = activeColor;
        }
    }

    private void LockActivePiece()
    {
        for (int i = 0; i < activeShape.Length; i++)
        {
            Vector2Int boardPos = activeOrigin + activeShape[i];

            occupied[boardPos.x, boardPos.y] = true;

            Image lockedCell = Instantiate(cellPrefab, cellsRoot);
            lockedCell.color = activeColor;
            lockedCell.raycastTarget = false;

            PositionCell(lockedCell.rectTransform, boardPos.x, boardPos.y);
            lockedCellImages[boardPos.x, boardPos.y] = lockedCell;
        }

        ClearActivePieceVisual();
    }

    private int ClearCompletedLines()
    {
        int clearedCount = 0;

        for (int y = 0; y < rows; y++)
        {
            if (IsRowFull(y))
            {
                ClearRow(y);
                ShiftRowsDown(y);
                clearedCount++;
                y--;
            }
        }

        return clearedCount;
    }

    private bool IsRowFull(int row)
    {
        for (int x = 0; x < columns; x++)
        {
            if (!occupied[x, row])
                return false;
        }

        return true;
    }

    private void ClearRow(int row)
    {
        for (int x = 0; x < columns; x++)
        {
            occupied[x, row] = false;

            if (lockedCellImages[x, row] != null)
            {
                Destroy(lockedCellImages[x, row].gameObject);
                lockedCellImages[x, row] = null;
            }
        }
    }

    private void ShiftRowsDown(int clearedRow)
    {
        for (int y = clearedRow + 1; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                occupied[x, y - 1] = occupied[x, y];
                lockedCellImages[x, y - 1] = lockedCellImages[x, y];

                if (lockedCellImages[x, y - 1] != null)
                {
                    PositionCell(lockedCellImages[x, y - 1].rectTransform, x, y - 1);
                }
            }
        }

        for (int x = 0; x < columns; x++)
        {
            occupied[x, rows - 1] = false;
            lockedCellImages[x, rows - 1] = null;
        }
    }

    private void AddScoreAndLines(int clearedLines)
    {
        lines += clearedLines;
        score += clearedLines * 100;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreValueText != null)
            scoreValueText.text = score.ToString();

        if (linesValueText != null)
            linesValueText.text = lines.ToString();
    }

    private void ShowGameOverUI()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (finalScoreValueText != null)
            finalScoreValueText.text = score.ToString();

        if (finalLinesValueText != null)
            finalLinesValueText.text = lines.ToString();
    }

    private void HideGameOverUI()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void PositionCell(RectTransform cellRect, int x, int y)
    {
        float cellWidth = backgroundGridLayout.cellSize.x;
        float cellHeight = backgroundGridLayout.cellSize.y;

        float stepX = cellWidth + backgroundGridLayout.spacing.x;
        float stepY = cellHeight + backgroundGridLayout.spacing.y;

        int padLeft = backgroundGridLayout.padding.left;
        int padTop = backgroundGridLayout.padding.top;

        cellRect.anchorMin = new Vector2(0f, 1f);
        cellRect.anchorMax = new Vector2(0f, 1f);
        cellRect.pivot = new Vector2(0f, 1f);
        cellRect.localScale = Vector3.one;
        cellRect.localRotation = Quaternion.identity;
        cellRect.sizeDelta = new Vector2(cellWidth, cellHeight);

        float posX = padLeft + x * stepX;
        float posY = -(padTop + (rows - 1 - y) * stepY);

        cellRect.anchoredPosition = new Vector2(posX, posY);
    }

    private void ClearActivePieceVisual()
    {
        for (int i = 0; i < activeCellImages.Count; i++)
        {
            if (activeCellImages[i] != null)
            {
                Destroy(activeCellImages[i].gameObject);
            }
        }

        activeCellImages.Clear();
    }

    private void RestartBoard()
    {
        isGameOver = false;
        fallTimer = 0f;
        score = 0;
        lines = 0;

        UpdateUI();
        HideGameOverUI();

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                occupied[x, y] = false;

                if (lockedCellImages[x, y] != null)
                {
                    Destroy(lockedCellImages[x, y].gameObject);
                    lockedCellImages[x, y] = null;
                }
            }
        }

        ClearActivePieceVisual();
        SpawnRandomPiece();
    }
}