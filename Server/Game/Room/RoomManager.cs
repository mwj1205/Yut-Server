using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Server.Game
{
    public class RoomManager
    {
        public static RoomManager Instance { get; } = new RoomManager();

        object _lock = new object();
        Dictionary<int, GameRoom> _rooms = new Dictionary<int, GameRoom>();
        int _roomId = 1;

        public GameRoom Add()
        {
            GameRoom gameRoom = new GameRoom();
            gameRoom.Push(gameRoom.Init);

            lock (_lock)
            {
                gameRoom.RoomId = _roomId;
                _rooms.Add(_roomId, gameRoom);
                _roomId++;
            }

            return gameRoom;
        }

        public bool Remove(int roomId)
        {
            lock (_lock)
            {
                return _rooms.Remove(roomId);
            }
        }

        public GameRoom Find(int roomId)
        {
            lock (_lock)
            {
                GameRoom room = null;
                if (_rooms.TryGetValue(roomId, out room))
                    return room;

                return null;
            }
        }

        public void SendRoomList(Player player)
        {
            S_RoomList roomlistPacket = new S_RoomList();
            RoomInfo roominfo = new RoomInfo();

            lock (_lock)
            {
                foreach (var kvp in _rooms)
                {
                    GameRoom gameroom = kvp.Value;
                    roominfo.RoomId = kvp.Key;
                    roominfo.Roomname = gameroom.RoomName;

                    Console.WriteLine(roominfo.Roomname + "  " + roominfo.RoomId);

                    roomlistPacket.RoomInfos.Add(roominfo);
                }
            }

            Console.WriteLine("send room list");

            player.Session.Send(roomlistPacket);
        }
    }
}
