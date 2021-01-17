﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FreddansBokhandel
{
    public partial class UserControlPublishers : UserControl
    {
        List<Publisher> publishers;

        public UserControlPublishers()
        {
            InitializeComponent();
        }

        private void LoadPublishersFromDatabase()
        {
            using (var db = new FreddansBokhandelContext())
            {
                if (db.Database.CanConnect())
                {
                    publishers = db.Förlag.ToList();
                }

                else
                {
                    MessageBox.Show("Kunde inte koppla upp mot databasen.");
                }
            }
        }

        private void PopulateDataGridPublishers()
        {
            dataGridViewPublishers.Rows.Clear();

            foreach (var publisher in publishers)
            {
                int rowIndex = dataGridViewPublishers.Rows.Add();
                dataGridViewPublishers.Rows[rowIndex].Cells["ID"].Value = publisher.Id;
                dataGridViewPublishers.Rows[rowIndex].Cells["Namn"].Value = publisher.Name;
                dataGridViewPublishers.Rows[rowIndex].Cells["Adress"].Value = publisher.Address;
                dataGridViewPublishers.Rows[rowIndex].Cells["Postnr"].Value = publisher.ZipCode;
                dataGridViewPublishers.Rows[rowIndex].Cells["Postort"].Value = publisher.PostalAddress;
                dataGridViewPublishers.Rows[rowIndex].Cells["Land"].Value = publisher.Country;
            }
        }

        private void AddNewEmployee()
        {
            FormAddPublisher newPublisher = new FormAddPublisher(publishers);
            newPublisher.ShowDialog();
        }

        private void UserControlPublishers_Load(object sender, EventArgs e)
        {
            LoadPublishersFromDatabase();
            PopulateDataGridPublishers();
        }

        private void buttonAddPublisher_Click(object sender, EventArgs e)
        {
            AddNewEmployee();
            LoadPublishersFromDatabase();
            PopulateDataGridPublishers();
        }
    }
}