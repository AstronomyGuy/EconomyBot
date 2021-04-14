using System;
using System.Collections.Generic;
using System.Text;

namespace EconomyBot.Economy
{
    public class Government : EconomyObject
    {
        public double income = 0;
        public double spending = 0;
        
        public Dictionary<Individual,double> employees;
        //<Tax bracket upper limit, percentage (decimal form)>
        Dictionary<double, double> incomeTaxBrackets = new Dictionary<double, double>()
        {
            {9875/100, 0.10 },
            {40125/100, 0.15 },
            {85525/100, 0.20 },
            {163300/100, 0.25 },
            {207350/100, 0.30 },
            {518400/100, 0.40 },
            {double.MaxValue, 0.45 }
        };

        //<Tax bracket upper limit, percentage (decimal form)>
        Dictionary<double, double> corporateTaxBrackets = new Dictionary<double, double>()
        {
            {1000000/100, 0.05 },
            {10000000/100, 0.10 },
            {100000000/100, 0.15 },
            {1000000000/100, 0.25 },
            {double.MaxValue, 0.30 }
        };
        public void setIncome(double d) {
            income = d;
        }
        public void setSpending(double d)
        {
            spending = d;
        }
        public double getGrossIncome()
        {
            return income;
        }
        public double getSpending()
        {
            return spending;
        }
        public override Dictionary<string, double> getBuyable()
        {
            return new Dictionary<string, double>() { };
        }
        public double fitIncomeTax(double income) {
            double b = 0;
            foreach (KeyValuePair<double, double> bracket in incomeTaxBrackets) {
                if (income <= bracket.Key)
                {
                    b = bracket.Value;
                }
                else {
                    break;
                }
            }
            return b;
        }
        public double fitCorporateTax(double income)
        {
            double b = 0;
            foreach (KeyValuePair<double, double> bracket in corporateTaxBrackets)
            {
                if (income <= bracket.Key)
                {
                    b = bracket.Value;
                }
            }
            return b;
        }

        public override double getIncome()
        {
            return income - spending;
        }
    }
}
