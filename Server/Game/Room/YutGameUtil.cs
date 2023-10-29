using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game
{
    public class YutGameUtil
    {
        public static YutGameUtil Instance { get; } = new YutGameUtil();

        public const int FIRST_CORNER = 5;
        public const int SECOND_CORNER = 10;
        public const int THIRD_CORNER = 15;
        public const int CENTER = 22;
        public const int GOAL = 30;
        public const int FIRST_FORK = 20;
        public const int SECOND_FORK = 25;
        public const int CENTER_STRAIGHT = 24;
        public const int CENTER_FORK = 28;

        public struct Vector3Int
        {
            public int x;
            public int y;
            public int z;

            public Vector3Int(int x, int y, int z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        }

        static Random random = new Random();

        public YutResult GetYutResult()
        {
            int randomNumber = random.Next(1, 101); // 1부터 100 사이의 랜덤 숫자 생성

            if (randomNumber <= 3) // 확률 3%
                return YutResult.Nak;
            else if (randomNumber <= 7) // 확률 4%
                return YutResult.Backdo;
            else if (randomNumber <= 25) // 확률 18%
                return YutResult.Do;
            else if (randomNumber <= 55) // 확률 30%
                return YutResult.Gae;
            else if (randomNumber <= 83) // 확률 28%
                return YutResult.Geol;
            else if (randomNumber <= 96) // 확률 13%
                return YutResult.Yut;
            else // 확률 4%
                return YutResult.Mo;
        }

        public int convertYutResult(YutResult result)
        {
            switch (result)
            {
                case YutResult.Backdo:
                    return -1;
                case YutResult.Nak:
                    return 0;
                case YutResult.Do:
                    return 1;
                case YutResult.Gae:
                    return 2;
                case YutResult.Geol:
                    return 3;
                case YutResult.Yut:
                    return 4;
                case YutResult.Mo:
                    return 5;
                default:
                    Console.WriteLine("error");
                    return 5;
            }
        }

        public int BackdoRoute(YutHorse horse)
        {
            int position = horse._nowPosition;
            int lastPosition = horse._prevPosition;
            int result = -1;

            switch (position)
            {
                case 0:
                    result = 0;
                    break;
                case FIRST_FORK:
                    result = FIRST_CORNER;
                    break;
                case SECOND_FORK:
                    result = SECOND_CORNER;
                    break;
                case THIRD_CORNER:
                    if (lastPosition == THIRD_CORNER) { result = CENTER_STRAIGHT; }
                    break;
                case 1:
                    result = GOAL;
                    break;
                default:
                    result = position - 1;
                    break;
            }
            return result;
        }

        public int NormalRoute(int position, int lastPosition)
        {
            int result = -1;
            switch (position)
            {
                case FIRST_CORNER:
                    if (lastPosition == FIRST_CORNER)
                    {
                        result = FIRST_FORK;
                    }
                    break;

                case SECOND_CORNER:
                    if (lastPosition == SECOND_CORNER)
                    {
                        result = SECOND_FORK;
                    }
                    break;

                case CENTER:
                    if (lastPosition == CENTER)
                    {
                        result = CENTER_FORK;
                    }
                    break;

                case 24:
                    result = THIRD_CORNER;
                    break;

                case 29:
                case 19:
                    result = GOAL;
                    break;
            }
            return result;
        }
    }
}
