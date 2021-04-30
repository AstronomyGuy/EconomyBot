using Discord;
using Discord.Commands;
using Discord.WebSocket;
using EconomyBot.Economy;
using Extreme.Statistics.Distributions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EconomyBot
{
    public class CoreClass
    {

        public static readonly HttpClient HTTPclient = new HttpClient();

        public static ServerEconomy economy;

        public static List<MessageResponseThread> responseThreads = new List<MessageResponseThread>();

        public static DiscordSocketClient client;
        static private CommandService Commands;

        public static string DEFAULT_PREFIX = "$";
        public static ulong SERVER_ID = 799129855563137054;
        internal static bool debug = false;

        static void Main(string[] args)
        {
            Console.WriteLine("Starting...");
            Thread t = new Thread(new ThreadStart(async delegate {
                CoreClass.updateLoop();
            }));
            t.Start();
            Console.WriteLine("Background Loop Started");
            new CoreClass().MainAsync().GetAwaiter().GetResult();
            Console.ReadLine();
        }

        public static void updateDB() {
            MongoUtil.updateEcon(CoreClass.economy);
        }
        public static void manualUpdate() {
            economy.updateAll();
            MongoUtil.updateEcon(CoreClass.economy);
        }

        public static DateTime nextUpdate;
        public static int updates = 0;
        public static void updateLoop() {
            nextUpdate = DateTime.Now.AddHours(2);
            Thread.Sleep(10000);
            while (true) {
                if (economy == null) {
                    continue;
                }           
                
                if (updates % 4 == 0) {
                    economy.updateAll();
                    nextUpdate = DateTime.Now.AddHours(2);
                }

                MongoUtil.updateEcon(CoreClass.economy);
                updates++;
                //Wait 30 mins
                Thread.Sleep(1000 * 60 * 30);                
            }
        }

        private async Task MainAsync()
        {

            client = new DiscordSocketClient();
            Commands = new CommandService();


            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug //Set to Error or Critical at Release
            });
            Console.WriteLine("Logging In...");
            await client.LoginAsync(TokenType.Bot, Tokens.token_bot);
            await client.StartAsync();
            Console.WriteLine("Logged In");
            await client.SetGameAsync("$help | THE LINE ONLY GOES UP!!!!!!!");

            economy = MongoUtil.getEconomy();

            CommandHandler ch = new CommandHandler(client, Commands);
            await ch.InstallCommandsAsync();

            client.Ready += ClientReady;
            client.JoinedGuild += Client_JoinedGuild;
            client.LeftGuild += Client_LeftGuild;

            //Put stuff here
            //client.MessageReceived += IN COMMAND HANDLER;

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Client_LeftGuild(SocketGuild arg)
        {
                        
            return Task.CompletedTask;
        }

        private Task Client_JoinedGuild(SocketGuild arg)
        {
            
            return Task.CompletedTask;
        }

        static private async Task ClientReady()
        {
            //await MongoUtil.indexGuildAsync(614202470289244171);
            Console.WriteLine("Ready");
        }
    }
}
