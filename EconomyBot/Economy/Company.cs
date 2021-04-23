using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Extreme.Statistics.Distributions;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace EconomyBot.Economy
{
    public class Company : Organization, IComparable<Company>
    {
        public string type = "company"; 
        public int astroturfs = 0;

        [BsonDictionaryOptions(Representation = DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<ulong,double> employeeWages = new Dictionary<ulong, double>();
        [BsonDictionaryOptions(Representation = DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<ulong, string> employeeProduce = new Dictionary<ulong, string>();
       

        [BsonDictionaryOptions(Representation = DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<string, double> products = new Dictionary<string, double>();
        //Certain employees produce cetrain products
        [BsonDictionaryOptions(Representation = DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<string, int> productStock = new Dictionary<string, int>();

        //Literally just the most recent price it was sold for
        //God I can't believe this is a real-life thing
        public double stock_price = 0;
        //For making the funny stock graph
        public List<double> stock_history = new List<double>();

        //Each share is 1/100,000 of a company, or 0.001%
        public static double SHARES_PER_COMPANY = 100000;        

        /// <summary>
        /// Increases product stock and pays the employee working (if the employee can work)
        /// </summary>
        /// <param name="employee">The employee working</param>
        /// <returns>true if the employee successfully works, false if the employee is unable</returns>
        public bool work(Individual employee) {
            if (!employeeWages.ContainsKey(employee.ID) || !employee.canWork()) {
                return false;
            }
            productStock[employeeProduce[employee.ID]]++;
            employee.balance += employeeWages[employee.ID];
            this.balance -= employeeWages[employee.ID];
            employee.setWork();
            return true;
        }
        /// <summary>
        /// Hire an employee
        /// </summary>
        /// <param name="id">ID of the employee</param>
        /// <param name="wage">How much the employee is paid when they work</param>
        /// <param name="product">The product that the employee produces</param>
        public void addEmployee(ulong id, double wage, string product = "nothing") {
            if (!employeeWages.ContainsKey(id))
            {
                employeeWages.Add(id, wage);
                employeeProduce.Add(id, product);
                members.Add(id);
            }
            else {
                employeeWages[id] = wage;
                if (product != null) {
                    employeeProduce[id] = product;
                }
            }
        }
        /// <summary>
        /// Remove an employee from the company
        /// </summary>
        /// <param name="id">ID of the employee to remove</param>
        public void removeEmployee(ulong id)
        {
            employeeWages.Remove(id);
            members.Remove(id);
        }
        /// <summary>
        /// Get all things that a company can buy
        /// </summary>
        /// <returns>A dictionary of products, the name being the key and the price being the value</returns>
        public override Dictionary<string, double> getBuyable()
        {
            return new Dictionary<string, double> {
                { "advertising campaign", 750},
                { "astroturfing campaign", 250 }
            };
        }
        /// <summary>
        /// Buy a product from the list of products available
        /// </summary>
        /// <param name="product">The product to buy</param>
        /// <param name="pricing">The list of products to use, default is the list from this.getBuyable()</param>
        /// <param name="count">amount of product to buy</param>
        /// <returns>true if the sale is successful, false if not</returns>
        public override bool buy(string product, Dictionary<string, double> pricing = null, int count = 1)
        {
            if (pricing == null)
            {
                pricing = getBuyable();
            }
            if (product.Equals("advertising campaign"))
            {
                if (balance >= count*pricing[product])
                {
                    balance -= count*pricing[product];
                }
                else { 
                    return false;
                }
                for (int i = 0; i < count; i++) {
                    double x = popularity;
                    double pain = (6*Math.Pow(Math.E, (x/20) - 2.5))/(Math.Pow(Math.Pow(Math.E, (x / 20) - 2.5) + 1, 2));
                    pain *= 25;
                    popularity += pain;
                    astroturfs++;
                }

                return true;
            }
            else if (product.Equals("astroturfing campaign"))
            {
                if (balance >= count*pricing[product])
                {
                    balance -= count*pricing[product];
                }
                else
                {
                    return false;
                }
                for (int i = 0; i < count; i++)
                {
                    double x = popularity;
                    double pain = (6 * Math.Pow(Math.E, (x / 20) - 2.5)) / (Math.Pow(Math.Pow(Math.E, (x / 20) - 2.5) + 1, 2));
                    pain *= 5;
                    popularity += pain;
                    astroturfs += 5;
                }
                return true;
            }            
            else {
                return false;
            }
        }
        //0 = worst case, 1 = slow-day income, 2 = expected income, 3 = maximum income
        /// <summary>
        /// Gets the range of potential income for this company
        /// </summary>
        /// <returns>an array of projections</returns>
        public double[] getIncomeProjection() {
            //productStock.Select(t => t.Value * products[t.Key]).Sum();
            double pos = productStock.Select(t => t.Value * products[t.Key]).Sum();
            pos *= popularityToModifier(popularity);

            double percentToDividend = 0;
            foreach (Individual i in CoreClass.economy.citizens.Where(i => i.ownedStock.Exists(s => s.companyBought == this.ID)))
            {
                if (i.ID == orgOwner)
                {
                    //Profits that would normally go to owner stay in the company
                    continue;
                }
                else
                {
                    Stock s = i.ownedStock.Find(s => s.companyBought == this.ID);
                    percentToDividend += sharesToPercentage(s.amount);
                }
            }
            pos *= 1 - percentToDividend;

            double[] output = new double[4];
            output[0] = pos * 0.5; //Worst case
            output[1] = pos * 1; //Unpog
            output[2] = pos * 1.23; //Average
            output[3] = pos * 2; //Best Case
            return output;
        }
        /// <summary>
        /// Converts the company's popularity to a modifier
        /// </summary>
        /// <param name="pop">the popularity of this company</param>
        /// <returns>a value between 0.0 and 1.0 as a percentage for popularity</returns>
        private double popularityToModifier(double pop) {
            double output = 1;
            output += Math.Pow(Math.E, -((pop - 50) / 25));
            return 1 / output;
        }
        /// <summary>
        /// Calculates the income of this company, updates popularity based on astroturfs, and removes sold stock
        /// </summary>
        /// <returns>the amount of money the company earned in this turn</returns>
        public double getIncome()
        {
            TriangularDistribution t = new TriangularDistribution(0.5, 2, 1.2);
            double sellRate = popularityToModifier(popularity * t.Sample());
            double output = 0;            
            foreach (string key in productStock.Keys.ToList()) {
                int sold = (int)(productStock[key] * sellRate);
                productStock[key] -= sold;
                output += products[key] * sold;               
            }

            //Dividends
            double totalDividendCost = 0;
            foreach (Individual i in CoreClass.economy.citizens.Where(i => i.ownedStock.Exists(s => s.companyBought == this.ID)).ToList()) {
                if (i.ID == orgOwner)
                {
                    //Profits that would normally go to owner stay in the company
                    continue;
                }
                else {
                    Stock s = i.ownedStock.Find(s => s.companyBought == this.ID);
                    double dividend = output * sharesToPercentage(s.amount);
                    totalDividendCost += dividend;
                    //Stock stuff goes directly to bank
                    i.balance += dividend;
                    CoreClass.economy.updateUser(i);
                }
            }
            output -= totalDividendCost;

            //Update popularity
            if (astroturfs < 1)
            {
                astroturfs = 1;
            }
            popularity *= (1 + (astroturfs)) / (2 + (astroturfs));
            if (astroturfs > 1) {
                astroturfs--;
            }

            //Update stock history
            stock_history.Add(stock_price);

            //Update owner
            updateOwner();
            return output;
        }
        /// <summary>
        /// Converts a percentage ownership of a company to shares
        /// </summary>
        /// <param name="percentage">The percentage to be converted, in a range of 0-1</param>
        /// <returns>An amount of shares that corrosponds to the given percentage</returns>
        public static double percentageToShares(double percentage) {
            return percentage * Company.SHARES_PER_COMPANY;
        }
        /// <summary>
        /// Converts shares to a percentage ownership of a company
        /// </summary>
        /// <param name="shares">The amount of shares to convert to a percentage</param>
        /// <returns>The percentage of a company represented by the given amount of shares, from 0.0 to 1.0</returns>
        public static double sharesToPercentage(double shares) {
            return shares / SHARES_PER_COMPANY;         
        }
        /// <summary>
        /// Update the owner to the person who owns the most stock
        /// </summary>
        public void updateOwner()
        {
            ulong maxID = 0;
            double maxAmount = 0;
            foreach (Individual cit in CoreClass.economy.citizens) {
                if (cit.ownedStock.Exists(s => s.companyBought == this.ID)) {
                    Stock stock = cit.ownedStock.Find(s => s.companyBought == this.ID);
                    if (stock.amount > maxAmount)
                    {
                        maxAmount = stock.amount;
                        maxID = stock.owner;
                    }
                    //Prioritize current owner if people own the same amoutn of stock
                    else if (stock.amount == maxAmount && maxID == this.orgOwner) {
                        maxAmount = stock.amount;
                        maxID = stock.owner;
                    }
                }
            }
        }

        /// <summary>
        /// Compare two companies based on their popularity
        /// </summary>
        /// <param name="other">The company to compare this company with</param>
        /// <returns></returns>
        int IComparable<Company>.CompareTo(Company other)
        {
            return this.popularity.CompareTo(other.popularity);
        }
    }
}
