using Extreme.Statistics.Distributions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EconomyBot.Economy
{
    //Class for all organizations
    //On its own, this acts like a NGO/Movement
    public class Organization : EconomyObject
    {
        public ulong orgOwner;
        public List<ulong> members = new List<ulong>();
        public double popularity;
        public string name;

        public override Dictionary<string, double> getBuyable()
        {
            return new Dictionary<string, double> {
                { "rally", 1000 },
                { "online campaign", 500},
                { "protest/demonstation", 2500}
            };
        }
        protected bool breadcrumbBuy(string product) {
            return base.buy(product);
        }
        public override bool buy(string product, Dictionary<string, double> stock = null, int count = 1)
        {
            if (stock == null) {
                stock = getBuyable();
            }
            if (product.Equals("rally"))
            {
                if (balance < getBuyable()["rally"] * count) {
                    return false;
                }
                for (int i = 0; i < count; i++)
                {
                    double x = popularity;
                    double pain = (27 * Math.Pow(Math.E, ((3 * (x - 50)) / 40))) / (40 * Math.Pow((Math.Pow(Math.E, 3 * (x - 5) / 40)), 2));
                    pain *= 40.0 / 4.5;
                    popularity += pain;
                }
                return true;
            }
            else if (product.Equals("online campaign"))
            {
                if (balance < getBuyable()["online campaign"] * count)
                {
                    return false;
                }
                for (int i = 0; i < count; i++)
                {
                    double x = popularity;
                    double pain = (27 * Math.Pow(Math.E, ((3 * (x - 50)) / 40))) / (40 * Math.Pow((Math.Pow(Math.E, 3 * (x - 5) / 40)), 2));
                    pain *= 10.0 / 4.5;
                    popularity += pain;
                }
                return true;
            }
            else if (product.Equals("protest") || product.Equals("demonstration") || product.Equals("protest/demonstration"))
            {
                if (balance < getBuyable()["protest/demonstration"] * count)
                {
                    return false;
                }
                for (int i = 0; i < count; i++)
                {
                    double x = popularity;
                    double pain = (27 * Math.Pow(Math.E, ((3 * (x - 50)) / 40))) / (40 * Math.Pow((Math.Pow(Math.E, 3 * (x - 5) / 40)), 2));
                    pain *= 150.0 / 4.5;
                    popularity += pain;
                }
                return true;
            }
            else {
                return false;
            }
        }

        public override double getIncome() {
            double pos = 0.01*Math.Pow(popularity+members.Count, 2);            
            TriangularDistribution t = new TriangularDistribution(-0.5, 3, 1);
            pos *= t.Sample(rand);
            return pos * 2.5;
        }
    }
}
