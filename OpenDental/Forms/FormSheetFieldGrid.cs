using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OpenDentBusiness;
using OpenDental.UI;
using System.Linq;

namespace OpenDental {
	public partial class FormSheetFieldGrid:Form {
		public SheetDef SheetDefCur;
		public SheetFieldDef SheetFieldDefCur;
		public bool IsReadOnly;

		public FormSheetFieldGrid() {
			InitializeComponent();
			Lan.F(this);
		}

		private void FormSheetFieldGrid_Load(object sender,EventArgs e) {
			if(IsReadOnly) {
				butOK.Enabled=false;
				butDelete.Enabled=false;
			}
			textGridType.Text=SheetFieldDefCur.FieldName;
			textXPos.Text=SheetFieldDefCur.XPos.ToString();
			textYPos.Text=SheetFieldDefCur.YPos.ToString();
			List<DisplayField> Columns=SheetUtil.GetGridColumnsAvailable(SheetFieldDefCur.FieldName);
			SheetFieldDefCur.Width=0;
			foreach(DisplayField f in Columns) {
				SheetFieldDefCur.Width+=f.ColumnWidth;
			}
			textWidth.Text=SheetFieldDefCur.Width.ToString();
			UI.ODGrid odGrid=new ODGrid();
			using(Graphics g=Graphics.FromImage(new Bitmap(100,100))) {
				SheetFieldDefCur.Height=0;
				if(SheetFieldDefCur.FieldName=="StatementPayPlan") {
					SheetFieldDefCur.Height+=odGrid.TitleHeight;
				}
				SheetFieldDefCur.Height+=odGrid.HeaderHeight+(int)g.MeasureString("Any",odGrid.Font,100,StringFormat.GenericDefault).Height+3;
				textHeight.Text=SheetFieldDefCur.Height.ToString();
			}
			for(int i=0;i<Enum.GetNames(typeof(GrowthBehaviorEnum)).Length;i++) {
				comboGrowthBehavior.Items.Add(Enum.GetNames(typeof(GrowthBehaviorEnum))[i]);
				if((int)SheetFieldDefCur.GrowthBehavior==i) {
					comboGrowthBehavior.SelectedIndex=i;
				}
			}
		}
		
		private void butDelete_Click(object sender,EventArgs e) {
			SheetFieldDefCur=null;
			DialogResult=DialogResult.OK;
		}

		private void butOK_Click(object sender,EventArgs e) {
			//don't save to database here.
			SheetFieldDefCur.XPos=PIn.Int(textXPos.Text);
			SheetFieldDefCur.YPos=PIn.Int(textYPos.Text);
			SheetFieldDefCur.Height=PIn.Int(textHeight.Text);
			SheetFieldDefCur.Width=PIn.Int(textWidth.Text);
			DialogResult=DialogResult.OK;
		}

		private void butCancel_Click(object sender,EventArgs e) {
			DialogResult=DialogResult.Cancel;
		}


	}
}