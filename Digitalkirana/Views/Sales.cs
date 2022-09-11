﻿using Digitalkirana.BusinessLogicLayer;
using Digitalkirana.DataAccessLayer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Forms;

namespace Digitalkirana.Views
{
    public partial class Sales : Form
    {
        public Sales()
        {
            InitializeComponent();
            InitialConstraints();
        }

        private void InitialConstraints()
        {
            textBoxRate.Maximum = Decimal.MaxValue;
            textBoxQuantity.Maximum = Decimal.MaxValue;
            textBoxInventory.Maximum = Decimal.MaxValue;
            textBoxQuantity.Maximum = Decimal.MaxValue;
            textBoxDiscount.Maximum = Decimal.MaxValue;
            textBoxVat.Maximum = Decimal.MaxValue;
            textBoxPaidAmt.Maximum = Decimal.MaxValue;
            textboxSubtotal.Maximum = Decimal.MaxValue;
            textBoxGrandTotal.Maximum = Decimal.MaxValue;
        }

        CustomerDAL customerDAL = new CustomerDAL();
        ProductDAL productDAL = new ProductDAL();
        DataTable productDt = new DataTable();
        SalesDAL salesDAL = new SalesDAL();
        UserDAL userDAL = new UserDAL();
        SalesDetailsDAL salesDetailsDAL = new SalesDetailsDAL();
        public string productId;
        public int customerId;

        private void Sales_Load(object sender, EventArgs e)
        {
            productDt.Columns.Add("ID");
            productDt.Columns.Add("Product Name");
            productDt.Columns.Add("Rate");
            productDt.Columns.Add("Quantity");
            productDt.Columns.Add("Total");
        }

        private void textBoxCustomerSearch_TextChanged(object sender, EventArgs e)
        {
            string keyword = textBoxCustomerSearch.Text;
            if (keyword == null || keyword == "")
            {
                textBoxCustomerName.Clear();
                textBoxEmail.Clear();
                textBoxAddress.Clear();
                textBoxPhone.Clear();
                customerId = 0;
            }
            else
            {
                var customer = customerDAL.SearchCustomerForSale(keyword);
                customerId = customer.Id;
                textBoxCustomerName.Text = customer.CustomerName;
                textBoxPhone.Text = customer.Phone;
                textBoxAddress.Text = customer.Address;
                textBoxEmail.Text = customer.Email;
            }
        }

        private void btnProductAdd_Click(object sender, EventArgs e)
        {
            string productName = textBoxProductName.Text;
            decimal rate = textBoxRate.Value;
            decimal quantity = textBoxQuantity.Value;
            decimal total = rate * quantity;
            decimal subtotal = textboxSubtotal.Value;
            subtotal = subtotal + total;
            if (productName == "")
            {
                MessageBox.Show("No any product selected");
            }
            else
            {
                productDt.Rows.Add(productId, productName, rate, quantity, total);
                dataGridViewAddedProducts.DataSource = productDt;
                textBoxProductSearch.Clear();
                textBoxProductName.Clear();
                textBoxInventory.Value = 0;
                textBoxRate.Value = 0;
                textBoxQuantity.Value = 0;
                textboxSubtotal.Value = subtotal;
            }
        }

        private void textBoxDiscount_ValueChanged(object sender, EventArgs e)
        {
            decimal discountPercent = textBoxDiscount.Value;
            decimal subTotal = textboxSubtotal.Value;
            decimal discountAmt = ((discountPercent / 100) * subTotal);
            decimal grandTotal = subTotal - discountAmt;
            textBoxGrandTotal.Value = grandTotal;
        }

        private void textBoxVat_ValueChanged(object sender, EventArgs e)
        {
            if (textBoxGrandTotal.Value > 0)
            {
                decimal vatPercent = textBoxVat.Value;
                decimal total = textBoxGrandTotal.Value;
                decimal vatAmt = ((vatPercent / 100) * total);
                decimal grandTotal = total + vatAmt;
                textBoxGrandTotal.Value = grandTotal;
            }
        }

        private void textBoxPaidAmt_ValueChanged(object sender, EventArgs e)
        {
            decimal grandTotal = textBoxGrandTotal.Value;
            decimal paidAmt = textBoxPaidAmt.Value;
            decimal returnAmt = paidAmt - grandTotal;
            textBoxReturnAmt.Text = returnAmt.ToString("0.00");
        }

        private void Reset()
        {
            dataGridViewAddedProducts.DataSource = null;
            dataGridViewAddedProducts.Rows.Clear();
            textBoxProductSearch.Clear();
            textBoxCustomerSearch.Clear();
            textBoxQuantity.Value = 0;
            textboxSubtotal.Value = 0;
            textBoxVat.Value = 0;
            textBoxDiscount.Value = 0;
            textBoxGrandTotal.Value = 0;
            textBoxReturnAmt.Clear();
            textBoxPaidAmt.Value = 0;
        }

        private void saveBtn_Click(object sender, EventArgs e)
        {
            SalesBLL sale = new SalesBLL();
            sale.CustomerId = customerId;
            sale.GrandTotal = textBoxGrandTotal.Value;
            sale.Date = dateTimePickerBill.Value;
            sale.Tax = textBoxVat.Value;
            sale.Discount = textBoxDiscount.Value;
            sale.AddedBy = userDAL.getUserId(Login.username);
            sale.SalesDetails = productDt;

            using (TransactionScope scope = new TransactionScope())
            {
                bool success = false;
                int salesId = -1;
                bool w = salesDAL.InsertSales(sale, out salesId);
                for (int i = 0; i < productDt.Rows.Count; i++)
                {
                    SalesDetailsBLL salesDetailsBLL = new SalesDetailsBLL();
                    salesDetailsBLL.ProductId = productDt.Rows[i][0].ToString();
                    salesDetailsBLL.Rate = Convert.ToDecimal(productDt.Rows[i][2]);
                    salesDetailsBLL.Quantity = Convert.ToDecimal(productDt.Rows[i][3]);
                    salesDetailsBLL.Total = Convert.ToDecimal(productDt.Rows[i][4]);
                    salesDetailsBLL.CustomerId = customerId;
                    salesDetailsBLL.AddedDate = DateTime.Now;
                    salesDetailsBLL.SalesId = salesId;
                    salesDetailsBLL.AddedBy = userDAL.getUserId(Login.username);
                    bool x = productDAL.DecreaseQuantity(salesDetailsBLL.ProductId, salesDetailsBLL.Quantity);

                    bool y = salesDetailsDAL.InsertSalesDetails(salesDetailsBLL);
                    success = x && w && y;
                }
                if (success)
                {
                    scope.Complete();
                    MessageBox.Show("Sales transaction successful.");
                    Reset();
                }
                else
                {
                    MessageBox.Show("Sales transaction failed.");
                }
            }
        }

        private void textBoxProductSearch_TextChanged(object sender, EventArgs e)
        {
            string keyword = textBoxProductSearch.Text;
            if (keyword == null || keyword == "")
            {
                textBoxProductName.Clear();
                textBoxRate.Value = 0;
                textBoxQuantity.Value = 0;
                textBoxInventory.Value = 0;
                productId = null;
            }
            else
            {
                var product = productDAL.SearchProductForPurchase(keyword);
                productId = product.Id;
                textBoxProductName.Text = product.ProductName;
                textBoxRate.Value = product.Rate;
                textBoxInventory.Value = product.Quantity;
            }
        }
    }
}