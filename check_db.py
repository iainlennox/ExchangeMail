import sqlite3

try:
    conn = sqlite3.connect('C:/ExchangeMailData/exchangemail.db')
    cursor = conn.cursor()
    
    # Check messages
    print("\n--- Messages ---")
    print("\n--- UserMessages ---")
    cursor.execute("SELECT UserId, Folder, IsFocused FROM UserMessages ORDER BY MessageId DESC LIMIT 10")
    rows = cursor.fetchall()
    for row in rows:
        print(f"User: {row[0]}")
        print(f"Folder: {row[1]}")
        print(f"Focused: {row[2]}")
        print("-" * 10)
        
    conn.close()
except Exception as e:
    print(f"Error: {e}")
