using System;
using System.Collections.Generic;

namespace FrogSea
{
    public enum GameMode { Stages, Endless }
    public readonly struct Ledge
    {
        public readonly float X, Y, Width;
        public readonly bool Sticky;
        public Ledge(float x, float y, float width, bool sticky = false)
        { X = x; Y = y; Width = width; Sticky = sticky; }
    }
    // Eight interleaved lanes cover the whole circumference; every row branches left and right.
    public sealed class StageModel
    {
        public readonly List<Ledge> Ledges = new List<Ledge>();
        public readonly GameMode Mode;
        public readonly int Stage;
        public readonly float GoalHeight;
        int generatedRows;
        float generatedHeight;
        public StageModel(GameMode mode, int stage, float circumference)
        {
            Mode = mode; Stage = stage;
            GoalHeight = mode == GameMode.Endless ? float.PositiveInfinity : 30 + stage * 12;
            Ledges.Add(new Ledge(0, 0, circumference));
            ExtendTo(mode == GameMode.Endless ? 48 : GoalHeight, circumference);
        }
        public StageModel(int stage, float circumference, float goalHeight, IEnumerable<Ledge> authored)
        {
            Mode = GameMode.Stages; Stage = stage; GoalHeight = goalHeight;
            Ledges.Add(new Ledge(0, 0, circumference)); Ledges.AddRange(authored);
            Ledges.Sort((a, b) => a.Y.CompareTo(b.Y));
        }
        public void ExtendTo(float height, float circumference)
        {
            while (generatedHeight < height)
            {
                if (generatedHeight >= GoalHeight) break;
                generatedRows++;
                generatedHeight = Math.Min(generatedRows * (1.65f + Stage * 0.15f), GoalHeight);
                float spacing = circumference / 8;
                float offset = generatedRows % 2 * spacing / 2;
                float width = 2.85f - Stage * 0.15f;
                for (int lane = 0; lane < 8; lane++)
                {
                    float x = -circumference / 2 + lane * spacing + offset;
                    bool sticky = generatedHeight < GoalHeight && (lane + generatedRows) % 5 == 0;
                    Ledges.Add(new Ledge(x, generatedHeight, width, sticky));
                }
            }
        }
        public static float WrappedDelta(float x, float center, float circumference)
        {
            float delta = (x - center) % circumference;
            if (delta > circumference / 2) delta -= circumference;
            if (delta < -circumference / 2) delta += circumference;
            return delta;
        }
    }
}
