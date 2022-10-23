using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TaiwuModdingLib.Core.Plugin;
using HarmonyLib;
using Unity;

namespace JieYiMatchmaker
{
    [PluginConfig("JieYiMatchmaker", "evixenon", "0.1.1")]
    public class JieYiMatchmakerFrontend : TaiwuRemakePlugin
    {
        Harmony harmony;
        public override void Dispose()
        {
            if (harmony != null)
            {
                harmony.UnpatchSelf();
            }
        }

        public override void Initialize()
        {
            harmony = Harmony.CreateAndPatchAll(typeof(JieYiMatchmakerFrontend));
        }

    }
}
