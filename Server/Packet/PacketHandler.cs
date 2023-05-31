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
    public static void C_MoveHandler(PacketSession session, IMessage packet)
    {
        C_Move movePacket = packet as C_Move;
        ClientSession clientSession = session as ClientSession;

        if (clientSession.MyPlayer == null)
            return;
        if (clientSession.MyPlayer.Room == null)
            return;

        // TODO : 검증

        // 일단 서버에서 좌표 이동
        PlayerInfo info = clientSession.MyPlayer.Info;
        info.PosInfo = movePacket.PosInfo;

        // 다른 플레이어한테도 알려준다
        S_Move resMovePacket = new S_Move();
        resMovePacket.PlayerId = clientSession.MyPlayer.Info.PlayerId;
        resMovePacket.PosInfo = movePacket.PosInfo;

        Console.WriteLine(resMovePacket.PlayerId);
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
        PlayerInfo info = clientSession.MyPlayer.Info;
        info.RotInfo = rotationPacket.RotInfo;

        // 다른 플레이어한테도 알려준다
        S_Rotation resRotationPacket = new S_Rotation();
        resRotationPacket.PlayerId = clientSession.MyPlayer.Info.PlayerId;
        resRotationPacket.RotInfo = rotationPacket.RotInfo;

        clientSession.MyPlayer.Room.Broadcast(resRotationPacket);
    }
}
