using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickHandler : MonoBehaviour, IPointerDownHandler
{
    public MainScript mainScript;
    public MainScript.Cell cell;
    public bool clickable;
    public bool receiving;
    public MainScript.Move receivingMove;
    public MainScript.Cell highlightedCell;
    public bool clicked;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (receiving)
        {
            receiving = false;
            cell.parentBoard.makeMove(receivingMove, switchTurn: true, updateScreen: true, mainMove: true);

            if (!mainScript.sideBySide) mainScript.playerNetworkScript.moveServerRpc(new MainScript.MoveData(receivingMove));
        }

        foreach (MainScript.Move move in mainScript.highlightedMoves)
            move.dest.clickHandler.receiving = false;

        cell.parentBoard.clearHighlights('g');
        cell.parentBoard.clearHighlights('y', cell.parentBoard.lastMoved);

        if (!cell.parentBoard.gameOver && clickable && cell.isWhite == mainScript.board.turnIsWhite && !clicked && (mainScript.sideBySide || mainScript.playerIsWhite == cell.isWhite))
        {
            List<MainScript.Move> legalMoves = MainScript.legalMoves(cell, cell.parentBoard);
            foreach (MainScript.Move move in legalMoves)
            {
                MainScript.Cell dest = move.dest;
                dest.highlightCell('g');
                dest.clickHandler.receiving = true;
                dest.clickHandler.receivingMove = move;
                mainScript.highlightedMoves = legalMoves;
            }
            cell.highlightCell('y');
            highlightedCell = cell;
            foreach (MainScript.Cell _cell in cell.parentBoard.board) _cell.clickHandler.clicked = false;
            clicked = true;
        }
        else if (clicked) clicked = false;

        if (cell.isEmpty && !receiving) foreach (MainScript.Cell _cell in cell.parentBoard.board) _cell.clickHandler.clicked = false;
    }
}