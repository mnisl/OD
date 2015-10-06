//This file is automatically generated.
//Do not attempt to make changes to this file because the changes will be erased and overwritten.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace OpenDentBusiness.Crud{
	public class TreatPlanCrud {
		///<summary>Gets one TreatPlan object from the database using the primary key.  Returns null if not found.</summary>
		public static TreatPlan SelectOne(long treatPlanNum){
			string command="SELECT * FROM treatplan "
				+"WHERE TreatPlanNum = "+POut.Long(treatPlanNum);
			List<TreatPlan> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets one TreatPlan object from the database using a query.</summary>
		public static TreatPlan SelectOne(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<TreatPlan> list=TableToList(Db.GetTable(command));
			if(list.Count==0) {
				return null;
			}
			return list[0];
		}

		///<summary>Gets a list of TreatPlan objects from the database using a query.</summary>
		public static List<TreatPlan> SelectMany(string command){
			if(RemotingClient.RemotingRole==RemotingRole.ClientWeb) {
				throw new ApplicationException("Not allowed to send sql directly.  Rewrite the calling class to not use this query:\r\n"+command);
			}
			List<TreatPlan> list=TableToList(Db.GetTable(command));
			return list;
		}

		///<summary>Converts a DataTable to a list of objects.</summary>
		public static List<TreatPlan> TableToList(DataTable table){
			List<TreatPlan> retVal=new List<TreatPlan>();
			TreatPlan treatPlan;
			for(int i=0;i<table.Rows.Count;i++) {
				treatPlan=new TreatPlan();
				treatPlan.TreatPlanNum= PIn.Long  (table.Rows[i]["TreatPlanNum"].ToString());
				treatPlan.PatNum      = PIn.Long  (table.Rows[i]["PatNum"].ToString());
				treatPlan.DateTP      = PIn.Date  (table.Rows[i]["DateTP"].ToString());
				treatPlan.Heading     = PIn.String(table.Rows[i]["Heading"].ToString());
				treatPlan.Note        = PIn.String(table.Rows[i]["Note"].ToString());
				treatPlan.Signature   = PIn.String(table.Rows[i]["Signature"].ToString());
				treatPlan.SigIsTopaz  = PIn.Bool  (table.Rows[i]["SigIsTopaz"].ToString());
				treatPlan.ResponsParty= PIn.Long  (table.Rows[i]["ResponsParty"].ToString());
				retVal.Add(treatPlan);
			}
			return retVal;
		}

		///<summary>Inserts one TreatPlan into the database.  Returns the new priKey.</summary>
		public static long Insert(TreatPlan treatPlan){
			if(DataConnection.DBtype==DatabaseType.Oracle) {
				treatPlan.TreatPlanNum=DbHelper.GetNextOracleKey("treatplan","TreatPlanNum");
				int loopcount=0;
				while(loopcount<100){
					try {
						return Insert(treatPlan,true);
					}
					catch(Oracle.DataAccess.Client.OracleException ex){
						if(ex.Number==1 && ex.Message.ToLower().Contains("unique constraint") && ex.Message.ToLower().Contains("violated")){
							treatPlan.TreatPlanNum++;
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
				return Insert(treatPlan,false);
			}
		}

		///<summary>Inserts one TreatPlan into the database.  Provides option to use the existing priKey.</summary>
		public static long Insert(TreatPlan treatPlan,bool useExistingPK){
			if(!useExistingPK && PrefC.RandomKeys) {
				treatPlan.TreatPlanNum=ReplicationServers.GetKey("treatplan","TreatPlanNum");
			}
			string command="INSERT INTO treatplan (";
			if(useExistingPK || PrefC.RandomKeys) {
				command+="TreatPlanNum,";
			}
			command+="PatNum,DateTP,Heading,Note,Signature,SigIsTopaz,ResponsParty) VALUES(";
			if(useExistingPK || PrefC.RandomKeys) {
				command+=POut.Long(treatPlan.TreatPlanNum)+",";
			}
			command+=
				     POut.Long  (treatPlan.PatNum)+","
				+    POut.Date  (treatPlan.DateTP)+","
				+"'"+POut.String(treatPlan.Heading)+"',"
				+"'"+POut.String(treatPlan.Note)+"',"
				+"'"+POut.String(treatPlan.Signature)+"',"
				+    POut.Bool  (treatPlan.SigIsTopaz)+","
				+    POut.Long  (treatPlan.ResponsParty)+")";
			if(useExistingPK || PrefC.RandomKeys) {
				Db.NonQ(command);
			}
			else {
				treatPlan.TreatPlanNum=Db.NonQ(command,true);
			}
			return treatPlan.TreatPlanNum;
		}

		///<summary>Updates one TreatPlan in the database.</summary>
		public static void Update(TreatPlan treatPlan){
			string command="UPDATE treatplan SET "
				+"PatNum      =  "+POut.Long  (treatPlan.PatNum)+", "
				+"DateTP      =  "+POut.Date  (treatPlan.DateTP)+", "
				+"Heading     = '"+POut.String(treatPlan.Heading)+"', "
				+"Note        = '"+POut.String(treatPlan.Note)+"', "
				+"Signature   = '"+POut.String(treatPlan.Signature)+"', "
				+"SigIsTopaz  =  "+POut.Bool  (treatPlan.SigIsTopaz)+", "
				+"ResponsParty=  "+POut.Long  (treatPlan.ResponsParty)+" "
				+"WHERE TreatPlanNum = "+POut.Long(treatPlan.TreatPlanNum);
			Db.NonQ(command);
		}

		///<summary>Updates one TreatPlan in the database.  Uses an old object to compare to, and only alters changed fields.  This prevents collisions and concurrency problems in heavily used tables.  Returns true if an update occurred.</summary>
		public static bool Update(TreatPlan treatPlan,TreatPlan oldTreatPlan){
			string command="";
			if(treatPlan.PatNum != oldTreatPlan.PatNum) {
				if(command!=""){ command+=",";}
				command+="PatNum = "+POut.Long(treatPlan.PatNum)+"";
			}
			if(treatPlan.DateTP != oldTreatPlan.DateTP) {
				if(command!=""){ command+=",";}
				command+="DateTP = "+POut.Date(treatPlan.DateTP)+"";
			}
			if(treatPlan.Heading != oldTreatPlan.Heading) {
				if(command!=""){ command+=",";}
				command+="Heading = '"+POut.String(treatPlan.Heading)+"'";
			}
			if(treatPlan.Note != oldTreatPlan.Note) {
				if(command!=""){ command+=",";}
				command+="Note = '"+POut.String(treatPlan.Note)+"'";
			}
			if(treatPlan.Signature != oldTreatPlan.Signature) {
				if(command!=""){ command+=",";}
				command+="Signature = '"+POut.String(treatPlan.Signature)+"'";
			}
			if(treatPlan.SigIsTopaz != oldTreatPlan.SigIsTopaz) {
				if(command!=""){ command+=",";}
				command+="SigIsTopaz = "+POut.Bool(treatPlan.SigIsTopaz)+"";
			}
			if(treatPlan.ResponsParty != oldTreatPlan.ResponsParty) {
				if(command!=""){ command+=",";}
				command+="ResponsParty = "+POut.Long(treatPlan.ResponsParty)+"";
			}
			if(command==""){
				return false;
			}
			command="UPDATE treatplan SET "+command
				+" WHERE TreatPlanNum = "+POut.Long(treatPlan.TreatPlanNum);
			Db.NonQ(command);
			return true;
		}

		///<summary>Deletes one TreatPlan from the database.</summary>
		public static void Delete(long treatPlanNum){
			string command="DELETE FROM treatplan "
				+"WHERE TreatPlanNum = "+POut.Long(treatPlanNum);
			Db.NonQ(command);
		}

	}
}