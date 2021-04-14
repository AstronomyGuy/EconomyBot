using System;
using System.Collections.Generic;
using System.Text;

namespace EconomyBot.Economy
{
    //Coming soon!
    class WorkerCoop : Company
    {
        string type = "coop";
        /// <summary>
        /// Proliferates the effect of someone working
        /// </summary>
        /// <param name="employee">The person working</param>
        public void work(Individual employee)
        {
            base.work(employee);
            foreach (ulong worker in members)
            {
                Individual w = CoreClass.economy.getUser(worker);
                w.balance += employeeWages[w.ID] / employeeWages.Count;
            }
        }
        /// <summary>
        /// Gets profits and distributes them equally between all workers.
        /// </summary>
        public new void update()
        {
            double inc = getIncome();
            foreach (ulong worker in members)
            {
                Individual w = CoreClass.economy.getUser(worker);
                w.balance += inc / employeeWages.Count;
            }
            base.update(0.0);
        }
    }
}
