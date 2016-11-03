using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace DeathCounter
{
    [ApiVersion(1, 23)]
    public class DeathCounter : TerrariaPlugin
    {
        #region Info
        public override string Name { get { return "DeathCounter"; } }
        public override string Author { get { return "Ryozuki"; } }
        public override string Description { get { return "A example"; } }
        public override Version Version { get { return new Version(1, 0, 0); } }
        #endregion

        public DeathCounter(Main game) : base(game)
        {

        }

        #region Initialize
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
            }
        }

        void OnInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("exampleCommand.use", exampleCommand, "excom")
            {
                HelpText = "Usage: /excom ags"
            });
        }

        void exampleCommand(CommandArgs e)
        {
            if (e.Parameters.Count == 0)
            {
                e.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}excom args", Commands.Specifier);
                return;
            }

            string args = String.Join(" ", e.Parameters.ToArray());


            try
            {
                ///command logic
                int result = 1;
                e.Player.SendSuccessMessage("Result is: {0}.", result);
            }
            catch
            {
                e.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}calc <operation>", Commands.Specifier);
            }
        }
    }
}