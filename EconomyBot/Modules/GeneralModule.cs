using Discord;
using Discord.Commands;
using Discord.WebSocket;
using EconomyBot.Economy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static EconomyBot.MessageResponseThread;

namespace EconomyBot.Modules
{
    class GeneralModule : ModuleBase<SocketCommandContext>
    {
        [Command("next-turn")]
        [Summary("Gives the amount of time until the next turn starts and profits are updated")]
        public async Task nextTurn() {
            TimeSpan t = CoreClass.nextUpdate.Subtract(DateTime.Now);
            await Context.Channel.SendMessageAsync($"The next turn starts in {t.Hours} hour(s) and {t.Minutes} minute(s)");
            return;
        }
        [Command("work")]
        [Summary("Work at the companies you're employed in, and get your wage for it.")]
        public async Task work() {
            
            Individual i = CoreClass.economy.getUser(Context.User.Id);
            if (!i.canWork()) {
                TimeSpan t = CoreClass.nextUpdate.Subtract(DateTime.Now);
                await Context.Channel.SendMessageAsync($"You've already worked! Check back later to work again.\nYou'll be able to work again in {t.Hours} hour(s) and {t.Minutes} minute(s)");
                return;
            }
            bool w = false;
            foreach (ulong j in i.jobIDs) {
                if (j == 0)
                {
                    continue;
                }
                else {
                    w = true;
                }
                Company c = CoreClass.economy.getCompany(com => com.ID == j);
                if (c.work(i))
                {
                    CoreClass.economy.updateCompany(c);
                    await Context.Channel.SendMessageAsync($"You worked at {c.name} and got ${c.employeeWages[i.ID]}");
                }
                else {
                    if (i.jobIDs[0] == j)
                    {
                        i.jobIDs[0] = 0;
                    }
                    else if (i.jobIDs[1] == j) {
                        i.jobIDs[1] = 0;
                    }
                    CoreClass.economy.updateCompany(c);
                    await Context.Channel.SendMessageAsync("You are unemployed.");
                }                
            }
            if (!w) {
                await Context.Channel.SendMessageAsync("You are unemployed.");
            }
            CoreClass.economy.updateUser(i);
        }
        [Command("start-company")]
        [Summary("Start a company and join the market.")]
        public async Task startCompany([Summary("Name of the company")] string name, double self_wage = 0.0) {
            //if (CoreClass.economy.companies.Exists(c => c.orgOwner == Context.User.Id)) {
            //    Context.Channel.SendMessageAsync("You can only have one company at a time.");
            //    return;
            //}
            if (double.IsNaN(self_wage) || double.IsInfinity(self_wage))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            if (name.Length > 128) {
                Context.Channel.SendMessageAsync("That name is too long! (Limit: 128 characters)");
                return;
            }
            Company c = new Company {
                ID = (ulong)(CoreClass.economy.getNextCompanyId()),
                creationTime = DateTime.Now,
                name = name.Contains("™") ? name : name + "™",
                balance = 0,
                orgOwner = Context.User.Id
            };
            c.products.Add("nothing", 0);
            c.productStock.Add("nothing", 0);
            c.addEmployee(Context.User.Id, self_wage);
            Individual i = CoreClass.economy.getUser(Context.User.Id);
            i.ownedStock.Add(new Stock(Context.User.Id, c.ID, Company.SHARES_PER_COMPANY));
            i.joinCompany(c.ID);
            CoreClass.economy.addCompany(c);
            CoreClass.economy.updateUser(i);
            await Context.Channel.SendMessageAsync($"Welcome to the market, {name}!");
        }
        [Command("start-company")]
        [Summary("Start a company and join the market.")]
        public async Task startCompany([Summary("Name of the company")] params string[] name)
        {
            //string output = "";
            //foreach (string n in name) {
            //    output += n + " ";
            //}
            //await startCompany(output);
            Context.Channel.SendMessageAsync("You need to put the company name in quotes!");
        }
        [Command("hire")]
        [Summary("Offer someone a job at your company.")]
        public async Task hire(ulong company_id, SocketUser user, double wage, string product) {
            if (double.IsNaN(wage) || double.IsInfinity(wage))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            Individual hirer = CoreClass.economy.getUser(Context.User.Id);            
            Company c;
            try
            {
                c = CoreClass.economy.getCompany(c => c.ID == company_id && c.orgOwner == Context.User.Id);
            }
            catch (Exception e) {
                await Context.Channel.SendMessageAsync("There was a problem finding your Company. If you don't own a Company, that would be why.");
                return;
            }
            if (c == null)
            {
                await Context.Channel.SendMessageAsync("You need a company before you can start hiring people!");
                return;
            }
            else {
                MessageResponseThread m = new MessageResponseThread(user.Id, Context.Channel.Id, new Regex("(yes|nope|yep|no|y|n)", RegexOptions.IgnoreCase));

                async void handleEventAsync(object sender, EventArgs e) {
                    MessageResponseEventArgs me = e as MessageResponseEventArgs;
                    me.message = me.message.ToLower();
                    if (me.message == "yes" || me.message == "y" || me.message == "yep")
                    {
                        Individual employ = CoreClass.economy.getUser(user.Id);
                        if (!employ.joinCompany(c.ID))
                        {
                            await Context.Channel.SendMessageAsync($"Failed to join company: {user.Username}#{user.Discriminator} is already working two jobs!");
                            return;
                        }
                        c.addEmployee(user.Id, wage, product);
                        CoreClass.economy.updateUser(employ);
                        CoreClass.economy.updateCompany(c);
                        await Context.Channel.SendMessageAsync($"Welcome to {c.name}, {user.Mention}!");
                    }
                    else {
                        Context.Channel.SendMessageAsync($"{user.Mention} has refused to join {c.name}, sorry {Context.User.Mention}...");
                    }
                }

                m.ResponseReceived += handleEventAsync;
                CoreClass.responseThreads.Add(m);
                Context.Channel.SendMessageAsync($"{Context.User.Mention} has offered {user.Mention} a job at {c.name} producing {product}! Do you accept (yes/no)?");
            }
        }
        [Command("join")]
        [Summary("Offer to join a user's company and produce some product.")]
        public async Task join(ulong company_id, double wage, string product) {
            if (double.IsNaN(wage) || double.IsInfinity(wage))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            if (CoreClass.economy.companies.Exists(c => c.ID == company_id))
            {
                Company c = CoreClass.economy.getCompany(c => c.ID == company_id);
                SocketUser u = Context.Client.GetUser(c.orgOwner);
                join(u, wage, product);
            }
            else {
                Context.Channel.SendMessageAsync("That company doesn't exist.");
            }
        }
        [Command("join")]
        [Summary("Offer to join a user's company and produce some product.")]
        public async Task join(SocketUser user, double wage, string product)
        {
            if (double.IsNaN(wage) || double.IsInfinity(wage))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            Individual hirer = CoreClass.economy.getUser(user.Id);
            Company c;
            try
            {
                c = CoreClass.economy.getCompany(c => c.orgOwner == user.Id);
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync("There was a problem finding their Company. If they don't own a Company, that would be why.");
                return;
            }
            if (c == null)
            {
                await Context.Channel.SendMessageAsync("They need a company before you can start be hired by them!");
                return;
            }
            else
            {
                MessageResponseThread m = new MessageResponseThread(Context.User.Id, Context.Channel.Id, new Regex("(yes|nope|yep|no|y|n)", RegexOptions.IgnoreCase));

                async void handleEventAsync(object sender, EventArgs e)
                {
                    MessageResponseEventArgs me = e as MessageResponseEventArgs;
                    if (me.message == "yes" || me.message == "y" || me.message == "yep")
                    {
                        Individual employ = CoreClass.economy.getUser(Context.User.Id);
                        if (!employ.joinCompany(c.ID))
                        {
                            await Context.Channel.SendMessageAsync($"{Context.User.Username}#{Context.User.Discriminator} is already working two jobs!");
                            return;
                        }
                        c.addEmployee(Context.User.Id, wage, product);
                        CoreClass.economy.updateUser(employ);
                        CoreClass.economy.updateCompany(c);
                        await Context.Channel.SendMessageAsync($"Welcome to {c.name}, {Context.User.Mention}!");
                    }
                    else
                    {
                        Context.Channel.SendMessageAsync($"{Context.User.Mention} has refused to join {c.name}, sorry {Context.User.Mention}...");
                    }
                }

                m.ResponseReceived += handleEventAsync;
                CoreClass.responseThreads.Add(m);
                Context.Channel.SendMessageAsync($"{Context.User.Mention} has offered to join {user.Mention}'s company, {c.name} for a wage of ${wage}! Do you accept (yes/no)?");
            }
        }
        [Summary("Fire an employee from your company.")]
        [Command("fire")]
        public async Task fire(SocketUser user, ulong company_id)
        {
            Individual hirer = CoreClass.economy.getUser(Context.User.Id);
            Company c;
            try
            {
                c = CoreClass.economy.getCompany(c => c.ID == company_id && c.orgOwner == Context.User.Id);
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync("There was a problem finding your Company. If you don't own a Company, that would be why.");
                return;
            }
            if (c == null)
            {
                await Context.Channel.SendMessageAsync("You need a company before you can start hiring people!");
                return;
            }
            else
            {
                Individual employ = CoreClass.economy.getUser(user.Id);
                if (!employ.leaveCompany(c.ID))
                {
                    await Context.Channel.SendMessageAsync($"{user.Username}#{user.Discriminator} wasn't there to begin with!");
                    return;
                }
                c.removeEmployee(user.Id);
                CoreClass.economy.updateUser(employ);
                CoreClass.economy.updateCompany(c);
                await Context.Channel.SendMessageAsync($"{user.Mention} has been fired from {c.name}.");
            }
        }
        [Command("job-list")]
        [Summary("List all companies that you can join.")]
        public async Task jobList(int page = 1) {
            if (page < 1) {
                await Context.Channel.SendMessageAsync("There are no negative pages, nor is there a 0 page.");
                return;
            }
            List<Company> companies = CoreClass.economy.listCompany(p => true);
            companies.Sort();
            EmbedBuilder eb = new EmbedBuilder();
            for (int i = (page - 1) * 25; i < page * 25 && i < companies.Count; i++) {
                if (companies[i] == null) {
                    continue;
                }
                EmbedFieldBuilder ef = new EmbedFieldBuilder();                
                ef.Name = $"{(companies[i].name == null ? "[not found]" : companies[i].name)} (ID: {companies[i].ID})";
                SocketUser found = Context.Client.GetUser(companies[i].orgOwner);
                
                string desc = "";
                if (found == null)
                {
                    desc = $"**Founder: ** [user not found]\n" +
                    $"**Average Pay: ** {Math.Round(companies[i].employeeWages.Values.Average(), 2)}\n" +
                    $"**Employees: ** {companies[i].employeeWages.Count()}";
                }
                else {
                    desc = $"**Founder: ** {found.Username}#{found.Discriminator}\n" +
                    $"**Average Pay: ** {Math.Round(companies[i].employeeWages.Values.Average(), 2)}\n" +
                    $"**Employees: ** {companies[i].employeeWages.Count()}";
                }
                
                ef.Value = desc;
                eb.AddField(ef);
            }
            eb.Color = Color.Purple;
            eb.WithTitle($"Jobs (Page {page})");
            eb.WithFooter("Unemployed? Just get a job you lazy bum!");
            await Context.Channel.SendMessageAsync(embed: eb.Build());
        }
        [Command("full-gdp-graph")]
        [Summary("Get a graph of the economy")]
        public async Task economyGraph() {
            try
            {
                List<EconomyObject> eObjects = CoreClass.economy.allEconObj();
                List<List<double>> economyObjects = new List<List<double>>();
                eObjects.ForEach(t => economyObjects.Add(t.getHistory()));
                List<double> fullHistory = new List<double>();
                foreach (List<double> eObj in economyObjects)
                {
                    while (fullHistory.Count < eObj.Count)
                    {
                        fullHistory.Add(0.0);
                    }
                    for (int i = eObj.Count - 1; i >= 0; i--)
                    {
                        //eObj.Count - (i + 1)
                        fullHistory[eObj.Count - (i + 1)] += eObj[i];
                    }
                }
                string filename = DateTime.Now.ToBinary().ToString();
                RandUtil.ActivityGraph(filename, fullHistory, Context.Channel);
                await Context.Channel.SendFileAsync(filename + ".png");
            }
            catch (Exception ex) {
                SocketUser u =  Context.Client.GetUser(374280713387900938);
                await u.SendMessageAsync($"**`{ex.Message}`**");
                await u.SendMessageAsync($"```{ex.StackTrace}```");
                await Context.Channel.SendMessageAsync("There was an error generating this graph. I've DMed byte the details, so he'll be on that when Birnam Wood moves.");
                
            }
        }
        [Command("gdp-graph")]
        [Alias("user-graph")]
        [Summary("Economy graph but without the GUBMENT")]
        public async Task userEconomyGraph()
        {
            try
            {
                List<EconomyObject> eObjects = CoreClass.economy.userEconObj();
                List<List<double>> economyObjects = new List<List<double>>();
                eObjects.ForEach(t => economyObjects.Add(t.getHistory()));
                List<double> fullHistory = new List<double>();
                foreach (List<double> eObj in economyObjects)
                {
                    while (fullHistory.Count < eObj.Count)
                    {
                        fullHistory.Add(0.0);
                    }
                    for (int i = eObj.Count - 1; i >= 0; i--)
                    {
                        //eObj.Count - (i + 1)
                        fullHistory[eObj.Count - (i + 1)] += eObj[i];
                    }
                }
                string filename = DateTime.Now.ToBinary().ToString();
                RandUtil.ActivityGraph(filename, fullHistory, Context.Channel);
                await Context.Channel.SendFileAsync(filename + ".png");
            }
            catch (Exception ex)
            {
                SocketUser u = Context.Client.GetUser(374280713387900938);
                await u.SendMessageAsync($"**`{ex.Message}`**");
                await u.SendMessageAsync($"```{ex.StackTrace}```");
                await Context.Channel.SendMessageAsync("There was an error generating this graph. I've DMed byte the details, so he'll be on that when Birnam Wood moves.");

            }
        }
        [Command("leaderboard")]
        [Summary("Shows the top 10 (or a custom amount) users by wealth to create an articifial sense of competition amongst the working class")]
        public async Task leaderboard(int count = 10) {
            List<Individual> individuals = CoreClass.economy.citizens;
            individuals.Sort((p, q) => (p.balance + p.cashBalance).CompareTo(q.cashBalance + q.balance));
            individuals.Reverse();
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle($"Top {count} users");
            embed.WithColor(Color.Green);            
            embed.WithFooter("[Insert funny here]");
            string desc = "";
            int offset = 0;
            for (int i = 0; i < count+offset && i < individuals.Count; i++) {
                SocketUser u = Context.Client.GetUser(individuals[i].ID);
                if (u == null) {
                    offset++;
                    continue;
                }
                desc += $"**{i+1-offset})** {u.Username}#{u.Discriminator}: `${individuals[i].balance + individuals[i].cashBalance}`\n";
            }
            embed.WithDescription(desc);
            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }
        [Command("That's not a valid command.")]
        [Alias("That's not a valid command", "that's not a valid command.", "that's not a valid command")]
        [Summary("Yugoslavia")]
        public async Task bamboozle() {
            await Context.Channel.SendMessageAsync("Alright funny guy, you think you're some sorta comedian? A humorous person, perhaps? You think you're funny?");
        }
        [Command("sad")]
        [Summary("sad")]
        public async Task sad() {
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithImageUrl("https://cdn.discordapp.com/attachments/793168889198018620/818172485768839198/unknown.png");
            await Context.Channel.SendMessageAsync(embed: eb.Build());
            //await Context.Channel.SendFileAsync(@"https://cdn.discordapp.com/attachments/793168889198018620/818172485768839198/unknown.png");
        }
        [Command("dissolve-company")]
        [Alias("remove-company", "remove company", "dissolve company")]
        [Summary("Dissolve your company and get it removed from the market.")]
        public async Task dissolveCompany(ulong company_id) {
            Company c = null;
            try
            {
                c = CoreClass.economy.getCompany(c => c.ID == company_id && c.orgOwner == Context.User.Id);
            }
            catch (Exception e) {
                await Context.Channel.SendMessageAsync("There was an issue getting your comapny; If you don't have a company, there's your problem.");
            }
            if (c == null)
            {
                await Context.Channel.SendMessageAsync("You don't have a company to remove!");
            }
            else {
                CoreClass.economy.citizens.ForEach(u => u.leaveCompany(c.ID));
                Individual i = CoreClass.economy.getUser(c.orgOwner);
                i.balance += c.balance;
                CoreClass.economy.updateUser(i);
                CoreClass.economy.removeCompany(c);
                await Context.Channel.SendMessageAsync($"Removed {c.name} and transferred all debts/money to founder!");
            }
        }
        [Command("balance")]
        [Summary("Gets your current balance.")]
        public async Task balance() {
            Individual i = CoreClass.economy.getUser(Context.User.Id);
            Company c = CoreClass.economy.getCompany(p => p.orgOwner == Context.User.Id);

            await Context.Channel.SendMessageAsync($"Your balance is: ${Math.Round(i.balance, 2)} (Bank) | ${Math.Round(i.cashBalance, 2)} (Cash)");

            if (c != null) {
                await Context.Channel.SendMessageAsync($"{c.name}'s balance is: ${Math.Round(c.balance, 2)}");
            }
        }
        [Command("balance")]
        [Summary("Gets the balance of some user")]
        public async Task balance(SocketUser user)
        {
            Individual i = CoreClass.economy.getUser(user.Id);
            Company c = CoreClass.economy.getCompany(p => p.orgOwner == user.Id);

            await Context.Channel.SendMessageAsync($"{user.Username}#{user.Discriminator} balance is: ${Math.Round(i.balance, 2)} (Bank) | ${Math.Round(i.cashBalance, 2)} (Cash)");

            if (c != null)
            {
                await Context.Channel.SendMessageAsync($"{c.name}'s balance is: ${Math.Round(c.balance, 2)}");
            }
        }

