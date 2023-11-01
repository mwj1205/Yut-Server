using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections.Generic;

class PacketManager
{
	#region Singleton
	static PacketManager _instance = new PacketManager();
	public static PacketManager Instance { get { return _instance; } }
	#endregion

	PacketManager()
	{
		Register();
	}

	Dictionary<ushort, Action<PacketSession, ArraySegment<byte>, ushort>> _onRecv = new Dictionary<ushort, Action<PacketSession, ArraySegment<byte>, ushort>>();
	Dictionary<ushort, Action<PacketSession, IMessage>> _handler = new Dictionary<ushort, Action<PacketSession, IMessage>>();
		
	public Action<PacketSession, IMessage, ushort> CustomHandler { get; set; }

	public void Register()
	{		
		_onRecv.Add((ushort)MsgId.CMakeRoom, MakePacket<C_MakeRoom>);
		_handler.Add((ushort)MsgId.CMakeRoom, PacketHandler.C_MakeRoomHandler);		
		_onRecv.Add((ushort)MsgId.CRoomList, MakePacket<C_RoomList>);
		_handler.Add((ushort)MsgId.CRoomList, PacketHandler.C_RoomListHandler);		
		_onRecv.Add((ushort)MsgId.CEnterRoom, MakePacket<C_EnterRoom>);
		_handler.Add((ushort)MsgId.CEnterRoom, PacketHandler.C_EnterRoomHandler);		
		_onRecv.Add((ushort)MsgId.CStartGame, MakePacket<C_StartGame>);
		_handler.Add((ushort)MsgId.CStartGame, PacketHandler.C_StartGameHandler);		
		_onRecv.Add((ushort)MsgId.CThrowYut, MakePacket<C_ThrowYut>);
		_handler.Add((ushort)MsgId.CThrowYut, PacketHandler.C_ThrowYutHandler);		
		_onRecv.Add((ushort)MsgId.CYutMove, MakePacket<C_YutMove>);
		_handler.Add((ushort)MsgId.CYutMove, PacketHandler.C_YutMoveHandler);		
		_onRecv.Add((ushort)MsgId.CMove, MakePacket<C_Move>);
		_handler.Add((ushort)MsgId.CMove, PacketHandler.C_MoveHandler);		
		_onRecv.Add((ushort)MsgId.CRotation, MakePacket<C_Rotation>);
		_handler.Add((ushort)MsgId.CRotation, PacketHandler.C_RotationHandler);		
		_onRecv.Add((ushort)MsgId.CDoAttack, MakePacket<C_DoAttack>);
		_handler.Add((ushort)MsgId.CDoAttack, PacketHandler.C_DoAttackHandler);		
		_onRecv.Add((ushort)MsgId.CUpdateRound, MakePacket<C_UpdateRound>);
		_handler.Add((ushort)MsgId.CUpdateRound, PacketHandler.C_UpdateRoundHandler);		
		_onRecv.Add((ushort)MsgId.CSelectWall, MakePacket<C_SelectWall>);
		_handler.Add((ushort)MsgId.CSelectWall, PacketHandler.C_SelectWallHandler);		
		_onRecv.Add((ushort)MsgId.CAttackWall, MakePacket<C_AttackWall>);
		_handler.Add((ushort)MsgId.CAttackWall, PacketHandler.C_AttackWallHandler);		
		_onRecv.Add((ushort)MsgId.CDefMove, MakePacket<C_DefMove>);
		_handler.Add((ushort)MsgId.CDefMove, PacketHandler.C_DefMoveHandler);		
		_onRecv.Add((ushort)MsgId.CPlayerCollision, MakePacket<C_PlayerCollision>);
		_handler.Add((ushort)MsgId.CPlayerCollision, PacketHandler.C_PlayerCollisionHandler);		
		_onRecv.Add((ushort)MsgId.CDefgameWin, MakePacket<C_DefgameWin>);
		_handler.Add((ushort)MsgId.CDefgameWin, PacketHandler.C_DefgameWinHandler);		
		_onRecv.Add((ushort)MsgId.CGameReady, MakePacket<C_GameReady>);
		_handler.Add((ushort)MsgId.CGameReady, PacketHandler.C_GameReadyHandler);		
		_onRecv.Add((ushort)MsgId.CGameEndReady, MakePacket<C_GameEndReady>);
		_handler.Add((ushort)MsgId.CGameEndReady, PacketHandler.C_GameEndReadyHandler);
	}

	public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer)
	{
		ushort count = 0;

		ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
		count += 2;
		ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
		count += 2;

		Action<PacketSession, ArraySegment<byte>, ushort> action = null;
		if (_onRecv.TryGetValue(id, out action))
			action.Invoke(session, buffer, id);
	}

	void MakePacket<T>(PacketSession session, ArraySegment<byte> buffer, ushort id) where T : IMessage, new()
	{
		T pkt = new T();
		pkt.MergeFrom(buffer.Array, buffer.Offset + 4, buffer.Count - 4);

		if (CustomHandler != null)
		{
			CustomHandler.Invoke(session, pkt, id);
		}
		else
		{
			Action<PacketSession, IMessage> action = null;
			if (_handler.TryGetValue(id, out action))
				action.Invoke(session, pkt);
		}
	}

	public Action<PacketSession, IMessage> GetPacketHandler(ushort id)
	{
		Action<PacketSession, IMessage> action = null;
		if (_handler.TryGetValue(id, out action))
			return action;
		return null;
	}
}