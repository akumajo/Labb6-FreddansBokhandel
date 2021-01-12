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

namespace FreddansBokhandel
{
    public partial class UserControlOrders : UserControl
    {
        List<DateTime> orderDates = new List<DateTime>();
        FreddansBokhandelContext db;
        Ordrar selectedOrder;
        List<Ordrar> orders;
        public UserControlOrders(FormMain form1)
        {
            InitializeComponent();
            form1.LeaveOrdersTab += Form1_LeaveOrdersTab;
            form1.EnterOrdersTab += Form1_EnterOrdersTab;
        }

        private void Form1_EnterOrdersTab(object sender, EventArgs e)
        {
            LoadFromDatabase();
            PopulateTreeNodeOrders(orders);
        }

        private void Form1_LeaveOrdersTab(object sender, EventArgs e)
        {
            db.Dispose();
        }

        private void LoadFromDatabase()
        {
            db = new FreddansBokhandelContext();

            if (db.Database.CanConnect())
            {
                orders = db.Ordrar
                      .Include(o => o.Orderhuvud)
                      .Include(a => a.AnställningsID)
                      .Include(b => b.Butik)
                      .ThenInclude(ls => ls.LagerSaldo)
                      .ThenInclude(i => i.IsbnNavigation)
                      .OrderBy(b => b.Beställningsdatum)
                      .ToList();

                foreach (var date in orders)
                {
                    orderDates.Add(date.Beställningsdatum);
                }

                orderDates = orderDates.Distinct().ToList();
            }
            else
            {
                MessageBox.Show("Kunde inte ladda in från databasen.");
            }
        }

        private void PopulateTreeNodeOrders(List<Ordrar> orders)
        {
            treeViewOrders.Nodes.Clear();

            foreach (var date in orderDates)
            {
                TreeNode orderDate = new TreeNode(date.ToString("yyyy-MM-dd"));

                foreach (var order in orders)
                {
                    if (orderDate.Text == order.Beställningsdatum.ToString("yyyy-MM-dd"))
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

        private void treeViewOrders_AfterSelect(object sender, TreeViewEventArgs e)
        {
            SelectingANode();
        }

        private void buttonSearch_Click(object sender, EventArgs e)
        {
            SearchOrders();
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

        private void SelectingANode()
        {
            selectedOrder = null;

            if (treeViewOrders.SelectedNode.Tag == null) { return; }

            selectedOrder = treeViewOrders.SelectedNode.Tag as Ordrar;

            textBoxOrderID.Text = selectedOrder.Id.ToString();
            textBoxOrderDate.Text = selectedOrder.Beställningsdatum.ToString();
            textBoxOrderSent.Text = selectedOrder.SkickatDatum.ToString();
            textBoxButik.Text = selectedOrder.Butik.Namn;
            textBoxSeller.Text = selectedOrder.AnställningsID.Förnamn;

            PopulateDataGridView();

            if (selectedOrder.MottagareFörnamn != null)
            {
                textBoxBuyerInfo.Text = $"{selectedOrder.MottagareFörnamn} {selectedOrder.MottagareEfternamn}{Environment.NewLine}" +
                    $"{selectedOrder.MottagareAdress}{Environment.NewLine}" +
                    $"{selectedOrder.MottagarePostnummer} {selectedOrder.MottagarePostort}{Environment.NewLine}" +
                    $"{selectedOrder.MottagareLand}";
            }
            else
            {
                textBoxBuyerInfo.Text = "-";
            }
        }

        private void PopulateDataGridView()
        {
            dataGridView2.Rows.Clear();
            var bajs = treeViewOrders.SelectedNode.Tag as Ordrar;
            int totalPris = 0;

            foreach (var balance in bajs.Orderhuvud)
            {
                int rowIndex = dataGridView2.Rows.Add();
                dataGridView2.Rows[rowIndex].Cells["columnIsbn"].Value = balance.Isbn;
                dataGridView2.Rows[rowIndex].Cells["columnPris"].Value = balance.Pris;
                dataGridView2.Rows[rowIndex].Cells["columnKvantitet"].Value = balance.Kvantitet;
                dataGridView2.Rows[rowIndex].Cells["columnTitel"].Value = balance.IsbnNavigation.Titel;

                totalPris += balance.Pris * balance.Kvantitet;
            }
        }

        private void buttonCreateOrder_Click(object sender, EventArgs e)
        {
            FormAddNewOrder order = new FormAddNewOrder();

            order.ShowDialog();
            LoadFromDatabase();
            PopulateTreeNodeOrders(orders);
        }

        private void buttonRemoveOrder_Click(object sender, EventArgs e)
        {
            db.Remove(selectedOrder);
            db.SaveChanges();
            LoadFromDatabase();
            PopulateTreeNodeOrders(orders);
        }
    }
}