using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace EconomyBot.Economy
{    
    public abstract class EconomyObject
    {
        public ulong ID;
        public double balance = 0;
        protected Random rand = new Random();
        public DateTime creationTime;
        public List<double> history = new List<double>();

        //STORAGE SYNTAX
        //0: {TYPE}§{ID}§{CRATION DATE}§{TYPE-SPECIFIC}
        //ALL ELSE:{double; balance}
        private static void waitForFileAvailable(string path, int intervalMilliseconds = 100, int timeOut = 10000) {
            int time = timeOut;
            while (true) {
                try
                {
                    File.ReadAllLines(path);
                    return;
                }
                catch
                {
                    Thread.Sleep(intervalMilliseconds);
                    time -= intervalMilliseconds;
                    if (time <= 0)
                    {
                        return;
                    }
                }
            }
        }
        public List<double> getHistory() {
            //if (!Directory.Exists($@"history")) {
            //    Directory.CreateDirectory($@"history");
            //}
            ////string[] lines = null;
            //if (!File.Exists($@"history/{ID}.txt")) {
            //    FileStream cringe = File.Create($@"history/{ID}.txt");
            //    cringe.Close();
            //}            
            //waitForFileAvailable($@"history/{ID}.txt");
            //string[] lines = File.ReadAllLines($@"history/{ID}.txt");
            //List<double> output = new List<double>();
            //if (lines.Length > 0)
            //{
            //   output.AddRange(lines.Select(v => Double.Parse(v)));
            //}
            //return output;
            return this.history;
        }
        public abstract double getIncome();
        public void update(double income) {
            balance += income;
            if (history.Count == 0)
            {
                history.Add(balance);
            }
            else if (history.Count + 1 < (DateTime.Now.Subtract(creationTime).TotalHours / 2))
            {
                history.Add(balance);
            }
            else if (history.Count < (DateTime.Now.Subtract(creationTime).TotalHours / 2))
            {
                List<string> values = new List<string>();
                double cursor = history.Last();
                double change = (balance - cursor) / (DateTime.Now.Subtract(creationTime).TotalHours / 2);
                while (cursor < balance)
                {
                    cursor += change;
                    history.Add(cursor);
                }
            }
            else
            {
                history.Add(balance);
            }

            //List<double> hist = getHistory();
            //if (hist.Count == 0) {
            //    waitForFileAvailable($@"history/{ID}.txt");
            //    File.AppendAllLines($@"history/{ID}.txt", new List<string>() { balance.ToString() });
            //}
            //else if (hist.Count < DateTime.Now.Subtract(creationTime).TotalDays) {
            //    List<string> values = new List<string>();
            //    double cursor = hist.Last();
            //    double change = (balance - cursor) / DateTime.Now.Subtract(creationTime).TotalDays;
            //    while (cursor < balance) {
            //        cursor += change;
            //        values.Add(cursor.ToString());
            //    }
            //    waitForFileAvailable($@"history/{ID}.txt");
            //    File.AppendAllLines($@"history/{ID}.txt", values);
            //}
            //else {
            //    waitForFileAvailable($@"history/{ID}.txt");
            //    string[] lines = File.ReadAllLines($@"history/{ID}.txt");
            //    lines[lines.Length - 1] = balance.ToString();
            //}            
        }
        public abstract Dictionary<string, double> getBuyable();
        public virtual bool buy(string product, Dictionary<string, double> stock = null, int count = 1) {
            if (stock == null) {
                stock = this.getBuyable();
            }
            if (stock.Keys.Contains(product)) {
                if (balance >= count*stock[product])
                {
                    balance -= count*stock[product];
                    return true;
                }
                else {
                    //Tell the user they're too poor for this
                    return false;
                }
            }
            return false;
        }
    }
}