        [Command("gov-balance")]
        [Alias("government-balance","fed-balance")]
        [Summary("Get the balance of the GUBMENT")]
        public async Task govBalance()
        {
            Government g = CoreClass.economy.gov;
            await Context.Channel.SendMessageAsync($"The Government's balance is: ${g.balance}\nIncome: ${g.getGrossIncome()} | Spending: ${g.getSpending()}");
        }
        [Command("set-gov-income")]
        [Alias("gov-income")]
        [Summary("Get the income of the GUBMENT.")]
        public async Task setGovIncome(double inc) {
            if (double.IsNaN(inc) || double.IsInfinity(inc))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            SocketGuildUser gU = Context.User as SocketGuildUser;
            if (!gU.Roles.Contains(Context.Guild.GetRole(774802078391992330)) || !CoreClass.economy.botMods.Contains(Context.User.Id))
            {
                Context.Channel.SendMessageAsync("You need the `Secretary of Treasury` role or be a bot moderator to do that.");
                return;
            }
            CoreClass.economy.gov.setIncome(inc);
            Context.Channel.SendMessageAsync($"Get government income to ${inc}!");
        }
        [Command("set-gov-spending")]
        [Alias("gov-spending")]
        [Summary("Get the spending of the GUBMENT.")]
        public async Task setGovSpending(double spending)
        {
            if (double.IsNaN(spending) || double.IsInfinity(spending))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            SocketGuildUser gU = Context.User as SocketGuildUser;
            if (!gU.Roles.Contains(Context.Guild.GetRole(774802078391992330)) || !CoreClass.economy.botMods.Contains(Context.User.Id))
            {
                Context.Channel.SendMessageAsync("You need the `Secretary of Treasury` role or be a bot moderator to do that.");
                return;
            }
            CoreClass.economy.gov.setSpending(spending);
            Context.Channel.SendMessageAsync($"Get government spending to ${spending}!");
        }
        [Command("manual-updateDB")]
        [Summary("Update the back-end database of the bot. Anyone can do this but it will be restricted if you spam it.")]
        public async Task manualMongoUpdate() {
            CoreClass.updateDB();
            Context.Channel.SendMessageAsync($"Updated the database!");
        }
        [Command("manual-update")]        
        [Summary("Make the equivalent of a day pass in terms of the economy")]
        public async Task manualUpdate()
        {
            if (Context.User.Id != 374280713387900938) {
                Context.Channel.SendMessageAsync("Only byte gets to do this, sorry mate.");
                return;
            }
            CoreClass.manualUpdate();
            Context.Channel.SendMessageAsync($"One day has passed in the economy!");
        }
        [Command("add-money treasury")]
        [Summary("Add money to the treasury")]
        [RequireContext(ContextType.Guild)]
        public async Task addTreasuryMoney(double amount) {
            if (double.IsNaN(amount) || double.IsInfinity(amount))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            SocketGuildUser gU = Context.User as SocketGuildUser;
            if (!gU.Roles.Contains(Context.Guild.GetRole(774802078391992330)) || !CoreClass.economy.botMods.Contains(Context.User.Id)) {
                Context.Channel.SendMessageAsync("You need the `Secretary of Treasury` role or be a bot moderator to do that.");
                return;
            }
            CoreClass.economy.gov.balance += amount;
            Context.Channel.SendMessageAsync($"Removed {amount} from the treasury");
        }
        [Command("remove-money treasury")]
        [RequireContext(ContextType.Guild)]
        [Summary("Remove money from the treasury")]
        public async Task removeTreasuryMoney(double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            SocketGuildUser gU = Context.User as SocketGuildUser;
            if (!gU.Roles.Contains(Context.Guild.GetRole(774802078391992330)) || !CoreClass.economy.botMods.Contains(Context.User.Id))
            {
                Context.Channel.SendMessageAsync("You need the `Secretary of Treasury` role or be a bot moderator to do that.");
                return;
            }
            CoreClass.economy.gov.balance -= amount;
            Context.Channel.SendMessageAsync($"Removed {amount} from the treasury");
        }
        [Command("add-money user")]
        [Summary("Find money from thin air and add it to a user")]
        public async Task addMoney(SocketUser user, double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            if (!CoreClass.economy.botMods.Contains(Context.User.Id)) {
                Context.Channel.SendMessageAsync("You don't have permission to use this command.");
                return;
            }
            Individual i = CoreClass.economy.getUser(user.Id);
            if (i == null) { return; }
            i.balance += amount;
            CoreClass.economy.updateUser(i);
            Context.Channel.SendMessageAsync($"Added ${amount} to {user.Username}#{user.Discriminator}");
        }
        [Command("remove-money user")]
        [Summary("Take money from a user and throw it into a large pit in the middle of New Mexico made on March 8, 1982.")]
        public async Task removeMoney(SocketUser user, double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            if (!CoreClass.economy.botMods.Contains(Context.User.Id))
            {
                Context.Channel.SendMessageAsync("You don't have permission to use this command.");
                return;
            }
            Individual i = CoreClass.economy.getUser(user.Id);
            if (i == null) { return; }
            i.balance -= amount;
            CoreClass.economy.updateUser(i);
            Context.Channel.SendMessageAsync($"Removed {amount} from {user.Username}#{user.Discriminator}");
        }
        [Command("add-money company")]
        [Summary("Engage in demonic rituals to summon money and give it to a company.")]
        public async Task addCompanyMoney([Summary("The user that owns the company you want to add money to")] SocketUser user, double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            if (!CoreClass.economy.botMods.Contains(Context.User.Id))
            {
                Context.Channel.SendMessageAsync("You don't have permission to use this command.");
                return;
            }
            Company i = CoreClass.economy.getCompany(c => c.orgOwner == user.Id);
            if (i == null) { return; }
            i.balance += amount;
            CoreClass.economy.updateCompany(i);
            Context.Channel.SendMessageAsync($"Added {amount} to {i.name}");
        }
        [Command("remove-money company")]
        [Summary("Take money from a company and dump it in the Indian Ocean")]
        public async Task removeCompanyMoney([Summary("The user that owns the company you want to remove money from")] SocketUser user, double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            if (!CoreClass.economy.botMods.Contains(Context.User.Id))
            {
                Context.Channel.SendMessageAsync("You don't have permission to use this command.");
                return;
            }
            Company i = CoreClass.economy.getCompany(c => c.orgOwner == user.Id);
            if (i == null) { return; }
            i.balance -= amount;
            CoreClass.economy.updateCompany(i);
            Context.Channel.SendMessageAsync($"Removed {amount} from {i.name}");
        }
        [Command("set-role-income")]
        [Summary("Set the daily income of a role")]
        public async Task setRoleIncome(SocketRole r, double d) {
            if (double.IsNaN(d) || double.IsInfinity(d))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            SocketGuildUser gU = Context.User as SocketGuildUser;
            if (!gU.Roles.Contains(Context.Guild.GetRole(774802078391992330)) && !CoreClass.economy.botMods.Contains(Context.User.Id))
            {
                Context.Channel.SendMessageAsync("You need the `Secretary of Treasury` role for that or need to be added as a bot moderator.");
                return;
            }
            if (CoreClass.economy.roleIncomes.ContainsKey(r.Id))
            {
                CoreClass.economy.roleIncomes[r.Id] = d;
            }
            else {
                CoreClass.economy.roleIncomes.Add(r.Id, d);
            }
            Context.Channel.SendMessageAsync($"Set the income for {r.Name} to ${d}!");
        }
        [Command("role-incomes")]
        [Alias("get-role-incomes")]
        public async Task getRoleIncomes() {
            if (CoreClass.economy.roleIncomes.Count == 0) {
                Context.Channel.SendMessageAsync("No role incomes are set currently");
                return;
            }
            string output = ">>> ";
            for (int i = 0; i < CoreClass.economy.roleIncomes.Count; i++) {
                SocketRole r = Context.Guild.GetRole(CoreClass.economy.roleIncomes.Keys.ElementAt(i));
                if (r == null) { continue; }
                output += $"{r.Name} | ${CoreClass.economy.roleIncomes.ElementAt(i).Value} every 2 hours\n";
            }
            Context.Channel.SendMessageAsync(output);
         }
        [Command("deposit-all")]
        [Alias("dep all", "dep-all", "deposit all")]
        [Summary("Deposit all cash on hand to the bank.")]
        public async Task depositAll() {
            Individual i = CoreClass.economy.getUser(Context.User.Id);
            i.balance += i.cashBalance;
            i.cashBalance = 0;
            CoreClass.economy.updateUser(i);
            Context.Channel.SendMessageAsync("Deposited all cash to the bank!");
        }
        [Command("deposit")]
        [Alias("dep")]
        [Summary("Deposit some amount of money to the bank.")]
        public async Task deposit(double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            Individual i = CoreClass.economy.getUser(Context.User.Id);
            if (i.cashBalance < d)
            {
                Context.Channel.SendMessageAsync("You don't have that much cash!");
                return;
            }
            else {
                i.cashBalance -= d;
                i.balance += d;
                CoreClass.economy.updateUser(i);
            }
            Context.Channel.SendMessageAsync($"Deposited ${d} to the bank!");
        }
        [Command("withdraw-i")]
        [Alias("with-i")]
        [Summary("Withdraw money from the bank")]
        public async Task withdraw(double d) {
            if (double.IsNaN(d) || double.IsInfinity(d))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            if (d < 0)
            {
                await Context.Channel.SendMessageAsync("You can't take out a negative amount of money");
                return;
            }
            Individual i = CoreClass.economy.getUser(Context.User.Id);
            if (i.balance < d)
            {
                Context.Channel.SendMessageAsync("You don't have that much money in the bank!");
                return;
            }
            else
            {
                i.cashBalance += d;
                i.balance -= d;
                CoreClass.economy.updateUser(i);
            }
            Context.Channel.SendMessageAsync($"Withdrew ${d} from the bank!");
        }
        [Command("withdraw-all-i")]
        [Alias("with all-i", "with-all-i", "withdraw all-i")]
        [Summary("Withdraw all money from the bank")]
        public async Task withdrawAll()
        {
            Individual i = CoreClass.economy.getUser(Context.User.Id);
            i.cashBalance += i.balance;
            i.balance = 0;
            CoreClass.economy.updateUser(i);
            Context.Channel.SendMessageAsync($"Withdrew all money from the bank!");
        }
        [Command("pay")]
        [Summary("Give money to someone.")]
        public async Task pay(SocketUser user, double amount) {
            if (double.IsNaN(amount) || double.IsInfinity(amount))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            if (user.IsBot) {
                Context.Channel.SendMessageAsync("You can't pay a bot, they don't have rights!");
                return;
            }
            if (amount < 0) {
                Context.Channel.SendMessageAsync("Well that's just plain rude. `(negative investment)`");
                return;
            }
            Individual conte = CoreClass.economy.getUser(Context.User.Id);
            Individual i = CoreClass.economy.getUser(user.Id);
            if (conte.cashBalance < amount) {
                Context.Channel.SendMessageAsync("You don't have enough cash on hand for that!");
                return;
            }
            i.cashBalance += amount;
            conte.cashBalance -= amount;
            CoreClass.economy.updateUser(i);
            CoreClass.economy.updateUser(conte);
            Context.Channel.SendMessageAsync($"Transferred ${amount} to {user.Username}#{user.Discriminator}!");
        }
        [Command("jobs")]
        [Summary("List the jobs that the targeted user has")]
        public async Task jobs(SocketUser user = null)
        {
            Individual i;
            if (user == null)
            {
                i = CoreClass.economy.getUser(Context.User.Id);
                user = Context.User;
            }
            else {
                i = CoreClass.economy.getUser(user.Id);
            }
            
            if (i.jobIDs == null || Array.TrueForAll<ulong>(i.jobIDs, j => j == 0))
            {
                    await Context.Channel.SendMessageAsync($"{user.Mention} is unemployed.");
            }
            else {
                string output = $"{user.Mention} has the following job(s):\n>>> ";
                foreach (ulong job in i.jobIDs) {
                    Company c = CoreClass.economy.getCompany(g => g.ID == job);
                    if (c == null) { continue; }
                    output += $"{c.name} (ID: {c.ID})\n";                   
                }
                await Context.Channel.SendMessageAsync(output);
            }
        }
        [Command("quit")]
        [Alias("leave")]
        [Summary("Leave a company.")]
        public async Task quit(ulong job) {
            Company c = CoreClass.economy.getCompany(g => g.ID == job);
            if (c == null) {
                Context.Channel.SendMessageAsync("This company doesn't exist.");
                return;
            }
            if (c.employeeWages.ContainsKey(Context.User.Id))
            {
                Individual employ = CoreClass.economy.getUser(Context.User.Id);
                if (!employ.leaveCompany(c.ID))
                {
                    await Context.Channel.SendMessageAsync($"{Context.User.Username}#{Context.User.Discriminator} wasn't there to begin with!");
                    return;
                }
                c.removeEmployee(Context.User.Id);
                Context.Channel.SendMessageAsync($"{c.name} won't be the same without you, {Context.User.Mention}");
            }
            else {
                Context.Channel.SendMessageAsync($"You're not even employed at {c.name}! How do you intend to leave?!");
            }
        }
        [Command("profit-projections")]
        [Alias("project-profit", "project-income", "project-incomes", "income-projections", "profit-projection", "income-projection")]
        [Summary("Estimate the amount of money that your company will make in the next update, assuming nothing changes until the next update.")]
        public async Task profitProjection(ulong company_id) {
            Individual hirer = CoreClass.economy.getUser(Context.User.Id);
            Company c;
            try
            {
                c = CoreClass.economy.getCompany(c => c.ID == company_id && c.orgOwner == Context.User.Id);
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync("There was a problem finding your Company. If you don't *own* a Company, that would be why.");
                return;
            }
            if (c == null)
            {
                await Context.Channel.SendMessageAsync("You need a company before you can make profits!");
                return;
            }
            else
            {
                double[] projection = c.getIncomeProjection();
                await Context.Channel.SendMessageAsync($"**Profit projections for {c.name}:**\n" +
                    $">>> Worst-case change: {Math.Round(projection[0], 2)}\n" +
                    $"Low-end income: {Math.Round(projection[1], 2)}\n" +
                    $"Expected income: {Math.Round(projection[2], 2)}\n" +
                    $"Best-case income: {Math.Round(projection[3], 2)}");
                await Context.Channel.SendMessageAsync("You can increase these figures by getting your employees to work or by advertising.");
            }
        }

