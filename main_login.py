import tkinter as tk
from tkinter import messagebox, ttk
import sqlite3
from datetime import datetime

# =========================================================================
# 1. SALESMAN / BILLING SCREEN (With Smart Alphabetical Name Search Dropdown)
# =========================================================================
class SalesmanWindow:
    def __init__(self, root):
        self.root = root
        self.root.title("Sales & Billing Counter — Bhai Gee Traders")
        self.root.state('zoomed')
        self.root.configure(bg="#f1f5f9")
        
        self.cart_items = []  # Bill mein shamil maal ki list

        # --- HEADER ---
        header = tk.Frame(self.root, bg="#0284c7", bd=0)
        header.pack(fill="x", ipady=10)
        tk.Label(header, text="BHAI GEE TRADERS & CROCKERY STORE", font=("Impact", 28, "bold"), bg="#0284c7", fg="white").pack()
        tk.Label(header, text="RETAIL BILLING COUNTER  |  Sargodha", font=("Arial", 11, "bold"), bg="#0284c7", fg="#e0f2fe").pack()

        # --- MAIN SPLIT LAYOUT ---
        main_frame = tk.Frame(self.root, bg="#f1f5f9")
        main_frame.pack(fill="both", expand=True, padx=20, pady=15)

        # LEFT COLUMN: Scanning & Bill Table
        left_panel = tk.Frame(main_frame, bg="#f1f5f9")
        left_panel.pack(side="left", fill="both", expand=True, padx=(0, 10))

        # SMART SEARCH & ADD SECTION
        scan_frame = tk.LabelFrame(left_panel, text=" 🔍 SEARCH ITEM (Barcode Likhein ya Naam Se Dhoondein) ", font=("Arial", 12, "bold"), bg="white", fg="#0369a1", bd=2, padx=15, pady=15)
        scan_frame.pack(fill="x", pady=(0, 10))

        tk.Label(scan_frame, text="Barcode / Item Name:", font=("Arial", 11, "bold"), bg="white").pack(side="left", padx=5)
        
        # Smart Combobox jo search bhi karega aur dropdown list bhi dikhaye ga
        self.ent_scan = ttk.Combobox(scan_frame, font=("Arial", 13), width=35)
        self.ent_scan.pack(side="left", padx=10)
        self.ent_scan.focus()

        # Keyboard key dabane par dropdown kholna aur milte julte naam dhoondna
        self.ent_scan.bind("<KeyRelease>", self.on_search_type)
        self.ent_scan.bind("<Return>", self.scan_item)

        tk.Button(scan_frame, text="➕ Add to Bill", font=("Arial", 11, "bold"), bg="#0284c7", fg="white", padx=15, command=lambda: self.scan_item(None)).pack(side="left", padx=5)

        # BILL TABLE (TREEVIEW)
        table_frame = tk.Frame(left_panel, bg="white")
        table_frame.pack(fill="both", expand=True)

        scroll = tk.Scrollbar(table_frame)
        scroll.pack(side="right", fill="y")

        self.bill_table = ttk.Treeview(table_frame, columns=("barcode", "desc", "qty", "rate", "total"), show="headings", yscrollcommand=scroll.set)
        scroll.config(command=self.bill_table.yview)

        self.bill_table.heading("barcode", text="Barcode")
        self.bill_table.heading("desc", text="Item Description / Maal ka Naam")
        self.bill_table.heading("qty", text="Qty")
        self.bill_table.heading("rate", text="Rate")
        self.bill_table.heading("total", text="Total Price")

        self.bill_table.column("barcode", width=120, anchor="center")
        self.bill_table.column("desc", width=300, anchor="w")
        self.bill_table.column("qty", width=80, anchor="center")
        self.bill_table.column("rate", width=100, anchor="center")
        self.bill_table.column("total", width=120, anchor="center")
        self.bill_table.pack(fill="both", expand=True)

        # Delete Button for Cart
        tk.Button(left_panel, text="❌ Remove Selected Item (Maal Khatam Karein)", font=("Arial", 11, "bold"), bg="#dc2626", fg="white", pady=5, command=self.remove_item).pack(anchor="w", pady=5)


        # RIGHT COLUMN: Calculations & Cash Calculation
        right_panel = tk.LabelFrame(main_frame, text=" 💰 BILL CALCULATION (Hisab Kitab) ", font=("Arial", 12, "bold"), bg="white", fg="#0369a1", bd=2, padx=20, pady=20)
        right_panel.pack(side="right", fill="both", width=380)

        # Grand Total Display
        tk.Label(right_panel, text="TOTAL BILL (Kul Paise)", font=("Arial", 14, "bold"), bg="white", fg="#1e293b").pack(anchor="w", pady=(10, 2))
        self.lbl_grand_total = tk.Label(right_panel, text="Rs. 0.00", font=("Arial", 36, "bold"), bg="white", fg="#16a34a")
        self.lbl_grand_total.pack(anchor="w", pady=(0, 20))

        # Cash Received Entry
        tk.Label(right_panel, text="CASH RECEIVED (Grahak se Liye):", font=("Arial", 12, "bold"), bg="white", fg="#1e293b").pack(anchor="w", pady=5)
        self.ent_cash_in = tk.Entry(right_panel, font=("Arial", 18, "bold"), bd=2, bg="#f8fafc", justify="center")
        self.ent_cash_in.pack(fill="x", pady=5)
        self.ent_cash_in.bind("<KeyRelease>", self.calculate_change)

        # Cash To Return Display
        tk.Label(right_panel, text="CASH TO RETURN (Baqaya Wapas):", font=("Arial", 12, "bold"), bg="white", fg="#1e293b").pack(anchor="w", pady=(20, 5))
        self.lbl_change = tk.Label(right_panel, text="Rs. 0.00", font=("Arial", 24, "bold"), bg="white", fg="#dc2626")
        self.lbl_change.pack(anchor="w", pady=(0, 30))

        # Save and Print Button
        btn_save_bill = tk.Button(right_panel, text="💾 SAVE & PRINT BILL\n(Ctrl + P)", font=("Arial", 16, "bold"), bg="#16a34a", fg="white", height=3, cursor="hand2", command=self.save_bill)
        btn_save_bill.pack(fill="x", pady=10)

    # --- SMART SEARCH: Type karte hi milte julte naam A to Z order mein lana ---
    def on_search_type(self, event):
        # Kuch ahem keys (jaise Enter, Up/Down arrow) par list filter nahi karni
        if event.keysym in ["Return", "Up", "Down", "Left", "Right", "Escape"]:
            return

        typed_text = self.ent_scan.get().strip()
        
        if not typed_text:
            self.ent_scan.event_generate('<Escape>') # List band karein agar text mita diya jaye
            return

        try:
            conn = sqlite3.connect('bhai_gee_traders.db')
            cursor = conn.cursor()
            
            # Barcode match karein YA naam ke andar kahin bhi woh lafz ho (LIKE clause) 
            # ORDER BY description ASC se saare naam Alphabetically (A se Z) aayenge
            query = """
                SELECT barcode, description FROM Products 
                WHERE barcode LIKE ? OR description LIKE ? 
                ORDER BY description ASC
            """
            cursor.execute(query, (f'%{typed_text}%', f'%{typed_text}%'))
            results = cursor.fetchall()
            conn.close()

            # Dropdown ke liye list tayyar karna: "Description | Barcode" ki shakal mein
            dropdown_values = []
            for res in results:
                dropdown_values.append(f"{res[1]} | {res[0]}")

            if dropdown_values:
                self.ent_scan['values'] = dropdown_values
                self.ent_scan.event_generate('<Down>') # Dropdown list ko khud-ba-khud neeche khol dena
            else:
                self.ent_scan['values'] = []
                
        except Exception as e:
            print("Search Error:", e)

    # --- ITEM ADD TO BILL ---
    def scan_item(self, event):
        input_value = self.ent_scan.get().strip()
        if not input_value:
            return

        barcode = ""
        # Agar salesman ne dropdown se select kiya hai toh format hoga: "Description | Barcode"
        if " | " in input_value:
            barcode = input_value.split(" | ")[-1].strip()
        else:
            barcode = input_value # Agar direct scan kiya ya sirf code likha

        try:
            conn = sqlite3.connect('bhai_gee_traders.db')
            cursor = conn.cursor()
            cursor.execute("SELECT barcode, description, sale_rate, stock_qty, purchase_rate FROM Products WHERE barcode=?", (barcode,))
            product = cursor.fetchone()
            conn.close()

            if product:
                b_code, desc, rate, stock, pur_rate = product
                
                if stock <= 0:
                    messagebox.showwarning("Stock Out", f"Afsoos! '{desc}' ka stock khatam hai.", parent=self.root)
                    self.ent_scan.set("")
                    return

                # Check karte hain ke kya item pehle se bill mein hai?
                for item in self.cart_items:
                    if item['barcode'] == b_code:
                        if item['qty'] + 1 > stock:
                            messagebox.showwarning("Out of Stock", "Dukan mein is se zyada maal nahi hai!", parent=self.root)
                            return
                        item['qty'] += 1
                        item['total'] = item['qty'] * item['rate']
                        self.update_table()
                        self.ent_scan.set("")
                        return

                # Agar naya item hai toh cart mein dalein
                self.cart_items.append({
                    'barcode': b_code,
                    'desc': desc,
                    'qty': 1,
                    'rate': rate,
                    'pur_rate': pur_rate,
                    'total': rate
                })
                self.update_table()
            else:
                messagebox.showerror("Not Found", "Yeh item ya barcode database mein nahi mila!", parent=self.root)
            
            self.ent_scan.set("") # Entry bilkul saaf
        except Exception as e:
            messagebox.showerror("Error", str(e))

    def update_table(self):
        for row in self.bill_table.get_children():
            self.bill_table.delete(row)

        grand_total = 0
        for item in self.cart_items:
            self.bill_table.insert("", "end", values=(item['barcode'], item['desc'], item['qty'], item['rate'], item['total']))
            grand_total += item['total']

        self.lbl_grand_total.config(text=f"Rs. {grand_total:.2f}")
        self.calculate_change(None)

    def remove_item(self):
        selected = self.bill_table.selection()
        if not selected:
            messagebox.showwarning("Select", "Pehle list se koi cheez select karein!")
            return
        for sel in selected:
            item_values = self.bill_table.item(sel, 'values')
            barcode = item_values[0]
            self.cart_items = [i for i in self.cart_items if i['barcode'] != barcode]
        self.update_table()

    def calculate_change(self, event):
        try:
            total_str = self.lbl_grand_total.cget("text").replace("Rs. ", "")
            total = float(total_str)
            cash_in = self.ent_cash_in.get().strip()
            if not cash_in:
                self.lbl_change.config(text="Rs. 0.00")
                return
            cash_in = float(cash_in)
            change = cash_in - total
            if change >= 0:
                self.lbl_change.config(text=f"Rs. {change:.2f}", fg="#16a34a")
            else:
                self.lbl_change.config(text=f"Rs. {change:.2f} (Paise Kam Hain)", fg="#dc2626")
        except ValueError:
            self.lbl_change.config(text="Rs. 0.00")

    def save_bill(self):
        if not self.cart_items:
            messagebox.showwarning("Khali Bill", "Bill khali hai! Pehle maal scan karein.")
            return
        try:
            total_str = self.lbl_grand_total.cget("text").replace("Rs. ", "")
            total_bill = float(total_str)
            cash_received = self.ent_cash_in.get().strip()

            if not cash_received or float(cash_received) < total_bill:
                messagebox.showerror("Paise Kam Hain", "Grahak se poore paise vasool karein!")
                return

            cash_received = float(cash_received)
            cash_returned = cash_received - total_bill
            current_time = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

            total_profit = 0
            for item in self.cart_items:
                profit_per_item = item['rate'] - item['pur_rate']
                total_profit += profit_per_item * item['qty']

            conn = sqlite3.connect('bhai_gee_traders.db')
            cursor = conn.cursor()

            cursor.execute('''
                INSERT INTO Sales_Master (date_time, total_bill, discount, grand_total, cash_received, cash_returned, total_profit)
                VALUES (?, ?, 0, ?, ?, ?, ?)
            ''', (current_time, total_bill, total_bill, cash_received, cash_returned, total_profit))
            
            invoice_id = cursor.lastrowid

            for item in self.cart_items:
                cursor.execute('INSERT INTO Sales_Details (invoice_id, barcode, qty_sold, rate_at_sale) VALUES (?, ?, ?, ?)', (invoice_id, item['barcode'], item['qty'], item['rate']))
                cursor.execute('UPDATE Products SET stock_qty = stock_qty - ? WHERE barcode = ?', (item['qty'], item['barcode']))

            conn.commit()
            conn.close()

            messagebox.showinfo("Bill Saved", f"Bill Number: {invoice_id} kamyabi se save ho gaya!")
            self.cart_items = []
            self.update_table()
            self.ent_cash_in.delete(0, tk.END)
            self.lbl_change.config(text="Rs. 0.00")
            self.ent_scan.focus()
        except Exception as e:
            messagebox.showerror("Error", f"Bill save karne mein masla: {str(e)}")


