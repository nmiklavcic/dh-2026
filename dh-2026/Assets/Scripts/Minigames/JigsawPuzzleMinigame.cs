using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class JigsawPuzzleMinigame : MonoBehaviour
{
    public static JigsawPuzzleMinigame Instance { get; private set; }

    [SerializeField] private Color correctPlacementColor = Color.green;
    [SerializeField] private Color pieceColor = Color.white;
    [SerializeField] private Color pieceOutlineColor = Color.black;

    private const int GRID_WIDTH = 4;
    private const int GRID_HEIGHT = 4;
    private const float CELL_WIDTH = 100f;
    private const float CELL_HEIGHT = 100f;
    private const float EDGE_MARGIN = 18f;
    private const float SNAP_DISTANCE = 30f;
    private const int EDGE_SEGMENTS = 6;
    private const float EDGE_JITTER = 16f;

    private VisualElement puzzlePanel;
    private VisualElement puzzleContainer;
    private VisualElement piecesContainer;

    private readonly List<PuzzlePiece> pieces = new List<PuzzlePiece>();
    private int piecesPlaced;
    private bool isGameActive;

    private PuzzlePiece draggedPiece;
    private Vector2 dragOffset;
    private Vector2 boardOrigin;

    private JaggedEdgeProfile[,] verticalSeams;
    private JaggedEdgeProfile[,] horizontalSeams;

    private event Action gameCompletedEvent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        AutoSetup();
    }

    private void AutoSetup()
    {
        var uiDoc = GetComponentInParent<UIDocument>() ?? FindAnyObjectByType<UIDocument>();
        if (uiDoc == null)
        {
            Debug.LogError("No UIDocument found.");
            return;
        }

        var root = uiDoc.rootVisualElement;
        puzzlePanel = root.Q("PuzzlePanel");
        if (puzzlePanel == null)
        {
            Debug.LogError("PuzzlePanel not found.");
            return;
        }

        puzzleContainer = puzzlePanel.Q("PuzzleContainer");
        if (puzzleContainer == null)
        {
            puzzleContainer = new VisualElement
            {
                name = "PuzzleContainer"
            };
            puzzleContainer.style.width = Length.Percent(100);
            puzzleContainer.style.height = Length.Percent(100);
            puzzleContainer.style.backgroundColor = Color.black;
            puzzleContainer.style.position = Position.Relative;
            puzzlePanel.Add(puzzleContainer);
        }

        piecesContainer = new VisualElement
        {
            name = "PiecesContainer"
        };
        piecesContainer.style.position = Position.Absolute;
        piecesContainer.style.left = 0;
        piecesContainer.style.top = 0;
        piecesContainer.style.right = 0;
        piecesContainer.style.bottom = 0;
        piecesContainer.style.overflow = Overflow.Hidden;
        puzzleContainer.Add(piecesContainer);

        root.RegisterCallback<PointerDownEvent>(OnPointerDown);
        root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        root.RegisterCallback<PointerUpEvent>(OnPointerUp);

        puzzleContainer.RegisterCallback<GeometryChangedEvent>(OnContainerReady);
    }

    private void OnContainerReady(GeometryChangedEvent evt)
    {
        puzzleContainer.UnregisterCallback<GeometryChangedEvent>(OnContainerReady);
        GeneratePuzzlePieces();
        isGameActive = true;
    }

    private void Update()
    {
    }

    private void GeneratePuzzlePieces()
    {
        piecesContainer?.Clear();
        pieces.Clear();
        piecesPlaced = 0;
        draggedPiece = default;

        float containerWidth = puzzleContainer.resolvedStyle.width;
        float containerHeight = puzzleContainer.resolvedStyle.height;

        if (containerWidth < 1f || containerHeight < 1f)
        {
            var layout = puzzleContainer.panel?.visualTree.layout ?? Rect.zero;
            containerWidth = layout.width;
            containerHeight = layout.height;
        }

        if (containerWidth < 1f || containerHeight < 1f)
        {
            Debug.LogWarning("Puzzle container has no valid size yet.");
            return;
        }

        float boardWidth = GRID_WIDTH * CELL_WIDTH;
        float boardHeight = GRID_HEIGHT * CELL_HEIGHT;
        boardOrigin = new Vector2(
            Mathf.Max(EDGE_MARGIN, (containerWidth - boardWidth) * 0.5f),
            Mathf.Max(EDGE_MARGIN, (containerHeight - boardHeight) * 0.18f));

        BuildSharedSeams();

        List<int> shuffledIndices = new List<int>();
        for (int i = 0; i < GRID_WIDTH * GRID_HEIGHT; i++)
        {
            shuffledIndices.Add(i);
        }

        for (int i = shuffledIndices.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            (shuffledIndices[i], shuffledIndices[swapIndex]) = (shuffledIndices[swapIndex], shuffledIndices[i]);
        }

        float fullPieceWidth = CELL_WIDTH + EDGE_MARGIN * 2f;
        float fullPieceHeight = CELL_HEIGHT + EDGE_MARGIN * 2f;

        for (int i = 0; i < shuffledIndices.Count; i++)
        {
            int gridIndex = shuffledIndices[i];
            int row = gridIndex / GRID_WIDTH;
            int column = gridIndex % GRID_WIDTH;

            Vector2 correctPosition = boardOrigin + new Vector2(column * CELL_WIDTH, row * CELL_HEIGHT) - new Vector2(EDGE_MARGIN, EDGE_MARGIN);
            Vector2 startPosition = CreateScatterPosition(containerWidth, containerHeight, fullPieceWidth, fullPieceHeight, correctPosition);

            CreatePuzzlePiece(row, column, gridIndex, startPosition, correctPosition, fullPieceWidth, fullPieceHeight);
        }
    }

    private void BuildSharedSeams()
    {
        verticalSeams = new JaggedEdgeProfile[GRID_HEIGHT, GRID_WIDTH - 1];
        horizontalSeams = new JaggedEdgeProfile[GRID_HEIGHT - 1, GRID_WIDTH];

        var seamRandom = new System.Random(UnityEngine.Random.Range(1, int.MaxValue));

        for (int row = 0; row < GRID_HEIGHT; row++)
        {
            for (int column = 0; column < GRID_WIDTH - 1; column++)
            {
                verticalSeams[row, column] = CreateJaggedProfile(seamRandom);
            }
        }

        for (int row = 0; row < GRID_HEIGHT - 1; row++)
        {
            for (int column = 0; column < GRID_WIDTH; column++)
            {
                horizontalSeams[row, column] = CreateJaggedProfile(seamRandom);
            }
        }
    }

    private static JaggedEdgeProfile CreateJaggedProfile(System.Random seamRandom)
    {
        float[] offsets = new float[EDGE_SEGMENTS + 1];
        offsets[0] = 0f;
        offsets[EDGE_SEGMENTS] = 0f;

        float drift = 0f;
        int accentIndex = seamRandom.Next(1, EDGE_SEGMENTS);

        for (int i = 1; i < EDGE_SEGMENTS; i++)
        {
            float t = i / (float)EDGE_SEGMENTS;
            float step = ((float)seamRandom.NextDouble() * 2f - 1f) * EDGE_JITTER * 0.9f;
            drift = Mathf.Clamp(drift + step, -EDGE_JITTER, EDGE_JITTER);

            float taper = Mathf.Sin(t * Mathf.PI);
            float offset = drift * taper;

            if (i == accentIndex)
            {
                offset += Mathf.Sign(offset == 0f ? ((float)seamRandom.NextDouble() - 0.5f) : offset) * EDGE_JITTER * 0.45f;
            }

            offsets[i] = Mathf.Clamp(offset, -EDGE_JITTER, EDGE_JITTER);
        }

        return new JaggedEdgeProfile(offsets);
    }

    private Vector2 CreateScatterPosition(float containerWidth, float containerHeight, float pieceWidth, float pieceHeight, Vector2 correctPosition)
    {
        float maxX = Mathf.Max(0f, containerWidth - pieceWidth);
        float maxY = Mathf.Max(0f, containerHeight - pieceHeight);
        Rect boardRect = new Rect(
            boardOrigin - new Vector2(EDGE_MARGIN, EDGE_MARGIN),
            new Vector2(GRID_WIDTH * CELL_WIDTH + EDGE_MARGIN * 2f, GRID_HEIGHT * CELL_HEIGHT + EDGE_MARGIN * 2f));

        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector2 candidate = new Vector2(
                UnityEngine.Random.Range(0f, maxX),
                UnityEngine.Random.Range(0f, maxY));

            Rect candidateRect = new Rect(candidate, new Vector2(pieceWidth, pieceHeight));
            if (!candidateRect.Overlaps(boardRect) && Vector2.Distance(candidate, correctPosition) > CELL_WIDTH * 0.75f)
            {
                return candidate;
            }
        }

        return new Vector2(
            UnityEngine.Random.Range(0f, maxX),
            UnityEngine.Random.Range(0f, maxY));
    }

    private void CreatePuzzlePiece(int row, int column, int gridIndex, Vector2 position, Vector2 correctPosition, float width, float height)
    {
        JaggedEdgeProfile topEdge = row == 0 ? JaggedEdgeProfile.Flat(EDGE_SEGMENTS) : horizontalSeams[row - 1, column];
        JaggedEdgeProfile rightEdge = column == GRID_WIDTH - 1 ? JaggedEdgeProfile.Flat(EDGE_SEGMENTS) : verticalSeams[row, column];
        JaggedEdgeProfile bottomEdge = row == GRID_HEIGHT - 1 ? JaggedEdgeProfile.Flat(EDGE_SEGMENTS) : horizontalSeams[row, column];
        JaggedEdgeProfile leftEdge = column == 0 ? JaggedEdgeProfile.Flat(EDGE_SEGMENTS) : verticalSeams[row, column - 1];

        var pieceElement = new JaggedPuzzlePiece(
            CELL_WIDTH,
            CELL_HEIGHT,
            EDGE_MARGIN,
            topEdge,
            rightEdge,
            bottomEdge,
            leftEdge,
            pieceColor,
            pieceOutlineColor);

        pieceElement.name = $"Piece_{gridIndex}";
        pieceElement.style.position = Position.Absolute;
        pieceElement.style.left = position.x;
        pieceElement.style.top = position.y;

        piecesContainer.Add(pieceElement);

        pieces.Add(new PuzzlePiece
        {
            element = pieceElement,
            gridIndex = gridIndex,
            currentPosition = position,
            correctPosition = correctPosition,
            width = width,
            height = height,
            isPlaced = false
        });
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (!isGameActive || evt.button != 0)
        {
            return;
        }

        Vector2 clickPos = piecesContainer.WorldToLocal(evt.position);

        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            if (pieces[i].isPlaced)
            {
                continue;
            }

            PuzzlePiece piece = pieces[i];
            Rect pieceRect = new Rect(piece.currentPosition, new Vector2(piece.width, piece.height));
            if (!pieceRect.Contains(clickPos))
            {
                continue;
            }

            draggedPiece = piece;
            dragOffset = clickPos - piece.currentPosition;
            draggedPiece.element.SetDragged(true);

            draggedPiece.element.RemoveFromHierarchy();
            piecesContainer.Add(draggedPiece.element);
            break;
        }
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!isGameActive || draggedPiece.element == null)
        {
            return;
        }

        Vector2 mousePos = piecesContainer.WorldToLocal(evt.position);
        Vector2 newPosition = mousePos - dragOffset;

        draggedPiece.currentPosition = newPosition;
        draggedPiece.element.style.left = newPosition.x;
        draggedPiece.element.style.top = newPosition.y;

        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].element == draggedPiece.element)
            {
                pieces[i] = draggedPiece;
                break;
            }
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (draggedPiece.element == null)
        {
            return;
        }

        draggedPiece.element.SetDragged(false);
        Vector2 distToCorrect = draggedPiece.currentPosition - draggedPiece.correctPosition;

        if (distToCorrect.magnitude < SNAP_DISTANCE)
        {
            draggedPiece.currentPosition = draggedPiece.correctPosition;
            draggedPiece.element.style.left = draggedPiece.correctPosition.x;
            draggedPiece.element.style.top = draggedPiece.correctPosition.y;
            draggedPiece.element.SetPlaced(correctPlacementColor);
            draggedPiece.isPlaced = true;

            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i].element == draggedPiece.element)
                {
                    pieces[i] = draggedPiece;
                    piecesPlaced++;
                    break;
                }
            }

            UIManager.Instance?.ShowMinigameProgress(piecesPlaced, pieces.Count);

            if (piecesPlaced >= GRID_WIDTH * GRID_HEIGHT)
            {
                CompleteGame();
            }
        }

        draggedPiece = default;
    }

    private void CompleteGame()
    {
        isGameActive = false;

        if (puzzleContainer != null)
        {
            puzzleContainer.RemoveFromHierarchy();
        }

        UIManager.Instance?.ShowMinigameCompletion("Puzzle solved!");
        gameCompletedEvent?.Invoke();
        Destroy(gameObject);
    }

    public void StartGame(Action onComplete = null)
    {
        UIManager.Instance?.ShowPuzzle();

        if (onComplete != null)
        {
            gameCompletedEvent += onComplete;
        }
    }

    public void ResetGame()
    {
        foreach (PuzzlePiece piece in pieces)
        {
            if (piece.element != null)
            {
                piece.element.RemoveFromHierarchy();
            }
        }

        pieces.Clear();
        piecesPlaced = 0;
        draggedPiece = default;
        UIManager.Instance?.ClearMinigameFeedback();

        if (puzzleContainer != null && puzzleContainer.resolvedStyle.width > 0)
        {
            GeneratePuzzlePieces();
            isGameActive = true;
        }
    }

    private struct PuzzlePiece
    {
        public JaggedPuzzlePiece element;
        public int gridIndex;
        public Vector2 currentPosition;
        public Vector2 correctPosition;
        public float width;
        public float height;
        public bool isPlaced;
    }
}

