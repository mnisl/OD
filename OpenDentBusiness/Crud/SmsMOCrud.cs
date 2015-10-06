//This file is automatically generated.
//Do not attempt to make changes to this file because the changes will be erased and overwritten.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace OpenDentBusiness.Crud{
	public class SmsMOCrud {
		///<summary>Gets one SmsMO object from the database using the primary key.  Returns null if not found.</summary>
		public static SmsMO SelectOne(long smsMONum){
			string command="SELECT * FROM smsmo "
				+"WHERE SmsMONum = "+POut.Long(smsMONum);
			List<SmsMO> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets one SmsMO object from the database using a query.</summary>
		public static SmsMO SelectOne(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<SmsMO> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets a list of SmsMO objects from the database using a query.</summary>
		public static List<SmsMO> SelectMany(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<SmsMO> list=TableToList(Db.GetTable(command));
			return list;
		}

		///<summary>Converts a DataTable to a list of objects.</summary>
		public static List<SmsMO> TableToList(DataTable table){
			List<SmsMO> retVal=new List<SmsMO>();
			SmsMO smsMO;
			for(int i=0;i<table.Rows.Count;i++) {
				smsMO=new SmsMO();
				smsMO.SmsMONum     = PIn.Long  (table.Rows[i]["SmsMONum"].ToString());
				smsMO.PatNum       = PIn.Long  (table.Rows[i]["PatNum"].ToString());
				smsMO.ClinicNum    = PIn.Long  (table.Rows[i]["ClinicNum"].ToString());
				smsMO.CommlogNum   = PIn.Long  (table.Rows[i]["CommlogNum"].ToString());
				smsMO.MsgText      = PIn.String(table.Rows[i]["MsgText"].ToString());
				smsMO.DateTimeEntry= PIn.DateT (table.Rows[i]["DateTimeEntry"].ToString());
				smsMO.SmsVln       = PIn.String(table.Rows[i]["SmsVln"].ToString());
				smsMO.MsgPart      = PIn.String(table.Rows[i]["MsgPart"].ToString());
				smsMO.MsgTotal     = PIn.String(table.Rows[i]["MsgTotal"].ToString());
				smsMO.MsgRefID     = PIn.String(table.Rows[i]["MsgRefID"].ToString());
				retVal.Add(smsMO);
			}
			return retVal;
		}

		///<summary>Inserts one SmsMO into the database.  Returns the new priKey.</summary>
		public static long Insert(SmsMO smsMO){
			if(DataConnection.DBtype==DatabaseType.Oracle) {
				smsMO.SmsMONum=DbHelper.GetNextOracleKey("smsmo","SmsMONum");
				int loopcount=0;
				while(loopcount<100){
					try {
						return Insert(smsMO,true);
					}
					catch(Oracle.DataAccess.Client.OracleException ex){
						if(ex.Number==1 && ex.Message.ToLower().Contains("unique constraint") && ex.Message.ToLower().Contains("violated")){
							smsMO.SmsMONum++;
							loopcount++;
						}
						else{
							throw ex;
						}
					}
				}
				throw new ApplicationException("Insert failed.  Could not generate primary key.");
			}
			else {
				return Insert(smsMO,false);
			}
		}

		///<summary>Inserts one SmsMO into the database.  Provides option to use the existing priKey.</summary>
		public static long Insert(SmsMO smsMO,bool useExistingPK){
			if(!useExistingPK && PrefC.RandomKeys) {
				smsMO.SmsMONum=ReplicationServers.GetKey("smsmo","SmsMONum");
			}
			string command="INSERT INTO smsmo (";
			if(useExistingPK || PrefC.RandomKeys) {
				command+="SmsMONum,";
			}
			command+="PatNum,ClinicNum,CommlogNum,MsgText,DateTimeEntry,SmsVln,MsgPart,MsgTotal,MsgRefID) VALUES(";
			if(useExistingPK || PrefC.RandomKeys) {
				command+=POut.Long(smsMO.SmsMONum)+",";
			}
			command+=
				     POut.Long  (smsMO.PatNum)+","
				+    POut.Long  (smsMO.ClinicNum)+","
				+    POut.Long  (smsMO.CommlogNum)+","
				+    DbHelper.ParamChar+"paramMsgText,"
				+    POut.DateT (smsMO.DateTimeEntry)+","
				+"'"+POut.String(smsMO.SmsVln)+"',"
				+"'"+POut.String(smsMO.MsgPart)+"',"
				+"'"+POut.String(smsMO.MsgTotal)+"',"
				+"'"+POut.String(smsMO.MsgRefID)+"')";
			if(smsMO.MsgText==null) {
				smsMO.MsgText="";
			}
			OdSqlParameter paramMsgText=new OdSqlParameter("paramMsgText",OdDbType.Text,POut.StringNote(smsMO.MsgText));
			if(useExistingPK || PrefC.RandomKeys) {
				Db.NonQ(command,paramMsgText);
			}
			else {
				smsMO.SmsMONum=Db.NonQ(command,true,paramMsgText);
			}
			return smsMO.SmsMONum;
		}

		///<summary>Updates one SmsMO in the database.</summary>
		public static void Update(SmsMO smsMO){
			string command="UPDATE smsmo SET "
				+"PatNum       =  "+POut.Long  (smsMO.PatNum)+", "
				+"ClinicNum    =  "+POut.Long  (smsMO.ClinicNum)+", "
				+"CommlogNum   =  "+POut.Long  (smsMO.CommlogNum)+", "
				+"MsgText      =  "+DbHelper.ParamChar+"paramMsgText, "
				+"DateTimeEntry=  "+POut.DateT (smsMO.DateTimeEntry)+", "
				+"SmsVln       = '"+POut.String(smsMO.SmsVln)+"', "
				+"MsgPart      = '"+POut.String(smsMO.MsgPart)+"', "
				+"MsgTotal     = '"+POut.String(smsMO.MsgTotal)+"', "
				+"MsgRefID     = '"+POut.String(smsMO.MsgRefID)+"' "
				+"WHERE SmsMONum = "+POut.Long(smsMO.SmsMONum);
			if(smsMO.MsgText==null) {
				smsMO.MsgText="";
			}
			OdSqlParameter paramMsgText=new OdSqlParameter("paramMsgText",OdDbType.Text,POut.StringNote(smsMO.MsgText));
			Db.NonQ(command,paramMsgText);
		}

		///<summary>Updates one SmsMO in the database.  Uses an old object to compare to, and only alters changed fields.  This prevents collisions and concurrency problems in heavily used tables.  Returns true if an update occurred.</summary>
		public static bool Update(SmsMO smsMO,SmsMO oldSmsMO){
			string command="";
			if(smsMO.PatNum != oldSmsMO.PatNum) {
				if(command!=""){ command+=",";}
				command+="PatNum = "+POut.Long(smsMO.PatNum)+"";
			}
			if(smsMO.ClinicNum != oldSmsMO.ClinicNum) {
				if(command!=""){ command+=",";}
				command+="ClinicNum = "+POut.Long(smsMO.ClinicNum)+"";
			}
			if(smsMO.CommlogNum != oldSmsMO.CommlogNum) {
				if(command!=""){ command+=",";}
				command+="CommlogNum = "+POut.Long(smsMO.CommlogNum)+"";
			}
			if(smsMO.MsgText != oldSmsMO.MsgText) {
				if(command!=""){ command+=",";}
				command+="MsgText = "+DbHelper.ParamChar+"paramMsgText";
			}
			if(smsMO.DateTimeEntry != oldSmsMO.DateTimeEntry) {
				if(command!=""){ command+=",";}
				command+="DateTimeEntry = "+POut.DateT(smsMO.DateTimeEntry)+"";
			}
			if(smsMO.SmsVln != oldSmsMO.SmsVln) {
				if(command!=""){ command+=",";}
				command+="SmsVln = '"+POut.String(smsMO.SmsVln)+"'";
			}
			if(smsMO.MsgPart != oldSmsMO.MsgPart) {
				if(command!=""){ command+=",";}
				command+="MsgPart = '"+POut.String(smsMO.MsgPart)+"'";
			}
			if(smsMO.MsgTotal != oldSmsMO.MsgTotal) {
				if(command!=""){ command+=",";}
				command+="MsgTotal = '"+POut.String(smsMO.MsgTotal)+"'";
			}
			if(smsMO.MsgRefID != oldSmsMO.MsgRefID) {
				if(command!=""){ command+=",";}
				command+="MsgRefID = '"+POut.String(smsMO.MsgRefID)+"'";
			}
			if(command==""){
				return false;
			}
			if(smsMO.MsgText==null) {
				smsMO.MsgText="";
			}
			OdSqlParameter paramMsgText=new OdSqlParameter("paramMsgText",OdDbType.Text,POut.StringNote(smsMO.MsgText));
			command="UPDATE smsmo SET "+command
				+" WHERE SmsMONum = "+POut.Long(smsMO.SmsMONum);
			Db.NonQ(command,paramMsgText);
			return true;
		}

		///<summary>Deletes one SmsMO from the database.</summary>
		public static void Delete(long smsMONum){
			string command="DELETE FROM smsmo "
				+"WHERE SmsMONum = "+POut.Long(smsMONum);
			Db.NonQ(command);
		}

	}
}