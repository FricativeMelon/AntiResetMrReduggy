using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Objects;
using static StardewValley.Objects.Chest;

namespace AntiResetMrReduggy
{
    class ModConfig
    {
    }

    class ModData
    {
        public int runs = 0;
        public string location = "Farmhouse";
        public int time = 0;
        public DateTime realTime = DateTime.MinValue;
        public int dayNum = 0;
        public string saveFileName = "";
        public bool metReduggy = false;
    }

    class ReduggyDialogueBox : DialogueBox
    {
        public string oldDialogueString;
        private Texture2D te;

        public ReduggyDialogueBox(Texture2D te, string dialogue, string preface = "")
            : base(new Dialogue(preface+dialogue, MakeReduggy(te)))
        {
            this.te = te;
            oldDialogueString = dialogue;
        }


        private static NPC MakeReduggy(Texture2D te)
        {
            return new NPC(null, new Vector2(0, 0), "Mines", 0, "Mr. Reduggy", false, null, te);
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            receiveLeftClick(x, y, playSound);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (characterIndexInDialogue < getCurrentString().Length - 1)
            {
                Game1.activeClickableMenu =  new ReduggyDialogueBox(te, oldDialogueString, "Seriously? You're not even listening, are you?"
                    + " I'd better start from the top, since you obviously haven't heard a word I've said.$5#$b#");
            }
            else
            {
                base.receiveLeftClick(x, y, playSound);
            }
        }
    }

    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {

        /*********
        ** Properties
        *********/
        /// <summary>The mod configuration from the player.</summary>
        private ModConfig Config;

        private bool visitedFarmToday = false;

        private bool metReduggy = false;

        private int time = 0;

        private Texture2D reduggyPortraits;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            //this.Config = this.Helper.ReadConfig<ModConfig>();
            helper.Events.Player.Warped += this.OnWarp;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
            helper.Events.Content.AssetRequested += this.OnAssetRequested;

            reduggyPortraits = Helper.ModContent.Load<Texture2D>("assets/portraits.png");
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo("FricativeMelon.AntiResetMrReduggy"))
            {
                e.LoadFromModFile
                    <Texture2D>
                    (Path.Combine("assets", "portraits.png") , AssetLoadPriority.Exclusive);
            }
        }

            private string GetReduggyString(ModData context)
        {
            IDictionary<string, string> data = Helper.ModContent.Load<Dictionary<string, string>>("assets/data.json");
            string res = data["intro"+Math.Min(6, context.runs-1).ToString()];
            if (!metReduggy)
            {
                res += data["first"];
            }
            if (context.location.StartsWith("UndergroundMine") || context.location.StartsWith("Mine"))
            {
                res += data["mines"];
            }
            else if (context.time <= 1100)
            {
                res+=data["early"];
            }
            else if (context.time <= 1600)
            {
                res += data["late"];
            }
            else if (context.time <= 2100)
            {
                res += data["late"];
            }
            else if (context.time <= 2100)
            {
                res += data["late"];
            }
            return res + data["outro"+Math.Min(4, context.runs/2).ToString()];
        }

        /*********
        ** Private methods
        *********/

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            visitedFarmToday = false;
            time = 0;

            var model = this.Helper.Data.ReadGlobalData<ModData>("reset");
            metReduggy = model != null && model.metReduggy;

            if (model == null || model.saveFileName != Constants.SaveFolderName
                || model.dayNum < Game1.Date.TotalDays || DateTime.Now.Subtract(model.realTime) > TimeSpan.FromMinutes(612))
            {
                model = new ModData
                {
                    dayNum = Game1.Date.TotalDays,
                    saveFileName = Constants.SaveFolderName,
                    runs = 0,
                    realTime = DateTime.Now,
                    metReduggy = metReduggy
                };
                this.Helper.Data.WriteGlobalData("reset", model);
            }

        }

        private void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (visitedFarmToday && e.NewTime - time > 500)
            {
                time = e.NewTime;

                var model = this.Helper.Data.ReadGlobalData<ModData>("reset");

                model.time = time;

                // save data (if needed)
                this.Helper.Data.WriteGlobalData("reset", model);
            }
        }


        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnWarp(object sender, WarpedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            var model = this.Helper.Data.ReadGlobalData<ModData>("reset");

            if (visitedFarmToday)
            {
                model.location = e.NewLocation.Name;
            }

            if (!visitedFarmToday && e.NewLocation is Farm)
            {
                visitedFarmToday = true;

                model.runs++;

                if (model.runs > 1)
                {
                    model.metReduggy = true;
                    /*Game1.player.setTileLocation(new Vector2(64, 15));
                    Game1.player.faceDirection(2);
                    Game1.player.position.Y -= 16f;*/
                    DialogueBox db = new ReduggyDialogueBox(reduggyPortraits, GetReduggyString(model));
                    Game1.activeClickableMenu = db;
                }
            }
            // save data (if needed)
            this.Helper.Data.WriteGlobalData("reset", model);
        }
    }
}