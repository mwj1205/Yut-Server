using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using Server.Game;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Text;

class PacketHandler
{
    public static void C_MakeRoomHandler(PacketSession session, IMessage packet)
    {
        C_MakeRoom makeroomPacket = packet as C_MakeRoom;
        ClientSession clientSession = session as ClientSession;

        if (clientSession.MyPlayer == null)
            return;

        // 게임 방 생성
        GameRoom room = RoomManager.Instance.Add();
        Program.TickRoom(room, 50);

        room.RoomName = makeroomPacket.RoomName;

        room.Push(room.EnterGame, clientSession.MyPlayer);
    }

    public static void C_EnterRoomHandler(PacketSession session, IMessage packet)
    {
        C_EnterRoom enterroomPacket = packet as C_EnterRoom;
        ClientSession clientSession = session as ClientSession;

        if(clientSession.MyPlayer == null) 
            return;

        // 플레이어가 선택한 룸에 입장
        GameRoom room = RoomManager.Instance.Find(enterroomPacket.RoomId);
        room.Push(room.EnterGame, clientSession.MyPlayer);
    }

    public static void C_MoveHandler(PacketSession session, IMessage packet)
    {
        C_Move movePacket = packet as C_Move;
        ClientSession clientSession = session as ClientSession;

        if (clientSession.MyPlayer == null)
            return;
        if (clientSession.MyPlayer.Room == null)
            return;

        // TODO : 검증

        // 서버에서 좌표 이동
        ObjectInfo info = clientSession.MyPlayer.Info;
        info.PosInfo = movePacket.PosInfo;

        // 다른 플레이어한테도 알려준다
        S_Move resMovePacket = new S_Move();
        resMovePacket.ObjectId = clientSession.MyPlayer.Info.ObjectId;
        resMovePacket.PosInfo = movePacket.PosInfo;

        Console.WriteLine(resMovePacket.ObjectId);
        Console.WriteLine(resMovePacket.PosInfo);
        clientSession.MyPlayer.Room.Broadcast(resMovePacket);
    }

    public static void C_RotationHandler(PacketSession session, IMessage packet)
    {
        C_Rotation rotationPacket = packet as C_Rotation;
        ClientSession clientSession = session as ClientSession;

        if (clientSession.MyPlayer == null)
            return;
        if (clientSession.MyPlayer.Room == null)
            return;

        // TODO : 검증

        // 일단 서버에서 좌표 이동
        ObjectInfo info = clientSession.MyPlayer.Info;
        info.RotInfo = rotationPacket.RotInfo;

        // 다른 플레이어한테도 알려준다
        S_Rotation resRotationPacket = new S_Rotation();
        resRotationPacket.ObjectId = clientSession.MyPlayer.Info.ObjectId;
        resRotationPacket.RotInfo = rotationPacket.RotInfo;

        clientSession.MyPlayer.Room.Broadcast(resRotationPacket);
    }
}
