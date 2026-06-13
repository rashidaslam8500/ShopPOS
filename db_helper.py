import sqlite3

def get_connection():
    return sqlite3.connect("bhai_gee_traders.db")

def execute_query(query, params=()):
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute(query, params)
    conn.commit()
    conn.close()

def fetch_data(query, params=()):
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute(query, params)
    rows = cursor.fetchall()
    conn.close()
    return rows

def fetch_one(query, params=()):
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute(query, params)
    row = cursor.fetchone()
    conn.close()
    return row

def create_tables():
    # Products Table
    execute_query('''
        CREATE TABLE IF NOT EXISTS Products (
            barcode TEXT PRIMARY KEY,
            description TEXT,
            category TEXT,
            stock_qty INTEGER,
            purchase_rate REAL,
            sale_rate REAL,
            date_entered TEXT
        )
    ''')
    
    # Price History Table
    execute_query('''
        CREATE TABLE IF NOT EXISTS Product_Price_History (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            barcode TEXT,
            old_purchase_rate REAL,
            old_sale_rate REAL,
            date_changed TEXT
        )
    ''')

    # Naye Tables: Bills aur Bill_Items
    execute_query('''
        CREATE TABLE IF NOT EXISTS Bills (
            bill_no INTEGER PRIMARY KEY AUTOINCREMENT,
            date_time TEXT,
            total_amount REAL
        )
    ''')

    execute_query('''
        CREATE TABLE IF NOT EXISTS Bill_Items (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            bill_no INTEGER,
            barcode TEXT,
            qty INTEGER,
            price REAL,
            FOREIGN KEY(bill_no) REFERENCES Bills(bill_no)
        )
    ''')

# Database banate waqt ye function call karein
create_tables()