using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using System.Text.RegularExpressions;

/*
    Version: 1.0.0
    Bot Name: Stocks Opening - Jack's Idea Retest of Yesterday's Close
    Features:
    - Does not trade entries after X hour
    - Closes all trades at the end of the day


    Description:
    This bot works by observing the price movement at the start of the trading day. 
    If the price opens higher than the previous day's close, it waits for a retest 
    of the previous day's close before entering a long market order. Conversely, 
    if the price opens lower than the previous day's close, it waits for a retest of
    the previous day's close before entering a short market order.
*/



namespace cAlgo
{

    public static class BarsExtensions 
    { 
        /// <summary> 
        /// Determines if there are no trades allowed after the specified hour. 
        /// </summary> 
        /// <param name="bars">The Bars object representing the market data.</param> 
        /// <param name="CloseAllTradesHour">The hour after which no trades are allowed.</param> 
        /// <returns>True if the current hour is greater than or equal to the specified hour, otherwise false.</returns> 
        public static bool NoTradesAfterHour(this Bars bars, int NoTradesAfterHour) 
        { 
            // Get the hour of the last bar's opening time 
            var hour = bars.OpenTimes[bars.Count - 1].Hour; 
            // Check if the current hour is greater than or equal to the specified close all trades hour 
            if (hour >= NoTradesAfterHour) 
            { 
                return true; 
            } 
            return false; 
        } 
        
        
        /// <summary>
        /// Closes all trades with the specified label if the current time is past the specified hour.
        /// </summary>
        /// <param name="thisPositions">The collection of positions.</param>
        /// <param name="bars">The bar data to check the current time.</param>
        /// <param name="LABEL">The label of the trades to close.</param>
        /// <param name="CloseAllTradesHour">The hour after which all trades should be closed.</param>
        /// <param name="LogMessage">The logging action to record messages.</param>
        /// <returns>Returns false if trades were closed, otherwise true.</returns>
        public static bool CloseAllTrades(this Positions thisPositions, Bars bars, string LABEL, int CloseAllTradesHour, Action<string> LogMessage)
        {
            // Get the hour of the most recent bar
            var hour = bars.OpenTimes[bars.Count - 1].Hour;
            
            // Check if it's past the close all trades hour
            if (hour >= CloseAllTradesHour)
            {
                foreach (var pos in thisPositions.Where(p => p.Label == LABEL))
                {
                    pos.Close();
                }
                
                // Log the closure of all trades
                LogMessage("It's past " + CloseAllTradesHour + "; all trades have been closed.");
                return false;
            }
            
            return true;
        }

    }
    
    
    /// <summary>
    /// Represents the possible trade directions.
    /// </summary>
    public enum TradeDirection
    {
        /// <summary>
        /// Indicates a long trade direction.
        /// </summary>
        LONG,
    
        /// <summary>
        /// Indicates a short trade direction.
        /// </summary>
        SHORT,
    
        /// <summary>
        /// Indicates no trade direction.
        /// </summary>
        NONE,
    }

    
    
    /// <summary>
    /// Manages the strategy-related data and operations.
    /// </summary>
    public static class StrategyManagement
    {
        private static Position _position;
        private static TradeDirection _todaysDirection = TradeDirection.NONE;
        private static double _entryPrice = 0;
        private static bool _tradedToday = false;
    
        /// <summary>
        /// Updates the current position.
        /// </summary>
        /// <param name="newPosition">The new position to update.</param>
        public static void UpdatedPosition(Position newPosition)
        {
            _position = newPosition;
        }
    
        /// <summary>
        /// Updates the entry price.
        /// </summary>
        /// <param name="newEntryPrice">The new entry price to update.</param>
        private static void UpdatedEntryPrice(double newEntryPrice)
        {
            _entryPrice = newEntryPrice;
        }
    
        /// <summary>
        /// Updates today's trade direction.
        /// </summary>
        /// <param name="newDirection">The new trade direction to update.</param>
        private static void UpdatedTodaysDirection(TradeDirection newDirection)
        {
            _todaysDirection = newDirection;
        }
    
        /// <summary>
        /// Updates the traded today flag to true.
        /// </summary>
        public static void UpdatedTradedToday()
        {
            _tradedToday = true;
        }
    
