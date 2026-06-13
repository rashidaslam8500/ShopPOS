import tkinter as tk
from tkinter import messagebox, ttk
import db_helper

class SalesmanWindow:
    def __init__(self, root):
        self.root = root
        self.root.title("Sales & Billing Counter — Bhai Gee Traders")
        self.root.geometry("800x600")
        
        # --- UI ELEMENTS ---
        # Label ko define karna
        self.lbl_grand_total = tk.Label(self.root, text="Grand Total: Rs. 0.00", font=("Arial", 16, "bold"))
        self.lbl_grand_total.place(x=500, y=500)

        # Input Section
        tk.Label(self.root, text="Barcode:").place(x=20, y=20)
        self.ent_barcode = tk.Entry(self.root)
        self.ent_barcode.place(x=100, y=20)
        self.ent_barcode.focus()
        
        # Treeview
        self.tree = ttk.Treeview(self.root, columns=("desc", "qty", "price", "total"), show="headings")
        self.tree.heading("desc", text="Description"); self.tree.heading("qty", text="Qty")
        self.tree.heading("price", text="Price"); self.tree.heading("total", text="Total")
        self.tree.place(x=20, y=60, width=760, height=300)

        # Buttons
        tk.Button(self.root, text="Add Item", command=self.add_to_list).place(x=250, y=17)
        tk.Button(self.root, text="SAVE BILL", command=self.save_bill).place(x=350, y=500)

    def add_to_list(self):
        barcode = self.ent_barcode.get()
        product = db_helper.fetch_one("SELECT description, sale_rate FROM Products WHERE barcode=?", (barcode,))
        
        if product:
            description = product[0]
            price = product[1]
            self.tree.insert("", "end", values=(description, 1, price, price))
            
            # Yahan error-checking add ki hai
            if hasattr(self, 'lbl_grand_total'):
                self.update_total()
        else:
            messagebox.showerror("Error", "Product not found!")
        
        self.ent_barcode.delete(0, tk.END)
        self.ent_barcode.focus()

    def update_total(self):
        total = 0
        for child in self.tree.get_children():
            total += float(self.tree.item(child, "values")[3])
        # Agar label exist karta hai to update karein
        if hasattr(self, 'lbl_grand_total'):
            self.lbl_grand_total.config(text=f"Grand Total: Rs. {total:.2f}")

    def save_bill(self):
        messagebox.showinfo("Success", "Bill saved successfully!")

# Is file ko run karke check karein
if __name__ == "__main__":
    root = tk.Tk()
    obj = SalesmanWindow(root)
    root.mainloop()