using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
    public class StartGamePacket : ASerializable
    {
        public string player1Name;
        public string player2Name;
        public TicTacToeBoardData boardData;
        public int startingMoves;

        public override void Deserialize(Packet pPacket)
        {
            //throw new NotImplementedException();
            player1Name = pPacket.ReadString();
            player2Name = pPacket.ReadString();
            boardData = pPacket.Read<TicTacToeBoardData>();
            startingMoves = pPacket.ReadInt();
        }

        public override void Serialize(Packet pPacket)
        {
            //throw new NotImplementedException();
            pPacket.Write(player1Name);
            pPacket.Write(player2Name);
            pPacket.Write(boardData);
            pPacket.Write(startingMoves);
        }

        public StartGamePacket(string pPlayer1Name, string pPlayer2Name)
        {
            player1Name = pPlayer1Name;
            player2Name = pPlayer2Name;

            boardData = new TicTacToeBoardData();
            boardData.board = new int[9] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            startingMoves = 0;
        }
        public StartGamePacket() { }
    }
}
