using shared;
using System;

namespace server
{
	/**
	 * This room runs a single Game (at a time). 
	 * 
	 * The 'Game' is very simple at the moment:
	 *	- all client moves are broadcasted to all clients
	 *	
	 * The game has no end yet (that is up to you), in other words:
	 * all players that are added to this room, stay in here indefinitely.
	 */
	class GameRoom : Room
	{
		public bool IsGameInPlay { get; private set; }
		public int instance;

		//wraps the board to play on...
		private TicTacToeBoard _board = new TicTacToeBoard();

		public GameRoom(TCPGameServer pOwner) : base(pOwner)
		{
		}

		public void StartGame (TcpMessageChannel pPlayer1, TcpMessageChannel pPlayer2)
		{
			if (IsGameInPlay) throw new Exception("Programmer error duuuude.");

			IsGameInPlay = true;
			addMember(pPlayer1);
			addMember(pPlayer2);
			StartGamePacket startGamePacket = new StartGamePacket(_server.GetPlayerInfo(pPlayer1).name,_server.GetPlayerInfo(pPlayer2).name);
			Log.LogInfo("\nPlayer 1 name: " + startGamePacket.player1Name + "\nPlayer 2 name: " + startGamePacket.player2Name, this);
			sendToAll(startGamePacket);
		}

		protected override void addMember(TcpMessageChannel pMember)
		{
			base.addMember(pMember);

			//notify client he has joined a game room 
			RoomJoinedEvent roomJoinedEvent = new RoomJoinedEvent();
			roomJoinedEvent.room = RoomJoinedEvent.Room.GAME_ROOM;
			pMember.SendMessage(roomJoinedEvent);
		}

		public override void Update()
		{
			//demo of how we can tell people have left the game...
			int oldMemberCount = memberCount;
			base.Update();
			int newMemberCount = memberCount;

			if (oldMemberCount != newMemberCount)
			{
				Log.LogInfo("People left the game...", this);
			}
		}

		protected override void handleNetworkMessage(ASerializable pMessage, TcpMessageChannel pSender)
		{
			if (pMessage is MakeMoveRequest)
			{
				handleMakeMoveRequest(pMessage as MakeMoveRequest, pSender);
			}
		}

		private void handleMakeMoveRequest(MakeMoveRequest pMessage, TcpMessageChannel pSender)
		{
			//we have two players, so index of sender is 0 or 1, which means playerID becomes 1 or 2
			int playerID = indexOfMember(pSender) + 1;
			//make the requested move (0-8) on the board for the player
			_board.MakeMove(pMessage.move, playerID);

			//and send the result of the boardstate back to all clients
			MakeMoveResult makeMoveResult = new MakeMoveResult();
			makeMoveResult.whoMadeTheMove = playerID;
			makeMoveResult.boardData = _board.GetBoardData();
			sendToAll(makeMoveResult);
			Log.LogInfo("Send updated board to all players", this);
			winCheck();
		}

		private void winCheck()
		{
			int playerWhoWon = _board.GetBoardData().WhoHasWon();
			if(playerWhoWon != 1 && playerWhoWon != 2)
			{
				return;
			}
            sendToAll(new GameResult());
            for (int i = 0; i < base.getAllMember().Count; i++)
			{
				if(playerWhoWon - 1 == i)
				{
					ChatMessage winMessage = new ChatMessage();
					winMessage.message = "You won!";
					base.getAllMember()[i].SendMessage(winMessage);
				}
				else
				{
                    ChatMessage loseMessage = new ChatMessage();
                    loseMessage.message = "You lost :(";
                    base.getAllMember()[i].SendMessage(loseMessage);
                }
			}
			destroyRoom();
		}

		private void resetBoard()
		{
            MakeMoveResult resetBoardResult = new MakeMoveResult();
            resetBoardResult.boardData = _board.ResetBoardData();
            sendToAll(resetBoardResult);
        }

		private void destroyRoom()
		{
			//Reset scene by sending empty boardstate to players
			for(int i = base.memberCount - 1; i >= 0; i--)
			{
				_server.GetLobbyRoom().AddMember(base.getAllMember()[i]);
				base.removeMember(base.getAllMember()[i]);
			}
			
			_server.removeGameRoom(this);
		}
    }
}
