﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Game;
using ServerCore;


namespace Server
{
    class Program
	{
        static Listener _listener = new Listener();
        static Dictionary<int, System.Timers.Timer> _timers = new Dictionary<int, System.Timers.Timer>();

        public static void TickRoom(GameRoom room, int tick = 100)
        {
            var timer = new System.Timers.Timer();
            timer.Interval = tick;
            timer.Elapsed += ((s, e) => { room.Update(); });
            timer.AutoReset = true;
            timer.Enabled = true;

            _timers.Add(room.RoomId, timer);
        }

        public static void RemoveTickRoom(int roomid)
        {
            if (_timers.ContainsKey(roomid))
            {
                var timer = _timers[roomid];
                timer.Stop();
                timer.Dispose();
                _timers.Remove(roomid);
            }
        }

        static void Main(string[] args)
        {
            // DNS (Domain Name System)
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            //IPAddress ipAddr = ipHost.AddressList[0];
            IPAddress ipAddr = IPAddress.Parse("172.30.1.47");
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

            _listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
            Console.WriteLine("Listening...");

            //FlushRoom();
            //JobTimer.Instance.Push(FlushRoom);

            // TODO
            while (true)
            {
                //JobTimer.Instance.Flush();
                Thread.Sleep(100);
            }
        }
	}
}
