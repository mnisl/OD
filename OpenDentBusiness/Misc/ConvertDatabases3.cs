﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace OpenDentBusiness {
	public partial class ConvertDatabases {
		public static System.Version LatestVersion=new Version("15.2.1.0");//This value must be changed when a new conversion is to be triggered.

		#region Helper Functions

		///<summary>Encrypts signature text and returns a base 64 string so that it can go directly into the database.
		///Copied from MiscUtils.Encrypt() so that the data conversion will never change historically.</summary>
		public static string Encrypt(string encrypt) {
			UTF8Encoding enc=new UTF8Encoding();
			byte[] arrayEncryptBytes=Encoding.UTF8.GetBytes(encrypt);
			MemoryStream ms=new MemoryStream();
			CryptoStream cs=null;
			Aes aes=new AesManaged();
			aes.Key=enc.GetBytes("AKQjlLUjlcABVbqp");
			aes.IV=new byte[16];
			ICryptoTransform encryptor=aes.CreateEncryptor(aes.Key,aes.IV);
			cs=new CryptoStream(ms,encryptor,CryptoStreamMode.Write);
			cs.Write(arrayEncryptBytes,0,arrayEncryptBytes.Length);
			cs.FlushFinalBlock();
			byte[] retval=new byte[ms.Length];
			ms.Position=0;
			ms.Read(retval,0,(int)ms.Length);
			cs.Dispose();
			ms.Dispose();
			if(aes!=null) {
				aes.Clear();
			}
			return Convert.ToBase64String(retval);
		}

		///<summary>Helper method to determine if an index already exists.  Returns true if colNames matches the concatenation of all COLUMN_NAME(s) for the column(s) referenced by an index on the corresponding tableName.  If the index references multiple columns, colNames must have the column names in the exact order in which the index was created separated by commas, without spaces.  Example: the claimproc table has the multi-column index on columns ClaimPaymentNum, Status, and InsPayAmt.  To see if that index already exists, the parameters would be tableName="claimproc" and colNames="ClaimPaymentNum,Status,InsPayAmt".  Not case sensitive.  This will always return false for Oracle.</summary>
		public static bool IndexExists(string tableName,string colNames) {
			if(DataConnection.DBtype==DatabaseType.Oracle) {//Oracle will not allow the same column to be indexed more than once
				return false;
			}
			string command="SELECT COUNT(*) FROM ("
				+"SELECT GROUP_CONCAT(LOWER(COLUMN_NAME) ORDER BY SEQ_IN_INDEX) ColNames "
				+"FROM INFORMATION_SCHEMA.STATISTICS "
				+"WHERE TABLE_SCHEMA=SCHEMA() "
				+"AND LOWER(TABLE_NAME)='"+POut.String(tableName.ToLower())+"' "
				+"GROUP BY INDEX_NAME) cols "
				+"WHERE cols.ColNames='"+POut.String(colNames.ToLower())+"'";
			if(Db.GetCount(command)=="0") {
				return false;
			}
			return true;
		}

		#endregion Helper Functions

		///<summary>Oracle compatible: 07/11/2013</summary>
		private static void To13_2_1() {
			if(FromVersion<new Version("13.2.1.0")) {
				string command;
				//Add TaskEdit permission to everyone------------------------------------------------------
				command="SELECT DISTINCT UserGroupNum FROM grouppermission";
				DataTable table=Db.GetTable(command);
				long groupNum;
				if(DataConnection.DBtype==DatabaseType.MySql) {
					for(int i=0;i<table.Rows.Count;i++) {
						groupNum=PIn.Long(table.Rows[i]["UserGroupNum"].ToString());
						command="INSERT INTO grouppermission (UserGroupNum,PermType) "
							+"VALUES("+POut.Long(groupNum)+",66)";//POut.Int((int)Permissions.TaskEdit)
						Db.NonQ(command);
					}
				}
				else {//oracle
					for(int i=0;i<table.Rows.Count;i++) {
						groupNum=PIn.Long(table.Rows[i]["UserGroupNum"].ToString());
						command="INSERT INTO grouppermission (GroupPermNum,NewerDays,UserGroupNum,PermType) "
							+"VALUES((SELECT MAX(GroupPermNum)+1 FROM grouppermission),0,"+POut.Long(groupNum)+",66)";//POut.Int((int)Permissions.TaskEdit)
						Db.NonQ(command);
					}
				}
				//add WikiListSetup permissions for users that have security admin------------------------------------------------------
				command="SELECT UserGroupNum FROM grouppermission WHERE PermType=24";//POut.Int((int)Permissions.SecurityAdmin)
				table=Db.GetTable(command);
				if(DataConnection.DBtype==DatabaseType.MySql) {
					for(int i=0;i<table.Rows.Count;i++) {
						groupNum=PIn.Long(table.Rows[i][0].ToString());
						command="INSERT INTO grouppermission (NewerDate,UserGroupNum,PermType) "
						+"VALUES('0001-01-01',"+POut.Long(groupNum)+",67)";//POut.Int((int)Permissions.WikiListSetup);
						Db.NonQ32(command);
					}
				}
				else {//oracle
					for(int i=0;i<table.Rows.Count;i++) {
						groupNum=PIn.Long(table.Rows[i][0].ToString());
						command="INSERT INTO grouppermission (GroupPermNum,NewerDays,NewerDate,UserGroupNum,PermType) "
						+"VALUES((SELECT MAX(GroupPermNum)+1 FROM grouppermission),0,TO_DATE('0001-01-01','YYYY-MM-DD'),"+POut.Long(groupNum)+",67)";//POut.Int((int)Permissions.WikiListSetup)
						Db.NonQ32(command);
					}
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('PatientPortalURL','')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'PatientPortalURL','')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE procedurelog ADD BillingNote varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE procedurelog ADD BillingNote varchar2(255)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE procedurelog ADD RepeatChargeNum bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE procedurelog ADD INDEX (RepeatChargeNum)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE procedurelog ADD RepeatChargeNum number(20)";
					Db.NonQ(command);
					command="UPDATE procedurelog SET RepeatChargeNum = 0 WHERE RepeatChargeNum IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE procedurelog MODIFY RepeatChargeNum NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX procedurelog_RepeatChargeNum ON procedurelog (RepeatChargeNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS reseller";
					Db.NonQ(command);
					command=@"CREATE TABLE reseller (
						ResellerNum bigint NOT NULL auto_increment PRIMARY KEY,
						PatNum bigint NOT NULL,
						UserName varchar(255) NOT NULL,
						ResellerPassword varchar(255) NOT NULL,
						INDEX(PatNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE reseller'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE reseller (
						ResellerNum number(20) NOT NULL,
						PatNum number(20) NOT NULL,
						UserName varchar2(255),
						ResellerPassword varchar2(255),
						CONSTRAINT reseller_ResellerNum PRIMARY KEY (ResellerNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX reseller_PatNum ON reseller (PatNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS resellerservice";
					Db.NonQ(command);
					command=@"CREATE TABLE resellerservice (
						ResellerServiceNum bigint NOT NULL auto_increment PRIMARY KEY,
						ResellerNum bigint NOT NULL,
						CodeNum bigint NOT NULL,
						Fee double NOT NULL,
						INDEX(ResellerNum),
						INDEX(CodeNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE resellerservice'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE resellerservice (
						ResellerServiceNum number(20) NOT NULL,
						ResellerNum number(20) NOT NULL,
						CodeNum number(20) NOT NULL,
						Fee number(38,8) NOT NULL,
						CONSTRAINT resellerservice_ResellerServic PRIMARY KEY (ResellerServiceNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX resellerservice_ResellerNum ON resellerservice (ResellerNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX resellerservice_CodeNum ON resellerservice (CodeNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE registrationkey ADD IsResellerCustomer tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE registrationkey ADD IsResellerCustomer number(3)";
					Db.NonQ(command);
					command="UPDATE registrationkey SET IsResellerCustomer = 0 WHERE IsResellerCustomer IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE registrationkey MODIFY IsResellerCustomer NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE repeatcharge ADD CopyNoteToProc tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE repeatcharge ADD CopyNoteToProc number(3)";
					Db.NonQ(command);
					command="UPDATE repeatcharge SET CopyNoteToProc = 0 WHERE CopyNoteToProc IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE repeatcharge MODIFY CopyNoteToProc NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS xchargetransaction";
					Db.NonQ(command);
					command=@"CREATE TABLE xchargetransaction (
						XChargeTransactionNum bigint NOT NULL auto_increment PRIMARY KEY,
						TransType varchar(255) NOT NULL,
						Amount double NOT NULL,
						CCEntry varchar(255) NOT NULL,
						PatNum bigint NOT NULL,
						Result varchar(255) NOT NULL,
						ClerkID varchar(255) NOT NULL,
						ResultCode varchar(255) NOT NULL,
						Expiration varchar(255) NOT NULL,
						CCType varchar(255) NOT NULL,
						CreditCardNum varchar(255) NOT NULL,
						BatchNum varchar(255) NOT NULL,
						ItemNum varchar(255) NOT NULL,
						ApprCode varchar(255) NOT NULL,
						TransactionDateTime datetime NOT NULL DEFAULT '0001-01-01 00:00:00',
						INDEX(PatNum),
						INDEX(TransactionDateTime)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE xchargetransaction'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE xchargetransaction (
						XChargeTransactionNum number(20) NOT NULL,
						TransType varchar2(255),
						Amount number(38,8) NOT NULL,
						CCEntry varchar2(255),
						PatNum number(20) NOT NULL,
						Result varchar2(255),
						ClerkID varchar2(255),
						ResultCode varchar2(255),
						Expiration varchar2(255),
						CCType varchar2(255),
						CreditCardNum varchar2(255),
						BatchNum varchar2(255),
						ItemNum varchar2(255),
						ApprCode varchar2(255),
						TransactionDateTime date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						CONSTRAINT xchargetransaction_XChargeTran PRIMARY KEY (XChargeTransactionNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX xchargetransaction_PatNum ON xchargetransaction (PatNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX xchargetransaction_Transaction ON xchargetransaction (TransactionDateTime)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('CanadaODAMemberNumber','')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'CanadaODAMemberNumber','')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('CanadaODAMemberPass','')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'CanadaODAMemberPass','')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE patient ADD SmokingSnoMed varchar(32) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE patient ADD SmokingSnoMed varchar2(32)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE repeatcharge ADD CreatesClaim tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE repeatcharge ADD CreatesClaim number(3)";
					Db.NonQ(command);
					command="UPDATE repeatcharge SET CreatesClaim = 0 WHERE CreatesClaim IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE repeatcharge MODIFY CreatesClaim NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE repeatcharge ADD IsEnabled tinyint NOT NULL";
					Db.NonQ(command);
					command="UPDATE repeatcharge SET IsEnabled = 1";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE repeatcharge ADD IsEnabled number(3)";
					Db.NonQ(command);
					//command="UPDATE repeatcharge SET IsEnabled = 0 WHERE IsEnabled IS NULL";
					command="UPDATE repeatcharge SET IsEnabled = 1 WHERE IsEnabled IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE repeatcharge MODIFY IsEnabled NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE rxalert ADD IsHighSignificance tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE rxalert ADD IsHighSignificance number(3)";
					Db.NonQ(command);
					command="UPDATE rxalert SET IsHighSignificance = 0 WHERE IsHighSignificance IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE rxalert MODIFY IsHighSignificance NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('EhrRxAlertHighSeverity','0')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'EhrRxAlertHighSeverity','0')";
					Db.NonQ(command);
				}
				//Oracle compatible
				command="UPDATE patient SET SmokingSnoMed='449868002' WHERE SmokeStatus=5";//CurrentEveryDay
				Db.NonQ(command);
				command="UPDATE patient SET SmokingSnoMed='428041000124106' WHERE SmokeStatus=4";//CurrentSomeDay
				Db.NonQ(command);
				command="UPDATE patient SET SmokingSnoMed='8517006' WHERE SmokeStatus=3";//FormerSmoker
				Db.NonQ(command);
				command="UPDATE patient SET SmokingSnoMed='266919005' WHERE SmokeStatus=2";//NeverSmoked
				Db.NonQ(command);
				command="UPDATE patient SET SmokingSnoMed='77176002' WHERE SmokeStatus=1";//SmokerUnknownCurrent
				Db.NonQ(command);
				command="UPDATE patient SET SmokingSnoMed='266927001' WHERE SmokeStatus=0";//UnknownIfEver
				Db.NonQ(command);
				command="ALTER TABLE patient DROP COLUMN SmokeStatus";
				Db.NonQ(command);
				//Add ICD9Code to DiseaseDef and update eduresource and disease to use DiseaseDefNum instead of ICD9Num----------------------------------------------------
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE diseasedef ADD ICD9Code varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE diseasedef ADD ICD9Code varchar2(255)";
					Db.NonQ(command);
				}
				//command="SELECT DISTINCT Description,ICD9Code,icd9.ICD9Num "
				//  +"FROM icd9,eduresource,disease,reminderrule "
				//  +"WHERE icd9.ICD9Num=eduresource.ICD9Num "
				//  +"OR icd9.ICD9Num=disease.ICD9Num "
				//  +"OR (ReminderCriterion=6 AND icd9.ICD9Num=CriterionFK)";//6=ICD9
				//table=Db.GetTable(command);
				List<string> listDescription=new List<string>();
				List<string> listICD9Code=new List<string>();
				List<long> listICD9Num=new List<long>();
				command="SELECT DISTINCT icd9.Description,icd9.ICD9Code,icd9.ICD9Num "
					+"FROM icd9,eduresource "
					+"WHERE icd9.ICD9Num=eduresource.ICD9Num";
				table=Db.GetTable(command);
				for(int i=0;i<table.Rows.Count;i++) {
					listDescription.Add(PIn.String(table.Rows[i]["Description"].ToString()));
					listICD9Code.Add(PIn.String(table.Rows[i]["ICD9Code"].ToString()));
					listICD9Num.Add(PIn.Long(table.Rows[i]["ICD9Num"].ToString()));
				}
				command="SELECT DISTINCT Description,ICD9Code,icd9.ICD9Num "
					+"FROM icd9,disease "
					+"WHERE icd9.ICD9Num=disease.ICD9Num ";//6=ICD9
				table=Db.GetTable(command);
				for(int i=0;i<table.Rows.Count;i++) {
					if(listICD9Num.Contains(PIn.Long(table.Rows[i]["ICD9Num"].ToString()))) {
						continue;
					}
					listDescription.Add(PIn.String(table.Rows[i]["Description"].ToString()));
					listICD9Code.Add(PIn.String(table.Rows[i]["ICD9Code"].ToString()));
					listICD9Num.Add(PIn.Long(table.Rows[i]["ICD9Num"].ToString()));
				}
				command="SELECT DISTINCT Description,ICD9Code,icd9.ICD9Num "
					+"FROM icd9,reminderrule "
					+"WHERE (ReminderCriterion=6 AND icd9.ICD9Num=CriterionFK)";//6=ICD9
				table=Db.GetTable(command);
				for(int i=0;i<table.Rows.Count;i++) {
					if(listICD9Num.Contains(PIn.Long(table.Rows[i]["ICD9Num"].ToString()))) {
						continue;
					}
					listDescription.Add(PIn.String(table.Rows[i]["Description"].ToString()));
					listICD9Code.Add(PIn.String(table.Rows[i]["ICD9Code"].ToString()));
					listICD9Num.Add(PIn.Long(table.Rows[i]["ICD9Num"].ToString()));
				}
				command="SELECT MAX(ItemOrder) FROM diseasedef";
				int itemOrderCur=PIn.Int(Db.GetScalar(command));
				//for(int i=0;i<table.Rows.Count;i++) {
				//  itemOrderCur++;
				//  if(DataConnection.DBtype==DatabaseType.MySql) {
				//    command="INSERT INTO diseasedef(DiseaseName,ItemOrder,ICD9Code) VALUES('"
				//      +POut.String(table.Rows[i]["Description"].ToString())+"',"+POut.Int(itemOrderCur)+",'"+POut.String(table.Rows[i]["ICD9Code"].ToString())+"')";
				//  }
				//  else {//oracle
				//    command="INSERT INTO diseasedef(DiseaseDefNum,DiseaseName,ItemOrder,ICD9Code) VALUES((SELECT MAX(DiseaseDefNum)+1 FROM diseasedef),'"
				//      +POut.String(table.Rows[i]["Description"].ToString())+"',"+POut.Int(itemOrderCur)+",'"+POut.String(table.Rows[i]["ICD9Code"].ToString())+"')";
				//  }
				//  long defNum=Db.NonQ(command,true);
				//  command="UPDATE eduresource SET DiseaseDefNum="+POut.Long(defNum)+" WHERE ICD9Num="+table.Rows[i]["ICD9Num"].ToString();
				//  Db.NonQ(command);
				//  command="UPDATE disease SET DiseaseDefNum="+POut.Long(defNum)+" WHERE ICD9Num="+table.Rows[i]["ICD9Num"].ToString();
				//  Db.NonQ(command);
				//  command="UPDATE reminderrule SET CriterionFK="+POut.Long(defNum)+" WHERE CriterionFK="+table.Rows[i]["ICD9Num"].ToString()+" AND ReminderCriterion=6";
				//  Db.NonQ(command);
				//}
				for(int i=0;i<listICD9Num.Count;i++) {
					itemOrderCur++;
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="INSERT INTO diseasedef(DiseaseName,ItemOrder,ICD9Code) VALUES('"
							+POut.String(listDescription[i])+"',"+POut.Int(itemOrderCur)+",'"+POut.String(listICD9Code[i])+"')";
					}
					else {//oracle
						command="INSERT INTO diseasedef(DiseaseDefNum,DiseaseName,ItemOrder,ICD9Code) VALUES((SELECT MAX(DiseaseDefNum)+1 FROM diseasedef),'"
							+POut.String(listDescription[i])+"',"+POut.Int(itemOrderCur)+",'"+POut.String(listICD9Code[i])+"')";
					}
					long defNum=Db.NonQ(command,true);
					command="UPDATE eduresource SET DiseaseDefNum="+POut.Long(defNum)+" WHERE ICD9Num="+POut.Long(listICD9Num[i]);
					Db.NonQ(command);
					command="UPDATE disease SET DiseaseDefNum="+POut.Long(defNum)+" WHERE ICD9Num="+POut.Long(listICD9Num[i]);
					Db.NonQ(command);
					command="UPDATE reminderrule SET CriterionFK="+POut.Long(defNum)+" WHERE CriterionFK="+POut.Long(listICD9Num[i])+" AND ReminderCriterion=6";
					Db.NonQ(command);
				}
				command="ALTER TABLE eduresource DROP COLUMN ICD9Num";
				Db.NonQ(command);
				command="ALTER TABLE disease DROP COLUMN ICD9Num";
				Db.NonQ(command);
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE diseasedef ADD SnomedCode varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE diseasedef ADD SnomedCode varchar2(255)";
					Db.NonQ(command);
				}
				//Update reminderrule.ReminderCriterion - set ICD9 (6) to Problem (0)------------------------------------------------------------------------------------
				command="UPDATE reminderrule SET ReminderCriterion=0 WHERE ReminderCriterion=6";
				Db.NonQ(command);
				//Update patientrace-------------------------------------------------------------------------------------------------------------------------------------
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('LanguagesIndicateNone','Declined to Specify')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'LanguagesIndicateNone','Declined to Specify')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS patientrace";
					Db.NonQ(command);
					command=@"CREATE TABLE patientrace (
						PatientRaceNum bigint NOT NULL auto_increment PRIMARY KEY,
						PatNum bigint NOT NULL,
						Race tinyint NOT NULL,
						INDEX(PatNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE patientrace'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE patientrace (
						PatientRaceNum number(20) NOT NULL,
						PatNum number(20) NOT NULL,
						Race number(3) NOT NULL,
						CONSTRAINT patientrace_PatientRaceNum PRIMARY KEY (PatientRaceNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX patientrace_PatNum ON patientrace (PatNum)";
					Db.NonQ(command);
				}
				//Create Custom Language "DeclinedToSpecify" ----------------------------------------------------------------------------------------------------------
				command="SELECT ValueString FROM preference WHERE PrefName = 'LanguagesUsedByPatients'";
				string valueString=Db.GetScalar(command);
				command="UPDATE preference SET ValueString='"+(POut.String(valueString)+",Declined to Specify").Trim(',')+"'"//trim ,(comma) off
					+" WHERE PrefName='LanguagesUsedByPatients'";
				Db.NonQ(command);
				//update Race and Ethnicity for EHR.---------------------------------------------------------------------------------------------------------------------
				//Get a list of patients that have a race set.
				command="SELECT PatNum, Race FROM patient WHERE Race!=0";
				table=Db.GetTable(command);
				string maxPkStr="1";//Used for Orcale.  Oracle has to insert the first row manually setting the PK to 1.
				for(int i=0;i<table.Rows.Count;i++) {
					string patNum=table.Rows[i]["PatNum"].ToString();
					switch(PIn.Int(table.Rows[i]["Race"].ToString())) {//PatientRaceOld
						case 0://PatientRaceOld.Unknown
							//Do nothing.  No entry means "Unknown", the old default.
							continue;
						case 1://PatientRaceOld.Multiracial
							if(DataConnection.DBtype==DatabaseType.MySql) {
								command="INSERT INTO patientrace (PatNum,Race) VALUES ("+patNum+",7)";
							}
							else {//oracle
								command="INSERT INTO patientrace (PatientRaceNum,PatNum,Race) VALUES ("+maxPkStr+","+patNum+",7)";
							}
							break;
						case 2://PatientRaceOld.HispanicLatino
							if(DataConnection.DBtype==DatabaseType.MySql) {
								command="INSERT INTO patientrace (PatNum,Race) VALUES ("+patNum+",9)";
								Db.NonQ(command);
								command="INSERT INTO patientrace (PatNum,Race) VALUES ("+patNum+",6)";
							}
							else {//oracle
								command="INSERT INTO patientrace (PatientRaceNum,PatNum,Race) VALUES ("+maxPkStr+","+patNum+",9)";
								Db.NonQ(command);
								command="INSERT INTO patientrace (PatientRaceNum,PatNum,Race) VALUES ((SELECT MAX(PatientRaceNum+1) FROM patientrace),"+patNum+",6)";
							}
							break;
						case 3://PatientRaceOld.AfricanAmerican
							if(DataConnection.DBtype==DatabaseType.MySql) {
								command="INSERT INTO patientrace (PatNum,Race) VALUES ("+patNum+",1)";
							}
							else {//oracle
								command="INSERT INTO patientrace (PatientRaceNum,PatNum,Race) VALUES ("+maxPkStr+","+patNum+",1)";
							}
							break;
						case 4://PatientRaceOld.White
							if(DataConnection.DBtype==DatabaseType.MySql) {
								command="INSERT INTO patientrace (PatNum,Race) VALUES ("+patNum+",9)";
							}
							else {//oracle
								command="INSERT INTO patientrace (PatientRaceNum,PatNum,Race) VALUES ("+maxPkStr+","+patNum+",9)";
							}
							break;
						case 5://PatientRaceOld.HawaiiOrPacIsland
							if(DataConnection.DBtype==DatabaseType.MySql) {
								command="INSERT INTO patientrace (PatNum,Race) VALUES ("+patNum+",5)";
							}
							else {//oracle
								command="INSERT INTO patientrace (PatientRaceNum,PatNum,Race) VALUES ("+maxPkStr+","+patNum+",5)";
							}
							break;
						case 6://PatientRaceOld.AmericanIndian
							if(DataConnection.DBtype==DatabaseType.MySql) {
								command="INSERT INTO patientrace (PatNum,Race) VALUES ("+patNum+",2)";
							}
							else {//oracle
								command="INSERT INTO patientrace (PatientRaceNum,PatNum,Race) VALUES ("+maxPkStr+","+patNum+",2)";
							}
							break;
						case 7://PatientRaceOld.Asian
							if(DataConnection.DBtype==DatabaseType.MySql) {
								command="INSERT INTO patientrace (PatNum,Race) VALUES ("+patNum+",3)";
							}
							else {//oracle
								command="INSERT INTO patientrace (PatientRaceNum,PatNum,Race) VALUES ("+maxPkStr+","+patNum+",3)";
							}
							break;
						case 8://PatientRaceOld.Other
							if(DataConnection.DBtype==DatabaseType.MySql) {
								command="INSERT INTO patientrace (PatNum,Race) VALUES ("+patNum+",8)";
							}
							else {//oracle
								command="INSERT INTO patientrace (PatientRaceNum,PatNum,Race) VALUES ("+maxPkStr+","+patNum+",8)";
							}
							break;
						case 9://PatientRaceOld.Aboriginal
							if(DataConnection.DBtype==DatabaseType.MySql) {
								command="INSERT INTO patientrace (PatNum,Race) VALUES ("+patNum+",0)";
							}
							else {//oracle
								command="INSERT INTO patientrace (PatientRaceNum,PatNum,Race) VALUES ("+maxPkStr+","+patNum+",0)";
							}
							break;
						case 10://PatientRaceOld.BlackHispanic
							if(DataConnection.DBtype==DatabaseType.MySql) {
								command="INSERT INTO patientrace (PatNum,Race) VALUES ("+patNum+",1)";
								Db.NonQ(command);
								command="INSERT INTO patientrace (PatNum,Race) VALUES ("+patNum+",6)";
							}
							else {//oracle
								command="INSERT INTO patientrace (PatientRaceNum,PatNum,Race) VALUES ("+maxPkStr+","+patNum+",1)";
								Db.NonQ(command);
								command="INSERT INTO patientrace (PatientRaceNum,PatNum,Race) VALUES ((SELECT MAX(PatientRaceNum+1) FROM patientrace),"+patNum+",6)";
							}
							break;
						default:
							//should never happen, useful for debugging.
							continue;
					}//end switch
					Db.NonQ(command);
					if(DataConnection.DBtype==DatabaseType.Oracle && maxPkStr=="1") {
						//At least one row has been entered.  Set the pk string to the auto-increment SQL for Oracle.
						maxPkStr="(SELECT MAX(PatientRaceNum+1) FROM patientrace)";
					}
				}
				//Apex clearinghouse.
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command=@"INSERT INTO clearinghouse(Description,ExportPath,Payors,Eformat,ISA05,SenderTin,ISA07,ISA08,ISA15,Password,ResponsePath,CommBridge,ClientProgram,
						LastBatchNumber,ModemPort,LoginID,SenderName,SenderTelephone,GS03,ISA02,ISA04,ISA16,SeparatorData,SeparatorSegment) 
						VALUES ('Apex','"+POut.String(@"C:\ONETOUCH\")+"','','5','ZZ','870578776','ZZ','99999','P','','','0','',0,0,'','Apex','8008409152','99999','','','','','')";
					Db.NonQ(command);
				}
				else {//oracle
					command=@"INSERT INTO clearinghouse(ClearinghouseNum,Description,ExportPath,Payors,Eformat,ISA05,SenderTin,ISA07,ISA08,ISA15,Password,ResponsePath,CommBridge,ClientProgram,
						LastBatchNumber,ModemPort,LoginID,SenderName,SenderTelephone,GS03,ISA02,ISA04,ISA16,SeparatorData,SeparatorSegment) 
						VALUES ((SELECT MAX(ClearinghouseNum+1) FROM clearinghouse),'Apex','"+POut.String(@"C:\ONETOUCH\")+"','','5','ZZ','870578776','ZZ','99999','P','','','0','',0,0,'','Apex','8008409152','99999','','','','','')";
					Db.NonQ(command);
				}
				//Insert Guru Bridge
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO program (ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"'Guru', "
				    +"'Guru from guru.waziweb.com', "
				    +"'0', "
				    +"'',"
				    +"'', "
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"'"+POut.Long(programNum)+"', "
				    +"'Guru image path', "
				    +"'C:\')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"'"+POut.Long(programNum)+"', "
				    +"'2', "//ToolBarsAvail.ChartModule
				    +"'Guru')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO program (ProgramNum,ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"(SELECT MAX(ProgramNum)+1 FROM program),"
				    +"'Guru', "
				    +"'Guru from guru.waziweb.com/', "
				    +"'0', "
				    +"'',"
				    +"'', "
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'Guru image path', "
				    +"'C:\')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ToolButItemNum,ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"(SELECT MAX(ToolButItemNum)+1 FROM toolbutitem),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'2', "//ToolBarsAvail.ChartModule
				    +"'Guru')";
					Db.NonQ(command);
				}//end Guru bridge
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('NewCropPartnerName','')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'NewCropPartnerName','')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE emailaddress ADD SMTPserverIncoming varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE emailaddress ADD SMTPserverIncoming varchar2(255)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE emailaddress ADD ServerPortIncoming int NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE emailaddress ADD ServerPortIncoming number(11)";
					Db.NonQ(command);
					command="UPDATE emailaddress SET ServerPortIncoming = 0 WHERE ServerPortIncoming IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE emailaddress MODIFY ServerPortIncoming NOT NULL";
					Db.NonQ(command);
				}
				command="UPDATE preference SET ValueString = '13.2.1.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To13_2_2();
		}

		///<summary>Oracle compatible: 07/11/2013</summary>
		private static void To13_2_2() {
			if(FromVersion<new Version("13.2.2.0")) {
				string command;
				//Convert languages in the LanguagesUsedByPatients preference from ISO639-1 to ISO639-2 for languages which are not custom.
				command="SELECT ValueString FROM preference WHERE PrefName='LanguagesUsedByPatients'";
				string strLanguageList=PIn.String(Db.GetScalar(command));
				if(strLanguageList!="") {
					StringBuilder sb=new StringBuilder();
					string[] lanstring=strLanguageList.Split(',');
					for(int i=0;i<lanstring.Length;i++) {
						if(lanstring[i]=="") {
							continue;
						}
						if(sb.Length>0) {
							sb.Append(",");
						}
						try {
							sb.Append(CultureInfo.GetCultureInfo(lanstring[i]).ThreeLetterISOLanguageName);
						}
						catch {//custom language
							sb.Append(lanstring[i]);
						}
					}
					command="UPDATE preference SET ValueString='"+POut.String(sb.ToString())+"' WHERE PrefName='LanguagesUsedByPatients'";
					Db.NonQ(command);
				}
				//Convert languages in the patient.Langauge column from ISO639-1 to ISO639-2 for languages which are not custom.
				command="SELECT PatNum,Language FROM patient WHERE Language<>'' AND Language IS NOT NULL";
				DataTable tablePatLanguages=Db.GetTable(command);
				for(int i=0;i<tablePatLanguages.Rows.Count;i++) {
					string lang=PIn.String(tablePatLanguages.Rows[i]["Language"].ToString());
					try {
						lang=CultureInfo.GetCultureInfo(lang).ThreeLetterISOLanguageName;
						long patNum=PIn.Long(tablePatLanguages.Rows[i]["PatNum"].ToString());
						command="UPDATE patient SET Language='"+POut.String(lang)+"' WHERE PatNum="+POut.Long(patNum);
						Db.NonQ(command);
					}
					catch {//Custom language
						//Do not modify.
					}
				}
				command="UPDATE preference SET ValueString = '13.2.2.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To13_2_4();
		}

		private static void To13_2_4() {
			if(FromVersion<new Version("13.2.4.0")) {
				//This fixes a bug in the conversion script above at lines 324 and 328
				string command;
				command="SELECT DiseaseDefNum,DiseaseName,ICD9Code,SnomedCode FROM diseasedef ORDER BY DiseaseDefNum ASC";
				DataTable table=Db.GetTable(command);
				for(int i=0;i<table.Rows.Count;i++) {//compare each row (i)
					for(int j=i+1;j<table.Rows.Count;j++) {//with every row below it
						if(PIn.String(table.Rows[i]["DiseaseName"].ToString())!=PIn.String(table.Rows[j]["DiseaseName"].ToString())
							|| PIn.String(table.Rows[i]["ICD9Code"].ToString())!=PIn.String(table.Rows[j]["ICD9Code"].ToString())
							|| PIn.String(table.Rows[i]["SnomedCode"].ToString())!=PIn.String(table.Rows[j]["SnomedCode"].ToString())) 
						{
							continue;
						}
						//row i and row j are "identical".  Because DiseaseDefNum is ascending, we want to keep row j, not row i.
						//Always use POut when entering data into the database. Jordan ok'd omitting it here for readability. Do not use this as an example.
						//The queries below will probably not make any changes.  Just if they used this part of the program heavily after the 
						command="UPDATE eduresource SET DiseaseDefNum="+table.Rows[j]["DiseaseDefNum"].ToString()+" WHERE DiseaseDefNum="+table.Rows[i]["DiseaseDefNum"].ToString();
						Db.NonQ(command);
						command="UPDATE disease SET DiseaseDefNum="+table.Rows[j]["DiseaseDefNum"].ToString()+" WHERE DiseaseDefNum="+table.Rows[i]["DiseaseDefNum"].ToString();
						Db.NonQ(command);
						command="UPDATE reminderrule SET CriterionFK="+table.Rows[j]["DiseaseDefNum"].ToString()+" WHERE CriterionFK="+table.Rows[i]["DiseaseDefNum"].ToString()+" AND ReminderCriterion=6";
						Db.NonQ(command);
						command="DELETE FROM diseasedef WHERE DiseaseDefNum="+table.Rows[i]["DiseaseDefNum"].ToString();
						Db.NonQ(command);
					}
				}
				command="UPDATE preference SET ValueString = '13.2.4.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To13_2_7();
		}

		private static void To13_2_7() {
			if(FromVersion<new Version("13.2.7.0")) {
				string command;
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE patient ADD Country varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE patient ADD Country varchar2(255)";
					Db.NonQ(command);
				}
				command="UPDATE preference SET ValueString = '13.2.7.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To13_2_16();
		}

		private static void To13_2_16() {
			if(FromVersion<new Version("13.2.16.0")) {
				string command;
				//Get the 1500 claim form primary key. The unique ID is OD9.
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="SELECT ClaimFormNum FROM claimform WHERE UniqueID='OD9' LIMIT 1";
				}
				else {//oracle doesn't have LIMIT
					command="SELECT * FROM (SELECT ClaimFormNum FROM claimform WHERE UniqueID='OD9') WHERE RowNum<=1";
				}
				DataTable tableClaimFormNum=Db.GetTable(command);
				if(tableClaimFormNum.Rows.Count>0) {//The claim form should exist, but might not if foreign.
					long claimFormNum=PIn.Long(Db.GetScalar(command));
					//Change the form facility address from the pay to address to the treating address.  The pay to address still shows under the billing section of the 1500 claim form.
					command="UPDATE claimformitem SET FieldName='TreatingDentistAddress' WHERE claimformnum="+POut.Long(claimFormNum)+" AND FieldName='PayToDentistAddress' AND XPos<400";
					Db.NonQ(command);
					command="UPDATE claimformitem SET FieldName='TreatingDentistCity' WHERE claimformnum="+POut.Long(claimFormNum)+" AND FieldName='PayToDentistCity' AND XPos<470";
					Db.NonQ(command);
					command="UPDATE claimformitem SET FieldName='TreatingDentistST' WHERE claimformnum="+POut.Long(claimFormNum)+" AND FieldName='PayToDentistST' AND XPos<500";
					Db.NonQ(command);
					command="UPDATE claimformitem SET FieldName='TreatingDentistZip' WHERE claimformnum="+POut.Long(claimFormNum)+" AND FieldName='PayToDentistZip' AND XPos<520";
					Db.NonQ(command);
				}
				command="UPDATE preference SET ValueString = '13.2.16.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To13_2_22();
		}

		private static void To13_2_22() {
			if(FromVersion<new Version("13.2.22.0")) {
				string command;
				//Moving codes to the Obsolete category that were deleted in CDT 2014.
				if(CultureInfo.CurrentCulture.Name.EndsWith("US")) {//United States
					//Move depricated codes to the Obsolete procedure code category.
					//Make sure the procedure code category exists before moving the procedure codes.
					string procCatDescript="Obsolete";
					long defNum=0;
					command="SELECT DefNum FROM definition WHERE Category=11 AND ItemName='"+POut.String(procCatDescript)+"'";//11 is DefCat.ProcCodeCats
					DataTable dtDef=Db.GetTable(command);
					if(dtDef.Rows.Count==0) { //The procedure code category does not exist, add it
						if(DataConnection.DBtype==DatabaseType.MySql) {
							command="INSERT INTO definition (Category,ItemName,ItemOrder) "
									+"VALUES (11"+",'"+POut.String(procCatDescript)+"',"+POut.Long(DefC.Long[11].Length)+")";//11 is DefCat.ProcCodeCats
						}
						else {//oracle
							command="INSERT INTO definition (DefNum,Category,ItemName,ItemOrder) "
									+"VALUES ((SELECT MAX(DefNum)+1 FROM definition),11,'"+POut.String(procCatDescript)+"',"+POut.Long(DefC.Long[11].Length)+")";//11 is DefCat.ProcCodeCats
						}
						defNum=Db.NonQ(command,true);
					}
					else { //The procedure code category already exists, get the existing defnum
						defNum=PIn.Long(dtDef.Rows[0]["DefNum"].ToString());
					}
					string[] cdtCodesDeleted=new string[] {
						"D0363","D3354","D5860","D5861"
					};
					for(int i=0;i<cdtCodesDeleted.Length;i++) {
						string procCode=cdtCodesDeleted[i];
						command="SELECT CodeNum FROM procedurecode WHERE ProcCode='"+POut.String(procCode)+"'";
						DataTable dtProcCode=Db.GetTable(command);
						if(dtProcCode.Rows.Count==0) { //The procedure code does not exist in this database.
							continue;//Do not try to move it.
						}
						long codeNum=PIn.Long(dtProcCode.Rows[0]["CodeNum"].ToString());
						command="UPDATE procedurecode SET ProcCat="+POut.Long(defNum)+" WHERE CodeNum="+POut.Long(codeNum);
						Db.NonQ(command);
					}
				}//end United States update
				command="UPDATE preference SET ValueString = '13.2.22.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To13_2_27();
		}

		private static void To13_2_27() {
			if(FromVersion<new Version("13.2.27.0")) {
				string command;
				//Insert DentalStudio Bridge
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO program (ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"'DentalStudio', "
				    +"'DentalStudio from www.villasm.com', "
				    +"'0', "
				    +"'"+POut.String(@"C:\Program Files (x86)\DentalStudioPlus\AutoStartup.exe")+"',"
				    +"'', "
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"'"+POut.Long(programNum)+"', "
				    +"'UserName (clear to use OD username)', "
				    +"'Admin')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"'"+POut.Long(programNum)+"', "
				    +"'UserPassword', "
				    +"'12345678')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"'"+POut.Long(programNum)+"', "
				    +"'"+POut.Int(((int)ToolBarsAvail.ChartModule))+"', "
				    +"'DentalStudio')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO program (ProgramNum,ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"(SELECT MAX(ProgramNum)+1 FROM program),"
				    +"'DentalStudio', "
				    +"'DentalStudio from www.villasm.com', "
				    +"'0', "
				    +"'"+POut.String(@"C:\Program Files (x86)\DentalStudioPlus\AutoStartup.exe")+"',"
				    +"'', "
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'UserName (clear to use OD username)', "
				    +"'Admin')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'UserPassword', "
				    +"'12345678')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ToolButItemNum,ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"(SELECT MAX(ToolButItemNum)+1 FROM toolbutitem),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'"+POut.Int(((int)ToolBarsAvail.ChartModule))+"', "
				    +"'DentalStudio')";
					Db.NonQ(command);
				}//end DentalStudio bridge
				command="UPDATE preference SET ValueString = '13.2.27.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To13_3_1();
		}

		private static void To13_3_1() {
			if(FromVersion<new Version("13.3.1.0")) {
				string command;
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('EmailInboxComputerName','')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'EmailInboxComputerName','')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('EmailInboxCheckInterval','5')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'EmailInboxCheckInterval','5')";
					Db.NonQ(command);
				}
				//Add Family Health table for EHR A.13 (Family Health History)
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS familyhealth";
					Db.NonQ(command);
					command=@"CREATE TABLE familyhealth (
						FamilyHealthNum bigint NOT NULL auto_increment PRIMARY KEY,
						PatNum bigint NOT NULL,
						Relationship tinyint NOT NULL,
						DiseaseDefNum bigint NOT NULL,
						PersonName varchar(255) NOT NULL,
						INDEX(PatNum),
						INDEX(DiseaseDefNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE familyhealth'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE familyhealth (
						FamilyHealthNum number(20) NOT NULL,
						PatNum number(20) NOT NULL,
						Relationship number(3) NOT NULL,
						DiseaseDefNum number(20) NOT NULL,
						PersonName varchar2(255),
						CONSTRAINT familyhealth_FamilyHealthNum PRIMARY KEY (FamilyHealthNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX familyhealth_PatNum ON familyhealth (PatNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX familyhealth_DiseaseDefNum ON familyhealth (DiseaseDefNum)";
					Db.NonQ(command);
				}
				//Add securityloghash table for EHR D.2
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS securityloghash";
					Db.NonQ(command);
					command=@"CREATE TABLE securityloghash (
						SecurityLogHashNum bigint NOT NULL auto_increment PRIMARY KEY,
						SecurityLogNum bigint NOT NULL,
						LogHash varchar(255) NOT NULL,
						INDEX(SecurityLogNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE securityloghash'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE securityloghash (
						SecurityLogHashNum number(20) NOT NULL,
						SecurityLogNum number(20) NOT NULL,
						LogHash varchar2(255),
						CONSTRAINT securityloghash_SecurityLogHas PRIMARY KEY (SecurityLogHashNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX securityloghash_SecurityLogNum ON securityloghash (SecurityLogNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE emailmessage CHANGE BodyText BodyText LONGTEXT NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					//Changing a column's datatype from VARCHAR2 to clob yields the following error in oracle:  ORA-22858: invalid alteration of datatype
					//The easiest way to get change the datatype from VARCHAR2 to clob is to create a temp column then rename it.
					command="ALTER TABLE emailmessage ADD (BodyTextClob clob NOT NULL)";
					Db.NonQ(command);
					command="UPDATE emailmessage SET BodyTextClob=BodyText";
					Db.NonQ(command);
					command="ALTER TABLE emailmessage DROP COLUMN BodyText";
					Db.NonQ(command);
					command="ALTER TABLE emailmessage RENAME COLUMN BodyTextClob TO BodyText";
					Db.NonQ(command);
				}
				//Electronic Dental Services (EDS) clearinghouse.
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command=@"INSERT INTO clearinghouse(Description,ExportPath,Payors,Eformat,ISA05,SenderTin,ISA07,ISA08,ISA15,Password,ResponsePath,CommBridge,
					ClientProgram,LastBatchNumber,ModemPort,LoginID,SenderName,SenderTelephone,GS03,ISA02,ISA04,ISA16,SeparatorData,SeparatorSegment) 
					VALUES ('Electronic Dental Services','"+POut.String(@"C:\EDS\Claims\In\")+"','','1','ZZ','','ZZ','EDS','P','','','0','"+POut.String(@"C:\Program Files\EDS\edsbridge.exe")+"',0,0,'','','','EDS','','','','','')";
					Db.NonQ(command);
				}
				else {//oracle
					command=@"INSERT INTO clearinghouse (ClearinghouseNum,Description,ExportPath,Payors,Eformat,ISA05,SenderTin,ISA07,ISA08,ISA15,Password,ResponsePath,CommBridge,ClientProgram,
					LastBatchNumber,ModemPort,LoginID,SenderName,SenderTelephone,GS03,ISA02,ISA04,ISA16,SeparatorData,SeparatorSegment) 
					VALUES ((SELECT MAX(ClearinghouseNum+1) FROM clearinghouse),'Electronic Dental Services','"+POut.String(@"C:\EDS\Claims\In\")+"','','1','ZZ','','ZZ','EDS','P','','','0','"+POut.String(@"C:\Program Files\EDS\edsbridge.exe")+"',0,0,'','','','EDS','','','','','')";
					Db.NonQ(command);
				}
				//codesystem
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS codesystem";
					Db.NonQ(command);
					command=@"CREATE TABLE codesystem (
						CodeSystemNum bigint NOT NULL auto_increment PRIMARY KEY,
						CodeSystemName varchar(255) NOT NULL,
						VersionCur varchar(255) NOT NULL,
						VersionAvail varchar(255) NOT NULL,
						HL7OID varchar(255) NOT NULL,
						Note varchar(255) NOT NULL,
						INDEX(CodeSystemName)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE codesystem'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE codesystem (
						CodeSystemNum number(20) NOT NULL,
						CodeSystemName varchar2(255),
						VersionCur varchar2(255),
						VersionAvail varchar2(255),
						HL7OID varchar2(255),
						Note varchar2(255),
						CONSTRAINT codesystem_CodeSystemNum PRIMARY KEY (CodeSystemNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX codesystem_CodeSystemName ON codesystem (CodeSystemName)";
					Db.NonQ(command);
				}
				//No need for mysql/oracle split
				command=@"INSERT INTO codesystem (CodeSystemNum,CodeSystemName,HL7OID,VersionAvail,VersionCur,Note) VALUES (1,'AdministrativeSex','2.16.840.1.113883.18.2','HL7v2.5','HL7v2.5','')";
				Db.NonQ(command);
				command=@"INSERT INTO codesystem (CodeSystemNum,CodeSystemName,HL7OID) VALUES (2,'CDCREC','2.16.840.1.113883.6.238')";
				Db.NonQ(command);
				command=@"INSERT INTO codesystem (CodeSystemNum,CodeSystemName,HL7OID) VALUES (3,'CDT','2.16.840.1.113883.6.13')";
				Db.NonQ(command);
				command=@"INSERT INTO codesystem (CodeSystemNum,CodeSystemName,HL7OID) VALUES (4,'CPT','2.16.840.1.113883.6.12')";
				Db.NonQ(command);
				command=@"INSERT INTO codesystem (CodeSystemNum,CodeSystemName,HL7OID) VALUES (5,'CVX','2.16.840.1.113883.12.292')";
				Db.NonQ(command);
				command=@"INSERT INTO codesystem (CodeSystemNum,CodeSystemName,HL7OID) VALUES (6,'HCPCS','2.16.840.1.113883.6.285')";
				Db.NonQ(command);
				command=@"INSERT INTO codesystem (CodeSystemNum,CodeSystemName,HL7OID) VALUES (7,'ICD10CM','2.16.840.1.113883.6.90')";
				Db.NonQ(command);
				command=@"INSERT INTO codesystem (CodeSystemNum,CodeSystemName,HL7OID) VALUES (8,'ICD9CM','2.16.840.1.113883.6.103')";
				Db.NonQ(command);
				command=@"INSERT INTO codesystem (CodeSystemNum,CodeSystemName,HL7OID) VALUES (9,'LOINC','2.16.840.1.113883.6.1')";
				Db.NonQ(command);
				command=@"INSERT INTO codesystem (CodeSystemNum,CodeSystemName,HL7OID) VALUES (10,'RXNORM','2.16.840.1.113883.6.88')";
				Db.NonQ(command);
				command=@"INSERT INTO codesystem (CodeSystemNum,CodeSystemName,HL7OID) VALUES (11,'SNOMEDCT','2.16.840.1.113883.6.96')";
				Db.NonQ(command);
				command=@"INSERT INTO codesystem (CodeSystemNum,CodeSystemName,HL7OID) VALUES (12,'SOP','2.16.840.1.113883.3.221.5')";
				Db.NonQ(command);
#region Create Code Systems Tables
				//CDCREC (CDC Race and Ethnicity)-------------------------------------------------------------------------------------------------------------------------
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS cdcrec";
					Db.NonQ(command);
					command=@"CREATE TABLE cdcrec (
						CdcrecNum bigint NOT NULL auto_increment PRIMARY KEY,
						CdcrecCode varchar(255) NOT NULL,
						HeirarchicalCode varchar(255) NOT NULL,
						Description varchar(255) NOT NULL,
						INDEX(CdcrecCode)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE cdcrec'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE cdcrec (
						CdcrecNum number(20) NOT NULL,
						CdcrecCode varchar2(255),
						HeirarchicalCode varchar2(255),
						Description varchar2(255),
						CONSTRAINT cdcrec_CdcrecNum PRIMARY KEY (CdcrecNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX cdcrec_CdcrecCode ON cdcrec (CdcrecCode)";
					Db.NonQ(command);
				}
				//CDT ----------------------------------------------------------------------------------------------------------------------------------------------------
				//Not neccesary, stored in ProcCode table
				//CPT (Current Procedure Terminology)---------------------------------------------------------------------------------------------------------------------
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS cpt";
					Db.NonQ(command);
					command=@"CREATE TABLE cpt (
						CptNum bigint NOT NULL auto_increment PRIMARY KEY,
						CptCode varchar(255) NOT NULL,
						Description varchar(4000) NOT NULL,
						INDEX(CptCode)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE cpt'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE cpt (
						CptNum number(20) NOT NULL,
						CptCode varchar2(255),
						Description varchar2(4000),
						CONSTRAINT cpt_CptNum PRIMARY KEY (CptNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX cpt_CptCode ON cpt (CptCode)";
					Db.NonQ(command);
				}
				//CVX (Vaccine Administered)------------------------------------------------------------------------------------------------------------------------------
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS cvx";
					Db.NonQ(command);
					command=@"CREATE TABLE cvx (
						CvxNum bigint NOT NULL auto_increment PRIMARY KEY,
						CvxCode varchar(255) NOT NULL,
						Description varchar(255) NOT NULL,
						IsActive varchar(255) NOT NULL,
						INDEX(CvxCode)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE cvx'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE cvx (
						CvxNum number(20) NOT NULL,
						CvxCode varchar2(255),
						Description varchar2(255),
						IsActive varchar2(255),
						CONSTRAINT cvx_CvxNum PRIMARY KEY (CvxNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX cvx_CvxCode ON cvx (CvxCode)";
					Db.NonQ(command);
				}
				//HCPCS (Healhtcare Common Procedure Coding System)-------------------------------------------------------------------------------------------------------
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS hcpcs";
					Db.NonQ(command);
					command=@"CREATE TABLE hcpcs (
						HcpcsNum bigint NOT NULL auto_increment PRIMARY KEY,
						HcpcsCode varchar(255) NOT NULL,
						DescriptionShort varchar(255) NOT NULL,
						INDEX(HcpcsCode)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE hcpcs'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE hcpcs (
						HcpcsNum number(20) NOT NULL,
						HcpcsCode varchar2(255),
						DescriptionShort varchar2(255),
						CONSTRAINT hcpcs_HcpcsNum PRIMARY KEY (HcpcsNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX hcpcs_HcpcsCode ON hcpcs (HcpcsCode)";
					Db.NonQ(command);
				}
				//ICD10CM International Classification of Diseases, 10th Revision, Clinical Modification------------------------------------------------------------------
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS icd10";
					Db.NonQ(command);
					command=@"CREATE TABLE icd10 (
						Icd10Num bigint NOT NULL auto_increment PRIMARY KEY,
						Icd10Code varchar(255) NOT NULL,
						Description varchar(255) NOT NULL,
						IsCode varchar(255) NOT NULL,
						INDEX(Icd10Code)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE icd10'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE icd10 (
						Icd10Num number(20) NOT NULL,
						Icd10Code varchar2(255),
						Description varchar2(255),
						IsCode varchar2(255),
						CONSTRAINT icd10_Icd10Num PRIMARY KEY (Icd10Num)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX icd10_Icd10Code ON icd10 (Icd10Code)";
					Db.NonQ(command);
				}
				//ICD9CM International Classification of Diseases, 9th Revision, Clinical Modification--------------------------------------------------------------------
				//Already Exists.
				//LOINC (Logical Observation Identifier Names and Codes)--------------------------------------------------------------------------------------------------
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS loinc";
					Db.NonQ(command);
					command=@"CREATE TABLE loinc (
						LoincNum bigint NOT NULL auto_increment PRIMARY KEY,
						LoincCode varchar(255) NOT NULL,
						Component varchar(255) NOT NULL,
						PropertyObserved varchar(255) NOT NULL,
						TimeAspct varchar(255) NOT NULL,
						SystemMeasured varchar(255) NOT NULL,
						ScaleType varchar(255) NOT NULL,
						MethodType varchar(255) NOT NULL,
						StatusOfCode varchar(255) NOT NULL,
						NameShort varchar(255) NOT NULL,
						ClassType varchar(255) NOT NULL,
						UnitsRequired tinyint NOT NULL,
						OrderObs varchar(255) NOT NULL,
						HL7FieldSubfieldID varchar(255) NOT NULL,
						ExternalCopyrightNotice text NOT NULL,
						NameLongCommon varchar(255) NOT NULL,
						UnitsUCUM varchar(255) NOT NULL,
						RankCommonTests int NOT NULL,
						RankCommonOrders int NOT NULL,
						INDEX(LoincCode)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE loinc'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE loinc (
						LoincNum number(20) NOT NULL,
						LoincCode varchar2(255),
						Component varchar2(255),
						PropertyObserved varchar2(255),
						TimeAspct varchar2(255),
						SystemMeasured varchar2(255),
						ScaleType varchar2(255),
						MethodType varchar2(255),
						StatusOfCode varchar2(255),
						NameShort varchar2(255),
						ClassType varchar2(255) NOT NULL,
						UnitsRequired number(3) NOT NULL,
						OrderObs varchar2(255),
						HL7FieldSubfieldID varchar2(255),
						ExternalCopyrightNotice varchar2(4000),
						NameLongCommon varchar2(255),
						UnitsUCUM varchar2(255),
						RankCommonTests number(11) NOT NULL,
						RankCommonOrders number(11) NOT NULL,
						CONSTRAINT loinc_LoincNum PRIMARY KEY (LoincNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX loinc_LoincCode ON loinc (LoincCode)";
					Db.NonQ(command);
				}
				//RXNORM--------------------------------------------------------------------------------------------------------------------------------------------------
				//Already Exists.
				//SNOMEDCT (Systematic Nomencalture of Medicine Clinical Terms)-------------------------------------------------------------------------------------------
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS snomed";
					Db.NonQ(command);
					command=@"CREATE TABLE snomed (
						SnomedNum bigint NOT NULL auto_increment PRIMARY KEY,
						SnomedCode varchar(255) NOT NULL,
						Description varchar(255) NOT NULL,
						INDEX(SnomedCode)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE snomed'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE snomed (
						SnomedNum number(20) NOT NULL,
						SnomedCode varchar2(255),
						Description varchar2(255),
						CONSTRAINT snomed_SnomedNum PRIMARY KEY (SnomedNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX snomed_SnomedCode ON snomed (SnomedCode)";
					Db.NonQ(command);
				}
				//SOP (Source of Payment Typology)------------------------------------------------------------------------------------------------------------------------
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS sop";
					Db.NonQ(command);
					command=@"CREATE TABLE sop (
						SopNum bigint NOT NULL auto_increment PRIMARY KEY,
						SopCode varchar(255) NOT NULL,
						Description varchar(255) NOT NULL,
						INDEX(SopCode)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE sop'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE sop (
						SopNum number(20) NOT NULL,
						SopCode varchar2(255),
						Description varchar2(255),
						CONSTRAINT sop_SopNum PRIMARY KEY (SopNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX sop_SopCode ON sop (SopCode)";
					Db.NonQ(command);
				}
#endregion
				//Rename emailaddress.SMTPserverIncoming to emailaddress.Pop3ServerIncoming, but leave data type alone. CRUD generator cannot write this query. See pattern for convert database.
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE emailaddress CHANGE SMTPserverIncoming Pop3ServerIncoming varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE emailaddress RENAME COLUMN SMTPserverIncoming TO Pop3ServerIncoming";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE medicationpat ADD MedDescript varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE medicationpat ADD MedDescript varchar2(255)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE medicationpat ADD RxCui bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE medicationpat ADD INDEX (RxCui)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE medicationpat ADD RxCui number(20)";
					Db.NonQ(command);
					command="UPDATE medicationpat SET RxCui = 0 WHERE RxCui IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE medicationpat MODIFY RxCui NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX medicationpat_RxCui ON medicationpat (RxCui)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="UPDATE medicationpat,medication SET medicationpat.RxCui=medication.RxCui WHERE medicationpat.MedicationNum=medication.MedicationNum";
					Db.NonQ(command);
				}
				else {//oracle
					command="UPDATE medicationpat SET medicationpat.RxCui=(SELECT medication.RxCui FROM medication WHERE medication.MedicationNum=medicationpat.MedicationNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE medicationpat ADD NewCropGuid varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE medicationpat ADD NewCropGuid varchar2(255)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE medicationpat ADD IsCpoe tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE medicationpat ADD IsCpoe number(3)";
					Db.NonQ(command);
					command="UPDATE medicationpat SET IsCpoe = 0 WHERE IsCpoe IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE medicationpat MODIFY IsCpoe NOT NULL";
					Db.NonQ(command);
				}
				//oracle compatible
				command="UPDATE medicationpat SET IsCpoe=1 "
						+"WHERE PatNote!='' AND DateStart > "+POut.Date((new DateTime(1880,1,1)));
				Db.NonQ(command);
				//Add additional EHR Measures to DB
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO ehrmeasure(MeasureType,Numerator,Denominator) VALUES(16,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(MeasureType,Numerator,Denominator) VALUES(17,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(MeasureType,Numerator,Denominator) VALUES(18,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(MeasureType,Numerator,Denominator) VALUES(19,-1,-1)";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO ehrmeasure(EhrMeasureNum,MeasureType,Numerator,Denominator) VALUES((SELECT MAX(EhrMeasureNum)+1 FROM ehrmeasure),16,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(EhrMeasureNum,MeasureType,Numerator,Denominator) VALUES((SELECT MAX(EhrMeasureNum)+1 FROM ehrmeasure),17,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(EhrMeasureNum,MeasureType,Numerator,Denominator) VALUES((SELECT MAX(EhrMeasureNum)+1 FROM ehrmeasure),18,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(EhrMeasureNum,MeasureType,Numerator,Denominator) VALUES((SELECT MAX(EhrMeasureNum)+1 FROM ehrmeasure),19,-1,-1)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('MeaningfulUseTwo','0')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'MeaningfulUseTwo','0')";
					Db.NonQ(command);
				}
				//Time Card Overhaul for differential pay----------------------------------------------------------------------------------------------------------------
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE clockevent ADD Rate2Hours time NOT NULL";
					Db.NonQ(command);
					command="UPDATE clockevent SET rate2hours='-01:00:00'";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE clockevent ADD Rate2Hours varchar2(255)";
					Db.NonQ(command);
					command="UPDATE clockevent SET rate2hours='-01:00:00'";
					Db.NonQ(command);
				}				
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE clockevent ADD Rate2Auto time NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE clockevent ADD Rate2Auto varchar2(255)";
					Db.NonQ(command);
				}
				command="ALTER TABLE timecardrule DROP COLUMN AmtDiff";
				Db.NonQ(command);
				command="ALTER TABLE clockevent DROP COLUMN AmountBonus";
				Db.NonQ(command);
				command="ALTER TABLE clockevent DROP COLUMN AmountBonusAuto";
				Db.NonQ(command);
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS ehramendment";
					Db.NonQ(command);
					command=@"CREATE TABLE ehramendment (
						EhrAmendmentNum bigint NOT NULL auto_increment PRIMARY KEY,
						PatNum bigint NOT NULL,
						IsAccepted tinyint NOT NULL,
						Description text NOT NULL,
						Source tinyint NOT NULL,
						SourceName text NOT NULL,
						FileName varchar(255) NOT NULL,
						RawBase64 longtext NOT NULL,
						DateTRequest datetime NOT NULL DEFAULT '0001-01-01 00:00:00',
						DateTAcceptDeny datetime NOT NULL DEFAULT '0001-01-01 00:00:00',
						DateTAppend datetime NOT NULL DEFAULT '0001-01-01 00:00:00',
						INDEX(PatNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE ehramendment'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE ehramendment (
						EhrAmendmentNum number(20) NOT NULL,
						PatNum number(20) NOT NULL,
						IsAccepted number(3) NOT NULL,
						Description varchar2(2000),
						Source number(3) NOT NULL,
						SourceName varchar2(2000),
						FileName varchar2(255),
						RawBase64 clob,
						DateTRequest date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						DateTAcceptDeny date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						DateTAppend date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						CONSTRAINT ehramendment_EhrAmendmentNum PRIMARY KEY (EhrAmendmentNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehramendment_PatNum ON ehramendment (PatNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE popup ADD UserNum bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE popup ADD INDEX (UserNum)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE popup ADD UserNum number(20)";
					Db.NonQ(command);
					command="UPDATE popup SET UserNum = 0 WHERE UserNum IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE popup MODIFY UserNum NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX popup_UserNum ON popup (UserNum)";
					Db.NonQ(command);
				} 
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE popup ADD DateTimeEntry datetime NOT NULL";
					Db.NonQ(command);
					command="UPDATE popup SET DateTimeEntry='0001-01-01 00:00:00'";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE popup ADD DateTimeEntry date";
					Db.NonQ(command);
					command="UPDATE popup SET DateTimeEntry = TO_DATE('0001-01-01','YYYY-MM-DD') WHERE DateTimeEntry IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE popup MODIFY DateTimeEntry NOT NULL";
					Db.NonQ(command);
				} 
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE popup ADD IsArchived tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE popup ADD IsArchived number(3)";
					Db.NonQ(command);
					command="UPDATE popup SET IsArchived = 0 WHERE IsArchived IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE popup MODIFY IsArchived NOT NULL";
					Db.NonQ(command);
				} 
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE popup ADD PopupNumArchive bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE popup ADD INDEX (PopupNumArchive)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE popup ADD PopupNumArchive number(20)";
					Db.NonQ(command);
					command="UPDATE popup SET PopupNumArchive = 0 WHERE PopupNumArchive IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE popup MODIFY PopupNumArchive NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX popup_PopupNumArchive ON popup (PopupNumArchive)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE patientrace ADD CdcrecCode varchar(255) NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE patientrace ADD INDEX (CdcrecCode)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE patientrace ADD CdcrecCode varchar2(255)";
					Db.NonQ(command);
					command="UPDATE patientrace SET CdcrecCode = '' WHERE CdcrecCode IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE patientrace MODIFY CdcrecCode NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX patientrace_CdcrecCode ON patientrace (CdcrecCode)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE vitalsign ADD WeightCode varchar(255) NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE vitalsign ADD INDEX (WeightCode)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE vitalsign ADD WeightCode varchar2(255)";
					Db.NonQ(command);
					command=@"CREATE INDEX vitalsign_WeightCode ON vitalsign (WeightCode)";
					Db.NonQ(command);
				}
				//Add indexes for code systems------------------------------------------------------------------------------------------------------
				try {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="ALTER TABLE diseasedef ADD INDEX (Icd9Code)";
						Db.NonQ(command);
						command="ALTER TABLE diseasedef ADD INDEX (SnomedCode)";
						Db.NonQ(command);
					}
					else {//oracle
						command=@"CREATE INDEX diseasedef_Icd9Code ON diseasedef (Icd9Code)";
						Db.NonQ(command);
						command=@"CREATE INDEX diseasedef_SnomedCode ON diseasedef (SnomedCode)";
						Db.NonQ(command);
					}
				}
				catch(Exception ex) { }//Only an index. (Exception ex) required to catch thrown exception
				try {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="ALTER TABLE icd9 ADD INDEX (Icd9Code)";
						Db.NonQ(command);
					}
					else {//oracle
						command=@"CREATE INDEX icd9_Icd9Code ON icd9 (Icd9Code)";
						Db.NonQ(command);
					}
				}
				catch(Exception ex) { }//Only an index. (Exception ex) required to catch thrown exception
				try {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="ALTER TABLE rxnorm ADD INDEX (RxCui)";
						Db.NonQ(command);
					}
					else {//oracle
						command=@"CREATE INDEX rxnorm_RxCui ON rxnorm (RxCui)";
						Db.NonQ(command);
					}
				}	
				catch(Exception ex) { }//Only an index. (Exception ex) required to catch thrown exception
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE insplan ADD SopCode varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE insplan ADD SopCode varchar2(255)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS payortype";
					Db.NonQ(command);
					command=@"CREATE TABLE payortype (
						PayorTypeNum bigint NOT NULL auto_increment PRIMARY KEY,
						PatNum bigint NOT NULL,
						DateStart date NOT NULL DEFAULT '0001-01-01',
						SopCode varchar(255) NOT NULL,
						Note text NOT NULL,
						INDEX(PatNum),
						INDEX(SopCode)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE payortype'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE payortype (
						PayorTypeNum number(20) NOT NULL,
						PatNum number(20) NOT NULL,
						DateStart date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						SopCode varchar2(255),
						Note varchar2(2000),
						CONSTRAINT payortype_PayorTypeNum PRIMARY KEY (PayorTypeNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX payortype_PatNum ON payortype (PatNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX payortype_SopCode ON payortype (SopCode)";
					Db.NonQ(command);
				}
				//oracle compatible
				command="UPDATE patientrace SET CdcrecCode='2054-5' WHERE Race=1";//AfricanAmerican
				Db.NonQ(command);
				command="UPDATE patientrace SET CdcrecCode='1002-5' WHERE Race=2";//AmericanIndian
				Db.NonQ(command);
				command="UPDATE patientrace SET CdcrecCode='2028-9' WHERE Race=3";//Asian
				Db.NonQ(command);
				command="UPDATE patientrace SET CdcrecCode='2076-8' WHERE Race=5";//HawaiiOrPacIsland
				Db.NonQ(command);
				command="UPDATE patientrace SET CdcrecCode='2135-2' WHERE Race=6";//Hispanic
				Db.NonQ(command);
				command="UPDATE patientrace SET CdcrecCode='2131-1' WHERE Race=8";//Other
				Db.NonQ(command);
				command="UPDATE patientrace SET CdcrecCode='2106-3' WHERE Race=9";//White
				Db.NonQ(command);
				command="UPDATE patientrace SET CdcrecCode='2186-5' WHERE Race=10";//NotHispanic
				Db.NonQ(command);
				//oracle compatible
				//We will insert another patientrace row specifying 'NotHispanic' if there is not a Hispanic entry or a DeclinedToSpecify entry but there is at least one other patientrace entry.  The absence of ethnicity was assumed NotHispanic in the past, now we are going to explicitly store that value.  enum=10, CdcrecCode='2186-5'
				command="SELECT DISTINCT PatNum FROM patientrace WHERE PatNum NOT IN(SELECT PatNum FROM patientrace WHERE Race IN(4,6))";//4=DeclinedToSpecify,6=Hispanic
				DataTable table=Db.GetTable(command);
				long patNum=0;
				if(DataConnection.DBtype==DatabaseType.MySql) {
					for(int i=0;i<table.Rows.Count;i++) {
						patNum=PIn.Long(table.Rows[i]["PatNum"].ToString());
						command="INSERT INTO patientrace (PatNum,Race,CdcrecCode) VALUES("+POut.Long(patNum)+",10,'2186-5')";
						Db.NonQ(command);
					}
				}
				else {//oracle
					for(int i=0;i<table.Rows.Count;i++) {
						patNum=PIn.Long(table.Rows[i]["PatNum"].ToString());
						command="INSERT INTO patientrace (PatientRaceNum,PatNum,Race,CdcrecCode) "
							+"VALUES((SELECT MAX(PatientRaceNum)+1 FROM patientrace),"+POut.Long(patNum)+",10,'2186-5')";
						Db.NonQ(command);
					}
				}
				//intervention
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS intervention";
					Db.NonQ(command);
					command=@"CREATE TABLE intervention (
						InterventionNum bigint NOT NULL auto_increment PRIMARY KEY,
						PatNum bigint NOT NULL,
						ProvNum bigint NOT NULL,
						CodeValue varchar(30) NOT NULL,
						CodeSystem varchar(30) NOT NULL,
						Note text NOT NULL,
						DateTimeEntry date NOT NULL DEFAULT '0001-01-01',
						CodeSet tinyint NOT NULL,
						INDEX(PatNum),
						INDEX(ProvNum),
						INDEX(CodeValue)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE intervention'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE intervention (
						InterventionNum number(20) NOT NULL,
						PatNum number(20) NOT NULL,
						ProvNum number(20) NOT NULL,
						CodeValue varchar2(30),
						CodeSystem varchar2(30),
						Note varchar2(4000),
						CodeSet number(3) NOT NULL,
						DateTimeEntry date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						CONSTRAINT intervention_InterventionNum PRIMARY KEY (InterventionNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX intervention_PatNum ON intervention (PatNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX intervention_ProvNum ON intervention (ProvNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX intervention_CodeValue ON intervention (CodeValue)";
					Db.NonQ(command);
				}
				//ehrnotperformed
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS ehrnotperformed";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrnotperformed (
						EhrNotPerformedNum bigint NOT NULL auto_increment PRIMARY KEY,
						PatNum bigint NOT NULL,
						ProvNum bigint NOT NULL,
						CodeValue varchar(30) NOT NULL,
						CodeSystem varchar(30) NOT NULL,
						CodeValueReason varchar(30) NOT NULL,
						CodeSystemReason varchar(30) NOT NULL,
						Note text NOT NULL,
						DateEntry date NOT NULL DEFAULT '0001-01-01',
						INDEX(PatNum),
						INDEX(ProvNum),
						INDEX(CodeValue),
						INDEX(CodeValueReason)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE ehrnotperformed'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrnotperformed (
						EhrNotPerformedNum number(20) NOT NULL,
						PatNum number(20) NOT NULL,
						ProvNum number(20) NOT NULL,
						CodeValue varchar2(30),
						CodeSystem varchar2(30),
						CodeValueReason varchar2(30),
						CodeSystemReason varchar2(30),
						Note varchar2(4000),
						DateEntry date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						CONSTRAINT ehrnotperformed_EhrNotPerforme PRIMARY KEY (EhrNotPerformedNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrnotperformed_PatNum ON ehrnotperformed (PatNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrnotperformed_ProvNum ON ehrnotperformed (ProvNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrnotperformed_CodeValue ON ehrnotperformed (CodeValue)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrnotperformed_CodeValueReaso ON ehrnotperformed (CodeValueReason)";
					Db.NonQ(command);
				}
				//encounter
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS encounter";
					Db.NonQ(command);
					command=@"CREATE TABLE encounter (
						EncounterNum bigint NOT NULL auto_increment PRIMARY KEY,
						PatNum bigint NOT NULL,
						ProvNum bigint NOT NULL,
						CodeValue varchar(30) NOT NULL,
						CodeSystem varchar(30) NOT NULL,
						Note text NOT NULL,
						DateEncounter date NOT NULL DEFAULT '0001-01-01',
						INDEX(PatNum),
						INDEX(ProvNum),
						INDEX(CodeValue)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE encounter'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE encounter (
						EncounterNum number(20) NOT NULL,
						PatNum number(20) NOT NULL,
						ProvNum number(20) NOT NULL,
						CodeValue varchar2(30),
						CodeSystem varchar2(30),
						Note varchar2(4000),
						DateEncounter date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						CONSTRAINT encounter_EncounterNum PRIMARY KEY (EncounterNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX encounter_PatNum ON encounter (PatNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX encounter_ProvNum ON encounter (ProvNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX encounter_CodeValue ON encounter (CodeValue)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('NistTimeServerUrl','nist-time-server.eoni.com')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'NistTimeServerUrl','nist-time-server.eoni.com')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE ehrsummaryccd ADD EmailAttachNum bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE ehrsummaryccd ADD INDEX (EmailAttachNum)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE ehrsummaryccd ADD EmailAttachNum number(20)";
					Db.NonQ(command);
					command="UPDATE ehrsummaryccd SET EmailAttachNum = 0 WHERE EmailAttachNum IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE ehrsummaryccd MODIFY EmailAttachNum NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrsummaryccd_EmailAttachNum ON ehrsummaryccd (EmailAttachNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE vitalsign ADD HeightExamCode varchar(30) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE vitalsign ADD HeightExamCode varchar2(30)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE vitalsign ADD WeightExamCode varchar(30) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE vitalsign ADD WeightExamCode varchar2(30)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE vitalsign ADD BMIExamCode varchar(30) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE vitalsign ADD BMIExamCode varchar2(30)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE vitalsign ADD EhrNotPerformedNum bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE vitalsign ADD INDEX (EhrNotPerformedNum)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE vitalsign ADD EhrNotPerformedNum number(20)";
					Db.NonQ(command);
					command="UPDATE vitalsign SET EhrNotPerformedNum = 0 WHERE EhrNotPerformedNum IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE vitalsign MODIFY EhrNotPerformedNum NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX vitalsign_EhrNotPerformedNum ON vitalsign (EhrNotPerformedNum)";
					Db.NonQ(command);
				}
				//Add exam codes to vital sign rows currently in the db using the most generic code from each set
				command="UPDATE vitalsign SET HeightExamCode='8302-2' WHERE Height!=0";//8302-2 is "Body height"
				Db.NonQ(command);
				command="UPDATE vitalsign SET WeightExamCode='29463-7' WHERE Weight!=0";//29463-7 is "Body weight"
				Db.NonQ(command);
				command="UPDATE vitalsign SET BMIExamCode='59574-4' WHERE Height!=0 AND Weight!=0";//59574-4 is "Body mass index (BMI) [Percentile]"
				Db.NonQ(command);
				if(DataConnection.DBtype==DatabaseType.MySql) {
					//Update over/underweight code, only 1 LOINC code for overweight and 1 for underweight
					//Based on age before the start of the measurement period, which is age before any birthday in the year of DateTaken.  Different range for 18-64 and 65+.  Under 18 not classified under/over
					command="UPDATE vitalsign,patient SET WeightCode='238131007'/*Overweight*/ "
						+"WHERE patient.PatNum=vitalsign.PatNum AND Height!=0 AND Weight!=0 "
						+"AND Birthdate>'1880-01-01' AND ("
						+"(YEAR(DateTaken)-YEAR(Birthdate)-1>=65 AND (Weight*703)/(Height*Height)>=30) "
						+"OR "
						+"(YEAR(DateTaken)-YEAR(Birthdate)-1 BETWEEN 18 AND 64 AND (Weight*703)/(Height*Height)>=25))";
					Db.NonQ(command);
					command="UPDATE vitalsign,patient	SET WeightCode='248342006'/*Underweight*/ "
						+"WHERE patient.PatNum=vitalsign.PatNum	AND Height!=0 AND Weight!=0 "
						+"AND Birthdate>'1880-01-01' AND ("
						+"(YEAR(DateTaken)-YEAR(patient.Birthdate)-1>=65 AND (Weight*703)/(Height*Height)<23) "
						+"OR "
						+"(YEAR(DateTaken)-YEAR(patient.Birthdate)-1 BETWEEN 18 AND 64 AND (Weight*703)/(Height*Height)<18.5))";
					Db.NonQ(command);
				}
				else {//oracle
					//EHR not oracle compatible so the vital sign WeightCode will not be used, only for ehr reporting
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE vitalsign ADD PregDiseaseNum bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE vitalsign ADD INDEX (PregDiseaseNum)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE vitalsign ADD PregDiseaseNum number(20)";
					Db.NonQ(command);
					command="UPDATE vitalsign SET PregDiseaseNum = 0 WHERE PregDiseaseNum IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE vitalsign MODIFY PregDiseaseNum NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX vitalsign_PregDiseaseNum ON vitalsign (PregDiseaseNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('CQMDefaultEncounterCodeValue','none')";//we cannot preset this to a SNOMEDCT code since the customer may not be US
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'CQMDefaultEncounterCodeValue','none')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('CQMDefaultEncounterCodeSystem','')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'CQMDefaultEncounterCodeSystem','')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('PregnancyDefaultCodeValue','none')";//we cannot preset this to a SNOMEDCT code since the customer may not be US
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'PregnancyDefaultCodeValue','none')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('PregnancyDefaultCodeSystem','')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'PregnancyDefaultCodeSystem','')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE diseasedef ADD Icd10Code varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE diseasedef ADD Icd10Code varchar2(255)";
					Db.NonQ(command);
				}
				//Add indexes for code systems------------------------------------------------------------------------------------------------------
				try {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="ALTER TABLE diseasedef ADD INDEX (Icd10Code)";
						Db.NonQ(command);
					}
					else {//oracle
						command=@"CREATE INDEX diseasedef_Icd10Code ON diseasedef (Icd10Code)";
						Db.NonQ(command);
					}
				}
				catch(Exception ex) { }//Only an index. (Exception ex) required to catch thrown exception
				//Add indexes to speed up payroll------------------------------------------------------------------------------------------------------
				try {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="ALTER TABLE clockevent ADD INDEX (TimeDisplayed1)";
						Db.NonQ(command);
					}
					else {//oracle
						command=@"CREATE INDEX clockevent_TimeDisplayed1 ON clockevent (TimeDisplayed1)";
						Db.NonQ(command);
					}
				}
				catch(Exception ex) { }//Only an index. (Exception ex) required to catch thrown exception
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName) VALUES('ADPCompanyCode')";//No default value.
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'ADPCompanyCode')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE employee ADD PayrollID varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE employee ADD PayrollID varchar2(255)";
					Db.NonQ(command);
				}
				//Add indexes to speed up customer management window------------------------------------------------------------------------------------
				try {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="ALTER TABLE registrationkey ADD INDEX (PatNum)";
						Db.NonQ(command);
					}
					else {//oracle
						command=@"CREATE INDEX registrationkey_PatNum ON clockevent (PatNum)";
						Db.NonQ(command);
					}
				}
				catch(Exception ex) { }//Only an index. (Exception ex) required to catch thrown exception
				try {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="ALTER TABLE repeatcharge ADD INDEX (PatNum)";
						Db.NonQ(command);
					}
					else {//oracle
						command=@"CREATE INDEX repeatcharge_PatNum ON clockevent (PatNum)";
						Db.NonQ(command);
					}
				}
				catch(Exception ex) { }//Only an index. (Exception ex) required to catch thrown exception
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE allergydef CHANGE Snomed SnomedType tinyint";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE allergydef RENAME COLUMN Snomed TO SnomedType";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE allergydef ADD SnomedAllergyTo varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE allergydef ADD SnomedAllergyTo varchar2(255)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE allergy ADD SnomedReaction varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE allergy ADD SnomedReaction varchar2(255)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE dunning ADD EmailSubject varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE dunning ADD EmailSubject varchar2(255)";
					Db.NonQ(command);
				} 
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE dunning ADD EmailBody mediumtext NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE dunning ADD EmailBody clob";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE statement ADD EmailSubject varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE statement ADD EmailSubject varchar2(255)";
					Db.NonQ(command);
				} 
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE statement ADD EmailBody mediumtext NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE statement ADD EmailBody clob";
					Db.NonQ(command);
				}				
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS maparea";
					Db.NonQ(command);
					command=@"CREATE TABLE maparea (
						MapAreaNum bigint NOT NULL auto_increment PRIMARY KEY,
						Extension int NOT NULL,
						XPos double NOT NULL,
						YPos double NOT NULL,
						Width double NOT NULL,
						Height double NOT NULL,
						Description varchar(255) NOT NULL,
						ItemType tinyint NOT NULL
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE maparea'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE maparea (
						MapAreaNum number(20) NOT NULL,
						Extension number(11) NOT NULL,
						XPos number(38,8) NOT NULL,
						YPos number(38,8) NOT NULL,
						Width number(38,8) NOT NULL,
						Height number(38,8) NOT NULL,
						Description varchar2(255),
						ItemType number(3) NOT NULL,
						CONSTRAINT maparea_MapAreaNum PRIMARY KEY (MapAreaNum)
						)";
					Db.NonQ(command);
				}
				command="UPDATE preference SET ValueString = '13.3.1.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To13_3_4();
		}

		private static void To13_3_4() {
			if(FromVersion<new Version("13.3.4.0")) {
				string command;
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE emailmessage ADD RecipientAddress varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE emailmessage ADD RecipientAddress varchar2(255)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS emailmessageuid";
					Db.NonQ(command);
					command=@"CREATE TABLE emailmessageuid (
						EmailMessageUidNum bigint NOT NULL auto_increment PRIMARY KEY,
						Uid text NOT NULL,
						RecipientAddress varchar(255) NOT NULL
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE emailmessageuid'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE emailmessageuid (
						EmailMessageUidNum number(20) NOT NULL,
						""Uid"" varchar2(4000),
						RecipientAddress varchar2(255),
						CONSTRAINT emailmessageuid_EmailMessageUi PRIMARY KEY (EmailMessageUidNum)
						)";
					Db.NonQ(command);
				}
				command="UPDATE preference SET ValueString = '13.3.4.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To13_3_5();
		}

		private static void To13_3_5() {
			if(FromVersion<new Version("13.3.5.0")) {
				string command;
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE erxlog ADD ProvNum bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE erxlog ADD INDEX (ProvNum)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE erxlog ADD ProvNum number(20)";
					Db.NonQ(command);
					command="UPDATE erxlog SET ProvNum = 0 WHERE ProvNum IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE erxlog MODIFY ProvNum NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX erxlog_ProvNum ON erxlog (ProvNum)";
					Db.NonQ(command);
				}
				command="UPDATE preference SET ValueString = '13.3.5.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To13_3_6();
		}

		///<summary>Oracle compatible: 12/26/2013</summary>
		private static void To13_3_6() {
			if(FromVersion<new Version("13.3.6.0")) {
				string command;
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE emailmessage ADD RawEmailIn longtext NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE emailmessage ADD RawEmailIn clob";
					Db.NonQ(command);
				}
				command="UPDATE preference SET ValueString = '13.3.6.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To13_3_7();
		}

		private static void To13_3_7() {
			if(FromVersion<new Version("13.3.7.0")) {
				string command;
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE emailmessageuid CHANGE Uid MsgId text";
					Db.NonQ(command);
				}
				else {//oracle
					command=@"ALTER TABLE emailmessageuid RENAME COLUMN ""Uid"" TO MsgId";
					Db.NonQ(command);
				}
				command="UPDATE preference SET ValueString = '13.3.7.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To14_1_1();
		}

		private static void To14_1_1() {
			if(FromVersion<new Version("14.1.1.0")) {
				string command;
				//Added permission EhrShowCDS.     No one has this permission by default.  This is more like a user level preference than a permission.
				//Added permission EhrInfoButton.  No one has this permission by default.  This is more like a user level preference than a permission.
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS ehrtrigger";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrtrigger (
						EhrTriggerNum bigint NOT NULL auto_increment PRIMARY KEY,
						Description varchar(255) NOT NULL,
						ProblemSnomedList text NOT NULL,
						ProblemIcd9List text NOT NULL,
						ProblemIcd10List text NOT NULL,
						ProblemDefNumList text NOT NULL,
						MedicationNumList text NOT NULL,
						RxCuiList text NOT NULL,
						CvxList text NOT NULL,
						AllergyDefNumList text NOT NULL,
						DemographicsList text NOT NULL,
						LabLoincList text NOT NULL,
						VitalLoincList text NOT NULL,
						Instructions text NOT NULL,
						Bibliography text NOT NULL,
						Cardinality tinyint NOT NULL
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE ehrtrigger'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrtrigger (
						EhrTriggerNum number(20) NOT NULL,
						Description varchar2(255),
						ProblemSnomedList varchar2(4000),
						ProblemIcd9List varchar2(4000),
						ProblemIcd10List varchar2(4000),
						ProblemDefNumList varchar2(4000),
						MedicationNumList varchar2(4000),
						RxCuiList varchar2(4000),
						CvxList varchar2(4000),
						AllergyDefNumList varchar2(4000),
						DemographicsList varchar2(4000),
						LabLoincList varchar2(4000),
						VitalLoincList varchar2(4000),
						Instructions varchar2(4000),
						Bibliography varchar2(4000),
						Cardinality number(3) NOT NULL,
						CONSTRAINT ehrtrigger_EhrTriggerNum PRIMARY KEY (EhrTriggerNum)
						)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE provider ADD EmailAddressNum bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE provider ADD INDEX (EmailAddressNum)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE provider ADD EmailAddressNum number(20)";
					Db.NonQ(command);
					command="UPDATE provider SET EmailAddressNum = 0 WHERE EmailAddressNum IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE provider MODIFY EmailAddressNum NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX provider_EmailAddressNum ON provider (EmailAddressNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE ehrmeasureevent ADD CodeValueEvent varchar(30) NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE ehrmeasureevent ADD INDEX (CodeValueEvent)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE ehrmeasureevent ADD CodeValueEvent varchar2(30)";
					Db.NonQ(command);
					command="CREATE INDEX ehrmeasureevent_CodeValueEvent ON ehrmeasureevent (CodeValueEvent)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE ehrmeasureevent ADD CodeSystemEvent varchar(30) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE ehrmeasureevent ADD CodeSystemEvent varchar2(30)";
					Db.NonQ(command);
				}
				//oracle compatible
				command="UPDATE ehrmeasureevent SET CodeValueEvent='11366-2' WHERE EventType=8";//Set all TobaccoUseAssessed ehrmeasureevents to code for 'History of tobacco use Narrative'
				Db.NonQ(command);
				command="UPDATE ehrmeasureevent SET CodeSystemEvent='LOINC' WHERE EventType=8";//All TobaccoUseAssessed codes are LOINC codes
				Db.NonQ(command);
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE ehrmeasureevent ADD CodeValueResult varchar(30) NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE ehrmeasureevent ADD INDEX (CodeValueResult)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE ehrmeasureevent ADD CodeValueResult varchar2(30)";
					Db.NonQ(command);
					command="CREATE INDEX ehrmeasureevent_CodeValueResul ON ehrmeasureevent (CodeValueResult)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE ehrmeasureevent ADD CodeSystemResult varchar(30) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE ehrmeasureevent ADD CodeSystemResult varchar2(30)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE intervention CHANGE DateTimeEntry DateEntry date NOT NULL DEFAULT '0001-01-01'";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE intervention RENAME COLUMN DateTimeEntry TO DateEntry";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE ehrsummaryccd MODIFY ContentSummary longtext NOT NULL";
					Db.NonQ(command);
				}
				//oracle ContentSummary data type is already clob which can handle up to 4GB of data.
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE disease ADD SnomedProblemType varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE disease ADD SnomedProblemType varchar2(255)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE disease ADD FunctionStatus tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE disease ADD FunctionStatus number(3)";
					Db.NonQ(command);
					command="UPDATE disease SET FunctionStatus = 0 WHERE FunctionStatus IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE disease MODIFY FunctionStatus NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS ehrcareplan";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrcareplan (
						EhrCarePlanNum bigint NOT NULL auto_increment PRIMARY KEY,
						PatNum bigint NOT NULL,
						SnomedEducation varchar(255) NOT NULL,
						Instructions varchar(255) NOT NULL,
						INDEX(PatNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE ehrcareplan'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrcareplan (
						EhrCarePlanNum number(20) NOT NULL,
						PatNum number(20) NOT NULL,
						SnomedEducation varchar2(255),
						Instructions varchar2(255),
						CONSTRAINT ehrcareplan_EhrCarePlanNum PRIMARY KEY (EhrCarePlanNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrcareplan_PatNum ON ehrcareplan (PatNum)";
					Db.NonQ(command);
				}
				//Add UCUM table
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS ucum";
					Db.NonQ(command);
					command=@"CREATE TABLE ucum (
						UcumNum bigint NOT NULL auto_increment PRIMARY KEY,
						UcumCode varchar(255) NOT NULL,
						Description varchar(255) NOT NULL,
						IsInUse tinyint NOT NULL,
						INDEX(UcumCode)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE ucum'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE ucum (
						UcumNum number(20) NOT NULL,
						UcumCode varchar2(255),
						Description varchar2(255),
						IsInUse number(3) NOT NULL,
						CONSTRAINT ucum_UcumNum PRIMARY KEY (UcumNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX ucum_UcumCode ON ucum (UcumCode)";
					Db.NonQ(command);
				}
				//Add UCUM to Code System Importer
				command=@"INSERT INTO codesystem (CodeSystemNum,CodeSystemName,HL7OID) VALUES (13,'UCUM','2.16.840.1.113883.6.8')";
				Db.NonQ(command);
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE ehrcareplan ADD DatePlanned date NOT NULL DEFAULT '0001-01-01'";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE ehrcareplan ADD DatePlanned date";
					Db.NonQ(command);
					command="UPDATE ehrcareplan SET DatePlanned = TO_DATE('0001-01-01','YYYY-MM-DD') WHERE DatePlanned IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE ehrcareplan MODIFY DatePlanned NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('PatientPortalNotifyBody','Please go to this link and login using your credentials. [URL]')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'PatientPortalNotifyBody','Please go to this link and login using your credentials. [URL]')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('PatientPortalNotifySubject','You have a secure message waiting for you')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'PatientPortalNotifySubject','You have a secure message waiting for you')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE emailmessage ADD ProvNumWebMail bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE emailmessage ADD INDEX (ProvNumWebMail)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE emailmessage ADD ProvNumWebMail number(20)";
					Db.NonQ(command);
					command="UPDATE emailmessage SET ProvNumWebMail = 0 WHERE ProvNumWebMail IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE emailmessage MODIFY ProvNumWebMail NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX emailmessage_ProvNumWebMail ON emailmessage (ProvNumWebMail)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE emailmessage ADD PatNumSubj bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE emailmessage ADD INDEX (PatNumSubj)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE emailmessage ADD PatNumSubj number(20)";
					Db.NonQ(command);
					command="UPDATE emailmessage SET PatNumSubj = 0 WHERE PatNumSubj IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE emailmessage MODIFY PatNumSubj NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX emailmessage_PatNumSubj ON emailmessage (PatNumSubj)";
					Db.NonQ(command);
				}
				//Added Table cdspermission
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS cdspermission";
					Db.NonQ(command);
					command=@"CREATE TABLE cdspermission (
						CDSPermissionNum bigint NOT NULL auto_increment PRIMARY KEY,
						UserNum bigint NOT NULL,
						SetupCDS tinyint NOT NULL,
						ShowCDS tinyint NOT NULL,
						ShowInfobutton tinyint NOT NULL,
						EditBibliography tinyint NOT NULL,
						ProblemCDS tinyint NOT NULL,
						MedicationCDS tinyint NOT NULL,
						AllergyCDS tinyint NOT NULL,
						DemographicCDS tinyint NOT NULL,
						LabTestCDS tinyint NOT NULL,
						VitalCDS tinyint NOT NULL,
						INDEX(UserNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE cdspermission'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE cdspermission (
						CDSPermissionNum number(20) NOT NULL,
						UserNum number(20) NOT NULL,
						SetupCDS number(3) NOT NULL,
						ShowCDS number(3) NOT NULL,
						ShowInfobutton number(3) NOT NULL,
						EditBibliography number(3) NOT NULL,
						ProblemCDS number(3) NOT NULL,
						MedicationCDS number(3) NOT NULL,
						AllergyCDS number(3) NOT NULL,
						DemographicCDS number(3) NOT NULL,
						LabTestCDS number(3) NOT NULL,
						VitalCDS number(3) NOT NULL,
						CONSTRAINT cdspermission_CDSPermissionNum PRIMARY KEY (CDSPermissionNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX cdspermission_UserNum ON cdspermission (UserNum)";
					Db.NonQ(command);
				}
				#region EHR Lab framework (never going to be used)
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS ehrlab";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlab (
						EhrLabNum bigint NOT NULL auto_increment PRIMARY KEY,
						PatNum bigint NOT NULL,
						EhrLabMessageNum bigint NOT NULL,
						OrderControlCode varchar(255) NOT NULL,
						PlacerOrderNum varchar(255) NOT NULL,
						PlacerOrderNamespace varchar(255) NOT NULL,
						PlacerOrderUniversalID varchar(255) NOT NULL,
						PlacerOrderUniversalIDType varchar(255) NOT NULL,
						FillerOrderNum varchar(255) NOT NULL,
						FillerOrderNamespace varchar(255) NOT NULL,
						FillerOrderUniversalID varchar(255) NOT NULL,
						FillerOrderUniversalIDType varchar(255) NOT NULL,
						PlacerGroupNum varchar(255) NOT NULL,
						PlacerGroupNamespace varchar(255) NOT NULL,
						PlacerGroupUniversalID varchar(255) NOT NULL,
						PlacerGroupUniversalIDType varchar(255) NOT NULL,
						OrderingProviderID varchar(255) NOT NULL,
						OrderingProviderLName varchar(255) NOT NULL,
						OrderingProviderFName varchar(255) NOT NULL,
						OrderingProviderMiddleNames varchar(255) NOT NULL,
						OrderingProviderSuffix varchar(255) NOT NULL,
						OrderingProviderPrefix varchar(255) NOT NULL,
						OrderingProviderAssigningAuthorityNamespaceID varchar(255) NOT NULL,
						OrderingProviderAssigningAuthorityUniversalID varchar(255) NOT NULL,
						OrderingProviderAssigningAuthorityIDType varchar(255) NOT NULL,
						OrderingProviderNameTypeCode varchar(255) NOT NULL,
						OrderingProviderIdentifierTypeCode varchar(255) NOT NULL,
						SetIdOBR bigint NOT NULL,
						UsiID varchar(255) NOT NULL,
						UsiText varchar(255) NOT NULL,
						UsiCodeSystemName varchar(255) NOT NULL,
						UsiIDAlt varchar(255) NOT NULL,
						UsiTextAlt varchar(255) NOT NULL,
						UsiCodeSystemNameAlt varchar(255) NOT NULL,
						UsiTextOriginal varchar(255) NOT NULL,
						ObservationDateTimeStart varchar(255) NOT NULL,
						ObservationDateTimeEnd varchar(255) NOT NULL,
						SpecimenActionCode varchar(255) NOT NULL,
						ResultDateTime varchar(255) NOT NULL,
						ResultStatus varchar(255) NOT NULL,
						ParentObservationID varchar(255) NOT NULL,
						ParentObservationText varchar(255) NOT NULL,
						ParentObservationCodeSystemName varchar(255) NOT NULL,
						ParentObservationIDAlt varchar(255) NOT NULL,
						ParentObservationTextAlt varchar(255) NOT NULL,
						ParentObservationCodeSystemNameAlt varchar(255) NOT NULL,
						ParentObservationTextOriginal varchar(255) NOT NULL,
						ParentObservationSubID varchar(255) NOT NULL,
						ParentPlacerOrderNum varchar(255) NOT NULL,
						ParentPlacerOrderNamespace varchar(255) NOT NULL,
						ParentPlacerOrderUniversalID varchar(255) NOT NULL,
						ParentPlacerOrderUniversalIDType varchar(255) NOT NULL,
						ParentFillerOrderNum varchar(255) NOT NULL,
						ParentFillerOrderNamespace varchar(255) NOT NULL,
						ParentFillerOrderUniversalID varchar(255) NOT NULL,
						ParentFillerOrderUniversalIDType varchar(255) NOT NULL,
						ListEhrLabResultsHandlingF tinyint NOT NULL,
						ListEhrLabResultsHandlingN tinyint NOT NULL,
						TQ1SetId bigint NOT NULL,
						TQ1DateTimeStart varchar(255) NOT NULL,
						TQ1DateTimeEnd varchar(255) NOT NULL,
						INDEX(PatNum),
						INDEX(EhrLabMessageNum),
						INDEX(SetIdOBR),
						INDEX(TQ1SetId)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					//EHR lab features have been hidden for Oracle users due to some column names exceeding the 30 character limit.
					//TODO: Fix the column names to be less than 30 characters in the future.
					//	This will require unhiding EHR lab features for Oracle users, altering the table types and columns in the both MySQL and Oracle.
					/*
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE ehrlab'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlab (
						EhrLabNum number(20) NOT NULL,
						PatNum number(20) NOT NULL,
						EhrLabMessageNum number(20) NOT NULL,
						OrderControlCode varchar2(255),
						PlacerOrderNum varchar2(255),
						PlacerOrderNamespace varchar2(255),
						PlacerOrderUniversalID varchar2(255),
						PlacerOrderUniversalIDType varchar2(255),
						FillerOrderNum varchar2(255),
						FillerOrderNamespace varchar2(255),
						FillerOrderUniversalID varchar2(255),
						FillerOrderUniversalIDType varchar2(255),
						PlacerGroupNum varchar2(255),
						PlacerGroupNamespace varchar2(255),
						PlacerGroupUniversalID varchar2(255),
						PlacerGroupUniversalIDType varchar2(255),
						OrderingProviderID varchar2(255),
						OrderingProviderLName varchar2(255),
						OrderingProviderFName varchar2(255),
						OrderingProviderMiddleNames varchar2(255),
						OrderingProviderSuffix varchar2(255),
						OrderingProviderPrefix varchar2(255),
						OrderingProviderAssigningAuthorityNamespaceID varchar2(255),
						OrderingProviderAssigningAuthorityUniversalID varchar2(255),
						OrderingProviderAssigningAuthorityIDType varchar2(255),
						OrderingProviderNameTypeCode varchar2(255),
						OrderingProviderIdentifierTypeCode varchar2(255),
						SetIdOBR number(20) NOT NULL,
						UsiID varchar2(255),
						UsiText varchar2(255),
						UsiCodeSystemName varchar2(255),
						UsiIDAlt varchar2(255),
						UsiTextAlt varchar2(255),
						UsiCodeSystemNameAlt varchar2(255),
						UsiTextOriginal varchar2(255),
						ObservationDateTimeStart varchar2(255),
						ObservationDateTimeEnd varchar2(255),
						SpecimenActionCode varchar2(255),
						ResultDateTime varchar2(255),
						ResultStatus varchar2(255),
						ParentObservationID varchar2(255),
						ParentObservationText varchar2(255),
						ParentObservationCodeSystemName varchar2(255),
						ParentObservationIDAlt varchar2(255),
						ParentObservationTextAlt varchar2(255),
						ParentObservationCodeSystemNameAlt varchar2(255),
						ParentObservationTextOriginal varchar2(255),
						ParentObservationSubID varchar2(255),
						ParentPlacerOrderNum varchar2(255),
						ParentPlacerOrderNamespace varchar2(255),
						ParentPlacerOrderUniversalID varchar2(255),
						ParentPlacerOrderUniversalIDType varchar2(255),
						ParentFillerOrderNum varchar2(255),
						ParentFillerOrderNamespace varchar2(255),
						ParentFillerOrderUniversalID varchar2(255),
						ParentFillerOrderUniversalIDType varchar2(255),
						ListEhrLabResultsHandlingF number(3) NOT NULL,
						ListEhrLabResultsHandlingN number(3) NOT NULL,
						TQ1SetId number(20) NOT NULL,
						TQ1DateTimeStart varchar2(255),
						TQ1DateTimeEnd varchar2(255),
						CONSTRAINT ehrlab_EhrLabNum PRIMARY KEY (EhrLabNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlab_PatNum ON ehrlab (PatNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlab_EhrLabMessageNum ON ehrlab (EhrLabMessageNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlab_SetIdOBR ON ehrlab (SetIdOBR)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlab_TQ1SetId ON ehrlab (TQ1SetId)";
					Db.NonQ(command);
					 */
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS ehrlabresult";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlabresult (
						EhrLabResultNum bigint NOT NULL auto_increment PRIMARY KEY,
						EhrLabNum bigint NOT NULL,
						SetIdOBX bigint NOT NULL,
						ValueType varchar(255) NOT NULL,
						ObservationIdentifierID varchar(255) NOT NULL,
						ObservationIdentifierText varchar(255) NOT NULL,
						ObservationIdentifierCodeSystemName varchar(255) NOT NULL,
						ObservationIdentifierIDAlt varchar(255) NOT NULL,
						ObservationIdentifierTextAlt varchar(255) NOT NULL,
						ObservationIdentifierCodeSystemNameAlt varchar(255) NOT NULL,
						ObservationIdentifierTextOriginal varchar(255) NOT NULL,
						ObservationIdentifierSub varchar(255) NOT NULL,
						ObservationValueCodedElementID varchar(255) NOT NULL,
						ObservationValueCodedElementText varchar(255) NOT NULL,
						ObservationValueCodedElementCodeSystemName varchar(255) NOT NULL,
						ObservationValueCodedElementIDAlt varchar(255) NOT NULL,
						ObservationValueCodedElementTextAlt varchar(255) NOT NULL,
						ObservationValueCodedElementCodeSystemNameAlt varchar(255) NOT NULL,
						ObservationValueCodedElementTextOriginal varchar(255) NOT NULL,
						ObservationValueDateTime varchar(255) NOT NULL,
						ObservationValueTime time NOT NULL DEFAULT '00:00:00',
						ObservationValueComparator varchar(255) NOT NULL,
						ObservationValueNumber1 double NOT NULL,
						ObservationValueSeparatorOrSuffix varchar(255) NOT NULL,
						ObservationValueNumber2 double NOT NULL,
						ObservationValueNumeric double NOT NULL,
						ObservationValueText varchar(255) NOT NULL,
						UnitsID varchar(255) NOT NULL,
						UnitsText varchar(255) NOT NULL,
						UnitsCodeSystemName varchar(255) NOT NULL,
						UnitsIDAlt varchar(255) NOT NULL,
						UnitsTextAlt varchar(255) NOT NULL,
						UnitsCodeSystemNameAlt varchar(255) NOT NULL,
						UnitsTextOriginal varchar(255) NOT NULL,
						referenceRange varchar(255) NOT NULL,
						AbnormalFlags varchar(255) NOT NULL,
						ObservationResultStatus varchar(255) NOT NULL,
						ObservationDateTime varchar(255) NOT NULL,
						AnalysisDateTime varchar(255) NOT NULL,
						PerformingOrganizationName varchar(255) NOT NULL,
						PerformingOrganizationNameAssigningAuthorityNamespaceId varchar(255) NOT NULL,
						PerformingOrganizationNameAssigningAuthorityUniversalId varchar(255) NOT NULL,
						PerformingOrganizationNameAssigningAuthorityUniversalIdType varchar(255) NOT NULL,
						PerformingOrganizationIdentifierTypeCode varchar(255) NOT NULL,
						PerformingOrganizationIdentifier varchar(255) NOT NULL,
						PerformingOrganizationAddressStreet varchar(255) NOT NULL,
						PerformingOrganizationAddressOtherDesignation varchar(255) NOT NULL,
						PerformingOrganizationAddressCity varchar(255) NOT NULL,
						PerformingOrganizationAddressStateOrProvince varchar(255) NOT NULL,
						PerformingOrganizationAddressZipOrPostalCode varchar(255) NOT NULL,
						PerformingOrganizationAddressCountryCode varchar(255) NOT NULL,
						PerformingOrganizationAddressAddressType varchar(255) NOT NULL,
						PerformingOrganizationAddressCountyOrParishCode varchar(255) NOT NULL,
						MedicalDirectorID varchar(255) NOT NULL,
						MedicalDirectorLName varchar(255) NOT NULL,
						MedicalDirectorFName varchar(255) NOT NULL,
						MedicalDirectorMiddleNames varchar(255) NOT NULL,
						MedicalDirectorSuffix varchar(255) NOT NULL,
						MedicalDirectorPrefix varchar(255) NOT NULL,
						MedicalDirectorAssigningAuthorityNamespaceID varchar(255) NOT NULL,
						MedicalDirectorAssigningAuthorityUniversalID varchar(255) NOT NULL,
						MedicalDirectorAssigningAuthorityIDType varchar(255) NOT NULL,
						MedicalDirectorNameTypeCode varchar(255) NOT NULL,
						MedicalDirectorIdentifierTypeCode varchar(255) NOT NULL,
						INDEX(EhrLabNum),
						INDEX(SetIdOBX)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					//EHR lab features have been hidden for Oracle users due to some column names exceeding the 30 character limit.
					//TODO: Fix the column names to be less than 30 characters in the future.
					//	This will require unhiding EHR lab features for Oracle users, altering the table types and columns in the both MySQL and Oracle.
					/*
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE ehrlabresult'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlabresult (
						EhrLabResultNum number(20) NOT NULL,
						EhrLabNum number(20) NOT NULL,
						SetIdOBX number(20) NOT NULL,
						ValueType varchar2(255),
						ObservationIdentifierID varchar2(255),
						ObservationIdentifierText varchar2(255),
						ObservationIdentifierCodeSystemName varchar2(255),
						ObservationIdentifierIDAlt varchar2(255),
						ObservationIdentifierTextAlt varchar2(255),
						ObservationIdentifierCodeSystemNameAlt varchar2(255),
						ObservationIdentifierTextOriginal varchar2(255),
						ObservationIdentifierSub varchar2(255),
						ObservationValueCodedElementID varchar2(255),
						ObservationValueCodedElementText varchar2(255),
						ObservationValueCodedElementCodeSystemName varchar2(255),
						ObservationValueCodedElementIDAlt varchar2(255),
						ObservationValueCodedElementTextAlt varchar2(255),
						ObservationValueCodedElementCodeSystemNameAlt varchar2(255),
						ObservationValueCodedElementTextOriginal varchar2(255),
						ObservationValueDateTime varchar2(255),
						ObservationValueTime date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						ObservationValueComparator varchar2(255),
						ObservationValueNumber1 number(38,8) NOT NULL,
						ObservationValueSeparatorOrSuffix varchar2(255),
						ObservationValueNumber2 number(38,8) NOT NULL,
						ObservationValueNumeric number(38,8) NOT NULL,
						ObservationValueText varchar2(255),
						UnitsID varchar2(255),
						UnitsText varchar2(255),
						UnitsCodeSystemName varchar2(255),
						UnitsIDAlt varchar2(255),
						UnitsTextAlt varchar2(255),
						UnitsCodeSystemNameAlt varchar2(255),
						UnitsTextOriginal varchar2(255),
						referenceRange varchar2(255),
						AbnormalFlags varchar2(255),
						ObservationResultStatus varchar2(255),
						ObservationDateTime varchar2(255),
						AnalysisDateTime varchar2(255),
						PerformingOrganizationName varchar2(255),
						PerformingOrganizationNameAssigningAuthorityNamespaceId varchar2(255),
						PerformingOrganizationNameAssigningAuthorityUniversalId varchar2(255),
						PerformingOrganizationNameAssigningAuthorityUniversalIdType varchar2(255),
						PerformingOrganizationIdentifierTypeCode varchar2(255),
						PerformingOrganizationIdentifier varchar2(255),
						PerformingOrganizationAddressStreet varchar2(255),
						PerformingOrganizationAddressOtherDesignation varchar2(255),
						PerformingOrganizationAddressCity varchar2(255),
						PerformingOrganizationAddressStateOrProvince varchar2(255),
						PerformingOrganizationAddressZipOrPostalCode varchar2(255),
						PerformingOrganizationAddressCountryCode varchar2(255),
						PerformingOrganizationAddressAddressType varchar2(255),
						PerformingOrganizationAddressCountyOrParishCode varchar2(255),
						MedicalDirectorID varchar2(255),
						MedicalDirectorLName varchar2(255),
						MedicalDirectorFName varchar2(255),
						MedicalDirectorMiddleNames varchar2(255),
						MedicalDirectorSuffix varchar2(255),
						MedicalDirectorPrefix varchar2(255),
						MedicalDirectorAssigningAuthorityNamespaceID varchar2(255),
						MedicalDirectorAssigningAuthorityUniversalID varchar2(255),
						MedicalDirectorAssigningAuthorityIDType varchar2(255),
						MedicalDirectorNameTypeCode varchar2(255),
						MedicalDirectorIdentifierTypeCode varchar2(255),
						CONSTRAINT ehrlabresult_EhrLabResultNum PRIMARY KEY (EhrLabResultNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlabresult_EhrLabNum ON ehrlabresult (EhrLabNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlabresult_SetIdOBX ON ehrlabresult (SetIdOBX)";
					Db.NonQ(command);
					 */
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS ehrlab";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlab (
						EhrLabNum bigint NOT NULL auto_increment PRIMARY KEY,
						PatNum bigint NOT NULL,
						OrderControlCode varchar(255) NOT NULL,
						PlacerOrderNum varchar(255) NOT NULL,
						PlacerOrderNamespace varchar(255) NOT NULL,
						PlacerOrderUniversalID varchar(255) NOT NULL,
						PlacerOrderUniversalIDType varchar(255) NOT NULL,
						FillerOrderNum varchar(255) NOT NULL,
						FillerOrderNamespace varchar(255) NOT NULL,
						FillerOrderUniversalID varchar(255) NOT NULL,
						FillerOrderUniversalIDType varchar(255) NOT NULL,
						PlacerGroupNum varchar(255) NOT NULL,
						PlacerGroupNamespace varchar(255) NOT NULL,
						PlacerGroupUniversalID varchar(255) NOT NULL,
						PlacerGroupUniversalIDType varchar(255) NOT NULL,
						OrderingProviderID varchar(255) NOT NULL,
						OrderingProviderLName varchar(255) NOT NULL,
						OrderingProviderFName varchar(255) NOT NULL,
						OrderingProviderMiddleNames varchar(255) NOT NULL,
						OrderingProviderSuffix varchar(255) NOT NULL,
						OrderingProviderPrefix varchar(255) NOT NULL,
						OrderingProviderAssigningAuthorityNamespaceID varchar(255) NOT NULL,
						OrderingProviderAssigningAuthorityUniversalID varchar(255) NOT NULL,
						OrderingProviderAssigningAuthorityIDType varchar(255) NOT NULL,
						OrderingProviderNameTypeCode varchar(255) NOT NULL,
						OrderingProviderIdentifierTypeCode varchar(255) NOT NULL,
						SetIdOBR bigint NOT NULL,
						UsiID varchar(255) NOT NULL,
						UsiText varchar(255) NOT NULL,
						UsiCodeSystemName varchar(255) NOT NULL,
						UsiIDAlt varchar(255) NOT NULL,
						UsiTextAlt varchar(255) NOT NULL,
						UsiCodeSystemNameAlt varchar(255) NOT NULL,
						UsiTextOriginal varchar(255) NOT NULL,
						ObservationDateTimeStart varchar(255) NOT NULL,
						ObservationDateTimeEnd varchar(255) NOT NULL,
						SpecimenActionCode varchar(255) NOT NULL,
						ResultDateTime varchar(255) NOT NULL,
						ResultStatus varchar(255) NOT NULL,
						ParentObservationID varchar(255) NOT NULL,
						ParentObservationText varchar(255) NOT NULL,
						ParentObservationCodeSystemName varchar(255) NOT NULL,
						ParentObservationIDAlt varchar(255) NOT NULL,
						ParentObservationTextAlt varchar(255) NOT NULL,
						ParentObservationCodeSystemNameAlt varchar(255) NOT NULL,
						ParentObservationTextOriginal varchar(255) NOT NULL,
						ParentObservationSubID varchar(255) NOT NULL,
						ParentPlacerOrderNum varchar(255) NOT NULL,
						ParentPlacerOrderNamespace varchar(255) NOT NULL,
						ParentPlacerOrderUniversalID varchar(255) NOT NULL,
						ParentPlacerOrderUniversalIDType varchar(255) NOT NULL,
						ParentFillerOrderNum varchar(255) NOT NULL,
						ParentFillerOrderNamespace varchar(255) NOT NULL,
						ParentFillerOrderUniversalID varchar(255) NOT NULL,
						ParentFillerOrderUniversalIDType varchar(255) NOT NULL,
						ListEhrLabResultsHandlingF tinyint NOT NULL,
						ListEhrLabResultsHandlingN tinyint NOT NULL,
						TQ1SetId bigint NOT NULL,
						TQ1DateTimeStart varchar(255) NOT NULL,
						TQ1DateTimeEnd varchar(255) NOT NULL,
						INDEX(PatNum),
						INDEX(SetIdOBR),
						INDEX(TQ1SetId)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					//EHR lab features have been hidden for Oracle users due to some column names exceeding the 30 character limit.
					//TODO: Fix the column names to be less than 30 characters in the future.
					//	This will require unhiding EHR lab features for Oracle users, altering the table types and columns in the both MySQL and Oracle.
					/*
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE ehrlab'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlab (
						EhrLabNum number(20) NOT NULL,
						PatNum number(20) NOT NULL,
						OrderControlCode varchar2(255),
						PlacerOrderNum varchar2(255),
						PlacerOrderNamespace varchar2(255),
						PlacerOrderUniversalID varchar2(255),
						PlacerOrderUniversalIDType varchar2(255),
						FillerOrderNum varchar2(255),
						FillerOrderNamespace varchar2(255),
						FillerOrderUniversalID varchar2(255),
						FillerOrderUniversalIDType varchar2(255),
						PlacerGroupNum varchar2(255),
						PlacerGroupNamespace varchar2(255),
						PlacerGroupUniversalID varchar2(255),
						PlacerGroupUniversalIDType varchar2(255),
						OrderingProviderID varchar2(255),
						OrderingProviderLName varchar2(255),
						OrderingProviderFName varchar2(255),
						OrderingProviderMiddleNames varchar2(255),
						OrderingProviderSuffix varchar2(255),
						OrderingProviderPrefix varchar2(255),
						OrderingProviderAssigningAuthorityNamespaceID varchar2(255),
						OrderingProviderAssigningAuthorityUniversalID varchar2(255),
						OrderingProviderAssigningAuthorityIDType varchar2(255),
						OrderingProviderNameTypeCode varchar2(255),
						OrderingProviderIdentifierTypeCode varchar2(255),
						SetIdOBR number(20) NOT NULL,
						UsiID varchar2(255),
						UsiText varchar2(255),
						UsiCodeSystemName varchar2(255),
						UsiIDAlt varchar2(255),
						UsiTextAlt varchar2(255),
						UsiCodeSystemNameAlt varchar2(255),
						UsiTextOriginal varchar2(255),
						ObservationDateTimeStart varchar2(255),
						ObservationDateTimeEnd varchar2(255),
						SpecimenActionCode varchar2(255),
						ResultDateTime varchar2(255),
						ResultStatus varchar2(255),
						ParentObservationID varchar2(255),
						ParentObservationText varchar2(255),
						ParentObservationCodeSystemName varchar2(255),
						ParentObservationIDAlt varchar2(255),
						ParentObservationTextAlt varchar2(255),
						ParentObservationCodeSystemNameAlt varchar2(255),
						ParentObservationTextOriginal varchar2(255),
						ParentObservationSubID varchar2(255),
						ParentPlacerOrderNum varchar2(255),
						ParentPlacerOrderNamespace varchar2(255),
						ParentPlacerOrderUniversalID varchar2(255),
						ParentPlacerOrderUniversalIDType varchar2(255),
						ParentFillerOrderNum varchar2(255),
						ParentFillerOrderNamespace varchar2(255),
						ParentFillerOrderUniversalID varchar2(255),
						ParentFillerOrderUniversalIDType varchar2(255),
						ListEhrLabResultsHandlingF number(3) NOT NULL,
						ListEhrLabResultsHandlingN number(3) NOT NULL,
						TQ1SetId number(20) NOT NULL,
						TQ1DateTimeStart varchar2(255),
						TQ1DateTimeEnd varchar2(255),
						CONSTRAINT ehrlab_EhrLabNum PRIMARY KEY (EhrLabNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlab_PatNum ON ehrlab (PatNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlab_SetIdOBR ON ehrlab (SetIdOBR)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlab_TQ1SetId ON ehrlab (TQ1SetId)";
					Db.NonQ(command);
					 */
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS ehrlabclinicalinfo";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlabclinicalinfo (
						EhrLabClinicalInfoNum bigint NOT NULL auto_increment PRIMARY KEY,
						EhrLabNum bigint NOT NULL,
						ClinicalInfoID varchar(255) NOT NULL,
						ClinicalInfoText varchar(255) NOT NULL,
						ClinicalInfoCodeSystemName varchar(255) NOT NULL,
						ClinicalInfoIDAlt varchar(255) NOT NULL,
						ClinicalInfoTextAlt varchar(255) NOT NULL,
						ClinicalInfoCodeSystemNameAlt varchar(255) NOT NULL,
						ClinicalInfoTextOriginal varchar(255) NOT NULL,
						INDEX(EhrLabNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE ehrlabclinicalinfo'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlabclinicalinfo (
						EhrLabClinicalInfoNum number(20) NOT NULL,
						EhrLabNum number(20) NOT NULL,
						ClinicalInfoID varchar2(255),
						ClinicalInfoText varchar2(255),
						ClinicalInfoCodeSystemName varchar2(255),
						ClinicalInfoIDAlt varchar2(255),
						ClinicalInfoTextAlt varchar2(255),
						ClinicalInfoCodeSystemNameAlt varchar2(255),
						ClinicalInfoTextOriginal varchar2(255),
						CONSTRAINT ehrlabclinicalinfo_EhrLabClini PRIMARY KEY (EhrLabClinicalInfoNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlabclinicalinfo_EhrLabNum ON ehrlabclinicalinfo (EhrLabNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS ehrlabnote";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlabnote (
						EhrLabNoteNum bigint NOT NULL auto_increment PRIMARY KEY,
						EhrLabNum bigint NOT NULL,
						EhrLabResultNum bigint NOT NULL,
						Comments text NOT NULL,
						INDEX(EhrLabNum),
						INDEX(EhrLabResultNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE ehrlabnote'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlabnote (
						EhrLabNoteNum number(20) NOT NULL,
						EhrLabNum number(20) NOT NULL,
						EhrLabResultNum number(20) NOT NULL,
						Comments clob,
						CONSTRAINT ehrlabnote_EhrLabNoteNum PRIMARY KEY (EhrLabNoteNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlabnote_EhrLabNum ON ehrlabnote (EhrLabNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlabnote_EhrLabResultNum ON ehrlabnote (EhrLabResultNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS ehrlabresult";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlabresult (
						EhrLabResultNum bigint NOT NULL auto_increment PRIMARY KEY,
						EhrLabNum bigint NOT NULL,
						SetIdOBX bigint NOT NULL,
						ValueType varchar(255) NOT NULL,
						ObservationIdentifierID varchar(255) NOT NULL,
						ObservationIdentifierText varchar(255) NOT NULL,
						ObservationIdentifierCodeSystemName varchar(255) NOT NULL,
						ObservationIdentifierIDAlt varchar(255) NOT NULL,
						ObservationIdentifierTextAlt varchar(255) NOT NULL,
						ObservationIdentifierCodeSystemNameAlt varchar(255) NOT NULL,
						ObservationIdentifierTextOriginal varchar(255) NOT NULL,
						ObservationIdentifierSub varchar(255) NOT NULL,
						ObservationValueCodedElementID varchar(255) NOT NULL,
						ObservationValueCodedElementText varchar(255) NOT NULL,
						ObservationValueCodedElementCodeSystemName varchar(255) NOT NULL,
						ObservationValueCodedElementIDAlt varchar(255) NOT NULL,
						ObservationValueCodedElementTextAlt varchar(255) NOT NULL,
						ObservationValueCodedElementCodeSystemNameAlt varchar(255) NOT NULL,
						ObservationValueCodedElementTextOriginal varchar(255) NOT NULL,
						ObservationValueDateTime varchar(255) NOT NULL,
						ObservationValueTime time NOT NULL DEFAULT '00:00:00',
						ObservationValueComparator varchar(255) NOT NULL,
						ObservationValueNumber1 double NOT NULL,
						ObservationValueSeparatorOrSuffix varchar(255) NOT NULL,
						ObservationValueNumber2 double NOT NULL,
						ObservationValueNumeric double NOT NULL,
						ObservationValueText varchar(255) NOT NULL,
						UnitsID varchar(255) NOT NULL,
						UnitsText varchar(255) NOT NULL,
						UnitsCodeSystemName varchar(255) NOT NULL,
						UnitsIDAlt varchar(255) NOT NULL,
						UnitsTextAlt varchar(255) NOT NULL,
						UnitsCodeSystemNameAlt varchar(255) NOT NULL,
						UnitsTextOriginal varchar(255) NOT NULL,
						referenceRange varchar(255) NOT NULL,
						AbnormalFlags varchar(255) NOT NULL,
						ObservationResultStatus varchar(255) NOT NULL,
						ObservationDateTime varchar(255) NOT NULL,
						AnalysisDateTime varchar(255) NOT NULL,
						PerformingOrganizationName varchar(255) NOT NULL,
						PerformingOrganizationNameAssigningAuthorityNamespaceId varchar(255) NOT NULL,
						PerformingOrganizationNameAssigningAuthorityUniversalId varchar(255) NOT NULL,
						PerformingOrganizationNameAssigningAuthorityUniversalIdType varchar(255) NOT NULL,
						PerformingOrganizationIdentifierTypeCode varchar(255) NOT NULL,
						PerformingOrganizationIdentifier varchar(255) NOT NULL,
						PerformingOrganizationAddressStreet varchar(255) NOT NULL,
						PerformingOrganizationAddressOtherDesignation varchar(255) NOT NULL,
						PerformingOrganizationAddressCity varchar(255) NOT NULL,
						PerformingOrganizationAddressStateOrProvince varchar(255) NOT NULL,
						PerformingOrganizationAddressZipOrPostalCode varchar(255) NOT NULL,
						PerformingOrganizationAddressCountryCode varchar(255) NOT NULL,
						PerformingOrganizationAddressAddressType varchar(255) NOT NULL,
						PerformingOrganizationAddressCountyOrParishCode varchar(255) NOT NULL,
						MedicalDirectorID varchar(255) NOT NULL,
						MedicalDirectorLName varchar(255) NOT NULL,
						MedicalDirectorFName varchar(255) NOT NULL,
						MedicalDirectorMiddleNames varchar(255) NOT NULL,
						MedicalDirectorSuffix varchar(255) NOT NULL,
						MedicalDirectorPrefix varchar(255) NOT NULL,
						MedicalDirectorAssigningAuthorityNamespaceID varchar(255) NOT NULL,
						MedicalDirectorAssigningAuthorityUniversalID varchar(255) NOT NULL,
						MedicalDirectorAssigningAuthorityIDType varchar(255) NOT NULL,
						MedicalDirectorNameTypeCode varchar(255) NOT NULL,
						MedicalDirectorIdentifierTypeCode varchar(255) NOT NULL,
						INDEX(EhrLabNum),
						INDEX(SetIdOBX)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					//EHR lab features have been hidden for Oracle users due to some column names exceeding the 30 character limit.
					//TODO: Fix the column names to be less than 30 characters in the future.
					//	This will require unhiding EHR lab features for Oracle users, altering the table types and columns in the both MySQL and Oracle.
					/*
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE ehrlabresult'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlabresult (
						EhrLabResultNum number(20) NOT NULL,
						EhrLabNum number(20) NOT NULL,
						SetIdOBX number(20) NOT NULL,
						ValueType varchar2(255),
						ObservationIdentifierID varchar2(255),
						ObservationIdentifierText varchar2(255),
						ObservationIdentifierCodeSystemName varchar2(255),
						ObservationIdentifierIDAlt varchar2(255),
						ObservationIdentifierTextAlt varchar2(255),
						ObservationIdentifierCodeSystemNameAlt varchar2(255),
						ObservationIdentifierTextOriginal varchar2(255),
						ObservationIdentifierSub varchar2(255),
						ObservationValueCodedElementID varchar2(255),
						ObservationValueCodedElementText varchar2(255),
						ObservationValueCodedElementCodeSystemName varchar2(255),
						ObservationValueCodedElementIDAlt varchar2(255),
						ObservationValueCodedElementTextAlt varchar2(255),
						ObservationValueCodedElementCodeSystemNameAlt varchar2(255),
						ObservationValueCodedElementTextOriginal varchar2(255),
						ObservationValueDateTime varchar2(255),
						ObservationValueTime date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						ObservationValueComparator varchar2(255),
						ObservationValueNumber1 number(38,8) NOT NULL,
						ObservationValueSeparatorOrSuffix varchar2(255),
						ObservationValueNumber2 number(38,8) NOT NULL,
						ObservationValueNumeric number(38,8) NOT NULL,
						ObservationValueText varchar2(255),
						UnitsID varchar2(255),
						UnitsText varchar2(255),
						UnitsCodeSystemName varchar2(255),
						UnitsIDAlt varchar2(255),
						UnitsTextAlt varchar2(255),
						UnitsCodeSystemNameAlt varchar2(255),
						UnitsTextOriginal varchar2(255),
						referenceRange varchar2(255),
						AbnormalFlags varchar2(255),
						ObservationResultStatus varchar2(255),
						ObservationDateTime varchar2(255),
						AnalysisDateTime varchar2(255),
						PerformingOrganizationName varchar2(255),
						PerformingOrganizationNameAssigningAuthorityNamespaceId varchar2(255),
						PerformingOrganizationNameAssigningAuthorityUniversalId varchar2(255),
						PerformingOrganizationNameAssigningAuthorityUniversalIdType varchar2(255),
						PerformingOrganizationIdentifierTypeCode varchar2(255),
						PerformingOrganizationIdentifier varchar2(255),
						PerformingOrganizationAddressStreet varchar2(255),
						PerformingOrganizationAddressOtherDesignation varchar2(255),
						PerformingOrganizationAddressCity varchar2(255),
						PerformingOrganizationAddressStateOrProvince varchar2(255),
						PerformingOrganizationAddressZipOrPostalCode varchar2(255),
						PerformingOrganizationAddressCountryCode varchar2(255),
						PerformingOrganizationAddressAddressType varchar2(255),
						PerformingOrganizationAddressCountyOrParishCode varchar2(255),
						MedicalDirectorID varchar2(255),
						MedicalDirectorLName varchar2(255),
						MedicalDirectorFName varchar2(255),
						MedicalDirectorMiddleNames varchar2(255),
						MedicalDirectorSuffix varchar2(255),
						MedicalDirectorPrefix varchar2(255),
						MedicalDirectorAssigningAuthorityNamespaceID varchar2(255),
						MedicalDirectorAssigningAuthorityUniversalID varchar2(255),
						MedicalDirectorAssigningAuthorityIDType varchar2(255),
						MedicalDirectorNameTypeCode varchar2(255),
						MedicalDirectorIdentifierTypeCode varchar2(255),
						CONSTRAINT ehrlabresult_EhrLabResultNum PRIMARY KEY (EhrLabResultNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlabresult_EhrLabNum ON ehrlabresult (EhrLabNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlabresult_SetIdOBX ON ehrlabresult (SetIdOBX)";
					Db.NonQ(command);
					 */
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS ehrlabresultscopyto";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlabresultscopyto (
						EhrLabResultsCopyToNum bigint NOT NULL auto_increment PRIMARY KEY,
						EhrLabNum bigint NOT NULL,
						CopyToID varchar(255) NOT NULL,
						CopyToLName varchar(255) NOT NULL,
						CopyToFName varchar(255) NOT NULL,
						CopyToMiddleNames varchar(255) NOT NULL,
						CopyToSuffix varchar(255) NOT NULL,
						CopyToPrefix varchar(255) NOT NULL,
						CopyToAssigningAuthorityNamespaceID varchar(255) NOT NULL,
						CopyToAssigningAuthorityUniversalID varchar(255) NOT NULL,
						CopyToAssigningAuthorityIDType varchar(255) NOT NULL,
						CopyToNameTypeCode varchar(255) NOT NULL,
						CopyToIdentifierTypeCode varchar(255) NOT NULL,
						INDEX(EhrLabNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					//EHR lab features have been hidden for Oracle users due to some column names exceeding the 30 character limit.
					//TODO: Fix the column names to be less than 30 characters in the future.
					//	This will require unhiding EHR lab features for Oracle users, altering the table types and columns in the both MySQL and Oracle.
					/*
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE ehrlabresultscopyto'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlabresultscopyto (
						EhrLabResultsCopyToNum number(20) NOT NULL,
						EhrLabNum number(20) NOT NULL,
						CopyToID varchar2(255),
						CopyToLName varchar2(255),
						CopyToFName varchar2(255),
						CopyToMiddleNames varchar2(255),
						CopyToSuffix varchar2(255),
						CopyToPrefix varchar2(255),
						CopyToAssigningAuthorityNamespaceID varchar2(255),
						CopyToAssigningAuthorityUniversalID varchar2(255),
						CopyToAssigningAuthorityIDType varchar2(255),
						CopyToNameTypeCode varchar2(255),
						CopyToIdentifierTypeCode varchar2(255),
						CONSTRAINT ehrlabresultscopyto_EhrLabResu PRIMARY KEY (EhrLabResultsCopyToNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlabresultscopyto_EhrLabNum ON ehrlabresultscopyto (EhrLabNum)";
					Db.NonQ(command);
					 */
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS ehrlabspecimen";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlabspecimen (
						EhrLabSpecimenNum bigint NOT NULL auto_increment PRIMARY KEY,
						EhrLabNum bigint NOT NULL,
						SetIdSPM bigint NOT NULL,
						SpecimenTypeID varchar(255) NOT NULL,
						SpecimenTypeText varchar(255) NOT NULL,
						SpecimenTypeCodeSystemName varchar(255) NOT NULL,
						SpecimenTypeIDAlt varchar(255) NOT NULL,
						SpecimenTypeTextAlt varchar(255) NOT NULL,
						SpecimenTypeCodeSystemNameAlt varchar(255) NOT NULL,
						SpecimenTypeTextOriginal varchar(255) NOT NULL,
						CollectionDateTimeStart varchar(255) NOT NULL,
						CollectionDateTimeEnd varchar(255) NOT NULL,
						INDEX(EhrLabNum),
						INDEX(SetIdSPM)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE ehrlabspecimen'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlabspecimen (
						EhrLabSpecimenNum number(20) NOT NULL,
						EhrLabNum number(20) NOT NULL,
						SetIdSPM number(20) NOT NULL,
						SpecimenTypeID varchar2(255),
						SpecimenTypeText varchar2(255),
						SpecimenTypeCodeSystemName varchar2(255),
						SpecimenTypeIDAlt varchar2(255),
						SpecimenTypeTextAlt varchar2(255),
						SpecimenTypeCodeSystemNameAlt varchar2(255),
						SpecimenTypeTextOriginal varchar2(255),
						CollectionDateTimeStart varchar2(255),
						CollectionDateTimeEnd varchar2(255),
						CONSTRAINT ehrlabspecimen_EhrLabSpecimenN PRIMARY KEY (EhrLabSpecimenNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlabspecimen_EhrLabNum ON ehrlabspecimen (EhrLabNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlabspecimen_SetIdSPM ON ehrlabspecimen (SetIdSPM)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS ehrlabspecimencondition";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlabspecimencondition (
						EhrLabSpecimenConditionNum bigint NOT NULL auto_increment PRIMARY KEY,
						EhrLabSpecimenNum bigint NOT NULL,
						SpecimenConditionID varchar(255) NOT NULL,
						SpecimenConditionText varchar(255) NOT NULL,
						SpecimenConditionCodeSystemName varchar(255) NOT NULL,
						SpecimenConditionIDAlt varchar(255) NOT NULL,
						SpecimenConditionTextAlt varchar(255) NOT NULL,
						SpecimenConditionCodeSystemNameAlt varchar(255) NOT NULL,
						SpecimenConditionTextOriginal varchar(255) NOT NULL,
						INDEX(EhrLabSpecimenNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					//EHR lab features have been hidden for Oracle users due to some column names exceeding the 30 character limit.
					//TODO: Fix the column names to be less than 30 characters in the future.
					//	This will require unhiding EHR lab features for Oracle users, altering the table types and columns in the both MySQL and Oracle.
					/*
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE ehrlabspecimencondition'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlabspecimencondition (
						EhrLabSpecimenConditionNum number(20) NOT NULL,
						EhrLabSpecimenNum number(20) NOT NULL,
						SpecimenConditionID varchar2(255),
						SpecimenConditionText varchar2(255),
						SpecimenConditionCodeSystemName varchar2(255),
						SpecimenConditionIDAlt varchar2(255),
						SpecimenConditionTextAlt varchar2(255),
						SpecimenConditionCodeSystemNameAlt varchar2(255),
						SpecimenConditionTextOriginal varchar2(255),
						CONSTRAINT ehrlabspecimencondition_EhrLab PRIMARY KEY (EhrLabSpecimenConditionNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlabspecimencondition_EhrLab ON ehrlabspecimencondition (EhrLabSpecimenNum)";
					Db.NonQ(command);
					 */
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS ehrlabspecimenrejectreason";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlabspecimenrejectreason (
						EhrLabSpecimenRejectReasonNum bigint NOT NULL auto_increment PRIMARY KEY,
						EhrLabSpecimenNum bigint NOT NULL,
						SpecimenRejectReasonID varchar(255) NOT NULL,
						SpecimenRejectReasonText varchar(255) NOT NULL,
						SpecimenRejectReasonCodeSystemName varchar(255) NOT NULL,
						SpecimenRejectReasonIDAlt varchar(255) NOT NULL,
						SpecimenRejectReasonTextAlt varchar(255) NOT NULL,
						SpecimenRejectReasonCodeSystemNameAlt varchar(255) NOT NULL,
						SpecimenRejectReasonTextOriginal varchar(255) NOT NULL,
						INDEX(EhrLabSpecimenNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					//EHR lab features have been hidden for Oracle users due to some column names exceeding the 30 character limit.
					//TODO: Fix the column names to be less than 30 characters in the future.
					//	This will require unhiding EHR lab features for Oracle users, altering the table types and columns in the both MySQL and Oracle.
					/*
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE ehrlabspecimenrejectreason'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlabspecimenrejectreason (
						EhrLabSpecimenRejectReasonNum number(20) NOT NULL,
						EhrLabSpecimenNum number(20) NOT NULL,
						SpecimenRejectReasonID varchar2(255),
						SpecimenRejectReasonText varchar2(255),
						SpecimenRejectReasonCodeSystemName varchar2(255),
						SpecimenRejectReasonIDAlt varchar2(255),
						SpecimenRejectReasonTextAlt varchar2(255),
						SpecimenRejectReasonCodeSystemNameAlt varchar2(255),
						SpecimenRejectReasonTextOriginal varchar2(255),
						CONSTRAINT ehrlabspecimenrejectreason_Ehr PRIMARY KEY (EhrLabSpecimenRejectReasonNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlabspecimenrejectreason_Ehr ON ehrlabspecimenrejectreason (EhrLabSpecimenNum)";
					Db.NonQ(command);
					 */
				}
				#endregion
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE guardian ADD IsGuardian tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE guardian ADD IsGuardian number(3)";
					Db.NonQ(command);
					command="UPDATE guardian SET IsGuardian = 0 WHERE IsGuardian IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE guardian MODIFY IsGuardian NOT NULL";
					Db.NonQ(command);
				}
				command="UPDATE guardian SET IsGuardian=1";//Works for both MySQL and Oracle.
				Db.NonQ(command);
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE allergydef ADD UniiCode varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE allergydef ADD UniiCode varchar2(255)";
					Db.NonQ(command);
				}
				//Oracle compatible.
				command="ALTER TABLE allergydef DROP COLUMN SnomedAllergyTo";
				Db.NonQ(command);
				//OID External
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS oidexternal";
					Db.NonQ(command);
					command=@"CREATE TABLE oidexternal (
						OIDExternalNum bigint NOT NULL auto_increment PRIMARY KEY,
						IDType varchar(255) NOT NULL,
						IDInternal bigint NOT NULL,
						IDExternal varchar(255) NOT NULL,
						rootExternal varchar(255) NOT NULL,
						INDEX(IDType,IDInternal),
						INDEX(rootExternal(62),IDExternal(62))
						) DEFAULT CHARSET=utf8";//Index is 1000/8=125/n where n is the number of columns to be indexed together. In this case the result is 62.5=62
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE oidexternal'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE oidexternal (
						OIDExternalNum number(20) NOT NULL,
						IDType varchar2(255),
						IDInternal number(20),
						IDExternal varchar2(255),
						rootExternal varchar2(255),
						CONSTRAINT oidexternal_OIDExternalNum PRIMARY KEY (OIDExternalNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX oidexternal_type_ID ON oidexternal (IDType, IDInternal)";
					Db.NonQ(command);
					command=@"CREATE INDEX oidexternal_root_extension ON oidexternal (rootExternal, IDExternal)";
					Db.NonQ(command);
				}
				//OID Internal
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS oidinternal";
					Db.NonQ(command);
					command=@"CREATE TABLE oidinternal (
						OIDInternalNum bigint NOT NULL auto_increment PRIMARY KEY,
						IDType varchar(255) NOT NULL,
						IDRoot varchar(255) NOT NULL
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE oidinternal'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE oidinternal (
						OIDInternalNum number(20) NOT NULL,
						IDType varchar2(255),
						IDRoot varchar2(255),
						CONSTRAINT oidinternal_OIDInternalNum PRIMARY KEY (OIDInternalNum)
						)";
					Db.NonQ(command);
				}				
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE vaccinepat ADD FilledCity varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE vaccinepat ADD FilledCity varchar2(255)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE vaccinepat ADD FilledST varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE vaccinepat ADD FilledST varchar2(255)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE vaccinepat ADD CompletionStatus tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE vaccinepat ADD CompletionStatus number(3)";
					Db.NonQ(command);
					command="UPDATE vaccinepat SET CompletionStatus = 0 WHERE CompletionStatus IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE vaccinepat MODIFY CompletionStatus NOT NULL";
					Db.NonQ(command);
				}
				//MySQL and Oracle
				command="UPDATE vaccinepat SET CompletionStatus=CASE WHEN NotGiven=1 THEN 2 ELSE 0 END";//If was NotGiven then CompletionStatus=NotAdministered, otherwise CompletionStatus=Complete.
				Db.NonQ(command);
				//MySQL and Oracle
				command="ALTER TABLE vaccinepat DROP COLUMN NotGiven";
				Db.NonQ(command);
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE vaccinepat ADD AdministrationNoteCode tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE vaccinepat ADD AdministrationNoteCode number(3)";
					Db.NonQ(command);
					command="UPDATE vaccinepat SET AdministrationNoteCode = 0 WHERE AdministrationNoteCode IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE vaccinepat MODIFY AdministrationNoteCode NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE vaccinepat ADD UserNum bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE vaccinepat ADD INDEX (UserNum)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE vaccinepat ADD UserNum number(20)";
					Db.NonQ(command);
					command="UPDATE vaccinepat SET UserNum = 0 WHERE UserNum IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE vaccinepat MODIFY UserNum NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX vaccinepat_UserNum ON vaccinepat (UserNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE vaccinepat ADD ProvNumOrdering bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE vaccinepat ADD INDEX (ProvNumOrdering)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE vaccinepat ADD ProvNumOrdering number(20)";
					Db.NonQ(command);
					command="UPDATE vaccinepat SET ProvNumOrdering = 0 WHERE ProvNumOrdering IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE vaccinepat MODIFY ProvNumOrdering NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX vaccinepat_ProvNumOrdering ON vaccinepat (ProvNumOrdering)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE vaccinepat ADD ProvNumAdminister bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE vaccinepat ADD INDEX (ProvNumAdminister)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE vaccinepat ADD ProvNumAdminister number(20)";
					Db.NonQ(command);
					command="UPDATE vaccinepat SET ProvNumAdminister = 0 WHERE ProvNumAdminister IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE vaccinepat MODIFY ProvNumAdminister NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX vaccinepat_ProvNumAdminister ON vaccinepat (ProvNumAdminister)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE vaccinepat ADD DateExpire date NOT NULL DEFAULT '0001-01-01'";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE vaccinepat ADD DateExpire date";
					Db.NonQ(command);
					command="UPDATE vaccinepat SET DateExpire = TO_DATE('0001-01-01','YYYY-MM-DD') WHERE DateExpire IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE vaccinepat MODIFY DateExpire NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE vaccinepat ADD RefusalReason tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE vaccinepat ADD RefusalReason number(3)";
					Db.NonQ(command);
					command="UPDATE vaccinepat SET RefusalReason = 0 WHERE RefusalReason IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE vaccinepat MODIFY RefusalReason NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE vaccinepat ADD ActionCode tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE vaccinepat ADD ActionCode number(3)";
					Db.NonQ(command);
					command="UPDATE vaccinepat SET ActionCode = 0 WHERE ActionCode IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE vaccinepat MODIFY ActionCode NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE vaccinepat ADD AdministrationRoute tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE vaccinepat ADD AdministrationRoute number(3)";
					Db.NonQ(command);
					command="UPDATE vaccinepat SET AdministrationRoute = 0 WHERE AdministrationRoute IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE vaccinepat MODIFY AdministrationRoute NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE vaccinepat ADD AdministrationSite tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE vaccinepat ADD AdministrationSite number(3)";
					Db.NonQ(command);
					command="UPDATE vaccinepat SET AdministrationSite = 0 WHERE AdministrationSite IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE vaccinepat MODIFY AdministrationSite NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS vaccineobs";
					Db.NonQ(command);
					command=@"CREATE TABLE vaccineobs (
						VaccineObsNum bigint NOT NULL auto_increment PRIMARY KEY,
						VaccinePatNum bigint NOT NULL,
						ValType tinyint NOT NULL,
						IdentifyingCode tinyint NOT NULL,
						ValReported varchar(255) NOT NULL,
						ValCodeSystem tinyint NOT NULL,
						VaccineObsNumGroup bigint NOT NULL,
						UcumCode varchar(255) NOT NULL,
						DateObs date NOT NULL DEFAULT '0001-01-01',
						MethodCode varchar(255) NOT NULL,
						INDEX(VaccinePatNum),
						INDEX(VaccineObsNumGroup)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE vaccineobs'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE vaccineobs (
						VaccineObsNum number(20) NOT NULL,
						VaccinePatNum number(20) NOT NULL,
						ValType number(3) NOT NULL,
						IdentifyingCode number(3) NOT NULL,
						ValReported varchar2(255),
						ValCodeSystem number(3) NOT NULL,
						VaccineObsNumGroup number(20) NOT NULL,
						UcumCode varchar2(255),
						DateObs date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						MethodCode varchar2(255),
						CONSTRAINT vaccineobs_VaccineObsNum PRIMARY KEY (VaccineObsNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX vaccineobs_VaccinePatNum ON vaccineobs (VaccinePatNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX vaccineobs_VaccineObsNumGroup ON vaccineobs (VaccineObsNumGroup)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE ehrmeasureevent ADD FKey bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE ehrmeasureevent ADD INDEX (FKey)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE ehrmeasureevent ADD FKey number(20)";
					Db.NonQ(command);
					command="UPDATE ehrmeasureevent SET FKey = 0 WHERE FKey IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE ehrmeasureevent MODIFY FKey NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrmeasureevent_FKey ON ehrmeasureevent (FKey)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE vitalsign ADD BMIPercentile int NOT NULL";
					Db.NonQ(command);
					command="UPDATE vitalsign SET BMIPercentile=-1";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE vitalsign ADD BMIPercentile number(11)";
					Db.NonQ(command);
					command="UPDATE vitalsign SET BMIPercentile = -1 WHERE BMIPercentile IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE vitalsign MODIFY BMIPercentile NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE procedurelog ADD SnomedBodySite varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE procedurelog ADD SnomedBodySite varchar2(255)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS ehrpatient";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrpatient (
						PatNum bigint NOT NULL PRIMARY KEY,
						MotherMaidenFname varchar(255) NOT NULL,
						MotherMaidenLname varchar(255) NOT NULL,
						VacShareOk tinyint NOT NULL
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE ehrpatient'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrpatient (
						PatNum number(20) NOT NULL,
						MotherMaidenFname varchar2(255),
						MotherMaidenLname varchar2(255),
						VacShareOk number(3) NOT NULL,
						CONSTRAINT ehrpatient_PatNum PRIMARY KEY (PatNum)
						)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS ehraptobs";
					Db.NonQ(command);
					command=@"CREATE TABLE ehraptobs (
						EhrAptObsNum bigint NOT NULL auto_increment PRIMARY KEY,
						AptNum bigint NOT NULL,
						IdentifyingCode tinyint NOT NULL,
						ValType tinyint NOT NULL,
						ValReported varchar(255) NOT NULL,
						UcumCode varchar(255) NOT NULL,
						ValCodeSystem varchar(255) NOT NULL,
						INDEX(AptNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE ehraptobs'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE ehraptobs (
						EhrAptObsNum number(20) NOT NULL,
						AptNum number(20) NOT NULL,
						IdentifyingCode number(3) NOT NULL,
						ValType number(3) NOT NULL,
						ValReported varchar2(255),
						UcumCode varchar2(255),
						ValCodeSystem varchar2(255),
						CONSTRAINT ehraptobs_EhrAptObsNum PRIMARY KEY (EhrAptObsNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehraptobs_AptNum ON ehraptobs (AptNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE patient ADD DateTimeDeceased datetime DEFAULT '0001-01-01' NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE patient ADD DateTimeDeceased date";
					Db.NonQ(command);
					command="UPDATE patient SET DateTimeDeceased = TO_DATE('0001-01-01','YYYY-MM-DD') WHERE DateTimeDeceased IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE patient MODIFY DateTimeDeceased NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE ehrlab ADD IsCpoe tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					//EHR lab features have been hidden for Oracle users due to some column names exceeding the 30 character limit.
					//TODO: Fix the column names to be less than 30 characters in the future.
					//	This will require unhiding EHR lab features for Oracle users, altering the table types and columns in the both MySQL and Oracle.
					/*
					command="ALTER TABLE ehrlab ADD IsCpoe number(3)";
					Db.NonQ(command);
					command="UPDATE ehrlab SET IsCpoe = 0 WHERE IsCpoe IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE ehrlab MODIFY IsCpoe NOT NULL";
					Db.NonQ(command);
					 */
				}
				//Add additional EHR Measures to DB
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO ehrmeasure(MeasureType,Numerator,Denominator) VALUES(20,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(MeasureType,Numerator,Denominator) VALUES(21,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(MeasureType,Numerator,Denominator) VALUES(22,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(MeasureType,Numerator,Denominator) VALUES(23,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(MeasureType,Numerator,Denominator) VALUES(24,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(MeasureType,Numerator,Denominator) VALUES(25,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(MeasureType,Numerator,Denominator) VALUES(26,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(MeasureType,Numerator,Denominator) VALUES(27,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(MeasureType,Numerator,Denominator) VALUES(28,-1,-1)";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO ehrmeasure(EhrMeasureNum,MeasureType,Numerator,Denominator) VALUES((SELECT MAX(EhrMeasureNum)+1 FROM ehrmeasure),20,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(EhrMeasureNum,MeasureType,Numerator,Denominator) VALUES((SELECT MAX(EhrMeasureNum)+1 FROM ehrmeasure),21,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(EhrMeasureNum,MeasureType,Numerator,Denominator) VALUES((SELECT MAX(EhrMeasureNum)+1 FROM ehrmeasure),22,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(EhrMeasureNum,MeasureType,Numerator,Denominator) VALUES((SELECT MAX(EhrMeasureNum)+1 FROM ehrmeasure),23,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(EhrMeasureNum,MeasureType,Numerator,Denominator) VALUES((SELECT MAX(EhrMeasureNum)+1 FROM ehrmeasure),24,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(EhrMeasureNum,MeasureType,Numerator,Denominator) VALUES((SELECT MAX(EhrMeasureNum)+1 FROM ehrmeasure),25,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(EhrMeasureNum,MeasureType,Numerator,Denominator) VALUES((SELECT MAX(EhrMeasureNum)+1 FROM ehrmeasure),26,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(EhrMeasureNum,MeasureType,Numerator,Denominator) VALUES((SELECT MAX(EhrMeasureNum)+1 FROM ehrmeasure),27,-1,-1)";
					Db.NonQ(command);
					command="INSERT INTO ehrmeasure(EhrMeasureNum,MeasureType,Numerator,Denominator) VALUES((SELECT MAX(EhrMeasureNum)+1 FROM ehrmeasure),28,-1,-1)";
					Db.NonQ(command);
				}
				//Split patientrace DeclinedToSpecify into DeclinedToSpecifyRace and DeclinedToSpecifyEthnicity.
				command="SELECT PatNum FROM patientrace WHERE Race=4";//DeclinedToSpecifyRace
				DataTable table=Db.GetTable(command);
				for(int i=0;i<table.Rows.Count;i++) {
					string patNum=table.Rows[i]["PatNum"].ToString();
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="INSERT INTO patientrace (PatNum,Race) VALUES ("+patNum+",11)";//DeclinedToSpecifyEthnicity
					}
					else {//oracle
						command="INSERT INTO patientrace (PatientRaceNum,PatNum,Race,CdcrecCode) VALUES ((SELECT MAX(PatientRaceNum+1) FROM patientrace),"+patNum+",11,'')";
					}
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS ehrlabimage";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlabimage (
						EhrLabImageNum bigint NOT NULL auto_increment PRIMARY KEY,
						EhrLabNum bigint NOT NULL,
						DocNum bigint NOT NULL,
						INDEX(EhrLabNum),
						INDEX(DocNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE ehrlabimage'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE ehrlabimage (
						EhrLabImageNum number(20) NOT NULL,
						EhrLabNum number(20) NOT NULL,
						DocNum number(20) NOT NULL,
						CONSTRAINT ehrlabimage_EhrLabImageNum PRIMARY KEY (EhrLabImageNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlabimage_EhrLabNum ON ehrlabimage (EhrLabNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX ehrlabimage_DocNum ON ehrlabimage (DocNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE apptfielddef CHANGE PickList PickList TEXT NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE apptfielddef MODIFY (PickList varchar2(4000) NOT NULL)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE refattach ADD ProvNum bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE refattach ADD INDEX (ProvNum)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE refattach ADD ProvNum number(20)";
					Db.NonQ(command);
					command="UPDATE refattach SET ProvNum = 0 WHERE ProvNum IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE refattach MODIFY ProvNum NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX refattach_ProvNum ON refattach (ProvNum)";
					Db.NonQ(command);
				} 
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE ehrlab ADD OriginalPIDSegment text NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					//EHR lab features have been hidden for Oracle users due to some column names exceeding the 30 character limit.
					//TODO: Fix the column names to be less than 30 characters in the future.
					//	This will require unhiding EHR lab features for Oracle users, altering the table types and columns in the both MySQL and Oracle.
					/*
					command="ALTER TABLE ehrlab ADD OriginalPIDSegment varchar2(4000)";
					Db.NonQ(command);
					 */
				}
				//Added TimeCardADPExportIncludesName preference
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('TimeCardADPExportIncludesName','0')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'TimeCardADPExportIncludesName','0')";
					Db.NonQ(command);
				}
				command="UPDATE preference SET ValueString = '14.1.1.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To14_1_3();
		}

		private static void To14_1_3() {
			if(FromVersion<new Version("14.1.3.0")) {
				string command;
				//add programproperty to eClinicalWorks program link for changing the FT1 segments of the DFT messages
				//to place quadrants in the ToothNum component instead of the surface component
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="SELECT ProgramNum FROM program WHERE ProgName='eClinicalWorks'";
					int programNum=PIn.Int(Db.GetScalar(command));
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
							+") VALUES("
							+"'"+POut.Long(programNum)+"', "
							+"'IsQuadAsToothNum', "
							+"'0')";//set to 0 (false) by default so behavior of existing customers will not change
					Db.NonQ(command);
				}
				else {//oracle
					//eCW will never use Oracle.
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE hl7def ADD IsQuadAsToothNum tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE hl7def ADD IsQuadAsToothNum number(3)";
					Db.NonQ(command);
					command="UPDATE hl7def SET IsQuadAsToothNum = 0 WHERE IsQuadAsToothNum IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE hl7def MODIFY IsQuadAsToothNum NOT NULL";
					Db.NonQ(command);
				}
				command="UPDATE preference SET ValueString = '14.1.3.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To14_1_8();
		}

		private static void To14_1_8() {
			if(FromVersion<new Version("14.1.8.0")) {
				string command;
				//add programproperty to eClinicalWorks program link for changing the cookie creation for the LBSESSIONID
				//This is a fix for their version 10 so that the medical panel will work correctly
				command="SELECT ProgramNum FROM program WHERE ProgName='eClinicalWorks'";
				int programNum=PIn.Int(Db.GetScalar(command));
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
							+") VALUES("
							+"'"+POut.Long(programNum)+"', "
							+"'IsLBSessionIdExcluded', "
							+"'0')";//set to 0 (false) by default so behavior of existing customers will not change
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
							+") VALUES("
							+"(SELECT MAX(ProgramPropertyNum)+1 FROM programproperty),"
							+"'"+POut.Long(programNum)+"', "
							+"'IsLBSessionIdExcluded', "
							+"'0')";//set to 0 (false) by default so behavior of existing customers will not change
					Db.NonQ(command);
				}
				command="UPDATE preference SET ValueString = '14.1.8.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To14_2_1();
		}

		///<summary>Oracle compatible: 05/13/2014</summary>
		private static void To14_2_1() {
			if(FromVersion<new Version("14.2.1.0")) {
				string command;
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('CustListenerPort','25255')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'CustListenerPort','25255')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE claimpayment ADD PayType bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE claimpayment ADD INDEX (PayType)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE claimpayment ADD PayType number(20)";
					Db.NonQ(command);
					command="UPDATE claimpayment SET PayType = 0 WHERE PayType IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE claimpayment MODIFY PayType NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX claimpayment_PayType ON claimpayment (PayType)";
					Db.NonQ(command);
				}
				//Add program property for Tigerview enchancement
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"'"+POut.Long(Programs.GetProgramNum(ProgramName.TigerView))+"', "
				    +"'TigerView EMR folder path', "
				    +"'')";
					Db.NonQ(command);
				}
				else {
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
						+"(SELECT MAX(ProgramPropertyNum)+1 FROM programproperty),"
				    +"'"+POut.Long(Programs.GetProgramNum(ProgramName.TigerView))+"', "
				    +"'TigerView EMR folder path', "
				    +"'')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO definition (Category,ItemName,ItemOrder,ItemValue) "
									+"VALUES (32"+",'"+POut.String("Check")+"','0','')";
				}
				else {//oracle
					command="INSERT INTO definition (DefNum,Category,ItemName,ItemOrder,ItemValue) "
									+"VALUES ((SELECT MAX(DefNum)+1 FROM definition),32,'"+POut.String("Check")+"','0','')";
				}
				long defNum=Db.NonQ(command,true);
				//At this point in time, all claimpayments in the database are assumed to be of pay type "Check".
				command="UPDATE claimpayment SET PayType = "+POut.Long(defNum)+ " WHERE PayType = 0";
				Db.NonQ(command);
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO definition (Category,ItemName,ItemOrder,ItemValue) "
									+"VALUES (32"+",'"+POut.String("EFT")+"','1','N')";
				}
				else {//oracle
					command="INSERT INTO definition (DefNum,Category,ItemName,ItemOrder,ItemValue) "
									+"VALUES ((SELECT MAX(DefNum)+1 FROM definition),32,'"+POut.String("EFT")+"','1','N')";
				}
				Db.NonQ(command,true);
				//Insert VistaDent Bridge
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO program (ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"'VistaDent', "
				    +"'VistaDent from www.gactechnocenter.com', "
				    +"'0', "
				    +"'"+POut.String(@"C:\Program Files\GAC\VistaDent\VistaDent.exe")+"',"
				    +"'', "
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"'"+POut.Long(programNum)+"', "
				    +"'"+POut.Int(((int)ToolBarsAvail.ChartModule))+"', "
				    +"'VistaDent')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO program (ProgramNum,ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"(SELECT MAX(ProgramNum)+1 FROM program),"
				    +"'VistaDent', "
				    +"'VistaDent from www.gactechnocenter.com', "
				    +"'0', "
				    +"'"+POut.String(@"C:\Program Files\GAC\VistaDent\VistaDent.exe")+"',"
				    +"'', "
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ToolButItemNum,ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"(SELECT MAX(ToolButItemNum)+1 FROM toolbutitem),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'"+POut.Int(((int)ToolBarsAvail.ChartModule))+"', "
				    +"'VistaDent')";
					Db.NonQ(command);
				}//end VistaDent bridge
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE procedurelog ADD DiagnosticCode2 varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE procedurelog ADD DiagnosticCode2 varchar2(255)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE procedurelog ADD DiagnosticCode3 varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE procedurelog ADD DiagnosticCode3 varchar2(255)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE procedurelog ADD DiagnosticCode4 varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE procedurelog ADD DiagnosticCode4 varchar2(255)";
					Db.NonQ(command);
				}
				//Drop depricated Formulary table
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS formulary";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE formulary'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
				}
				//Drop depricated FormularyMed table
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS formularymed";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE formularymed'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
				}
				//Blue theme
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('ColorTheme','1')";//On by default
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'ColorTheme','1')";//On by default
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('ShowFeaturePatientClone','0')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'ShowFeaturePatientClone','0')";
					Db.NonQ(command);
				}
				//Add new SoftwareName preference for EHR report printing. Defaults to Open Dental Software.
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('SoftwareName','Open Dental Software')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'SoftwareName','Open Dental Software')";
					Db.NonQ(command);
				}
				//Add the 1500 claim form version 02/12 fields if the claim form does not already exist. The unique ID is OD12.
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="SELECT ClaimFormNum FROM claimform WHERE UniqueID='OD12' LIMIT 1";
				}
				else {//oracle doesn't have LIMIT
					command="SELECT * FROM (SELECT ClaimFormNum FROM claimform WHERE UniqueID='OD12') WHERE RowNum<=1";
				}
				DataTable tableClaimFormNum=Db.GetTable(command);
				if(tableClaimFormNum.Rows.Count==0) {
					long claimFormNum=0;
					//The 1500 claim form version 02/12 does not exist, so safe to add.
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="INSERT INTO claimform(Description,IsHidden,FontName,FontSize,UniqueID,PrintImages,OffsetX,OffsetY) "+
							"VALUES ('1500_02_12',0,'Arial',9,'OD12',0,0,0)";
						claimFormNum=Db.NonQ(command,true);
					}
					else {//oracle
						command="INSERT INTO claimform(ClaimFormNum,Description,IsHidden,FontName,FontSize,UniqueID,PrintImages,OffsetX,OffsetY) "+
							"VALUES ((SELECT MAX(ClaimFormNum)+1 FROM claimform),'1500_02_12',0,'Arial',9,'OD12',1,0,0)";
						claimFormNum=Db.NonQ(command,true);
					}
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'1500_02_12.gif','','','6','6','850','1170')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','AccidentST','','467','386','30','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','BillingDentist','','531','984','250','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','BillingDentist','','256','985','235','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','BillingDentistNPI','','260','1035','92','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','BillingDentistNPI','','531','1035','92','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','BillingDentistNumIsSSN','','189','951','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','BillingDentistNumIsTIN','','209','951','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','BillingDentistPh123','','680','968','40','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','BillingDentistPh456','','719','968','40','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','BillingDentistPh78910','','759','968','48','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','BillingDentistSSNorTIN','','39','949','131','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','DiagnosisA','','46','651','70','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','DiagnosisB','','176','651','70','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','DiagnosisC','','306','651','70','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','DiagnosisD','','436','651','70','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','DiagnosisE','','46','668','70','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','DiagnosisF','','176','668','70','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','DiagnosisG','','306','668','70','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','DiagnosisH','','436','668','70','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','DiagnosisI','','46','684','70','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','DiagnosisJ','','176','684','70','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','DiagnosisK','','306','684','70','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','DiagnosisL','','436','683','70','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','EmployerName','','540','386','250','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','GroupNum','','531','319','250','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','ICDindicator','','437','636','20','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','IsAutoAccident','','368','386','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','IsGroupHealthPlan','','326','154','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','IsMedicaidClaim','','96','154','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','IsNotAutoAccident','','428','385','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','IsNotOccupational','','428','352','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','IsNotOtherAccident','','428','418','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','IsOccupational','','368','352','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','IsOtherAccident','','368','418','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','OtherInsCarrierName','','36','450','245','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','OtherInsExists','','537','451','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','OtherInsGroupNum','','36','353','245','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','OtherInsNotExists','','588','451','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','OtherInsSubscrLastFirst','','36','320','245','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P1Code','','274','752','55','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P1CodeMod1','','340','752','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P1CodeMod2','','375','752','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P1CodeMod3','','405','752','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P1CodeMod4','','435','752','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P1Date','MM    dd    yy','32','752','80','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P1Date','MM    dd     yy','122','752','80','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P1DiagnosisPoint','','467','752','48','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P1Fee','','598','752','70','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P1PlaceNumericCode','','206','752','28','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P1TreatProvNPI','','698','752','100','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P1UnitQty','','615','752','20','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P2Code','','274','786','55','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P2CodeMod1','','340','786','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P2CodeMod2','','375','786','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P2CodeMod3','','405','786','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P2CodeMod4','','435','786','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P2Date','MM    dd    yy','32','786','80','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P2Date','MM    dd     yy','122','786','80','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P2DiagnosisPoint','','467','786','48','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P2Fee','','598','786','70','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P2PlaceNumericCode','','206','786','28','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P2TreatProvNPI','','698','786','100','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P2UnitQty','','615','786','20','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P3Code','','274','818','55','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P3CodeMod1','','340','818','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P3CodeMod2','','375','818','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P3CodeMod3','','405','818','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P3CodeMod4','','435','818','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P3Date','MM    dd    yy','32','818','80','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P3Date','MM    dd     yy','121','818','80','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P3DiagnosisPoint','','467','818','48','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P3Fee','','598','818','70','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P3PlaceNumericCode','','206','819','28','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P3TreatProvNPI','','698','818','100','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P3UnitQty','','615','818','20','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P4Code','','274','852','55','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P4CodeMod1','','340','852','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P4CodeMod2','','375','852','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P4CodeMod3','','405','852','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P4CodeMod4','','435','852','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P4Date','MM    dd    yy','32','852','80','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P4Date','MM    dd     yy','122','852','80','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P4DiagnosisPoint','','467','852','48','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P4Fee','','598','852','70','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P4PlaceNumericCode','','206','853','28','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P4TreatProvNPI','','698','852','100','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P4UnitQty','','615','852','20','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P5Code','','274','885','55','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P5CodeMod1','','340','885','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P5CodeMod2','','375','885','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P5CodeMod3','','405','885','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P5CodeMod4','','435','885','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P5Date','MM    dd    yy','32','885','80','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P5Date','MM    dd     yy','122','885','80','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P5DiagnosisPoint','','467','885','48','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P5Fee','','598','885','70','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P5PlaceNumericCode','','206','885','28','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P5TreatProvNPI','','698','885','100','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P5UnitQty','','615','885','20','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P6Code','','274','919','55','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P6CodeMod1','','340','919','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P6CodeMod2','','375','919','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P6CodeMod3','','405','919','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P6CodeMod4','','435','919','26','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P6Date','MM    dd    yy','32','919','80','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P6Date','MM    dd     yy','122','919','80','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P6DiagnosisPoint','','467','919','48','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P6Fee','','598','919','70','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P6PlaceNumericCode','','206','919','28','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P6TreatProvNPI','','698','919','100','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','P6UnitQty','','615','919','20','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PatientAddress','','37','216','245','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PatientAssignment','','577','512','210','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PatientCity','','37','251','200','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PatientDOB','MM    dd    yyyy','333','187','95','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PatientIsFemale','','487','186','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PatientIsMale','','437','186','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PatientLastFirst','','37','184','245','13')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PatientPhone','','169','286','120','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PatientRelease','','78','512','240','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PatientReleaseDate','MM/dd/yyyy','386','512','113','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PatientST','','281','253','30','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PatientZip','','37','287','100','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PayToDentistAddress','','531','1000','250','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PayToDentistCity','','531','1016','139','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PayToDentistST','','671','1016','30','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PayToDentistZip','','701','1016','80','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PriInsAddress','','419','86','250','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PriInsAddress2','','419','100','250','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PriInsCarrierName','','419','72','250','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PriInsCarrierName','','527','416','245','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PriInsCity','','419','114','140','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PriInsST','','560','114','30','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PriInsZip','','590','114','79','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','PriorAuthString','','528','685','282','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','RelatIsChild','','437','218','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','RelatIsOther','','487','218','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','RelatIsSelf','','347','218','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','RelatIsSpouse','','397','218','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','SubscrAddress','','530','219','250','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','SubscrCity','','530','253','200','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','SubscrDOB','MM    dd     yyyy','554','353','100','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','SubscrID','','530','153','100','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','SubscrIsFemale','','769','352','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','SubscrIsMale','','699','352','0','0')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','SubscrLastFirst','','530','186','250','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','SubscrPhone','','672','287','120','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','SubscrST','','760','253','50','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','SubscrZip','','532','286','100','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','TotalFee','','620','951','75','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','TreatingDentistAddress','','256','999','235','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','TreatingDentistCity','','256','1013','132','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','TreatingDentistSigDate','','169','1025','74','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','TreatingDentistSignature','','27','1010','142','30')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','TreatingDentistST','','388','1013','28','14')";
					Db.NonQ(command);
					command="INSERT INTO claimformitem (ClaimFormItemNum,ClaimFormNum,ImageFileName,FieldName,FormatString,XPos,YPos,Width,Height) "
						+"VALUES ("+GetClaimFormItemNum()+","+POut.Long(claimFormNum)+",'','TreatingDentistZip','','416','1013','75','14')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE ehrprovkey ADD YearValue int NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE ehrprovkey ADD YearValue number(11)";
					Db.NonQ(command);
					command="UPDATE ehrprovkey SET YearValue = 0 WHERE YearValue IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE ehrprovkey MODIFY YearValue NOT NULL";
					Db.NonQ(command);
				}
				command="ALTER TABLE provider DROP COLUMN EhrHasReportAccess";
				Db.NonQ(command);
				command="ALTER TABLE ehrprovkey DROP COLUMN HasReportAccess";
				Db.NonQ(command);
				command="UPDATE preference SET ValueString = '14.2.1.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To14_2_18();
		}

		private static void To14_2_18() {
			if(FromVersion<new Version("14.2.18.0")) {
				string command;
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="SELECT ClaimFormNum FROM claimform WHERE UniqueID='OD11' LIMIT 1";
				}
				else {//oracle doesn't have LIMIT
					command="SELECT * FROM (SELECT ClaimFormNum FROM claimform WHERE UniqueID='OD11') WHERE RowNum<=1";
				}
				long claimFormNum=PIn.Long(Db.GetScalar(command));
				command="UPDATE claimformitem SET claimformitem.FieldName = 'P1DiagnosisPoint' WHERE ClaimFormNum="+POut.Long(claimFormNum)+" AND claimformitem.FieldName = 'P1Diagnosis'";
				Db.NonQ(command);
				command="UPDATE claimformitem SET claimformitem.FieldName = 'P2DiagnosisPoint' WHERE ClaimFormNum="+POut.Long(claimFormNum)+" AND claimformitem.FieldName = 'P2Diagnosis'";
				Db.NonQ(command);
				command="UPDATE claimformitem SET claimformitem.FieldName = 'P3DiagnosisPoint' WHERE ClaimFormNum="+POut.Long(claimFormNum)+" AND claimformitem.FieldName = 'P3Diagnosis'";
				Db.NonQ(command);
				command="UPDATE claimformitem SET claimformitem.FieldName = 'P4DiagnosisPoint' WHERE ClaimFormNum="+POut.Long(claimFormNum)+" AND claimformitem.FieldName = 'P4Diagnosis'";
				Db.NonQ(command);
				command="UPDATE claimformitem SET claimformitem.FieldName = 'P5DiagnosisPoint' WHERE ClaimFormNum="+POut.Long(claimFormNum)+" AND claimformitem.FieldName = 'P5Diagnosis'";
				Db.NonQ(command);
				command="UPDATE claimformitem SET claimformitem.FieldName = 'P6DiagnosisPoint' WHERE ClaimFormNum="+POut.Long(claimFormNum)+" AND claimformitem.FieldName = 'P6Diagnosis'";
				Db.NonQ(command);
				command="UPDATE claimformitem SET claimformitem.FieldName = 'P7DiagnosisPoint' WHERE ClaimFormNum="+POut.Long(claimFormNum)+" AND claimformitem.FieldName = 'P7Diagnosis'";
				Db.NonQ(command);
				command="UPDATE claimformitem SET claimformitem.FieldName = 'P8DiagnosisPoint' WHERE ClaimFormNum="+POut.Long(claimFormNum)+" AND claimformitem.FieldName = 'P8Diagnosis'";
				Db.NonQ(command);
				command="UPDATE claimformitem SET claimformitem.FieldName = 'P9DiagnosisPoint' WHERE ClaimFormNum="+POut.Long(claimFormNum)+" AND claimformitem.FieldName = 'P9Diagnosis'";
				Db.NonQ(command);
				command="UPDATE claimformitem SET claimformitem.FieldName = 'P10DiagnosisPoint' WHERE ClaimFormNum="+POut.Long(claimFormNum)+" AND claimformitem.FieldName = 'P10Diagnosis'";
				Db.NonQ(command);
				command="UPDATE claimformitem SET claimformitem.FieldName = 'DiagnosisA' WHERE ClaimFormNum="+POut.Long(claimFormNum)+" AND claimformitem.FieldName = 'Diagnosis1'";
				Db.NonQ(command);
				command="UPDATE claimformitem SET claimformitem.FieldName = 'DiagnosisB' WHERE ClaimFormNum="+POut.Long(claimFormNum)+" AND claimformitem.FieldName = 'Diagnosis2'";
				Db.NonQ(command);
				command="UPDATE claimformitem SET claimformitem.FieldName = 'DiagnosisC' WHERE ClaimFormNum="+POut.Long(claimFormNum)+" AND claimformitem.FieldName = 'Diagnosis3'";
				Db.NonQ(command);
				command="UPDATE claimformitem SET claimformitem.FieldName = 'DiagnosisD' WHERE ClaimFormNum="+POut.Long(claimFormNum)+" AND claimformitem.FieldName = 'Diagnosis4'";
				Db.NonQ(command);
				command="UPDATE preference SET ValueString = '14.2.18.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To14_2_20();
		}

		///<summary>Oracle compatible: 07/07/2014</summary>
		private static void To14_2_20() {
			if(FromVersion<new Version("14.2.20.0")) {
				string command;
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="SELECT CanadianNetworkNum FROM canadiannetwork WHERE Abbrev='TELUS B' LIMIT 1";
				}
				else {
					command="SELECT CanadianNetworkNum FROM canadiannetwork WHERE Abbrev='TELUS B' AND RowNum<=1";
				}
				long canadianNetworkNumTelusB=PIn.Long(Db.GetScalar(command));
				command="UPDATE carrier SET "+
					"CDAnetVersion='04',"+
					"CanadianSupportedTypes=2044,"+//Claims, Reversals, Predeterminations, COBs.
					"CanadianNetworkNum="+POut.Long((long)canadianNetworkNumTelusB)+" "+
					"WHERE IsCDA<>0 AND ElectID='000016'";
				Db.NonQ32(command);
				command="UPDATE preference SET ValueString = '14.2.20.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To14_2_21();
		}

		private static void To14_2_21() {
			if(FromVersion<new Version("14.2.21.0")) {
				string command;
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE claim ADD ProvOrderOverride bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE claim ADD INDEX (ProvOrderOverride)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE claim ADD ProvOrderOverride number(20)";
					Db.NonQ(command);
					command="UPDATE claim SET ProvOrderOverride = 0 WHERE ProvOrderOverride IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE claim MODIFY ProvOrderOverride NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX claim_ProvOrderOverride ON claim (ProvOrderOverride)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE procedurelog ADD ProvOrderOverride bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE procedurelog ADD INDEX (ProvOrderOverride)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE procedurelog ADD ProvOrderOverride number(20)";
					Db.NonQ(command);
					command="UPDATE procedurelog SET ProvOrderOverride = 0 WHERE ProvOrderOverride IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE procedurelog MODIFY ProvOrderOverride NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX procedurelog_ProvOrderOverride ON procedurelog (ProvOrderOverride)";
					Db.NonQ(command);
				}
				//Users started to get a (403) Forbidden error when trying to update.
				//Come to find out it was due to a redirect issue.  We're going to update the Uri to point to opendental.com instead of open-dent.com so that this doesn't happen again.
				command=@"UPDATE preference SET ValueString='http://www.opendental.com/updates/' WHERE PrefName='UpdateWebsitePath' AND ValueString='http://www.open-dent.com/updates/'";
				Db.NonQ(command);
				command="UPDATE preference SET ValueString = '14.2.21.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To14_2_23();
		}

		private static void To14_2_23() {
			if(FromVersion<new Version("14.2.23.0")) {
				string command;
				//We fixed the (403) Forbidden error by getting rid of the redirect, changing the A record for open-dent.com and pointing it to HQ.
				//Therefore, we now want users to be pointing to open-dent instead of opendental.  This simply undoes what happened at the end of the 14.2.21 method.
				command=@"UPDATE preference SET ValueString='http://www.open-dent.com/updates/' WHERE PrefName='UpdateWebsitePath' AND ValueString='http://www.opendental.com/updates/'";
				Db.NonQ(command);
				command="UPDATE preference SET ValueString = '14.2.23.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To14_2_32();
		}

		private static void To14_2_32() {
			if(FromVersion<new Version("14.2.32.0")) {
				string command;
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('FamPhiAccess','1')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'FamPhiAccess','1')";
					Db.NonQ(command);
				}
				command="UPDATE preference SET ValueString = '14.2.32.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To14_3_1();
		}

		private static void To14_3_1() {
			if(FromVersion<new Version("14.3.1.0")) {
				string command;
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO ehrprovkey (LName,FName,YearValue,ProvKey)"
					+" SELECT provider.LName,provider.FName,14,provider.EhrKey"
					+" FROM provider WHERE provider.EhrKey!=''";
					Db.NonQ(command);
				}
				command="ALTER TABLE provider DROP COLUMN EhrKey";
				Db.NonQ(command);
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE provider ADD IsInstructor tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE provider ADD IsInstructor number(3)";
					Db.NonQ(command);
					command="UPDATE provider SET IsInstructor = 0 WHERE IsInstructor IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE provider MODIFY IsInstructor NOT NULL";
					Db.NonQ(command);
				}
				command="SELECT ValueString FROM preference WHERE PrefName = 'EasyHideDentalSchools'";
				string valueString=Db.GetScalar(command);
				if(valueString=="0") {//Works for Oracle as well.
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="SELECT provider.ProvNum FROM provider "
					+"INNER JOIN userod ON provider.ProvNum=userod.ProvNum "
					+"INNER JOIN usergroup ON userod.UserGroupNum=usergroup.UserGroupNum "
					+"INNER JOIN grouppermission ON grouppermission.UserGroupNum=usergroup.UserGroupNum "
					+"WHERE grouppermission.PermType=8";//Permission - Setup
						DataTable dt=Db.GetTable(command);
						StringBuilder sb=new StringBuilder();
						for(int i=0;i<dt.Rows.Count;i++) {
							sb.Append("UPDATE provider SET IsInstructor = 1 WHERE provider.ProvNum="+POut.Long((long)dt.Rows[i][0])+";");
						}
						try {
							Db.NonQ(sb.ToString());
						}
						catch(Exception ex) {
							//In the rare case that the StringBuilder is too large for the MySQL connector (very rare) we don't want the convert script to fail.
							//Users can go manually set IsInstructor after the upgrade finishes.
						}
					}
					else {//oracle
						//Oracle does not allow calling multiple queries in one call. We are skipping adding this permission for Oracle users. They can still manually add this permission.
					}
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('SecurityGroupForStudents','')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'SecurityGroupForStudents','')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('SecurityGroupForInstructors','')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'SecurityGroupForInstructors','')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS evaluation";
					Db.NonQ(command);
					command=@"CREATE TABLE evaluation (
						EvaluationNum bigint NOT NULL auto_increment PRIMARY KEY,
						InstructNum bigint NOT NULL,
						StudentNum bigint NOT NULL,
						SchoolCourseNum bigint NOT NULL,
						EvalTitle varchar(255) NOT NULL,
						DateEval date NOT NULL DEFAULT '0001-01-01',
						GradingScaleNum bigint NOT NULL,
						OverallGradeShowing varchar(255) NOT NULL,
						OverallGradeNumber float NOT NULL,
						Notes text NOT NULL,
						INDEX(InstructNum),
						INDEX(StudentNum),
						INDEX(SchoolCourseNum),
						INDEX(GradingScaleNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE evaluation'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE evaluation (
						EvaluationNum number(20) NOT NULL,
						InstructNum number(20) NOT NULL,
						StudentNum number(20) NOT NULL,
						SchoolCourseNum number(20) NOT NULL,
						EvalTitle varchar2(255),
						DateEval date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						GradingScaleNum number(20) NOT NULL,
						OverallGradeShowing varchar2(255),
						OverallGradeNumber number(38,8) NOT NULL,
						Notes varchar2(2000),
						CONSTRAINT evaluation_EvaluationNum PRIMARY KEY (EvaluationNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX evaluation_InstructNum ON evaluation (InstructNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX evaluation_StudentNum ON evaluation (StudentNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX evaluation_SchoolCourseNum ON evaluation (SchoolCourseNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX evaluation_GradingScaleNum ON evaluation (GradingScaleNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS evaluationcriterion";
					Db.NonQ(command);
					command=@"CREATE TABLE evaluationcriterion (
						EvaluationCriterionNum bigint NOT NULL auto_increment PRIMARY KEY,
						EvaluationNum bigint NOT NULL,
						CriterionDescript varchar(255) NOT NULL,
						IsCategoryName tinyint NOT NULL,
						GradingScaleNum bigint NOT NULL,
						GradeShowing varchar(255) NOT NULL,
						GradeNumber float NOT NULL,
						Notes text NOT NULL,
						ItemOrder int NOT NULL,
						INDEX(EvaluationNum),
						INDEX(GradingScaleNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE evaluationcriterion'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE evaluationcriterion (
						EvaluationCriterionNum number(20) NOT NULL,
						EvaluationNum number(20) NOT NULL,
						CriterionDescript varchar(255) NOT NULL,
						IsCategoryName number(3) NOT NULL,
						GradingScaleNum number(20) NOT NULL,
						GradeShowing varchar2(255),
						GradeNumber number(38,8) NOT NULL,
						Notes varchar2(2000),
						ItemOrder number(11) NOT NULL,
						CONSTRAINT evaluationcriterion_Evaluation PRIMARY KEY (EvaluationCriterionNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX evaluationcriterion_EvalNum ON evaluationcriterion (EvaluationNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX evaluationcriterion_GradingSca ON evaluationcriterion (GradingScaleNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS evaluationcriteriondef";
					Db.NonQ(command);
					command=@"CREATE TABLE evaluationcriteriondef (
						EvaluationCriterionDefNum bigint NOT NULL auto_increment PRIMARY KEY,
						EvaluationDefNum bigint NOT NULL,
						CriterionDescript varchar(255) NOT NULL,
						IsCategoryName tinyint NOT NULL,
						GradingScaleNum bigint NOT NULL,
						ItemOrder int NOT NULL,
						INDEX(EvaluationDefNum),
						INDEX(GradingScaleNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE evaluationcriteriondef'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE evaluationcriteriondef (
						EvaluationCriterionDefNum number(20) NOT NULL,
						EvaluationDefNum number(20) NOT NULL,
						CriterionDescript varchar(255) NOT NULL,
						IsCategoryName number(3) NOT NULL,
						GradingScaleNum number(20) NOT NULL,
						ItemOrder number(11) NOT NULL,
						CONSTRAINT evaluationcriteriondef_Evaluat PRIMARY KEY (EvaluationCriterionDefNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX evaluationcriteriondef_EvalDef ON evaluationcriteriondef (EvaluationDefNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX evaluationcriteriondef_Grading ON evaluationcriteriondef (GradingScaleNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS evaluationdef";
					Db.NonQ(command);
					command=@"CREATE TABLE evaluationdef (
						EvaluationDefNum bigint NOT NULL auto_increment PRIMARY KEY,
						SchoolCourseNum bigint NOT NULL,
						EvalTitle varchar(255) NOT NULL,
						GradingScaleNum bigint NOT NULL,
						INDEX(SchoolCourseNum),
						INDEX(GradingScaleNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE evaluationdef'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE evaluationdef (
						EvaluationDefNum number(20) NOT NULL,
						SchoolCourseNum number(20) NOT NULL,
						EvalTitle varchar2(255),
						GradingScaleNum number(20) NOT NULL,
						CONSTRAINT evaluationdef_EvaluationDefNum PRIMARY KEY (EvaluationDefNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX evaluationdef_SchoolCourseNum ON evaluationdef (SchoolCourseNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX evaluationdef_GradingScaleNum ON evaluationdef (GradingScaleNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS gradingscale";
					Db.NonQ(command);
					command=@"CREATE TABLE gradingscale (
						GradingScaleNum bigint NOT NULL auto_increment PRIMARY KEY,
						Description varchar(255) NOT NULL
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE gradingscale'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE gradingscale (
						GradingScaleNum number(20) NOT NULL,
						Description varchar2(255),
						CONSTRAINT gradingscale_GradingScaleNum PRIMARY KEY (GradingScaleNum)
						)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS gradingscaleitem";
					Db.NonQ(command);
					command=@"CREATE TABLE gradingscaleitem (
						GradingScaleItemNum bigint NOT NULL auto_increment PRIMARY KEY,
						GradingScaleNum bigint NOT NULL,
						GradeShowing varchar(255) NOT NULL,
						GradeNumber float NOT NULL,
						Description varchar(255) NOT NULL,
						INDEX(GradingScaleNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE gradingscaleitem'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE gradingscaleitem (
						GradingScaleItemNum number(20) NOT NULL,
						GradingScaleNum number(20) NOT NULL,
						GradeShowing varchar2(255),
						GradeNumber number(38,8) NOT NULL,
						Description varchar2(255),
						CONSTRAINT gradingscaleitem_GradingScaleI PRIMARY KEY (GradingScaleItemNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX gradingscaleitem_GradingScaleN ON gradingscaleitem (GradingScaleNum)";
					Db.NonQ(command);
				}
				//Add OrthoChartEdit permission to everyone------------------------------------------------------
				command="SELECT DISTINCT UserGroupNum FROM grouppermission";
				DataTable table=Db.GetTable(command);
				long groupNum;
				if(DataConnection.DBtype==DatabaseType.MySql) {
					for(int i=0;i<table.Rows.Count;i++) {
						groupNum=PIn.Long(table.Rows[i]["UserGroupNum"].ToString());
						command="INSERT INTO grouppermission (UserGroupNum,PermType) "
							+"VALUES("+POut.Long(groupNum)+",79)";
						Db.NonQ(command);
					}
				}
				else {//oracle
					for(int i=0;i<table.Rows.Count;i++) {
						groupNum=PIn.Long(table.Rows[i]["UserGroupNum"].ToString());
						command="INSERT INTO grouppermission (GroupPermNum,NewerDays,UserGroupNum,PermType) "
							+"VALUES((SELECT MAX(GroupPermNum)+1 FROM grouppermission),0,"+POut.Long(groupNum)+",79)";
						Db.NonQ(command);
					}
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE provider ADD EhrMuStage int NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE provider ADD EhrMuStage number(3)";
					Db.NonQ(command);
					command="UPDATE provider SET EhrMuStage = 0 WHERE EhrMuStage IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE provider MODIFY EhrMuStage NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE claimproc ADD PayPlanNum bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE claimproc ADD INDEX (PayPlanNum)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE claimproc ADD PayPlanNum number(20)";
					Db.NonQ(command);
					command="UPDATE claimproc SET PayPlanNum = 0 WHERE PayPlanNum IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE claimproc MODIFY PayPlanNum NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX claimproc_PayPlanNum ON claimproc (PayPlanNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE etrans ADD TranSetId835 varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE etrans ADD TranSetId835 varchar2(255)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE evaluationcriteriondef ADD MaxPointsPoss float NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE evaluationcriteriondef ADD MaxPointsPoss number(38,8)";
					Db.NonQ(command);
					command="UPDATE evaluationcriteriondef SET MaxPointsPoss = 0 WHERE MaxPointsPoss IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE evaluationcriteriondef MODIFY MaxPointsPoss NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE gradingscale ADD ScaleType tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE gradingscale ADD ScaleType number(3)";
					Db.NonQ(command);
					command="UPDATE gradingscale SET ScaleType = 0 WHERE ScaleType IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE gradingscale MODIFY ScaleType NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE evaluationcriterion ADD MaxPointsPoss float NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE evaluationcriterion ADD MaxPointsPoss number(38,8)";
					Db.NonQ(command);
					command="UPDATE evaluationcriterion SET MaxPointsPoss = 0 WHERE MaxPointsPoss IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE evaluationcriterion MODIFY MaxPointsPoss NOT NULL";
					Db.NonQ(command);
				}
				//Insert HandyDentist bridge
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO program (ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"'HandyDentist', "
				    +"'HandyDentist from handycreate.com', "
				    +"'0', "
				    +"'"+POut.String(@"C:\HandyDentist\HandyDentist.exe")+"', "
				    +"'', "
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"'"+POut.Long(programNum)+"', "
				    +"'2', "//ToolBarsAvail.ChartModule
				    +"'HandyDentist')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO program (ProgramNum,ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"(SELECT MAX(ProgramNum)+1 FROM program),"
				    +"'HandyDentist', "
				    +"'HandyDentist from handycreate.com', "
				    +"'0', "
				    +"'"+POut.String(@"C:\HandyDentist\HandyDentist.exe")+"', "
				    +"'', "
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ToolButItemNum,ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"(SELECT MAX(ToolButItemNum)+1 FROM toolbutitem),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'2', "//ToolBarsAvail.ChartModule
				    +"'HandyDentist')";
					Db.NonQ(command);
				}//end HandyDentist bridge
				//Insert XVWeb bridge
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO program (ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
						+") VALUES("
						+"'XVWeb', "
						+"'XVWeb from www.apteryx.com/xvweb', "
						+"'0', "
						+"'', "
						+"'', "//leave blank if none
						+"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"'"+POut.Long(programNum)+"', "
						+"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
						+"'0')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"'"+POut.Long(programNum)+"', "
						+"'Enter desired URL address for XVWeb', "
						+"'https://demo2.apteryxweb.com/')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ProgramNum,ToolBar,ButtonText) "
						+"VALUES ("
						+"'"+POut.Long(programNum)+"', "
						+"'2', "//ToolBarsAvail.ChartModule
						+"'XVWeb')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO program (ProgramNum,ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
						+") VALUES("
						+"(SELECT MAX(ProgramNum)+1 FROM program),"
						+"'XVWeb', "
						+"'XVWeb from www.apteryx.com/xvweb', "
						+"'0', "
						+"'', "
						+"'', "//leave blank if none
						+"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
						+"'"+POut.Long(programNum)+"', "
						+"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
						+"'0')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
						+"'"+POut.Long(programNum)+"', "
						+"'Enter desired URL address for XVWeb', "
						+"'https://demo2.apteryxweb.com/')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ToolButItemNum,ProgramNum,ToolBar,ButtonText) "
						+"VALUES ("
						+"(SELECT MAX(ToolButItemNum)+1 FROM toolbutitem),"
						+"'"+POut.Long(programNum)+"', "
						+"'2', "//ToolBarsAvail.ChartModule
						+"'XVWeb')";
					Db.NonQ(command);
				}//end XVWeb bridge
				//Insert VixWinBase36 bridge
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO program (ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
						+") VALUES("
						+"'VixWinBase36', "
						+"'VixWin(Base36) from www.gendexxray.com', "
						+"'0', "
						+"'"+POut.String(@"C:\VixWin\VixWin.exe")+"',"
						+"'', "
						+"'This VixWin bridge uses base 36 PatNums.')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+POut.Long(programNum)+", "
						+"'Image Path', "
						+"'')";//User will be required to set up image path before using bridge. If they try to use it they will get a warning message and the bridge will fail gracefully.
					Db.NonQ32(command);
					command="INSERT INTO toolbutitem (ProgramNum,ToolBar,ButtonText) "
						+"VALUES ("
						+POut.Long(programNum)+", "
						+"'2', "//ToolBarsAvail.ChartModule
						+"'VixWinBase36')";
					Db.NonQ32(command);
				}
				else {//oracle
					command="INSERT INTO program (ProgramNum,ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
						+") VALUES("
						+"(SELECT MAX(ProgramNum)+1 FROM program),"
						+"'VixWinBase36', "
						+"'VixWin(Base36) from www.gendexxray.com', "
						+"'0', "
						+"'"+POut.String(@"C:\VixWin\VixWin.exe")+"',"
						+"'', "
						+"'This VixWin bridge uses base 36 PatNums.')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"(SELECT MAX(ProgramPropertyNum)+1 FROM programproperty),"
						+POut.Long(programNum)+", "
						+"'Image Path', "
						+"'')";//User will be required to set up image path before using bridge. If they try to use it they will get a warning message and the bridge will fail gracefully.
					Db.NonQ32(command);
					command="INSERT INTO toolbutitem (ToolButItemNum,ProgramNum,ToolBar,ButtonText) "
						+"VALUES ("
						+"(SELECT MAX(ToolButItemNum)+1 FROM toolbutitem),"
						+POut.Long(programNum)+", "
						+"'2', "//ToolBarsAvail.ChartModule
						+"'VixWinBase36')";
					Db.NonQ32(command);
				}//end VixWinBase36 bridge
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE procedurelog ADD Discount double NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE procedurelog ADD Discount number(38,8)";
					Db.NonQ(command);
					command="UPDATE procedurelog SET Discount = 0 WHERE Discount IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE procedurelog MODIFY Discount NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('TreatPlanDiscountPercent','0')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'TreatPlanDiscountPercent','0')";
					Db.NonQ(command);
				}
				//Make sure the database has a "Discount" definition because we are adding a new feature that needs a default discount adjustment type.
				command="SELECT DefNum,IsHidden FROM definition WHERE Category=1 AND ItemName='Discount'";//1 - AdjTypes
				table=Db.GetTable(command);
				if(table.Rows.Count==0) {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="INSERT INTO definition(Category,ItemName,ItemValue) VALUES(1,'Discount','-')";//1 - AdjTypes, ItemValue of '-' makes it a subtraction type.	
						long defNum=Db.NonQ(command);
						table.Rows.Add(defNum,0);
					}
					else {
						command="INSERT INTO definition(DefNum,Category,ItemOrder,ItemColor,ItemName,ItemValue,IsHidden) VALUES((SELECT MAX(DefNum)+1,1,SELECT MAX(ItemOrder)+1,0,'Discount','-',0)";//1 - AdjTypes, ItemValue of '-' makes it a subtraction type.	
						long defNum=Db.NonQ(command);
						table.Rows.Add(defNum,0);
					}
				}
				else {
					if(table.Rows[0]["IsHidden"].ToString()=="1") {
						command="UPDATE definition SET IsHidden=0 WHERE DefNum='"+table.Rows[0]["DefNum"]+"'";
						Db.NonQ(command);
					}
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('TreatPlanDiscountAdjustmentType','"+table.Rows[0]["DefNum"]+"')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'TreatPlanDiscountAdjustmentType','"+table.Rows[0]["DefNum"]+"')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="SELECT usergroup.UserGroupNum FROM usergroup "
					+"INNER JOIN grouppermission ON grouppermission.UserGroupNum=usergroup.UserGroupNum "
					+"WHERE grouppermission.PermType=17";//Permission - AdjustmentCreate
					DataTable dts=Db.GetTable(command);
					StringBuilder sbs=new StringBuilder();
					for(int i=0;i<dts.Rows.Count;i++) {
						sbs.Append("INSERT INTO grouppermission(UserGroupNum,PermType) VALUES("+POut.Long((long)dts.Rows[i][0])+",82);");//Permission - TreatPlanDiscountEdit
					}
					try {
						Db.NonQ(sbs.ToString());
					}
					catch(Exception ex) {
						//In the rare case that the StringBuilder is too large for the MySQL connector (very rare) we don't want the convert script to fail.
						//Users can go manually set IsInstructor after the upgrade finishes.
					}
				}
				else {//oracle
					//Oracle does not allow calling multiple queries in one call. We are skipping adding this permission for Oracle users. They can still manually add this permission.
				}
				//Insert AudaxCeph bridge
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO program (ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
						+") VALUES("
						+"'AudaxCeph', "
						+"'AudaxCeph from www.audaxceph.com', "
						+"'0', "
						+"'"+POut.String(@"C:\AudaxCeph\AxCeph.exe")+"',"
						+"'', "
						+"'AudaxCeph needs to be running in the background for the bridge to work.')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"'"+POut.Long(programNum)+"', "
						+"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
						+"'0')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ProgramNum,ToolBar,ButtonText) "
						+"VALUES ("
						+POut.Long(programNum)+", "
						+"'2', "//ToolBarsAvail.ChartModule
						+"'AudaxCeph')";
					Db.NonQ32(command);
				}
				else {//oracle
					command="INSERT INTO program (ProgramNum,ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
						+") VALUES("
						+"(SELECT MAX(ProgramNum)+1 FROM program),"
						+"'AudaxCeph', "
						+"'AudaxCeph from www.audaxceph.com', "
						+"'0', "
						+"'"+POut.String(@"C:\AudaxCeph\AxCeph.exe")+"',"
						+"'', "
						+"'AudaxCeph needs to be running in the background for the bridge to work.')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
						+"'"+POut.Long(programNum)+"', "
						+"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
						+"'0')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ToolButItemNum,ProgramNum,ToolBar,ButtonText) "
						+"VALUES ("
						+"(SELECT MAX(ToolButItemNum)+1 FROM toolbutitem),"
						+POut.Long(programNum)+", "
						+"'2', "//ToolBarsAvail.ChartModule
						+"'AudaxCeph')";
					Db.NonQ32(command);
				}//end AudaxCeph bridge
				//Insert PandaPerio bridge
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO program (ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"'PandaPerio', "
				    +"'PandaPerio from www.pandaperio.com', "
				    +"'0', "
				    +"'"+POut.String(@"C:\Program Files (x86)\Panda Perio\Panda.exe")+"', "
				    +"'', "
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"'"+POut.Long(programNum)+"', "
				    +"'2', "//ToolBarsAvail.ChartModule
				    +"'PandaPerio')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO program (ProgramNum,ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"(SELECT MAX(ProgramNum)+1 FROM program),"
				    +"'PandaPerio', "
				    +"'PandaPerio from www.pandaperio.com', "
				    +"'0', "
				    +"'"+POut.String(@"C:\Program Files (x86)\Panda Perio\Panda.exe")+"', "
				    +"'', "
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ToolButItemNum,ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"(SELECT MAX(ToolButItemNum)+1 FROM toolbutitem),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'2', "//ToolBarsAvail.ChartModule
				    +"'PandaPerio')";
					Db.NonQ(command);
				}//end PandaPerio bridge
				//Insert DemandForce bridge
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO program (ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"'DemandForce', "
				    +"'DemandForce from www.demandforce.com', "
				    +"'0', "
				    +"'"+POut.String(@"d3one.exe")+"', "
				    +"'', "
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter your DemandForce license key (required)', "
				    +"'')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"'"+POut.Long(programNum)+"', "
				    +"'7', "//ToolBarsAvail.ChartModule
				    +"'DemandForce')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO program (ProgramNum,ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"(SELECT MAX(ProgramNum)+1 FROM program),"
				    +"'DemandForce', "
				    +"'DemandForce from www.demandforce.com', "
				    +"'0', "
				    +"'"+POut.String(@"d3one.exe")+"', "
				    +"'', "
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter your DemandForce license key (required)', "
				    +"'')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ToolButItemNum,ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"(SELECT MAX(ToolButItemNum)+1 FROM toolbutitem),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'7', "//ToolBarsAvail.ChartModule
				    +"'DemandForce')";
					Db.NonQ(command);
				}//end DemandForce bridge
				//Update any text based columns that are not CLOBs to allow NULL entries.  This is because Oracle treats empty strings as NULLs.
				if(DataConnection.DBtype==DatabaseType.Oracle) {
					command=@"SELECT TABLE_NAME, COLUMN_NAME 
						FROM USER_TAB_COLUMNS 
						WHERE NULLABLE='N' AND DATA_TYPE LIKE '%CHAR%'";
					table=Db.GetTable(command);
					for(int i=0;i<table.Rows.Count;i++) {
						command="ALTER TABLE "+table.Rows[i]["TABLE_NAME"].ToString()+" MODIFY("+table.Rows[i]["COLUMN_NAME"].ToString()+" NULL)";
						try {
							Db.NonQ(command);
						}
						catch {
							//This will only cause issues if the user tries to insert empty string into a NOT NULL text based column.
							//Therefore, I'd rather the failure happen within the program instead of here in the convert script.
						}
					}
				}
				//Added ReplicationUserQueryServer preference to stop CREATE TABLE or DROP TABLE user queries from being ran on any computer that is not the ReplicationUserQueryServer.
				//This is set in the Replication Setup Window.  Defaults to empty string.
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('ReplicationUserQueryServer','')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'ReplicationUserQueryServer','')";
					Db.NonQ(command);
				}
				//Insert iRYS NNTBridge bridge
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO program (ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"'iRYS', "
				    +"'iRYS from www.cefladental.com', "
				    +"'0', "
				    +"'"+POut.String(@"C:\NNT\NNTBridge.exe")+"', "
				    +"'', "
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"'"+POut.Long(programNum)+"', "
				    +"'2', "//ToolBarsAvail.ChartModule
				    +"'iRYS')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO program (ProgramNum,ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"(SELECT MAX(ProgramNum)+1 FROM program),"
				    +"'iRYS', "
				    +"'iRYS from www.cefladental.com', "
				    +"'0', "
				    +"'"+POut.String(@"C:\NNT\NNTBridge.exe")+"', "
				    +"'', "
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ToolButItemNum,ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"(SELECT MAX(ToolButItemNum)+1 FROM toolbutitem),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'2', "//ToolBarsAvail.ChartModule
				    +"'iRYS')";
					Db.NonQ(command);
				}//end iRYS bridge
				//Insert visOra bridge
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO program (ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"'visOra', "
				    +"'visOra from www.visoraimaging.com', "
				    +"'0', "
				    +"'"+POut.String(@"C:\Program Files\Cieos\Cieos Workstation\Cieos.Workstation.Shell.exe")+"', "
				    +"'', "
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"'"+POut.Long(programNum)+"', "
				    +"'2', "//ToolBarsAvail.ChartModule
				    +"'visOra')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO program (ProgramNum,ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"(SELECT MAX(ProgramNum)+1 FROM program),"
				    +"'visOra', "
				    +"'visOra from www.visoraimaging.com', "
				    +"'0', "
				    +"'"+POut.String(@"C:\Program Files\Cieos\Cieos Workstation\Cieos.Workstation.Shell.exe")+"', "
				    +"'', "
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ToolButItemNum,ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"(SELECT MAX(ToolButItemNum)+1 FROM toolbutitem),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'2', "//ToolBarsAvail.ChartModule
				    +"'visOra')";
					Db.NonQ(command);
				}//end visOra bridge
				//Insert Z-Image bridge
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO program (ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"'Z-Image', "
				    +"'Z-Image from www.visoraimaging.com', "
				    +"'0', "
				    +"'"+POut.String(@"C:\Program Files\Zuma\Zuma Workstation\Zuma.Workstation.Shell.exe")+"', "
				    +"'', "
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"'"+POut.Long(programNum)+"', "
				    +"'2', "//ToolBarsAvail.ChartModule
				    +"'Z-Image')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO program (ProgramNum,ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"(SELECT MAX(ProgramNum)+1 FROM program),"
				    +"'Z-Image', "
				    +"'Z-Image from www.visoraimaging.com', "
				    +"'0', "
				    +"'"+POut.String(@"C:\Program Files\Zuma\Zuma Workstation\Zuma.Workstation.Shell.exe")+"', "
				    +"'', "
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ToolButItemNum,ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"(SELECT MAX(ToolButItemNum)+1 FROM toolbutitem),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'2', "//ToolBarsAvail.ChartModule
				    +"'Z-Image')";
					Db.NonQ(command);
				}//end Z-Image bridge
				//Add option to hide Rx buttons in Chart Module for eClinicalWorks
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="SELECT ProgramNum FROM program WHERE ProgName='eClinicalWorks'";
					long programNum=PIn.Long(Db.GetScalar(command));
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
							+") VALUES("
							+"'"+POut.Long(programNum)+"', "
							+"'HideChartRxButtons', "
							+"'0')";//set to 0 (false) by default so behavior of existing customers will not change
					Db.NonQ(command);
				}
				else {//oracle
					//eCW will never use Oracle.
				}
				//Added AccountShowQuestionnaire preference to show Questionnaire button in account module.  This is set in the Show Features window.  Defaults to false.
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('AccountShowQuestionnaire','0')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'AccountShowQuestionnaire','0')";
					Db.NonQ(command);
				}
				//Added AccountShowTrojanExpressCollect preference to show TrojanCollect button in account module.  This is set in the Show Features window.  Defaults to false.
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('AccountShowTrojanExpressCollect','0')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'AccountShowTrojanExpressCollect','0')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE userod ADD InboxHidePopups tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE userod ADD InboxHidePopups number(3)";
					Db.NonQ(command);
					command="UPDATE userod SET InboxHidePopups = 0 WHERE InboxHidePopups IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE userod MODIFY InboxHidePopups NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('ChartNonPatientWarn','1')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'ChartNonPatientWarn','1')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('TreatPlanItemized','1')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'TreatPlanItemized','1')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS procbuttonquick";
					Db.NonQ(command);
					command=@"CREATE TABLE procbuttonquick (
						ProcButtonQuickNum bigint NOT NULL auto_increment PRIMARY KEY,
						Description varchar(255) NOT NULL,
						CodeValue varchar(255) NOT NULL,
						Surf varchar(255) NOT NULL,
						YPos int NOT NULL,
						ItemOrder int NOT NULL,
						IsLabel tinyint NOT NULL
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE procbuttonquick'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE procbuttonquick (
						ProcButtonQuickNum number(20) NOT NULL,
						Description varchar2(255),
						CodeValue varchar2(255),
						Surf varchar2(255),
						YPos number(11) NOT NULL,
						ItemOrder number(11) NOT NULL,
						IsLabel number(3) NOT NULL,
						CONSTRAINT procbuttonquick_ProcButtonQuic PRIMARY KEY (ProcButtonQuickNum)
						)";
					Db.NonQ(command);
				}
				//Fill ProckButton Quick with buttons to emulate current behavior with new ODButtonPanel
				//MySQL and Oracle compatible
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (1,'Post. Composite','','',0,0,1)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (2,'MO','D2392','MO',0,1,0)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (3,'MOD','D2393','MOD',1,1,0)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (4,'O','D2391','O',2,1,0)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (5,'DO','D2392','DO',3,1,0)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (7,'        ','','',4,1,1)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (6,'Seal','D1351','',5,1,0)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (8,'OL','D2392','OL',0,2,0)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (9,'OB','D2392','OB',1,2,0)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (10,'MODL','D2394','MODL',2,2,0)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (11,'MODB','D2394','MODB',3,2,0)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (12,'Ant. Composite','','',0,4,1)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (13,'DL','D2331','',0,5,0)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (14,'MDL','D2332','',1,5,0)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (15,'ML','D2331','',2,5,0)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (16,'Amalgam','','',0,7,1)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (17,'MO','D2150','MO',0,8,0)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (18,'MOD','D2160','MOD',1,8,0)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (19,'O','D2140','O',2,8,0)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (20,'DO','D2150','DO',3,8,0)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (21,'OL','D2150','OL',0,9,0)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (22,'OB','D2150','OB',1,9,0)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (23,'MODL','D2161','MODL',2,9,0)";
				Db.NonQ(command);
				command="INSERT INTO procbuttonquick (ProcButtonQuickNum, Description, CodeValue, Surf, ItemOrder, YPos, IsLabel) VALUES (24,'MODB','D2161','MODB',3,9,0)";
				Db.NonQ(command);
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE sheetdef ADD PageCount int NOT NULL";
					Db.NonQ(command);
					command="UPDATE sheetdef SET PageCount = 1";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE sheetdef ADD PageCount number(11)";
					Db.NonQ(command);
					command="UPDATE sheetdef SET PageCount = 1 WHERE PageCount IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE sheetdef MODIFY PageCount NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE hl7defmessage ADD MessageStructure varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE hl7defmessage ADD MessageStructure varchar2(255)";
					Db.NonQ(command);
				}
				//Oracle compatible
				command="UPDATE hl7defmessage SET MessageStructure='ADT_A01' WHERE EventType='A04'";//All ADT's and ACK messages are event type A04 in the db
				Db.NonQ(command);
				command="UPDATE hl7defmessage SET MessageStructure='SIU_S12' WHERE EventType='S12'";//All SIU's are event type S12 in the db
				Db.NonQ(command);
				command="UPDATE hl7defmessage SET MessageStructure='DFT_P03' WHERE EventType='P03'";//All DFT's are event type P03 in the db
				Db.NonQ(command);
				command="UPDATE hl7defmessage SET MessageStructure='NotDefined' WHERE EventType='NotDefined' OR EventType=''";//Any messages with NotDefined or blank event type
				Db.NonQ(command);
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE appointment ADD AppointmentTypeNum bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE appointment ADD INDEX (AppointmentTypeNum)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE appointment ADD AppointmentTypeNum number(20)";
					Db.NonQ(command);
					command="UPDATE appointment SET AppointmentTypeNum = 0 WHERE AppointmentTypeNum IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE appointment MODIFY AppointmentTypeNum NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX appointment_AppointmentTypeNum ON appointment (AppointmentTypeNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS appointmenttype";
					Db.NonQ(command);
					command=@"CREATE TABLE appointmenttype (
						AppointmentTypeNum bigint NOT NULL auto_increment PRIMARY KEY,
						AppointmentTypeName varchar(255) NOT NULL,
						AppointmentTypeColor int NOT NULL,
						ItemOrder int NOT NULL,
						IsHidden tinyint NOT NULL
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE appointmenttype'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE appointmenttype (
						AppointmentTypeNum number(20) NOT NULL,
						AppointmentTypeName varchar2(255),
						AppointmentTypeColor number(11) NOT NULL,
						ItemOrder number(11) NOT NULL,
						IsHidden number(3) NOT NULL,
						CONSTRAINT appointmenttype_AppointmentTyp PRIMARY KEY (AppointmentTypeNum)
						)";
					Db.NonQ(command);
				}
				try {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="ALTER TABLE etrans ADD INDEX (etransmessagetextnum)";
						Db.NonQ(command);
					}
					else {//oracle
						command=@"CREATE INDEX etrans_etransmessagetextnum ON etrans (etransmessagetextnum)";
						Db.NonQ(command);
					}
				}
				catch(Exception ex) { }//Only an index. (Exception ex) required to catch thrown exception
				command="UPDATE preference SET ValueString = '14.3.1.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To14_3_3();
		}

		private static void To14_3_3() {
			if(FromVersion<new Version("14.3.3.0")) {
				string command;
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE sheet ADD IsMultiPage tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE sheet ADD IsMultiPage number(3)";
					Db.NonQ(command);
					command="UPDATE sheet SET IsMultiPage = 0 WHERE IsMultiPage IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE sheet MODIFY IsMultiPage NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE sheetdef ADD IsMultiPage tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE sheetdef ADD IsMultiPage number(3)";
					Db.NonQ(command);
					command="UPDATE sheetdef SET IsMultiPage = 0 WHERE IsMultiPage IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE sheetdef MODIFY IsMultiPage NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE sheetfield ADD INDEX (FieldType)";
					Db.NonQ(command);
				}
				else {//oracle
					command=@"CREATE INDEX sheetfield_FieldType ON sheetfield (FieldType)";
					Db.NonQ(command);
				}
				command="UPDATE preference SET ValueString = '14.3.3.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To14_3_4();
		}

		private static void To14_3_4() {
			if(FromVersion<new Version("14.3.4.0")) {
				string command;
				//adding EmailNotifyAddressNum preference
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="SELECT ValueString FROM preference WHERE PrefName='EmailDefaultAddressNum' LIMIT 1";
				}
				else {//oracle doesn't have LIMIT
					command="SELECT * FROM (SELECT ValueString FROM preference WHERE PrefName='EmailDefaultAddressNum') WHERE RowNum<=1";
				}
				long emailDefaultAddressNum=PIn.Long(Db.GetScalar(command));
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('EmailNotifyAddressNum','"+POut.Long(emailDefaultAddressNum)+"')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'EmailNotifyAddressNum','"+POut.Long(emailDefaultAddressNum)+"')";
					Db.NonQ(command);
				}
				command="UPDATE preference SET ValueString = '14.3.4.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To14_3_9();
		}

		///<summary>Oracle compatible: 10/08/2014</summary>
		private static void To14_3_9() {
			if(FromVersion<new Version("14.3.9.0")) {
				string command;
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="SELECT COUNT(*) FROM preference WHERE PrefName='FamPhiAccess' LIMIT 1";
				}
				else {//oracle doesn't have LIMIT
					command="SELECT COUNT(*) FROM (SELECT ValueString FROM preference WHERE PrefName='FamPhiAccess') WHERE RowNum<=1";
				}
				long hasFamPhiAccess=PIn.Long(Db.GetCount(command));
				if(hasFamPhiAccess==0) {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="INSERT INTO preference(PrefName,ValueString) VALUES('FamPhiAccess','1')";
						Db.NonQ(command);
					}
					else {//oracle
						command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'FamPhiAccess','1')";
						Db.NonQ(command);
					}
				}
				command="UPDATE preference SET ValueString = '14.3.9.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To14_3_12();
		}

		///<summary>Oracle compatible: 10/10/2014</summary>
		private static void To14_3_12() {
			if(FromVersion<new Version("14.3.12.0")) {
				string command;
				//Moving codes to the Obsolete category that were deleted in CDT 2015.
				if(CultureInfo.CurrentCulture.Name.EndsWith("US")) {//United States
					//Move depricated codes to the Obsolete procedure code category.
					//Make sure the procedure code category exists before moving the procedure codes.
					string procCatDescript="Obsolete";
					long defNum=0;
					command="SELECT DefNum FROM definition WHERE Category=11 AND ItemName='"+POut.String(procCatDescript)+"'";//11 is DefCat.ProcCodeCats
					DataTable dtDef=Db.GetTable(command);
					if(dtDef.Rows.Count==0) { //The procedure code category does not exist, add it
						if(DataConnection.DBtype==DatabaseType.MySql) {
							command="INSERT INTO definition (Category,ItemName,ItemOrder) "
									+"VALUES (11"+",'"+POut.String(procCatDescript)+"',"+POut.Long(DefC.Long[11].Length)+")";//11 is DefCat.ProcCodeCats
						}
						else {//oracle
							command="INSERT INTO definition (DefNum,Category,ItemName,ItemOrder) "
									+"VALUES ((SELECT MAX(DefNum)+1 FROM definition),11,'"+POut.String(procCatDescript)+"',"+POut.Long(DefC.Long[11].Length)+")";//11 is DefCat.ProcCodeCats
						}
						defNum=Db.NonQ(command,true);
					}
					else { //The procedure code category already exists, get the existing defnum
						defNum=PIn.Long(dtDef.Rows[0]["DefNum"].ToString());
					}
					string[] arrayCdtCodesDeleted=new string[] {
						"D6053","D6054","D6078","D6079","D6975"
					};
					for(int i=0;i<arrayCdtCodesDeleted.Length;i++) {
						string procCode=arrayCdtCodesDeleted[i];
						command="SELECT CodeNum FROM procedurecode WHERE ProcCode='"+POut.String(procCode)+"'";
						DataTable dtProcCode=Db.GetTable(command);
						if(dtProcCode.Rows.Count==0) { //The procedure code does not exist in this database.
							continue;//Do not try to move it.
						}
						long codeNum=PIn.Long(dtProcCode.Rows[0]["CodeNum"].ToString());
						command="UPDATE procedurecode SET ProcCat="+POut.Long(defNum)+" WHERE CodeNum="+POut.Long(codeNum);
						Db.NonQ(command);
					}
				}//end United States update
				command="UPDATE preference SET ValueString = '14.3.12.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To14_3_24();
		}

		///<summary></summary>
		private static void To14_3_24() {
			if(FromVersion<new Version("14.3.24.0")) {
				string command="";
				//Bug fix for missing surfaces on quickbuttons. This should only affect quick buttons if they have not been edited by users.
				command=@"UPDATE procbuttonquick SET Surf='DL' WHERE ProcButtonQuickNum=13 AND Description='DL'AND CodeValue='D2331' AND Surf=''";
				Db.NonQ(command);
				command=@"UPDATE procbuttonquick SET Surf='MDL' WHERE ProcButtonQuickNum=13 AND Description='MDL'AND CodeValue='D2332' AND Surf=''";
				Db.NonQ(command);
				command=@"UPDATE procbuttonquick SET Surf='ML' WHERE ProcButtonQuickNum=13 AND Description='ML'AND CodeValue='D2331' AND Surf=''";
				Db.NonQ(command);
				command="UPDATE preference SET ValueString = '14.3.24.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To14_3_30();
		}

		///<summary></summary>
		private static void To14_3_30() {
			if(FromVersion<new Version("14.3.30.0")) {
				string command="";
				//Inline DBM to remove old signals.  This query is run regularly as of version 15.1.  This is here to tide user over until they update to version 15.1.
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DELETE FROM signalod WHERE SigType = 1 AND SigDateTime < DATE_ADD(NOW(),INTERVAL -2 DAY)";//Itypes only older than 2 days
					Db.NonQ(command);
					command="DELETE FROM signalod WHERE SigType = 0 AND AckTime != '0001-01-01' AND SigDateTime < DATE_ADD(NOW(),INTERVAL -2 DAY)";//Only unacknowledged buttons older than 2 days
					Db.NonQ(command);
				}
				else {//oracle
					command="DELETE FROM signalod WHERE SigType = 1 AND SigDateTime < CURRENT_TIMESTAMP -2";//Itypes only older than 2 days
					Db.NonQ(command);
					command="DELETE FROM signalod WHERE SigType = 0 AND AckTime != TO_DATE('0001-01-01','YYYY-MM-DD') AND SigDateTime < CURRENT_TIMESTAMP -2";//Only unacknowledged buttons older than 2 days
					Db.NonQ(command);
				}
				//The ReplicationUserQueryServer preference used to store the "case insensitive computer name" of one singular computer.
				//When using replication, that one computer was designated as the ONLY computer that could run dangerous user queries.
				//Nathan qualified this as a bug because it was not good enough for one of our large customers.  We instead need to have the preference store the Repliction Server PK.
				//This way, ANY computer connected to the "report server" can run dangerous user queries.
				command="UPDATE preference SET ValueString = '0' WHERE PrefName = 'ReplicationUserQueryServer'";
				Db.NonQ(command);//Simply clear out the old computer name because there is no way we can guess which specific database is the "report server" based on a computer name.
				command="UPDATE preference SET ValueString = '14.3.30.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To14_3_35();
		}

		///<summary></summary>
		private static void To14_3_35() {
			if(FromVersion<new Version("14.3.35.0")) {
				string command="";
				//AppointmentBubblesNoteLength also inserted into version 15.1.13
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('AppointmentBubblesNoteLength','0')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'AppointmentBubblesNoteLength','0')";
					Db.NonQ(command);
				}
				command="UPDATE preference SET ValueString = '14.3.35.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To14_3_37();
		}

		///<summary></summary>
		private static void To14_3_37() {
			if(FromVersion<new Version("14.3.37.0")) {
				string command="";
				//InsPPOsecWriteoffs also inserted into version 15.1
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('InsPPOsecWriteoffs','0')";
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'InsPPOsecWriteoffs','0')";
				}
				Db.NonQ(command);
				command="UPDATE preference SET ValueString = '14.3.37.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To15_1_1();
		}

		private static void To15_1_1() {
			if(FromVersion<new Version("15.1.1.0")) {
				string command;
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS dispsupply";
					Db.NonQ(command);
					command=@"CREATE TABLE dispsupply (
						DispSupplyNum bigint NOT NULL auto_increment PRIMARY KEY,
						SupplyNum bigint NOT NULL,
						ProvNum bigint NOT NULL,
						DateDispensed date NOT NULL DEFAULT '0001-01-01',
						DispQuantity float NOT NULL,
						Note text NOT NULL,
						INDEX(SupplyNum),
						INDEX(ProvNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE dispsupply'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE dispsupply (
						DispSupplyNum number(20) NOT NULL,
						SupplyNum number(20) NOT NULL,
						ProvNum number(20) NOT NULL,
						DateDispensed date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						DispQuantity number(38,8) NOT NULL,
						Note clob,
						CONSTRAINT dispsupply_DispSupplyNum PRIMARY KEY (DispSupplyNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX dispsupply_SupplyNum ON dispsupply (SupplyNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX dispsupply_ProvNum ON dispsupply (ProvNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE equipment ADD ProvNumCheckedOut bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE equipment ADD INDEX (ProvNumCheckedOut)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE equipment ADD ProvNumCheckedOut number(20)";
					Db.NonQ(command);
					command="UPDATE equipment SET ProvNumCheckedOut = 0 WHERE ProvNumCheckedOut IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE equipment MODIFY ProvNumCheckedOut NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX equipment_ProvNumCheckedOut ON equipment (ProvNumCheckedOut)";
					Db.NonQ(command);
				}				
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE equipment ADD DateCheckedOut date NOT NULL DEFAULT '0001-01-01'";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE equipment ADD DateCheckedOut date";
					Db.NonQ(command);
					command="UPDATE equipment SET DateCheckedOut = TO_DATE('0001-01-01','YYYY-MM-DD') WHERE DateCheckedOut IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE equipment MODIFY DateCheckedOut NOT NULL";
					Db.NonQ(command);
				}				
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE equipment ADD DateExpectedBack date NOT NULL DEFAULT '0001-01-01'";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE equipment ADD DateExpectedBack date";
					Db.NonQ(command);
					command="UPDATE equipment SET DateExpectedBack = TO_DATE('0001-01-01','YYYY-MM-DD') WHERE DateExpectedBack IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE equipment MODIFY DateExpectedBack NOT NULL";
					Db.NonQ(command);
				}				
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE equipment ADD DispenseNote text NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE equipment ADD DispenseNote clob";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE supply ADD BarCodeOrID varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE supply ADD BarCodeOrID varchar2(255)";
					Db.NonQ(command);
				}				
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE supply ADD DispDefaultQuant float NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE supply ADD DispDefaultQuant number(38,8)";
					Db.NonQ(command);
					command="UPDATE supply SET DispDefaultQuant = 0 WHERE DispDefaultQuant IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE supply MODIFY DispDefaultQuant NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE supply ADD DispUnitsCount int NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE supply ADD DispUnitsCount number(11)";
					Db.NonQ(command);
					command="UPDATE supply SET DispUnitsCount = 0 WHERE DispUnitsCount IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE supply MODIFY DispUnitsCount NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE supply ADD DispUnitDesc varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE supply ADD DispUnitDesc varchar2(255)";
					Db.NonQ(command);
				}
				//Add index to claimproc table to speed up ClaimProcs.AttachAllOutstandingToPayment() query.  Added because of slowness with AppleTree.
				//Using new multi-column naming pattern.
				try {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="ALTER TABLE claimproc ADD INDEX indexCPNSIPA (ClaimPaymentNum, Status, InsPayAmt)";
						Db.NonQ(command);
					}
					else {//oracle
						command=@"CREATE INDEX claimproc_CPNSIPA ON claimproc (ClaimPaymentNum, Status, InsPayAmt)";
						Db.NonQ(command);
					}
				}
				catch(Exception ex) { }//Only an index. (Exception ex) required to catch thrown exception if index already exists.
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE screen CHANGE Race RaceOld tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE screen RENAME COLUMN Race TO RaceOld";
					Db.NonQ(command);
				}
				//oracle compatible
				command="ALTER TABLE patient DROP COLUMN Race";
				Db.NonQ(command);
				//				
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS wikilisthist";
					Db.NonQ(command);
					command=@"CREATE TABLE wikilisthist (
						WikiListHistNum bigint NOT NULL auto_increment PRIMARY KEY,
						UserNum bigint NOT NULL,
						ListName varchar(255) NOT NULL,
						ListHeaders text NOT NULL,
						ListContent mediumtext NOT NULL,
						DateTimeSaved datetime NOT NULL DEFAULT '0001-01-01 00:00:00',
						INDEX(UserNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE wikilisthist'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE wikilisthist (
						WikiListHistNum number(20) NOT NULL,
						UserNum number(20) NOT NULL,
						ListName varchar2(255),
						ListHeaders varchar2(4000),
						ListContent clob,
						DateTimeSaved date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						CONSTRAINT wikilisthist_WikiListHistNum PRIMARY KEY (WikiListHistNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX wikilisthist_UserNum ON wikilisthist (UserNum)";
					Db.NonQ(command);
				}
				//Insert SMARTDent bridge-----------------------------------------------------------------
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO program (ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
						+") VALUES("
						+"'SMARTDent', "
						+"'SMARTDent from www.raymedical.com', "
						+"'0', "
						+"'"+POut.String(@"C:\Ray\RayView\SMARTDent.exe")+"', "
						+"'', "
						+"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"'"+POut.Long(programNum)+"', "
						+"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
						+"'0')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ProgramNum,ToolBar,ButtonText) "
						+"VALUES ("
						+"'"+POut.Long(programNum)+"', "
						+"'2', "//ToolBarsAvail.ChartModule
						+"'SMARTDent')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO program (ProgramNum,ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
						+") VALUES("
						+"(SELECT MAX(ProgramNum)+1 FROM program),"
						+"'SMARTDent', "
						+"'SMARTDent from www.raymedical.com', "
						+"'0', "
						+"'"+POut.String(@"C:\Ray\RayView\SMARTDent.exe")+"', "
						+"'', "
						+"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
						+"'"+POut.Long(programNum)+"', "
						+"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
						+"'0')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ToolButItemNum,ProgramNum,ToolBar,ButtonText) "
						+"VALUES ("
						+"(SELECT MAX(ToolButItemNum)+1 FROM toolbutitem),"
						+"'"+POut.Long(programNum)+"', "
						+"'2', "//ToolBarsAvail.ChartModule
						+"'SMARTDent')";
					Db.NonQ(command);
				}//end SMARTDent bridge
				try {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						if(!IndexExists("hl7msg","MsgText")) {//only add this index if it has not already been added
							//this could be slow on a very large hl7msg table, however most of the rows in a table will have empty MsgText fields due to old message text being deleted.
							//on a table with 870,000 rows, 145,000 filled with MsgText, with hl7msg.MYD file size of over 9 GB, this query took 10 minutes to run on my local PC
							//on our local test eCW server with 1690 rows, 500 with message text the query took 2 seconds.
							command="ALTER TABLE hl7msg ADD INDEX (MsgText(100))";
							Db.NonQ(command);
						}
					}
					else {//oracle
						//Cannot index a clob column in oracle.  Not likely that an oracle user will also be using HL7.
					}
				}
				catch(Exception ex) { }//Only an index. (Exception ex) required to catch thrown exception
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="SELECT ProgramNum FROM program WHERE ProgName='Xcharge' LIMIT 1";
				}
				else {//oracle doesn't have LIMIT
					command="SELECT * FROM (SELECT ProgramNum FROM program WHERE ProgName='Xcharge') WHERE RowNum<=1";
				}
				long ProgramNum=PIn.Long(Db.GetScalar(command));
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="SELECT PropertyValue FROM programproperty WHERE ProgramNum="+POut.Long(ProgramNum)+" AND PropertyDesc='Password' LIMIT 1";
				}
				else {//oracle doesn't have LIMIT
					command="SELECT * FROM (SELECT PropertyValue FROM programproperty WHERE ProgramNum="+POut.Long(ProgramNum)+" AND PropertyDesc='Password') WHERE RowNum<=1";
				}
				string pw=PIn.String(Db.GetScalar(command));
				command="UPDATE programproperty SET PropertyValue='"+Encrypt(pw)+"' WHERE ProgramNum="+POut.Long(ProgramNum)+" AND PropertyDesc='Password'";//Oracle doesn't have any rescrictions with this query.
				Db.NonQ(command);
				//Web Sched preferences-----------
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('WebSchedService','0')";//Service will be off by default
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'WebSchedService','0')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('WebSchedSubject','Dental Care Reminder for [NameF]')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'WebSchedSubject','Dental Care Reminder for [NameF]')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('WebSchedMessage','You or your family member is due for a regular dental check-up on [DueDate].  Please visit our online scheduler link below or call our office today at [OfficePhone] in order to schedule your appointment.\r\n[URL]')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'WebSchedMessage','You or your family member is due for a regular dental check-up on [DueDate].  Please visit our online scheduler link below or call our office today at [OfficePhone] in order to schedule your appointment.\r\n[URL]')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('WebSchedSubject2','Dental Care Reminder for [NameF]')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'WebSchedSubject2','Dental Care Reminder for [NameF]')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('WebSchedMessage2','You or your family member is due for a regular dental check-up on [DueDate].  Please visit our online scheduler link below or call our office today at [OfficePhone] in order to schedule your appointment.\r\n[URL]')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'WebSchedMessage2','You or your family member is due for a regular dental check-up on [DueDate].  Please visit our online scheduler link below or call our office today at [OfficePhone] in order to schedule your appointment.\r\n[URL]')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('WebSchedSubject3','Dental Care Reminder for [NameF]')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'WebSchedSubject3','Dental Care Reminder for [NameF]')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('WebSchedMessage3','You or your family member is due for a regular dental check-up on [DueDate].  Please visit our online scheduler link below or call our office today at [OfficePhone] in order to schedule your appointment.\r\n[URL]')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'WebSchedMessage3','You or your family member is due for a regular dental check-up on [DueDate].  Please visit our online scheduler link below or call our office today at [OfficePhone] in order to schedule your appointment.\r\n[URL]')";
					Db.NonQ(command);
				}
				//End Web Sched preferences------
				try {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						//This index was added to the primary key of the table in To6_1_1() when the table was created.
						//It is redundant to add an index to the primary key.
						command="ALTER TABLE anesthmedsgiven DROP INDEX AnestheticMedNum";
						Db.NonQ(command);
					}
					else {
						//table not added in oracle and oracle does not allow adding an index to the same column twice like MySQL, even if named differently
					}
				}
				catch(Exception ex) { }//Only an index. (Exception ex) required to catch thrown exception
				try {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						//The procedurelog table does not have the column ChartNum.
						//This index is incorrectly named and is on the PatNum column, which already has an index named indexPatNum.
						command="ALTER TABLE procedurelog DROP INDEX ChartNum";
						Db.NonQ(command);
					}
					else {
						//oracle does not allow adding an index to the same column twice like MySQL, even if named differently
					}
				}
				catch(Exception ex) { }//Only an index. (Exception ex) required to catch thrown exception
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE procedurelog ADD IsDateProsthEst tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE procedurelog ADD IsDateProsthEst number(3)";
					Db.NonQ(command);
					command="UPDATE procedurelog SET IsDateProsthEst = 0 WHERE IsDateProsthEst IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE procedurelog MODIFY IsDateProsthEst NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE referral ADD IsTrustedDirect tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE referral ADD IsTrustedDirect number(3)";
					Db.NonQ(command);
					command="UPDATE referral SET IsTrustedDirect = 0 WHERE IsTrustedDirect IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE referral MODIFY IsTrustedDirect NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('ProblemListIsAlpabetical','0')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'ProblemListIsAlpabetical','0')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="SELECT ClaimFormNum FROM claimform WHERE UniqueID='OD12'";
					DataTable Table=Db.GetTable(command);
					List<long> ListClaimFormNums=new List<long>();
					for(int i=0;i<Table.Rows.Count;i++) {
						ListClaimFormNums.Add(PIn.Long(Table.Rows[i]["ClaimFormNum"].ToString()));
					}
					for(int i=0;i<ListClaimFormNums.Count;i++) {
						command="UPDATE claimformitem SET width='250' WHERE ClaimFormNum='"+POut.Long(ListClaimFormNums[i])+"' AND FieldName='SubscrID'";
						Db.NonQ(command);
					}
				}
				else {//oracle
					command="SELECT ClaimFormNum FROM claimform WHERE UniqueID='OD12'";
					DataTable Table=Db.GetTable(command);
					List<long> ListClaimFormNums=new List<long>();
					for(int i=0;i<Table.Rows.Count;i++) {
						ListClaimFormNums.Add(PIn.Long(Table.Rows[i]["ClaimFormNum"].ToString()));
					}
					for(int i=0;i<ListClaimFormNums.Count;i++) {
						command="UPDATE claimformitem SET width='250' WHERE ClaimFormNum='"+POut.Long(ListClaimFormNums[i])+"' AND FieldName='SubscrID'";
						Db.NonQ(command);
					}
				}
				//Add EmailSend permission to everyone------------------------------------------------------
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="SELECT DISTINCT UserGroupNum FROM grouppermission";
					DataTable table=Db.GetTable(command);
					long groupNum;
					for(int i=0;i<table.Rows.Count;i++) {
						groupNum=PIn.Long(table.Rows[i]["UserGroupNum"].ToString());
						command="INSERT INTO grouppermission (UserGroupNum,PermType) "
							 +"VALUES("+POut.Long(groupNum)+",85)";  //85: EmailSend
						Db.NonQ(command);
					}
				}
				else {//oracle
					command="SELECT DISTINCT UserGroupNum FROM grouppermission";
					DataTable table=Db.GetTable(command);
					long groupNum;
					for(int i=0;i<table.Rows.Count;i++) {
						groupNum=PIn.Long(table.Rows[i]["UserGroupNum"].ToString());
						command="INSERT INTO grouppermission (GroupPermNum,NewerDays,UserGroupNum,PermType) "
							 +"VALUES((SELECT MAX(GroupPermNum)+1 FROM grouppermission),0,"+POut.Long(groupNum)+",85)";  //85: EmailSend
						Db.NonQ(command);
					}
				}
				//Add WebmailSend permission to everyone------------------------------------------------------
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="SELECT DISTINCT UserGroupNum FROM grouppermission";
					DataTable table=Db.GetTable(command);
					long groupNum;
					for(int i=0;i<table.Rows.Count;i++) {
						groupNum=PIn.Long(table.Rows[i]["UserGroupNum"].ToString());
						command="INSERT INTO grouppermission (UserGroupNum,PermType) "
							 +"VALUES("+POut.Long(groupNum)+",86)";  //86: WebmailSend
						Db.NonQ(command);
					}
				}
				else {//oracle
					command="SELECT DISTINCT UserGroupNum FROM grouppermission";
					DataTable table=Db.GetTable(command);
					long groupNum;
					for(int i=0;i<table.Rows.Count;i++) {
						groupNum=PIn.Long(table.Rows[i]["UserGroupNum"].ToString());
						command="INSERT INTO grouppermission (GroupPermNum,NewerDays,UserGroupNum,PermType) "
							 +"VALUES((SELECT MAX(GroupPermNum)+1 FROM grouppermission),0,"+POut.Long(groupNum)+",86)";  //86: WebmailSend
						Db.NonQ(command);
					}
				}
				//START CPT column VersionIDs-------------------------------
				//Add new column to cpt table for keeping track of the years the cpt code existed in.
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE cpt ADD VersionIDs varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE cpt ADD VersionIDs varchar2(255)";
					Db.NonQ(command);
				}
				//Add UserQueryAdmin permission to everyone-------------------------------------------------------
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="SELECT DISTINCT UserGroupNum FROM grouppermission WHERE PermType=19";//Anyone who currently has UserQuery access will initially have UserQueryAdmin access.
					DataTable table=Db.GetTable(command);
					long groupNum;
					for(int i=0;i<table.Rows.Count;i++) {
						groupNum=PIn.Long(table.Rows[i]["UserGroupNum"].ToString());
						command="INSERT INTO grouppermission (UserGroupNum,PermType) "
							 +"VALUES("+POut.Long(groupNum)+",87)";  //87: UserQueryAdmin
						Db.NonQ(command);
					}
				}
				else {//oracle
					command="SELECT DISTINCT UserGroupNum FROM grouppermission WHERE PermType=19";//Anyone who currently has UserQuery access will initially have UserQueryAdmin access.
					DataTable table=Db.GetTable(command);
					long groupNum;
					for(int i=0;i<table.Rows.Count;i++) {
						groupNum=PIn.Long(table.Rows[i]["UserGroupNum"].ToString());
						command="INSERT INTO grouppermission (GroupPermNum,NewerDays,UserGroupNum,PermType) "
							 +"VALUES((SELECT MAX(GroupPermNum)+1 FROM grouppermission),0,"+POut.Long(groupNum)+",87)";  //87: UserQueryAdmin
						Db.NonQ(command);
					}
				}
				//Update every current cpt code to verion 2014.  Importing of 2015 codes was not implemented until this OD version or later.
				//oracle compatible
				command="UPDATE cpt SET VersionIDs='2014'";
				Db.NonQ(command);
				//END CPT column VersionIDs
				command="DELETE FROM grouppermission WHERE usergroupNum NOT IN (SELECT usergroupnum FROM usergroup)";//remove any orphaned grouppermissions; Oracle compatable
				Db.NonQ(command);
				//Add InsPlanChangeAssign permission to everyone------------------------------------------------------
				command="SELECT DISTINCT UserGroupNum FROM grouppermission";
				DataTable tableGroupPerm=Db.GetTable(command);
				if(DataConnection.DBtype==DatabaseType.MySql) {
					long groupNum;
					for(int i=0;i<tableGroupPerm.Rows.Count;i++) {
						groupNum=PIn.Long(tableGroupPerm.Rows[i]["UserGroupNum"].ToString());
						command="INSERT INTO grouppermission (UserGroupNum,PermType) "
							 +"VALUES("+POut.Long(groupNum)+",88)";  //88: InsPlanChangeAssign
						Db.NonQ(command);
					}
				}
				else {//oracle
					long groupNum;
					for(int i=0;i<tableGroupPerm.Rows.Count;i++) {
						groupNum=PIn.Long(tableGroupPerm.Rows[i]["UserGroupNum"].ToString());
						command="INSERT INTO grouppermission (GroupPermNum,NewerDays,UserGroupNum,PermType) "
							 +"VALUES((SELECT MAX(GroupPermNum)+1 FROM grouppermission),0,"+POut.Long(groupNum)+",88)";  //88: InsPlanChangeAssign
						Db.NonQ(command);
					}
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE sheetfield ADD TextAlign tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE sheetfield ADD TextAlign number(3)";
					Db.NonQ(command);
					command="UPDATE sheetfield SET TextAlign = 0 WHERE TextAlign IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE sheetfield MODIFY TextAlign NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE sheetfielddef ADD TextAlign tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE sheetfielddef ADD TextAlign number(3)";
					Db.NonQ(command);
					command="UPDATE sheetfielddef SET TextAlign = 0 WHERE TextAlign IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE sheetfielddef MODIFY TextAlign NOT NULL";
					Db.NonQ(command);
				} 
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE sheetfielddef ADD IsPaymentOption tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE sheetfielddef ADD IsPaymentOption number(3)";
					Db.NonQ(command);
					command="UPDATE sheetfielddef SET IsPaymentOption = 0 WHERE IsPaymentOption IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE sheetfielddef MODIFY IsPaymentOption NOT NULL";
					Db.NonQ(command);
				}
				string cBlack=POut.Int(System.Drawing.Color.Black.ToArgb());
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE sheetfield ADD ItemColor int NOT NULL DEFAULT "+cBlack;
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE sheetfield ADD ItemColor number(11)";
					Db.NonQ(command);
					command="UPDATE sheetfield SET ItemColor = "+cBlack+" WHERE ItemColor IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE sheetfield MODIFY ItemColor NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE sheetfielddef ADD ItemColor int NOT NULL DEFAULT "+cBlack;
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE sheetfielddef ADD ItemColor number(11)";
					Db.NonQ(command);
					command="UPDATE sheetfielddef SET ItemColor = "+cBlack+" WHERE ItemColor IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE sheetfielddef MODIFY ItemColor NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('StatementsUseSheets','0')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'StatementsUseSheets','0')";
					Db.NonQ(command);
				}
				//Insert Office bridge-----------------------------------------------------------------
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO program (ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
						+") VALUES("
						+"'Office', "
						+"'Office', "
						+"'0', "
						+"'"+POut.String("word.exe")+"', "
						+"'', "//leave blank if none
						+"'Verify the Path of file to open.')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"'"+POut.Long(programNum)+"', "
						+"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
						+"'0')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"'"+POut.Long(programNum)+"', "
						+"'Document folder', "
						+"'"+POut.String(@"C:\OpenDentImages\")+"')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"'"+POut.Long(programNum)+"', "
						+"'File extension', "
						+"'.doc')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ProgramNum,ToolBar,ButtonText) "
						+"VALUES ("
						+"'"+POut.Long(programNum)+"', "
						+"'2', "//ToolBarsAvail.ChartModule
						+"'Office')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO program (ProgramNum,ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
						+") VALUES("
						+"(SELECT MAX(ProgramNum)+1 FROM program),"
						+"'Office', "
						+"'Office', "
						+"'0', "
						+"'"+POut.String("word.exe")+"', "
						+"'', "//leave blank if none
						+"'Verify the Path of file to open.')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
						+"'"+POut.Long(programNum)+"', "
						+"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
						+"'0')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
						+"'"+POut.Long(programNum)+"', "
						+"'Document folder', "
						+"'"+POut.String(@"C:\OpenDentImages\")+"')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
						+"'"+POut.Long(programNum)+"', "
						+"'File extension', "
						+"'.doc')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ToolButItemNum,ProgramNum,ToolBar,ButtonText) "
						+"VALUES ("
						+"(SELECT MAX(ToolButItemNum)+1 FROM toolbutitem),"
						+"'"+POut.Long(programNum)+"', "
						+"'2', "//ToolBarsAvail.ChartModule
						+"'Office')";
					Db.NonQ(command);
				}//end Office bridge
				//Inserting PriorityDefNum into task table
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE task ADD PriorityDefNum bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE task ADD INDEX (PriorityDefNum)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE task ADD PriorityDefNum number(20)";
					Db.NonQ(command);
					command="UPDATE task SET PriorityDefNum = 0 WHERE PriorityDefNum IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE task MODIFY PriorityDefNum NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX task_PriorityDefNum ON task (PriorityDefNum)";
					Db.NonQ(command);
				}
				//Inserting new category for task PriorityDefNum defcat in definition table
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO definition (Category,ItemOrder,ItemName,ItemValue,ItemColor) "
						+"VALUES(33,0,'Normal','D',-1)";//Inserting definition with category 33 (TaskPriorities) with default of white (-1)
				}
				else {
					command="INSERT INTO definition (DefNum,Category,ItemOrder,ItemName,ItemValue,ItemColor) "
						+"VALUES((SELECT MAX(DefNum)+1 FROM definition),33,0,'Normal','D',-1)";//33 (TaskPriorities) with default of white (-1)
				}
				long defNum=Db.NonQ(command,true);
				//Updating all tasks with white priority level
				command="UPDATE task SET PriorityDefNum="+POut.Long(defNum);
				Db.NonQ(command,true);
				//Add UserNameManualEntry to preference with a default value of '0' (so that it is disabled by default)
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('UserNameManualEntry','0')";
					Db.NonQ(command);
				}
				else{
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference), 'UserNameManualEntry','0')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE payplan ADD PaySchedule tinyint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE payplan ADD PaySchedule number(3)";
					Db.NonQ(command);
					command="UPDATE payplan SET PaySchedule = 0 WHERE PaySchedule IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE payplan MODIFY PaySchedule NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE payplan ADD NumberOfPayments int NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE payplan ADD NumberOfPayments number(11)";
					Db.NonQ(command);
					command="UPDATE payplan SET NumberOfPayments = 0 WHERE NumberOfPayments IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE payplan MODIFY NumberOfPayments NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE payplan ADD PayAmt double NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE payplan ADD PayAmt number(38,8)";
					Db.NonQ(command);
					command="UPDATE payplan SET PayAmt = 0 WHERE PayAmt IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE payplan MODIFY PayAmt NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE payplan ADD DownPayment double NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE payplan ADD DownPayment number(38,8)";
					Db.NonQ(command);
					command="UPDATE payplan SET DownPayment = 0 WHERE DownPayment IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE payplan MODIFY DownPayment NOT NULL";
					Db.NonQ(command);
				}
				//Add triple column index to procedurelog table for clinic filter enhancement, specifically patient selection.
				try {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						if(!IndexExists("procedurelog","PatNum,ProcStatus,ClinicNum")) {
							command="ALTER TABLE procedurelog ADD INDEX indexPNPSCN (PatNum,ProcStatus,ClinicNum)";
							Db.NonQ(command);
						}
					}
					else {//oracle
						command=@"CREATE INDEX procedurelog_PNPSCN ON procedurelog (PatNum,ProcStatus,ClinicNum)";
						Db.NonQ(command);
					}
				}
				catch(Exception ex) { }//Only an index. (Exception ex) required to catch thrown exception
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE commlog ADD IsWebSched tinyint NOT NULL";
					Db.NonQ(command);
					command="UPDATE commlog SET IsWebSched = 0";//Set all commlogs to not be web sched
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE commlog ADD IsWebSched number(3)";
					Db.NonQ(command);
					command="UPDATE commlog SET IsWebSched = 0 WHERE IsWebSched IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE commlog MODIFY IsWebSched NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE securitylog ADD LogSource tinyint NOT NULL";
					Db.NonQ(command);
					command="UPDATE securitylog SET LogSource = 0";//Set all securitylogs to none
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE securitylog ADD LogSource number(3)";
					Db.NonQ(command);
					command="UPDATE securitylog SET LogSource = 0 WHERE LogSource IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE securitylog MODIFY LogSource NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('SignalLastClearedDate','0001-01-01')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'SignalLastClearedDate',TO_DATE('0001-01-01','YYYY-MM-DD'))";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE computerpref ADD ClinicNum bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE computerpref ADD INDEX (ClinicNum)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE computerpref ADD ClinicNum number(20)";
					Db.NonQ(command);
					command="UPDATE computerpref SET ClinicNum = 0 WHERE ClinicNum IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE computerpref MODIFY ClinicNum NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX computerpref_ClinicNum ON computerpref (ClinicNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE computerpref ADD ApptViewNum bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE computerpref ADD INDEX (ApptViewNum)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE computerpref ADD ApptViewNum number(20)";
					Db.NonQ(command);
					command="UPDATE computerpref SET ApptViewNum = 0 WHERE ApptViewNum IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE computerpref MODIFY ApptViewNum NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX computerpref_ApptViewNum ON computerpref (ApptViewNum)";
					Db.NonQ(command);
				}
				//convert RecentApptView (index in list of views for the recent view) to ApptViewNum (FK to the actual view)
				command="SELECT * FROM computerpref";
				DataTable tableCompPrefs=Db.GetTable(command);
				command="SELECT * FROM apptview ORDER BY ItemOrder";
				DataTable tableApptViews=Db.GetTable(command);
				for(int i=0;i<tableCompPrefs.Rows.Count;i++) {
					try {
						long compPrefNum=PIn.Long(tableCompPrefs.Rows[i]["ComputerPrefNum"].ToString());
						//The computer preference 'RecentApptView' column is stored as a byte that represents the selected index of the apptview within the appt view combo box.  It is 1 based.
						int apptViewIndex=PIn.Int(tableCompPrefs.Rows[i]["RecentApptView"].ToString());
						long apptViewNum=0;//Default to 'none' view.
						if(apptViewIndex > 0 && apptViewIndex<=tableApptViews.Rows.Count) {
							apptViewIndex--;//Subtract 1 from apptViewIndex because RecentApptView is 1 based and we need to treat it 0 based.  If it is already zero, let it go through.
							apptViewNum=PIn.Long(tableApptViews.Rows[apptViewIndex]["ApptViewNum"].ToString());//Get the apptview based on the index of the old RecentApptView computer preference.
						}
						command="UPDATE computerpref SET ApptViewNum="+POut.Long(apptViewNum)+" WHERE ComputerPrefNum="+POut.Long(compPrefNum);
						Db.NonQ(command);
					}
					catch(Exception) {
						//Don't fail the upgrade for failing to set a default appt view for this computer.
						//The worst that could happen is first user to log into this computer will see a default "none" view.
						//Keep trying to set defaults for subsequent computers.
					}
				}
				//after converting data in RecentApptView to ApptViewNum, drop the RecentApptView column
				command="ALTER TABLE computerpref DROP COLUMN RecentApptView";
				Db.NonQ(command);
				#region Duplicate Views for Clinics
				//Any apptviews with a ClinicNum set will need to have an apptviewitem for each clinic associated to that clinic.
				//For any views that contained an operatory and were "assigned to a clinic", they will no longer have access to that operatory.  This is expected behavior with our new clinic filtering.
				//Once all clinic apptviews are moved out of the 'Headquarters' view, we'll need to go back through the 'Headquarters' apptview list and fix the ItemOrders.
				if(DataConnection.DBtype==DatabaseType.MySql) {
					//Start by grabbing all apptviews with a clinic set.
					command="SELECT apptview.ApptViewNum,apptview.ClinicNum FROM apptview WHERE apptview.ClinicNum > 0";
					DataTable tableApptViewClinics=Db.GetTable(command);
					Dictionary<long,DataTable> dictOpsForClinics=new Dictionary<long,DataTable>();//Key = ClinicNum, Value= DataTable of OperatoryNums for the specified clinic.
					for(int i=0;i<tableApptViewClinics.Rows.Count;i++) {
						DataTable tableClinicOpNums=null;
						long clinicNum=PIn.Long(tableApptViewClinics.Rows[i]["ClinicNum"].ToString());
						//Now that we have a list of apptviews that will be moved into their own sub lists, we need to clear out any current operatories and replace them with ALL ops for that clinic.
						//Check to see if we've already gone to the database to retrieve all operatories for the clinic for this apptview
						if(dictOpsForClinics.ContainsKey(clinicNum)) {
							tableClinicOpNums=dictOpsForClinics[clinicNum];
						}
						else {
							command="SELECT OperatoryNum FROM operatory WHERE operatory.ClinicNum="+tableApptViewClinics.Rows[i]["ClinicNum"].ToString();
							tableClinicOpNums=Db.GetTable(command);
							dictOpsForClinics.Add(clinicNum,tableClinicOpNums);
						}
						if(tableClinicOpNums.Rows.Count==0) {
							//There is no such thing as an apptview with no operatory selected.  If there are no ops for this particular clinic, we must remove the clinic filter so that they can manually set it later.
							command="UPDATE apptview SET apptview.ClinicNum=0 WHERE apptview.ApptViewNum="+tableApptViewClinics.Rows[i]["ApptViewNum"].ToString();
							Db.NonQ(command);
							continue;
						}
						//Remove all current apptviewitems that are for 'ops'.  We have to remove them all because they might be for ops that are not associated to the apptview's clinic.
						command="DELETE FROM apptviewitem "
							+"WHERE apptviewitem.ApptViewNum="+tableApptViewClinics.Rows[i]["ApptViewNum"].ToString()+" "
							+"AND apptviewitem.OpNum > 0";
						Db.NonQ(command);
						//Add an 'op' apptviewitem for every single operatory that is associated to the apptview's clinic.
						command="";
						for(int j=0;j<tableClinicOpNums.Rows.Count;j++) {
							command+="INSERT INTO apptviewitem (ApptViewNum,OpNum) VALUES ("+tableApptViewClinics.Rows[i]["ApptViewNum"].ToString()+","+tableClinicOpNums.Rows[j]["OperatoryNum"].ToString()+");\r\n";
						}
						Db.NonQ(command);//Bulk inserts are not Oracle compatible with this current syntax.
					}
					//Update item orders for all apptviews split up into sub groups by clinic.
					command="SELECT ApptViewNum,ClinicNum FROM apptview ORDER BY ClinicNum,ItemOrder";
					DataTable tableClinicViewsOrdered=Db.GetTable(command);
					long clinicNumPrev=0;
					int itemOrderCur=0;
					for(int i=0;i<tableClinicViewsOrdered.Rows.Count;i++) {
						long clinicNumCur=PIn.Long(tableClinicViewsOrdered.Rows[i]["ClinicNum"].ToString());
						long apptViewNumCur=PIn.Long(tableClinicViewsOrdered.Rows[i]["ApptViewNum"].ToString());
						if(i==0 || clinicNumCur!=clinicNumPrev) {
							itemOrderCur=0;
							clinicNumPrev=clinicNumCur;
						}
						else if(clinicNumCur==clinicNumPrev) {
							itemOrderCur++;
						}
						command="UPDATE apptview SET ItemOrder="+POut.Int(itemOrderCur)+" WHERE ApptViewNum="+POut.Long(apptViewNumCur);
						Db.NonQ(command);
					}
				}
				else {//oracle
					//we won't try to duplicate the views for oracle, the user will have to create the views for each clinic manually
				}
				#endregion
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('TempFolderDateFirstCleaned',CURDATE())";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'TempFolderDateFirstCleaned',SYSDATE)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS userodapptview";
					Db.NonQ(command);
					command=@"CREATE TABLE userodapptview (
						UserodApptViewNum bigint NOT NULL auto_increment PRIMARY KEY,
						UserNum bigint NOT NULL,
						ClinicNum bigint NOT NULL,
						ApptViewNum bigint NOT NULL,
						INDEX(UserNum),
						INDEX(ClinicNum),
						INDEX(ApptViewNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE userodapptview'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE userodapptview (
						UserodApptViewNum number(20) NOT NULL,
						UserNum number(20) NOT NULL,
						ClinicNum number(20) NOT NULL,
						ApptViewNum number(20) NOT NULL,
						CONSTRAINT userodapptview_UserodApptViewN PRIMARY KEY (UserodApptViewNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX userodapptview_UserNum ON userodapptview (UserNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX userodapptview_ClinicNum ON userodapptview (ClinicNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX userodapptview_ApptViewNum ON userodapptview (ApptViewNum)";
					Db.NonQ(command);
				}
				command="UPDATE preference SET ValueString = '15.1.1.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To15_1_3();
		}

		///<summary></summary>
		private static void To15_1_3() {
			if(FromVersion<new Version("15.1.3.0")) {
				string command="";
				//We dropped RecentApptView in 15.1.1 but should not have because it is a column that is used by older versions prior to calling the 'update file copier' code which will cause UEs to occur.
				//Bringing the column back with deprecation comments for the database documentation in our manual.
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE computerpref ADD RecentApptView tinyint unsigned NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE computerpref ADD RecentApptView number(3)";
					Db.NonQ(command);
					command="UPDATE computerpref SET RecentApptView = 0 WHERE RecentApptView IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE computerpref MODIFY RecentApptView NOT NULL";
					Db.NonQ(command);
				}
				command="UPDATE preference SET ValueString = '15.1.3.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To15_1_13();
		}

		///<summary>Oracle compatible: 02/25/2015</summary>
		private static void To15_1_13() {
			if(FromVersion<new Version("15.1.13.0")) {
				string command="";
				//AppointmentBubblesNoteLength also inserted into version 14.3.35
				//This code was copied from the pattern used to insert FamPhiAccess pref in To14_3_9
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="SELECT COUNT(*) FROM preference WHERE PrefName='AppointmentBubblesNoteLength' LIMIT 1";
				}
				else {//oracle doesn't have LIMIT
					command="SELECT COUNT(*) FROM (SELECT ValueString FROM preference WHERE PrefName='AppointmentBubblesNoteLength') WHERE RowNum<=1";
				}
				long hasAppointmentBubblesNoteLength=PIn.Long(Db.GetCount(command));
				if(hasAppointmentBubblesNoteLength==0) {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="INSERT INTO preference(PrefName,ValueString) VALUES('AppointmentBubblesNoteLength','0')";
						Db.NonQ(command);
					}
					else {//oracle
						command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'AppointmentBubblesNoteLength','0')";
						Db.NonQ(command);
					}
				}
				try {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						if(!IndexExists("sheetfield","FieldType")) {
							command="ALTER TABLE sheetfield ADD INDEX (FieldType)";
							Db.NonQ(command);
						}
					}
					else {//oracle
						command=@"CREATE INDEX sheetfield_FieldType ON sheetfield (FieldType)";
						Db.NonQ(command);
					}
				}
				catch(Exception ex) { }//Only an index. (Exception ex) required to catch thrown exception
				command="UPDATE preference SET ValueString = '15.1.13.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To15_1_14();
		}

		///<summary>Oracle compatible: 03/02/2015</summary>
		private static void To15_1_14() {
			if(FromVersion<new Version("15.1.14.0")) {
				string command="";
				try {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="ALTER TABLE adjustment ADD INDEX indexProvNum (ProvNum)";
						Db.NonQ(command);
					}
					else {//oracle
						command=@"CREATE INDEX adjustment_ProvNum ON adjustment (ProvNum)";
						Db.NonQ(command);
					}
				}
				catch(Exception ex) { }//Only an index. (Exception ex) required to catch thrown exception if index already exists.
				try {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="ALTER TABLE claimproc ADD INDEX indexPNPD (ProvNum,ProcDate)";
						Db.NonQ(command);
					}
					else {//oracle
						command=@"CREATE INDEX claimproc_PNPD ON claimproc (ProvNum,ProcDate)";
						Db.NonQ(command);
					}
				}
				catch(Exception ex) { }//Only an index. (Exception ex) required to catch thrown exception if index already exists.
				try {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="ALTER TABLE claimproc ADD INDEX indexPNDCP (ProvNum,DateCP)";
						Db.NonQ(command);
					}
					else {//oracle
						command=@"CREATE INDEX claimproc_PNDCP ON claimproc (ProvNum,DateCP)";
						Db.NonQ(command);
					}
				}
				catch(Exception ex) { }//Only an index. (Exception ex) required to catch thrown exception if index already exists.
				try {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="ALTER TABLE procedurelog ADD INDEX indexPNPD (ProvNum,ProcDate)";
						Db.NonQ(command);
					}
					else {//oracle
						command=@"CREATE INDEX procedurelog_PNPD ON procedurelog (ProvNum,ProcDate)";
						Db.NonQ(command);
					}
				}
				catch(Exception ex) { }//Only an index. (Exception ex) required to catch thrown exception if index already exists.
				try {
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="ALTER TABLE tasklist ADD INDEX indexParent (Parent)";
						Db.NonQ(command);
					}
					else {//oracle
						command=@"CREATE INDEX tasklist_Parent ON tasklist (Parent)";
						Db.NonQ(command);
					}
				}
				catch(Exception ex) { }//Only an index. (Exception ex) required to catch thrown exception if index already exists.
				command="UPDATE preference SET ValueString = '15.1.14.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To15_1_16();
		}

		///<summary>Oracle compatible: 03/18/2015</summary>
		private static void To15_1_16() {
			if(FromVersion<new Version("15.1.16.0")) {
				string command="";
				command="SELECT * FROM preference WHERE PrefName='InsPPOsecWriteoffs'";
				DataTable tableCur=Db.GetTable(command);
				if(tableCur.Rows.Count==0) {//The InsPPOsecWriteoffs pref does not already exist
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="INSERT INTO preference(PrefName,ValueString) VALUES('InsPPOsecWriteoffs','0')";
					}
					else {//oracle
						command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'InsPPOsecWriteoffs','0')";
					}
					Db.NonQ(command);
				}
				command="UPDATE preference SET ValueString = '15.1.16.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To15_1_17();
		}

		///<summary>Oracle compatible: 03/24/2015</summary>
		private static void To15_1_17() {
			if(FromVersion<new Version("15.1.17.0")) {
				string command="";
				//Insert Triana bridge-----------------------------------------------------------------
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO program (ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"'Triana', "
				    +"'Triana from genorayamerica.com', "
				    +"'0', "
				    +"'"+POut.String(@"Triana.exe")+"', "
				    +"'', "//leave blank if none
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"'"+POut.Long(programNum)+"', "
				    +"'Import.ini path', "
				    +"'"+POut.String(@"C:\Program Files\Triana\Import.ini")+"')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"'"+POut.Long(programNum)+"', "
				    +"'2', "//ToolBarsAvail.ChartModule
				    +"'Triana')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO program (ProgramNum,ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"(SELECT MAX(ProgramNum)+1 FROM program),"
				    +"'Triana', "
				    +"'Triana from genorayamerica.com', "
				    +"'0', "
				    +"'"+POut.String(@"Triana.exe")+"', "
				    +"'', "//leave blank if none
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'Import.ini path', "
				    +"'"+POut.String(@"C:\Program Files\Triana\Import.ini")+"')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ToolButItemNum,ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"(SELECT MAX(ToolButItemNum)+1 FROM toolbutitem),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'2', "//ToolBarsAvail.ChartModule
				    +"'Triana')";
					Db.NonQ(command);
				}//end Triana bridge
				//Insert VixWinNumbered bridge-----------------------------------------------------------------
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO program (ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"'VixWinNumbered', "
				    +"'VixWin(numbered) from www.gendexxray.com', "
				    +"'0', "
				    +"'"+POut.String(@"C:\VixWin\VixWin.exe")+"', "
				    +"'', "//leave blank if none
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"'"+POut.Long(programNum)+"', "
				    +"'Image Path', "
				    +"'"+POut.String(@"X:\VXImages\")+"')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"'"+POut.Long(programNum)+"', "
				    +"'2', "//ToolBarsAvail.ChartModule
				    +"'VixWinNumbered')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO program (ProgramNum,ProgName,ProgDesc,Enabled,Path,CommandLine,Note"
				    +") VALUES("
				    +"(SELECT MAX(ProgramNum)+1 FROM program),"
				    +"'VixWinNumbered', "
				    +"'VixWin(numbered) from www.gendexxray.com', "
				    +"'0', "
				    +"'"+POut.String(@"C:\VixWin\VixWin.exe")+"', "
				    +"'', "//leave blank if none
				    +"'')";
					long programNum=Db.NonQ(command,true);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'Enter 0 to use PatientNum, or 1 to use ChartNum', "
				    +"'0')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
				    +") VALUES("
				    +"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'Image Path', "
				    +"'"+POut.String(@"X:\VXImages\")+"')";
					Db.NonQ(command);
					command="INSERT INTO toolbutitem (ToolButItemNum,ProgramNum,ToolBar,ButtonText) "
				    +"VALUES ("
				    +"(SELECT MAX(ToolButItemNum)+1 FROM toolbutitem),"
				    +"'"+POut.Long(programNum)+"', "
				    +"'2', "//ToolBarsAvail.ChartModule
				    +"'VixWinNumbered')";
					Db.NonQ(command);
				}//end VixWinNumbered bridge
				command="UPDATE preference SET ValueString = '15.1.17.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To15_1_20();
		}

		///<summary></summary>
		private static void To15_1_20() {
			if(FromVersion<new Version("15.1.20.0")) {
				string command="";
				command="SELECT DefNum FROM definition WHERE Category='27' AND ItemValue='RECALL' ORDER BY ItemOrder";//commlog type for recall
				DataTable tableRecallTypes=Db.GetTable(command);
				DataTable tableCommlogs=new DataTable();//Will have no rows when there are no recall types set up.
				if(tableRecallTypes.Rows.Count>0) {
					command="SELECT PatNum,CommDateTime,Note FROM commlog where CommType='"+tableRecallTypes.Rows[0]["DefNum"].ToString()+"'";
					tableCommlogs=Db.GetTable(command);
				}
				for(int i=0;i<tableCommlogs.Rows.Count;i++) {//Make ehrmeasureevent for users who have been sending reminders from FormRecallList
					DateTime dateTimeComm=PIn.DateT(tableCommlogs.Rows[i]["CommDateTime"].ToString());
					long patNum=PIn.Long(tableCommlogs.Rows[i]["PatNum"].ToString());
					string note=PIn.String(tableCommlogs.Rows[i]["Note"].ToString());
					if(DataConnection.DBtype==DatabaseType.MySql) {
						command="INSERT INTO ehrmeasureevent (DateTEvent,EventType,PatNum,MoreInfo) "
							+"VALUES( "
							+POut.DateT(dateTimeComm,true)+","//DateTEvent
							+"5,"//EventType ReminderSent
							+POut.Long(patNum)+","//PatNum
							+"'"+POut.String(note)+"'); ";//MoreInfo
					}
					else {
						//EHR is not Oracle compatable, so we don't worry about Oracle here.
					}
					Db.NonQ(command);
				}
				command="UPDATE preference SET ValueString = '15.1.20.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To15_1_22();
		}

		///<summary></summary>
		private static void To15_1_22() {
			if(FromVersion<new Version("15.1.22.0")) {
				string command="";
				//The next command is MySQL and Oracle compatible.  Used LTRIM() to remove the leading space after "NewCrop" is removed from description.
				command="UPDATE program SET ProgName='eRx', ProgDesc=LTRIM(REPLACE(ProgDesc,'NewCrop','')), Note=REPLACE(Note,'NewCrop','eRx') WHERE ProgName='NewCrop'";
				Db.NonQ(command);
				command="UPDATE preference SET ValueString = '15.1.22.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			To15_2_1();
		}

		///<summary></summary>
		private static void To15_2_1() {
			if(FromVersion<new Version("15.2.1.0")) {
				string command="";
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('WaitingRoomFilterByView','0')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'WaitingRoomFilterByView','0')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('BrokenApptCommLogWithProcedure','0')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'BrokenApptCommLogWithProcedure','0')";
					Db.NonQ(command);
				}
				command="SELECT CodeNum FROM procedurecode WHERE ProcCode='D9986'";//Broken appointment procedure for CDT 2015.  Oracle compatible.
				long codeNum=PIn.Long(Db.GetScalar(command));
				if(codeNum!=0) {//If foreign, the customer will probably not have D9986.  This is for USA only.
					if(DataConnection.DBtype==DatabaseType.MySql) {
						//Make a 'broken appointment' procedure for every adjustment in the database.
						command="SELECT ValueString FROM preference WHERE PrefName='BrokenAppointmentAdjustmentType'";
						long brokenAdjType=PIn.Long(Db.GetScalar(command));
						command="SELECT * FROM adjustment WHERE AdjType="+brokenAdjType;
						DataTable tableBrokenAdjustments=Db.GetTable(command);
						for(int i=0;i<tableBrokenAdjustments.Rows.Count;i++) {
							DateTime dateAdj=PIn.Date(tableBrokenAdjustments.Rows[i]["AdjDate"].ToString());
							command="INSERT INTO procedurelog ("
								+"PatNum,DateTP,ProcDate,DateEntryC,ProcFee,ProcStatus,ProvNum,ClinicNum,CodeNum,UnitQty,CodeMod1,CodeMod2,CodeMod3,CodeMod4,RevCode) VALUES("
								+tableBrokenAdjustments.Rows[i]["PatNum"].ToString()+","
								+POut.Date(dateAdj,true)+","//DateTP
								+POut.Date(dateAdj,true)+","//ProcDate
								+POut.Date(dateAdj,true)+","//DateEntryC
								+"0,"//ProcFee
								+"2,"//ProcStatus complete
								+tableBrokenAdjustments.Rows[i]["ProvNum"].ToString()+","
								+tableBrokenAdjustments.Rows[i]["ClinicNum"].ToString()+","
								+codeNum.ToString()+","//Code D9986
								+"1,"//UnitQty
								+"'','','','','')";//CodeMod1,CodeMod2,CodeMod3,CodeMod4,RevCode
							Db.NonQ(command);
						}
						//Remove all adjustments that do not have an amount.
						//Leaving adjustments with amounts is intended because the procedures we created will have a 0 fee which will not affect reports.
						command="DELETE FROM adjustment WHERE AdjType="+brokenAdjType+" AND AdjAmt=0";
						Db.NonQ(command);
					}
					else {//Oracle
						//Not going to worry about Oracle automation for inserting procedures.
						//We would have to spell out every single column that does not allow null values and no one uses Oracle. -jsalmon
					}
					command="UPDATE procedurecode SET NoBillIns=1 WHERE CodeNum="+codeNum;//oracle compatible
					Db.NonQ(command);
				}
				#region medlab tables for LabCorp HL7 interface
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS medlab";
					Db.NonQ(command);
					command=@"CREATE TABLE medlab (
						MedLabNum bigint NOT NULL auto_increment PRIMARY KEY,
						SendingApp varchar(255) NOT NULL,
						SendingFacility varchar(255) NOT NULL,
						PatNum bigint NOT NULL,
						ProvNum bigint NOT NULL,
						PatIDLab varchar(255) NOT NULL,
						PatIDAlt varchar(255) NOT NULL,
						PatAge varchar(255) NOT NULL,
						PatAccountNum varchar(255) NOT NULL,
						PatFasting tinyint NOT NULL,
						SpecimenID varchar(255) NOT NULL,
						SpecimenIDFiller varchar(255) NOT NULL,
						ObsTestID varchar(255) NOT NULL,
						ObsTestDescript varchar(255) NOT NULL,
						ObsTestLoinc varchar(255) NOT NULL,
						ObsTestLoincText varchar(255) NOT NULL,
						DateTimeCollected datetime NOT NULL DEFAULT '0001-01-01 00:00:00',
						TotalVolume varchar(255) NOT NULL,
						ActionCode varchar(255) NOT NULL,
						ClinicalInfo varchar(255) NOT NULL,
						DateTimeEntered datetime NOT NULL DEFAULT '0001-01-01 00:00:00',
						OrderingProvNPI varchar(255) NOT NULL,
						OrderingProvLocalID varchar(255) NOT NULL,
						OrderingProvLName varchar(255) NOT NULL,
						OrderingProvFName varchar(255) NOT NULL,
						SpecimenIDAlt varchar(255) NOT NULL,
						DateTimeReported datetime NOT NULL DEFAULT '0001-01-01 00:00:00',
						ResultStatus varchar(255) NOT NULL,
						ParentObsID varchar(255) NOT NULL,
						ParentObsTestID varchar(255) NOT NULL,
						NotePat text NOT NULL,
						NoteLab text NOT NULL,
						FileName varchar(255) NOT NULL,
						OriginalPIDSegment text NOT NULL,
						INDEX(PatNum),
						INDEX(ProvNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE medlab'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE medlab (
						MedLabNum number(20) NOT NULL,
						SendingApp varchar2(255),
						SendingFacility varchar2(255),
						PatNum number(20) NOT NULL,
						ProvNum number(20) NOT NULL,
						PatIDLab varchar2(255),
						PatIDAlt varchar2(255),
						PatAge varchar2(255),
						PatAccountNum varchar2(255),
						PatFasting number(3) NOT NULL,
						SpecimenID varchar2(255),
						SpecimenIDFiller varchar2(255),
						ObsTestID varchar2(255),
						ObsTestDescript varchar2(255),
						ObsTestLoinc varchar2(255),
						ObsTestLoincText varchar2(255),
						DateTimeCollected date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						TotalVolume varchar2(255),
						ActionCode varchar2(255),
						ClinicalInfo varchar2(255),
						DateTimeEntered date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						OrderingProvNPI varchar2(255),
						OrderingProvLocalID varchar2(255),
						OrderingProvLName varchar2(255),
						OrderingProvFName varchar2(255),
						SpecimenIDAlt varchar2(255),
						DateTimeReported date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						ResultStatus varchar2(255),
						ParentObsID varchar2(255),
						ParentObsTestID varchar2(255),
						NotePat clob,
						NoteLab clob,
						FileName varchar2(255),
						OriginalPIDSegment varchar2(4000),
						CONSTRAINT medlab_MedLabNum PRIMARY KEY (MedLabNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX medlab_PatNum ON medlab (PatNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX medlab_ProvNum ON medlab (ProvNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS medlabresult";
					Db.NonQ(command);
					command=@"CREATE TABLE medlabresult (
						MedLabResultNum bigint NOT NULL auto_increment PRIMARY KEY,
						MedLabNum bigint NOT NULL,
						ObsID varchar(255) NOT NULL,
						ObsText varchar(255) NOT NULL,
						ObsLoinc varchar(255) NOT NULL,
						ObsLoincText varchar(255) NOT NULL,
						ObsIDSub varchar(255) NOT NULL,
						ObsValue text NOT NULL,
						ObsSubType varchar(255) NOT NULL,
						ObsUnits varchar(255) NOT NULL,
						ReferenceRange varchar(255) NOT NULL,
						AbnormalFlag varchar(255) NOT NULL,
						ResultStatus varchar(255) NOT NULL,
						DateTimeObs datetime NOT NULL DEFAULT '0001-01-01 00:00:00',
						FacilityID varchar(255) NOT NULL,
						DocNum bigint NOT NULL,
						Note text NOT NULL,
						INDEX(MedLabNum),
						INDEX(DocNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE medlabresult'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE medlabresult (
						MedLabResultNum number(20) NOT NULL,
						MedLabNum number(20) NOT NULL,
						ObsID varchar2(255),
						ObsText varchar2(255),
						ObsLoinc varchar2(255),
						ObsLoincText varchar2(255),
						ObsIDSub varchar2(255),
						ObsValue clob,
						ObsSubType varchar2(255),
						ObsUnits varchar2(255),
						ReferenceRange varchar2(255),
						AbnormalFlag varchar2(255),
						ResultStatus varchar2(255),
						DateTimeObs date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						FacilityID varchar2(255),
						DocNum number(20) NOT NULL,
						Note clob,
						CONSTRAINT medlabresult_MedLabResultNum PRIMARY KEY (MedLabResultNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX medlabresult_MedLabNum ON medlabresult (MedLabNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX medlabresult_DocNum ON medlabresult (DocNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS medlabspecimen";
					Db.NonQ(command);
					command=@"CREATE TABLE medlabspecimen (
						MedLabSpecimenNum bigint NOT NULL auto_increment PRIMARY KEY,
						MedLabNum bigint NOT NULL,
						SpecimenID varchar(255) NOT NULL,
						SpecimenDescript varchar(255) NOT NULL,
						DateTimeCollected datetime NOT NULL DEFAULT '0001-01-01 00:00:00',
						INDEX(MedLabNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE medlabspecimen'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE medlabspecimen (
						MedLabSpecimenNum number(20) NOT NULL,
						MedLabNum number(20) NOT NULL,
						SpecimenID varchar2(255),
						SpecimenDescript varchar2(255),
						DateTimeCollected date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						CONSTRAINT medlabspecimen_MedLabSpecimenN PRIMARY KEY (MedLabSpecimenNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX medlabspecimen_MedLabNum ON medlabspecimen (MedLabNum)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS medlabfacility";
					Db.NonQ(command);
					command=@"CREATE TABLE medlabfacility (
						MedLabFacilityNum bigint NOT NULL auto_increment PRIMARY KEY,
						FacilityName varchar(255) NOT NULL,
						Address varchar(255) NOT NULL,
						City varchar(255) NOT NULL,
						State varchar(255) NOT NULL,
						Zip varchar(255) NOT NULL,
						Phone varchar(255) NOT NULL,
						DirectorTitle varchar(255) NOT NULL,
						DirectorLName varchar(255) NOT NULL,
						DirectorFName varchar(255) NOT NULL
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE medlabfacility'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE medlabfacility (
						MedLabFacilityNum number(20) NOT NULL,
						FacilityName varchar2(255),
						Address varchar2(255),
						City varchar2(255),
						State varchar2(255),
						Zip varchar2(255),
						Phone varchar2(255),
						DirectorTitle varchar2(255),
						DirectorLName varchar2(255),
						DirectorFName varchar2(255),
						CONSTRAINT medlabfacility_MedLabFacilityN PRIMARY KEY (MedLabFacilityNum)
						)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS medlabfacattach";
					Db.NonQ(command);
					command=@"CREATE TABLE medlabfacattach (
						MedLabFacAttachNum bigint NOT NULL auto_increment PRIMARY KEY,
						MedLabNum bigint NOT NULL,
						MedLabResultNum bigint NOT NULL,
						MedLabFacilityNum bigint NOT NULL,
						INDEX(MedLabNum),
						INDEX(MedLabResultNum),
						INDEX(MedLabFacilityNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE medlabfacattach'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE medlabfacattach (
						MedLabFacAttachNum number(20) NOT NULL,
						MedLabNum number(20) NOT NULL,
						MedLabResultNum number(20) NOT NULL,
						MedLabFacilityNum number(20) NOT NULL,
						CONSTRAINT medlabfacattach_MedLabFacAttac PRIMARY KEY (MedLabFacAttachNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX medlabfacattach_MedLabNum ON medlabfacattach (MedLabNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX medlabfacattach_MedLabResultNu ON medlabfacattach (MedLabResultNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX medlabfacattach_MedLabFacility ON medlabfacattach (MedLabFacilityNum)";
					Db.NonQ(command);
				}
				#endregion medlab tables for LabCorp HL7 interface
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE claim ADD OrthoTotalM tinyint unsigned NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE claim ADD OrthoTotalM number(3)";
					Db.NonQ(command);
					command="UPDATE claim SET OrthoTotalM = 0 WHERE OrthoTotalM IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE claim MODIFY OrthoTotalM NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('CentralManagerSecurityLock','0')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'CentralManagerSecurityLock','0')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE userod ADD UserNumCEMT bigint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE userod ADD UserNumCEMT number(20)";
					Db.NonQ(command);
					command="UPDATE userod SET UserNumCEMT = 0 WHERE UserNumCEMT IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE userod MODIFY UserNumCEMT NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE usergroup ADD UserGroupNumCEMT bigint NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE usergroup ADD UserGroupNumCEMT number(20)";
					Db.NonQ(command);
					command="UPDATE usergroup SET UserGroupNumCEMT = 0 WHERE UserGroupNumCEMT IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE usergroup MODIFY UserGroupNumCEMT NOT NULL";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE clinic ADD DefaultProv bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE clinic ADD INDEX (DefaultProv)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE clinic ADD DefaultProv number(20)";
					Db.NonQ(command);
					command="UPDATE clinic SET DefaultProv = 0 WHERE DefaultProv IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE clinic MODIFY DefaultProv NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX clinic_DefaultProv ON clinic (DefaultProv)";
					Db.NonQ(command);
				}
				#region ConnectionGroup tables for Central Enterprise Management Tool
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS connectiongroup";
					Db.NonQ(command);
					command=@"CREATE TABLE connectiongroup (
						ConnectionGroupNum bigint NOT NULL auto_increment PRIMARY KEY,
						Description varchar(255) NOT NULL
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE connectiongroup'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE connectiongroup (
						ConnectionGroupNum number(20) NOT NULL,
						Description varchar2(255),
						CONSTRAINT connectiongroup_ConnGroupNum PRIMARY KEY (ConnectionGroupNum)
						)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS conngroupattach";
					Db.NonQ(command);
					command=@"CREATE TABLE conngroupattach (
						ConnGroupAttachNum bigint NOT NULL auto_increment PRIMARY KEY,
						ConnectionGroupNum bigint NOT NULL,
						CentralConnectionNum bigint NOT NULL,
						INDEX(ConnectionGroupNum),
						INDEX(CentralConnectionNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE conngroupattach'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE conngroupattach (
						ConnGroupAttachNum number(20) NOT NULL,
						ConnectionGroupNum number(20) NOT NULL,
						CentralConnectionNum number(20) NOT NULL,
						CONSTRAINT conngroupattach_ConnGroupAttac PRIMARY KEY (ConnGroupAttachNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX conngroupattach_CentralConnect ON conngroupattach (CentralConnectionNum)";
					Db.NonQ(command);
				}
				#endregion
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE hl7def ADD LabResultImageCat bigint NOT NULL";
					Db.NonQ(command);
					command="ALTER TABLE hl7def ADD INDEX (LabResultImageCat)";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE hl7def ADD LabResultImageCat number(20)";
					Db.NonQ(command);
					command="UPDATE hl7def SET LabResultImageCat = 0 WHERE LabResultImageCat IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE hl7def MODIFY LabResultImageCat NOT NULL";
					Db.NonQ(command);
					command=@"CREATE INDEX hl7def_LabResultImageCat ON hl7def (LabResultImageCat)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE hl7def ADD SftpUsername varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE hl7def ADD SftpUsername varchar2(255)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE hl7def ADD SftpPassword varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE hl7def ADD SftpPassword varchar2(255)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE hl7def ADD SftpInSocket varchar(255) NOT NULL";
					Db.NonQ(command);
				}
				else {//oracle
					command="ALTER TABLE hl7def ADD SftpInSocket varchar2(255)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('HelpKey','')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'HelpKey','')";
					Db.NonQ(command);
				}
				#region//==========================ADD SMS TABLES===========================
				//Add table eservicesignal
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS eservicesignal";
					Db.NonQ(command);
					command=@"CREATE TABLE eservicesignal (
						EServiceSignalNum bigint NOT NULL auto_increment PRIMARY KEY,
						ServiceCode int NOT NULL,
						ReasonCategory int NOT NULL,
						ReasonCode int NOT NULL,
						Severity tinyint NOT NULL,
						Description varchar(255) NOT NULL,
						SigDateTime datetime NOT NULL DEFAULT '0001-01-01 00:00:00',
						Tag varchar(255) NOT NULL,
						IsProcessed tinyint NOT NULL
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				} else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE eservicesignal'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE eservicesignal (
						EServiceSignalNum number(20) NOT NULL,
						ServiceCode number(11) NOT NULL,
						ReasonCategory number(11) NOT NULL,
						ReasonCode number(11) NOT NULL,
						Severity number(3) NOT NULL,
						Description varchar2(255),
						SigDateTime date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						Tag varchar2(255),
						IsProcessed number(3) NOT NULL,
						CONSTRAINT eservicesignal_EServiceSignalN PRIMARY KEY (EServiceSignalNum)
						)";
					Db.NonQ(command);
				}
				//Add SmsMo table
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS smsmo";
					Db.NonQ(command);
					command=@"CREATE TABLE smsmo (
						SmsMONum bigint NOT NULL auto_increment PRIMARY KEY,
						PatNum bigint NOT NULL,
						ClinicNum bigint NOT NULL,
						CommlogNum bigint NOT NULL,
						MsgText text NOT NULL,
						DateTimeEntry datetime NOT NULL DEFAULT '0001-01-01 00:00:00',
						SmsVln varchar(255) NOT NULL,
						MsgPart varchar(255) NOT NULL,
						MsgTotal varchar(255) NOT NULL,
						MsgRefID varchar(255) NOT NULL,
						INDEX(PatNum),
						INDEX(ClinicNum),
						INDEX(CommlogNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE smsmo'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE smsmo (
						SmsMONum number(20) NOT NULL,
						PatNum number(20) NOT NULL,
						ClinicNum number(20) NOT NULL,
						CommlogNum number(20) NOT NULL,
						MsgText clob,
						DateTimeEntry date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						SmsVln varchar2(255),
						MsgPart varchar2(255),
						MsgTotal varchar2(255),
						MsgRefID varchar2(255),
						CONSTRAINT smsmo_SmsMONum PRIMARY KEY (SmsMONum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX smsmo_PatNum ON smsmo (PatNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX smsmo_ClinicNum ON smsmo (ClinicNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX smsmo_CommlogNum ON smsmo (CommlogNum)";
					Db.NonQ(command);
				}
				//Add SmsMt table
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS smsmt";
					Db.NonQ(command);
					command=@"CREATE TABLE smsmt (
						SmsMTNum bigint NOT NULL auto_increment PRIMARY KEY,
						PatNum bigint NOT NULL,
						GuidMessage varchar(255) NOT NULL,
						GuidBatch varchar(255) NOT NULL,
						VlnNumber varchar(255) NOT NULL,
						PhonePat varchar(255) NOT NULL,
						IsTimeSensitive tinyint NOT NULL,
						MsgType tinyint NOT NULL,
						MsgText text NOT NULL,
						Status tinyint NOT NULL,
						MsgParts int NOT NULL,
						MsgCost double NOT NULL,
						ClinicNum bigint NOT NULL,
						CustErrorText varchar(255) NOT NULL,
						DateTimeEntry datetime NOT NULL DEFAULT '0001-01-01 00:00:00',
						DateTimeTerminated datetime NOT NULL DEFAULT '0001-01-01 00:00:00',
						INDEX(PatNum),
						INDEX(ClinicNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				}
				else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE smsmt'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE smsmt (
						SmsMTNum number(20) NOT NULL,
						PatNum number(20) NOT NULL,
						GuidMessage varchar2(255),
						GuidBatch varchar2(255),
						VlnNumber varchar2(255),
						PhonePat varchar2(255),
						IsTimeSensitive number(3) NOT NULL,
						MsgType number(3) NOT NULL,
						MsgText clob,
						Status number(3) NOT NULL,
						MsgParts number(11) NOT NULL,
						MsgCost number(38,8) NOT NULL,
						ClinicNum number(20) NOT NULL,
						CustErrorText varchar2(255),
						DateTimeEntry date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						DateTimeTerminated date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						CONSTRAINT smsmt_SmsMTNum PRIMARY KEY (SmsMTNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX smsmt_PatNum ON smsmt (PatNum)";
					Db.NonQ(command);
					command=@"CREATE INDEX smsmt_ClinicNum ON smsmt (ClinicNum)";
					Db.NonQ(command);
				}
				//add table smsvln
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="DROP TABLE IF EXISTS smsvln";
					Db.NonQ(command);
					command=@"CREATE TABLE smsvln (
						SmsVlnNum bigint NOT NULL auto_increment PRIMARY KEY,
						ClinicNum bigint NOT NULL,
						VlnNumber varchar(255) NOT NULL,
						DateActive datetime NOT NULL DEFAULT '0001-01-01 00:00:00',
						DateInactive datetime NOT NULL DEFAULT '0001-01-01 00:00:00',
						InactiveCode varchar(255) NOT NULL,
						INDEX(ClinicNum)
						) DEFAULT CHARSET=utf8";
					Db.NonQ(command);
				} else {//oracle
					command="BEGIN EXECUTE IMMEDIATE 'DROP TABLE smsvln'; EXCEPTION WHEN OTHERS THEN NULL; END;";
					Db.NonQ(command);
					command=@"CREATE TABLE smsvln (
						SmsVlnNum number(20) NOT NULL,
						ClinicNum number(20) NOT NULL,
						VlnNumber varchar2(255),
						DateActive date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						DateInactive date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD') NOT NULL,
						InactiveCode varchar2(255),
						CONSTRAINT smsvln_SmsVlnNum PRIMARY KEY (SmsVlnNum)
						)";
					Db.NonQ(command);
					command=@"CREATE INDEX smsvln_ClinicNum ON smsvln (ClinicNum)";
					Db.NonQ(command);
				}
				#endregion//========================END ADD SMS TABLES=========================
				//SMS contract fields
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('SmsContractDate','')";
					Db.NonQ(command);
				} else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'SmsContractDate','')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('SmsContractName','')";
					Db.NonQ(command);
				} else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'SmsContractName','')";
					Db.NonQ(command);
				} 
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE clinic ADD SmsContractDate datetime NOT NULL DEFAULT '0001-01-01 00:00:00'";
					Db.NonQ(command);
				} else {//oracle
					command="ALTER TABLE clinic ADD SmsContractDate date DEFAULT TO_DATE('0001-01-01','YYYY-MM-DD')";
					Db.NonQ(command);
					command="UPDATE clinic SET SmsContractDate = TO_DATE('0001-01-01','YYYY-MM-DD') WHERE SmsContractDate IS NULL";
					Db.NonQ(command);
					command="ALTER TABLE clinic MODIFY SmsContractDate NOT NULL";
					Db.NonQ(command);
				} 
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="ALTER TABLE clinic ADD SmsContractName varchar(255) NOT NULL";
					Db.NonQ(command);
				} else {//oracle
					command="ALTER TABLE clinic ADD SmsContractName varchar2(255)";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="SELECT ProgramNum FROM program WHERE ProgName='Xcharge' LIMIT 1";
				}
				else {//oracle doesn't have LIMIT
					command="SELECT ProgramNum FROM (SELECT ProgramNum FROM program WHERE ProgName='Xcharge') WHERE RowNum<=1";
				}
				long programNum=PIn.Long(Db.GetScalar(command));
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"'"+POut.Long(programNum)+"', "
						+"'XWebID', "
						+"'')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"'"+POut.Long(programNum)+"', "
						+"'AuthKey', "
						+"'')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"'"+POut.Long(programNum)+"', "
						+"'TerminalID', "
						+"'')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
						+"'"+POut.Long(programNum)+"', "
						+"'XWebID', "
						+"'')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
						+"'"+POut.Long(programNum)+"', "
						+"'AuthKey', "
						+"'')";
					Db.NonQ(command);
					command="INSERT INTO programproperty (ProgramPropertyNum,ProgramNum,PropertyDesc,PropertyValue"
						+") VALUES("
						+"(SELECT MAX(ProgramPropertyNum+1) FROM programproperty),"
						+"'"+POut.Long(programNum)+"', "
						+"'TerminalID', "
						+"'')";
					Db.NonQ(command);
				}//end X-Web properties.
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('SignalInactiveMinutes','0')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'SignalInactiveMinutes','0')";
					Db.NonQ(command);
				}
				//Add waiting room prefs
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('WaitingRoomAlertColor','-16777216')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'WaitingRoomAlertColor','-16777216')";
					Db.NonQ(command);
				}
				if(DataConnection.DBtype==DatabaseType.MySql) {
					command="INSERT INTO preference(PrefName,ValueString) VALUES('WaitingRoomAlertTime','0')";
					Db.NonQ(command);
				}
				else {//oracle
					command="INSERT INTO preference(PrefNum,PrefName,ValueString) VALUES((SELECT MAX(PrefNum)+1 FROM preference),'WaitingRoomAlertTime','0')";
					Db.NonQ(command);
				}
				//Give all users with Setup permission the new EServices permission------------------------------------------------------
				command="SELECT DISTINCT UserGroupNum FROM grouppermission WHERE PermType=8";//Setup
				DataTable table=Db.GetTable(command);
				long groupNum;
				if(DataConnection.DBtype==DatabaseType.MySql) {
					for(int i=0;i<table.Rows.Count;i++) {
						groupNum=PIn.Long(table.Rows[i]["UserGroupNum"].ToString());
						command="INSERT INTO grouppermission (UserGroupNum,PermType) "
							+"VALUES("+POut.Long(groupNum)+",91)";//EServices
						Db.NonQ32(command);
					}
				}
				else {//oracle
					for(int i=0;i<table.Rows.Count;i++) {
						groupNum=PIn.Long(table.Rows[i]["UserGroupNum"].ToString());
						command="INSERT INTO grouppermission (GroupPermNum,NewerDays,UserGroupNum,PermType) "
							+"VALUES((SELECT MAX(GroupPermNum)+1 FROM grouppermission),0,"+POut.Long(groupNum)+",91)";//EServices
						Db.NonQ32(command);
					}
				}
				//Listener Service monitoring.  As of right now the Patient Portal is the only important eService.  
				//Turn on Listener Service monitoring for offices using the patient portal by inserting an eService signal of status 'Critical'
				if(DataConnection.DBtype==DatabaseType.MySql) {
					//Patients with OnlinePasswords will be the indicator that an office is or has attempted to use the patient portal.
					command="SELECT COUNT(*) FROM patient WHERE OnlinePassword!=''";
					int countPatPortals=PIn.Int(Db.GetCount(command));
					if(countPatPortals > 5) {//Check for more than 5 patient portal patients to avoid false positives.
						//Insert a 'Critical' signal into the eservicesignal table to trigger Listener Service monitoring.
						//Customers with active Listener Services will instantly have a 'Working' signal inserted and will not get notified of the service being down.
						//However, if the customer does not know that their service is down (not tech savy) then this will alert them to that fact (our goal).
						command="INSERT INTO eservicesignal (ServiceCode,ReasonCategory,ReasonCode,Severity,Description,SigDateTime,Tag,IsProcessed) VALUES("
						+"1,"//ListenerService
						+"0,"
						+"0,"
						+"5,"//Critical
						+"'Patient Portal users detected.  Listener Service status set to critical to trigger monitoring.',"
						+POut.DateT(DateTime.Now)+","
						+"'',"
						+"0)";
						Db.NonQ(command);
					}
				}
				else {//Oracle
					//eServices do not currently support Oracle.
				}
				command="UPDATE preference SET ValueString = '15.2.1.0' WHERE PrefName = 'DataBaseVersion'";
				Db.NonQ(command);
			}
			//To15_2_X();
		}
		


	}
}
















				

