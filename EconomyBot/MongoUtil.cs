using Discord;
using Discord.WebSocket;
using EconomyBot.Economy;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EconomyBot
{
    class MongoUtil
    {
        static MongoClient client = new MongoClient();
        static IMongoDatabase DB = client.GetDatabase("Economy");
        /// <summary>
        /// Collections:
        ///  - Users
        ///  - Organizations/Parties + Gov
        ///  - Companies and Worker co-ops
        ///  
        /// Functions:
        ///  - Get
        ///  - List?
        ///  - Add
        ///  - Remove
        ///  - Update
        ///  - Exists
        /// </summary>

        public static IMongoCollection<ServerEconomy> GetEconCollection()
        {
            IMongoCollection<ServerEconomy> collection;
            try
            {
                collection = DB.GetCollection<ServerEconomy>("economies");
            }
            catch
            {
                DB.CreateCollection("economies");
                collection = DB.GetCollection<ServerEconomy>("economies");
            }
            return collection;
        }


        public static ServerEconomy getEconomy()
        {
            IFindFluent<ServerEconomy, ServerEconomy> find = findEconomy(e => true);
            if (find.CountDocuments() == 0) {
                IMongoCollection<ServerEconomy> col = GetEconCollection();
                ServerEconomy econ = new ServerEconomy();
                col.InsertOne(econ);
                return econ;
            }
            return find.First();
        }
        public static void updateEcon(ServerEconomy c)
        {
            IMongoCollection<ServerEconomy> collection = GetEconCollection();
            collection.ReplaceOne(com => com._id == c._id, c);
            return;
        }

        private static IFindFluent<ServerEconomy, ServerEconomy> findEconomy(Expression<Func<ServerEconomy, bool>> p)
        {
            IMongoCollection<ServerEconomy> collection = GetEconCollection();
            IFindFluent<ServerEconomy, ServerEconomy> find = collection.Find(p);
            return find;
        }

        //public static IMongoCollection<Individual> GetUserCollection()
        //{
        //    IMongoCollection<Individual> collection;
        //    try
        //    {
        //        collection = DB.GetCollection<Individual>("users");
        //    }
        //    catch
        //    {
        //        DB.CreateCollection("users");
        //        collection = DB.GetCollection<Individual>("users");
        //    }
        //    return collection;
        //}
        //public static IMongoCollection<Organization> GetOrgCollection()
        //{
        //    IMongoCollection<Organization> collection;
        //    try
        //    {
        //        collection = DB.GetCollection<Organization>("organizations");
        //    }
        //    catch
        //    {
        //        DB.CreateCollection("organizations");
        //        collection = DB.GetCollection<Organization>("organizations");
        //    }
        //    return collection;
        //}
        //public static IMongoCollection<Company> GetCompanyCollection()
        //{
        //    IMongoCollection<Company> collection;
        //    try
        //    {
        //        collection = DB.GetCollection<Company>("companies");
        //    }
        //    catch
        //    {
        //        DB.CreateCollection("companies");
        //        collection = DB.GetCollection<Company>("companies");
        //    }
        //    return collection;
        //}

        //private static IFindFluent<Individual, Individual> findUser(Expression<Func<Individual, bool>> filter)
        //{
        //    IMongoCollection<Individual> collection = GetUserCollection();
        //    IFindFluent<Individual, Individual> find = collection.Find(filter: filter);
        //    return find;
        //}
        //private static IFindFluent<Organization, Organization> findOrg(Expression<Func<Organization, bool>> filter)
        //{
        //    IMongoCollection<Organization> collection = GetOrgCollection();
        //    IFindFluent<Organization, Organization> find = collection.Find(filter: filter);
        //    return find;
        //}

        //private static IFindFluent<Company, Company> findOrg(Expression<Func<Company, bool>> filter)
        //{
        //    IMongoCollection<Company> collection = GetCompanyCollection();
        //    IFindFluent<Company, Company> find = collection.Find(filter: filter);
        //    return find;
        //}

        //public static void addUser(Individual d)
        //{
        //    IMongoCollection<Individual> guildData = GetUserCollection();
        //    guildData.InsertOne(d);
        //}

        //public static void addOrg(Organization d)
        //{
        //    IMongoCollection<Organization> guildData = GetOrgCollection();
        //    guildData.InsertOne(d);
        //}

        //public static void addCompany(Company d)
        //{
        //    IMongoCollection<Company> guildData = GetCompanyCollection();
        //    guildData.InsertOne(d);
        //}

        //public static List<Individual> listUsers(Expression<Func<Individual, bool>> filter)
        //{
        //    IFindFluent<Individual, Individual> find = findUser(filter);
        //    return find.ToList();
        //}

        //public static List<Organization> listOrg(Expression<Func<Organization, bool>> filter)
        //{
        //    IFindFluent<Organization, Organization> find = findOrg(filter);
        //    return find.ToList();
        //}

        //public static List<Company> listCompany(Expression<Func<Company, bool>> filter)
        //{
        //    IFindFluent<Company, Company> find = findOrg(filter);
        //    return find.ToList();
        //}

        //public static Individual getUser(Expression<Func<Individual, bool>> filter)
        //{
        //    IFindFluent<Individual, Individual> find = findUser(filter);
        //    return find.First();
        //}
        //public static Individual getUser(ulong filter, bool create = true)
        //{
        //    IFindFluent<Individual, Individual> find = findUser(u => u.ID == filter);
        //    if (find.CountDocuments() == 0 && create)
        //    {
        //        Individual i = new Individual()
        //        {
        //            ID = filter,
        //            balance = 0,
        //            creationTime = DateTime.Now
        //        };
        //        addUser(i);
        //        return i;
        //    }
        //    else if (find.CountDocuments() == 0) {
        //        return null;
        //    }
        //    return find.First();
        //}

        //public static Organization getOrg(Expression<Func<Organization, bool>> filter)
        //{
        //    IFindFluent<Organization, Organization> find = findOrg(filter);
        //    return find.First();
        //}

        //public static Company getCompany(Expression<Func<Company, bool>> filter)
        //{
        //    IFindFluent<Company, Company> find = findOrg(filter);
        //    return find.First();
        //}

        //public static void updateUser(Individual c)
        //{
        //    IMongoCollection<Individual> collection = GetUserCollection();
        //    collection.ReplaceOne(com => com.ID == c.ID, c);
        //    return;
        //}
        //public static void updateOrg(Organization c)
        //{
        //    IMongoCollection<Organization> collection = GetOrgCollection();
        //    collection.ReplaceOne(com => com.ID == c.ID, c);
        //    return;
        //}
        //public static void updateCompany(Company c) {
        //    IMongoCollection<Company> collection = GetCompanyCollection();
        //    collection.ReplaceOne(com => com.ID == c.ID, c);
        //    return;
        //}

        //public static void removeUser(Individual i) {
        //    IMongoCollection<Individual> collection = GetUserCollection();
        //    collection.DeleteOne(com => com.ID == i.ID);
        //    return;
        //}
        //public static void removeOrg(Organization i)
        //{
        //    IMongoCollection<Organization> collection = GetOrgCollection();
        //    collection.DeleteOne(com => com.ID == i.ID);
        //    return;
        //}
        //public static void removeCompany(Company i)
        //{
        //    IMongoCollection<Company> collection = GetCompanyCollection();
        //    collection.DeleteOne(com => com.ID == i.ID);
        //    return;
        //}

        ////public static bool messageExists(MongoMessage m)
        ////{
        ////    return countMessages(a => a == m) > 0;
        ////}


    }
}
