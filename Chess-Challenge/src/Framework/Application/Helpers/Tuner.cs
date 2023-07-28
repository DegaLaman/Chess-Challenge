namespace ChessChallenge.Application
{
    public static class AutoTuner
    {
        
        public static void Run() {
            int[] table = 
            {
    -74, -35, -18, -18, -11,  15,   4, -17,
    -12,  17,  14,  17,  17,  38,  23,  11,
     10,  17,  23,  15,  20,  45,  44,  13,
     -8,  22,  24,  27,  26,  33,  26,   3,
    -18,  -4,  21,  24,  27,  23,   9, -11,
    -19,  -3,  11,  21,  23,  16,   7,  -9,
    -27, -11,   4,  13,  14,   4,  -5, -17,
    -53, -34, -21, -11, -28, -14, -24, -43
};



            ulong [] Scoreboard = convertPSTtoScoreboard(table);
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

                // Clamp
                PST[index] = (PST[index] > 255) ? 255 : PST[index];
                PST[index] = (PST[index] < 0) ? 0 : PST[index];
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
