using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EconomyBot.Economy
{
    public class Individual : EconomyObject
    {
        public double cashBalance = 1000;
        public ulong[] jobIDs = new ulong[2] { 0, 0 };
        bool worked = false;
        public List<Stock> ownedStock = new List<Stock>();        
        /// <summary>
        /// Add this user to a given company
        /// </summary>
        /// <param name="id">ID of the company to add this user to</param>
        /// <returns>true if the user is successfully added, false if not</returns>
        public bool joinCompany(ulong id) {
            if (jobIDs[0] == 0)
            {
                jobIDs[0] = id;
                return true;
            }
            else if (jobIDs[1] == 0)
            {
                jobIDs[1] = id;
                return true;
            }
            else {
                return false;
            }            
        }
        /// <summary>
        /// Add stock to this user
        /// </summary>
        /// <param name="companyID">ID of the company</param>
        /// <param name="amount">Amount of stock</param>
        public void addStock(ulong companyID, double amount) {
            if (ownedStock.Exists(s => s.companyBought == companyID))
            {
                Stock stock = ownedStock.Find(s => s.companyBought == companyID);
                //Don't need to check for negative because that's already handled
                stock.amount -= amount;
                ownedStock.RemoveAll(s => s.companyBought == companyID);
                //Don't re-add if the stock amount is 0, saves space
                if (stock.amount != 0) {
                    ownedStock.Add(stock);
                }
            }
            else {
                Stock stock = new Stock(this.ID, companyID, amount);
                ownedStock.Add(stock);
            }
        }
        /// <summary>
        /// Get the per-update income of this user
        /// </summary>
        /// <returns>the per-update income of this user</returns>
        public override double getIncome() {
            return 0;
        }
        /// <summary>
        /// Returns whether the user is able to work this turn
        /// </summary>
        /// <returns>whether the user is able to work this turn</returns>
        internal bool canWork() {
            return !worked;
        }
        /// <summary>
        /// Sets whether or not the user can work this turn
        /// </summary>
        internal void setWork() {
            worked = true;
        }
        internal bool getWork() {
            return worked;
        }
        /// <summary>
        /// Remove a user from the given company
        /// </summary>
        /// <param name="id">ID of the company to remove the user from</param>
        /// <returns>true if the user was successfully removed, false if not</returns>
        public bool leaveCompany(ulong id)
        {
            if (jobIDs[0] == id)
            {
                jobIDs[0] = 0;
                return true;
            }
            else if (jobIDs[1] == id)
            {
                jobIDs[1] = 0;
                return true;
            }
            else
            {
                return false;
            }
        }

        public new void update(double d) {
            worked = false;
            base.update(d);
        }
        /// <summary>
        /// Gets all products that a user can buy
        /// </summary>
        /// <returns>a dictionary with the name of the products as the key, and its price as the value</returns>
        public override Dictionary<string, double> getBuyable()
        {
            Dictionary<string, double> buyable = new Dictionary<string, double>();
            foreach (Company c in CoreClass.economy.companies) {
                foreach (KeyValuePair<string, double> valuePair in c.products.ToList()) {
                    if (valuePair.Key == "nothing" && valuePair.Value == 0) {
                        continue;
                    }
                    buyable.Add($"_{c.name}_ {valuePair.Key}", valuePair.Value);
                }
            }
            return buyable;
        }

        public override bool buy(string product, Dictionary<string, double> stock = null, int count = 1)
        {
            if (stock == null)
            {
                stock = this.getBuyable();
            }
            if (stock.Keys.Contains(product))
            {
                if (balance >= count * stock[product])
                {
                    Company c = CoreClass.economy.getCompany(c => product.Contains(c.name));
                    string trimProduct = product.Substring($"_{c.name}_ ".Length); //Product without the company's name
                    balance -= count * stock[trimProduct];
                    c.balance += count * stock[trimProduct];
                    CoreClass.economy.updateCompany(c);
                    return true;
                }
                else
                {
                    //Tell the user they're too poor for this
                    //Imagine
                    return false;
                }
            }
            return false;
        }
    }
}
