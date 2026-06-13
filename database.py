import sqlite3

def create_database():
    conn = sqlite3.connect('bhai_gee_traders.db')
    cursor = conn.cursor()
    
    # 1. Users Table (Logins)
    cursor.execute('''
    CREATE TABLE IF NOT EXISTS Users (
        user_id INTEGER PRIMARY KEY AUTOINCREMENT,
        username TEXT UNIQUE NOT NULL,
        password TEXT NOT NULL,
        role TEXT NOT NULL
    )
    ''')
    
    # 2. Products Table (Inventory Description & Barcode)
    cursor.execute('''
    CREATE TABLE IF NOT EXISTS Products (
        item_id INTEGER PRIMARY KEY AUTOINCREMENT,
        barcode TEXT UNIQUE NOT NULL,    
        description TEXT NOT NULL,       
        category TEXT NOT NULL,          
        stock_qty INTEGER NOT NULL,      
        purchase_rate REAL NOT NULL,     
        sale_rate REAL NOT NULL,         
        date_entered TEXT NOT NULL       
    )
    ''')
    
    # 3. Product_Price_History Table (Old Rates History)
    cursor.execute('''
    CREATE TABLE IF NOT EXISTS Product_Price_History (
        history_id INTEGER PRIMARY KEY AUTOINCREMENT,
        barcode TEXT NOT NULL,
        old_purchase_rate REAL NOT NULL,
        old_sale_rate REAL NOT NULL,
        date_changed TEXT NOT NULL,
        FOREIGN KEY (barcode) REFERENCES Products (barcode)
    )
    ''')
    
    # 4. Sales_Master Table (Bill main record)
    cursor.execute('''
    CREATE TABLE IF NOT EXISTS Sales_Master (
        invoice_id INTEGER PRIMARY KEY AUTOINCREMENT,
        date_time TEXT NOT NULL,
        total_bill REAL NOT NULL,
        discount REAL DEFAULT 0,
        grand_total REAL NOT NULL,
        cash_received REAL NOT NULL,    
        cash_returned REAL NOT NULL,    
        total_profit REAL NOT NULL      
    )
    ''')
    
    # 5. Sales_Details Table (Old Price Adjustment Return)
    cursor.execute('''
    CREATE TABLE IF NOT EXISTS Sales_Details (
        detail_id INTEGER PRIMARY KEY AUTOINCREMENT,
        invoice_id INTEGER NOT NULL,
        barcode TEXT NOT NULL,
        qty_sold INTEGER NOT NULL,
        rate_at_sale REAL NOT NULL,     
        FOREIGN KEY (invoice_id) REFERENCES Sales_Master (invoice_id)
    )
    ''')
    
    # Default Usernames & Passwords
    cursor.execute("INSERT OR IGNORE INTO Users (username, password, role) VALUES ('admin', '1234', 'owner')")
    cursor.execute("INSERT OR IGNORE INTO Users (username, password, role) VALUES ('sales', '0000', 'salesman')")
    
    conn.commit()
    conn.close()
    print("Bunyad Taiyar Hai: Bhai Gee Traders Database Kamyabi se ban gaya hai!")

if __name__ == "__main__":
    create_database()