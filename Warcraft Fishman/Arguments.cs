using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace Fishman
{
    /// <summary>
    /// Usage:
    /// [--dump] - Dump cursor.
    /// [-p .\Main-Retail.json] - Retail.
    /// [-p .\Main-Wrath.json] - Wrath.
    /// [-c -p .\Main-Classic.json] - Classic.
    /// </summary>
    class Arguments
    {
        [Option('s', "save", HelpText = "Saves default preset into file.")]
        public bool Save { get; set; }

        [Option('p', "preset", HelpText = "Path to selected preset. Example: margoss.json")]
        public string Preset { get; set; }

        [Option('d', "dump", HelpText = "Run bot in dump mode. Use it without other options.")]
        public bool Dump { get; set; }

        [Option('c', "classic", HelpText = "Use WoW Classic version of bot.")]
        public bool Classic { get; set; }
    }
}