# =========================================================================
# 2. OWNER DASHBOARD SCREEN
# =========================================================================
class OwnerDashboard:
    def __init__(self, root):
        self.root = root
        self.root.title("Owner Dashboard — Bhai Gee Traders")
        self.root.state('zoomed')
        self.root.configure(bg="#f8fafc")

        header_frame = tk.Frame(self.root, bg="#0f172a", bd=0)
        header_frame.pack(fill="x", ipady=15)
        tk.Label(header_frame, text="BHAI GEE TRADERS & CROCKERY STORE", font=("Impact", 32, "bold"), bg="#0f172a", fg="#38bdf8").pack(pady=(15, 5))
        tk.Label(header_frame, text="OWNER CONTROL PANEL  |  Hafiz Muhammad Usman", font=("Arial", 13, "bold"), bg="#0f172a", fg="#94a3b8").pack(pady=(0, 15))

        main_frame = tk.Frame(self.root, bg="#f8fafc")
        main_frame.pack(pady=40, padx=50, fill="both", expand=True)

        left_frame = tk.LabelFrame(main_frame, text=" SHOP RECORDS & REPORTS ", font=("Arial", 14, "bold"), bg="#ffffff", fg="#1e293b", bd=2, padx=20, pady=20)
        left_frame.pack(side="left", fill="both", expand=True, padx=15)

        buttons_left = [
            ("📊 VIEW INVENTORY RECORD", self.view_inventory),
            ("🧾 VIEW ALL BILLS RECORD", self.view_bills),
            ("💰 DAILY SALES & PROFIT", lambda: self.view_profit("Daily")),
            ("📅 WEEKLY SALES & PROFIT", lambda: self.view_profit("Weekly")),
            ("📅 MONTHLY SALES & PROFIT", lambda: self.view_profit("Monthly")),
            ("📆 YEARLY SALES & PROFIT", lambda: self.view_profit("Yearly"))
        ]

        for text, command in buttons_left:
            tk.Button(left_frame, text=text, font=("Arial", 13, "bold"), bg="#1e293b", fg="white", height=2, cursor="hand2", command=command).pack(fill="x", pady=8)

        right_frame = tk.LabelFrame(main_frame, text=" INVENTORY ACTIONS ", font=("Arial", 14, "bold"), bg="#ffffff", fg="#1e293b", bd=2, padx=20, pady=20)
        right_frame.pack(side="right", fill="both", expand=True, padx=15)

        tk.Button(right_frame, text="➕ ADD NEW INVENTORY\n(Full Description & Rates)", font=("Arial", 14, "bold"), bg="#16a34a", fg="white", height=3, cursor="hand2", command=self.add_inventory_window).pack(fill="x", pady=20)
        tk.Button(right_frame, text="🔄 RETURN / ADJUST BILL\n(Old Price Adjustment System)", font=("Arial", 14, "bold"), bg="#dc2626", fg="white", height=3, cursor="hand2", command=self.return_bill_window).pack(fill="x", pady=20)

    def view_inventory(self): messagebox.showinfo("Inventory Record", "Poori dukan ka maal yahan show hoga...")
    def view_bills(self): messagebox.showinfo("Bill Record", "Purane saare bills ki list yahan khulegi...")
    def view_profit(self, report_type): messagebox.showinfo(f"{report_type} Report", f"{report_type} Sales aur Asli Munafa ka hisab yahan show hoga...")
    def return_bill_window(self): messagebox.showinfo("Return Bill", "Bill adjustment ki window yahan khulegi...")

    def add_inventory_window(self):
        self.add_win = tk.Toplevel(self.root)
        self.add_win.title("Add New Inventory - Bhai Gee Traders")
        self.add_win.geometry("500x620")
        self.add_win.configure(bg="#ffffff")
        self.add_win.resizable(False, False)
        self.add_win.grab_set()

        tk.Label(self.add_win, text="➕ NAYE MAAL KI ENTRY", font=("Arial", 16, "bold"), bg="#16a34a", fg="white", pady=10).pack(fill="x")
        form_frame = tk.Frame(self.add_win, bg="#ffffff", padx=30, pady=10)
        form_frame.pack(fill="both", expand=True)

        tk.Label(form_frame, text="Barcode / Item ID:", font=("Arial", 11, "bold"), bg="#ffffff", anchor="w").pack(fill="x", pady=(8,2))
        self.ent_barcode = tk.Entry(form_frame, font=("Arial", 12), bd=2)
        self.ent_barcode.pack(fill="x", pady=2)
        self.ent_barcode.focus()

        tk.Label(form_frame, text="Item Description:", font=("Arial", 11, "bold"), bg="#ffffff", anchor="w").pack(fill="x", pady=(8,2))
        self.ent_desc = tk.Entry(form_frame, font=("Arial", 12), bd=2)
        self.ent_desc.pack(fill="x", pady=2)

        tk.Label(form_frame, text="Category (Qism):", font=("Arial", 11, "bold"), bg="#ffffff", anchor="w").pack(fill="x", pady=(8,2))
        self.var_cat = tk.StringVar(value="Steel")
        opt_cat = tk.OptionMenu(form_frame, self.var_cat, "Steel", "Crockery")
        opt_cat.config(font=("Arial", 11), bg="#f1f5f9")
        opt_cat.pack(fill="x", pady=2)

        tk.Label(form_frame, text="Quantity (Tadaad):", font=("Arial", 11, "bold"), bg="#ffffff", anchor="w").pack(fill="x", pady=(8,2))
        self.ent_qty = tk.Entry(form_frame, font=("Arial", 12), bd=2)
        self.ent_qty.pack(fill="x", pady=2)

        tk.Label(form_frame, text="Purchase Rate (Khareed Keemat):", font=("Arial", 11, "bold"), bg="#ffffff", anchor="w").pack(fill="x", pady=(8,2))
        self.ent_pur_rate = tk.Entry(form_frame, font=("Arial", 12), bd=2)
        self.ent_pur_rate.pack(fill="x", pady=2)

        tk.Label(form_frame, text="Sale Rate (Bechnay ki Keemat):", font=("Arial", 11, "bold"), bg="#ffffff", anchor="w").pack(fill="x", pady=(8,2))
        self.ent_sale_rate = tk.Entry(form_frame, font=("Arial", 12), bd=2)
        self.ent_sale_rate.pack(fill="x", pady=2)

        btn_save = tk.Button(form_frame, text="💾 SAVE ITEM IN DATABASE", font=("Arial", 13, "bold"), bg="#16a34a", fg="white", pady=12, cursor="hand2", command=self.save_inventory_data)
        btn_save.pack(fill="x", pady=20)

    def save_inventory_data(self):
        barcode = self.ent_barcode.get().strip()
        desc = self.ent_desc.get().strip()
        category = self.var_cat.get()
        qty = self.ent_qty.get().strip()
        pur_rate = self.ent_pur_rate.get().strip()
        sale_rate = self.ent_sale_rate.get().strip()
        current_date = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

        if not barcode or not desc or not qty or not pur_rate or not sale_rate:
            messagebox.showerror("Error", "Saari fields dakhil karein!", parent=self.add_win)
            return
        try:
            qty = int(qty)
            pur_rate = float(pur_rate)
            sale_rate = float(sale_rate)
        except ValueError:
            messagebox.showerror("Error", "Numbers sahi likhein!", parent=self.add_win)
            return

        try:
            conn = sqlite3.connect('bhai_gee_traders.db')
            cursor = conn.cursor()
            cursor.execute("SELECT stock_qty, purchase_rate, sale_rate FROM Products WHERE barcode=?", (barcode,))
            existing_product = cursor.fetchone()

            if existing_product:
                old_qty, old_pur, old_sale = existing_product
                new_qty = old_qty + qty
                if old_pur != pur_rate or old_sale != sale_rate:
                    cursor.execute('INSERT INTO Product_Price_History (barcode, old_purchase_rate, old_sale_rate, date_changed) VALUES (?, ?, ?, ?)', (barcode, old_pur, old_sale, current_date))
                cursor.execute('UPDATE Products SET description=?, category=?, stock_qty=?, purchase_rate=?, sale_rate=?, date_entered=? WHERE barcode=?', (desc, category, new_qty, pur_rate, sale_rate, current_date, barcode))
                messagebox.showinfo("Success", f"Stock Update ho gaya! Total: {new_qty}", parent=self.add_win)
            else:
                cursor.execute('INSERT INTO Products (barcode, description, category, stock_qty, purchase_rate, sale_rate, date_entered) VALUES (?, ?, ?, ?, ?, ?, ?)', (barcode, desc, category, qty, pur_rate, sale_rate, current_date))
                messagebox.showinfo("Success", "Naya Item save ho gaya!", parent=self.add_win)
            conn.commit()
            conn.close()
            self.add_win.destroy()
        except Exception as e:
            messagebox.showerror("Database Error", str(e))


