﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine
{
    public interface IConsoleService
    {
        public CursorPosition GetCursorPosition();
    }
}
