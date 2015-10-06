//This file is automatically generated.
//Do not attempt to make changes to this file because the changes will be erased and overwritten.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace OpenDentBusiness.Crud{
	public class ReconcileCrud {
		///<summary>Gets one Reconcile object from the database using the primary key.  Returns null if not found.</summary>
		public static Reconcile SelectOne(long reconcileNum){
			string command="SELECT * FROM reconcile "
				+"WHERE ReconcileNum = "+POut.Long(reconcileNum);
			List<Reconcile> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets one Reconcile object from the database using a query.</summary>
		public static Reconcile SelectOne(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<Reconcile> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets a list of Reconcile objects from the database using a query.</summary>
		public static List<Reconcile> SelectMany(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<Reconcile> list=TableToList(Db.GetTable(command));
			return list;
		}

		///<summary>Converts a DataTable to a list of objects.</summary>
		public static List<Reconcile> TableToList(DataTable table){
			List<Reconcile> retVal=new List<Reconcile>();
			Reconcile reconcile;
			for(int i=0;i<table.Rows.Count;i++) {
				reconcile=new Reconcile();
				reconcile.ReconcileNum = PIn.Long  (table.Rows[i]["ReconcileNum"].ToString());
				reconcile.AccountNum   = PIn.Long  (table.Rows[i]["AccountNum"].ToString());
				reconcile.StartingBal  = PIn.Double(table.Rows[i]["StartingBal"].ToString());
				reconcile.EndingBal    = PIn.Double(table.Rows[i]["EndingBal"].ToString());
				reconcile.DateReconcile= PIn.Date  (table.Rows[i]["DateReconcile"].ToString());
				reconcile.IsLocked     = PIn.Bool  (table.Rows[i]["IsLocked"].ToString());
				retVal.Add(reconcile);
			}
			return retVal;
		}

		///<summary>Inserts one Reconcile into the database.  Returns the new priKey.</summary>
		public static long Insert(Reconcile reconcile){
			if(DataConnection.DBtype==DatabaseType.Oracle) {
				reconcile.ReconcileNum=DbHelper.GetNextOracleKey("reconcile","ReconcileNum");
				int loopcount=0;
				while(loopcount<100){
					try {
						return Insert(reconcile,true);
					}
					catch(Oracle.DataAccess.Client.OracleException ex){
						if(ex.Number==1 && ex.Message.ToLower().Contains("unique constraint") && ex.Message.ToLower().Contains("violated")){
							reconcile.ReconcileNum++;
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
				return Insert(reconcile,false);
			}
		}

		///<summary>Inserts one Reconcile into the database.  Provides option to use the existing priKey.</summary>
		public static long Insert(Reconcile reconcile,bool useExistingPK){
			if(!useExistingPK && PrefC.RandomKeys) {
				reconcile.ReconcileNum=ReplicationServers.GetKey("reconcile","ReconcileNum");
			}
			string command="INSERT INTO reconcile (";
			if(useExistingPK || PrefC.RandomKeys) {
				command+="ReconcileNum,";
			}
			command+="AccountNum,StartingBal,EndingBal,DateReconcile,IsLocked) VALUES(";
			if(useExistingPK || PrefC.RandomKeys) {
				command+=POut.Long(reconcile.ReconcileNum)+",";
			}
			command+=
				     POut.Long  (reconcile.AccountNum)+","
				+"'"+POut.Double(reconcile.StartingBal)+"',"
				+"'"+POut.Double(reconcile.EndingBal)+"',"
				+    POut.Date  (reconcile.DateReconcile)+","
				+    POut.Bool  (reconcile.IsLocked)+")";
			if(useExistingPK || PrefC.RandomKeys) {
				Db.NonQ(command);
			}
			else {
				reconcile.ReconcileNum=Db.NonQ(command,true);
			}
			return reconcile.ReconcileNum;
		}

		///<summary>Updates one Reconcile in the database.</summary>
		public static void Update(Reconcile reconcile){
			string command="UPDATE reconcile SET "
				+"AccountNum   =  "+POut.Long  (reconcile.AccountNum)+", "
				+"StartingBal  = '"+POut.Double(reconcile.StartingBal)+"', "
				+"EndingBal    = '"+POut.Double(reconcile.EndingBal)+"', "
				+"DateReconcile=  "+POut.Date  (reconcile.DateReconcile)+", "
				+"IsLocked     =  "+POut.Bool  (reconcile.IsLocked)+" "
				+"WHERE ReconcileNum = "+POut.Long(reconcile.ReconcileNum);
			Db.NonQ(command);
		}

		///<summary>Updates one Reconcile in the database.  Uses an old object to compare to, and only alters changed fields.  This prevents collisions and concurrency problems in heavily used tables.  Returns true if an update occurred.</summary>
		public static bool Update(Reconcile reconcile,Reconcile oldReconcile){
			string command="";
			if(reconcile.AccountNum != oldReconcile.AccountNum) {
				if(command!=""){ command+=",";}
				command+="AccountNum = "+POut.Long(reconcile.AccountNum)+"";
			}
			if(reconcile.StartingBal != oldReconcile.StartingBal) {
				if(command!=""){ command+=",";}
				command+="StartingBal = '"+POut.Double(reconcile.StartingBal)+"'";
			}
			if(reconcile.EndingBal != oldReconcile.EndingBal) {
				if(command!=""){ command+=",";}
				command+="EndingBal = '"+POut.Double(reconcile.EndingBal)+"'";
			}
			if(reconcile.DateReconcile != oldReconcile.DateReconcile) {
				if(command!=""){ command+=",";}
				command+="DateReconcile = "+POut.Date(reconcile.DateReconcile)+"";
			}
			if(reconcile.IsLocked != oldReconcile.IsLocked) {
				if(command!=""){ command+=",";}
				command+="IsLocked = "+POut.Bool(reconcile.IsLocked)+"";
			}
			if(command==""){
				return false;
			}
			command="UPDATE reconcile SET "+command
				+" WHERE ReconcileNum = "+POut.Long(reconcile.ReconcileNum);
			Db.NonQ(command);
			return true;
		}

		///<summary>Deletes one Reconcile from the database.</summary>
		public static void Delete(long reconcileNum){
			string command="DELETE FROM reconcile "
				+"WHERE ReconcileNum = "+POut.Long(reconcileNum);
			Db.NonQ(command);
		}

	}
}