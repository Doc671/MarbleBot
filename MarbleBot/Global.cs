using Discord.Commands;
using MarbleBot.BaseClasses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarbleBot
{
    /// <summary> Contains global variables. </summary>
    static class Global
    {
        internal static CommandService CommandService { get; set; }

        internal static Random Rand = new Random();
        internal static DateTime StartTime = new DateTime();
        internal static string YTKey = "";
        internal static string GDKey = "";
        internal static string UoM = "<:unitofmoney:372385317581488128>";
        internal static ulong BotId = 286228526234075136;
        internal static Dictionary<string, string> Autoresponses = new Dictionary<string, string>();
        internal static DateTime ARLastUse = new DateTime();
        internal static ulong[] BotChannels = { 229280519697727488, 269922990936948737, 318053391777660929, 394090786578366474, 409655798730326016, 540638882740305932 };
        internal static ulong[] UsableChannels = { 229280519697727488, 269922990936948737, 318053391777660929, 394090786578366474, 409655798730326016, 540638882740305932, 252481530130202624, 224478087046234112, 293837572130603008 };

        // Games
        internal static Dictionary<ulong, byte> RaceAlive = new Dictionary<ulong, byte>();
        internal static Dictionary<ulong, Queue<Item>> ScavengeInfo = new Dictionary<ulong, Queue<Item>>();
        internal static List<Task> ScavengeSessions = new List<Task>();
        internal static Dictionary<ulong, Siege> SiegeInfo = new Dictionary<ulong, Siege>();
        internal static List<Task> Sieges = new List<Task>();
        internal static readonly Boss PreeTheTree = new Boss("Pree the Tree", 300, Difficulty.Simple, "https://cdn.discordapp.com/attachments/296376584238137355/541383182197719040/BossPreeTheTree.png", new Attack[] {
            new Attack("Falling Leaves", 3, 40, MSE.None),
            new Attack("Spinning Leaves", 3, 50, MSE.None),
            new Attack("Acorn Bomb", 5, 55, MSE.None),
            new Attack("Floating Twigs", 2, 65, MSE.None)
        });
        internal static readonly Boss HATTMANN = new Boss("HATT MANN", 600, Difficulty.Easy, "https://cdn.discordapp.com/attachments/296376584238137355/541383185481596940/BossHATTMANN.png", new Attack[] {
            new Attack("Hat Trap", 4, 45, MSE.None),
            new Attack("Inverted Hat", 3, 45, MSE.None),
            new Attack("HATT GUNN", 6, 40, MSE.None),
            new Attack("Hat Spawner", 2, 90, MSE.None)
        });
        internal static readonly Boss Orange = new Boss("Orange", 1200, Difficulty.Decent, "https://cdn.discordapp.com/attachments/296376584238137355/541383189114126339/BossOrange.png", new Attack[] {
            new Attack("Poup Soop Barrel", 4, 45, MSE.None),
            new Attack("Poup Krumb", 8, 50, MSE.None),
            new Attack("ORANGE HEDDS", 5, 40, MSE.None),
            new Attack("How To Be An Idiot Vol. 3", 3, 45, MSE.None)
        });
        internal static readonly Boss Green = new Boss("Green", 1500, Difficulty.Risky, "https://cdn.discordapp.com/attachments/296376584238137355/541383199943819289/BossGreen.png", new Attack[] {
            new Attack("Wobbly Toxicut", 9, 45, MSE.Poison),
            new Attack("Falling Hellslash", 11, 45, MSE.None),
            new Attack("Attractive Domesday", 17, 20, MSE.None),
            new Attack("Spinning Pyroclash", 8, 65, MSE.None),
            new Attack("Accurate Flarer", 6, 90, MSE.None)
        });
        internal static readonly Boss Destroyer = new Boss("Destroyer", 3720, Difficulty.Extreme, "https://cdn.discordapp.com/attachments/296376584238137355/541383205048287262/BossDestroyer.png", new Attack[] {
            new Attack("Antimatter Missile", 13, 50, MSE.None),
            new Attack("Annihilator-A", 11, 45, MSE.None),
            new Attack("Flamethrower", 10, 55, MSE.None),
            new Attack("Black Hole", 14, 55, MSE.None),
            new Attack("Repulsor Blast", 8, 70, MSE.None)
        });
        internal static readonly Boss HelpMeTheTree = new Boss("Help Me the Tree", 500, Difficulty.Easy, "https://cdn.discordapp.com/attachments/296376584238137355/548220911317286932/BossHelpMeTheTree.png", new Attack[] {
            new Attack("Donation Box", 5, 45, MSE.None),
            new Attack("Cry For Help", 0, 40, MSE.Doom),
            new Attack("Sandstorm", 3, 75, MSE.None),
            new Attack("Decay", 2, 50, MSE.Poison)
        });
        internal static readonly Boss Erango = new Boss("erangO", 1200, Difficulty.Moderate, "https://cdn.discordapp.com/attachments/296376584238137355/548221808294232071/unknown.png", new Attack[] {
            new Attack("erangO Pellets", 6, 90, MSE.None),
            new Attack("Doom Beam", 10, 45, MSE.Doom),
            new Attack("Fake Poup Soop", 8, 55, MSE.None),
            new Attack("Unapproved by Orange", 3, 55, MSE.Stun)
        });
        internal static readonly Boss Octopheesh = new Boss("Octopheesh", 800, Difficulty.Risky, "https://cdn.discordapp.com/attachments/296376584238137355/548220914488049665/BossOctopheesh.png", new Attack[] {
            new Attack("Two Bipheesh", 13, 75, MSE.None),
            new Attack("EMP Burst", 11, 45, MSE.Stun),
            new Attack("Vile Beam", 18, 40, MSE.None),
            new Attack("Pheesh Swarm", 11, 95, MSE.None)
        });
    }
}
