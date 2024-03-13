﻿using System.Collections.Generic;
using UnityChess;
using UnityEngine;
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


	private List<GameObject> boardHighlights = new List<GameObject>();

	private void Start() {
		potentialLandingSquares = new List<GameObject>();
		thisTransform = transform;
		boardCamera = Camera.main;
	}

	public void OnMouseDown() {
		if (enabled) {
			piecePositionSS = boardCamera.WorldToScreenPoint(transform.position);

			var piece = GameManager.Instance.CurrentBoard[CurrentSquare];
			var legalMoves = GameManager.Instance.GetLegalMovesForPiece(piece);

			if (legalMoves.Count == 0) {
				return;
			} else {
				// Change piece material to highlightMaterial.
				originalMaterial = thisTransform.GetComponent<MeshRenderer>().material;
				thisTransform.GetComponent<MeshRenderer>().material = highlightMaterial;

				foreach (Movement move in legalMoves) {
					GameObject squareGO = BoardManager.Instance.GetSquareGOByPosition(move.End);
					
					// Highlight position x=0, y=0.05, z=0 of the square.
					var position = squareGO.transform.position;
					position.y = position.y + 0.01f;

					GameObject highlight = Instantiate(boardHighlightPrefab, position, Quaternion.identity);
					//GameObject boardHighlight = Instantiate(boardHighlightPrefab, squareGO.transform);
					boardHighlights.Add(highlight);
				}

			}

			// Show potential landing squares.
			/*potentialLandingSquares.Clear();
			BoardManager.Instance.GetSquareGOsWithinRadius(potentialLandingSquares, thisTransform.position, SquareCollisionRadius);
			foreach (GameObject potentialLandingSquare in potentialLandingSquares) {

				// Create new boardHighlightPrefab and add it to the potentialLandingSquare.
				GameObject boardHighlight = Instantiate(boardHighlightPrefab, potentialLandingSquare.transform);
				boardHighlights.Add(boardHighlight);
				
			}*/


		}
	}

	private void OnMouseDrag() {
		if (enabled) {
			// Move piece only in x and z axes but follow mouse in screen space.
			Vector3 mousePositionSS = new Vector3(Input.mousePosition.x, Input.mousePosition.y, piecePositionSS.z);
			Vector3 mousePositionWS = boardCamera.ScreenToWorldPoint(mousePositionSS);
			thisTransform.position = new Vector3(mousePositionWS.x, thisTransform.position.y, mousePositionWS.z);



		}
	}

	public void OnMouseUp() {
		if (enabled) {
			potentialLandingSquares.Clear();
			BoardManager.Instance.GetSquareGOsWithinRadius(potentialLandingSquares, thisTransform.position, SquareCollisionRadius);

			if (potentialLandingSquares.Count == 0) { // piece moved off board
				thisTransform.position = thisTransform.parent.position;
				return;
			}
	
			// determine closest square out of potential landing squares.
			Transform closestSquareTransform = potentialLandingSquares[0].transform;
			float shortestDistanceFromPieceSquared = (closestSquareTransform.transform.position - thisTransform.position).sqrMagnitude;
			for (int i = 1; i < potentialLandingSquares.Count; i++) {
				GameObject potentialLandingSquare = potentialLandingSquares[i];
				float distanceFromPieceSquared = (potentialLandingSquare.transform.position - thisTransform.position).sqrMagnitude;

				if (distanceFromPieceSquared < shortestDistanceFromPieceSquared) {
					shortestDistanceFromPieceSquared = distanceFromPieceSquared;
					closestSquareTransform = potentialLandingSquare.transform;
				}
			}

			// Reset piece material to originalMaterial.
			thisTransform.GetComponent<MeshRenderer>().material = originalMaterial;

			// Destroy potential landing squares highlights.
			foreach (GameObject boardHighlight in boardHighlights) {
				Destroy(boardHighlight);
			}

			VisualPieceMoved?.Invoke(CurrentSquare, thisTransform, closestSquareTransform);
		}
	}
}