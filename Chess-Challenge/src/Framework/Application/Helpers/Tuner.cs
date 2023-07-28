namespace ChessChallenge.Application
{
    public static class AutoTuner
    {
        
        public static void Run() {

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