# =========================================================================
# 3. MAIN LOGIN SCREEN
# =========================================================================
class BhaiGeeLogin:
    def __init__(self, root):
        self.root = root
        self.root.title("Bhai Gee Traders & Crockery Store")
        self.root.state('zoomed')
        self.root.configure(bg="#f4f6f9")

        header_frame = tk.Frame(self.root, bg="#1e293b", bd=0)
        header_frame.pack(fill="x", ipady=20)
        tk.Label(header_frame, text="BHAI GEE TRADERS & CROCKERY STORE", font=("Impact", 36, "bold"), bg="#1e293b", fg="#ffffff").pack(pady=(20, 5))
        tk.Label(header_frame, text="Karkhana Bazar, Sargodha  |  Proprietor: Hafiz Muhammad Usman (0300-6027106)", font=("Arial", 12, "bold"), bg="#1e293b", fg="#cbd5e1").pack(pady=(0, 20))

        menu_frame = tk.Frame(self.root, bg="#f4f6f9")
        menu_frame.pack(pady=50)

        tk.Label(menu_frame, text="SOFTWARE LOGIN MENU", font=("Arial", 20, "bold", "underline"), bg="#f4f6f9", fg="#334155").pack(pady=20)

        tk.Button(menu_frame, text="1. SALESMAN LOGIN", font=("Arial", 16, "bold"), bg="#0284c7", fg="white", width=25, height=2, cursor="hand2", command=self.salesman_login_click).pack(pady=15)
        tk.Button(menu_frame, text="2. OWNER LOGIN", font=("Arial", 16, "bold"), bg="#475569", fg="white", width=25, height=2, cursor="hand2", command=self.owner_login_window).pack(pady=15)

    def salesman_login_click(self):
        self.sale_root = tk.Toplevel(self.root)
        self.app = SalesmanWindow(self.sale_root)
        self.root.withdraw()

    def owner_login_window(self):
        self.password_window = tk.Toplevel(self.root)
        self.password_window.title("Owner Verification")
        self.password_window.geometry("400x250")
        self.password_window.configure(bg="#ffffff")
        self.password_window.resizable(False, False)
        self.password_window.grab_set()

        tk.Label(self.password_window, text="Username:", font=("Arial", 11, "bold"), bg="#ffffff").pack(pady=(20, 2))
        self.ent_user = tk.Entry(self.password_window, font=("Arial", 12), bd=2, width=25)
        self.ent_user.pack(pady=5)
        self.ent_user.insert(0, "admin")

        tk.Label(self.password_window, text="Password:", font=("Arial", 11, "bold"), bg="#ffffff").pack(pady=(10, 2))
        self.ent_pass = tk.Entry(self.password_window, font=("Arial", 12), show="*", bd=2, width=25)
        self.ent_pass.pack(pady=5)
        self.ent_pass.focus()

        tk.Button(self.password_window, text="Login As Owner", font=("Arial", 11, "bold"), bg="#16a34a", fg="white", cursor="hand2", command=self.verify_owner_password).pack(pady=20)

    def verify_owner_password(self):
        if self.ent_user.get() == "admin" and self.ent_pass.get() == "1234":
            self.password_window.destroy()
            self.dash_root = tk.Toplevel(self.root)
            self.app = OwnerDashboard(self.dash_root)
            self.root.withdraw()
        else:
            messagebox.showerror("Error", "Ghalt Username ya Password!")

if __name__ == "__main__":
    root = tk.Tk()
    app = BhaiGeeLogin(root)
    root.mainloop()