﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FreddansBokhandel
{
    public partial class UserControlOrders : UserControl
    {
        List<DateTime> orderDates = new List<DateTime>();
        FreddansBokhandelContext db;
        Order selectedOrder;
        List<Order> orders;

        public UserControlOrders(FormMain form1)
        {
            InitializeComponent();
            form1.LeaveOrdersTab += Form1_LeaveOrdersTab;
            form1.EnterOrdersTab += Form1_EnterOrdersTab;
        }

        private async Task LoadOrdersFromDatabaseAsync()
        {
            db = new FreddansBokhandelContext();

            try
            {
                if (db.Database.CanConnect())
                {
                    orders = await db.Ordrar
                          .Include(o => o.OrderDetails)
                          .Include(a => a.EmployeeIDs)
                          .Include(b => b.Stores)
                          .ThenInclude(ls => ls.StockBalance)
                          .ThenInclude(i => i.IsbnNavigation)
                          .OrderBy(b => b.OrderDate)
                          .ToListAsync();

                    orderDates.Clear();

                    foreach (var date in orders)
                    {
                        orderDates.Add(date.OrderDate);
                    }

                    orderDates = orderDates.Distinct().ToList();
                }
                else
                {
                    MessageBox.Show("Kunde inte ladda in från databasen.");
                }
            }
            catch
            {
                return;
            }
        }

        private void PopulateDataGridView()
        {
            dataGridView2.Rows.Clear();
            var orderDetails = treeViewOrders.SelectedNode.Tag as Order;
            int totalPris = 0;
            

            foreach (var book in orderDetails.OrderDetails)
            {
                int rowIndex = dataGridView2.Rows.Add();
                dataGridView2.Rows[rowIndex].Cells["columnIsbn"].Value = book.Isbn;
                dataGridView2.Rows[rowIndex].Cells["columnPris"].Value = book.Price;
                dataGridView2.Rows[rowIndex].Cells["columnKvantitet"].Value = book.Quantity;
                dataGridView2.Rows[rowIndex].Cells["columnTitel"].Value = book.IsbnNavigation.Title;

                totalPris += book.Price * book.Quantity;
            }
        }

        private void PopulateTreeNodeOrders(List<Order> orders)
        {
            if (orderDates == null) { return; }

            treeViewOrders.Nodes.Clear();
            {
                foreach (var date in orderDates)
                {
                    TreeNode orderDate = new TreeNode(date.ToString("yyyy-MM-dd"));

                    foreach (var order in orders)
                    {
                        if (orderDate.Text == order.OrderDate.ToString("yyyy-MM-dd"))
                        {
                            var orderNode = orderDate.Nodes.Add(order.Id.ToString());
                            orderNode.Tag = order;
                            orderNode.Name = order.Id.ToString();
                        }
                    }
                    orderDate.Text = $"{orderDate.Text} ({orderDate.Nodes.Count})";
                    treeViewOrders.Nodes.Add(orderDate);
                }
            }
        }

        private void SearchOrders()
        {
            var search = textBoxSearch.Text;

            try
            {
                treeViewOrders.SelectedNode = treeViewOrders.Nodes.Find(search, true)[0];
            }
            catch
            {
                MessageBox.Show($"Ordern '{search}' finns inte.");
                return;
            }

            treeViewOrders.SelectedNode.Expand();
            treeViewOrders.Focus();
            SelectingANode();
        }

        private void ClearSelectedOrder()
        {
            buttonRemoveOrder.Enabled = false;
            textBoxOrderID.Text = null;
            textBoxOrderDate.Text = null;
            textBoxOrderSent.Text = null;
            textBoxButik.Text = null;
            textBoxSeller.Text = null;
            textBoxBuyerInfo.Text = null;
            dataGridView2.Rows.Clear();
        }

        private void SelectingANode()
        {
            selectedOrder = null;

            if (treeViewOrders.SelectedNode.Tag == null) { return; }

            selectedOrder = treeViewOrders.SelectedNode.Tag as Order;

            textBoxOrderID.Text = selectedOrder.Id.ToString();
            textBoxOrderDate.Text = selectedOrder.OrderDate.ToString();
            textBoxOrderSent.Text = selectedOrder.SentDate.ToString();
            textBoxButik.Text = selectedOrder.Stores.Name;
            textBoxSeller.Text = selectedOrder.EmployeeIDs.FirstName;

            PopulateDataGridView();

            if (selectedOrder.RecipientFirstName != null)
            {
                textBoxBuyerInfo.Text = $"{selectedOrder.RecipientFirstName} {selectedOrder.RecipientLastName}{Environment.NewLine}" +
                    $"{selectedOrder.RecipientAddress}{Environment.NewLine}" +
                    $"{selectedOrder.RecipientZipCode} {selectedOrder.RecipientPostalAddress}{Environment.NewLine}" +
                    $"{selectedOrder.RecipientCountry}";
            }
            else
            {
                textBoxBuyerInfo.Text = "-";
            }

            buttonRemoveOrder.Enabled = true;
        }

        private async Task CreateNewOrderAsync()
        {
            FormAddNewOrder order = new FormAddNewOrder();
            order.ShowDialog();
            await LoadOrdersFromDatabaseAsync();
        }

        private async Task RemoveSelectedOrderAsync()
        {
            db.Remove(selectedOrder);
            db.SaveChanges();
            await LoadOrdersFromDatabaseAsync();
        }

        private async void Form1_EnterOrdersTab(object sender, EventArgs e)
        {
            treeViewOrders.Nodes.Clear();
            ClearSelectedOrder();
            await LoadOrdersFromDatabaseAsync();
            PopulateTreeNodeOrders(orders);
            buttonCreateOrder.Enabled = true;
        }

        private void Form1_LeaveOrdersTab(object sender, EventArgs e)
        {
            if (db != null) { db.Dispose(); }
        }

        private async void buttonCreateOrder_Click(object sender, EventArgs e)
        {
            await CreateNewOrderAsync();
            PopulateTreeNodeOrders(orders);
        }

        private async void buttonRemoveOrder_Click(object sender, EventArgs e)
        {
            await RemoveSelectedOrderAsync();
            PopulateTreeNodeOrders(orders);
            ClearSelectedOrder();
        }

        private void treeViewOrders_AfterSelect(object sender, TreeViewEventArgs e)
        {
            SelectingANode();
        }

        private void buttonSearch_Click(object sender, EventArgs e)
        {
            SearchOrders();
        }
    }
}