public readonly struct JaggedEdgeProfile
{
    public float[] Offsets { get; }

    public JaggedEdgeProfile(float[] offsets)
    {
        Offsets = offsets;
    }

    public static JaggedEdgeProfile Flat(int segments)
    {
        return new JaggedEdgeProfile(new float[segments + 1]);
    }
}

public class JaggedPuzzlePiece : VisualElement
{
    private readonly float cellWidth;
    private readonly float cellHeight;
    private readonly float margin;
    private readonly JaggedEdgeProfile topEdge;
    private readonly JaggedEdgeProfile rightEdge;
    private readonly JaggedEdgeProfile bottomEdge;
    private readonly JaggedEdgeProfile leftEdge;
    private readonly Color fillColor;

    private readonly Vector2[] polygonPoints;
    private Color outlineColor;

    public JaggedPuzzlePiece(
        float cellWidth,
        float cellHeight,
        float margin,
        JaggedEdgeProfile topEdge,
        JaggedEdgeProfile rightEdge,
        JaggedEdgeProfile bottomEdge,
        JaggedEdgeProfile leftEdge,
        Color fillColor,
        Color outlineColor)
    {
        this.cellWidth = cellWidth;
        this.cellHeight = cellHeight;
        this.margin = margin;
        this.topEdge = topEdge;
        this.rightEdge = rightEdge;
        this.bottomEdge = bottomEdge;
        this.leftEdge = leftEdge;
        this.fillColor = fillColor;
        this.outlineColor = outlineColor;

        style.width = cellWidth + margin * 2f;
        style.height = cellHeight + margin * 2f;
        style.backgroundColor = Color.clear;

        polygonPoints = BuildPolygon();
        generateVisualContent += OnGenerateVisualContent;
    }

