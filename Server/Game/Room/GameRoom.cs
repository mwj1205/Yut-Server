using Google.Protobuf;
using Google.Protobuf.Protocol;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using static System.Formats.Asn1.AsnWriter;

namespace Server.Game
{
	public class GameRoom : JobSerializer
    {
        public GameState _gamestate = GameState.Waiting;

		public int RoomId { get; set; }
        public string RoomName { get; set; }

        static Random random = new Random();

        Dictionary<int, Player> _players = new Dictionary<int, Player>();
        int _numOfPlayer = 2;
        int _numofHorse = 4;
        Player[] _playerArray = new Player[2];


        public int _nowTurn; // 0이면 player1, 1이면 player2가 턴
        public int _yutChance;
        public List<int> steps = new List<int>();

        public int _wingamePlayer;

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

                if (_playerArray[0] == null)
                    _playerArray[0] = player;
                else if (_playerArray[1] == null)
                    _playerArray[1] = player;
                else
                {
                    Console.WriteLine("room is full");
                    return;
                }

                _players.Add(gameObject.Id, player);
                player.Room = this;
            }

            HandleStartGame();
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
            _nowTurn = random.Next(_numOfPlayer);
            _yutChance = 1;

            S_StartGame startGamePacket = new S_StartGame();
            startGamePacket.Nowturn = _nowTurn;