        /// <summary>
        /// Gets the traded today flag.
        /// </summary>
        /// <returns>True if a trade has been made today, otherwise false.</returns>
        public static bool GetTradedToday()
        {
            return _tradedToday;
        }
    
        /// <summary>
        /// Gets the entry price.
        /// </summary>
        /// <returns>The current entry price.</returns>
        public static double GetEntryPrice()
        {
            return _entryPrice;
        }
    
        /// <summary>
        /// Gets the trade direction for today.
        /// </summary>
        /// <returns>The current trade direction.</returns>
        public static TradeDirection GetTradeDirection()
        {
            return _todaysDirection;
        }
    
        /// <summary>
        /// Updates the entry price if the last two bars are from different days.
        /// </summary>
        /// <param name="barData">The bar data to check.</param>
        /// <param name="Log">The log action to record updates.</param>
        public static void UpdateEntryPrice(Bars barData, Action<string> Log)
        {
            double lastBarDate = barData.OpenTimes[barData.Count - 2].Day;
            double secondLastBarDate = barData.OpenTimes[barData.Count - 3].Day;
    
            if (lastBarDate != secondLastBarDate)
            {
                double secondLastBar = barData.ClosePrices[barData.Count - 3];
    
                if (_entryPrice == 0)
                {
                    UpdatedEntryPrice(secondLastBar);
                    Log($"[ENTRY PRICE]: {secondLastBar}");
                }
            }
        }
    
        /// <summary>
        /// Updates today's trade direction based on the closing prices of the last two bars.
        /// </summary>
        /// <param name="barData">The bar data to check.</param>
        /// <param name="Log">The log action to record updates.</param>
        public static void UpdatesTodaysDirection(Bars barData, Action<string> Log)
        {
            // Retrieve the closing price of the most recent bar.
            double lastClosePrice = barData.ClosePrices[barData.Count - 1];
            
            // Retrieve the closing price of the second most recent bar.
            double secondLastClosePrice = barData.ClosePrices[barData.Count - 2];
        
            // Retrieve the day of the most recent bar.
            int lastBarDay = barData.OpenTimes[barData.Count - 1].Day;
            
            // Retrieve the day of the second most recent bar.
            int secondLastBarDay = barData.OpenTimes[barData.Count - 2].Day;
        
            // If the last two bars are from the same day or the direction has already been set, exit the method.
            if (lastBarDay == secondLastBarDay || _todaysDirection != TradeDirection.NONE)
            {
                return;
            }
        
            // Determine the trade direction based on the comparison of the last two closing prices.
            if (lastClosePrice > secondLastClosePrice)
            {
                // Update today's trade direction to LONG if the most recent closing price is higher than the previous one.
                UpdatedTodaysDirection(TradeDirection.LONG);
                Log("[TRADE DIRECTION]: LONG");
            }
            else if (lastClosePrice < secondLastClosePrice)
            {
                // Update today's trade direction to SHORT if the most recent closing price is lower than the previous one.
                UpdatedTodaysDirection(TradeDirection.SHORT);
                Log("[TRADE DIRECTION]: SHORT");
            }
        }

    
        /// <summary>
        /// Resets the trade data if the last two bars are from different days.
        /// </summary>
        /// <param name="barData">The bar data to check.</param>
        /// <param name="Log">The log action to record updates.</param>
        public static void ResetDay(Bars barData, Action<string> Log)
        {
            // Retrieve the day of the most recent bar.
            int lastBarDay = barData.OpenTimes[barData.Count - 1].Day;
            
            // Retrieve the day of the second most recent bar.
            int secondLastBarDay = barData.OpenTimes[barData.Count - 2].Day;
        
            // Exit early if the last two bars are from the same day or the entry price is zero.
            if (lastBarDay == secondLastBarDay || _entryPrice == 0)
            {
                return;
            }
        
            // Reset trade data.
            _position = null;
            _todaysDirection = TradeDirection.NONE;
            _entryPrice = 0;
            _tradedToday = false;
        
            // Log the reset action.
            Log("[RESET]: Trade data Reset");
        }

    }

    
      
