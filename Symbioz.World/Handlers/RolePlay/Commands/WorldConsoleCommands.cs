using Symbioz.Core;
using Symbioz.Core.Commands;
using Symbioz.World.Network;

namespace Symbioz.World.Handlers.RolePlay.Commands
{
    public class WorldConsoleCommands
    {
        static Logger logger = new Logger();

        /// <summary>
        /// Execute an in-game command on behalf of a connected character.
        /// Usage: cmd <playerName> <commandName> [args]
        /// Example: cmd Pseudo item 12345 1
        ///          cmd Pseudo go 153879553
        ///          cmd Pseudo kick AutreJoueur
        /// </summary>
        [ConsoleCommand("cmd")]
        public static void RunAsPlayer(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                logger.Alert("Usage: cmd <playerName> <commandName> [args]");
                return;
            }

            var parts = input.Split(' ', 3);
            if (parts.Length < 2)
            {
                logger.Alert("Usage: cmd <playerName> <commandName> [args]");
                return;
            }

            string playerName = parts[0];
            string commandName = parts[1];
            string args = parts.Length > 2 ? parts[2] : string.Empty;

            WorldClient client = WorldServer.Instance.GetOnlineClient(playerName);
            if (client == null)
            {
                logger.Alert("Player '" + playerName + "' is not online.");
                return;
            }

            string fullCommand = CommandsHandler.CommandsPrefix + commandName + (args.Length > 0 ? " " + args : "");
            try
            {
                CommandsHandler.Handle(fullCommand, client);
                logger.Color2($"[OK] {commandName} → {playerName}" + (args.Length > 0 ? $" ({args})" : ""));
            }
            catch (Exception ex)
            {
                logger.Alert($"[FAIL] {commandName} → {playerName} : {ex.Message}");
            }
        }
    }
}
