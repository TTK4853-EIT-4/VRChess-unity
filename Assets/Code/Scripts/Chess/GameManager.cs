using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityChess;
using UnityChess.Engine;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GameManager : MonoBehaviourSingleton<GameManager> {
	public static event Action NewGameStartedEvent;
	public static event Action GameEndedEvent;
	public static event Action GameResetToHalfMoveEvent;
	public static event Action MoveExecutedEvent;

	public GameObject choosePromotionUIPanel;

	public VisualPiece selectedPiece { get; set; } = null;

	private PieceMove pieceMove = new PieceMove();
	
	public Board CurrentBoard {
		get {
			game.BoardTimeline.TryGetCurrent(out Board currentBoard);
			return currentBoard;
		}
	}

	public Side SideToMove {
		get {
			game.ConditionsTimeline.TryGetCurrent(out GameConditions currentConditions);
			return currentConditions.SideToMove;
		}
	}

	public Side StartingSide => game.ConditionsTimeline[0].SideToMove;
	public Timeline<HalfMove> HalfMoveTimeline => game.HalfMoveTimeline;
	public int LatestHalfMoveIndex => game.HalfMoveTimeline.HeadIndex;
	public int FullMoveNumber => StartingSide switch {
		Side.White => LatestHalfMoveIndex / 2 + 1,
		Side.Black => (LatestHalfMoveIndex + 1) / 2 + 1,
		_ => -1
	};

	private bool isWhiteAI;
	private bool isBlackAI;

	public List<(Square, Piece)> CurrentPieces {
		get {
			currentPiecesBacking.Clear();
			for (int file = 1; file <= 8; file++) {
				for (int rank = 1; rank <= 8; rank++) {
					Piece piece = CurrentBoard[file, rank];
					if (piece != null) currentPiecesBacking.Add((new Square(file, rank), piece));
				}
			}

			return currentPiecesBacking;
		}
	}


	private readonly List<(Square, Piece)> currentPiecesBacking = new List<(Square, Piece)>();
	
	[SerializeField] private UnityChessDebug unityChessDebug;
	private Game game;
	private FENSerializer fenSerializer;
	private PGNSerializer pgnSerializer;
	private CancellationTokenSource promotionUITaskCancellationTokenSource;
	private ElectedPiece userPromotionChoice = ElectedPiece.None;
	private Dictionary<GameSerializationType, IGameSerializer> serializersByType;
	private GameSerializationType selectedSerializationType = GameSerializationType.FEN;

	private IUCIEngine uciEngine;
	
	public void Start() {
		VisualPiece.VisualPieceMoved += OnPieceMoved;

		serializersByType = new Dictionary<GameSerializationType, IGameSerializer> {
			[GameSerializationType.FEN] = new FENSerializer(),
			[GameSerializationType.PGN] = new PGNSerializer()
		};
		
		StartNewGame();
		
#if DEBUG_VIEW
		unityChessDebug.gameObject.SetActive(true);
		unityChessDebug.enabled = true;
#endif
	}

	private void OnDestroy() {
		uciEngine?.ShutDown();
	}
	
#if AI_TEST
	public async void StartNewGame(bool isWhiteAI = true, bool isBlackAI = true) {
#else
	public async void StartNewGame(bool isWhiteAI = false, bool isBlackAI = false) {
#endif
		ClosePromotionUI();
		game = new Game();

		this.isWhiteAI = isWhiteAI;
		this.isBlackAI = isBlackAI;

		if (isWhiteAI || isBlackAI) {
			if (uciEngine == null) {
				uciEngine = new MockUCIEngine();
				uciEngine.Start();
			}
			
			await uciEngine.SetupNewGame(game);
			NewGameStartedEvent?.Invoke();

			if (isWhiteAI) {
				Movement bestMove = await uciEngine.GetBestMove(10_000);
				DoAIMove(bestMove);
			}
		} else {
			NewGameStartedEvent?.Invoke();
		}
	}

	public string SerializeGame() {
		return serializersByType.TryGetValue(selectedSerializationType, out IGameSerializer serializer)
			? serializer?.Serialize(game)
			: null;
	}
	
	public void LoadGame(string serializedGame) {
		choosePromotionUIPanel.SetActive(false);
		game = serializersByType[selectedSerializationType].Deserialize(serializedGame);
		NewGameStartedEvent?.Invoke();
	}

	public void ResetGameToHalfMoveIndex(int halfMoveIndex) {
		if (!game.ResetGameToHalfMoveIndex(halfMoveIndex)) return;
		
		UIManager.Instance.SetActivePromotionUI(false);
		promotionUITaskCancellationTokenSource?.Cancel();
		GameResetToHalfMoveEvent?.Invoke();
	}

	private bool TryExecuteMove(Movement move) {
		if (!game.TryExecuteMove(move)) {
			return false;
		}

		HalfMoveTimeline.TryGetCurrent(out HalfMove latestHalfMove);
		if (latestHalfMove.CausedCheckmate || latestHalfMove.CausedStalemate) {
			BoardManager.Instance.SetActiveAllPieces(false);
			GameEndedEvent?.Invoke();
		} else {
			BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(SideToMove);
		}

		MoveExecutedEvent?.Invoke();

		return true;
	}
	
	private async Task<bool> TryHandleSpecialMoveBehaviourAsync(SpecialMove specialMove) {
		switch (specialMove) {
			case CastlingMove castlingMove:
				BoardManager.Instance.CastleRook(castlingMove.RookSquare, castlingMove.GetRookEndSquare());
				return true;
			case EnPassantMove enPassantMove:
				BoardManager.Instance.TryDestroyVisualPiece(enPassantMove.CapturedPawnSquare);
				return true;
			case PromotionMove { PromotionPiece: null } promotionMove:
				UIManager.Instance.SetActivePromotionUI(true);
				BoardManager.Instance.SetActiveAllPieces(false);

				promotionUITaskCancellationTokenSource?.Cancel();
				promotionUITaskCancellationTokenSource = new CancellationTokenSource();
				
				ElectedPiece choice = await Task.Run(GetUserPromotionPieceChoice, promotionUITaskCancellationTokenSource.Token);
				
				UIManager.Instance.SetActivePromotionUI(false);
				BoardManager.Instance.SetActiveAllPieces(true);

				if (promotionUITaskCancellationTokenSource == null
				    || promotionUITaskCancellationTokenSource.Token.IsCancellationRequested
				) { return false; }

				promotionMove.SetPromotionPiece(
					PromotionUtil.GeneratePromotionPiece(choice, SideToMove)
				);
				BoardManager.Instance.TryDestroyVisualPiece(promotionMove.Start);
				BoardManager.Instance.TryDestroyVisualPiece(promotionMove.End);
				BoardManager.Instance.CreateAndPlacePieceGO(promotionMove.PromotionPiece, promotionMove.End);

				promotionUITaskCancellationTokenSource = null;
				return true;
			case PromotionMove promotionMove:
				BoardManager.Instance.TryDestroyVisualPiece(promotionMove.Start);
				BoardManager.Instance.TryDestroyVisualPiece(promotionMove.End);
				BoardManager.Instance.CreateAndPlacePieceGO(promotionMove.PromotionPiece, promotionMove.End);
				
				return true;
			default:
				return false;
		}
	}
	
	private ElectedPiece GetUserPromotionPieceChoice() {
		while (userPromotionChoice == ElectedPiece.None) { }

		ElectedPiece result = userPromotionChoice;
		userPromotionChoice = ElectedPiece.None;
		return result;
	}
	
	public void ElectPiece(ElectedPiece choice) {
		userPromotionChoice = choice;
	}

	private async void OnPieceMoved(Square movedPieceInitialSquare, Transform movedPieceTransform, Transform closestBoardSquareTransform, Piece promotionPiece = null) {
		Square endSquare = new Square(closestBoardSquareTransform.name);

		string source = movedPieceInitialSquare.ToString();
		string target = endSquare.ToString();
		Piece movedPiece = CurrentBoard[movedPieceInitialSquare];

		// Data to send to the socket server if format: { "room_id": "###", "color": "1|2", "move": { "source": "c7", "target": "c5", "piece": "bP" } }
		var data = new {
			room_id = UserData.Instance.currentRoom.roomId,
			color = movedPiece.Owner == Side.White ? "1" : "2",
			move = new {
				source = source,
				target = target,
				piece = movedPiece.ToShortAlgebraic()
			}
		};

		Debug.Log($"Piece {movedPiece.ToShortAlgebraic()} moved from {source} to {target}");

		SocketManager.Instance.socket.Emit("piece_move", response =>
        {
            try
            {
                StatusResponse respons = response.GetValue<StatusResponse>(0);


                if (respons.status == "success")
                {
                    string fen = respons.data;
					Debug.Log(fen);

					UnityThread.executeInUpdate(() =>
                    {
                        LoadGame(fen);
                    });
                } else {
					NotificationsManager.Instance.ShowNotification(respons.message, 3, "error");
					// It is not a legal move.
					// Reset the piece to its original position.
					movedPieceTransform.position = movedPieceTransform.parent.position;
					Debug.Log(respons.message);
				}
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
            }
        }, data);

		return;
	}

	private void DoAIMove(Movement move) {
		GameObject movedPiece = BoardManager.Instance.GetPieceGOAtPosition(move.Start);
		GameObject endSquareGO = BoardManager.Instance.GetSquareGOByPosition(move.End);
		OnPieceMoved(
			move.Start,
			movedPiece.transform,
			endSquareGO.transform,
			(move as PromotionMove)?.PromotionPiece
		);
	}

	public bool HasLegalMoves(Piece piece) {
		return game.TryGetLegalMovesForPiece(piece, out _);
	}

	// Return list of possible moves for a specific piece on the board.
	public ICollection<Movement> GetLegalMovesForPiece(Piece piece) {
		return game.TryGetLegalMovesForPiece(piece, out ICollection<Movement> legalMoves)
			? legalMoves
			: null;
	}

	private VisualPiece pieceToMoveTemp;
	private Piece movedPieceTemp;
	// Piece move
	public void MovePiece(Square start, Square end, VisualPiece pieceToMove) {
		pieceToMoveTemp = pieceToMove;
		movedPieceTemp = CurrentBoard[start];

		Debug.Log($"Try piece move {movedPieceTemp.ToShortAlgebraic()} from {start} to {end}");

		pieceMove = new PieceMove
		{
			room_id = UserData.Instance.currentRoom.roomId,
			color = movedPieceTemp.Owner == Side.White ? "1" : "2",
			move = new MoveData
			{
				source = start.ToString(),
				target = end.ToString(),
				piece = movedPieceTemp.ToShortAlgebraic(),
				promotedPiece = ""
			}
		};

		// Check if the move is pawn promotion
		if (movedPieceTemp is Pawn && (end.Rank == 1 || end.Rank == 8)) {
			// Show the promotion UI
			OpenPromotionUI();
			return;
		}

		SendPieceMoved();
	}

	// Open the promotion UI and set it to possition in fromn of the camera and lock it to the camera movement
	public void OpenPromotionUI() {
		// Set the position of the promotion UI to be in front of the camera
		choosePromotionUIPanel.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 2;
		choosePromotionUIPanel.transform.rotation = Camera.main.transform.rotation;

		choosePromotionUIPanel.SetActive(true);
	}

	// Promotion execute
	public void PromotionExecute(string promotedPiece) {
		// Set the promoted piece to the piece move data
		pieceMove.move.promotedPiece = promotedPiece;

		// Send the piece move to the server
		SendPieceMoved();

		ClosePromotionUI();
	}

	// Send piece_moved to the server
	public void SendPieceMoved() {
		Debug.Log($"Piece {movedPieceTemp.ToShortAlgebraic()} moved from {pieceMove.move.source} to {pieceMove.move.target}. Room ID: {pieceMove.room_id}");
		SocketManager.Instance.socket.Emit("piece_move", response =>
        {
            try
            {
                StatusResponse respons = response.GetValue<StatusResponse>(0);


                if (respons.status == "success")
                {
                    string fen = respons.data;
					Debug.Log(fen);

					UnityThread.executeInUpdate(() =>
                    {
						pieceToMoveTemp.ToggleHighlight(false);
						pieceToMoveTemp.RemoveAllHighlights();
                        LoadGame(fen);
                    });
                } else {
					NotificationsManager.Instance.ShowNotification(respons.message, 3, "error");
					// It is not a legal move.
					// Reset the piece to its original position.
					//movedPieceTransform.position = movedPieceTransform.parent.position;
					Debug.Log(respons.message);
					UnityThread.executeInUpdate(() =>
                    {
                        pieceToMoveTemp.ToggleHighlight(false);
						pieceToMoveTemp.RemoveAllHighlights();
                    });
				}
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
            }
        }, pieceMove);
	}

	// Close the promotion UI and unlock it from the camera movement
	public void ClosePromotionUI() {
		choosePromotionUIPanel.SetActive(false);
	}

	// On promotion buttons click
	
	// On queen promotion button click
	public void OnQueenPromotionClick() {
		PromotionExecute("q");
	}

	// On rook promotion button click
	public void OnRookPromotionClick() {
		PromotionExecute("r");
	}

	// On bishop promotion button click
	public void OnBishopPromotionClick() {
		PromotionExecute("b");
	}

	// On knight promotion button click
	public void OnKnightPromotionClick() {
		PromotionExecute("n");
	}

	// On promotion cancel button click
	public void OnPromotionCancelClick() {
		ClosePromotionUI();

		// Reset the selected piece
		pieceToMoveTemp.ToggleHighlight(false);
		pieceToMoveTemp.RemoveAllHighlights();
		pieceToMoveTemp = null;
		movedPieceTemp = null;
		selectedPiece = null;
	}


}