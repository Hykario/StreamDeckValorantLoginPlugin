using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LolLogin
{
    public partial class CredentialManager : Form
    {
        public const string CredentialManagementTypePrefix = "com.zaphop.lollogin";

        public CredentialManager()
        {
            InitializeComponent();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var senderGrid = (DataGridView)sender;
            if (e.ColumnIndex == 2 && e.RowIndex > -1 && e.RowIndex < senderGrid.Rows.Count - 1)
            {
                var targetUserName = senderGrid.Rows[e.RowIndex].Cells[0].Value.ToString();

                if (MessageBox.Show($"Are you sure you want to delete {targetUserName}?", "Are you sure?", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                {
                    var set = new CredentialManagement.CredentialSet();
                    set.Load();

                    foreach (var credential in set.Where(p => p.Target == $"{CredentialManagementTypePrefix} - {targetUserName}" && p.Username == targetUserName))
                        credential.Delete();

                    senderGrid.Rows.RemoveAt(e.RowIndex);
                }
            }
        }

        // https://stackoverflow.com/questions/30256811/datagridview-button-text-not-appearing-despite-usecolumntextforbuttontext-set-to
        private void dataGridView1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            var grid = (DataGridView)sender;
            //this needs to be altered for every DataGridViewButtonColumn with different Text
            int buttonColumn = 2;
            if (grid.Columns[buttonColumn] is DataGridViewButtonColumn)
            {
                if (grid.RowCount >= 0)
                {
                    grid.Rows[grid.RowCount - 1].Cells[buttonColumn].Value = "Delete";
                }
            }
        }

        // https://stackoverflow.com/questions/30256811/datagridview-button-text-not-appearing-despite-usecolumntextforbuttontext-set-to
        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            var grid = (DataGridView)sender;
            if (grid.Columns[e.ColumnIndex] is DataGridViewButtonColumn)
            {
                if (grid.RowCount >= 0)
                {
                    //this needs to be altered for every DataGridViewButtonColumn with different Text
                    grid.Rows[grid.RowCount - 1].Cells[e.ColumnIndex].Value = "Delete";
                }
            }

            if (e.RowIndex == -1)
                return; 

            var username = (string) grid.Rows[e.RowIndex].Cells[0].Value;
            var password = (string) grid.Rows[e.RowIndex].Cells[1].Value;

            if (string.IsNullOrWhiteSpace(username) == false && string.IsNullOrWhiteSpace(password) == false && password.Contains("֎") == false)
            {
                new CredentialManagement.Credential
                {
                    Username = username,
                    Password = password,
                    Description = "League of Legends Credential for use with LolLogin StreamDeck Plugin.",
                    PersistanceType = CredentialManagement.PersistanceType.LocalComputer,
                    Target = $"{CredentialManagementTypePrefix} - {username}",
                    Type = CredentialManagement.CredentialType.Generic
                }.Save();

                grid.Rows[e.RowIndex].Cells[1].Value = "֎֎֎֎֎֎֎֎֎֎֎֎";
                grid.Rows[e.RowIndex].Cells[0].ReadOnly = true;
            }
        }

        private void CredentialManager_Load(object sender, EventArgs e)
        {
            var set = new CredentialManagement.CredentialSet();
            set.Load();

            foreach (var credential in set.Where(p => p.Target.ToString().StartsWith(CredentialManagementTypePrefix) == true))
            {
                dataGridView1.Rows.Add(credential.Username, "֎֎֎֎֎֎֎֎֎֎֎֎");

                dataGridView1.Rows[dataGridView1.Rows.Count - 2].Cells[0].ReadOnly = true;
            }
        }
    }
}
