using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MainScript : MonoBehaviour
{
    public GameObject boardPrefab;
    public GameObject cellPrefab;
    public GameObject canvas;
    public static MainScript mainScript;
    public PlayerNetworkScript playerNetworkScript;
    public RelayScript relayScript;
    public UIScript uiScript;

    public GameObject capturedPiecePrefab;

    public List<PieceTheme> pieceThemes;

    public List<Move> highlightedMoves = new();

    private static Color black = new Color(0.85F, 0.75F, 0.6F, 1.0F);
    private static Color grayHighlight = new Color(0.5F, 0.5F, 0.5F, 0.5F);
    private static Color redHighlight = new Color(1.0F, 0.0F, 0.0F, 0.7F);
    private static Color transparent = new Color(1.0F, 1.0F, 1.0F, 0.0F);
    private static Color white = new Color(1.0F, 1.0F, 1.0F, 1.0F);
    private static Color yellowHighlight = new Color(1.0F, 1.0F, 0.0F, 0.35F);

    public bool sideBySide;

    public Board board;

    public Text joinCodeOutput;
    public InputField joinCodeInput;

    public int gameStarted;

    public bool playerIsWhite;
    public bool hostMode;

    public static List<string> pieceNames = new List<string>(new string[] { "bb", "bk", "bn", "bp", "bq", "br", "wb", "wk", "wn", "wp", "wq", "wr" });

    private void Start()
    {
        mainScript = GetComponent<MainScript>();
        canvas = GameObject.Find("Canvas");
        gameStarted = 0;
    }

    public void startGame(bool isWhite, bool _sideBySide)
    {
        board = new Board(new int[8, 8]
        {
            { 5, 2, 0, 4, 1, 0, 2, 5},
            { 3, 3, 3, 3, 3, 3, 3, 3},
            {-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1},
            { 9, 9, 9, 9, 9, 9, 9, 9},
            {11, 8, 6,10, 7, 6, 8,11},
        }, boardPrefab, cellPrefab, canvas, pieceThemes[1], !isWhite, capturedPiecePrefab);
        gameStarted = 5;
        playerIsWhite = isWhite;
        sideBySide = _sideBySide;
        uiScript.startingUIHolder.SetActive(false);
        uiScript.gameOverPanel.SetActive(false);
    }

    [System.Serializable]
    public class PieceTheme
    {
        public List<Sprite> pieces;

        public Sprite this[int index]
        {
            get => pieces[index];
            set => pieces[index] = value;
        }
    }

    public struct Position
    {
        public int x;
        public int y;

        public Position(int _x, int _y) => (x, y) = (_x, _y);

        public bool isValid() => 0 <= x && x <= 7 && 0 <= y && y <= 7;

        public Position(Position position) => this = new Position(position.x, position.y);

        public static Position operator +(Position a, Position b) => new Position(a.x + b.x, a.y + b.y);
        public static bool operator ==(Position a, Position b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Position a, Position b) => !(a == b);

        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();

        public Position U(int dist = 1) => new Position(x, y + dist);
        public Position D(int dist = 1) => new Position(x, y - dist);
        public Position R(int dist = 1) => new Position(x + dist, y);
        public Position L(int dist = 1) => new Position(x - dist, y);

        public Position F(bool isWhite, int dist = 1) => isWhite ? U(dist) : D(dist);
        public Position B(bool isWhite, int dist = 1) => isWhite ? D(dist) : U(dist);

        public Position UL(int u = 1, int l = 1) => U(u).L(l);
        public Position DL(int d = 1, int l = 1) => D(d).L(l);
        public Position UR(int u = 1, int r = 1) => U(u).R(r);
        public Position DR(int d = 1, int r = 1) => D(d).R(r);

        public Position FL(bool isWhite, int f = 1, int l = 1) => F(isWhite, f).L(l);
        public Position BL(bool isWhite, int b = 1, int l = 1) => B(isWhite, b).L(l);
        public Position FR(bool isWhite, int f = 1, int r = 1) => F(isWhite, f).R(r);
        public Position BR(bool isWhite, int b = 1, int r = 1) => B(isWhite, b).R(r);

        public Position UL(int dist = 1) => UL(dist, dist);
        public Position DL(int dist = 1) => DL(dist, dist);
        public Position UR(int dist = 1) => UR(dist, dist);
        public Position DR(int dist = 1) => DR(dist, dist);

        public Position FL(bool isWhite, int dist = 1) => FL(isWhite, dist, dist);
        public Position BL(bool isWhite, int dist = 1) => BL(isWhite, dist, dist);
        public Position FR(bool isWhite, int dist = 1) => FR(isWhite, dist, dist);
        public Position BR(bool isWhite, int dist = 1) => BR(isWhite, dist, dist);

        public string PGN() => $"{"abcdefgh"[x]}{y + 1}";
    }

    public class Board
    {
        public Cell[,] board;
        public GameObject boardContainer;
        public bool turnIsWhite;
        public bool[] castling;
        public int enpassant;
        public bool gameOver;
        public bool flipBoard;
        public List<Position> lastMoved;
        public List<Position> checkedSquare;

        public GameObject whiteCapturedPiecesHolder;
        public GameObject blackCapturedPiecesHolder;

        public CapturedPieces whiteCapturedPieces;
        public CapturedPieces blackCapturedPieces;

        public Board(int[,] pieceIndexes, GameObject boardPrefab, GameObject cellPrefab, GameObject canvas, PieceTheme pieceTheme, bool _flipBoard, GameObject _capturedPiecePrefab)
        {
            flipBoard = _flipBoard;
            boardContainer = Instantiate(boardPrefab, canvas.transform);

            if (!flipBoard)
            {
                whiteCapturedPiecesHolder = boardContainer.transform.GetChild(0).GetChild(1).gameObject;
                blackCapturedPiecesHolder = boardContainer.transform.GetChild(0).GetChild(0).gameObject;
            }
            else
            {
                whiteCapturedPiecesHolder = boardContainer.transform.GetChild(0).GetChild(0).gameObject;
                blackCapturedPiecesHolder = boardContainer.transform.GetChild(0).GetChild(1).gameObject;
            }

            whiteCapturedPieces = new CapturedPieces(true, whiteCapturedPiecesHolder, _capturedPiecePrefab, pieceTheme);
            blackCapturedPieces = new CapturedPieces(false, blackCapturedPiecesHolder, _capturedPiecePrefab, pieceTheme);

            board = new Cell[8, 8];
            for (int i = 0; i < 8; i++) for (int j = 0; j < 8; j++)
                {
                    board[i, j] = new Cell(new Position(j, 7 - i), pieceIndexes[i, j], boardContainer, cellPrefab, pieceTheme, this);
                }
            turnIsWhite = true;
            castling = new bool[4] { true, true, true, true };
            enpassant = -1;
            lastMoved = new();
            checkedSquare = new();
        }

        public Cell findKing(bool isWhite) => findPiece(isWhite ? 7 : 1);

        public Cell getPos(Position position) => board[7 - position.y, position.x];

        public Cell findPiece(int pieceSpriteIndex)
        {
            foreach (Cell cell in board) if (cell.piece.pieceSpriteIndex == pieceSpriteIndex) return cell;
            return null;
        }

        public UnMove makeMove(Move move, bool switchTurn = true, bool updateScreen = true, bool mainMove = true)
        {
            UnMove unMove = new UnMove(this);

            string PGN = null;
            if (updateScreen) PGN = move.PGN();

            bool captured = !move.dest.isEmpty;
            char capturedPieceType = ' ';
            if (captured) capturedPieceType = move.dest.piece.pieceType;

            if (move.from.isEmpty) return unMove;

            if (mainMove)
            {
                lastMoved.Clear();
                clearHighlights('y');
                lastMoved.Add(move.from.position);
                lastMoved.Add(move.dest.position);
                move.from.highlightCell('y');
                move.dest.highlightCell('y');
            }

            enpassant = -1;
            switch (move.from.piece.pieceType)
            {
                case 'k':
                    castling[turnIsWhite ? 0 : 2] = false;
                    castling[turnIsWhite ? 1 : 3] = false;
                    if (move.dest.position.x - move.from.position.x == 2)
                        unMove.merge(makeMove(new Move(new Position(7, move.from.position.y), new Position(5, move.from.position.y), this), false, updateScreen, false));
                    if (move.dest.position.x - move.from.position.x == -2)
                        unMove.merge(makeMove(new Move(new Position(0, move.from.position.y), new Position(3, move.from.position.y), this), false, updateScreen, false));
                    break;
                case 'r':
                    switch (move.from.position.x)
                    {
                        case 0:
                            castling[turnIsWhite ? 0 : 2] = false;
                            break;
                        case 7:
                            castling[turnIsWhite ? 1 : 3] = false;
                            break;
                    }
                    break;
                case 'p':
                    if (Mathf.Abs(move.dest.position.y - move.from.position.y) == 2) enpassant = move.from.position.x;
                    if (move.dest.isEmpty && move.dest.position.x != move.from.position.x)
                    {
                        unMove.addStep(getPos(move.dest.position.D()).updatePiece(-1, updateScreen));
                        if (updateScreen)
                        {
                            captured = true;
                            capturedPieceType = 'p';
                        }
                    }
                    if (move.dest.position.y == (turnIsWhite ? 7 : 0)) unMove.addStep(move.from.updatePiece(turnIsWhite ? 10 : 4, updateScreen));
                    break;
            }

            unMove.addStep(move.dest.updatePiece(move.from.piece.pieceSpriteIndex, updateScreen));
            unMove.addStep(move.from.updatePiece(-1, updateScreen));

            if (switchTurn) turnIsWhite = !turnIsWhite;

            if (updateScreen)
            {
                if (captured)
                {
                    CapturedPieces currentCapturedPieces = (turnIsWhite ? whiteCapturedPieces : blackCapturedPieces);
                    currentCapturedPieces.add(capturedPieceType);
                    currentCapturedPieces.updateScreen();
                }

                (bool check, bool mate) = gameStatus(turnIsWhite);


                gameOver = mate;

                checkedSquare.Clear();
                clearHighlights('r');

                if (check)
                {
                    Cell kingCell = findKing(turnIsWhite);
                    kingCell.highlightCell('r');
                    checkedSquare.Add(kingCell.position);

                    if (mate)
                    {
                        mainScript.uiScript.showGameOverPanel($"{(!turnIsWhite ? "White" : "Black")} wins!");
                        PGN += '#';
                    }
                    else PGN += '+';
                }
                else
                {
                    checkedSquare.Clear();
                    if (mate)
                    {
                        mainScript.uiScript.showGameOverPanel("Stalemate");
                        PGN += "(=)";
                    }
                }
                Debug.Log(PGN);
            }

            return unMove;
        }

        public void unMakeMove(UnMove unMove)
        {
            unMove.unMoveSteps.Reverse();
            foreach (UnMoveStep unMoveStep in unMove.unMoveSteps) { unMoveStep.cell.updatePiece(unMoveStep.oldPieceIndex); }

            enpassant = unMove.oldEnpassant;
            for (int i = 0; i < 4; i++) castling[i] = unMove.oldCastling[i];
            turnIsWhite = unMove.oldTurnIsWhite;
        }

        public (bool check, bool mate) gameStatus(bool isWhite)
        {
            bool check = false;
            bool mate = true;

            check = findKing(isWhite).isThreatened(!isWhite);

            foreach (Cell cell in board)
            {
                if (cell.isWhite == isWhite && legalMoves(cell, this, checkCastling: false).Count() != 0)
                {
                    mate = false;
                    break;
                }
            }

            return (check, mate);
        }

        public void clearHighlights(char color, List<Position> excludeLastMoved = null)
        {
            if (excludeLastMoved is null) excludeLastMoved = new();
            foreach (Cell cell in board)
            {
                if (excludeLastMoved.Contains(cell.position))
                {
                    cell.highlightCell('y');
                    continue;
                }
                if (checkedSquare.Contains(cell.position))
                {
                    cell.highlightCell('r');
                    continue;
                }
                if (cell.highlightColor == color)
                {
                    cell.highlightCell('w');
                }
            }
        }
    }

    public class Cell
    {
        public Position position;
        public GameObject cellContainer;
        public GameObject cellBackground;
        public Image highlight;
        public Image pieceImage;
        public GameObject clickableMask;
        public Piece piece;
        public bool isEmpty;
        public bool isWhite;
        public Board parentBoard;
        public bool hasMoved;
        public char highlightColor;
        public ClickHandler clickHandler;

        public Cell(Position _position, int pieceSpriteIndex, GameObject board, GameObject cellPrefab, PieceTheme pieceTheme, Board _board)
        {
            position = _position;
            parentBoard = _board;
            isEmpty = pieceSpriteIndex == -1;
            isWhite = !isEmpty && pieceSpriteIndex >= 6;
            cellContainer = Instantiate(cellPrefab, board.transform);
            cellContainer.transform.localPosition = getCoordsOfCell(_position, parentBoard.flipBoard);
            cellBackground = cellContainer.transform.GetChild(0).gameObject;
            cellBackground.GetComponent<Image>().color = (position.x + position.y) % 2 == 0 ? black : white;
            highlight = cellContainer.transform.GetChild(1).gameObject.GetComponent<Image>();
            pieceImage = cellContainer.transform.GetChild(2).gameObject.GetComponent<Image>();
            clickableMask = cellContainer.transform.GetChild(3).gameObject;
            clickHandler = clickableMask.GetComponent<ClickHandler>();
            clickHandler.mainScript = mainScript;
            clickHandler.cell = this;
            clickHandler.clickable = !isEmpty;
            highlightColor = 'w';
            piece = new Piece(this, pieceSpriteIndex, pieceImage, pieceTheme);

            switch (pieceSpriteIndex)
            {
                case 'p': hasMoved = position.y != (isWhite ? 1 : 6); break;
                case 'k': hasMoved = position.x != 4 || position.y != (isWhite ? 0 : 7); break;
                case 'r': hasMoved = (position.x != 0 && position.x != 7) || position.y != (isWhite ? 0 : 7); break;
                default: hasMoved = false; break;
            }
        }

        public UnMoveStep updatePiece(int pieceSpriteIndex, bool updateScreen = true)
        {
            UnMoveStep unMoveStep = new UnMoveStep(this, piece.pieceSpriteIndex);

            isEmpty = pieceSpriteIndex == -1;
            isWhite = !isEmpty && pieceSpriteIndex >= 6;

            clickHandler.clickable = !isEmpty;

            piece.updatePiece(pieceSpriteIndex, updateScreen);
            if (updateScreen) hasMoved = true;

            return unMoveStep;
        }

        public Move newMoveTo(Position dest) => new Move(position, dest, parentBoard);
        public Move newMoveTo(Cell dest) => new Move(position, dest.position, parentBoard);
        public List<Move> getMoves(MovePattern movePattern)
            => new List<Move>(from Position dest in movePattern.moves where (position + dest).isValid() select newMoveTo(position + dest));

        public bool isMovable(bool _isWhite) => isEmpty || isWhite != _isWhite;

        public void highlightCell(char color = 'w')
        {
            highlightColor = color;

            highlight.color = color switch
            {
                'g' => grayHighlight,
                'r' => redHighlight,
                'y' => yellowHighlight,
                'w' => transparent,
                _ => transparent
            };
        }

        public List<Move> tileMoves(Position[] directions)
        {
            List<Move> moves = new();
            foreach (Position direction in directions)
            {
                Position currentTile = new Position(position);
                for (int i = 0; i < 8; i++)
                {
                    currentTile += direction;
                    if (!currentTile.isValid()) break;
                    if (!parentBoard.getPos(currentTile).isMovable(isWhite)) break;
                    moves.Add(newMoveTo(currentTile));
                    if (!parentBoard.getPos(currentTile).isEmpty) break;
                }
            }
            return moves;
        }

        public Cell U(int dist = 1) => parentBoard.getPos(position.U(dist));
        public Cell D(int dist = 1) => parentBoard.getPos(position.D(dist));
        public Cell R(int dist = 1) => parentBoard.getPos(position.R(dist));
        public Cell L(int dist = 1) => parentBoard.getPos(position.L(dist));

        public Cell F(int dist = 1) => parentBoard.getPos(position.F(isWhite, dist));
        public Cell B(int dist = 1) => parentBoard.getPos(position.F(isWhite, dist));

        public Cell UL(int u = 1, int l = 1) => parentBoard.getPos(position.U(u).L(l));
        public Cell DL(int d = 1, int l = 1) => parentBoard.getPos(position.D(d).L(l));
        public Cell UR(int u = 1, int r = 1) => parentBoard.getPos(position.U(u).R(r));
        public Cell DR(int d = 1, int r = 1) => parentBoard.getPos(position.D(d).R(r));

        public Cell FL(int f = 1, int l = 1) => parentBoard.getPos(position.F(isWhite, f).L(l));
        public Cell BL(int b = 1, int l = 1) => parentBoard.getPos(position.B(isWhite, b).L(l));
        public Cell FR(int f = 1, int r = 1) => parentBoard.getPos(position.F(isWhite, f).R(r));
        public Cell BR(int b = 1, int r = 1) => parentBoard.getPos(position.B(isWhite, b).R(r));

        public Cell UL(int dist = 1) => UL(dist, dist);
        public Cell DL(int dist = 1) => DL(dist, dist);
        public Cell UR(int dist = 1) => UR(dist, dist);
        public Cell DR(int dist = 1) => DR(dist, dist);

        public Cell FL(int dist = 1) => FL(dist, dist);
        public Cell BL(int dist = 1) => BL(dist, dist);
        public Cell FR(int dist = 1) => FR(dist, dist);
        public Cell BR(int dist = 1) => BR(dist, dist);

        public override string ToString() => piece.pieceName;

        public bool isThreatened(bool isWhite)
        {
            foreach (Cell cell in parentBoard.board)
            {
                if (cell.isWhite == isWhite)
                {
                    foreach (Move move in legalMoves(cell, parentBoard, false, false))
                    {
                        if (move.dest.position == position) return true;
                    }
                }
            }
            return false;
        }
    }

    public class Piece
    {
        public Cell cell;
        public int pieceSpriteIndex;
        public Image pieceImage;
        public PieceTheme pieceTheme;
        public string pieceName;
        public char pieceType;

        public Piece(Cell _cell, int _pieceSpriteIndex, Image _pieceImage, PieceTheme _pieceTheme)
        {
            cell = _cell;
            pieceImage = _pieceImage;
            pieceTheme = _pieceTheme;

            updatePiece(_pieceSpriteIndex);
        }

        public void updatePiece(int _pieceSpriteIndex, bool updateScreen = true)
        {
            pieceSpriteIndex = _pieceSpriteIndex;
            if (!cell.isEmpty)
            {
                pieceName = pieceNames[pieceSpriteIndex];
                pieceType = pieceName[1];

                if (updateScreen)
                {
                    pieceImage.sprite = pieceTheme[pieceSpriteIndex];
                    pieceImage.color = white;
                }
            }
            else
            {
                if (updateScreen) pieceImage.color = transparent;
                pieceName = "ee";
                pieceType = 'e';
            }
        }
    }

    public static List<Move> legalMoves(Cell cell, Board board, bool excludeChecks = true, bool checkCastling = true)
    {
        List<Move> moves = new();
        Position[] directions;
        List<Move> rawMoves;

        switch (cell.piece.pieceType)
        {
            case 'p':
                if (!cell.position.F(cell.isWhite).isValid()) break;
                if (cell.F(1).isEmpty)
                {
                    moves.Add(cell.newMoveTo(cell.F(1)));
                    if (cell.position.y == (cell.isWhite ? 1 : 6) && cell.F(2).isEmpty) moves.Add(cell.newMoveTo(cell.F(2)));
                }
                if (cell.position.FL(cell.isWhite, 1).isValid())
                {
                    Cell fl = cell.FL(1);
                    if ((!fl.isEmpty && fl.isWhite != cell.isWhite) || (board.enpassant == fl.position.x && cell.position.y == (cell.isWhite ? 4 : 3)))
                        moves.Add(cell.newMoveTo(fl));
                }
                if (cell.position.FR(cell.isWhite, 1).isValid())
                {
                    Cell fr = cell.FR(1);
                    if ((!fr.isEmpty && fr.isWhite != cell.isWhite) || (board.enpassant == fr.position.x && cell.position.y == (cell.isWhite ? 4 : 3)))
                        moves.Add(cell.newMoveTo(fr));
                }
                break;
            case 'n':
                MovePattern knightMovePattern = new MovePattern();
                knightMovePattern.addMovesReflected(2, 1);
                rawMoves = cell.getMoves(knightMovePattern);
                moves = new List<Move>(from Move move in rawMoves where board.getPos(move.dest.position).isMovable(cell.isWhite) select move);
                break;
            case 'k':
                MovePattern kingMovePattern = new MovePattern();
                kingMovePattern.addMovesReflected(1, 1);
                kingMovePattern.addMovesReflected(0, 1);
                rawMoves = cell.getMoves(kingMovePattern);
                moves = new List<Move>(from Move move in rawMoves where board.getPos(move.dest.position).isMovable(cell.isWhite) select move);

                if (checkCastling && !cell.isThreatened(!cell.isWhite))
                {
                    if (board.castling[cell.isWhite ? 0 : 2] && cell.L(1).isEmpty && cell.L(2).isEmpty && cell.L(3).isEmpty && !cell.L(1).isThreatened(!cell.isWhite))
                        moves.Add(cell.newMoveTo(cell.position.L(2)));
                    if (board.castling[cell.isWhite ? 1 : 3] && cell.R(1).isEmpty && cell.R(2).isEmpty && !cell.R(1).isThreatened(!cell.isWhite))
                        moves.Add(cell.newMoveTo(cell.position.R(2)));
                }

                break;
            case 'b':
                directions = new Position[4] { new(1, 1), new(-1, 1), new(-1, -1), new(1, -1) };
                foreach (Move move in cell.tileMoves(directions)) moves.Add(move);
                break;
            case 'r':
                directions = new Position[4] { new(1, 0), new(0, 1), new(-1, 0), new(0, -1) };
                foreach (Move move in cell.tileMoves(directions)) moves.Add(move);
                break;
            case 'q':
                directions = new Position[8] { new(1, 1), new(-1, 1), new(-1, -1), new(1, -1), new(1, 0), new(0, 1), new(-1, 0), new(0, -1) };
                foreach (Move move in cell.tileMoves(directions)) moves.Add(move);
                break;
        }

        List<Move> finalMoves = new();

        if (excludeChecks)
        {
            foreach (Move move in moves)
            {
                UnMove unMove = board.makeMove(move, false, false, false);
                if (!board.findKing(board.turnIsWhite).isThreatened(!board.turnIsWhite)) finalMoves.Add(move);
                board.unMakeMove(unMove);
            }
        }
        else finalMoves = moves;

        return finalMoves;
    }

    public class Move
    {
        public Cell from, dest;
        public Board parentBoard;

        public Move(Position _from, Position _dest, Board board)
            => (from, dest, parentBoard) = (board.getPos(_from), board.getPos(_dest), board);

        public Move(MoveData moveData, Board board)
            => (from, dest, parentBoard) = (board.getPos(new Position(moveData.fromx, moveData.fromy)), board.getPos(new Position(moveData.destx, moveData.desty)), board);

        public bool isEnpassant() => from.piece.pieceType == 'p' && dest.isEmpty && from.position.x != dest.position.x;

        public int isCastle()
        {
            if (from.piece.pieceType != 'k') return 0;
            return (dest.position.x - from.position.x) switch { 2 => 1, -2 => 2, _ => 0 };
        }

        public override bool Equals(object obj)
        {
            Move that = obj as Move;

            return this.from.position == that.from.position && this.dest.position == that.from.position;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool isCapture() => isEnpassant() || !dest.isEmpty;

        public string PGN()
        {
            switch (isCastle())
            {
                case 1: return "O-O";
                case 2: return "O-O-O";
            }

            bool ambiguousX, ambiguousY;

            ambiguousX = ambiguousY = false;

            string PGNString = dest.position.PGN();

            if (isCapture()) PGNString = 'x' + PGNString;
            if (ambiguousY) PGNString = from.position.PGN()[1] + PGNString;
            if (ambiguousX) PGNString = from.position.PGN()[0] + PGNString;
            if (from.piece.pieceType != 'p') PGNString = char.ToUpper(from.piece.pieceType) + PGNString;

            return PGNString;
        }
    }

    public class MovePattern
    {
        public List<Position> moves;

        public MovePattern()
            => moves = new List<Position>();

        public void addMovesReflected(int x, int y)
        {
            int[] signs = new int[] { +1, -1 };
            foreach (int sx in signs) foreach (int sy in signs)
                {
                    moves.Add(new Position(x * sx, y * sy));
                    moves.Add(new Position(y * sy, x * sx));
                }
        }
    }

    public struct MoveData : INetworkSerializable
    {
        public int fromx;
        public int fromy;
        public int destx;
        public int desty;

        public MoveData(Move move)
        {
            fromx = move.from.position.x;
            fromy = move.from.position.y;
            destx = move.dest.position.x;
            desty = move.dest.position.y;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref fromx);
            serializer.SerializeValue(ref fromy);
            serializer.SerializeValue(ref destx);
            serializer.SerializeValue(ref desty);
        }
    }

    public class UnMoveStep
    {
        public Cell cell;
        public int oldPieceIndex;

        public UnMoveStep(Cell _cell, int _oldPieceIndex)
            => (cell, oldPieceIndex) = (_cell, _oldPieceIndex);
    }

    public class UnMove
    {
        public List<UnMoveStep> unMoveSteps;
        public bool[] oldCastling;
        public int oldEnpassant;
        public bool oldTurnIsWhite;

        public UnMove(Board board)
        {
            unMoveSteps = new();
            oldCastling = new bool[4];
            for (int i = 0; i < 4; i++) oldCastling[i] = board.castling[i];
            oldEnpassant = board.enpassant;
            oldTurnIsWhite = board.turnIsWhite;
        }

        public void addStep(UnMoveStep unMoveStep) => unMoveSteps.Add(unMoveStep);

        public void merge(UnMove unMove) => unMoveSteps.AddRange(unMove.unMoveSteps);
    }

    public class CapturedPieceGroup
    {
        public string pieceName;
        public char pieceType;
        public int quantity;
        public int pieceSpriteIndex;
        public int pieceSortOrder;

        public CapturedPieceGroup(string _pieceName)
        {
            pieceName = _pieceName;
            pieceType = pieceName[1];
            quantity = 0;
            pieceSpriteIndex = pieceNames.FindIndex(a => a == pieceName);
            pieceSortOrder = "pnbrqk".IndexOf(pieceName[1]);
        }
    }

    public class CapturedPieces
    {
        public List<CapturedPieceGroup> capturedPieceGroups;
        public bool isWhite;
        public GameObject container;
        public GameObject capturedPiecePrefab;
        public PieceTheme pieceTheme;

        public CapturedPieces(bool _isWhite, GameObject _container, GameObject _capturedPiecePrefab, PieceTheme _pieceTheme)
        {
            container = _container;
            capturedPiecePrefab = _capturedPiecePrefab;
            pieceTheme = _pieceTheme;

            isWhite = _isWhite;
            char colorPrefix = isWhite ? 'w' : 'b';

            capturedPieceGroups = new List<CapturedPieceGroup>(new CapturedPieceGroup[]
            {
                new($"{colorPrefix}p"),
                new($"{colorPrefix}n"),
                new($"{colorPrefix}b"),
                new($"{colorPrefix}r"),
                new($"{colorPrefix}q")
            });
        }

        public void add(char pieceType, int quantity = 1) => capturedPieceGroups.Find(a => a.pieceType == pieceType).quantity += quantity;

        public void updateScreen()
        {
            foreach (Transform child in container.transform) Destroy(child.gameObject);

            capturedPieceGroups = capturedPieceGroups.OrderBy(piece => piece.pieceSortOrder).ToList();

            int currentImageX = 20;
            foreach (CapturedPieceGroup capturedPieceGroup in capturedPieceGroups)
            {
                if (capturedPieceGroup.quantity == 0) continue;
                for (int i = 0; i < capturedPieceGroup.quantity; i++)
                {
                    GameObject newCapturedPiece = Instantiate(capturedPiecePrefab, container.transform);
                    newCapturedPiece.transform.localPosition = new Vector3(currentImageX, 0, 0);
                    newCapturedPiece.GetComponent<Image>().sprite = pieceTheme[capturedPieceGroup.pieceSpriteIndex];
                    currentImageX += 10;
                }
                currentImageX += 20;
            }
        }
    }

    private static Vector3 getCoordsOfCell(Position position, bool flipBoard) => (flipBoard ? -1 : +1) * new Vector3(-175 + (position.x * 50), -175 + (position.y * 50));

    public void destroyBoard() => Destroy(board.boardContainer);
}