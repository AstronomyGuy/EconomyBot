using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EconomyBot.Economy
{
    //The funny economy yes yes
    public class ServerEconomy
    {
        [BsonId]
        public ObjectId _id;
        public Government gov;
        public List<Individual> citizens = new List<Individual>();
        public List<Company> companies = new List<Company>();
        public List<Organization> organizations = new List<Organization>();

        public List<ulong> botMods = new List<ulong>() { 374280713387900938, 259898617958105088 };

        public double UBI = 50;
        //public double inflation_rate = 1.02;
        [BsonDictionaryOptions(Representation = DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<ulong, double> roleIncomes = new Dictionary<ulong, double>();

        public ServerEconomy() {
            gov = new Government();
            citizens = new List<Individual>();
            companies = new List<Company>();
            organizations = new List<Organization>();
        }
        /// <summary>
        /// Company IDs were being weird, now we have this abomination
        /// </summary>
        /// <returns>A unique ID for new companies</returns>
        public ulong getNextCompanyId() {
            ulong ID = 1;
            while (companies.Exists(c => c.ID == ID)) {
                ID++;
            }
            return ID;
        }
        /// <summary>
        /// Gets all objects in the economy and puts them in one list
        /// </summary>
        /// <returns>a list of all EconomyObjects in the economy</returns>
        public List<EconomyObject> allEconObj() {
            List<EconomyObject> output = new List<EconomyObject>();
            output.AddRange(citizens);
            output.AddRange(companies);
            output.AddRange(organizations);
            output.Add(gov);
            return output;
        }

        public List<EconomyObject> userEconObj() {
            List<EconomyObject> output = new List<EconomyObject>();
            output.AddRange(citizens);
            output.AddRange(companies);
            output.AddRange(organizations);
            return output;
        }
        /// <summary>
        /// Update all citizens' income with a UBI
        /// </summary>
        void updateUBI() {
            foreach (Individual i in citizens) {
                i.balance += UBI;                
                SocketGuildUser gu = null;
                try
                {
                    SocketGuild g = CoreClass.client.GetGuild(CoreClass.SERVER_ID);
                    if (g == null) { continue; }
                    gu = g.GetUser(i.ID);
                }
                catch
                {
                    //Do nothing
                }
                if (gu != null)
                {
                    foreach (SocketRole r in gu.Roles) {
                        if (roleIncomes.ContainsKey(r.Id)) {
                            i.balance += roleIncomes[r.Id];
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Update everyone's income
        /// </summary>
        public void updateAll() {
            gov.update(gov.getIncome());
            companies.ForEach(c => c.update(c.getIncome()));
            organizations.ForEach(c => c.update(c.getIncome()));

            updateUBI();
            //Citizens last so that wages are calculated beforehand
            citizens.ForEach(c => c.update(c.getIncome()));            
        }

        internal List<Company> listCompany(Func<Company, bool> p)
        {
            return companies.Where(p).ToList();
        }

        internal void updateUser(Individual user)
        {
            citizens.RemoveAll(i => i.ID == user.ID);
            citizens.Add(user);
        }

        internal void updateCompany(Company c)
        {
            companies.RemoveAll(i => i.ID == c.ID);
            companies.Add(c);
        }
        /// <summary>
        /// Get a user with a specified id, create one if nessasary
        /// </summary>
        /// <param name="id">ID of the user to look for</param>
        /// <returns>an Individual with the specified id</returns>
        internal Individual getUser(ulong id)
        {
            if (citizens.Exists(i => i.ID == id))
            {
                return citizens.Find(i => i.ID == id);
            }
            else {
                Individual i = new Individual()
                {
                    ID = id,
                    creationTime = DateTime.Now
                };
                citizens.Add(i);
                return i;
            }
        }

        internal Company getCompany(Predicate<Company> p)
        {
            return companies.Find(p);
        }
        internal Company getCompany(ulong p)
        {
            return getCompany(c => c.ID == p);
        }

        internal void addCompany(Company c)
        {
            if (companies.Exists(a => a.ID == c.ID))
            {
                updateCompany(c);
            }
            else {
                companies.Add(c);
            }
        }
        internal void removeCompany(Company c)
        {
            if (companies.Exists(a => a.ID == c.ID))
            {
                companies.RemoveAll(o => o.ID == c.ID);
            }            
        }
    }
}