    public void SetDragged(bool isDragged)
    {
        style.opacity = isDragged ? 0.82f : 1f;
    }

    public void SetPlaced(Color placedOutlineColor)
    {
        outlineColor = placedOutlineColor;
        MarkDirtyRepaint();
    }

    private void OnGenerateVisualContent(MeshGenerationContext context)
    {
        if (polygonPoints.Length == 0)
        {
            return;
        }

        var painter = context.painter2D;
        painter.fillColor = fillColor;
        painter.strokeColor = outlineColor;
        painter.lineWidth = 2f;

        painter.BeginPath();
        painter.MoveTo(polygonPoints[0]);

        for (int i = 1; i < polygonPoints.Length; i++)
        {
            painter.LineTo(polygonPoints[i]);
        }

        painter.ClosePath();
        painter.Fill();
        painter.Stroke();
    }

    private Vector2[] BuildPolygon()
    {
        List<Vector2> points = new List<Vector2>();

        AppendHorizontal(points, topEdge, margin, margin, cellWidth, false);
        AppendVertical(points, rightEdge, margin + cellWidth, margin, cellHeight, false);
        AppendHorizontal(points, bottomEdge, margin, margin + cellHeight, cellWidth, true);
        AppendVertical(points, leftEdge, margin, margin, cellHeight, true);

        return points.ToArray();
    }

