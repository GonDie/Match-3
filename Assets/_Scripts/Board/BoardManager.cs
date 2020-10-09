﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardManager : Singleton<BoardManager>
{
    const int PIECE_MATCH_THRESHOLD = 3;
    const float DRAG_DISTANCE_THRESHOLD = 0.25f;
    const float MOUSE_CLICK_THRESHOLD = 0.15f;

    public Transform piecesContainer;
    public Transform boardContainer;
    public GameObject boardPiecePrefab;
    public Transform[] spawnersTransform;

    [Header("Board Config")]
    [SerializeField] Vector2 _boardSize = new Vector2(8, 8);
    public Vector2 BoardSize { get => _boardSize; }
    [SerializeField] Vector2 _pieceSize = Vector2.one;
    Vector2 _spacing = Vector2.zero;

    BoardTile[,] _board;

    Camera _camera;

    MouseState _mouseState;
    BoardTile _selectedTile;

    float _mouseHoldTimer = 0f;

    #region Tests Properties & Vars
    [Header("Tests")]
    public bool displayNeighbor;
    public GameObject displayNeighborPrefab;
    Transform[] _displayNeighbors;
    #endregion

    protected override void Awake()
    {
        base.Awake();

        Events.OnGameStart += PrepareBoard;

        if(displayNeighbor)
            _displayNeighbors = new Transform[4] { Instantiate(displayNeighborPrefab).GetComponent<Transform>(), Instantiate(displayNeighborPrefab).GetComponent<Transform>(), Instantiate(displayNeighborPrefab).GetComponent<Transform>(), Instantiate(displayNeighborPrefab).GetComponent<Transform>() };
    }

    private void Start()
    {
        _camera = Camera.main;

        PrepareBoard();
    }

    protected override void OnDestroy()
    {
        base.Awake();

        Events.OnGameStart -= PrepareBoard;
    }

    private void Update()
    {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hitInfo = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity);
        List<BoardTile> matchesFrom = new List<BoardTile>();
        List<BoardTile> matchesTo = new List<BoardTile>();

        if (hitInfo.collider == null) return;

        switch (_mouseState)
        {
            case MouseState.Hovering:

                if (Input.GetMouseButtonDown(0))
                {
                    _mouseHoldTimer = 0f;
                    _mouseState = MouseState.DragSelectedPiece;
                    _selectedTile = hitInfo.collider.GetComponent<BoardTile>();
                }

            break;
            case MouseState.DragSelectedPiece:

                _mouseHoldTimer += Time.deltaTime;

                if (Input.GetMouseButtonUp(0))
                {
                    if (_mouseHoldTimer <= MOUSE_CLICK_THRESHOLD)
                    {
                        _mouseState = MouseState.ClickSelectedPiece;
                        return;
                    }

                    if (((Vector2)_selectedTile.Transform.position - hitInfo.point).magnitude < DRAG_DISTANCE_THRESHOLD)
                    {
                        _selectedTile = null;
                        _mouseState = MouseState.Hovering;
                        return;
                    }

                    float verticalDist = Mathf.Abs(_selectedTile.Transform.position.y - hitInfo.point.y);
                    float horizontalDist = Mathf.Abs(_selectedTile.Transform.position.x - hitInfo.point.x);

                    Direction dir;
                    if (verticalDist > horizontalDist) //TODO: Match Pieces
                        dir = _selectedTile.Transform.position.y > hitInfo.point.y ? Direction.Down : Direction.Up;
                    else
                        dir = _selectedTile.Transform.position.x > hitInfo.point.x ? Direction.Left : Direction.Right;

                    matchesFrom = CheckPossibleMatches(_selectedTile, _selectedTile.GetNeighbor(dir), _selectedTile.GetNeighbor(dir).MatchPiece.pieceType);
                    matchesTo = CheckPossibleMatches(_selectedTile.GetNeighbor(dir), _selectedTile, _selectedTile.MatchPiece.pieceType);
                    if (matchesFrom.Count >= PIECE_MATCH_THRESHOLD || matchesTo.Count >= PIECE_MATCH_THRESHOLD)
                    {
                        matchesFrom.AddRange(matchesTo);
                        ExchangeTilePieces(_selectedTile, _selectedTile.GetNeighbor(dir), () =>
                        {
                            KillPieces(matchesFrom);

                            _selectedTile = null;
                            _mouseState = MouseState.Hovering;
                        });
                    }
                    else
                    {
                        _selectedTile = null;
                        _mouseState = MouseState.Hovering;
                    }
                }

                break;
            case MouseState.ClickSelectedPiece:

                if (Input.GetMouseButtonDown(0))
                {
                    _mouseHoldTimer = 0f;
                    _mouseState = MouseState.ClickSelectedOtherPiece;
                }

            break;
            case MouseState.ClickSelectedOtherPiece:

                _mouseHoldTimer += Time.deltaTime;

                if (_mouseHoldTimer > MOUSE_CLICK_THRESHOLD)
                {
                    _mouseState = MouseState.ClickSelectedPiece;
                    return;
                }

                if (Input.GetMouseButtonUp(0))
                {
                    BoardTile otherTile = hitInfo.collider.GetComponent<BoardTile>();

                    if (_selectedTile == otherTile)
                    {
                        //TODO: Unselect
                        _mouseState = MouseState.Hovering;
                        Debug.Log("Deselect Piece");
                    }
                    else
                    {
                        Direction dir;
                        if (_selectedTile.IsNeighbor(otherTile, out dir))
                        {
                            //TODO: Match Pieces
                            matchesFrom = CheckPossibleMatches(_selectedTile, otherTile, otherTile.MatchPiece.pieceType);
                            matchesTo = CheckPossibleMatches(otherTile, _selectedTile, _selectedTile.MatchPiece.pieceType);
                            if (matchesFrom.Count >= PIECE_MATCH_THRESHOLD || matchesTo.Count >= PIECE_MATCH_THRESHOLD)
                            {
                                matchesFrom.AddRange(matchesTo);
                                ExchangeTilePieces(_selectedTile, otherTile, () =>
                                {
                                    KillPieces(matchesFrom);

                                    _selectedTile = null;
                                    _mouseState = MouseState.Hovering;
                                });
                            }
                            else
                            {
                                //TODO: Cant do this
                            }

                            _selectedTile = null;
                            _mouseState = MouseState.Hovering;
                        }
                        else
                        {
                            //TODO: Cant do this
                            _mouseState = MouseState.ClickSelectedPiece;
                        }
                    }
                }

            break;
            case MouseState.Standby:
            break;
        }

        #region Tests

        if (displayNeighbor && _displayNeighbors != null)
        {
            BoardTile testNeighborTile = hitInfo.collider.GetComponent<BoardTile>();

            for (int i = 0; i < (int)Direction.Count; i++)
            {
                _displayNeighbors[i].gameObject.SetActive(false);

                if (testNeighborTile.GetNeighbor((Direction)i) != null)
                {
                    _displayNeighbors[i].gameObject.SetActive(true);
                    _displayNeighbors[i].position = testNeighborTile.GetNeighbor((Direction)i).Transform.position;
                    _displayNeighbors[i].GetComponent<TMPro.TMP_Text>().text = ((Direction)i).ToString();
                }
            }
        }
        #endregion
    }

    void PrepareBoard()
    {
        _board = new BoardTile[(int)_boardSize.x, (int)_boardSize.y];

        BoardTile piece = null;
        Vector3 position = Vector3.zero;

        BoardTile up = null;
        BoardTile right = null;
        BoardTile down = null;
        BoardTile left = null;

        for (int row = 0; row < _boardSize.y; row++)
        {
            for (int col = 0; col < _boardSize.x; col++)
            {
                piece = Instantiate(boardPiecePrefab, boardContainer).GetComponent<BoardTile>();
                _board[row, col] = piece;
            }
        }

        for (int row = 0; row < _boardSize.y; row++)
        {
            for (int col = 0; col < _boardSize.x; col++)
            {
                position.x = -((((_pieceSize.x + _spacing.x) * _boardSize.x) / 2f) - (_pieceSize.x + _spacing.x) / 2f) + (_pieceSize.x + _spacing.x) * col;
                position.y = -((((_pieceSize.y + _spacing.y) * _boardSize.y) / 2f) - (_pieceSize.y + _spacing.y) / 2f) + (_pieceSize.y + _spacing.y) * row;

                if (col == 0) left = null;
                else left = _board[row, col - 1];

                if (col < _boardSize.x - 1) right = _board[row, col + 1];
                else right = null;

                if (row == 0) down = null;
                else down = _board[row - 1, col];

                if (row < _boardSize.y - 1) up = _board[row + 1, col];
                else up = null;

                _board[row, col].Init(position, new Vector2(row, col), up, right, down, left);

                spawnersTransform[col].position = new Vector3(boardContainer.position.x + position.x, spawnersTransform[col].position.y, spawnersTransform[col].position.z);
            }
        }

        FillBoard();
    }

    void FillBoard()
    {
        _mouseState = MouseState.Standby;

        for (int col = 0; col < (int)_boardSize.x; col++)
        {
            StartCoroutine(FillColumn(col));
        }

        StartCoroutine(WaitForBoardReady(() =>
        {
            CheckBoardMatches();
        }));
    }

    IEnumerator FillColumn(int columnIndex)
    {
        MatchPiece piece = null;
        BoardTile tile = null;
        for(int row = 0; row < (int)_boardSize.y; row++)
        {
            piece = null;
            tile = _board[row, columnIndex];
            if (tile.MatchPiece != null)
                continue;

            while (true)
            {
                tile = tile.GetNeighbor(Direction.Up);
                if (tile == null)
                    break;

                if (tile.MatchPiece != null)
                {
                    piece = tile.MatchPiece;
                    tile.SetCurrentMatchPiece(null, PieceMovementType.Drop);
                    break;
                }
            }

            if (piece == null)
            {
                ObjectPooler.Instance.GetObject(((PieceType)Random.Range(0, (int)PieceType.Count)).ToString(), (MatchPiece pooledPiece) =>
                {
                    piece = pooledPiece;
                    piece.Transform.SetParent(piecesContainer);
                    piece.Transform.position = spawnersTransform[columnIndex].position;
                    _board[row, columnIndex].SetCurrentMatchPiece(piece, PieceMovementType.Drop);
                });

                yield return new WaitUntil(() => piece != null);
            }
            else
                _board[row, columnIndex].SetCurrentMatchPiece(piece, PieceMovementType.Drop);
        }
    }

    List<BoardTile> CheckPossibleMatches(BoardTile tile, BoardTile ignoreOther, PieceType type)
    {
        List<BoardTile> tempMatches = new List<BoardTile>();
        List<BoardTile> tilesMatched = new List<BoardTile>();

        int matches = 1;

        BoardTile nextTile = tile.GetNeighbor(Direction.Right);
        while (nextTile != ignoreOther && nextTile != null && nextTile.MatchPiece.pieceType == type)
        {
            matches++;
            tempMatches.Add(nextTile);
            nextTile = nextTile.GetNeighbor(Direction.Right);
        }

        nextTile = tile.GetNeighbor(Direction.Left);
        while (nextTile != ignoreOther && nextTile != null && nextTile.MatchPiece.pieceType == type)
        {
            matches++;
            tempMatches.Add(nextTile);
            nextTile = nextTile.GetNeighbor(Direction.Left);
        }

        if (matches >= PIECE_MATCH_THRESHOLD)
        {
            tilesMatched.Add(tile);
            tilesMatched.AddRange(tempMatches);
        }

        tempMatches.Clear();
        matches = 1;
        nextTile = tile.GetNeighbor(Direction.Up);
        while (nextTile != ignoreOther && nextTile != null && nextTile.MatchPiece.pieceType == type)
        {
            matches++;
            tempMatches.Add(nextTile);
            nextTile = nextTile.GetNeighbor(Direction.Up);
        }

        nextTile = tile.GetNeighbor(Direction.Down);
        while (nextTile != ignoreOther && nextTile != null && nextTile.MatchPiece.pieceType == type)
        {
            matches++;
            tempMatches.Add(nextTile);
            nextTile = nextTile.GetNeighbor(Direction.Down);
        }

        if (matches >= PIECE_MATCH_THRESHOLD)
        {
            if(!tilesMatched.Contains(tile))
                tilesMatched.Add(tile);
            
            tilesMatched.AddRange(tempMatches);
        }

        return tilesMatched;
    }

    void ExchangeTilePieces(BoardTile fromTile, BoardTile toTile, SimpleEvent callback = null)
    {
        _mouseState = MouseState.Standby;

        MatchPiece piece = fromTile.MatchPiece;
        fromTile.SetCurrentMatchPiece(toTile.MatchPiece, PieceMovementType.Move);
        toTile.SetCurrentMatchPiece(piece, PieceMovementType.Move);

        StartCoroutine(WaitForBoardReady(callback));
    }

    void KillPieces(List<BoardTile> tiles)
    {
        for(int i = 0; i < tiles.Count; i++)
        {
            //TODO: Particles
            tiles[i].MatchPiece.gameObject.SetActive(false);
            tiles[i].SetCurrentMatchPiece(null, PieceMovementType.Drop);
        }

        FillBoard();
    }

    void CheckBoardMatches()
    {
        List<BoardTile> tiles;
        for(int row = 0; row < (int)_boardSize.x; row++)
        {
            for (int col = 0; col < (int)_boardSize.x; col++)
            {
                tiles = CheckPossibleMatches(_board[row, col], null, _board[row, col].MatchPiece.pieceType);
                if(tiles.Count >= PIECE_MATCH_THRESHOLD)
                {
                    KillPieces(tiles);
                    return;
                }
            }
        }

        _mouseState = MouseState.Hovering;
    }

    IEnumerator WaitForBoardReady(SimpleEvent callback)
    {
        IEnumerable<BoardTile> collection = _board.Cast<BoardTile>();
        yield return new WaitUntil(() => collection.All(x => x.MatchPiece != null && x.MatchPiece.IsReady));
        callback?.Invoke();
    }
}