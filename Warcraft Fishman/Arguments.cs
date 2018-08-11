using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace Fishman
{
    class Arguments
    {
        [Option('s', "save", HelpText = "Saves default preset into file")]
        public bool IsSave { get; set; }

        [Option('p', "preset", HelpText = "Path to selected preset. Example: margoss.json")]
        public string Preset { get; set; }

        [Option('o', "offset", HelpText = "Use few pixels offset to left. Useful with default bobber.")]
        public bool Offset { get; set; }

        [Option('d', "dump", HelpText = "Runs in dump mode. Use it alone.")]
        public bool IsDump { get; set; }
    }
}