    /// <summary>
    /// A filter to determine if the current price is within the maximum movement range.
    /// </summary>
    public static class MaxMovementFilter
    {
        /// <summary>
        /// List to store bar data values.
        /// </summary>
        public static List<double> barData = new();
    
    
        /// <summary>
        /// Determines if the current price is out of the maximum movement range.
        /// </summary>
        /// <param name="useMaxMoveMent">A flag to indicate if the maximum movement check should be used.</param>
        /// <param name="currentClosePrice">The current close price.</param>
        /// <param name="maxDistance">The maximum allowable distance.</param>
        /// <param name="Log">The log action to record updates.</param>
        /// <returns>Returns true if the current price is out of the maximum movement range; otherwise, false.</returns>
        public static bool IsOutOfMaxMoveFromOpen(bool useMaxMoveMent, double currentClosePrice, double maxDistance, Action<string> Log)
        {
            // Check if the maximum movement check should be used and barData is not empty
            if (!useMaxMoveMent || barData.Count == 0)
            {
                return false;
            }
        
            double distance;
            try
            {
                // Calculate the distance from open to highest
                distance = CalculateOpenToHighest(Log);
            }
            catch (Exception ex)
            {
                Log("[ERROR] Failed to calculate distance: " + ex.Message);
                return false;
            }
        
            // Check if the distance exceeds the maximum allowable distance
            if (distance.CompareTo(maxDistance) > 0)
            {
                Log("[MAX MOVEMENT OPEN TO HIGHEST] : TRADE STOPPED : " + distance);
                return true;
            }
        
            return false;
        }

    
        /// <summary>
        /// Calculates the maximum distance from the entry price to the furthest value in the bar data list.
        /// </summary>
        /// <param name="Log">The log action to record updates.</param>
        /// <returns>The distance from the entry price to the furthest value in the bar data list.</returns>
        private static double CalculateOpenToHighest(Action<string> Log)
        {
            // Retrieve the entry price
            double entry = StrategyManagement.GetEntryPrice();
            Log("[MAX MOVEMENT OPEN TO HIGHEST] : Entry: " + entry);
        
            // Ensure barData is not empty
            if (barData.Count == 0)
            {
                Log("[ERROR] barData is empty.");
                return 0;  // or an appropriate value indicating no calculation possible
            }
        
            try
            {
                // Find the value furthest from the entry
                double furthestValue = barData.OrderByDescending(v => Math.Abs(v - entry)).FirstOrDefault();
                
                // Calculate the distance from the entry point
                double distance = Math.Abs(furthestValue - entry);
                Log("[MAX MOVEMENT OPEN TO HIGHEST] : Furthest value: " + furthestValue);
                Log("[MAX MOVEMENT OPEN TO HIGHEST] : Distance: " + distance);
        
                return distance;
            }
            catch (Exception ex)
            {
                Log("[ERROR] Failed to calculate the maximum distance: " + ex.Message);
                return 0;  // or an appropriate value indicating an error
            }
        }

    
    
        /// <summary>
        /// Adds high and low values to the bar data list.
        /// </summary>
        /// <param name="high">The high value.</param>
        /// <param name="low">The low value.</param>
        public static void Add(double high, double low, Action<string> Log)
        {
            // Validate the input values (assuming positive values are required)
            if (high < 0 || low < 0)
            {
                throw new ArgumentException("High and Low values must be non-negative.");
            }
        
            // Add logging for debug purposes
            Log($"Adding high: {high}, low: {low}");
        
            barData.Add(high);
            barData.Add(low);
        }
        
        
        /// <summary>
        /// Determines if the current price is out of the maximum movement range.
        /// </summary>
        /// <param name="useMaxMoveMent">A flag to indicate if the maximum movement check should be used.</param>
        /// <param name="maxDistance">The maximum allowable distance.</param>
        /// <param name="Log">The log action to record updates.</param>
        /// <returns>Returns true if the current price is out of the maximum movement range; otherwise, false.</returns>
        public static bool IsOutOfMaxMoveHighToLowest(bool useMaxMoveMent, double maxDistance, Action<string> Log)
        {
            // Check if the maximum movement check should be used and barData is not empty
            if (!useMaxMoveMent || barData.Count == 0)
            {
                return false;
            }
        
            double distance;
            try
            {
                // Calculate the distance from highest to lowest
                distance = CalculateHighestToLowest(Log);
            }
            catch (Exception ex)
            {
                Log("[ERROR] Failed to calculate distance: " + ex.Message);
                return false;
            }
        
            // Check if the distance exceeds the maximum allowable distance
            if (distance.CompareTo(maxDistance) > 0)
            {
                Log("[MAX MOVEMENT HIGHEST TO LOWEST] : TRADE STOPPED : " + distance);
                return true;
            }
        
            return false;
        }
        
        
        /// <summary>
        /// Calculates the maximum distance between the highest and lowest values in the bar data list.
        /// </summary>
        /// <param name="Log">The log action to record updates.</param>
        /// <returns>The distance between the highest and lowest values in the bar data list.</returns>
        private static double CalculateHighestToLowest(Action<string> Log)
        {
            // Ensure barData is not empty
            if (barData.Count == 0)
            {
                Log("[ERROR] barData is empty.");
                return 0;  // or an appropriate value indicating no calculation possible
            }
        
            try
            {
                // Find the lowest and highest values in the list
                double lowestValue = barData.Min();
                double highestValue = barData.Max();
        
                // Calculate the distance between the lowest and highest values
                double distance = Math.Abs(highestValue - lowestValue);
        
                // Log the calculated distance
                Log("[MAX MOVEMENT] : Distance between highest and lowest: " + distance);
        
                return distance;
            }
            catch (Exception ex)
            {
                Log("[ERROR] Failed to calculate the maximum distance: " + ex.Message);
                return 0;  // or an appropriate value indicating an error
            }
        }

    }

}


namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None, AddIndicators = true)]
    public class StocksOpeningJacksIdeaRetest100 : Robot
    {
        [Parameter("Shares", DefaultValue = 500, MinValue = 1, Step = 1, Group = "Risk")]
        public double Shares { get; set; }
    
        [Parameter("Take profit in $  1 = 1$", DefaultValue = 3, MinValue = 0.1, MaxValue = 10, Step = 0.1, Group = "Risk")]
        public double TakeProfit { get; set; }
    
        [Parameter("Stop Loss in $  1 = 1$", DefaultValue = 3, MinValue = 0.1, MaxValue = 10, Step = 0.1, Group = "Risk")]
        public double StopLoss { get; set; }
        
        [Parameter("No Trade entry after Hour X", DefaultValue = 19, MinValue = 1, MaxValue = 25, Step = 1, Group = "Risk")]
        public int NoTradeAfterHour { get; set; }
        
        [Parameter("Close All Trades Hour", DefaultValue = 19, MinValue = 1, MaxValue = 25, Step = 1, Group = "Risk")]
        public int CloseAllTradesHour { get; set; }
        
        [Parameter("(1) Use Max movement filter (Close to Furthest)", DefaultValue = false, Group = "Max Movement (O/H)")]
        public bool UseMaxMovement { get; set; }
        
        [Parameter("(1) Maximum movement (Close to Furthest)", DefaultValue = 1, MinValue = 0.1, MaxValue = 10, Step = 0.1, Group = "Max Movement (O/H)")]
        public double MaxMovementDollars { get; set; }
        
        [Parameter("(2) Use Max movement filter (Highest to Lowest)", DefaultValue = false, Group = "Max Movement (H/L)")]
        public bool UseMaxMovementHighestToLowest { get; set; }
        
        [Parameter("(2) Maximum movement (Highest to Lowest)", DefaultValue = 1, MinValue = 0.1, MaxValue = 10, Step = 0.1, Group = "Max Movement (H/L)")]
        public double MaxMovementDollarsHighestToLowest { get; set; }

        protected override void OnStart()
        {
            // Subscribes to the Positions.Opened event to handle newly opened positions.
            Positions.Opened += PositionOpened;
        }
        
        protected override void OnTick()
        {
            // Resets the day based on current bar data and logs the action.
            StrategyManagement.ResetDay(Bars, Log);
        
            // Updates today's market direction based on current bar data and logs the action.
            StrategyManagement.UpdatesTodaysDirection(Bars, Log);
        
            // Updates the entry price based on current bar data and logs the action.
            StrategyManagement.UpdateEntryPrice(Bars, Log);
        
            // Executes the trading strategy.
            ExecuteTrade();
        }
        
        protected override void OnBar()
        {
            // Closes all trades at a specified hour with a given reason and logs the action.
            Positions.CloseAllTrades(Bars, "Stocks Opening - (Jacks Idea) Retest", CloseAllTradesHour, Log);
        
            // Checks the maximum allowed movement.
            MaxMovement();
        }


        #region Trade Functions
        /// <summary>
        /// Handles the event when a new position is opened.
        /// </summary>
        /// <param name="newPosition">The event arguments containing the new position information.</param>
        void PositionOpened(PositionOpenedEventArgs newPosition)
        {
            // Update the strategy management with the new position.
            StrategyManagement.UpdatedPosition(newPosition.Position);
        }
        
        
        /// <summary>
        /// Executes a trade based on the current market conditions and strategy rules.
        /// </summary>
        void ExecuteTrade()
        {
            // Check if trading is allowed based on the current time or if the entry price is not set.
            if (Bars.NoTradesAfterHour(NoTradeAfterHour) || StrategyManagement.GetEntryPrice() == 0)
            {
                // Log that trading is not allowed after the specified hour or if the trade is not ready.
                // Log("[AFTER ALLOWED TRADING HOUR OR TRADE NOT READY]");
                return;   
            }
            
            double entryPrice = StrategyManagement.GetEntryPrice();
            var tradeType = GetTradeType();
            bool hasTradedToday = StrategyManagement.GetTradedToday();
            
            // Execute a buy order if the conditions are met and no trade has been made today.
            if (Symbol.Ask <= entryPrice && tradeType == TradeType.Buy && !hasTradedToday)
            {
                StrategyManagement.UpdatedTradedToday();
                ExecuteOrder(TradeType.Buy);
            }
            // Execute a sell order if the conditions are met and no trade has been made today.
            else if (Symbol.Ask >= entryPrice && tradeType == TradeType.Sell && !hasTradedToday)
            {
                StrategyManagement.UpdatedTradedToday();
                ExecuteOrder(TradeType.Sell);
            }
        }
        
        
        /// <summary>
        /// Determines the trade type (Buy or Sell) based on the strategy's trade direction.
        /// </summary>
        /// <returns>The trade type (Buy or Sell) or null if the trade direction is not set.</returns>
        TradeType? GetTradeType()
        {
            return StrategyManagement.GetTradeDirection() switch
            {
                TradeDirection.LONG => TradeType.Buy,
                TradeDirection.SHORT => TradeType.Sell,
                _ => null,
            };
        }

        
        /// <summary>
        /// Executes a market order based on the specified trade type if filters allow.
        /// </summary>
        /// <param name="tradetype">The type of trade to execute (Buy or Sell).</param>
        void ExecuteOrder(TradeType tradetype)
        {
            // Check if any filters prevent the order from being executed.
            if (Filters())
            {
                return;    
            }
        
            // Execute the market order with the specified parameters.
            ExecuteMarketOrder(tradetype, Symbol.Name, Shares, "Stocks Opening - (Jacks Idea) Retest", StopLoss, TakeProfit);
        }


        #endregion


        #region Misc
        /// <summary>
        /// Logs a message to the console.
        /// </summary>
        /// <param name="text">The message to log.</param>
        void Log(string text)
        {
            // Print the provided text to the console.
            Print(text);
        }
        

        /// <summary>
        /// Applies filters to determine if the current price movement is within the allowed range.
        /// </summary>
        /// <returns>True if the movement is out of the allowed range, otherwise false.</returns>
        bool Filters()
        {
            // Check if the current close price is out of the maximum movement range.
            bool maxMovement = MaxMovementFilter.IsOutOfMaxMoveFromOpen(UseMaxMovement, Bars.ClosePrices[Bars.Count - 1], MaxMovementDollars, Log);
            bool maxMovementHighToLow = MaxMovementFilter.IsOutOfMaxMoveHighToLowest(UseMaxMovementHighestToLowest, MaxMovementDollarsHighestToLowest, Log);
            // Return the result of the max movement filter check.
            return maxMovement || maxMovementHighToLow;
        }
        

        /// <summary>
        /// Updates the maximum movement filter with the latest high and low prices.
        /// </summary>
        void MaxMovement()
        {
            // Get the high and low prices from the second last bar.
            double high = Bars.HighPrices[Bars.Count - 2];
            double low = Bars.LowPrices[Bars.Count - 2];
        
            // Add the high and low prices to the max movement filter.
            MaxMovementFilter.Add(high, low, Log);
        }

        #endregion
    }
}