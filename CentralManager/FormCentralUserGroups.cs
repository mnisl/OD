﻿using OpenDentBusiness;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CentralManager {
	public partial class FormCentralUserGroups:Form {
		public FormCentralUserGroups() {
			InitializeComponent();
		}

		private void FormCentralUserGroups_Load(object sender,EventArgs e) {
			FillList();
		}

		private void FillList(){
			UserGroups.RefreshCache();
			listGroups.Items.Clear();
			for(int i=0;i<UserGroups.List.Length;i++){
				listGroups.Items.Add(UserGroups.List[i].Description);
			}
		}

		private void listGroups_DoubleClick(object sender,EventArgs e) {
			if(listGroups.SelectedIndex==-1) {
				return;
			}
			UserGroup group=UserGroups.List[listGroups.SelectedIndex];
			FormCentralUserGroupEdit FormUGE=new FormCentralUserGroupEdit(group);
			FormUGE.ShowDialog();
			if(FormUGE.DialogResult==DialogResult.Cancel) {
				return;
			}
			FillList();
		}

		private void butAddGroup_Click(object sender,EventArgs e) {
			UserGroup group=new UserGroup();
			group.IsNew=true;
			FormCentralUserGroupEdit FormU=new FormCentralUserGroupEdit(group);
			FormU.ShowDialog();
			if(FormU.DialogResult==DialogResult.Cancel) {
				return;
			}
			FillList();
		}

		private void butClose_Click(object sender,EventArgs e) {
			DialogResult=DialogResult.Cancel;
		}
		
	}
}
