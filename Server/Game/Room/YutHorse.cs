using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game
{
    public class YutHorse
    {
        public int _nowPosition;
        public List<int> _destPosition;
        public int _prevPosition;

        public bool _doHammerGame;
        public bool _doDefenceGame;
        public int _fightPosition;
        public YutHorse? fighthorse;

        public bool _isgoal;
        public bool _isbind;
        public List<YutHorse> bindhorseList;

        public YutHorse()
        {
            _nowPosition = 0;
            _destPosition = new List<int>();
            _prevPosition = 0;
            _isgoal = false;
            _isbind = false;

            _doHammerGame = false;
            _doDefenceGame = false;
            _fightPosition = 0;
            fighthorse = null;

            bindhorseList = new List<YutHorse>();
        }
    }
}
