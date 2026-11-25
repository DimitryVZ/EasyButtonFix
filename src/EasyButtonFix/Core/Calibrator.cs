using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyButtonFix.Core
{
    /// <summary>
    /// Misst, wie stark eine Taste prellt: Der Nutzer drückt sie langsam N-mal.
    /// Alles, was sehr kurz nach dem Loslassen wieder "drückt", ist ein Fehlauslöser.
    /// </summary>
    public sealed class Calibrator
    {
        public const int TargetPresses = 10;
        const int BounceLimitMs = 150; // schneller drückt kein Mensch absichtlich, wenn er "langsam" drücken soll

        public ButtonState Button { get; }
        public int RealPresses { get; private set; }
        public List<int> Bounces { get; } = new List<int>();
        public bool Done => RealPresses >= TargetPresses;

        public Calibrator(ButtonState button) { Button = button; }

        public void Add(int gapMs)
        {
            if (Done) return;
            if (gapMs >= 0 && gapMs < BounceLimitMs) Bounces.Add(gapMs);
            else RealPresses++;
        }

        /// <summary>Empfohlene Sperrzeit in ms.</summary>
        public int Suggestion
        {
            get
            {
                if (Bounces.Count == 0) return 50; // leichter Grundschutz
                double s = Bounces.Max() * 1.5 + 15;
                s = Math.Ceiling(s / 10) * 10;
                return (int)Math.Max(40, Math.Min(ButtonState.MaxMs, s));
            }
        }

        public string ResultText => Bounces.Count == 0
            ? L.CalNoBounce
            : L.CalResult(Bounces.Count, RealPresses, Bounces.Min(), Bounces.Max(), Suggestion);
    }
}
