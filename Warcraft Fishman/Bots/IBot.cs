using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishman
{
    abstract class IBot
    {
        protected abstract Preset Preset { get; set; }
        protected abstract Bitmap FishhookCursor { get; set; }

        /// <summary>
        /// Main window handle.
        /// </summary>
        protected abstract IntPtr Handle { get; set; }

        public IBot()
        {
            
        }

        /// <summary>
        /// Start fishing session.
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// Stop fishing session.
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// Contains the main fishing loop: processing preset actions.
        /// </summary>
        protected abstract void FishingLoop();

        /// <summary>
        /// A single fishing iteration. Performs the actions required to complete one fishing attempt.
        /// </summary>
        /// <param name="fishing">The fishing action, must be of type <see cref="Action.Event.Fish"/>.</param>
        /// <returns>True if fishing attempt was successful, false otherwise (an error occurred or a timeout occurred).</returns>
        protected abstract bool Fishing(Action fishing);

        /// <summary>
        /// Scans the screen looking for a bobber in a loop.
        /// Should only be used for event type <see cref="Action.Event.Fish"/>.
        /// </summary>
        /// <returns>True if the bobber was found, false otherwise.</returns>
        protected abstract bool FindBobber();

        /// <summary>
        /// Scans the size of a bobber using cursor movement.
        /// </summary>
        /// <returns>The region in which the float is located.</returns>
        protected abstract Rectangle GetBobberSize();

        /// <summary>
        /// The method that should be called after <see cref="FindBobber"/> (if it returned true).
        /// Waits for a bite in a loop. The wait lasts until a bite occurs or a timeout occurs.
        /// Should only be used for event type <see cref="Action.Event.Fish"/>.
        /// </summary>
        /// <param name="timeout">Timeout in milliseconds.</param>
        /// <returns>True if a bite was detected.</returns>
        protected abstract bool WaitForBite(int timeout);
    }
}
