using Google.Protobuf;
using Google.Protobuf.Protocol;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
	public class GameRoom : JobSerializer
    {
        public GameState _gamestate = GameState.Waiting;

		public int RoomId { get; set; }
        public string RoomName { get; set; }

        static Random random = new Random();

        int[] _yutpos1 = new int[4] { 0, 0, 0, 0 };
        int[] _yutpos2 = new int[4] { 0, 0, 0, 0 };

        Dictionary<int, Player> _players = new Dictionary<int, Player>();
        Player[] _playerArray = new Player[2];
        public bool _nowTurn = false; // 0이면 player1, 1이면 player2가 턴
        List<YutResult> _yutResult = new List<YutResult>();

        private static int minigametime = 6000;
        int _timer = 0;
        public bool _playerdisconnect = false;

        public void Init()
        {
            _gamestate = GameState.Waiting;
            if (RoomName == null)
                RoomName = "default room name";
            Console.WriteLine("Room Id : " + RoomId);
            Console.WriteLine(RoomName);
        }

        // 누군가 주기적으로 호출해줘야 한다
        public void Update()
        {
            Flush();
            CheckMini2TimeSet();
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
            }

            //HandleStartGame();
            //SpawnGame2Player(gameObject);
        }

        public void LeaveGame(int objectId)
        {
            GameObjectType type = ObjectManager.GetObjectTypeById(objectId);

            if (type == GameObjectType.Player)
            {
                Player player = null;
                if (_players.Remove(objectId, out player) == false)
                    return;
                for(int i = 0; i < _playerArray.Length; i++)
                {
                    if (player == _playerArray[i])
                        _playerArray[i] = null;
                }
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

            CatchHorse();
        }

        bool isPlayerTurn(Player player)
        {
            if (player == null) return false;
            if (_nowTurn == true && player == _playerArray[0])
            {
                return true; // player1의 턴일 때 true 반환
            }
            else if (_nowTurn == false && player == _playerArray[1])
            {
                return true; // player2의 턴일 때 true 반환
            }
            else
            {
                return false; // 다른 경우에는 false 반환
            }
        }

        public void HandleThrowYut(Player throwplayer)
        {
            //if(_gamestate != GameState.Yutgame) return;
            //if (isPlayerTurn(throwplayer) == false) return;

            S_ThrowYut throwyutPacket = new S_ThrowYut();
            YutResult randomyut = GetYutResult();
            throwyutPacket.Result = randomyut;

            if(!(randomyut == YutResult.Yut) && !(randomyut == YutResult.Mo))
            {
                _nowTurn = !_nowTurn;
            }

            Broadcast(throwyutPacket);
            Console.WriteLine(randomyut);
        }

        static YutResult GetYutResult()
        {
            int randomNumber = random.Next(1, 101); // 1부터 100 사이의 랜덤 숫자 생성

            if (randomNumber <= 3) // 확률 3%
                return YutResult.Nak;
            else if (randomNumber <= 7) // 확률 4%
                return YutResult.Backdo;
            else if (randomNumber <= 25) // 확률 18%
                return YutResult.Do;
            else if (randomNumber <= 55) // 확률 30%
                return YutResult.Gae;
            else if (randomNumber <= 83) // 확률 28%
                return YutResult.Geol;
            else if (randomNumber <= 96) // 확률 13%
                return YutResult.Yut;
            else // 확률 4%
                return YutResult.Mo;
        }

        public void HandleYutMove(Player player, C_YutMove yutmovePacket)
        {
            if (player == null)
                return;

            //if (_gamestate != GameState.Yutgame) return;

            S_YutMove syutmovePacket = new S_YutMove();
            syutmovePacket.PlayerId = player.Id;
            syutmovePacket.UseResult = yutmovePacket.UseResult;
            syutmovePacket.MovedYut = yutmovePacket.MovedYut;
            syutmovePacket.MovedPos = yutmovePacket.MovedPos;
            Broadcast(syutmovePacket);

            Console.WriteLine("use yut : " + syutmovePacket.UseResult);
            Console.WriteLine("move yut : " + syutmovePacket.MovedYut);
            Console.WriteLine("yut dest : " + syutmovePacket.MovedPos);
        }

        public void CatchHorse()
        {
            _gamestate = GameState.Minitwo;

            //GameTimer = new Timer(TimeSetCallback, null, _minigameTime, Timeout.Infinite);

            for (int i = 0; i < _playerArray.Length; i++)
            {
                SpawnGame2Player(_playerArray[i]);
            }

            S_HorseCatch horseCatchPacket = new S_HorseCatch();
            horseCatchPacket.Playtime = minigametime;
            Broadcast(horseCatchPacket);
        }

        public void SpawnGame2Player(GameObject gameObject)
        {
            Player player = gameObject as Player;
            InitPlayerInfo(player);
            // 본인한테 정보 전송
            {
                S_EnterGame enterPacket = new S_EnterGame();
                enterPacket.Player = player.Info;
                player.Session.Send(enterPacket);

                //S_Spawn spawnPacket = new S_Spawn();
                //foreach (Player p in _players.Values)
                //{
                //    if (player != p)
                //        spawnPacket.Objects.Add(p.Info);
                //}

                //player.Session.Send(spawnPacket);
            }

            // 타인한테 정보 전송
            {
                S_Spawn spawnPacket = new S_Spawn();
                spawnPacket.Objects.Add(gameObject.Info);
                foreach (Player p in _players.Values)
                {
                    if (p.Id != gameObject.Id)
                        p.Session.Send(spawnPacket);
                }
            }
        }

        public void InitPlayerInfo(Player MyPlayer)
        {
            MyPlayer.Info.PosInfo.PosX = 1;
            MyPlayer.Info.PosInfo.PosY = 50;
            MyPlayer.Info.PosInfo.PosZ = 1;
            MyPlayer.Info.RotInfo.RotX = 0;
            MyPlayer.Info.RotInfo.RotY = 0;
            MyPlayer.Info.RotInfo.RotZ = 0;
            MyPlayer.Info.RotInfo.RotW = 0;
            MyPlayer.Info.StatInfo.Hp = 5;
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

            Broadcast(resMovePacket);

            if (player.Info.PosInfo.PosY <= 10)
            {
                PlayerDie(player, false);
                Console.WriteLine("die fall");
            }
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

        public void HandlePlayerAttack(Player player)
        {
            if (player == null)
                return;

            S_DoAttack atkPacket = new S_DoAttack();
            atkPacket.ObjectId = player.Info.ObjectId;
            Broadcast(atkPacket);

            for (int i = 0; i < _playerArray.Length; i++) {
                if (_playerArray[i] == null) return;

                if (_playerArray[i] != player)
                {
                    if(IsAttacked(3f, player, _playerArray[i]))
                    {
                        Vector3 attackedDirection = CalcAtkDirection(player, _playerArray[i]);
                        PlayerAttacked(_playerArray[i], attackedDirection);
                    }
                    
                }
            }
        }

        private static bool IsAttacked(float dist, Player player1, Player player2)
        {
            float deltaX = player1.Info.PosInfo.PosX - player2.Info.PosInfo.PosX;
            float deltaY = player1.Info.PosInfo.PosY - player2.Info.PosInfo.PosY;
            float deltaZ = player1.Info.PosInfo.PosZ - player2.Info.PosInfo.PosZ;

            // 거리 계산 (유클리드 거리)
            float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
            Console.WriteLine("distance : " + distance);
            if (distance < dist)
                return true;
            else return false;

        }

        private static Vector3 CalcAtkDirection(Player player1, Player player2)
        {
            Vector3 atkdir;

            atkdir.X = player1.Info.PosInfo.PosX - player2.Info.PosInfo.PosX;
            atkdir.Y = 0;
            atkdir.Z = player1.Info.PosInfo.PosZ - player2.Info.PosInfo.PosZ;
            Vector3.Normalize(atkdir);
            atkdir.Y = 5f;

            return atkdir;
        }

        private void PlayerAttacked(Player player, Vector3 attackedDirection)
        {
            if (player == null)
                return;

            S_PlayerAttacked attackedPacket = new S_PlayerAttacked();
            attackedPacket.ObjectId = player.Id;
            attackedPacket.AttackedDirection = new PositionInfo
            {
                PosX = -attackedDirection.X,
                PosY = attackedDirection.Y,
                PosZ = -attackedDirection.Z
            };
            attackedPacket.AttackForce = 10f;

            Broadcast(attackedPacket);

            player.Stat.Hp -= 1;
            Console.WriteLine("Player Id : " + player.Id);
            Console.WriteLine("Player Hp : " + player.Stat.Hp);
            if (player.Stat.Hp <= 0)
            {
                PlayerDie(player, false);
                Console.WriteLine("die hit");
            }
        }

        public void PlayerDie(Player player, bool istimeset)
        {
            if (player == null) return;
            S_Die diePacket = new S_Die();
            diePacket.ObjectId = player.Id;
            diePacket.Timeset = istimeset;

            Broadcast(diePacket);

            _gamestate = GameState.Yutgame;
            _timer = 0;
        }

        private void Mini2TimeSet()
        {
            int dieplayer;

            for (int i = 0; i < _playerArray.Length; i++)
            {
                if (_playerArray[i] == null)
                {
                    return;
                }
            }

            if (_playerArray[0].Stat.Hp > _playerArray[1].Stat.Hp)
            {
                dieplayer = 1;
            }
            else dieplayer = 0;

            Console.WriteLine("Time Set");
            Console.WriteLine("Win Player : " + _playerArray[dieplayer].Id);
            PlayerDie(_playerArray[dieplayer], true);
        }

        private void CheckMini2TimeSet()
        {
            if (_gamestate != GameState.Minitwo) return;

            _timer += 5;
            if (_timer % 100 == 0)
                Console.WriteLine(_timer / 100);
            if (_timer >= minigametime)
            {
                Mini2TimeSet();
            }
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
