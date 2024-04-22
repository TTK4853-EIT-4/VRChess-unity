using System.Collections;
using System.Collections.Generic;
using UnityChess;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class BoardHighlight : MonoBehaviour
{
    public Square parentSquare { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // On XR select interaction with select arguments
	public void OnXRPieceSelect(SelectEnterEventArgs args) {
        if(!enabled) return;
        if (GameManager.Instance.selectedPiece == null) return;

        // Get the selected piece and its square
        VisualPiece selectedPiece = GameManager.Instance.selectedPiece;
        Square selectedSquare = selectedPiece.CurrentSquare;

        // Make move
        GameManager.Instance.MovePiece(selectedSquare, parentSquare, selectedPiece);

        Debug.Log("Square clicked: " + parentSquare.ToString());
    }
}
