using System;
using System.Collections.Generic;
using System.Text;

namespace EconomyBot.Economy
{
    //Tracks how much stock a user owns of a company
    public class Stock
    {        
        //User who owns the stock
        public ulong owner;
        //Company whose stock is owned
        public ulong companyBought;
        //Amount of Stock
        public double amount;

        /// <summary>
        /// Transfer some or all of this stock to another user for some price
        /// </summary>
        /// <param name="buyer">The user to transfer stock to</param>
        /// <param name="price">The total price for the stock being sold</param>
        /// <param name="amountBuying">The amount of stock being sold. Leave blank to sell all</param>
        /// <returns>The result of the sale, if it was successful, denied, or failed</returns>
        public sellResult sell(ulong buyer, double price, double amountBuying = -1) {
            if (amountBuying == -1) {
                amountBuying = amount;
            }
            if (amountBuying > this.amount) {
                return sellResult.DENIED_AMOUNT_TOO_LARGE;
            }
            //Check if company exists
            if (!CoreClass.economy.companies.Exists(c => c.ID == companyBought)) {
                return sellResult.ERROR_INVALID_COMPANY;
            }
            //Check if owner exists before a new owner object is made
            if (!CoreClass.economy.citizens.Exists(c => c.ID == owner)) {
                return sellResult.ERROR_INVALID_OWNER;
            }
            Individual seller = CoreClass.economy.getUser(owner);
            Individual iBuyer = CoreClass.economy.getUser(buyer);
            Company c = CoreClass.economy.getCompany(ci => ci.ID == companyBought);

            //Stock trades make use of the bank
            if (iBuyer.balance < price) {
                return sellResult.DENIED_LACK_OF_FUNDS;
            }

            seller.balance += price;
            iBuyer.balance -= price;

            iBuyer.addStock(companyBought, amount);
            seller.addStock(companyBought, -1 * amount);

            c.stock_price = price / amountBuying;

            CoreClass.economy.updateUser(seller);
            CoreClass.economy.updateUser(iBuyer);
            CoreClass.economy.updateCompany(c);

            return sellResult.SOLD_SUCCESSFULLY;
        }
        public enum sellResult { 
            SOLD_SUCCESSFULLY,
            DENIED_AMOUNT_TOO_LARGE,
            DENIED_LACK_OF_FUNDS,            
            ERROR_INVALID_BUYER,
            ERROR_INVALID_OWNER,
            ERROR_INVALID_COMPANY
        }
    }
}