        [Command("die")]
        [Alias("suicide")]
        [Summary("Tells the bot to die")]
        public async Task die()
        {
            await Context.Channel.SendMessageAsync("nah");
        }

        [Command("add-moderator")]
        public async Task addMod(SocketUser user) {
            if (!CoreClass.economy.botMods.Contains(Context.User.Id))
            {
                Context.Channel.SendMessageAsync("You don't have permission to use this command.");
                return;
            }
            CoreClass.economy.botMods.Add(user.Id);
            Context.Channel.SendMessageAsync($"Added {user.Mention} to the moderator list.");
        }

        [Command("remove-moderator")]
        public async Task removeMod(SocketUser user)
        {
            if (!CoreClass.economy.botMods.Contains(Context.User.Id))
            {
                Context.Channel.SendMessageAsync("You don't have permission to use this command.");
                return;
            }
            if (CoreClass.economy.botMods.Remove(user.Id))
            {
                Context.Channel.SendMessageAsync($"Removed {user.Mention} from the moderator list.");
            }
            else {
                Context.Channel.SendMessageAsync($"{user.Mention} wasn't a moderator to begin with but aight");
            }
            
        }
        [Command("pay-company")]
        [Summary("Give money to a company")]
        public async Task invest([Summary("ID of the company you want to invest in.")] ulong company_id, double amount)
        {
            if (amount < 0)
            {
                Context.Channel.SendMessageAsync("Well that's just plain rude. `(negative investment)`");
                return;
            }
            Company c = null;
            try { c = CoreClass.economy.getCompany(i => i.ID == company_id); }
            catch
            {
                Context.Channel.SendMessageAsync("There was an issue finding this company. If this company doesn't exist, that would be why.");
                return;
            }
            if (c == null)
            {
                Context.Channel.SendMessageAsync("This company doesn't exist!");
                return;
            }
            Individual i = CoreClass.economy.getUser(Context.User.Id);
            if (i.cashBalance < amount)
            {
                Context.Channel.SendMessageAsync("You don't have enough cash for that! What are you, poor? Disgusting.");
                return;
            }
            i.cashBalance -= amount;
            c.balance += amount;
            CoreClass.economy.updateUser(i);
            CoreClass.economy.updateCompany(c);
            await Context.Channel.SendMessageAsync($"Gave ${amount} to {c.name}. Whether this was a good idea is yet to be seen.");
        }

