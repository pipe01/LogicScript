using System;
using System.IO;

namespace LogicScript.Utils
{
    internal sealed class CharReader : IDisposable
    {
        private readonly StreamReader Reader;

        public char Current { get; private set; }

        public bool IsEOF => Reader.EndOfStream;

        public CharReader(Stream stream)
        {
            this.Reader = new StreamReader(stream);

            TryAdvance();
        }

        public void Dispose()
        {
            this.Reader.Dispose();
        }

        public bool TryPeek(out char c)
        {
            int read = Reader.Peek();

            if (read == -1)
            {
                c = default;
                return false;
            }
            else
            {
                c = (char)read;
                return true;
            }
        }

        public bool TryAdvance()
        {
            int read = Reader.Read();

            if (read == -1)
            {
                return false;
            }
            else
            {
                Current = (char)read;
                return true;
            }
        }
    }
}
