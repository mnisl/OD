//This file is automatically generated.
//Do not attempt to make changes to this file because the changes will be erased and overwritten.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace OpenDentBusiness.Crud{
	public class RecallTypeCrud {
		///<summary>Gets one RecallType object from the database using the primary key.  Returns null if not found.</summary>
		public static RecallType SelectOne(long recallTypeNum){
			string command="SELECT * FROM recalltype "
				+"WHERE RecallTypeNum = "+POut.Long(recallTypeNum);
			List<RecallType> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets one RecallType object from the database using a query.</summary>
		public static RecallType SelectOne(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<RecallType> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets a list of RecallType objects from the database using a query.</summary>
		public static List<RecallType> SelectMany(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<RecallType> list=TableToList(Db.GetTable(command));
			return list;
		}

		///<summary>Converts a DataTable to a list of objects.</summary>
		public static List<RecallType> TableToList(DataTable table){
			List<RecallType> retVal=new List<RecallType>();
			RecallType recallType;
			for(int i=0;i<table.Rows.Count;i++) {
				recallType=new RecallType();
				recallType.RecallTypeNum  = PIn.Long  (table.Rows[i]["RecallTypeNum"].ToString());
				recallType.Description    = PIn.String(table.Rows[i]["Description"].ToString());
				recallType.DefaultInterval= new Interval(PIn.Int(table.Rows[i]["DefaultInterval"].ToString()));
				recallType.TimePattern    = PIn.String(table.Rows[i]["TimePattern"].ToString());
				recallType.Procedures     = PIn.String(table.Rows[i]["Procedures"].ToString());
				retVal.Add(recallType);
			}
			return retVal;
		}

		///<summary>Inserts one RecallType into the database.  Returns the new priKey.</summary>
		public static long Insert(RecallType recallType){
			if(DataConnection.DBtype==DatabaseType.Oracle) {
				recallType.RecallTypeNum=DbHelper.GetNextOracleKey("recalltype","RecallTypeNum");
				int loopcount=0;
				while(loopcount<100){
					try {
						return Insert(recallType,true);
					}
					catch(Oracle.DataAccess.Client.OracleException ex){
						if(ex.Number==1 && ex.Message.ToLower().Contains("unique constraint") && ex.Message.ToLower().Contains("violated")){
							recallType.RecallTypeNum++;
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
				return Insert(recallType,false);
			}
		}

		///<summary>Inserts one RecallType into the database.  Provides option to use the existing priKey.</summary>
		public static long Insert(RecallType recallType,bool useExistingPK){
			if(!useExistingPK && PrefC.RandomKeys) {
				recallType.RecallTypeNum=ReplicationServers.GetKey("recalltype","RecallTypeNum");
			}
			string command="INSERT INTO recalltype (";
			if(useExistingPK || PrefC.RandomKeys) {
				command+="RecallTypeNum,";
			}
			command+="Description,DefaultInterval,TimePattern,Procedures) VALUES(";
			if(useExistingPK || PrefC.RandomKeys) {
				command+=POut.Long(recallType.RecallTypeNum)+",";
			}
			command+=
				 "'"+POut.String(recallType.Description)+"',"
				+    POut.Int   (recallType.DefaultInterval.ToInt())+","
				+"'"+POut.String(recallType.TimePattern)+"',"
				+"'"+POut.String(recallType.Procedures)+"')";
			if(useExistingPK || PrefC.RandomKeys) {
				Db.NonQ(command);
			}
			else {
				recallType.RecallTypeNum=Db.NonQ(command,true);
			}
			return recallType.RecallTypeNum;
		}

		///<summary>Updates one RecallType in the database.</summary>
		public static void Update(RecallType recallType){
			string command="UPDATE recalltype SET "
				+"Description    = '"+POut.String(recallType.Description)+"', "
				+"DefaultInterval=  "+POut.Int   (recallType.DefaultInterval.ToInt())+", "
				+"TimePattern    = '"+POut.String(recallType.TimePattern)+"', "
				+"Procedures     = '"+POut.String(recallType.Procedures)+"' "
				+"WHERE RecallTypeNum = "+POut.Long(recallType.RecallTypeNum);
			Db.NonQ(command);
		}

		///<summary>Updates one RecallType in the database.  Uses an old object to compare to, and only alters changed fields.  This prevents collisions and concurrency problems in heavily used tables.  Returns true if an update occurred.</summary>
		public static bool Update(RecallType recallType,RecallType oldRecallType){
			string command="";
			if(recallType.Description != oldRecallType.Description) {
				if(command!=""){ command+=",";}
				command+="Description = '"+POut.String(recallType.Description)+"'";
			}
			if(recallType.DefaultInterval != oldRecallType.DefaultInterval) {
				if(command!=""){ command+=",";}
				command+="DefaultInterval = "+POut.Int(recallType.DefaultInterval.ToInt())+"";
			}
			if(recallType.TimePattern != oldRecallType.TimePattern) {
				if(command!=""){ command+=",";}
				command+="TimePattern = '"+POut.String(recallType.TimePattern)+"'";
			}
			if(recallType.Procedures != oldRecallType.Procedures) {
				if(command!=""){ command+=",";}
				command+="Procedures = '"+POut.String(recallType.Procedures)+"'";
			}
			if(command==""){
				return false;
			}
			command="UPDATE recalltype SET "+command
				+" WHERE RecallTypeNum = "+POut.Long(recallType.RecallTypeNum);
			Db.NonQ(command);
			return true;
		}

		///<summary>Deletes one RecallType from the database.</summary>
		public static void Delete(long recallTypeNum){
			string command="DELETE FROM recalltype "
				+"WHERE RecallTypeNum = "+POut.Long(recallTypeNum);
			Db.NonQ(command);
		}

	}
}