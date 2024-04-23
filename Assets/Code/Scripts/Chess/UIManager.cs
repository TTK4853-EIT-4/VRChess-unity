﻿using System;
using System.Collections.Generic;
using TMPro;
using UnityChess;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviourSingleton<UIManager> {
	[SerializeField] private GameObject promotionUI = null;
	[SerializeField] private Text resultText = null;
	[SerializeField] private InputField GameStringInputField = null;
	[SerializeField] private Image whiteTurnIndicator = null;
	[SerializeField] private Image blackTurnIndicator = null;
	[SerializeField] private GameObject moveHistoryContentParent = null;
	[SerializeField] private Scrollbar moveHistoryScrollbar = null;
	[SerializeField] private FullMoveUI moveUIPrefab = null;
	[SerializeField] private Text[] boardInfoTexts = null;
	[SerializeField] private Color backgroundColor = new Color(0.39f, 0.39f, 0.39f);
	[SerializeField] private Color textColor = new Color(1f, 0.71f, 0.18f);
	[SerializeField, Range(-0.25f, 0.25f)] private float buttonColorDarkenAmount = 0f;
	[SerializeField, Range(-0.25f, 0.25f)] private float moveHistoryAlternateColorDarkenAmount = 0f;
	

	// Custom fields
	[SerializeField] public TMP_Text detailsText = null;
	[SerializeField] public TMP_Text observersText = null;
	[SerializeField] public GameObject XROrigin = null;
	[SerializeField] public GameObject whiteBoard = null;

	private Timeline<FullMoveUI> moveUITimeline;
	private Color buttonColor;

	private void Start() {
		GameManager.NewGameStartedEvent += OnNewGameStarted;
		GameManager.GameEndedEvent += OnGameEnded;
		GameManager.MoveExecutedEvent += OnMoveExecuted;
		GameManager.GameResetToHalfMoveEvent += OnGameResetToHalfMove;
		GameNetwork.GameDataUpdatedEvent += OnGameDataUpdated;
		
		moveUITimeline = new Timeline<FullMoveUI>();
		foreach (Text boardInfoText in boardInfoTexts) {
			boardInfoText.color = textColor;
		}

		buttonColor = new Color(backgroundColor.r - buttonColorDarkenAmount, backgroundColor.g - buttonColorDarkenAmount, backgroundColor.b - buttonColorDarkenAmount);
	}

	// On Awale
	private void Awake() {
		OnGameDataUpdated();

		// Set the camera view point based on the player side or if the player is an observer
		if (UserData.Instance.playerSide == UserData.PlayerSide.White) {
			// Position: x = 0.7122346, y = 10, z = -12.26
			// Rotation: x = 0, y = 0, z = 0
			XROrigin.transform.position = new Vector3(0.7122346f, 10f, -12.26f);
			XROrigin.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

			// WhiteBoard postinion: x = 16.38169, y = 0, z = 0.9038709
			// WhiteBoard rotation: x = 0, y = -114.46, z = 0
			whiteBoard.transform.position = new Vector3(16.38169f, 0f, 0.9038709f);
			whiteBoard.transform.rotation = Quaternion.Euler(0f, -114.46f, 0f);
			
		} else if (UserData.Instance.playerSide == UserData.PlayerSide.Black) {
			// Position: x = 0.7122346, y = 10, z = 16.1
			// Rotation: x = 0, y = 180, z = 0
			XROrigin.transform.position = new Vector3(0.7122346f, 10f, 16.1f);
			XROrigin.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

			// WhiteBoard postinion: x = 16.64706, y = 0, z = 0.7874842
			// WhiteBoard rotation: x = 0, y = -69.869, z = 0
			whiteBoard.transform.position = new Vector3(16.64706f, 0f, 0.7874842f);
			whiteBoard.transform.rotation = Quaternion.Euler(0f, -69.869f, 0f);
		} else {
			// Position: x = -13.09, y = 10, z = 0.72
			// Rotation: x = 0, y = 90, z = 0
			XROrigin.transform.position = new Vector3(-13.09f, 10f, 0.72f);
			XROrigin.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

			// WhiteBoard postinion: x = 16.53909, y = 0, z = 0.8657131
			// WhiteBoard rotation: x = 0, y = -89.976, z = 0
			whiteBoard.transform.position = new Vector3(16.53909f, 0f, 0.8657131f);
			whiteBoard.transform.rotation = Quaternion.Euler(0f, -89.976f, 0f);
		}
	}

	private void OnGameDataUpdated() {
		string status = "Playing";
		if (UserData.Instance.currentRoom.gameStatus == GameStatus.ENDED) {
			status = "Finished";
		} else if (UserData.Instance.currentRoom.gameStatus == GameStatus.WAITING) {
			status = "Waiting";
		}

		string opponent = "None";
		string opponentSide = "";
		if (UserData.Instance.currentRoom.roomOpponent != null) {
			opponent = UserData.Instance.currentRoom.roomOpponent.username;
			opponentSide = UserData.Instance.currentRoom.roomOwnerSide == SideColor.WHITE ? "(b)" : "(w)";
		}

		string ownerSide = UserData.Instance.currentRoom.roomOwnerSide == SideColor.WHITE ? "(w)" : "(b)";

		detailsText.text = "Game Status: " + status + "\n";
		detailsText.text += "Owner: " + UserData.Instance.currentRoom.roomOwner.username + " " + ownerSide + "\n";
		detailsText.text += "Opponent: " + opponent + " " + opponentSide + "\n";

		if(UserData.Instance.currentRoom.gameStatus == GameStatus.ENDED) {
			detailsText.text += "Winner: " + UserData.Instance.currentRoom.gameWinner.username + "\n";
		}

		observersText.text = "";
		int i = 1;
		foreach (User observer in UserData.Instance.currentRoom.observers) {
			string you = observer.username == UserData.Instance.loggedUser.username ? " (You)" : "";
			observersText.text += i.ToString() + ". " + observer.username + you + "\n";
			i++;
		}
	}

	private void OnNewGameStarted() {
		UpdateGameStringInputField();
		ValidateIndicators();
		
		for (int i = 0; i < moveHistoryContentParent.transform.childCount; i++) {
			Destroy(moveHistoryContentParent.transform.GetChild(i).gameObject);
		}
		
		moveUITimeline.Clear();

		resultText.gameObject.SetActive(false);
	}

	private void OnGameEnded() {
		GameManager.Instance.HalfMoveTimeline.TryGetCurrent(out HalfMove latestHalfMove);

		if (latestHalfMove.CausedCheckmate) {
			resultText.text = $"{latestHalfMove.Piece.Owner} Wins!";
		} else if (latestHalfMove.CausedStalemate) {
			resultText.text = "Draw.";
		}

		resultText.gameObject.SetActive(true);
	}

	private void OnMoveExecuted() {
		UpdateGameStringInputField();
		Side sideToMove = GameManager.Instance.SideToMove;
		whiteTurnIndicator.enabled = sideToMove == Side.White;
		blackTurnIndicator.enabled = sideToMove == Side.Black;

		GameManager.Instance.HalfMoveTimeline.TryGetCurrent(out HalfMove lastMove);
		AddMoveToHistory(lastMove, sideToMove.Complement());
	}

	private void OnGameResetToHalfMove() {
		UpdateGameStringInputField();
		moveUITimeline.HeadIndex = GameManager.Instance.LatestHalfMoveIndex / 2;
		ValidateIndicators();
	}

	public void SetActivePromotionUI(bool value) => promotionUI.gameObject.SetActive(value);

	public void OnElectionButton(int choice) => GameManager.Instance.ElectPiece((ElectedPiece)choice);

	public void ResetGameToFirstHalfMove() => GameManager.Instance.ResetGameToHalfMoveIndex(0);

	public void ResetGameToPreviousHalfMove() => GameManager.Instance.ResetGameToHalfMoveIndex(Math.Max(0, GameManager.Instance.LatestHalfMoveIndex - 1));

	public void ResetGameToNextHalfMove() => GameManager.Instance.ResetGameToHalfMoveIndex(Math.Min(GameManager.Instance.LatestHalfMoveIndex + 1, GameManager.Instance.HalfMoveTimeline.Count - 1));

	public void ResetGameToLastHalfMove() => GameManager.Instance.ResetGameToHalfMoveIndex(GameManager.Instance.HalfMoveTimeline.Count - 1);

	public void StartNewGame() => GameManager.Instance.StartNewGame();
	
	public void LoadGame() => GameManager.Instance.LoadGame(GameStringInputField.text);

	private void AddMoveToHistory(HalfMove latestHalfMove, Side latestTurnSide) {
		RemoveAlternateHistory();
		
		switch (latestTurnSide) {
			case Side.Black: {
				if (moveUITimeline.HeadIndex == -1) {
					FullMoveUI newFullMoveUI = Instantiate(moveUIPrefab, moveHistoryContentParent.transform);
					moveUITimeline.AddNext(newFullMoveUI);
					
					newFullMoveUI.transform.SetSiblingIndex(GameManager.Instance.FullMoveNumber - 1);
					newFullMoveUI.backgroundImage.color = backgroundColor;
					newFullMoveUI.whiteMoveButtonImage.color = buttonColor;
					newFullMoveUI.blackMoveButtonImage.color = buttonColor;
					
					if (newFullMoveUI.FullMoveNumber % 2 == 0) {
						newFullMoveUI.SetAlternateColor(moveHistoryAlternateColorDarkenAmount);
					}

					newFullMoveUI.MoveNumberText.text = $"{newFullMoveUI.FullMoveNumber}.";
					newFullMoveUI.WhiteMoveButton.enabled = false;
				}
				
				moveUITimeline.TryGetCurrent(out FullMoveUI latestFullMoveUI);
				latestFullMoveUI.BlackMoveText.text = latestHalfMove.ToAlgebraicNotation();
				latestFullMoveUI.BlackMoveButton.enabled = true;
				
				break;
			}
			case Side.White: {
				FullMoveUI newFullMoveUI = Instantiate(moveUIPrefab, moveHistoryContentParent.transform);
				newFullMoveUI.transform.SetSiblingIndex(GameManager.Instance.FullMoveNumber - 1);
				newFullMoveUI.backgroundImage.color = backgroundColor;
				newFullMoveUI.whiteMoveButtonImage.color = buttonColor;
				newFullMoveUI.blackMoveButtonImage.color = buttonColor;

				if (newFullMoveUI.FullMoveNumber % 2 == 0) {
					newFullMoveUI.SetAlternateColor(moveHistoryAlternateColorDarkenAmount);
				}

				newFullMoveUI.MoveNumberText.text = $"{newFullMoveUI.FullMoveNumber}.";
				newFullMoveUI.WhiteMoveText.text = latestHalfMove.ToAlgebraicNotation();
				newFullMoveUI.BlackMoveText.text = "";
				newFullMoveUI.BlackMoveButton.enabled = false;
				newFullMoveUI.WhiteMoveButton.enabled = true;
				
				moveUITimeline.AddNext(newFullMoveUI);
				break;
			}
		}

		moveHistoryScrollbar.value = 0;
	}

	private void RemoveAlternateHistory() {
		if (!moveUITimeline.IsUpToDate) {
			GameManager.Instance.HalfMoveTimeline.TryGetCurrent(out HalfMove lastHalfMove);
			resultText.gameObject.SetActive(lastHalfMove.CausedCheckmate);
			List<FullMoveUI> divergentFullMoveUIs = moveUITimeline.PopFuture();
			foreach (FullMoveUI divergentFullMoveUI in divergentFullMoveUIs) {
				Destroy(divergentFullMoveUI.gameObject);
			}
		}
	}

	private void ValidateIndicators() {
		Side sideToMove = GameManager.Instance.SideToMove;
		whiteTurnIndicator.enabled = sideToMove == Side.White;
		blackTurnIndicator.enabled = sideToMove == Side.Black;
	}

	private void UpdateGameStringInputField() => GameStringInputField.text = GameManager.Instance.SerializeGame();
}