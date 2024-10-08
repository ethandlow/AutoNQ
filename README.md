# AutoNQ
![image](https://github.com/user-attachments/assets/8da7ae07-99e3-4641-9766-c23e75611a85)
AutoNQ is a highly specialized, automated futures scalping algorithm designed for traders seeking to exploit momentum setups on large Renko bars and capture high-probability setups.

# Renko
Renko bars are a type of price chart used in technical analysis that focus solely on price movement, ignoring time and volume. Unlike traditional candlestick or bar charts, which create a new bar at regular intervals (based on time), Renko bars are created only when price moves by a predetermined amount, called the "trend threshold". The "brick size" determines how much price must move for a reversal. By focusing on price changes and filtering out minor fluctuations, Renko bars are useful for spotting trends, breakouts, and reversals in markets.

<p align="center">
  <img src="https://github.com/user-attachments/assets/32b49902-0f19-4058-9a54-85cc5ba571bb" alt="5 Minute Chart" width="49%"/>
  <img src="https://github.com/user-attachments/assets/e6737158-0f3e-429b-a71e-a83625d02320" alt="200/5 Renko Chart" width="49%"/>
</p>

The overall trend is MUCH easier to spot on the 200/5 Renko chart (right) as opposed to the 5-minute chart (left).

One downside of traditional Renko bars is that the highs and lows are artificially filled, so you miss out on crucial information hidden inside of the bars. To combat this, I've designed my own RealRenko bars that have the true open, high, low, and close information right on the chart to provide a more comprehensive view of the markets.

<p align="center">
  <img src="https://github.com/user-attachments/assets/520f39a7-9476-4cd7-8d72-56dc2f866f27" alt="Traditional Renko" width="49%"/>
  <img src="https://github.com/user-attachments/assets/253a6b95-d5b9-4438-a7e1-8ee3f3a74547" alt="Real Renko" width="49%"/>
</p>

# Automated News Filter

The AutoNQ strategy includes a built-in news filter that efficiently manages the impact of US economic news events on trading. To ensure that trades are not executed during volatile periods triggered by important economic announcements, this filter automatically scans for US economic news and adjusts trading behavior accordingly.

#### Key Features:
- **Automatic News Scanning**: The news filter automatically scans online sources for upcoming US economic news events.
- **Local Data Storage**: To minimize the number of requests sent to the news provider's website, the news data is saved locally on your computer. This ensures the system is optimized for speed and efficiency, reducing the need to frequently poll for updates.
- **News Impact Categories**: The filter categorizes news events by their impact level (high, medium, or low) and allows you to set custom rules to avoid trading before and after these events. For example, you can configure the strategy to avoid trading 30 minutes before and after high-impact news events.
- **Customizable Timing**: Users can customize the number of minutes before and after a news event during which trading should be halted.

#### How It Works:
1. **Initial News Fetch**: When the strategy is launched, it automatically pulls the latest US economic news from a designated website.
2. **Local Storage**: The news is stored in a local file on your computer, ensuring that future requests are minimized and trading efficiency is maintained.
3. **Dynamic Updates**: The filter checks periodically for new events and updates the local file accordingly, keeping your trading strategy synchronized with the latest market conditions.

This feature helps to protect your trades from unexpected volatility due to news events while also optimizing system performance by minimizing external requests.

# Parameters

The AutoNQ strategy includes various customizable parameters that allow you to fine-tune the trading behavior to your preferences and specific market conditions. Below is a detailed explanation of these parameters, categorized by their purpose.

#### 1. **Time Settings**
These parameters allow you to define the time window during which the strategy will be active, as well as market session times.

- **Start Time**: Defines the time when the strategy begins trading.
- **End Time**: Defines the time when the strategy stops trading.
- **Market Open**: Sets the time when the market opens, used to define market session boundaries.

#### 2. **Trade Management Settings**
These parameters are crucial for managing the trades placed by the strategy, including risk parameters and scaling rules.

- **Stop Loss (Ticks)**: The number of ticks for the stop-loss order from the entry price.
- **Take Profit (Ticks)**: The number of ticks for the take-profit order from the entry price.
- **Base Contracts**: Defines the number of base contracts to trade.
- **Multiplier 1**: Multiplier for position sizing when certain conditions are met.
- **Multiplier 2**: Second multiplier for advanced position scaling.
- **Reversal Confirmation Bars**: Number of bars to confirm a reversal before taking action.
- **Volume Threshold**: Minimum volume required for a trade setup to be valid.

#### 3. **News Filter Settings**
The news filter allows the strategy to avoid trading around economic news events, which can cause high volatility and unpredictable price movements.

- **Avoid High Impact News**: Avoids trading during high-impact news events.
- **Minutes Before/After High Impact News**: Defines how long before and after high-impact news the strategy should stop trading.
- **Avoid Medium Impact News**: Avoids trading during medium-impact news events.
- **Minutes Before/After Medium Impact News**: Defines how long before and after medium-impact news the strategy should stop trading.
- **Avoid Low Impact News**: Avoids trading during low-impact news events.
- **Minutes Before/After Low Impact News**: Defines how long before and after low-impact news the strategy should stop trading.

#### 4. **Risk Management Settings**
These parameters allow you to set profit and loss limits to protect your capital.

- **Daily Profit Target ($)**: Maximum profit goal for the trading day. Once reached, the strategy stops trading.
- **Daily Max Loss ($)**: Maximum loss allowed for the trading day. Once reached, the strategy stops trading.
- **Total Profit Target ($)**: Overall profit goal for the strategy session. Once reached, no further trades will be placed.
- **Total Max Loss ($)**: Maximum allowable loss for the strategy session. Once reached, the strategy halts trading.
