import random
import json
import uuid
from datetime import datetime, timedelta

# Realistic price quotes (sample values)
price_quotes = {
    "AMZN": 319.68,
    "TSLA": 701.92,
    "AAPL": 150.41,
    "MSFT": 321.60
}

# Strategy definitions
ticker_strategies = {
    "AMZN": "Momentum",
    "TSLA": "Reversal",
    "AAPL": "Breakout",
    "MSFT": "Swing"
}

# Generate random positions
positions = []
open_positions = 0
closed_positions = 0

while len(positions) < 150:
    ticker = random.choice(list(price_quotes.keys()))
    strategy = ticker_strategies[ticker]
    quantity = random.randint(1, 50)
    open_price = price_quotes[ticker] * random.uniform(0.9, 1.1)
    current_price = price_quotes[ticker]
    close_price = open_price * random.uniform(0.95, 1.05)

    open_time = datetime.now() - timedelta(days=random.randint(1, 30))
    close_time = open_time + timedelta(days=random.randint(1, 30))

    # Determine if the position is open or closed
    if random.random() > 0.4:  # More closed positions than open
        state = "Closed"
        profit_or_loss = (close_price - open_price) * quantity
    else:
        state = "Open"
        profit_or_loss = (current_price - open_price) * quantity
        close_price = 0.0  # No close price for open positions
        close_time = datetime(1, 1, 1)  # Default close time for open positions
    
    positions.append({
        "PositionGuid": str(uuid.uuid4()),
        "Symbol": ticker,
        "InstrumentType": "Stock",
        "State": state,
        "OpenTime": open_time.isoformat(),
        "CloseTime": close_time.isoformat(),
        "Currency": "USD",
        "OpenPrice": round(open_price, 2),
        "ClosePrice": round(close_price, 2),
        "CurrentPrice": round(current_price, 2),
        "Quantity": quantity,
        "ProfitOrLoss": round(profit_or_loss, 2),
        "StrategyName": strategy,
        "StrategyDefinition": f"Buy on {strategy.lower()}",
        "PlatformOrderId": f"ORD{random.randint(100000, 999999)}"
    })

# Save positions to JSON file
with open("positions.json", "w") as file:
    json.dump(positions, file, indent=4)

print("Generated JSON file with 150 positions.")
