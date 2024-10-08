#region Using declarations
using System;
using System.IO;
using System.Net.Http;
using System.Xml.Linq;
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
	#region Economic News Class
	
	public class EconomicNews
	{
	    public string EventName { get; set; }
	    public DateTime Date { get; set; }
	    public string Impact { get; set; }
	
	    public override string ToString()
	    {
	        return $"{EventName},{Date.ToString()},{Impact}";
	    }
	}
	
	#endregion

	#region Parse Economic News
	
	public class EconomicNewsParser
	{
	    private static readonly HttpClient client = new HttpClient();
	    
	    public async Task<List<EconomicNews>> GetEconomicNewsAsync()
	    {
	        string url = "https://nfs.faireconomy.media/ff_calendar_thisweek.xml";
	        HttpResponseMessage response = await client.GetAsync(url);
	        
	        // Check if the request was successful (status code 200)
	        if (response.IsSuccessStatusCode)
	        {
	            // Parse XML if response was successful
	            string xmlData = await response.Content.ReadAsStringAsync();
	            return ParseEconomicNews(xmlData);
	        }
	        // Handle specific status codes
	        else if (response.StatusCode == (System.Net.HttpStatusCode)429) // Too Many Requests
	        {
	            Console.WriteLine("Received 'Too Many Requests' (429). Retry after delay...");
	            
				//return await GetEconomicNewsAsync(); // Retry the request
				return new List<EconomicNews>();
	        }
	        else
	        {
	            // Log the status code and reason
	            Console.WriteLine($"Failed to fetch data. Status Code: {response.StatusCode}, Reason: {response.ReasonPhrase}");
	            return new List<EconomicNews>(); // Return an empty list or handle the failure appropriately
	        }
	    }
	
	    // Helper method to parse XML data into a list of EconomicNews objects
	    private List<EconomicNews> ParseEconomicNews(string xmlData)
	    {
	        XDocument doc = XDocument.Parse(xmlData);
	        List<EconomicNews> newsList = new List<EconomicNews>();
	
	        foreach (var item in doc.Descendants("event"))
	        {
	            if (item.Element("country")?.Value == "USD")
				{
					EconomicNews news = new EconomicNews
		            {
		                EventName = item.Element("title")?.Value,
		                Date = DateTime.Parse($"{item.Element("date")?.Value}  {item.Element("time")?.Value}"),
		                Impact = item.Element("impact")?.Value
		            };
		
		            newsList.Add(news);
				}
	        }
	
	        return newsList;
	    }
	}

	#endregion
	
	#region Trade Data Class
	
	public class TradeData
	{
	    public double Volume { get; set; }
	    public double TimeToFormBar { get; set; }
		public double Price { get; set; }
	    public bool IsProfitable { get; set; } // Label: 1 for profitable, 0 for not profitable
	}
	
	#endregion
	
	[Gui.CategoryOrder("Time", 1)]
	[Gui.CategoryOrder("Trade Management", 2)]
	[Gui.CategoryOrder("News", 3)]
	[Gui.CategoryOrder("Risk Management", 4)]
	
	public class AutoNQ : Strategy
	{
		#region Variables
		
			#region Constants
			
			private static int LONG = -1;
			private static int SHORT = -2;
			private double BRICK_SIZE;
			private double TREND_THRESHOLD;
			
			#endregion
		
			#region Opening Range Breakout
			
			private int openBar = -1;
			private bool brokeRange = false;
			private double highOfRange = 0;
			private double lowOfRange = 0;
			
			#endregion
		
			#region Contract Management
			
			private int contracts;
			private List<int> nextContractSize;
			private int tradeIndex = 0;
			private string previousId = "";
			
			#endregion
		
			#region PnL
			
			private double startPL = 0;
			private double realizedPL = 0;
			
			#endregion
		
			#region Trade Booleans
		
			private bool tradeLock = false;
			private bool tradeWindow = false;
		
			#endregion
		
			#region Econ News
		
			private List<EconomicNews> news;
		
			#endregion
		
			#region KNN
		
			private List<TradeData> tradeDataSet;
			private List<TradeData> openTrades;
		
			#endregion
		
			#region Custom Fonts
			
			NinjaTrader.Gui.Tools.SimpleFont text = new NinjaTrader.Gui.Tools.SimpleFont("DejaVu Sans Mono", 16) { Size = 12, Bold = true };
			NinjaTrader.Gui.Tools.SimpleFont title = new NinjaTrader.Gui.Tools.SimpleFont("DejaVu Sans Mono", 16) { Size = 18, Bold = true };
			
			#endregion
		
		#endregion
		
		public override string DisplayName { get { return "AutoNQ"; } }
			
		#region On State Change
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "AutoNQ";
				Calculate									= Calculate.OnEachTick;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelCloseIgnoreRejects;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				StartTime									= DateTime.Parse("3:00", System.Globalization.CultureInfo.InvariantCulture);
				EndTime										= DateTime.Parse("16:00", System.Globalization.CultureInfo.InvariantCulture);
				MarketOpen									= DateTime.Parse("9:30", System.Globalization.CultureInfo.InvariantCulture);
				StopLoss									= 150;
				TakeProfit									= 50;
				BaseContracts								= 1;
				Multiplier1									= 4;
				Multiplier2									= 3;
				VolumeThreshold								= 12000;
				ReversalConfirmBars							= 2;
				DailyProfitTarget							= 0;
				ProfitTarget								= 0;
				DailyMaxLoss								= 0;
				MaxLoss										= 0;
				AvoidHighImpact								= true;
				AvoidMediumImpact							= false;
				AvoidLowImpact								= false;
			}
			else if (State == State.Configure)
			{
				Name = "";
				AddDataSeries(BarsPeriodType.Second, 1); // for time check accuracy
				
				// initialize contract management
				contracts = BaseContracts;
				
				nextContractSize = new List<int>();	
				nextContractSize.Add(BaseContracts);
				
				news = new List<EconomicNews>();
			}
			else if (State == State.DataLoaded)
			{
				BRICK_SIZE = Bars.BarsPeriod.Value * TickSize;
				TREND_THRESHOLD = Bars.BarsPeriod.Value2 * TickSize;
			}
		}
		
		#endregion

		#region On Bar Update
		
		protected override void OnBarUpdate()
		{
			if (CurrentBar < Math.Max(2, ReversalConfirmBars)) 
				return;
			
			if (BarsInProgress == 1)
			{
				checkSession();
			}
			
			if (BarsInProgress == 0)
			{
				if (Bars.IsFirstBarOfSession) 
				{
					startPL = SystemPerformance.RealTimeTrades.TradesPerformance.Currency.CumProfit;
					openBar = -1;
					highOfRange = 0;
					lowOfRange = Double.MaxValue;
					brokeRange = false;
					tradeLock = false;
					
					if (IsFirstTickOfBar)
						getNews();
				}
				
				profitLoss();
				contractManagement();
				trade();
				info();
			}
		}
		
		#endregion
		
		#region Get Economic News
		
		private async void getNews()
		{
			// check if we need to get the most recent news
			string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			string ninjaTraderScriptsPath = System.IO.Path.Combine(documentsPath, "NinjaTrader 8", "bin", "Custom");
        	string path = System.IO.Path.Combine(ninjaTraderScriptsPath, "EconomicNews.txt");
			bool upToDate = true;
			
			try
			{
				if (!File.Exists(path)) // File does not exist
				{
					File.CreateText(path);
				}
				
				if (File.Exists(path))
				{
					string[] lines = File.ReadAllLines(path);
					
					// Check if the file is not empty
	                if (lines.Length > 0)
	                {
	                    // Get the last line
	                    string lastLine = lines[lines.Length - 1];
	
	                    // Parse the line
	                    string[] parts = lastLine.Split(',');
	
	                    if (parts.Length == 3)
	                    {
	                        DateTime date = DateTime.Parse(parts[1].Trim());
	                        
							if (Time[0].Date > date) // check if we are up to date
							{
								upToDate = false;
							}
							
							else
							{
								Print("Economic news up to date.");
								
								foreach (string line in lines)
								{
									Print(line);
									parts = line.Split(',');
									news.Add(new EconomicNews() { EventName = parts[0], Date = DateTime.Parse(parts[1]), Impact = parts[2] });
								}
							}
	                    }
	                }
					
					else if (!upToDate || lines.Length == 0)
					{
						EconomicNewsParser parser = new EconomicNewsParser();
				        news = await parser.GetEconomicNewsAsync();
							
						Print("Writing to file...");
						using (StreamWriter sw = File.CreateText(path))
						{
							foreach (var n in news)
							{
								Print(n);
								sw.WriteLine(n);
							}
						}	
					}
	            }
			}
			
			catch (Exception e)
			{
				Print(e.ToString());	
			}
		}
		
		#endregion
		
		#region Check Session
		
		private void checkSession()
		{
			if (tradeLock) return;
			
			if (checkTime(StartTime, EndTime))
			{
				tradeWindow = true;
			}
			
			else
			{
				tradeWindow = false;
			}
			
			foreach (EconomicNews n in news)
			{				
				if (AvoidHighImpact && n.Impact == "High" && checkTime(n.Date.AddMinutes(HighMinutesBefore * -1), n.Date.AddMinutes(HighMinutesAfter)))
				{
					tradeWindow = false;
					Draw.TextFixed(this, "Econ", 
						$"Avoiding {n.EventName} from {n.Date.AddMinutes(HighMinutesBefore * -1).ToShortTimeString()} to {n.Date.AddMinutes(HighMinutesAfter).ToShortTimeString()}", 
						TextPosition.BottomRight, Brushes.Red, text, Brushes.Red, Brushes.Black, 50);
				}
				
				else if (AvoidMediumImpact && n.Impact == "Medium" && checkTime(n.Date.AddMinutes(MediumMinutesBefore * -1), n.Date.AddMinutes(MediumMinutesAfter)))
				{
					tradeWindow = false;
					Draw.TextFixed(this, "Econ", 
						$"Avoiding {n.EventName} from {n.Date.AddMinutes(MediumMinutesBefore * -1).ToShortTimeString()} to {n.Date.AddMinutes(MediumMinutesAfter).ToShortTimeString()}", 
						TextPosition.BottomRight, Brushes.Red, text, Brushes.Red, Brushes.Black, 50);
				}
				
				else if (AvoidLowImpact && n.Impact == "Medium" && checkTime(n.Date.AddMinutes(LowMinutesBefore * -1), n.Date.AddMinutes(LowMinutesAfter)))
				{
					tradeWindow = false;
					Draw.TextFixed(this, "Econ", 
						$"Avoiding {n.EventName} from {n.Date.AddMinutes(LowMinutesBefore * -1).ToShortTimeString()} to {n.Date.AddMinutes(LowMinutesAfter).ToShortTimeString()}", 
						TextPosition.BottomRight, Brushes.Red, text, Brushes.Red, Brushes.Black, 50);
				}
				
				else
				{
					RemoveDrawObject("Econ");	
				}
			}
			
			Draw.TextFixed(this, "Trade Window", (tradeWindow ? "Status: Trading\n\n\n" : "Status: Inactive\n\n\n"), TextPosition.BottomLeft, tradeWindow ? Brushes.LimeGreen : Brushes.Red, text, null, null, 100);
		}
		
		private bool checkTime(DateTime StartTime, DateTime EndTime)
		{
			if (EndTime.TimeOfDay < StartTime.TimeOfDay)
			{	
				if (Time[0].TimeOfDay > StartTime.TimeOfDay && Time[0].TimeOfDay < DateTime.MaxValue.TimeOfDay || Time[0].TimeOfDay > DateTime.MinValue.TimeOfDay && Time[0].TimeOfDay < EndTime.TimeOfDay) 
					return true;

				else
					return false;
			}
			
			else if (EndTime.TimeOfDay > StartTime.TimeOfDay)
			{
				if (Time[0].TimeOfDay > StartTime.TimeOfDay && Time[0].TimeOfDay < EndTime.TimeOfDay)
					return true;

				else
					return false;
			}
			
			return false;
		}
		
		#endregion
		
		#region Trade
		
		private void trade() 
		{
			if (Position.MarketPosition == MarketPosition.Flat && tradeWindow && !tradeLock) 
			{	
				if (Time[0].TimeOfDay > MarketOpen.AddMinutes(-30).TimeOfDay && Time[0].TimeOfDay < MarketOpen.AddMinutes(1).TimeOfDay) 
				{			
					if (High[0] > highOfRange && isGreen(0)) 
						highOfRange = High[0]; 
					
					if (Low[0] < lowOfRange && isRed(0)) 
						lowOfRange = Low[0];						
				}
				
				else if (Time[0].TimeOfDay > MarketOpen.TimeOfDay && Time[0].TimeOfDay < MarketOpen.AddMinutes(7).TimeOfDay && !brokeRange) 
				{	
					if (Time[0].TimeOfDay > MarketOpen.TimeOfDay && openBar == -1)
						openBar = CurrentBar;
									
					else if (CurrentBar > openBar + 1) 
					{
						/// Long ORB 
						if (isGreen(0) && Close[0] >= highOfRange + TREND_THRESHOLD && (Time[0] - Time[1]).TotalSeconds < 1) 
						{	
							for (int i = 0; i < 10; i++) // make sure more than 10 bars have formed
							{
								if (!isGreen(i)) {
									brokeRange = true;
									return;	
								}
							}
							
							enterTrade(LONG);
							brokeRange = true;
							return;
						}
						
						/// Short ORB
						else if (isRed(0) && Close[0] <= lowOfRange - TREND_THRESHOLD && (Time[0] - Time[1]).TotalSeconds < 1) 
						{
							for (int i = 0; i < 10; i++) 
							{
								if (!isRed(i)) {
									brokeRange = true;
									return;
								}
							}
							
							enterTrade(SHORT);
							brokeRange = true;
							return;
						}	
					}
				}
							
				else if (isFullRed(ReversalConfirmBars + 1) && isGreen(ReversalConfirmBars) && (Time[0] - Time[ReversalConfirmBars]).TotalSeconds < 1 + ReversalConfirmBars) 
				{
					/// Long Reversal Pattern
					for (int i = 0; i < ReversalConfirmBars - 1; i++) 
					{
						if (!isGreen(i))
							return;	
					}
					
					if (Close[0] >= High[1] + TREND_THRESHOLD) 
					{
						enterTrade(LONG);
						return;
					}	
				}
					
				else if (isFullGreen(ReversalConfirmBars + 1) && isRed(ReversalConfirmBars) && (Time[0] - Time[ReversalConfirmBars]).TotalSeconds < 1 + ReversalConfirmBars) 
				{
					/// Short Reversal Pattern	
					for (int i = 0; i < ReversalConfirmBars - 1; i++) 
					{
						if (!isRed(i))
							return;	
					}
					
					if (Close[0] <= Low[1] - TREND_THRESHOLD) 
					{
						enterTrade(SHORT);
						return;
					}
				}
				
				else if (High[1] - Low[1] > BRICK_SIZE && High[1] - Low[1] < BRICK_SIZE * 2 && (Time[0] - Time[1]).TotalSeconds < 1) 
				{					
					/// Long pullback
					if (isGreen(1) && isGreen(2) && Close[0] >= High[1] + TREND_THRESHOLD) 
					{
						enterTrade(LONG);
						return;
					}
					
					/// Short pullback
					else if (isRed(1) && isRed(2) && Close[0] <= Low[1] - TREND_THRESHOLD) 
					{
						enterTrade(SHORT);
						return;
					}		
				}	
					
				else if (isFull(1) && VOL()[1] > VolumeThreshold && (Time[0] - Time[1]).TotalSeconds < 1) 
				{					
					/// Long consolidation
					if (isGreen(1) && Close[0] >= High[1] + TREND_THRESHOLD) 
					{
						enterTrade(LONG);
						return;
					}
					
					/// Short consolidation
					else if (isRed(1) && Close[0] <= Low[1] - TREND_THRESHOLD) 
					{			
						enterTrade(SHORT);
						return;
					}	
				}
			}
		}
		
		private void enterTrade(int direction) 
		{
			SetProfitTarget(CalculationMode.Ticks, TakeProfit);
			SetStopLoss(CalculationMode.Ticks, StopLoss);
			
			if (direction == LONG) 
			{
				EnterLong(contracts);
				return;
			}
			
			if (direction == SHORT) 
			{
				EnterShort(contracts);
				return;
			}	
		}
		
		private bool isGreen(int i) {
			return Open[i] < Close[i];	
		}
		
		private bool isRed(int i) {
			return Open[i] > Close[i];	
		}
		
		private bool isFull(int i) {
			return High[i] - Low[i] == BRICK_SIZE;	
		}
		
		private bool isFullGreen(int i) {
			return isGreen(i) && isFull(i);
		}
		
		private bool isFullRed(int i) {
			return isRed(i) && isFull(i);
		}
		
		#endregion
		
		#region Contract Management
		
		private void contractManagement() 
		{
			if (SystemPerformance.AllTrades.Count > 0) 
			{	
				for (int i = tradeIndex; i < SystemPerformance.AllTrades.Count; i++) 
				{	
					Trade trade = SystemPerformance.AllTrades[i];
					
					if (trade.Entry.OrderId == previousId)
						continue;	
					
					previousId = trade.Entry.OrderId;
					
					if (trade.ProfitTicks < 0) 
					{	
						if (nextContractSize[0] == BaseContracts) 
						{
							nextContractSize.Add(BaseContracts * Multiplier1);
						}
						
						else if (nextContractSize[0] == BaseContracts * Multiplier1) 
						{
							nextContractSize.Add(BaseContracts * Multiplier1 * Multiplier2);
						}
						
						else if (nextContractSize[0] == BaseContracts * Multiplier1 * Multiplier2) 
						{
							nextContractSize.Add(BaseContracts * Multiplier1 * Multiplier2);
							nextContractSize.Add(BaseContracts * Multiplier1 * Multiplier2);
						}
					}
					
					if (trade.ProfitTicks > 0 && nextContractSize.Count == 1)
						nextContractSize.Add(BaseContracts);
					
					nextContractSize.RemoveAt(0);
				}
				
				tradeIndex = SystemPerformance.AllTrades.Count - 1;
				contracts = nextContractSize[0];
			}				
		}
		
		#endregion
		
		#region Profit Loss
		
		private void profitLoss() 
		{			
			realizedPL = SystemPerformance.RealTimeTrades.TradesPerformance.Currency.CumProfit;
			
			if (MaxLoss != 0 && realizedPL <= MaxLoss * -1)
				tradeLock = true;
				
			if (DailyMaxLoss != 0 && realizedPL - startPL <= DailyMaxLoss * -1)
				tradeLock = true;	
			
			else if (DailyProfitTarget != 0 && realizedPL - startPL >= DailyProfitTarget)
				tradeLock = true;
			
			else if (ProfitTarget != 0 && realizedPL >= ProfitTarget)
				tradeLock = true;
		}
		
		#endregion
		
		#region Info
		
		private void info() 
		{
			Draw.TextFixed(this, "contracts", "\n\n\n\t\t\t\tCurrent Contracts: " + contracts.ToString(), TextPosition.BottomLeft, Brushes.Aquamarine, text, Brushes.DeepSkyBlue, Brushes.Black, 85);		
			Draw.TextFixed(this, "info3", 
				"\t\t\t\tBase Contracts: " + BaseContracts.ToString() + "\n" +
				"\t\t\t\tMultiplier 1: " + Multiplier1.ToString() + "\n" +
				"\t\t\t\tMultiplier 2: " + Multiplier2.ToString() + "\n", TextPosition.BottomLeft, Brushes.Honeydew, text, null, null, 0);
			Draw.TextFixed(this, "Total Realized PL", "Total Realized PL: " + realizedPL.ToString("0.00"), TextPosition.BottomLeft, realizedPL > 0 ? Brushes.LimeGreen : Brushes.Red, text, null, null, 0);			
			Draw.TextFixed(this, "Daily Realized PL", "Daily Realized PL: " + (realizedPL - startPL).ToString("0.00") + "\n", TextPosition.BottomLeft, (realizedPL - startPL) > 0 ? Brushes.LimeGreen : Brushes.Red, text, null, null, 100);		
			Draw.TextFixed(this, "title", "Auto NQ", TextPosition.TopLeft, Brushes.Aquamarine, title, Brushes.DeepSkyBlue, Brushes.Black, 50);		
		}
		
		#endregion
		
		#region Properties
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Start Time", Order=1, GroupName="Time")]
		public DateTime StartTime
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="End Time", Order=2, GroupName="Time")]
		public DateTime EndTime
		{ get; set; }
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Market Open", Order=3, GroupName="Time")]
		public DateTime MarketOpen
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, Int32.MaxValue)]
		[Display(Name="Stop Loss (Ticks)", Order=1, GroupName="Trade Management")]
		public int StopLoss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, Int32.MaxValue)]
		[Display(Name="Take Profit (Ticks)", Order=2, GroupName="Trade Management")]
		public int TakeProfit
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, Int32.MaxValue)]
		[Display(Name="Base Contracts", Order=4, GroupName="Trade Management")]
		public int BaseContracts
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, Int32.MaxValue)]
		[Display(Name="Multiplier 1", Order=5, GroupName="Trade Management")]
		public int Multiplier1
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, Int32.MaxValue)]
		[Display(Name="Multiplier 2", Order=6, GroupName="Trade Management")]
		public int Multiplier2
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, Int32.MaxValue)]
		[Display(Name="Reversal Confirmation Bars", Order=7, GroupName="Trade Management")]
		public int ReversalConfirmBars
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, Int32.MaxValue)]
		[Display(Name="Volume Threshold", Order=9, GroupName="Trade Management")]
		public int VolumeThreshold
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Avoid High Impact News", Order=1, GroupName="News")]
		public bool AvoidHighImpact
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, Int32.MaxValue)]
		[Display(Name="Minutes Before", Order=2, GroupName="News")]
		public int HighMinutesBefore
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, Int32.MaxValue)]
		[Display(Name="Minutes After", Order=3, GroupName="News")]
		public int HighMinutesAfter
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Avoid Medium Impact News", Order=4, GroupName="News")]
		public bool AvoidMediumImpact
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, Int32.MaxValue)]
		[Display(Name="Minutes Before", Order=5, GroupName="News")]
		public int MediumMinutesBefore
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, Int32.MaxValue)]
		[Display(Name="Minutes After", Order=6, GroupName="News")]
		public int MediumMinutesAfter
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Avoid Low Impact News", Order=7, GroupName="News")]
		public bool AvoidLowImpact
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, Int32.MaxValue)]
		[Display(Name="Minutes Before", Order=8, GroupName="News")]
		public int LowMinutesBefore
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, Int32.MaxValue)]
		[Display(Name="Minutes After", Order=9, GroupName="News")]
		public int LowMinutesAfter
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, Int32.MaxValue)]
		[Display(Name="Daily Profit Target ($)", Order=1, GroupName="Risk Management")]
		public int DailyProfitTarget
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, Int32.MaxValue)]
		[Display(Name="Daily Max Loss ($)", Order=2, GroupName="Risk Management")]
		public int DailyMaxLoss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, Int32.MaxValue)]
		[Display(Name="Total Profit Target ($)", Order=3, GroupName="Risk Management")]
		public int ProfitTarget
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, Int32.MaxValue)]
		[Display(Name="Total Max Loss ($)", Order=4, GroupName="Risk Management")]
		public int MaxLoss
		{ get; set; }
		
		#endregion
	}
}