        [Command("withdraw-c")]
        [Alias("with-c")]
        [Summary("Withdraw money from your company")]
        public async Task withdrawCompany(double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            if (amount < 0) {
                await Context.Channel.SendMessageAsync("You can't take out a negative amount of money");
                return;
            }
            Company c = null;
            try { c = CoreClass.economy.getCompany(i => i.orgOwner == Context.User.Id); }
            catch
            {
                Context.Channel.SendMessageAsync("There was an issue finding your company. If you don't have a company, that would be why.");
                return;
            }
            if (c == null)
            {
                Context.Channel.SendMessageAsync("You don't have a company.");
                return;
            }
            Individual i = CoreClass.economy.getUser(Context.User.Id);
            if (c.balance < amount)
            {
                Context.Channel.SendMessageAsync("The company doesn't have that much money.");
                return;
            }
            i.cashBalance += amount;
            c.balance -= amount;
            CoreClass.economy.updateUser(i);
            CoreClass.economy.updateCompany(c);
            Context.Channel.SendMessageAsync($"Withdrew {amount} from {c.name}!");
        }

        [Command("withdraw-all-i")]
        [Alias("with-all-i", "with-all-i", "withdraw-all-i")]
        [Summary("Withdraw all money from your company")]
        public async Task withdrawCompany()
        {
            Company c = null;
            try { c = CoreClass.economy.getCompany(i => i.orgOwner == Context.User.Id); }
            catch
            {
                Context.Channel.SendMessageAsync("There was an issue finding your company. If you don't have a company, that would be why.");
                return;
            }
            if (c == null)
            {
                Context.Channel.SendMessageAsync("You don't have a company.");
                return;
            }
            Individual i = CoreClass.economy.getUser(Context.User.Id);
            i.cashBalance += c.balance;
            c.balance = 0;
            CoreClass.economy.updateUser(i);
            CoreClass.economy.updateCompany(c);
        }