    private static void AppendHorizontal(List<Vector2> points, JaggedEdgeProfile edge, float startX, float baseY, float length, bool reverse)
    {
        float[] offsets = edge.Offsets;
        int lastIndex = offsets.Length - 1;

        if (reverse)
        {
            for (int i = lastIndex; i >= 0; i--)
            {
                float t = i / (float)lastIndex;
                AddPoint(points, new Vector2(startX + t * length, baseY + offsets[i]));
            }
        }
        else
        {
            for (int i = 0; i <= lastIndex; i++)
            {
                float t = i / (float)lastIndex;
                AddPoint(points, new Vector2(startX + t * length, baseY + offsets[i]));
            }
        }
    }

    private static void AppendVertical(List<Vector2> points, JaggedEdgeProfile edge, float baseX, float startY, float length, bool reverse)
    {
        float[] offsets = edge.Offsets;
        int lastIndex = offsets.Length - 1;

        if (reverse)
        {
            for (int i = lastIndex; i >= 0; i--)
            {
                float t = i / (float)lastIndex;
                AddPoint(points, new Vector2(baseX + offsets[i], startY + t * length));
            }
        }
        else
        {
            for (int i = 0; i <= lastIndex; i++)
            {
                float t = i / (float)lastIndex;
                AddPoint(points, new Vector2(baseX + offsets[i], startY + t * length));
            }
        }
    }

    private static void AddPoint(List<Vector2> points, Vector2 point)
    {
        if (points.Count > 0 && Vector2.Distance(points[points.Count - 1], point) < 0.01f)
        {
            return;
        }

        points.Add(point);
    }
}