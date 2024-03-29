﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ServerCore;
using System.Net;
using Google.Protobuf.Protocol;
using Google.Protobuf;
using Server.Game;
using System.Text.Json.Serialization;

namespace Server
{
    public class ClientSession : PacketSession
	{
		public Player MyPlayer { get; set; }
		public int SessionId { get; set; }

		public void Send(IMessage packet)
		{
			string msgName = packet.Descriptor.Name.Replace("_", string.Empty);
			MsgId msgId = (MsgId)Enum.Parse(typeof(MsgId), msgName);
			ushort size = (ushort)packet.CalculateSize();
			byte[] sendBuffer = new byte[size + 4];
			Array.Copy(BitConverter.GetBytes((ushort)(size + 4)), 0, sendBuffer, 0, sizeof(ushort));
			Array.Copy(BitConverter.GetBytes((ushort)msgId), 0, sendBuffer, 2, sizeof(ushort));
			Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size);
			Send(new ArraySegment<byte>(sendBuffer));
		}

		public override void OnConnected(EndPoint endPoint)
		{
			Console.WriteLine($"OnConnected : {endPoint}");

			MyPlayer = ObjectManager.Instance.Add<Player>();
			{
				MyPlayer.Info.Name = $"Player_{MyPlayer.Info.ObjectId}";
				MyPlayer.Info.PosInfo.PosX = 0 ;
				MyPlayer.Info.PosInfo.PosY = 51 ;
				MyPlayer.Info.PosInfo.PosZ = 0 ;
				MyPlayer.Info.RotInfo.RotX = 0 ;
				MyPlayer.Info.RotInfo.RotY = 0 ;
				MyPlayer.Info.RotInfo.RotZ = 0 ;
				MyPlayer.Info.RotInfo.RotW = 0 ;
				MyPlayer.Info.StatInfo.Hp = 5;
				MyPlayer.Info.StatInfo.MaxHp = 5;
				MyPlayer.Info.StatInfo.Speed = 10;
				MyPlayer.Info.StatInfo.RunSpeed = 20;
				MyPlayer.Info.StatInfo.JumpForce = 10;
				
				MyPlayer.Session = this;
			}

            //GameRoom room = RoomManager.Instance.Find(1);
            //room.Push(room.EnterGame, MyPlayer);
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            PacketManager.Instance.OnRecvPacket(this, buffer);
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
			if (MyPlayer.Room != null)
			{
				GameRoom room = RoomManager.Instance.Find(MyPlayer.Room.RoomId);
				if (room != null)
				{
					room.Push(room.LeaveGame, MyPlayer.Info.ObjectId);
					RoomManager.Instance.Remove(room.RoomId);
				}
			}
			
            SessionManager.Instance.Remove(this);

            Console.WriteLine($"OnDisconnected : {endPoint}");
        }

        public override void OnSend(int numOfBytes)
        {
            //Console.WriteLine($"Transferred bytes: {numOfBytes}");
        }
    }
}
