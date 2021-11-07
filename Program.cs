using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        SimpleTimerSM timerSM;
        List<IMyBlockGroup> sabotGroups = new List<IMyBlockGroup>();
        List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
        List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
        public Program()
        {
            timerSM = new SimpleTimerSM(this, Sequence());

            Echo("Run with 'start' argument");
        }
        public void Main(string argument, UpdateType updateSource)
        {
            timerSM.Run(); // runs the next step in sequence if started, this can always stay here.

            if (argument == "start")
            {
                timerSM.Start();

                // required because timerSM.Run() needs to be called
                // can also set it to Update100 if the times between waits are long enough or you don't need 100% accuracy.
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
            }
        }
        IEnumerable<double> Sequence()
        {
            GridTerminalSystem.GetBlockGroups(groups);
            sabotGroups.Clear();

            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].Name.Contains("Sabot"))
                    sabotGroups.Add(groups[i]);
            }

            blocks.Clear();
            for (int J = 0; J < sabotGroups.Count; J++)
            {
                IMyBlockGroup sabot = sabotGroups[J];
                sabot.GetBlocks(blocks);
                foreach (var block in blocks)
                {
                    var warhead = block as IMyWarhead;
                    warhead?.StartCountdown();
                    var timer = block as IMyTimerBlock;
                    timer?.StartCountdown();
                    var functionblock = block as IMyFunctionalBlock;
                    if (functionblock != null)
                    {
                        functionblock.Enabled = !functionblock.Enabled;
                    }
                    //block.Enabled = !block.Enabled;                    
                }
                yield return 0.2;
            }
        }
        public class SimpleTimerSM
        {
            public readonly Program Program;

            /// <summary>
            /// Wether the timer starts automatically at initialization and auto-restarts it's done iterating.
            /// </summary>
            public bool AutoStart { get; set; }

            /// <summary>
            /// Returns true if a sequence is actively being cycled through.
            /// False if it ended or no sequence is assigned anymore.
            /// </summary>
            public bool Running { get; private set; }

            /// <summary>
            /// Setting this will change what sequence will be used when it's (re)started.
            /// </summary>
            public IEnumerable<double> Sequence;

            /// <summary>
            /// Time left until the next part is called.
            /// </summary>
            public double SequenceTimer { get; private set; }

            private IEnumerator<double> sequenceSM;

            public SimpleTimerSM(Program program, IEnumerable<double> sequence = null, bool autoStart = false)
            {
                Program = program;
                Sequence = sequence;
                AutoStart = autoStart;

                if (AutoStart)
                {
                    Start();
                }
            }

            /// <summary>
            /// (Re)Starts sequence, even if already running.
            /// Don't forget <see cref="IMyGridProgramRuntimeInfo.UpdateFrequency"/>.
            /// </summary>
            public void Start()
            {
                SetSequenceSM(Sequence);
            }

            /// <summary>
            /// <para>Call this in your <see cref="Program.Main(string, UpdateType)"/> and have a reasonable update frequency, usually Update10 is good for small delays, Update100 for 2s or more delays.</para>
            /// <para>Checks if enough time passed and executes the next chunk in the sequence.</para>
            /// <para>Does nothing if no sequence is assigned or it's ended.</para>
            /// </summary>
            public void Run()
            {
                if (sequenceSM == null)
                    return;

                SequenceTimer -= Program.Runtime.TimeSinceLastRun.TotalSeconds;

                if (SequenceTimer > 0)
                    return;

                bool hasValue = sequenceSM.MoveNext();

                if (hasValue)
                {
                    SequenceTimer = sequenceSM.Current;

                    if (SequenceTimer <= -0.5)
                        hasValue = false;
                }

                if (!hasValue)
                {
                    if (AutoStart)
                        SetSequenceSM(Sequence);
                    else
                        SetSequenceSM(null);
                }
            }

            private void SetSequenceSM(IEnumerable<double> seq)
            {
                Running = false;
                SequenceTimer = 0;

                sequenceSM?.Dispose();
                sequenceSM = null;

                if (seq != null)
                {
                    Running = true;
                    sequenceSM = seq.GetEnumerator();
                }
            }
        }
    }
}
