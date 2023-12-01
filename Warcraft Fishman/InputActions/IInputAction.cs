using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishman.InputActions
{
    interface IInputAction
    {
        /// <summary>
        /// Value of specific InputAction: input delay, key or mouse position. Check implementations for more.
        /// </summary>
        object Value { get; set; }
        void Invoke();
    }
}
