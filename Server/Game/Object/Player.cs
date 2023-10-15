using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public class Player : GameObject
    {
        public ClientSession Session { get; set; }
        public List<YutHorse> _horses;

        public Player()
        {
            ObjectType = GameObjectType.Player;
            _horses = new List<YutHorse>();
            for (int i = 0; i < 4; i++)
            {
                _horses.Add(new YutHorse());
            }
        }
    }
}