namespace ChessChallenge.Application
{
    public static class AutoTuner
    {
        
        public static void Run() {
            int[] eg_pawn_table = 
            {
                  0,   0,   0,   0,   0,   0,   0,   0,
                 13,   8,   8,  10,  13,   0,   2,  -7,
                  4,   7,  -6,   1,   0,  -5,  -1,  -8,
                 13,   9,  -3,  -7,  -7,  -8,   3,  -1,
                 32,  24,  13,   5,  -2,   4,  17,  17,
                 94, 100,  85,  67,  56,  53,  82,  84,
                178, 173, 158, 134, 147, 132, 165, 187,
                  0,   0,   0,   0,   0,   0,   0,   0
            };

            ulong [] Scoreboard = convertPSTtoScoreboard(eg_pawn_table);
            LogArray(Scoreboard);
        }

        public static void LogArray(ulong[] ulongs) {
            ConsoleHelper.Log("{",false,System.ConsoleColor.Green);
            foreach (ulong value in ulongs) {
                ConsoleHelper.Log("\t0x" + value.ToString("X16")+",",false,System.ConsoleColor.Green);
            }
            ConsoleHelper.Log("}",false,System.ConsoleColor.Green);
        }
        
        public static ulong[] convertPSTtoScoreboard(int[] PST)
        {
            ulong[] result = { 0, 0, 0, 0, 0, 0, 0, 0 };

            for (int index = 0; index < 64; index++)
            {
                PST[index] += 128;
                for (int scoreboardIndex = 0; scoreboardIndex < 8; scoreboardIndex++)
                {
                    if ((PST[index] & 1) == 1)
                    {
                        result[scoreboardIndex] |= (ulong)1 << index;
                    }

                    PST[index] >>= 1;
                }
            }

            return result;
        }


    }
}
