using Google.Protobuf;
using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
	public class GameRoom : JobSerializer
    {
        enum GameState
        {
            Waiting,
            Yutgame,
            minione,
            minitwo,
            minithree,
            end
        }
        GameState _gamestate;

        enum YutResult
        {
            NAK,
            BACKDO,
            DO,
            GAE,
            GEOL,
            YUT,
            MO
        }

		public int RoomId { get; set; }
        public string RoomName { get; set; }

        static Random random = new Random();

        int[] _yutpos1 = new int[4] { 0, 0, 0, 0 };
        int[] _yutpos2 = new int[4] { 0, 0, 0, 0 };

        Dictionary<int, Player> _players = new Dictionary<int, Player>();
        Player[] _playerArray = new Player[2];
        bool _nowTurn = false;
        List<YutResult> _yutResult = new List<YutResult>();

        public void Init()
        {
            _gamestate = GameState.Waiting;
            Console.WriteLine(RoomName);
        }

        // 누군가 주기적으로 호출해줘야 한다
        public void Update()
        {
            Flush();
        }

        public void EnterGame(GameObject gameObject)
		{
			if (gameObject == null)
				return;

            GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

            if (type == GameObjectType.Player)
            {
                Player player = gameObject as Player;
                _players.Add(gameObject.Id, player);
                player.Room = this;

                // 처음 들어온 두 사람이 플레이어
                if (_playerArray[0] == null )
                    _playerArray[0] = player;
                else if (_playerArray[1] == null)
                    _playerArray[1] = player;

                // 본인한테 정보 전송
                // 이 부분은 나중에 필요 없을듯?
                //{
                //    S_EnterGame enterPacket = new S_EnterGame();
                //    enterPacket.Player = player.Info;
                //    player.Session.Send(enterPacket);

                //    S_Spawn spawnPacket = new S_Spawn();
                //    foreach (Player p in _players.Values)
                //    {
                //        if (player != p)
                //            spawnPacket.Objects.Add(p.Info);
                //    }

                //    player.Session.Send(spawnPacket);
                //}
            }

            // 타인한테 정보 전송
            //{
            //    S_Spawn spawnPacket = new S_Spawn();
            //    spawnPacket.Objects.Add(gameObject.Info);
            //    foreach (Player p in _players.Values)
            //    {
            //        if (p.Id != gameObject.Id)
            //            p.Session.Send(spawnPacket);
            //    }
            //}
        }

        public void LeaveGame(int objectId)
        {
            GameObjectType type = ObjectManager.GetObjectTypeById(objectId);

            if (type == GameObjectType.Player)
            {
                Player player = null;
                if (_players.Remove(objectId, out player) == false)
                    return;

                player.Room = null;

                // 본인한테 정보 전송
                {
                    S_LeaveGame leavePacket = new S_LeaveGame();
                    player.Session.Send(leavePacket);
                }
            }

            // 타인한테 정보 전송
            {
                S_Despawn despawnPacket = new S_Despawn();
                despawnPacket.ObjectIds.Add(objectId);
                foreach (Player p in _players.Values)
                {
                    if (p.Id != objectId)
                        p.Session.Send(despawnPacket);
                }
            }
        }

        public void HandleStartGame()
        {
            if (_playerArray[0] == null || _playerArray[1] == null)
                return;
            else _gamestate = GameState.Yutgame;

            // 처음 턴 결정
            _nowTurn = (random.Next(2) == 0);

            S_StartGame startGamePacket = new S_StartGame();
            startGamePacket.Nowturn = _nowTurn;

            Broadcast(startGamePacket);
        }

        public void HandleThrowYut(Player player)
        {
            
        }

        static YutResult GetYutResult(GameState _gamestate)
        {
            int randomNumber = random.Next(1, 101); // 1부터 100 사이의 랜덤 숫자 생성

            if (randomNumber <= 3) // 확률 3%
                return YutResult.NAK;
            else if (randomNumber <= 7) // 확률 4%
                return YutResult.BACKDO;
            else if (randomNumber <= 25) // 확률 18%
                return YutResult.DO;
            else if (randomNumber <= 55) // 확률 30%
                return YutResult.GAE;
            else if (randomNumber <= 83) // 확률 28%
                return YutResult.GEOL;
            else if (randomNumber <= 96) // 확률 13%
                return YutResult.YUT;
            else // 확률 4%
                return YutResult.MO;
        }

        public void HandleYutMove(Player player, C_YutMove yutmovePacket)
        {
            if (player == null)
                return;
        }

        public void HandleMove(Player player, C_Move movePacket)
        {
            if (player == null)
                return;

            // 서버에서 좌표 이동
            ObjectInfo info = player.Info;
            info.PosInfo = movePacket.PosInfo;

            // 다른 플레이어한테도 알려준다
            S_Move resMovePacket = new S_Move();
            resMovePacket.ObjectId = player.Info.ObjectId;
            resMovePacket.PosInfo = movePacket.PosInfo;

            Console.WriteLine(resMovePacket.ObjectId);
            Console.WriteLine(resMovePacket.PosInfo);
            Broadcast(resMovePacket);
        }

        public void HandleRotation(Player player, C_Rotation rotationPacket)
        {
            if (player == null)
                return;

            // 일단 서버에서 좌표 이동
            ObjectInfo info = player.Info;
            info.RotInfo = rotationPacket.RotInfo;

            // 다른 플레이어한테도 알려준다
            S_Rotation resRotationPacket = new S_Rotation();
            resRotationPacket.ObjectId = player.Info.ObjectId;
            resRotationPacket.RotInfo = rotationPacket.RotInfo;

            Broadcast(resRotationPacket);
        }

        public Player FindPlayer(Func<GameObject, bool> condition)
        {
            foreach (Player player in _players.Values)
            {
                if (condition.Invoke(player))
                    return player;
            }

            return null;
        }

        public void Broadcast(IMessage packet)
        {
            foreach (Player p in _players.Values)
            {
                p.Session.Send(packet);
            }
        }
    }
}
