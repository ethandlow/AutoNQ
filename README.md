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
