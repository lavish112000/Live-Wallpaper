using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Common.Exceptions
{
    public class WallpaperFileException : Exception
    {
        public WallpaperFileException()
        {
        }

        public WallpaperFileException(string message)
            : base(message)
        {
        }

        public WallpaperFileException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
