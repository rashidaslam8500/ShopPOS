import tkinter as tk
from tkinter import ttk
import db_helper

def show_bills_window(parent_root):
    bill_win = tk.Toplevel(parent_root)
    bill_win.title("All Bills Record")
    bill_win.geometry("800x500")
    
    # 1. Colors aur Style ki setting
    style = ttk.Style()
    style.theme_use("clam")  # Clam theme lines dikhane ke liye behtar hai
    
    # Row colors (Zig-Zag)
    style.configure("Treeview", rowheight=30, fieldbackground="#ffffff")
    style.map("Treeview", background=[('selected', '#38bdf8')])
    
    # Alternating colors (White and Light Grey)
    style.configure("Treeview.Heading", font=('Arial', 11, 'bold'), background="#0f172a", foreground="white")
    
    # 2. Treeview banana
    columns = ("bill_no", "date", "total")
    tree = ttk.Treeview(bill_win, columns=columns, show="headings", style="Treeview")
    
    # 3. Columns ki setting (Alignment)
    tree.heading("bill_no", text="Bill No", anchor="w")
    tree.heading("date", text="Date & Time", anchor="w")
    tree.heading("total", text="Total Amount", anchor="w")
    
    tree.column("bill_no", width=100, anchor="w")
    tree.column("date", width=250, anchor="w")
    tree.column("total", width=150, anchor="e") # Numbers ko Right align (e) kiya hai
    
    # Striped rows (Zig-Zag color)
    tree.tag_configure('oddrow', background='#f1f5f9')
    tree.tag_configure('evenrow', background='#ffffff')
    
    tree.pack(fill="both", expand=True, padx=10, pady=10)
    
    # 4. Data fetch aur insert
    rows = db_helper.fetch_data("SELECT bill_no, date_time, total_amount FROM Bills")
    for i, row in enumerate(rows):
        tag = 'evenrow' if i % 2 == 0 else 'oddrow'
        tree.insert("", "end", values=row, tags=(tag,))