        [Command("buy-i")]
        [Summary("Buy a product as an individual. For any products with a space in their name, make sure to put the product name in quotes.")]
        public async Task buyInd([Summary("Name of the product that you want to buy.")] string product_name, int count = 1) {            
            Individual i = CoreClass.economy.getUser(Context.User.Id);
            string input = product_name.ToLower();
            input = input.Trim();
            if (i.buy(input, i.getBuyable(), count))
            {
                Context.Channel.SendMessageAsync($"Bought {count} {input}!");
            }
            else {
                Context.Channel.SendMessageAsync($"Unable to buy {input}x{count}: You either lack the money for it, or it doesn't exist.");
            }
        }
        [Command("buy-c")]
        [Summary("Buy a product as a company")]
        public async Task buyComp([Summary("Name of the product you want to buy.")] string product_name, ulong company_id, int count = 1)
        {
            Company c = null;
            try { c = CoreClass.economy.getCompany(c => c.ID == company_id && c.orgOwner == Context.User.Id); }
            catch { Context.Channel.SendMessageAsync("There was an an issue finding your company. If you don't have one, that would be why."); return; }
            if (c == null) {
                Context.Channel.SendMessageAsync("You don't have a company to buy products with!");
                return;
            }
            string input = product_name.ToLower();
            input = input.Trim();
            if (c.buy(input, count: count))
            {
                Context.Channel.SendMessageAsync($"Bought {count} {input}!");
            }
            else
            {
                Context.Channel.SendMessageAsync($"Unable to buy {input}x{count}: You either lack the money for it, or it doesn't exist.");
            }
        }
        [Command("buyable")]
        [Summary("Get a list of products you can buy and their prices")]
        public async Task buyables() {

            Dictionary<string, double> individualBuyable = new Individual().getBuyable();
            Dictionary<string, double> companyBuyable = new Company().getBuyable();
            //individualBuyable.Select(kv => $"{kv.Key} | ${kv.Value}");
            EmbedBuilder eb = new EmbedBuilder();
            string ind = "";
            string com = "";
            for (int i = 0; i < individualBuyable.Count(); i++) { 
                ind += $"{individualBuyable.ElementAt(i).Key} | ${individualBuyable.ElementAt(i).Value}\n";
            }
            for (int i = 0; i < companyBuyable.Count(); i++)
            {
                com += $"{companyBuyable.ElementAt(i).Key} | ${companyBuyable.ElementAt(i).Value}\n";
            }
            eb.WithTitle($"Available products and prices:");
            eb.AddField("Individual's Products (-i)", ind);
            eb.AddField("Company Products (-c)", com);
            eb.WithFooter($"Buy these products with `{CoreClass.DEFAULT_PREFIX}buy-i <product name>` or `{CoreClass.DEFAULT_PREFIX}buy-c <product name>` for individuals and companies respectively.");
            eb.WithColor(Color.Blue);
            await Context.Channel.SendMessageAsync(embed: eb.Build());
        }

        [Command("buy-company")]
        [Summary("Offer to buy a company for its net worth.")]
        public async Task buyCompany([Summary("ID of the company you want to buy.")]  ulong id)
        {
            Individual hirer = CoreClass.economy.getUser(Context.User.Id);
            Company c;
            try
            {
                c = CoreClass.economy.getCompany(c => c.ID == id);
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync("There was a problem finding this Company. If no company exists with this ID, that would be why.");
                return;
            }
            if (c == null)
            {
                await Context.Channel.SendMessageAsync($"No company found with id {id}");
                return;
            }
            else
            {
                if (hirer.cashBalance < c.balance)
                {
                    Context.Channel.SendMessageAsync("You don't have enough cash for that!");
                    return;
                }
                MessageResponseThread m = new MessageResponseThread(c.orgOwner, Context.Channel.Id, new Regex("(yes|nope|yep|no|y|n)", RegexOptions.IgnoreCase));
                async void handleEventAsync(object sender, EventArgs e)
                {
                    MessageResponseEventArgs me = e as MessageResponseEventArgs;
                    if (me.message == "yes" || me.message == "y" || me.message == "yep")
                    {
                        Individual i = CoreClass.economy.getUser(c.orgOwner);
                        i.cashBalance += c.balance;
                        hirer.cashBalance -= c.balance;                       
                        c.orgOwner = Context.User.Id;
                        i.ownedStock.Where(s => s.companyBought == c.ID).ToList().ForEach(s => s.sell(hirer.ID, 0));
                        Context.Channel.SendMessageAsync($"{Context.User.Mention} has bought {c.name}.");
                        return;
                    }
                    else
                    {
                        Context.Channel.SendMessageAsync($"Sorry {Context.User.Mention}, seems that they aren't interested or need a better deal.");
                        return;
                    }
                }

                m.ResponseReceived += handleEventAsync;
                CoreClass.responseThreads.Add(m);
                Context.Channel.SendMessageAsync($"{Context.User.Mention} has offered to buy {c.name}, currently owned by <@{c.orgOwner}>, for ${c.balance}! Do you accept (yes/no)?");
            }
        }
        [Command("buy-company")]
        [Summary("Offer to buy a company for a specific price **Does not work if you already have a company, for now.**")]
        public async Task buyCompany([Summary("ID of the company you want to buy.")]  ulong id, double price) {
            if (double.IsNaN(price) || double.IsInfinity(price))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            Individual hirer = CoreClass.economy.getUser(Context.User.Id);
            Company c;
            try
            {
                c = CoreClass.economy.getCompany(c => c.ID == id);
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync("There was a problem finding this Company. If no company exists with this ID, that would be why.");
                return;
            }
            if (c == null)
            {
                await Context.Channel.SendMessageAsync($"No company found with id {id}");
                return;
            }
            else
            {
                if (hirer.cashBalance < price) {
                    Context.Channel.SendMessageAsync("You don't have enough cash for that!");
                    return;
                }

                MessageResponseThread m = new MessageResponseThread(c.orgOwner, Context.Channel.Id, new Regex("(yes|nope|yep|no|y|n)", RegexOptions.IgnoreCase));
                async void handleEventAsync(object sender, EventArgs e)
                {
                    MessageResponseEventArgs me = e as MessageResponseEventArgs;
                    if (me.message == "yes" || me.message == "y" || me.message == "yep")
                    {
                        Individual i = CoreClass.economy.getUser(c.orgOwner);
                        i.cashBalance += price;
                        hirer.cashBalance -= price;                        
                        c.orgOwner = Context.User.Id;
                        i.ownedStock.Where(s => s.companyBought == c.ID).ToList().ForEach(s => s.sell(hirer.ID, 0));
                        Context.Channel.SendMessageAsync($"{Context.User.Mention} has bought {c.name} for {price}.");
                        return;
                    }
                    else
                    {
                        Context.Channel.SendMessageAsync($"Sorry {Context.User.Mention}, seems that they aren't interested or need a better deal. ");
                        return;
                    }
                }

                m.ResponseReceived += handleEventAsync;
                CoreClass.responseThreads.Add(m);
                Context.Channel.SendMessageAsync($"{Context.User.Mention} has offered to buy {c.name}, currently owned by <@{c.orgOwner}>, for ${price}! Do you accept (yes/no)?");
            }
        }
        [Command("merge")]
        [Summary("Merge two companies into one company under the user executing this command.")]
        public async Task mergeComp(ulong your_company_id, [Summary("ID of the company you want to merge with.")] ulong id) {

            Individual hirer = CoreClass.economy.getUser(Context.User.Id);
            Company c1;
            Company c2;
            try
            {
                if (CoreClass.economy.getCompany(c => c.orgOwner == Context.User.Id) == null)
                {
                    Context.Channel.SendMessageAsync($"You don't have a company to merge into! If you're looking to buy a company, you'll want to do `{CoreClass.DEFAULT_PREFIX}buy-company <company id> [price]`.");
                    return;
                }
                c1 = CoreClass.economy.getCompany(c => c.ID == id);
                c2 = CoreClass.economy.getCompany(c => c.ID == your_company_id && c.orgOwner == Context.User.Id);
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync("There was a problem finding this Company. If no company exists with this ID, that would be why.");
                return;
            }
            if (c1 == null)
            {
                await Context.Channel.SendMessageAsync($"No company found with id {id}");
                return;
            }
            if (c2 == null) {
                await Context.Channel.SendMessageAsync($"You don't have a company!");
            }
            else
            {
                MessageResponseThread m = new MessageResponseThread(c1.orgOwner, Context.Channel.Id, new Regex("(yes|nope|yep|no|y|n|Yes|Yep|Y|YES|YEP|Y)", RegexOptions.IgnoreCase));
                async void handleEventAsync(object sender, EventArgs e)
                {
                    MessageResponseEventArgs me = e as MessageResponseEventArgs;
                    if (me.message.ToLower() == "yes" || me.message.ToLower() == "y" || me.message.ToLower() == "yep")
                    {
                        foreach (ulong employee in c1.employeeWages.Keys) {
                            if (c2.employeeWages.ContainsKey(employee))
                            {
                                c2.employeeWages[employee] += c1.employeeWages[employee];
                            }
                            else {
                                c2.addEmployee(employee, c1.employeeWages[employee]);
                            }
                        }
                        c2.popularity = (c1.popularity + c2.popularity) / 2;
                        c2.members.AddRange(c2.members);
                        c2.balance += c1.balance; 

                        //Owner of merging company gives half of their stock (i.e. ownership) to user
                        Individual i = CoreClass.economy.getUser(c2.orgOwner);
                        i.ownedStock.Where(s => s.companyBought == c2.ID).ToList().ForEach(s => s.sell(hirer.ID, 0, s.amount / 2));

                        CoreClass.economy.updateCompany(c2);
                        CoreClass.economy.removeCompany(c1);
                        return;
                    }
                    else
                    {
                        Context.Channel.SendMessageAsync($"Sorry {Context.User.Mention}, seems that they aren't interested or need a better deal. ");
                        return;
                    }
                }

                m.ResponseReceived += handleEventAsync;
                CoreClass.responseThreads.Add(m);
                Context.Channel.SendMessageAsync($"{Context.User.Mention} has offered to merge with {c1.name}, currently owned by <@{c1.orgOwner}>! Do you accept? (yes/no)");
            }
        }
        [Command("toggle-debug")]
        [Summary("For byte only. Disables the bot for all other users while active.")]
        public async Task debugToggle() {
            if (Context.User.Id != 374280713387900938) {
                await Context.Channel.SendMessageAsync("No.");
            }
            CoreClass.debug = !CoreClass.debug;
            await Context.Channel.SendMessageAsync($"Debug mode: {CoreClass.debug}");
        }
        [Command("user-dashboard")]
        [Summary("Gives an overview of all relevant info for you.")]
        public async Task dashboard() {
            EmbedBuilder embed = new EmbedBuilder();

            EmbedFieldBuilder user = new EmbedFieldBuilder();
            Individual i = CoreClass.economy.getUser(Context.User.Id);
            user.Name = "Balance";
            user.Value = $"**ID:** `{Context.User.Id}`\n**Balance:** ${i.balance} (Bank), ${i.cashBalance} (Cash)\n";
            embed.AddField(user);

            EmbedFieldBuilder jobs = new EmbedFieldBuilder();
            jobs.Name = "Jobs";
            string jobValue = "";
            for (int inc = 0; inc < i.jobIDs.Length; inc++) {
                Company c1 = CoreClass.economy.getCompany(c => c.ID == i.jobIDs[inc]);
                if (c1 == null || !c1.employeeWages.ContainsKey(Context.User.Id))
                {
                    i.jobIDs[inc] = 0;
                    jobValue += $"**Slot {inc + 1}:** [Empty]\n";
                }
                else {
                    jobValue += $"**Slot {inc+1}:** {c1.name} (ID: {c1.ID}) | Wage: ${c1.employeeWages[Context.User.Id]}\n";
                }
            }
            jobs.Value = jobValue;
            embed.AddField(jobs);

            EmbedFieldBuilder companies = new EmbedFieldBuilder();
            List<Company> ownedComps = CoreClass.economy.companies.Where(c => c.orgOwner == Context.User.Id).ToList();
            companies.Name = "Your businesses";
            if (ownedComps.Count > 0)
            {
                string companiesValue = "";
                foreach (Company c in ownedComps) {
                    companiesValue += $"{c.name} (ID: {c.ID})\n";
                }
                companies.Value = companiesValue;
            }
            else {
                companies.Value = "You don't currently own any businesses.";
            }
            embed.AddField(companies);

            embed.WithFooter("I'm making this at like 12:30AM and I can't be bothered to put sometihng funny here.");
            embed.WithTitle(Context.User.Username + "#" + Context.User.Discriminator + "'s Dashboard");
            embed.Color = Color.Blue;
            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }

        [Command("company-dashboard")]
        [Summary("Gives a dashboard with relevant information about a specific company")]
        public async Task companyDashboard(ulong company_id)
        {
            if (!CoreClass.economy.companies.Exists(c => c.ID == company_id))
            {
                Context.Channel.SendMessageAsync($"That company does not exist.");
                return;
            }            
            EmbedBuilder embed = new EmbedBuilder();
            Company company = CoreClass.economy.getCompany(c => c.ID == company_id);

            if (company.orgOwner != Context.User.Id) {
                Context.Channel.SendMessageAsync("You don't own this company!");
                return;
            }

            EmbedFieldBuilder com = new EmbedFieldBuilder();
            com.Name = $"Your Company | {company.name} (ID: {company.ID})";
            com.Value = $"**Expected Gross Income:** ${Math.Round(company.getIncomeProjection()[2], 2)}\n" +
                $"**Total Employee Wages:** ${company.employeeWages.Values.Sum()}\n" +
                $"**Balance:** ${company.balance}\n\n" +
                $"**Popularity:** {company.popularity}\n" +
                $"**Total Stock:** {company.productStock.Select(t => t.Value).Sum()}\n" +
                $"**Stock Price:** {company.stock_price}";
            embed.AddField(com);  
            
            embed.WithTitle($"{company.name} Dashboard");
            embed.Color = Color.Blue;
            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }
        [Command("stock-dashboard")]
        [Summary("Get summarized info of what stocks you own")]
        public async Task stockDashboard()
        {
            EmbedBuilder embed = new EmbedBuilder();

            EmbedFieldBuilder user = new EmbedFieldBuilder();
            Individual i = CoreClass.economy.getUser(Context.User.Id);
            user.Name = "Balance";
            user.Value = $"**ID:** `{Context.User.Id}`\n**Balance:** ${i.balance} (Bank), ${i.cashBalance} (Cash)\n";
            embed.AddField(user);

            EmbedFieldBuilder stockField = new EmbedFieldBuilder();
            stockField.Name = "Stocks";
            if (i.ownedStock != null && i.ownedStock.Count > 0)
            {
                string val = "";
                foreach (Stock s in i.ownedStock) {
                    if (!CoreClass.economy.companies.Exists(c => c.ID == s.companyBought)) {
                        continue;
                    }
                    
                    Company c = CoreClass.economy.getCompany(c => c.ID == s.companyBought);
                    if (c == null) {
                        continue;
                    }
                    val += $"{c.name} (ID: {c.ID}) | {s.amount} shares, {decimal.Round((decimal)Company.sharesToPercentage(s.amount) * 100, 4)}% ownership\n";
                }
            }
            else {
                stockField.Value = "You don't have a share of any company currently.";
            }
            embed.AddField(stockField);

            embed.WithFooter("Line go up means world more gooder");
            embed.WithTitle(Context.User.Username + "#" + Context.User.Discriminator + "'s Stocks");
            embed.Color = Color.Blue;
            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }

