using Discord.Commands;
using Discord.WebSocket;
using EconomyBot.Modules;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EconomyBot
{
    //From https://discord.foxbot.me/docs/guides/commands/intro.html
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;

        public CommandHandler(DiscordSocketClient client, CommandService commands)
        {
            _commands = commands;
            _client = client;
        }

        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            _client.MessageReceived += HandleCommandAsync;

            //_commands.AddTypeReader(typeof(AXES), new AxesTypeReader());

            // Here we discover all of the command modules in the entry 
            // assembly and load them. Starting from Discord.NET 2.0, a
            // service provider is required to be passed into the
            // module registration method to inject the 
            // required dependencies.
            //
            // If you do not use Dependency Injection, pass null.
            // See Dependency Injection guide for more information.
            await _commands.AddModuleAsync<GeneralModule>(null);
            await _commands.AddModuleAsync<HelpModule>(null);
            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: null);
        }

        public async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            //MongoUtil.addMessage(messageParam);

            CoreClass.responseThreads.ForEach(r => r.CheckResponse(messageParam));

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);

            // Create a number to track where the prefix ends and the command begins
            int argPos = CoreClass.DEFAULT_PREFIX.Length;
            string p = CoreClass.DEFAULT_PREFIX;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasStringPrefix(p, ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
            {
                return;
            }
            else if (CoreClass.debug && !GeneralModule.BOT_DEVS.Contains(message.Author.Id)) {
                await message.Channel.SendMessageAsync("Debug mode is currently on. If byte isn't actively working on the bot, he probably forgot to turn it off.");
                return;
            }
            else
            {
                Console.WriteLine($"Command Received: {message.Content}");
            }


            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.

            // Keep in mind that result does not indicate a return value
            // rather an object stating if the command executed successfully.
            var result = await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: null);

            // Optionally, we may inform the user if the command fails
            // to be executed; however, this may not always be desired,
            // as it may clog up the request queue should a user spam a
            // command.
            if (!result.IsSuccess)
            {
                if (result.ErrorReason.Contains("Cannot send messages to this user"))
                {
                    await context.Channel.SendMessageAsync("This command only works if I'm able to send you a DM.");
                    return;
                }
                if (result.ErrorReason.Contains("Unknown command."))
                {
                    await context.Channel.SendMessageAsync("That's not a valid command.");
                    return;
                }
                if (result.ErrorReason.Contains("The input text has too few parameters"))
                {
                    await context.Channel.SendMessageAsync($"You're missing some parameters there, you can use `{CoreClass.DEFAULT_PREFIX}help` to find out what parameters are needed.");
                    return;
                }
                if (result.ErrorReason.Contains("The input text has too many parameters.")) {
                    await context.Channel.SendMessageAsync("Too many parameters. Try putting some of it in quotation marks. Otherwise use $help and double-check the syntax of this command.");
                    return;
                }
                if (result.ErrorReason.Contains("User not found.")) {
                    await context.Channel.SendMessageAsync("User not found.");
                    return;
                }
                await context.Channel.SendMessageAsync($"An unhandled error ocurred: ```{result.ErrorReason}```\n<@374280713387900938> you incompetent FOOL come look at this");
                //ISocketMessageChannel c = context.Client.GetChannel(834128708801134613) as ISocketMessageChannel;
                //c.SendMessageAsync($"**Error in {context.Guild.Name} - {context.Channel.Name}:** {result.ErrorReason}");                
            }
        }
    }
}
