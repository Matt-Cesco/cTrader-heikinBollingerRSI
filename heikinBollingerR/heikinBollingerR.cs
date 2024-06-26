using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class HeikinAshiBollingerRsiBot : Robot
    {
        private BollingerBands bollingerBands;
        private RelativeStrengthIndex rsi;

        private IndicatorDataSeries haClose;
        private IndicatorDataSeries haOpen;
        private IndicatorDataSeries haHigh;
        private IndicatorDataSeries haLow;

        [Parameter("Bollinger Bands Period", DefaultValue = 14)]
        public int BollingerBandsPeriod { get; set; }

        [Parameter("RSI Period", DefaultValue = 11)]
        public int RsiPeriod { get; set; }

        protected override void OnStart()
        {
            // Initialize the indicators
            bollingerBands = Indicators.BollingerBands(Bars.ClosePrices, BollingerBandsPeriod, 2, MovingAverageType.Simple);
            rsi = Indicators.RelativeStrengthIndex(Bars.ClosePrices, RsiPeriod);

            // Initialize Heikin Ashi data series
            haClose = CreateDataSeries();
            haOpen = CreateDataSeries();
            haHigh = CreateDataSeries();
            haLow = CreateDataSeries();

            // Calculate initial Heikin Ashi values for all bars
            for (int i = 0; i < Bars.ClosePrices.Count; i++)
            {
                CalculateHeikinAshi(i);
            }
        }

        private void CalculateHeikinAshi(int index)
        {
            if (index == 0)
            {
                haClose[index] = (Bars.OpenPrices[index] + Bars.ClosePrices[index] + Bars.HighPrices[index] + Bars.LowPrices[index]) / 4;
                haOpen[index] = (Bars.OpenPrices[index] + Bars.ClosePrices[index]) / 2;
                haHigh[index] = Bars.HighPrices[index];
                haLow[index] = Bars.LowPrices[index];
            }
            else
            {
                haClose[index] = (Bars.OpenPrices[index] + Bars.ClosePrices[index] + Bars.HighPrices[index] + Bars.LowPrices[index]) / 4;
                haOpen[index] = (haOpen[index - 1] + haClose[index - 1]) / 2;
                haHigh[index] = Math.Max(Bars.HighPrices[index], Math.Max(haOpen[index], haClose[index]));
                haLow[index] = Math.Min(Bars.LowPrices[index], Math.Min(haOpen[index], haClose[index]));
            }
        }

        protected override void OnTick()
        {
            int index = Bars.ClosePrices.Count - 1;

            // Calculate Heikin Ashi values for the current bar
            CalculateHeikinAshi(index);

            // Get the current Heikin Ashi values
            double haOpenValue = haOpen[index];
            double haCloseValue = haClose[index];
            double haHighValue = haHigh[index];
            double haLowValue = haLow[index];
            double upperBand = bollingerBands.Top.LastValue;
            double rollingMean = bollingerBands.Main.LastValue;
            double currentRsi = rsi.Result.LastValue;

            // Check if there are any open positions
            bool hasOpenPositions = Positions.Count > 0;

            if (!hasOpenPositions)
            {
                // Check buy conditions
                if (haCloseValue > haOpenValue && haOpenValue == haCloseValue && haCloseValue > upperBand && currentRsi > 60)
                {
                    double amountToInvest = Account.Balance * 0.50;
                    double volumeInUnits = amountToInvest / Symbol.Bid;

                    ExecuteMarketOrder(TradeType.Buy, SymbolName, volumeInUnits, "HeikinAshiBollingerRsiBot", null, null, null);
                    Chart.DrawIcon("Buy_" + Time, ChartIconType.UpArrow, Bars.OpenTimes.LastValue, Bars.LowPrices.LastValue, Color.Green);
                }
            }
            else
            {
                // Check sell conditions only if there are open buy positions
                var buyPositions = Positions.FindAll("HeikinAshiBollingerRsiBot", SymbolName, TradeType.Buy);
                if (buyPositions.Length > 0 && haLowValue < rollingMean && currentRsi < 60)
                {
                    ExecuteMarketOrder(TradeType.Sell, SymbolName, buyPositions[0].VolumeInUnits, "HeikinAshiBollingerRsiBot", null, null, null);
                    Chart.DrawIcon("Sell_" + Time, ChartIconType.DownArrow, Bars.OpenTimes.LastValue, Bars.HighPrices.LastValue, Color.Red);
                }
            }
        }

        protected override void OnStop()
        {
            // Code to run when the cBot is stopped
        }
    }
}
