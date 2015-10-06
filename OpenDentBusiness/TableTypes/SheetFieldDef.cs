using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace OpenDentBusiness{
	///<summary>One field on a sheetDef.</summary>
	[Serializable()]
	[CrudTable(IsSynchable=true)]
	public class SheetFieldDef:TableBase {
		///<summary>Primary key.</summary>
		[CrudColumn(IsPriKey=true)]
		public long SheetFieldDefNum;
		///<summary>FK to sheetdef.SheetDefNum.</summary>
		public long SheetDefNum;
		///<summary>Enum:SheetFieldType  OutputText, InputField, StaticText,Parameter(only used for SheetField, not SheetFieldDef),Image,Drawing,Line,Rectangle,CheckBox,SigBox,PatImage.</summary>
		public SheetFieldType FieldType;
		///<summary>Mostly for OutputText, InputField, and CheckBox types.  Each sheet typically has a main datatable type.  For OutputText types, FieldName is usually the string representation of the database column for the main table.  For other tables, it can be of the form table.Column.  There may also be extra fields available that are not strictly pulled from the database.  Extra fields will start with lowercase to indicate that they are not pure database fields.  The list of available fields for each type in SheetFieldsAvailable.  Users can pick from that list.  Likewise, InputField types are internally tied to actions to persist the data.  So they are also hard coded and are available in SheetFieldsAvailable.  For static images, this is the full file name including extension, but without path.  Static images paths are reconstructed by looking in the AtoZ folder, SheetImages folder.  For PatImages, this is the name of the DocCategory.</summary>
		public string FieldName;
		///<summary>For StaticText, this text can include bracketed fields, like [nameLF].
		///<para>For OutputText and InputField, this will be blank.  </para>
		///<para>For CheckBoxes, either X or blank.  Even if the checkbox is set to behave like a radio button.  </para>
		///<para>For Pat Images, this is blank.  The filename of a PatImage will later be stored in SheetField.FieldValue.</para></summary>
		[CrudColumn(SpecialType=CrudSpecialColType.TextIsClob)]
		public string FieldValue;
		///<summary>The fontSize for this field regardless of the default for the sheet.  The actual font must be saved with each sheetField.</summary>
		public float FontSize;
		///<summary>The fontName for this field regardless of the default for the sheet.  The actual font must be saved with each sheetField.</summary>
		public string FontName;
		///<summary>.</summary>
		public bool FontIsBold;
		///<summary>In pixels.</summary>
		public int XPos;
		///<summary>In pixels.</summary>
		public int YPos;
		///<summary>The field will be constrained horizontally to this size.  Not allowed to be zero.</summary>
		public int Width;
		///<summary>The field will be constrained vertically to this size.  Not allowed to be 0.  It's not allowed to be zero so that it will be visible on the designer.</summary>
		public int Height;
		///<summary>Enum:GrowthBehaviorEnum</summary>
		public GrowthBehaviorEnum GrowthBehavior;
		///<summary>This is only used for checkboxes that you want to behave like radiobuttons.  Set the FieldName the same for each Checkbox in the group.  The FieldValue will likely be X for one of them and empty string for the others.  Each of them will have a different RadioButtonValue.  Whichever box has X, the RadioButtonValue for that box will be used when importing.  This field is not used for "misc" radiobutton groups.</summary>
		public string RadioButtonValue;
		///<summary>Name which identifies the group within which the radio button belongs. FieldName must be set to "misc" in order for the group to take effect.</summary>
		public string RadioButtonGroup;
		///<summary>Set to true if this field is required to have a value before the sheet is closed.</summary>
		public bool IsRequired;
		///<summary>The Bitmap should be converted to Base64 using POut.Bitmap() before placing in this field.  Not stored in the database.  Only used when uploading SheetDefs to the web server.</summary>
		[CrudColumn(IsNotDbColumn=true)]
		public string ImageData;
		///<summary>Tab stop order for all fields. One-based.  Only checkboxes and input fields can have values other than 0.</summary>
		public int TabOrder;
		///<summary>Allows reporting on misc fields.</summary>
		public string ReportableName;
		///<summary>Text Alignment for text fields.</summary>
		public HorizontalAlignment TextAlign;
		///<summary>Used to determine if the field should be hidden when printing statements.</summary>
		public bool IsPaymentOption;
		///<summary>Text color, line color, rectangle color.</summary>
		[XmlIgnore]
		public Color ItemColor;

		///<summary>Used only for serialization purposes</summary>
		[XmlElement("ColorOverride",typeof(int))]
		public int ItemColorXml {
			get {
				return ItemColor.ToArgb();
			}
			set {
				ItemColor=Color.FromArgb(value);
			}
		}

		public SheetFieldDef(){//required for use as a generic.
			RadioButtonGroup="";
			ImageData="";
		}
	
		public SheetFieldDef(SheetFieldType fieldType,string fieldName,string fieldValue,
			float fontSize,string fontName,bool fontIsBold,
			int xPos,int yPos,int width,int height,
			GrowthBehaviorEnum growthBehavior,string radioButtonValue,bool isPaymentOption=false,KnownColor itemColor=KnownColor.Black,HorizontalAlignment textAlign=HorizontalAlignment.Left) 
		{
			FieldType=fieldType;
			FieldName=fieldName;
			FieldValue=fieldValue;
			FontSize=fontSize;
			FontName=fontName;
			FontIsBold=fontIsBold;
			XPos=xPos;
			YPos=yPos;
			Width=width;
			Height=height;
			GrowthBehavior=growthBehavior;
			RadioButtonValue=radioButtonValue;
			IsPaymentOption=isPaymentOption;
			ItemColor=Color.FromKnownColor(itemColor);
			TextAlign=textAlign;
		}

		public SheetFieldDef Copy(){
			return (SheetFieldDef)this.MemberwiseClone();
		}

		public override string ToString() {
			return FieldName+" "+FieldValue;
		}

		///<Summary></Summary>
		public Font GetFont(){
			FontStyle style=FontStyle.Regular;
			if(FontIsBold){
				style=FontStyle.Bold;
			}
			return new Font(FontName,FontSize,style);
		}

		//public static SheetFieldDef NewOutput(string fieldName,float fontSize,string fontName,bool fontIsBold,
		//	int xPos,int yPos,int width,int height)
		//{
		//	return new SheetFieldDef(SheetFieldType.OutputText,fieldName,"",fontSize,fontName,fontIsBold,
		//		xPos,yPos,width,height,GrowthBehaviorEnum.None,"");
		//}

		public static SheetFieldDef NewOutput(string fieldName,float fontSize,string fontName,bool fontIsBold,
			int xPos,int yPos,int width,int height,GrowthBehaviorEnum growthBehavior=GrowthBehaviorEnum.None,KnownColor itemColor=KnownColor.Black)
		{
			return new SheetFieldDef(SheetFieldType.OutputText,fieldName,"",fontSize,fontName,fontIsBold,
				xPos,yPos,width,height,growthBehavior,"",false,itemColor);
		}

		public static SheetFieldDef NewOutput(string fieldName,float fontSize,string fontName,bool fontIsBold,
			int xPos,int yPos,int width,int height,HorizontalAlignment textAlign, KnownColor itemColor=KnownColor.Black,
			GrowthBehaviorEnum growthBehavior=GrowthBehaviorEnum.None)
		{
			return new SheetFieldDef(SheetFieldType.OutputText,fieldName,"",fontSize,fontName,fontIsBold,
				xPos,yPos,width,height,growthBehavior,"",false,itemColor,textAlign);
		}

		//public static SheetFieldDef NewStaticText(string fieldValue,float fontSize,string fontName,bool fontIsBold,
		//	int xPos,int yPos,int width,int height) {
		//	return new SheetFieldDef(SheetFieldType.StaticText,"",fieldValue,fontSize,fontName,fontIsBold,
		//		xPos,yPos,width,height,GrowthBehaviorEnum.None,"");
		//}

		///<summary>Use named parameters if you only need to use some of the optional parameters.</summary>
		public static SheetFieldDef NewStaticText(string fieldValue,float fontSize,string fontName,bool fontIsBold,
			int xPos,int yPos,int width,int height,GrowthBehaviorEnum growthBehavior=GrowthBehaviorEnum.None,bool isPaymentOption=false,KnownColor itemColor=KnownColor.Black,
			HorizontalAlignment textAlign=HorizontalAlignment.Left) 
		{
			return new SheetFieldDef(SheetFieldType.StaticText,"",fieldValue,fontSize,fontName,fontIsBold,
				xPos,yPos,width,height,GrowthBehaviorEnum.None,"",isPaymentOption,itemColor,textAlign);
		}

		public static SheetFieldDef NewInput(string fieldName,float fontSize,string fontName,bool fontIsBold,
			int xPos,int yPos,int width,int height)
		{
			return new SheetFieldDef(SheetFieldType.InputField,fieldName,"",fontSize,fontName,fontIsBold,
				xPos,yPos,width,height,GrowthBehaviorEnum.None,"");
		}

		public static SheetFieldDef NewImage(string fileName,int xPos,int yPos,int width,int height) {
			return new SheetFieldDef(SheetFieldType.Image,fileName,"",0,"",false,
				xPos,yPos,width,height,GrowthBehaviorEnum.None,"");
		}

		public static SheetFieldDef NewLine(int xPos,int yPos,int width,int height,bool isPaymentOption=false,KnownColor itemColor=KnownColor.Black) {
			return new SheetFieldDef(SheetFieldType.Line,"","",0,"",false,
				xPos,yPos,width,height,GrowthBehaviorEnum.None,"",isPaymentOption,itemColor);
		}

		public static SheetFieldDef NewRect(int xPos,int yPos,int width,int height) {
			return new SheetFieldDef(SheetFieldType.Rectangle,"","",0,"",false,
				xPos,yPos,width,height,GrowthBehaviorEnum.None,"");
		}

		public static SheetFieldDef NewCheckBox(string fieldName,int xPos,int yPos,int width,int height) {
			return new SheetFieldDef(SheetFieldType.CheckBox,fieldName,"",0,"",false,
				xPos,yPos,width,height,GrowthBehaviorEnum.None,"");
		}

		public static SheetFieldDef NewRadioButton(string fieldName,string radioButtonValue,int xPos,int yPos,int width,int height) {
			return new SheetFieldDef(SheetFieldType.CheckBox,fieldName,"",0,"",false,
				xPos,yPos,width,height,GrowthBehaviorEnum.None,radioButtonValue);
		}

		public static SheetFieldDef NewSigBox(int xPos,int yPos,int width,int height) {
			return new SheetFieldDef(SheetFieldType.SigBox,"","",0,"",false,
				xPos,yPos,width,height,GrowthBehaviorEnum.None,"");
		}

		public static SheetFieldDef NewSpecial(int xPos,int yPos,int width,int height) {
			return new SheetFieldDef(SheetFieldType.Special,"","",0,"Calibri",false,
				xPos,yPos,width,height,GrowthBehaviorEnum.None,"");
		}

		public static SheetFieldDef NewGrid(string fieldName,int xPos,int yPos,int width,int height,float fontSize=8.5f,string fontName="") {
			SheetFieldDef retVal=new SheetFieldDef(SheetFieldType.Grid,fieldName,"",fontSize,fontName,false,
				xPos,yPos,width,height,GrowthBehaviorEnum.DownGlobal,"");
			return retVal;
		}

		///<Summary>Should only be called after FieldValue has been set, due to GrowthBehavior.</Summary>
		public Rectangle Bounds {
			get {
				return new Rectangle(XPos,YPos,Width,Height);
			}
		}
		
		///<Summary>Should only be called after FieldValue has been set, due to GrowthBehavior.</Summary>
		public RectangleF BoundsF {
			get {
				return new RectangleF(XPos,YPos,Width,Height);
			}
		}
	}

	

}
