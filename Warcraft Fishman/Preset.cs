using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishman
{
    class Preset
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static Preset Default = new Preset("Default");

        public string Name;
        public List<Action> Actions = new List<Action>();

        static Preset()
        {
            Default.Actions.Add(Action.Fish);
            Default.Actions.Add(new Action() { Key = WinApi.VirtualKeys.N2, CastTime = 2000, Description = "Oversized Bobber", Trigger = Action.Event.Interval, Interval = 30 * 60 });
        }

        /// <summary>
        /// Preset of actions for fishing
        /// </summary>
        /// <param name="Name">Name of preset. Used to save preset as file</param>
        public Preset(string Name)
        {
            this.Name = Name;
        }

        public bool Validate()
        {
            if (GetActions(Action.Event.None).Length > 0)
            {
                logger.Warn("Action list contains None trigger types");
                return false;
            }
            if (GetActions(Action.Event.Fish).Length > 1)
            {
                logger.Error("More than one fishing actions found!");
                return false;
            }
            
            // TODO: Key collision detection

            return true;
        }

        /// <summary>
        /// Returns actions that proper selected event type
        /// </summary>
        /// <param name="triggetType">Type of actions that will be returned</param>
        /// <returns>Array of objects that has declared event type</returns>
        public Action[] GetActions(Action.Event triggetType)
        {
            return Actions.Where(x => x.Trigger == triggetType).ToArray();
        }

        /// <summary>
        /// Saves current preset to json file with <see cref="Name"/>
        /// </summary>
        public void Save()
        {
            using (StreamWriter writer = new StreamWriter(Name + ".json"))
                writer.WriteLine(ToJson());
        }

        /// <summary>
        /// Loads preset from json file
        /// </summary>
        /// <param name="path">Path to json file with preset</param>
        /// <returns>Loaded preset</returns>
        public static Preset Load(string path)
        {
            using (StreamReader reader = new StreamReader(path))
                return JsonConvert.DeserializeObject<Preset>(reader.ReadToEnd());
        }

        public override string ToString()
        {
            string preset = string.Format("Preset name: \"{0}\"", Name) + Environment.NewLine;
            foreach (var action in Actions)
                preset += "\t" + action.ToString() + Environment.NewLine;
            return preset.TrimEnd();
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
