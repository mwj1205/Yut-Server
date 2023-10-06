using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using Server.Game;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

class PacketHandler
{
    public static void C_MakeRoomHandler(PacketSession session, IMessage packet)
    {
        C_MakeRoom makeroomPacket = packet as C_MakeRoom;
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        // 게임 방 생성
        GameRoom room = RoomManager.Instance.Add();
        Program.TickRoom(room, 50);
        room.RoomName = makeroomPacket.RoomName;

        Console.WriteLine("MakeRoom");

        room.Push(room.EnterGame, clientSession.MyPlayer);
    }

    public static void C_RoomListHandler(PacketSession session, IMessage packet)
    {
        C_RoomList roomlistPacket = packet as C_RoomList;
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        RoomManager.Instance.SendRoomList(clientSession.MyPlayer);
    }

    public static void C_EnterRoomHandler(PacketSession session, IMessage packet)
    {
        C_EnterRoom enterroomPacket = packet as C_EnterRoom;
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        Console.WriteLine("EnterRoom");

        // 플레이어가 선택한 룸에 입장
        GameRoom room = RoomManager.Instance.Find(enterroomPacket.RoomId);
        room.Push(room.EnterGame, clientSession.MyPlayer);
    }

    public static void C_StartGameHandler(PacketSession session, IMessage packet)
    {
        C_StartGame startgamePacket = packet as C_StartGame;
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        GameRoom room = player.Room;
        if (room == null)
            return;

        room.Push(room.HandleStartGame);
    }

    public static void C_ThrowYutHandler(PacketSession session, IMessage packet)
    {
        C_ThrowYut throwyutPacket = packet as C_ThrowYut;
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        GameRoom room = player.Room;
        if (room == null)
            return;

        room.Push(room.HandleThrowYut);
    }

    public static void C_YutMoveHandler(PacketSession session, IMessage packet)
    {

    }

    public static void C_MoveHandler(PacketSession session, IMessage packet)
    {
        C_Move movePacket = packet as C_Move;
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        GameRoom room = player.Room;
        if (room == null)
            return;

        room.Push(room.HandleMove, player, movePacket);
    }

    public static void C_RotationHandler(PacketSession session, IMessage packet)
    {
        C_Rotation rotationPacket = packet as C_Rotation;
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        GameRoom room = player.Room;
        if (room == null)
            return;

        room.Push(room.HandleRotation, player, rotationPacket);
    }

    public static void C_DoAttackHandler(PacketSession session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        GameRoom room = player.Room;
        if (room == null)
            return;

        room.Push(room.HandlePlayerAttack, player);
        Console.WriteLine("Attacked");
    }
}
