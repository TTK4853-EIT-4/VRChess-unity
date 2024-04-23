using System.Collections.Generic;
using UnityChess;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using static UnityChess.SquareUtil;

public class VisualPiece : MonoBehaviour {
	public delegate void VisualPieceMovedAction(Square movedPieceInitialSquare, Transform movedPieceTransform, Transform closestBoardSquareTransform, Piece promotionPiece = null);
	public static event VisualPieceMovedAction VisualPieceMoved;
	
	public Side PieceColor;
	public Square CurrentSquare => StringToSquare(transform.parent.name);
	
	private const float SquareCollisionRadius = 9f;
	private Camera boardCamera;
	private Vector3 piecePositionSS;
	private SphereCollider pieceBoundingSphere;
	private List<GameObject> potentialLandingSquares;
	private Transform thisTransform;

	// BoardHighlight prefab
	public GameObject boardHighlightPrefab;

	// Current material
	public Material originalMaterial;

	// Highlight Material
	public Material highlightMaterial;

	// Hover Material
	public Material hoverMaterial;

	// Hover Material When can be captured
	public Material captureMaterial;


	private List<GameObject> boardHighlights = new List<GameObject>();

	private void Start() {
		potentialLandingSquares = new List<GameObject>();
		thisTransform = transform;
		boardCamera = Camera.main;
	}

	public void OnMouseDown() {
		return;
	}

	private void OnMouseDrag() {
		return;
	}

	public void OnMouseUp() {
		return;
	}

	// On XR select interaction with select arguments
	public void OnXRPieceSelect(SelectEnterEventArgs args) {
		bool allowed = (Side)UserData.Instance.playerSide == GameManager.Instance.SideToMove;

		if(enabled && allowed && UserData.Instance.currentRoom.gameStatus == GameStatus.STARTED) {
			if (GameManager.Instance.selectedPiece != null && GameManager.Instance.selectedPiece == this) {
				ToggleHighlight(false);
				RemoveAllHighlights();
				GameManager.Instance.selectedPiece = null;
				return;
			} else if (GameManager.Instance.selectedPiece != null && GameManager.Instance.selectedPiece != this) {
				Debug.Log("Another piece is already selected");
				return;
			}

			GameManager.Instance.selectedPiece = this;
			ToggleHighlight(true);
			HighlightLegalMoves();
		}

		// If selected piece is not null and can capture this piece
		if (GameManager.Instance.selectedPiece != null && GameManager.Instance.selectedPiece != this) {
			// Get the selected piece and its square
			VisualPiece selectedPiece = GameManager.Instance.selectedPiece;
			Square selectedSquare = selectedPiece.CurrentSquare;

			// Check if this piece can be captured
			foreach (Movement movement in GameManager.Instance.GetLegalMovesForPiece(GameManager.Instance.CurrentBoard[selectedSquare])) {
				if (movement.End == CurrentSquare) {
					// Make move
					Debug.Log("Square clicked: " + CurrentSquare.ToString());
					GameManager.Instance.MovePiece(selectedSquare, CurrentSquare, selectedPiece);
				}
			}
		}
	}

	// On XR deselect interaction
	public void OnXRPieceDeselect(SelectExitEventArgs args) {
		if(enabled) {
			//ToggleHighlight(false);
			//RemoveAllHighlights();
			//GameManager.Instance.selectedPiece = null;
		}
	}

	// On XR hover enter interaction
	public void OnXRPieceHoverEnter(HoverEnterEventArgs args) {
		// Bool allowed if (Side)UserData.Instance.playerSide == GameManager.Instance.SideToMove
		bool allowed = (Side)UserData.Instance.playerSide == GameManager.Instance.SideToMove;

		Debug.Log("Hover enter: " + GameManager.Instance.SideToMove);

		if(enabled && allowed && UserData.Instance.currentRoom.gameStatus == GameStatus.STARTED) {
			if(GameManager.Instance.selectedPiece != null && GameManager.Instance.selectedPiece != this) return;

			// Change piece material to hoverMaterial.
			thisTransform.GetComponent<MeshRenderer>().material = hoverMaterial;
		}
	}

	// On XR hover exit interaction
	public void OnXRPieceHoverExit(HoverExitEventArgs args) {
		if(enabled) {
			if(GameManager.Instance.selectedPiece != null && GameManager.Instance.selectedPiece == this) return;

			// Reset piece material to originalMaterial.
			thisTransform.GetComponent<MeshRenderer>().material = originalMaterial;
		}
	}

	// Toggle on/off the piece highlight
	public void ToggleHighlight(bool toggle) {
		if (toggle) {
			// Change piece material to highlightMaterial.
			thisTransform.GetComponent<MeshRenderer>().material = highlightMaterial;
		} else {
			// Reset piece material to originalMaterial.
			thisTransform.GetComponent<MeshRenderer>().material = originalMaterial;
		}
	}

	// Highlight the legal moves squares
	public void HighlightLegalMoves() {
		var piece = GameManager.Instance.CurrentBoard[CurrentSquare];
		var legalMoves = GameManager.Instance.GetLegalMovesForPiece(piece);

		if (legalMoves.Count == 0) {
			return;
		} else {
			foreach (Movement move in legalMoves) {
				GameObject squareGO = BoardManager.Instance.GetSquareGOByPosition(move.End);
				
				// Highlight position x=0, y=0.05, z=0 of the square.
				var position = squareGO.transform.position;
				position.y = position.y + 0.01f;

				GameObject highlight = Instantiate(boardHighlightPrefab, position, Quaternion.identity);
				
				// highlight object has BoardHighlight script attached to it. Set the parentSquare to the square of the highlight.
				highlight.GetComponent<BoardHighlight>().parentSquare = move.End;

				boardHighlights.Add(highlight);
			}
		}
	}

	// Remove all highlights
	public void RemoveAllHighlights() {
		foreach (GameObject boardHighlight in boardHighlights) {
			// Execute destroy on Unity main thread
			UnityThread.executeInUpdate(() =>
            {
				Destroy(boardHighlight);
            });
		}
		boardHighlights.Clear();
	}
}