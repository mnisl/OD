using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using OpenDentBusiness;
using OpenDental.UI;
using System.Threading;

namespace OpenDental {
	public partial class FormXchargeTokenTool:Form {
		private List<CreditCard> CardList;

		public FormXchargeTokenTool() {
			InitializeComponent();
			Lan.F(this);
		}

		private void FormXchargeTokenTool_Load(object sender,EventArgs e) {
			CardList=CreditCards.GetCreditCardsWithTokens();
			textTotal.Text=CardList.Count.ToString();
			textVerified.Text="0";
			textInvalid.Text="0";
			Program prog=Programs.GetCur(ProgramName.Xcharge);
			string path=Programs.GetProgramPath(prog);
			if(prog==null || !prog.Enabled) {
				MsgBox.Show(this,"X-Charge entry is not set up.");//should never happen
				butCheck.Visible=false;
				return;
			}
			if(!File.Exists(path)) {
				MsgBox.Show(this,"X-Charge path is not valid.");
				butCheck.Visible=false;
				return;
			}
		}

		private void FillGrid() {
			Cursor=Cursors.WaitCursor;
			CardList=FilterCardList();
			Cursor=Cursors.Default;
			gridMain.BeginUpdate();
			gridMain.Columns.Clear();
			ODGridColumn col=new ODGridColumn(Lan.g("FormXChargeTest","PatNum"),80);
			gridMain.Columns.Add(col);
			col=new ODGridColumn(Lan.g("FormXChargeTest","First"),120);
			gridMain.Columns.Add(col);
			col=new ODGridColumn(Lan.g("FormXChargeTest","Last"),120);
			gridMain.Columns.Add(col);
			col=new ODGridColumn(Lan.g("FormXChargeTest","CCNumberMasked"),150);
			gridMain.Columns.Add(col);
			col=new ODGridColumn(Lan.g("FormXChargeTest","Exp"),50);
			gridMain.Columns.Add(col);
			col=new ODGridColumn(Lan.g("FormXChargeTest","Token"),100);
			gridMain.Columns.Add(col);
			gridMain.Rows.Clear();
			ODGridRow row;
			for(int i=0;i<CardList.Count;i++) {
				row=new ODGridRow();
				Patient pat=Patients.GetLim(CardList[i].PatNum);
				row.Cells.Add(CardList[i].PatNum.ToString());
				row.Cells.Add(pat.FName);
				row.Cells.Add(pat.LName);
				row.Cells.Add(CardList[i].CCNumberMasked);
				row.Cells.Add(CardList[i].CCExpiration.ToString("MMyy"));
				row.Cells.Add(CardList[i].XChargeToken);
				gridMain.Rows.Add(row);
			}
			gridMain.EndUpdate();
			if(CardList.Count==0) {
				MsgBox.Show(this,"There are no invalid tokens in the database.");
			}
		}

		private List<CreditCard> FilterCardList() {
			int verified=0;
			int invalid=0;
			textVerified.Text=verified.ToString();
			textInvalid.Text=invalid.ToString();
			for(int i=CardList.Count-1;i>=0;i--) {
				Program prog=Programs.GetCur(ProgramName.Xcharge);
				string path=Programs.GetProgramPath(prog);
				ProgramProperty prop=(ProgramProperty)ProgramProperties.GetForProgram(prog.ProgramNum)[0];
				ProcessStartInfo info=new ProcessStartInfo(path);
				string resultfile=Path.Combine(Path.GetDirectoryName(path),"XResult.txt");
				try {
					File.Delete(resultfile);//delete the old result file.
				}
				catch {
					MsgBox.Show(this,"Could not delete XResult.txt file.  It may be in use by another program, flagged as read-only, or you might not have sufficient permissions.");
					break;
				}
				info.Arguments+="/TRANSACTIONTYPE:ARCHIVEVAULTQUERY ";
				info.Arguments+="/XCACCOUNTID:"+CardList[i].XChargeToken+" ";
				info.Arguments+="/RESULTFILE:\""+resultfile+"\" ";
				info.Arguments+="/USERID:"+ProgramProperties.GetPropVal(prog.ProgramNum,"Username")+" ";
				info.Arguments+="/PASSWORD:"+CodeBase.MiscUtils.Decrypt(ProgramProperties.GetPropVal(prog.ProgramNum,"Password"))+" ";
				info.Arguments+="/AUTOPROCESS ";
				info.Arguments+="/AUTOCLOSE ";
				info.Arguments+="/NORESULTDIALOG ";
				Process process=new Process();
				process.StartInfo=info;
				process.EnableRaisingEvents=true;
				process.Start();
				while(!process.HasExited) {
					Application.DoEvents();
				}
				Thread.Sleep(200);//Wait 2/10 second to give time for file to be created.
				string resulttext="";
				string line="";
				string account="";
				string exp="";
				try {
					using(TextReader reader=new StreamReader(resultfile)) {
						line=reader.ReadLine();
						while(line!=null) {
							if(resulttext!="") {
								resulttext+="\r\n";
							}
							resulttext+=line;
							if(line.StartsWith("ACCOUNT=")) {
								account=line.Substring(8);
							}
							else if(line.StartsWith("EXPIRATION=")) {
								exp=line.Substring(11);
							}
							line=reader.ReadLine();
						}
						if(CardList[i].CCNumberMasked.Length>4 && account.Length>4
							&& CardList[i].CCNumberMasked.Substring(CardList[i].CCNumberMasked.Length-4)==account.Substring(account.Length-4)
							&& CardList[i].CCExpiration.ToString("MMyy")==exp) 
						{
							//The credit card on file matches the one in X-Charge, so remove from the list.
							CardList.Remove(CardList[i]);
							verified++;
						}
						else {
							invalid++;
						}
					}
				}
				catch {
					MsgBox.Show(this,"Something went wrong validating X-Charge tokens.  Please try again.");
					break;
				}
				textVerified.Text=verified.ToString();
				textInvalid.Text=invalid.ToString();
			}
			return CardList;
		}

		private void gridMain_CellDoubleClick(object sender,ODGridClickEventArgs e) {
			FormCreditCardManage FormCCM=new FormCreditCardManage(Patients.GetPat(CardList[gridMain.GetSelectedIndex()].PatNum));
			FormCCM.ShowDialog();
		}

		private void butCheck_Click(object sender,EventArgs e) {
			FillGrid();
		}

		private void butClose_Click(object sender,EventArgs e) {
			DialogResult=DialogResult.Cancel;
		}
	}
}