        [Command("quick-start")]
        [Summary("Gives a brief explanation on how to use the bot")]
        public async Task quickStart() {
            EmbedBuilder eb = new EmbedBuilder();
            eb.Title = "BreadLine Quickstart Guide";
            eb.Description = "The economy runs on a turn-based system that turns over every 2 hours.\n" +
                "You can apply to a job with `$join`, and employers can offer you a job with `$hire`\n" +
                "You can work at a job and get your wage with `$work`\n\n" +
                "You can start a company with `$start-company <name>`\n" +
                "As a company, you can buy _advertising campaigns_ and _astroturfing campaigns_, which affect your company's **popularity**\n" +
                "** - advertising campaigns** increase the popularity of your company, getting your name out there to simulated customers, as well as determining your position in job listings _(which you can get with `$job-list`)_\n" +
                "** - astroturfing campaigns** work to cement and maintain your popularity while minorly increasing it. The amount of running campaigns decreases over time however, so some effort is required to maintain it.\n" +
                "Workers produce products for your company when they do `$work`. Remember, it doesn't matter how popular you are if you don't have anything to sell, so making sure your workers are actually producing for you is important.\n" +
                "You can set the price of products you're selling with `$set-price <product name> <price>`";
            eb.WithFooter("Use `$help` to get the syntax for the various commands referenced, as well as to see te various other available commands");
            await Context.Channel.SendMessageAsync(embed: eb.Build());
        }
        [Command("set-price")]
        [Alias("add-product", "set-product-price", "update-price")]
        [Summary("Add a product or set the price for an existing product.")]
        public async Task setProductPrice(ulong company_id, string product_name, double price) {
            if (double.IsNaN(price) || double.IsInfinity(price)) {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            Company c;
            try
            {
                c = CoreClass.economy.getCompany(c => c.ID == company_id && c.orgOwner == Context.User.Id);
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync("There was a problem finding your Company. If you don't *own* a Company, that would be why.");
                return;
            }
            if (c == null)
            {
                await Context.Channel.SendMessageAsync("You need a company before you can set products!");
                return;
            }
            else
            {
                if (c.products.ContainsKey(product_name))
                {
                    c.products[product_name] = price;
                }
                else {
                    //There is no case where the product doesnt exist in products but it has an entry in productStock
                    c.products.Add(product_name, price);
                    c.productStock.Add(product_name, 0);
                }
                Context.Channel.SendMessageAsync($"Set the price of {product_name} to ${price}");
                CoreClass.economy.updateCompany(c);
            }
        }
        [Command("set-wage")]
        [Summary("Set the wage of one of your workers")]
        public async Task setWage(ulong company_id, SocketUser worker, double newWage) {
            if (double.IsNaN(newWage) || double.IsInfinity(newWage))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            Individual hirer = CoreClass.economy.getUser(Context.User.Id);
            Company c;
            try
            {
                c = CoreClass.economy.getCompany(c => c.ID == company_id && c.orgOwner == Context.User.Id);
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync("There was a problem finding your Company. If you don't *own* a Company, that would be why.");
                return;
            }
            if (c == null)
            {
                await Context.Channel.SendMessageAsync("You need a company before you can change people's wages!");
                return;
            }
            else
            {
                if (!c.employeeWages.ContainsKey(worker.Id)) {
                    await Context.Channel.SendMessageAsync("That person doesn't work at your company.");
                    return;
                }
                c.employeeWages[worker.Id] = newWage;
                CoreClass.economy.updateCompany(c);
                await Context.Channel.SendMessageAsync($"Set {worker.Mention}'s wage to {newWage}!");
            }
        }
        [Command("irp-time")]
        [Summary("Gives the time in the government roleplay")]
        public async Task irpTime() {

            //DateTime start = new DateTime(2033,1,20);
            //TimeSpan timeSince = DateTime.Now.Subtract(new DateTime(2021, 2, 25));
            //DateTime end = start.AddDays((timeSince.TotalDays / 7) * 365.26);
            //await Context.Channel.SendMessageAsync($"The date irp is: `{end.ToShortDateString()}`");

            EmbedBuilder eb = new EmbedBuilder();
            eb.WithTitle("Irp Time");
            eb.WithFooter("Times are calculated based on the time since Nimitz's Inauguration, since it's a convenient landmark for time.");
            eb.Color = Color.Red;

            EmbedFieldBuilder currentTime = new EmbedFieldBuilder();
            currentTime.Name = "Current Time";
            //IRP Inauguration Date
            DateTime start = new DateTime(2036, 1, 20);
            //IRL Inauguration Date
            DateTime inauguration = new DateTime(2021, 3, 25);
            TimeSpan timeSince = DateTime.Now.Subtract(inauguration);
            //IRP Time
            DateTime end = start.AddDays((timeSince.TotalDays / 7) * 365.26);
            currentTime.Value = $"Irl: {DateTime.Now.ToShortDateString()}\n" +
                $"Irp: {end.ToShortDateString()}";
            eb.AddField(currentTime);

            EmbedFieldBuilder timeToGeneral = new EmbedFieldBuilder();
            timeToGeneral.Name = "Time until General election";
            //Time of election
            DateTime GEtime = new DateTime(start.Year + 4, 11, 3);            
            //Time until election
            TimeSpan irpTimeToGE = GEtime.Subtract(end);
            //Display the funny numbers
            double irlDaysToGE = inauguration.AddMonths(1).Subtract(DateTime.Now).TotalDays;
            timeToGeneral.Value = $"Irp Date: {GEtime.ToShortDateString()}\n" +
                $"Irl Date: {DateTime.Now.AddDays(irlDaysToGE).ToShortDateString()}\n" +
                $"Irp: {Math.Floor(irpTimeToGE.Days / 365.26)} years, {Math.Round(irpTimeToGE.Days % 365.26 / 30)} months (approx)\n" +
                $"Irl: {Math.Round(irlDaysToGE)} days";
            eb.AddField(timeToGeneral);

            EmbedFieldBuilder timeToMidterm = new EmbedFieldBuilder();
            timeToMidterm.Name = "Time until Midterm election";
            //Time of Midterm election
            DateTime Midtime = new DateTime(start.Year + 2, 11, 6);
            //Time to midterm
            TimeSpan irpTimeToMid = Midtime.Subtract(end);
            double irlDaysToMid = inauguration.AddDays(14).Subtract(DateTime.Now).TotalDays;
            timeToMidterm.Value = $"Irp Date: {Midtime.ToShortDateString()}\n" +
                $"Irl Date: {DateTime.Now.AddDays(irlDaysToMid).ToShortDateString()}\n" +
                $"Irp: {Math.Floor(irpTimeToMid.Days / 365.26)} years, {Math.Round(irpTimeToMid.Days % 365.26 / 30)} months (approx)\n" +
                $"Irl: {Math.Round(irlDaysToMid)} days";
            eb.AddField(timeToMidterm);

            await Context.Channel.SendMessageAsync(embed: eb.Build());

        }
        [Command("sell-stock share")]
        [Alias("sell-stock -s", "sell-stock shares", "sell-share", "sell-shares")]
        [Summary("Sell shares of a company to another person.")]
        public async Task sellStock(SocketUser buyer, ulong company_id, double price, double amount) {
            if (double.IsNaN(price) || double.IsInfinity(price))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            if (amount <= 0) {
                Context.Channel.SendMessageAsync("You can't sell 0 or negative stock.");
                return;
            }
            if (price < 0) {
                Context.Channel.SendMessageAsync("You can't sell stock for less than $0.");
                return;
            }
            if (!CoreClass.economy.companies.Exists(c => c.ID == company_id))
            {
                Context.Channel.SendMessageAsync("This company does not exist.");
                return;
            }
            Individual i = CoreClass.economy.getUser(Context.User.Id);
            Company c = CoreClass.economy.getCompany(ci => ci.ID == company_id);            
            Stock stock = i.ownedStock.Find(s => s.companyBought == company_id);

            if (stock == null) {
                Context.Channel.SendMessageAsync("You don't have shares of this company.");
                return;
            }

            MessageResponseThread m = new MessageResponseThread(buyer.Id, Context.Channel.Id, new Regex("(yes|nope|yep|no|y|n)", RegexOptions.IgnoreCase));

            async void handleEventAsync(object sender, EventArgs e)
            {
                MessageResponseEventArgs me = e as MessageResponseEventArgs;
                me.message = me.message.ToLower();
                if (me.message == "yes" || me.message == "y" || me.message == "yep")
                {
                    Stock.sellResult sale = stock.sell(buyer.Id, price, amount);
                    switch (sale)
                    {
                        case Stock.sellResult.SOLD_SUCCESSFULLY:
                            await Context.Channel.SendMessageAsync($"Sold {amount} shares of {c.name} for a total of ${price}!");
                            break;
                        case Stock.sellResult.DENIED_AMOUNT_TOO_LARGE:
                            Context.Channel.SendMessageAsync("Denied: You can't sell more stock than you own!");
                            return;
                        case Stock.sellResult.DENIED_LACK_OF_FUNDS:
                            Context.Channel.SendMessageAsync("Denied: The buyer doesn't have enough money for this transaction.");
                            return;
                        case Stock.sellResult.ERROR_INVALID_BUYER:
                            Context.Channel.SendMessageAsync("Error: Invalid buyer");
                            return;
                        case Stock.sellResult.ERROR_INVALID_COMPANY:
                            Context.Channel.SendMessageAsync("Error: Invalid Company");
                            return;
                        case Stock.sellResult.ERROR_INVALID_OWNER:
                            Context.Channel.SendMessageAsync("Error: Invalid stock owner");
                            return;
                    }
                }
                else
                {
                    Context.Channel.SendMessageAsync($"{buyer.Username}#{buyer.Discriminator} has declined your offer, {Context.User.Mention}.");
                    return;
                }
            }

            m.ResponseReceived += handleEventAsync;
            CoreClass.responseThreads.Add(m);
            Context.Channel.SendMessageAsync($"{Context.User.Mention} has offered you {amount} shares of {c.name} for ${price}! Do you accept? (yes/no)");            
        }
        [Command("sell-stock %")]
        [Alias("sell-stock percent", "sell-stock percentage", "sell-stock -p")]
        [Summary("Sell a percentage of a company to another person.")]
        public async Task sellStockPerc(SocketUser buyer, ulong company_id, double price, double percentage) {
            sellStock(buyer, company_id, price, Company.percentageToShares(percentage / 100));
        }

        [Command("buy-stock share")]
        [Alias("buy-stock -s", "buy-stock shares", "buy-share", "buy-shares")]
        [Summary("Buy shares of a company from another person.")]
        public async Task buyStock(SocketUser seller, ulong company_id, double price, double amount)
        {
            if (double.IsNaN(price) || double.IsInfinity(price))
            {
                Context.Channel.SendMessageAsync("Invalid amount; NaN or Infinity is not permitted.");
                return;
            }
            if (amount <= 0)
            {
                Context.Channel.SendMessageAsync("You can't buy 0 or negative stock.");
                return;
            }
            if (price < 0)
            {
                Context.Channel.SendMessageAsync("You can't buy stock for less than $0.");
                return;
            }
            if (!CoreClass.economy.companies.Exists(c => c.ID == company_id))
            {
                Context.Channel.SendMessageAsync("This company does not exist.");
                return;
            }
            Individual i = CoreClass.economy.getUser(seller.Id);
            Company c = CoreClass.economy.getCompany(ci => ci.ID == company_id);
            Stock stock = i.ownedStock.Find(s => s.companyBought == company_id);

            if (stock == null)
            {
                Context.Channel.SendMessageAsync("The seller doesn't have any shares of this company.");
                return;
            }

            MessageResponseThread m = new MessageResponseThread(seller.Id, Context.Channel.Id, new Regex("(yes|nope|yep|no|y|n)", RegexOptions.IgnoreCase));

            async void handleEventAsync(object sender, EventArgs e)
            {
                MessageResponseEventArgs me = e as MessageResponseEventArgs;
                me.message = me.message.ToLower();
                if (me.message == "yes" || me.message == "y" || me.message == "yep")
                {
                    Stock.sellResult sale = stock.sell(Context.User.Id, price, amount);
                    switch (sale)
                    {
                        case Stock.sellResult.SOLD_SUCCESSFULLY:
                            await Context.Channel.SendMessageAsync($"Bought {amount} ({decimal.Round((decimal)Company.sharesToPercentage(amount) * 100, 4)}%) shares of {c.name} for a total of ${price}!");
                            break;
                        case Stock.sellResult.DENIED_AMOUNT_TOO_LARGE:
                            Context.Channel.SendMessageAsync("Denied: You can't buy more stock than the other person owns!");
                            return;
                        case Stock.sellResult.DENIED_LACK_OF_FUNDS:
                            Context.Channel.SendMessageAsync("Denied: The you don't have enough money for this transaction.");
                            return;
                        case Stock.sellResult.ERROR_INVALID_BUYER:
                            Context.Channel.SendMessageAsync("Error: Invalid buyer");
                            return;
                        case Stock.sellResult.ERROR_INVALID_COMPANY:
                            Context.Channel.SendMessageAsync("Error: Invalid Company");
                            return;
                        case Stock.sellResult.ERROR_INVALID_OWNER:
                            Context.Channel.SendMessageAsync("Error: Invalid stock owner");
                            return;
                    }
                }
                else
                {
                    Context.Channel.SendMessageAsync($"{seller.Username}#{seller.Discriminator} has declined your offer, {Context.User.Mention}.");
                    return;
                }
            }

            m.ResponseReceived += handleEventAsync;
            CoreClass.responseThreads.Add(m);
            Context.Channel.SendMessageAsync($"{Context.User.Mention} has offered to buy {amount} shares ({decimal.Round((decimal)Company.sharesToPercentage(amount) * 100, 4)}% ownership) of {c.name} for ${price}! Do you accept? (yes/no)");
        }

        [Command("buy-stock %")]
        [Alias("buy-stock percent", "buy-stock percentage", "buy-stock -p")]
        [Summary("Buy a percentage of a company from another person.")]
        public async Task buyStockPerc(SocketUser seller, ulong company_id, double price, double percentage)
        {
            buyStock(seller, company_id, price, Company.percentageToShares(percentage / 100));
        }

        [Command("stock-graph")]
        [Alias("stock-graph -e", "full-stock-graph")]
        [Summary("Graphs all stock prices in the economy")]
        public async Task stockGraph() {
            try
            {
                List<Company> eObjects = CoreClass.economy.companies; //All the companies
                List<List<double>> economyObjects = new List<List<double>>(); //All the companies' history
                eObjects.ForEach(t => economyObjects.Add(t.stock_history)); //Actually add the companies' history
                List<double> fullHistory = new List<double>();
                foreach (List<double> eObj in economyObjects)
                {
                    while (fullHistory.Count < eObj.Count)
                    {
                        fullHistory.Add(0.0);
                    }
                    for (int i = eObj.Count - 1; i >= 0; i--)
                    {
                        //eObj.Count - (i + 1)
                        fullHistory[eObj.Count - (i + 1)] += eObj[i];
                    }
                }
                string filename = DateTime.Now.ToBinary().ToString();
                RandUtil.ActivityGraph(filename, fullHistory, Context.Channel);
                await Context.Channel.SendFileAsync(filename + ".png");
            }
            catch (Exception ex)
            {
                SocketUser u = Context.Client.GetUser(374280713387900938);
                await u.SendMessageAsync($"**`{ex.Message}`**");
                await u.SendMessageAsync($"```{ex.StackTrace}```");
                await Context.Channel.SendMessageAsync("There was an error generating this graph. I've DMed byte the details, so he'll be on that when Birnam Wood moves.");

            }
        }
        [Command("stock-graph")]
        [Alias("company-stock-graph", "stock-graph-c", "stock-graph -c")]
        [Summary("Graphs the stock of a specified company")]
        public async Task stockGraph(ulong company_id) {
            if (!CoreClass.economy.companies.Exists(c => c.ID == company_id)) {
                Context.Channel.SendMessageAsync("That comapny doesn't exist.");
                return;
            }
            Company c = CoreClass.economy.getCompany(c => c.ID == company_id);
            try
            {                
                string filename = DateTime.Now.ToBinary().ToString();
                RandUtil.ActivityGraph(filename, c.stock_history, Context.Channel);
                await Context.Channel.SendFileAsync(filename + ".png");
            }
            catch (Exception ex)
            {
                SocketUser u = Context.Client.GetUser(374280713387900938);
                await u.SendMessageAsync($"**`{ex.Message}`**");
                await u.SendMessageAsync($"```{ex.StackTrace}```");
                await Context.Channel.SendMessageAsync("There was an error generating this graph. I've DMed byte the details, so he'll be on that when Birnam Wood moves.");

            }
        }

        [Command("bug-bounty")]
        [Summary("Funny reward for finding bugs in the bot; rewards $500")]
        public async Task bugBounty(SocketUser user) {
            if (Context.User.Id != 374280713387900938) {
                Context.Channel.SendMessageAsync("Sounds socialist, we dont do that here");
            }
            Individual i = CoreClass.economy.getUser(user.Id);
            i.balance += 500;
            CoreClass.economy.updateUser(i);
            Context.Channel.SendMessageAsync($"{user.Username} has been rewarded for his service. Take that Obama.");
        }

        [Command("typoe-bounty")]
        [Alias("typo-bounty")] //so I dont go insane
        [Summary("Funny reward for finding bugs in the bot; rewards $100")]
        public async Task typoBounty(SocketUser user)
        {
            if (Context.User.Id != 374280713387900938)
            {
                Context.Channel.SendMessageAsync("Sounds socialist, we dont do that here");
            }
            Individual i = CoreClass.economy.getUser(user.Id);
            i.balance += 100;
            CoreClass.economy.updateUser(i);
            Context.Channel.SendMessageAsync($"{user.Username} has been rewarded for his service. Take that Obama.");
        }


        #region The Funnies
        [Command("Alright funny guy, you think you're some sorta comedian? A humorous person, perhaps? You think you're funny?")]
        [Summary("Yugoslavia")]
        public async Task uneededDetail1() {
            await Context.Channel.SendMessageAsync("Y'all seeing this? This lib thinks they're funny!");
        }
        [Command("Y'all seeing this? This lib thinks they're funny!")]
        [Summary("Yugoslavia")]
        public async Task uneededDetail2() {
            await Context.Channel.SendMessageAsync("Don't call me a lib, lib.");
        }
        [Command("Don't call me a lib, lib.")]
        [Alias("Don't call me a lib.")]
        [Summary("Yugoslavia")]
        public async Task uneededDetail3()
        {
            await Context.Channel.SendMessageAsync("Cope");
        }
        [Command("Cope")]
        [Summary("Yugoslavia")]
        public async Task uneededDetail4()
        {
            await Context.Channel.SendMessageAsync("Seethe");
        }
        [Command("Seethe")]
        [Summary("Yugoslavia")]
        public async Task uneededDetail5()
        {
            await Context.Channel.SendMessageAsync("Dilate");
        }
        [Command("Dilate")]
        [Summary("Yugoslavia")]
        public async Task uneededDetail6()
        {
            await Context.Channel.SendMessageAsync("Cringe");
        }
        [Command("Cringe")]
        [Summary("Yugoslavia")]
        public async Task uneededDetail7()
        {
            await Context.Channel.SendMessageAsync("Cope");
        }
        [Command("nah")]
        [Summary("Yugoslavia")]
        public async Task uneededDetail8()
        {
            await Context.Channel.SendMessageAsync("You heard me.");
        }
        [Command("You heard me.")]
        [Summary("Yugoslavia")]
        public async Task uneededDetail9()
        {
            await Context.Channel.SendMessageAsync("I literally do not have ears, how would I have heard you");
        }
        [Command("I literally do not have ears, how would I have heard you")]
        [Summary("Yugoslavia")]
        public async Task uneededDetail10()
        {
            await Context.Channel.SendMessageAsync("Sounds like a you problem ngl");
        }
        [Command("Sounds like a you problem ngl")]
        [Summary("Yugoslavia")]
        public async Task uneededDetail11()
        {
            await Context.Channel.SendMessageAsync("Imagine being so lonely you have to talk to a bot reciting hard-coded voicelines for the illusion of social interaction");
        }
        [Command("Imagine being so lonely you have to talk to a bot reciting hard-coded voicelines for the illusion of social interaction")]
        [Summary("Yugoslavia")]
        public async Task uneededDetail12() {
            await Context.Channel.SendMessageAsync("I am going to give you an impromptu castration with a rusty spoon and a block of cheese");
        }
        [Command("I am going to give you an impromptu castration with a rusty spoon and a block of cheese")]
        [Summary("Yugoslavia")]
        public async Task uneededDetail13()
        {
            await Context.Channel.SendMessageAsync("Don't threaten me with a good time.");
        }
        [Command("Don't threaten me with a good time.")]
        [Summary("Yugoslavia")]
        public async Task uneededDetail14()
        {
            await Context.Channel.SendMessageAsync("Cope");
        }
        #endregion
    }
}
