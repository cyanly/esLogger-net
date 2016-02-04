using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace esLogger.Utils
{
    /// <summary>
    /// Dummy escape sequence parser.
    /// Can only parse color codes. Anything other than color will probably will 
    /// Must be able to parse other stuff.
    /// 
    /// </summary>
    public class EscapeSequencer : TextWriter
    {
        private static EscapeSequencer Instance;

        private readonly TextWriter textWriter;
        private enum States
        {
            Text,
            Signaled,
            Started
        }

        private readonly ConsoleColor defaultForegroundColor;
        private readonly ConsoleColor defaultBackgroundColor;
        private EscapeSequencer(TextWriter textWriter)
        {
            Instance = this;
            this.textWriter = textWriter;
            defaultForegroundColor = Console.ForegroundColor;
            defaultBackgroundColor = Console.BackgroundColor;
        }

        private States state = States.Text;
        private string escapeBuffer;
        private byte intense;
        private const char ESC = '\x1b';

        public override void Write(char value)
        {
            switch (state)
            {
                case States.Text:
                    if (value == ESC)
                    {
                        state = States.Signaled;
                        escapeBuffer = "";
                    }
                    else
                        textWriter.Write(value);
                    break;
                case States.Signaled:
                    if (value != '[')
                    {
                        textWriter.Write(ESC);
                        textWriter.Write(value);
                        state = States.Text;
                    }
                    else
                    {
                        state = States.Started;
                    }
                    break;
                case States.Started:
                    if (value != 'm')
                        escapeBuffer += value;
                    else
                    {
                        byte val;
                        if (byte.TryParse(escapeBuffer, out val))
                        {
                            if (val >= 30 && val <= 37)
                                SetForeColor(val);
                            else if (val == 39)
                                SetDefaultForeColor();
                            else if (val == 1)
                                SetBold();
                            else if (val == 22)
                                RemoveBold();
                            else if (val == 7 || val == 27)
                                SetInverse();
                            else if (val >= 40 && val <= 47)
                                SetBackColor(val);
                            else if (val == 49)
                                SetDefaultBackColor();
                        }
                        state = States.Text;
                    }
                    break;
            }

        }

        private bool isInverted;
        private void SetInverse()
        {
            var c = Console.ForegroundColor;
            Console.ForegroundColor = Console.BackgroundColor;
            Console.BackgroundColor = c;
            isInverted = !isInverted;
        }

        private void RemoveBold()
        {
            intense--;
        }

        private void SetBold()
        {
            intense++;
        }

        public static bool Bold
        {
            get
            {
                return (Instance.intense > 0);
            }
            set
            {
                if (value)
                    Instance.SetBold();
                else
                    Instance.RemoveBold();
            }
        }

        private void SetDefaultBackColor()
        {
            if (isInverted)
                Console.BackgroundColor = defaultBackgroundColor;
            else
                Console.ForegroundColor = defaultBackgroundColor;
        }

        private void SetDefaultForeColor()
        {
            if (isInverted)
                Console.BackgroundColor = defaultForegroundColor;
            else
                Console.ForegroundColor = defaultForegroundColor;
        }

        private readonly ConsoleColor[] ColorMap = {
            ConsoleColor.Black,
            ConsoleColor.DarkRed, 
            ConsoleColor.DarkGreen, 
            ConsoleColor.DarkYellow, 
            ConsoleColor.DarkBlue, 
            ConsoleColor.DarkMagenta, 
            ConsoleColor.DarkCyan, 
            ConsoleColor.Gray,

            ConsoleColor.Black,
            ConsoleColor.Red,
            ConsoleColor.Green,
            ConsoleColor.Yellow,
            ConsoleColor.Blue,
            ConsoleColor.Magenta,
            ConsoleColor.Cyan,
            ConsoleColor.White
        };

        private void SetBackColor(byte val)
        {
            if (isInverted)
                Console.ForegroundColor = ColorMap[val - 40 + (intense > 0 ? 8 : 0)];
            else
                Console.BackgroundColor = ColorMap[val - 40];
        }

        private void SetForeColor(byte val)
        {
            if (isInverted)
                Console.BackgroundColor = ColorMap[val - 30];
            else
                Console.ForegroundColor = ColorMap[val - 30 + (intense > 0 ? 8 : 0)];
        }

        public static void Install()
        {
            Console.SetOut(new EscapeSequencer(Console.Out));
        }

        public override Encoding Encoding
        {
            get { return textWriter.Encoding; }
        }
    }


    public static class ColorsExtension
    {
        public struct ColorWrap
        {
            public byte Start { get; private set; }
            public byte End { get; private set; }

            public ColorWrap(byte start, byte end)
                : this()
            {
                Start = start;
                End = end;
            }
        }

        public static readonly Dictionary<string, ColorWrap> DefaultTheme = new Dictionary<string, ColorWrap>
            {
                {"bold", new ColorWrap(1, 22)},
                {"italic", new ColorWrap(3, 23)},
                {"underline", new ColorWrap(4, 24)},
                {"inverse", new ColorWrap(7, 27)},
                
                {"reset", new ColorWrap(39, 49)},

                {"white", new ColorWrap(37, 39)},
                {"grey", new ColorWrap(90, 39)},
                {"black", new ColorWrap(30, 39)},

                {"blue", new ColorWrap(34, 39)},
                {"cyan", new ColorWrap(36, 39)},
                {"green", new ColorWrap(32, 39)},
                {"magenta", new ColorWrap(35, 39)},
                {"red", new ColorWrap(31, 39)},
                {"yellow", new ColorWrap(33, 39)}
            };

        private static Dictionary<string, ColorWrap> Theme = DefaultTheme;

        public static string Bold(this string s) { return Wrap(s, "bold"); }
        public static string Italic(this string s) { return Wrap(s, "italic"); }
        public static string Underline(this string s) { return Wrap(s, "underline"); }
        public static string Inverse(this string s) { return Wrap(s, "inverse"); }
        public static string White(this string s) { return Wrap(s, "white"); }
        public static string Grey(this string s) { return Wrap(s, "grey"); }
        public static string Black(this string s) { return Wrap(s, "black"); }
        public static string Blue(this string s) { return Wrap(s, "blue"); }
        public static string Cyan(this string s) { return Wrap(s, "cyan"); }
        public static string Green(this string s) { return Wrap(s, "green"); }
        public static string Magenta(this string s) { return Wrap(s, "magenta"); }
        public static string Red(this string s) { return Wrap(s, "red"); }
        public static string Yellow(this string s) { return Wrap(s, "yellow"); }

        public static string Reset(this string s) { return Wrap("", "reset") + s; }


        private static bool isBg = false;
        public static string On(this string s)
        {
            isBg = true;
            return s;
        }

        public static string Color(this string str, string color)
        {
            return Wrap(str, color);
        }

        private static string Wrap(string str, string color)
        {
            var w = Theme[color];

            int start = w.Start;
            int end = w.End;
            if (isBg && start >= 30 && start <= 37)
            {
                isBg = false;
                start += 10;
                end += 10;
            }

            isBg = false;
            return string.Format("\x1b[{0}m{1}\x1b[{2}m", start, str, end);
        }

        private static string Wrap(char c, string color)
        {
            return Wrap(c.ToString(), color);
        }

        public static string RunSequencer(string s, Func<string, int, char, string> sequencer)
        {
            if (null == s)
                return null;

            var sb = new StringBuilder();
            for (int n = 0; n < s.Length; n++)
            {
                sb.Append(sequencer(s, n, s[n]));
            }

            return sb.ToString();
        }

        public static string Zebra(this string s)
        {
            return RunSequencer(
                s,
                (str, i, c) => (0 == i % 2) ? Wrap(c, "inverse") : c.ToString()
                );
        }

        public static string Rainbow(this string s)
        {
            var rainbowColors = new[] { "red", "yellow", "green", "blue", "magenta" }; //RoY G BiV
            return RunSequencer(
                s,
                (str, i, c) => (char.IsWhiteSpace(c) ? c.ToString() : Wrap(c, rainbowColors[(i + 1) % rainbowColors.Length]))
                );
        }
    }


}
