//This file is automatically generated.
//Do not attempt to make changes to this file because the changes will be erased and overwritten.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace OpenDentBusiness.Crud{
	public class EServiceSignalCrud {
		///<summary>Gets one EServiceSignal object from the database using the primary key.  Returns null if not found.</summary>
		public static EServiceSignal SelectOne(long eServiceSignalNum){
			string command="SELECT * FROM eservicesignal "
				+"WHERE EServiceSignalNum = "+POut.Long(eServiceSignalNum);
			List<EServiceSignal> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets one EServiceSignal object from the database using a query.</summary>
		public static EServiceSignal SelectOne(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<EServiceSignal> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets a list of EServiceSignal objects from the database using a query.</summary>
		public static List<EServiceSignal> SelectMany(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<EServiceSignal> list=TableToList(Db.GetTable(command));
			return list;
		}

		///<summary>Converts a DataTable to a list of objects.</summary>
		public static List<EServiceSignal> TableToList(DataTable table){
			List<EServiceSignal> retVal=new List<EServiceSignal>();
			EServiceSignal eServiceSignal;
			for(int i=0;i<table.Rows.Count;i++) {
				eServiceSignal=new EServiceSignal();
				eServiceSignal.EServiceSignalNum= PIn.Long  (table.Rows[i]["EServiceSignalNum"].ToString());
				eServiceSignal.ServiceCode      = PIn.Int   (table.Rows[i]["ServiceCode"].ToString());
				eServiceSignal.ReasonCategory   = PIn.Int   (table.Rows[i]["ReasonCategory"].ToString());
				eServiceSignal.ReasonCode       = PIn.Int   (table.Rows[i]["ReasonCode"].ToString());
				eServiceSignal.Severity         = (OpenDentBusiness.eServiceSignalSeverity)PIn.Int(table.Rows[i]["Severity"].ToString());
				eServiceSignal.Description      = PIn.String(table.Rows[i]["Description"].ToString());
				eServiceSignal.SigDateTime      = PIn.DateT (table.Rows[i]["SigDateTime"].ToString());
				eServiceSignal.Tag              = PIn.String(table.Rows[i]["Tag"].ToString());
				eServiceSignal.IsProcessed      = PIn.Bool  (table.Rows[i]["IsProcessed"].ToString());
				retVal.Add(eServiceSignal);
			}
			return retVal;
		}

		///<summary>Inserts one EServiceSignal into the database.  Returns the new priKey.</summary>
		public static long Insert(EServiceSignal eServiceSignal){
			if(DataConnection.DBtype==DatabaseType.Oracle) {
				eServiceSignal.EServiceSignalNum=DbHelper.GetNextOracleKey("eservicesignal","EServiceSignalNum");
				int loopcount=0;
				while(loopcount<100){
					try {
						return Insert(eServiceSignal,true);
					}
					catch(Oracle.DataAccess.Client.OracleException ex){
						if(ex.Number==1 && ex.Message.ToLower().Contains("unique constraint") && ex.Message.ToLower().Contains("violated")){
							eServiceSignal.EServiceSignalNum++;
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
				return Insert(eServiceSignal,false);
			}
		}

		///<summary>Inserts one EServiceSignal into the database.  Provides option to use the existing priKey.</summary>
		public static long Insert(EServiceSignal eServiceSignal,bool useExistingPK){
			if(!useExistingPK && PrefC.RandomKeys) {
				eServiceSignal.EServiceSignalNum=ReplicationServers.GetKey("eservicesignal","EServiceSignalNum");
			}
			string command="INSERT INTO eservicesignal (";
			if(useExistingPK || PrefC.RandomKeys) {
				command+="EServiceSignalNum,";
			}
			command+="ServiceCode,ReasonCategory,ReasonCode,Severity,Description,SigDateTime,Tag,IsProcessed) VALUES(";
			if(useExistingPK || PrefC.RandomKeys) {
				command+=POut.Long(eServiceSignal.EServiceSignalNum)+",";
			}
			command+=
				     POut.Int   (eServiceSignal.ServiceCode)+","
				+    POut.Int   (eServiceSignal.ReasonCategory)+","
				+    POut.Int   (eServiceSignal.ReasonCode)+","
				+    POut.Int   ((int)eServiceSignal.Severity)+","
				+"'"+POut.String(eServiceSignal.Description)+"',"
				+    POut.DateT (eServiceSignal.SigDateTime)+","
				+"'"+POut.String(eServiceSignal.Tag)+"',"
				+    POut.Bool  (eServiceSignal.IsProcessed)+")";
			if(useExistingPK || PrefC.RandomKeys) {
				Db.NonQ(command);
			}
			else {
				eServiceSignal.EServiceSignalNum=Db.NonQ(command,true);
			}
			return eServiceSignal.EServiceSignalNum;
		}

		///<summary>Updates one EServiceSignal in the database.</summary>
		public static void Update(EServiceSignal eServiceSignal){
			string command="UPDATE eservicesignal SET "
				+"ServiceCode      =  "+POut.Int   (eServiceSignal.ServiceCode)+", "
				+"ReasonCategory   =  "+POut.Int   (eServiceSignal.ReasonCategory)+", "
				+"ReasonCode       =  "+POut.Int   (eServiceSignal.ReasonCode)+", "
				+"Severity         =  "+POut.Int   ((int)eServiceSignal.Severity)+", "
				+"Description      = '"+POut.String(eServiceSignal.Description)+"', "
				+"SigDateTime      =  "+POut.DateT (eServiceSignal.SigDateTime)+", "
				+"Tag              = '"+POut.String(eServiceSignal.Tag)+"', "
				+"IsProcessed      =  "+POut.Bool  (eServiceSignal.IsProcessed)+" "
				+"WHERE EServiceSignalNum = "+POut.Long(eServiceSignal.EServiceSignalNum);
			Db.NonQ(command);
		}

		///<summary>Updates one EServiceSignal in the database.  Uses an old object to compare to, and only alters changed fields.  This prevents collisions and concurrency problems in heavily used tables.  Returns true if an update occurred.</summary>
		public static bool Update(EServiceSignal eServiceSignal,EServiceSignal oldEServiceSignal){
			string command="";
			if(eServiceSignal.ServiceCode != oldEServiceSignal.ServiceCode) {
				if(command!=""){ command+=",";}
				command+="ServiceCode = "+POut.Int(eServiceSignal.ServiceCode)+"";
			}
			if(eServiceSignal.ReasonCategory != oldEServiceSignal.ReasonCategory) {
				if(command!=""){ command+=",";}
				command+="ReasonCategory = "+POut.Int(eServiceSignal.ReasonCategory)+"";
			}
			if(eServiceSignal.ReasonCode != oldEServiceSignal.ReasonCode) {
				if(command!=""){ command+=",";}
				command+="ReasonCode = "+POut.Int(eServiceSignal.ReasonCode)+"";
			}
			if(eServiceSignal.Severity != oldEServiceSignal.Severity) {
				if(command!=""){ command+=",";}
				command+="Severity = "+POut.Int   ((int)eServiceSignal.Severity)+"";
			}
			if(eServiceSignal.Description != oldEServiceSignal.Description) {
				if(command!=""){ command+=",";}
				command+="Description = '"+POut.String(eServiceSignal.Description)+"'";
			}
			if(eServiceSignal.SigDateTime != oldEServiceSignal.SigDateTime) {
				if(command!=""){ command+=",";}
				command+="SigDateTime = "+POut.DateT(eServiceSignal.SigDateTime)+"";
			}
			if(eServiceSignal.Tag != oldEServiceSignal.Tag) {
				if(command!=""){ command+=",";}
				command+="Tag = '"+POut.String(eServiceSignal.Tag)+"'";
			}
			if(eServiceSignal.IsProcessed != oldEServiceSignal.IsProcessed) {
				if(command!=""){ command+=",";}
				command+="IsProcessed = "+POut.Bool(eServiceSignal.IsProcessed)+"";
			}
			if(command==""){
				return false;
			}
			command="UPDATE eservicesignal SET "+command
				+" WHERE EServiceSignalNum = "+POut.Long(eServiceSignal.EServiceSignalNum);
			Db.NonQ(command);
			return true;
		}

		///<summary>Deletes one EServiceSignal from the database.</summary>
		public static void Delete(long eServiceSignalNum){
			string command="DELETE FROM eservicesignal "
				+"WHERE EServiceSignalNum = "+POut.Long(eServiceSignalNum);
			Db.NonQ(command);
		}

	}
}