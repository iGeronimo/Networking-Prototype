using shared;

/**
 * This is where we 'play' a game.
 */
public class GameState : ApplicationStateWithView<GameView>
{
    //just for fun we keep track of how many times a player clicked the board
    //note that in the current application you have no idea whether you are player 1 or 2
    //normally it would be better to maintain this sort of info on the server if it is actually important information
    private int player1MoveCount = 0;
    private int player2MoveCount = 0;

    private string player1Name = "";
    private string player2Name = "";

    public override void EnterState()
    {
        base.EnterState();
        
        view.gameBoard.OnCellClicked += _onCellClicked;
    }

    private void _onCellClicked(int pCellIndex)
    {
        MakeMoveRequest makeMoveRequest = new MakeMoveRequest();
        makeMoveRequest.move = pCellIndex;

        fsm.channel.SendMessage(makeMoveRequest);
    }

    public override void ExitState()
    {
        base.ExitState();
        view.gameBoard.OnCellClicked -= _onCellClicked;
    }

    private void Update()
    {
        receiveAndProcessNetworkMessages();
    }

    protected override void handleNetworkMessage(ASerializable pMessage)
    {
        if (pMessage is MakeMoveResult)
        {
            handleMakeMoveResult(pMessage as MakeMoveResult);
        }
        else if(pMessage is StartGamePacket)
        {
            handleStartGamePacket(pMessage as StartGamePacket);
        }
        else if(pMessage is GameResult)
        {
            handleGameResultPacket(pMessage as GameResult);
        }
    }

    private void handleStartGamePacket(StartGamePacket pPacket)
    {
        player1Name = "P1:" + pPacket.player1Name;
        player2Name = "P2:" + pPacket.player2Name;
        view.gameBoard.SetBoardData(pPacket.boardData);
        player1MoveCount = pPacket.startingMoves;
        player2MoveCount = pPacket.startingMoves;

        view.playerLabel1.text = $"{player1Name} (Movecount: {player1MoveCount})";
        view.playerLabel2.text = $"{player2Name} (Movecount: {player2MoveCount})";
    }

    private void handleMakeMoveResult(MakeMoveResult pMakeMoveResult)
    {
        view.gameBoard.SetBoardData(pMakeMoveResult.boardData);

        //some label display
        if (pMakeMoveResult.whoMadeTheMove == 1)
        {
            player1MoveCount++;
            view.playerLabel1.text = $"{player1Name} (Movecount: {player1MoveCount})";
        }
        if (pMakeMoveResult.whoMadeTheMove == 2)
        {
            player2MoveCount++;
            view.playerLabel2.text = $"{player2Name} (Movecount: {player2MoveCount})";
        }

    }

    private void handleGameResultPacket(GameResult pGameResult)
    {
        fsm.ChangeState<LobbyState>();
    }
}
