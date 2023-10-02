#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class JonathanBello : Strategy
	{
		
		private EMA ema9B1;
		private EMA ema21B1;		
		private SMA sma21B2;
		bool candado;

		
		private DateTime startTime;
        private DateTime endTime;
		//-----------------------------------------------------
		
        private List<TimeRange> restrictedTimeRanges = new List<TimeRange>
        {
            new TimeRange(new TimeSpan(8, 28, 0), new TimeSpan(8, 35, 0)),   // 8:28 AM to 8:35 AM
            new TimeRange(new TimeSpan(9, 30, 0), new TimeSpan(9, 40, 0)),   // 9:30 AM to 9:40 AM
            new TimeRange(new TimeSpan(9, 58, 0), new TimeSpan(10, 2, 0))    // 9:58 AM to 10:02 AM
        };		
		
		//---------------------------------------------------
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Code for Jonathan Bello UpWork";
				Name										= "JonathanBello";
				Calculate									= Calculate.OnEachTick;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 60;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				
				
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
			}
			else if (State == State.Configure)
			{
				// Add a 1 minute Bars object to the strategy
				AddDataSeries(Data.BarsPeriodType.Minute, 1);
				// Add a 5 minute Bars object to the strategy
				AddDataSeries(Data.BarsPeriodType.Minute, 5);
				
			}
			else if (State == State.DataLoaded)
			{
				
				// Note: Bars are added to the BarsArray and can be accessed via an index value
				// E.G. BarsArray[1] ---> Accesses the 1 minute Bars object added above				
				ema9B1 = EMA(BarsArray[1], 9);
				ema21B1 = EMA(BarsArray[1], 21);
				sma21B2 = SMA(BarsArray[2], 21);

				// Add SMA & EMA to the chart for display
				// This only displays the SMA's & EMA's for the primary Bars object on the chart
				// Note only indicators based on the charts primary data series can be added.
				AddChartIndicator(ema9B1);
				AddChartIndicator(ema21B1);
				
				
				// Determine the start and end times based on the current date
                DateTime currentDate = Time[0].Date;
                startTime = currentDate.Add(new TimeSpan(6, 0, 0));     // Start time for trading (6:00 AM)
                endTime = currentDate.Add(new TimeSpan(15, 55, 0));    // End time for trading (3:55 PM)
			}
			

		}


		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1 || CurrentBars[1] < 1 || CurrentBars[2] < 1)
				return;
			
			// OnBarUpdate() will be called on incoming tick events on all Bars objects added to the strategy
			// We only want to process events on our primary Bars object (index = 0) which is set when adding
			// the strategy to a chart
			if (BarsInProgress != 0)
				return;
 //----------------------------------------------------------------------			
		    if (!IsWithinTradingHours() || IsWithinRestrictedTimeRanges()){
		        // Skip trading during restricted times
		        return;
		    }			
//--------------------------------------------------------------------------------------
			
			
			//Si no hay un trade abierto, entonces el candado esta abierto
			if(Position.MarketPosition == MarketPosition.Flat){
				candado = true;
			}
			
			//LONG
			if (candado && 
				Low[2] > ema9B1[2] && Low[3] > ema9B1[3] && Low[4] > ema9B1[4] &&
				Low[2] > ema21B1[2] && Low[3] > ema21B1[3] && Low[4] > ema21B1[4] &&
				Low[2] > sma21B2[2] && Low[3] > sma21B2[3] && Low[4] > sma21B2[4]){
				if (Low[1] <= ema9B1[1]){
					if(High[0] > High[1]){
						EnterLong(1, "longEntry");
						candado = false;
					}
				}
			}
			
			//SHORT	
			if (candado && 
				High[2] < ema9B1[2] && High[3] < ema9B1[3] && High[4] < ema9B1[4] &&
				High[2] < ema21B1[2] && High[3] < ema21B1[3] && High[4] < ema21B1[4] &&
				High[2] < sma21B2[2] && High[3] < sma21B2[3] && High[4] < sma21B2[4]){
				if (High[1] >= ema9B1[1]){
					if(Low[0] < Low[1]){
						EnterShort(1, "shortEntry");
						candado = false;
					}
				}
			}				
				
			SetStopLoss(CalculationMode.Currency, 200);	
			SetProfitTarget(CalculationMode.Currency, 200);
			
		}
		
		
		
		
//-----------------------------------------------------------------------------------
        private bool IsWithinTradingHours()
        {
            DateTime currentTime = Time[0];
            return currentTime.TimeOfDay >= startTime.TimeOfDay && currentTime.TimeOfDay <= endTime.TimeOfDay;
        }

        private bool IsWithinRestrictedTimeRanges()
        {
            DateTime currentTime = Time[0];
            return restrictedTimeRanges.Any(range =>
                currentTime.TimeOfDay >= range.StartTime &&
                currentTime.TimeOfDay <= range.EndTime
            );
        }

        private class TimeRange
        {
            public TimeSpan StartTime { get; private set; }
            public TimeSpan EndTime { get; private set; }

            public TimeRange(TimeSpan startTime, TimeSpan endTime)
            {
                StartTime = startTime;
                EndTime = endTime;
            }
        }
//--------------------------------------------------------------------------------
		
	}
		
	
}
