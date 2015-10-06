//This file is automatically generated.
//Do not attempt to make changes to this file because the changes will be erased and overwritten.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace OpenDentBusiness.Crud{
	public class CustRefEntryCrud {
		///<summary>Gets one CustRefEntry object from the database using the primary key.  Returns null if not found.</summary>
		public static CustRefEntry SelectOne(long custRefEntryNum){
			string command="SELECT * FROM custrefentry "
				+"WHERE CustRefEntryNum = "+POut.Long(custRefEntryNum);
			List<CustRefEntry> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets one CustRefEntry object from the database using a query.</summary>
		public static CustRefEntry SelectOne(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<CustRefEntry> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets a list of CustRefEntry objects from the database using a query.</summary>
		public static List<CustRefEntry> SelectMany(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<CustRefEntry> list=TableToList(Db.GetTable(command));
			return list;
		}

		///<summary>Converts a DataTable to a list of objects.</summary>
		public static List<CustRefEntry> TableToList(DataTable table){
			List<CustRefEntry> retVal=new List<CustRefEntry>();
			CustRefEntry custRefEntry;
			for(int i=0;i<table.Rows.Count;i++) {
				custRefEntry=new CustRefEntry();
				custRefEntry.CustRefEntryNum= PIn.Long  (table.Rows[i]["CustRefEntryNum"].ToString());
				custRefEntry.PatNumCust     = PIn.Long  (table.Rows[i]["PatNumCust"].ToString());
				custRefEntry.PatNumRef      = PIn.Long  (table.Rows[i]["PatNumRef"].ToString());
				custRefEntry.DateEntry      = PIn.Date  (table.Rows[i]["DateEntry"].ToString());
				custRefEntry.Note           = PIn.String(table.Rows[i]["Note"].ToString());
				retVal.Add(custRefEntry);
			}
			return retVal;
		}

		///<summary>Inserts one CustRefEntry into the database.  Returns the new priKey.</summary>
		public static long Insert(CustRefEntry custRefEntry){
			if(DataConnection.DBtype==DatabaseType.Oracle) {
				custRefEntry.CustRefEntryNum=DbHelper.GetNextOracleKey("custrefentry","CustRefEntryNum");
				int loopcount=0;
				while(loopcount<100){
					try {
						return Insert(custRefEntry,true);
					}
					catch(Oracle.DataAccess.Client.OracleException ex){
						if(ex.Number==1 && ex.Message.ToLower().Contains("unique constraint") && ex.Message.ToLower().Contains("violated")){
							custRefEntry.CustRefEntryNum++;
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
				return Insert(custRefEntry,false);
			}
		}

		///<summary>Inserts one CustRefEntry into the database.  Provides option to use the existing priKey.</summary>
		public static long Insert(CustRefEntry custRefEntry,bool useExistingPK){
			if(!useExistingPK && PrefC.RandomKeys) {
				custRefEntry.CustRefEntryNum=ReplicationServers.GetKey("custrefentry","CustRefEntryNum");
			}
			string command="INSERT INTO custrefentry (";
			if(useExistingPK || PrefC.RandomKeys) {
				command+="CustRefEntryNum,";
			}
			command+="PatNumCust,PatNumRef,DateEntry,Note) VALUES(";
			if(useExistingPK || PrefC.RandomKeys) {
				command+=POut.Long(custRefEntry.CustRefEntryNum)+",";
			}
			command+=
				     POut.Long  (custRefEntry.PatNumCust)+","
				+    POut.Long  (custRefEntry.PatNumRef)+","
				+    POut.Date  (custRefEntry.DateEntry)+","
				+"'"+POut.String(custRefEntry.Note)+"')";
			if(useExistingPK || PrefC.RandomKeys) {
				Db.NonQ(command);
			}
			else {
				custRefEntry.CustRefEntryNum=Db.NonQ(command,true);
			}
			return custRefEntry.CustRefEntryNum;
		}

		///<summary>Updates one CustRefEntry in the database.</summary>
		public static void Update(CustRefEntry custRefEntry){
			string command="UPDATE custrefentry SET "
				+"PatNumCust     =  "+POut.Long  (custRefEntry.PatNumCust)+", "
				+"PatNumRef      =  "+POut.Long  (custRefEntry.PatNumRef)+", "
				+"DateEntry      =  "+POut.Date  (custRefEntry.DateEntry)+", "
				+"Note           = '"+POut.String(custRefEntry.Note)+"' "
				+"WHERE CustRefEntryNum = "+POut.Long(custRefEntry.CustRefEntryNum);
			Db.NonQ(command);
		}

		///<summary>Updates one CustRefEntry in the database.  Uses an old object to compare to, and only alters changed fields.  This prevents collisions and concurrency problems in heavily used tables.  Returns true if an update occurred.</summary>
		public static bool Update(CustRefEntry custRefEntry,CustRefEntry oldCustRefEntry){
			string command="";
			if(custRefEntry.PatNumCust != oldCustRefEntry.PatNumCust) {
				if(command!=""){ command+=",";}
				command+="PatNumCust = "+POut.Long(custRefEntry.PatNumCust)+"";
			}
			if(custRefEntry.PatNumRef != oldCustRefEntry.PatNumRef) {
				if(command!=""){ command+=",";}
				command+="PatNumRef = "+POut.Long(custRefEntry.PatNumRef)+"";
			}
			if(custRefEntry.DateEntry != oldCustRefEntry.DateEntry) {
				if(command!=""){ command+=",";}
				command+="DateEntry = "+POut.Date(custRefEntry.DateEntry)+"";
			}
			if(custRefEntry.Note != oldCustRefEntry.Note) {
				if(command!=""){ command+=",";}
				command+="Note = '"+POut.String(custRefEntry.Note)+"'";
			}
			if(command==""){
				return false;
			}
			command="UPDATE custrefentry SET "+command
				+" WHERE CustRefEntryNum = "+POut.Long(custRefEntry.CustRefEntryNum);
			Db.NonQ(command);
			return true;
		}

		///<summary>Deletes one CustRefEntry from the database.</summary>
		public static void Delete(long custRefEntryNum){
			string command="DELETE FROM custrefentry "
				+"WHERE CustRefEntryNum = "+POut.Long(custRefEntryNum);
			Db.NonQ(command);
		}

	}
}