            Broadcast(startGamePacket);
        }

        bool isPlayerTurn(Player player)
        {
            if (player == null) return false;
            if (_nowTurn == 0 && player == _playerArray[0])
            {
                return true; // player1의 턴일 때 true 반환
            }
            else if (_nowTurn == 1 && player == _playerArray[1])
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
            YutResult randomyut = YutGameUtil.Instance.GetYutResult();
            throwyutPacket.Result = randomyut;
            steps.Add(YutGameUtil.Instance.convertYutResult(randomyut));

            if(!(randomyut == YutResult.Yut) && !(randomyut == YutResult.Mo))
            {
                _yutChance--;
            }

            Broadcast(throwyutPacket);
            Console.WriteLine(randomyut);

            if(_yutChance == 0)
                CalcHorseDestination();
        }

        public void HandleYutMove(Player player, C_YutMove yutmovePacket)
        {
            if (player == null)
                return;

            //if (_gamestate != GameState.Yutgame) return;

            // TODO
            // 온 데이터가 맞는지 확인 (calc랑 비교할 예정)
            YutHorse movehorse = _playerArray[_nowTurn]._horses[yutmovePacket.MovedYut];

            if(movehorse._nowPosition == 0 && steps[yutmovePacket.UseResult] == -1)
            {
                Console.WriteLine("backdo : no on field");
                return;
            }


            if (steps[yutmovePacket.UseResult] == -1)
            {
                bool isallstart = true;
                for (int i = 0; i < _numofHorse; i++)
                {
                    if (_playerArray[_nowTurn]._horses[i]._nowPosition != 0)
                    {
                        isallstart = false;
                        break;
                    }
                }

                if (isallstart)
                {
                    S_YutMove yutpacket = new S_YutMove();
                    yutpacket.PlayerId = player.Id;
                    yutpacket.UseResult = yutmovePacket.UseResult;
                    yutpacket.MovedYut = yutmovePacket.MovedYut;
                    yutpacket.MovedPos = movehorse._nowPosition;
                    Broadcast(yutpacket);

                    Console.WriteLine("can't move backdo");
                    nextturn();
                    return;
                }
            }

            YutMove(movehorse, steps[yutmovePacket.UseResult]);
            MoveBindYut(movehorse, steps[yutmovePacket.UseResult]);
            steps.RemoveAt(yutmovePacket.UseResult);
            movehorse._destPosition.RemoveAt(yutmovePacket.UseResult);
            HorseBind(movehorse);
            CalcHorseDestination();

            if (movehorse._doDefenceGame)
            {
                //_gamestate = GameState.Minione;
            }
            else if (movehorse._doHammerGame)
            {
                //_gamestate = GameState.Minitwo;
                Console.WriteLine("lets hammer");
                //StartMiniGame2();
                HammerGameEnd(_playerArray[_nowTurn], movehorse);
            }

            if (movehorse._isbind)
            {
                for (int i = 0; i < movehorse.bindhorseList.Count; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        if (movehorse.bindhorseList[i] == _playerArray[_nowTurn]._horses[j])
                        {
                            Console.WriteLine("bind with : " + j);
                            Console.WriteLine("bind pos : " + movehorse.bindhorseList[i]._nowPosition);
                        }
                    }
                }
            }

            if (steps.Count <= 0 && _yutChance <= 0)
            {
                nextturn();
            }

            S_YutMove syutmovePacket = new S_YutMove();
            syutmovePacket.PlayerId = player.Id;
            syutmovePacket.UseResult = yutmovePacket.UseResult;
            syutmovePacket.MovedYut = yutmovePacket.MovedYut;
            syutmovePacket.MovedPos = movehorse._nowPosition;
            Broadcast(syutmovePacket);

            Console.WriteLine("moved yut : " + yutmovePacket.MovedYut);
            Console.WriteLine("yut dest : " + syutmovePacket.MovedPos);


        }

        void nextturn()
        {
            _nowTurn++;
            if (_nowTurn >= _numOfPlayer)
                _nowTurn = 0;
            _yutChance = 1;
            Console.WriteLine("next turn");
        }

        void clearYutDest()
        {
            for (int i = 0; i < _numofHorse; i++)
            {
                _playerArray[_nowTurn]._horses[i]._destPosition.Clear();
            }
        }

        void YutMove(YutHorse horse, int stepsLeft)
        {
            int tempStepleft = stepsLeft;
            int calcPos = horse._nowPosition;

            if (tempStepleft == -1)
            {
                calcPos = YutGameUtil.Instance.BackdoRoute(horse);
            }
            else
            {
                while (tempStepleft > 0)
                {

                    int NormalRoute = YutGameUtil.Instance.NormalRoute(horse._nowPosition, calcPos);
                    if (NormalRoute != -1)
                    {
                        calcPos = NormalRoute;
                        tempStepleft--;
                    }
                    else if (NormalRoute == -1)
                    {
                        calcPos++;
                        tempStepleft--;
                    }
                    checkDoMiniGame(horse, calcPos, true);
                }
            }
            horse._prevPosition = horse._nowPosition;
            horse._nowPosition = calcPos;
            if(horse._nowPosition >= 31)
            {
                horse._nowPosition = 31;
                horse._isgoal = true;
            }
            checkDoMiniGame(horse, calcPos, false);
        }

        void MoveBindYut(YutHorse horse, int stepsLeft)
        {
            if (!horse._isbind) return;
            foreach(YutHorse bindhorse in horse.bindhorseList)
            {
                YutMove(bindhorse, stepsLeft);
            }

        }

        void CalcHorseDestination()
        {
            clearYutDest();

            foreach (YutHorse horse in _playerArray[_nowTurn]._horses)
            {
                foreach (int stepsLeft in steps)
                {
                    int tempStepleft = stepsLeft;
                    int calcPos = horse._nowPosition;

                    if (tempStepleft == -1)
                    {
                        calcPos = YutGameUtil.Instance.BackdoRoute(horse);
                    }
                    else
                    {
                        while (tempStepleft > 0)
                        {

                            int NormalRoute = YutGameUtil.Instance.NormalRoute(horse._nowPosition, calcPos);
                            if (NormalRoute != -1)
                            {
                                calcPos = NormalRoute;
                                tempStepleft--;
                            }
                            else if (NormalRoute == -1)
                            {
                                calcPos++;
                                tempStepleft--;
                            }
                        }
                    }
                    horse._destPosition.Add(calcPos);
                }

                Console.WriteLine("dest : ");
                for (int i = 0; i < steps.Count; i++)
                {
                    Console.WriteLine("{0} : ", steps[i]);
                    Console.WriteLine(horse._destPosition[i]);
                }

            }
        }

        void HorseBind(YutHorse horse)
        {
            if (horse._nowPosition == 0) return;
            for (int i = 0; i < _numofHorse; i++) {
                if (horse != _playerArray[_nowTurn]._horses[i])
                {
                    if (horse._nowPosition == _playerArray[_nowTurn]._horses[i]._nowPosition)
                    {
                        if (horse.bindhorseList.Contains(_playerArray[_nowTurn]._horses[i]))
                        {
                            Console.WriteLine("already exist");
                            return; // 이미 리스트에 있는 경우, return
                        }

                        horse._isbind = true;
                        horse.bindhorseList.Add(_playerArray[_nowTurn]._horses[i]);
                        _playerArray[_nowTurn]._horses[i]._isbind = true;
                        _playerArray[_nowTurn]._horses[i].bindhorseList.Add(horse);
                    }
                }
            }
        }

        void checkDoMiniGame(YutHorse movinghorse, int calcposition, bool ismoving)
        {
            for (int i = 0; i < _numOfPlayer; i++)
            {
                if (i != _nowTurn)
                {
                    for (int j = 0; j < _numofHorse; j++)
                    {
                        if (_playerArray[i]._horses[j]._isgoal) continue; // Skip to the next j if _isgoal is true
                        if (calcposition == _playerArray[i]._horses[j]._nowPosition)
                        {
                            movinghorse.fighthorse = _playerArray[i]._horses[j];
                            movinghorse._fightPosition = calcposition;

                            if (ismoving)
                            {
                                movinghorse._doDefenceGame = true;
                                movinghorse._fightPosition = calcposition;
                                movinghorse.fighthorse = _playerArray[i]._horses[j];
                            }
                            else
                            {
                                movinghorse._doDefenceGame = false;
                                movinghorse._doHammerGame = true;
                                movinghorse._fightPosition = calcposition;
                                movinghorse.fighthorse = _playerArray[i]._horses[j];
                            }
                        }
                    }
                }
            }
        }

        void HammerGameEnd(Player winplayer, YutHorse horse)
        {
            Console.WriteLine("Yut Catched");
            if (winplayer == _playerArray[_nowTurn])
            {
                int pluschance = 1;
                if (horse.fighthorse == null)
                {
                    Console.WriteLine("there is no fight horse");
                    return;
                }
                if (horse.fighthorse._isbind)
                {
                    pluschance = horse.fighthorse.bindhorseList.Count + 1;
                    foreach (YutHorse losehorse in horse.fighthorse.bindhorseList)
                    {
                        YutGotoStart(losehorse);
                    }
                }
                YutGotoStart(horse.fighthorse);
                _yutChance += pluschance;
            }
            else
            {
                if (horse._isbind)
                {
                    foreach (YutHorse losehorse in horse.bindhorseList)
                    {
                        YutGotoStart(losehorse);
                    }
                }
                YutGotoStart(horse);
            }
            horse._doHammerGame = false;
        }

        void YutGotoStart(YutHorse horse)
        {
            horse._nowPosition = 0;
            horse._destPosition.Clear();
            horse._prevPosition = 0;
            horse._isgoal = false;
            horse._isbind = false;

            horse._doHammerGame = false;
            horse._doDefenceGame = false;
            horse._fightPosition = 0;
            horse.fighthorse = null;

            horse.bindhorseList.Clear();
        }
        
        public void StartMiniGame2()
        {
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
            MyPlayer.Info.PosInfo.PosY = 51;
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

            for(int i = 0; i < _numOfPlayer; i++)
            {
                if (_playerArray[i] == player)
                {
                    _wingamePlayer = i + 1;
                    if(_wingamePlayer <= 2)
                    {
                        _wingamePlayer = 0;
                    }
                }
            }

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
